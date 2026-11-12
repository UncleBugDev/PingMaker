# Внутреннее устройство движка Valheim (Камера, Рейкаст, UI и Сущности)

В этом документе приведены результаты декомпиляции `assembly_valheim.dll` и `assembly_utils.dll`, детально описывающие подсистемы камеры, прицеливания, структуры существ и интерфейса.

---

## 1. Подсистема камеры и рейкаст прицела

### 1.1 Получение камеры
В Valheim главной камерой управляет синглтон `GameCamera.instance`:
```csharp
Camera mainCam = Utils.GetMainCamera();
```
* Если `mainCam == null` (например, во время загрузочного экрана), любые расчеты лучей отменяются.

### 1.2 Маска слоев для прицеливания
Игрок может целиться в землю, скалы, строения, предметы или существ. В ваниле используются следующие слои физики Unity:
```csharp
int layerMask = LayerMask.GetMask(
    "Default",
    "static_solid",
    "Default_small",
    "piece",
    "piece_nonsolid",
    "terrain",
    "character",
    "character_net",
    "item",
    "vehicle",
    "Water"
);
```

### 1.3 Алгоритм пуска луча (Raycast)
```csharp
Ray ray = new Ray(mainCam.transform.position, mainCam.transform.forward);
float maxDistance = 1000f; // Дальность до 1 км

if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, layerMask))
{
    // Игнорируем столкновения с коллайдером самого локального игрока
    if (Player.m_localPlayer != null && hit.collider.transform.root == Player.m_localPlayer.transform)
    {
        return;
    }

    // 1. Проверяем, попали ли в существо (монстра или животное)
    Character targetChar = hit.collider.GetComponentInParent<Character>();
    if (targetChar != null && targetChar != Player.m_localPlayer)
    {
        // Цель - монстр/существо!
        Vector3 headPos = targetChar.GetHeadPoint();
        ZDOID zdoid = targetChar.GetZDOID();
        string creatureName = targetChar.GetHoverName();
    }
    else
    {
        // Цель - точка в мире
        Vector3 worldPoint = hit.point;
    }
}
```

---

## 2. Структура персонажей и монстров (`Character.cs`)

Каждое существо в Valheim (`Character`, `MonsterAI`, `Player`) имеет следующую иерархию компонентов:
* `Character.m_visual`: Дочерний `GameObject` с именем `"Visual"`, содержащий 3D-модели (`MeshRenderer`, `SkinnedMeshRenderer`, `LODGroup`).
* `Character.GetHeadPoint()`: Возвращает точную позицию головы в мировых координатах (на основе аниматора и кости `HumanBodyBones.Head` или центра коллайдера).
* `Character.GetHoverName()`: Возвращает переведенное имя существа (например, "Тролль", "Грейдворф").
* `Character.GetLevel()`: Уровень/звездность (1 = 0 звёзд, 2 = 1 звезда, 3 = 2 звезды).
* `Character.GetHealth()` / `GetMaxHealth()`: Текущее и максимальное здоровье.
* `Character.IsBoss()`: Булевый флаг, определяющий, является ли существо боссом биома.

---

## 3. Управление материалами: `MaterialMan` vs `MaterialPropertyBlock`

Valheim оптимизирован для рендеринга сотен объектов с минимальным количеством Draw Calls (GPU instancing).

### 3.1 Архитектура `MaterialMan`
В игре существует встроенный класс `MaterialMan` (в `assembly_valheim.dll`). Он кэширует экземпляры `MaterialPropertyBlock` для каждого `GameObject`:
```csharp
// Включение свечения для существа:
MaterialMan.instance.SetValue(character.gameObject, ShaderProps._EmissionColor, highlightColor * 2f);

// Сброс свечения:
MaterialMan.instance.ResetValue(character.gameObject, ShaderProps._EmissionColor);
```
**Преимущества:**
* Не создает новых материалов в памяти Unity.
* Автоматически обновляет свойства для всех `MeshRenderer` и `SkinnedMeshRenderer` внутри дочерней иерархии `character.gameObject`.

---

## 4. UI-система: 3D-маркеры на Canvas и прилипание к экрану (Edge Clamping)

### 4.1 Почему ScreenSpace-Overlay превосходит WorldSpace Canvas
В Unity WorldSpace Canvas страдает от следующих проблем:
1. Замыливание шрифтов при изменении расстояния.
2. Прорезание сквозь стены и геометрию при приближении (Near Clip Plane).
3. Требует ручного расчета угла поворота (Billboard: `transform.LookAt(Camera)`).

**Решение:**
Использование общего полноэкранного холста `Canvas` с режимом `RenderMode.ScreenSpaceOverlay`.
* Позиция маркера на экране рассчитывается через:
  ```csharp
  Vector3 screenPos = mainCam.WorldToScreenPoint(worldPosition);
  ```
* Если объект находится **перед камерой** (`screenPos.z > 0`), он проецируется напрямую.
* Если объект находится **позади камеры** (`screenPos.z < 0`) или за пределами видимого сектора экрана:
  - Координаты инвертируются: `screenPos *= -1f;`
  - Маркер плавно прижимается к краю экрана (Edge Clamping).
  - Вокруг иконки отображается стрелка-указатель, направленная в сторону цели.
* Рядом с маркером выводится текст с ником игрока и динамическим расстоянием:
  ```csharp
  float distance = Vector3.Distance(mainCam.transform.position, worldPosition);
  distanceText.text = $"[{Mathf.RoundToInt(distance)}m]";
  ```
