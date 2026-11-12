# Сетевая архитектура Valheim и разработка RPC (ZRoutedRpc & ZPackage)

Данный документ описывает, как устроена сетевая модель Valheim и как правильно реализовывать мультиплеерные механики для плагинов без рассинхронизаций и багов.

---

## 1. Архитектура сетевого стека Valheim

Valheim использует гибридную клиент-серверную / P2P-архитектуру на базе Steamworks / PlayFab сокетов:
* **`ZNet`**: Главный сетевой узел (сервер или клиент).
* **`ZRoutedRpc`**: Менеджер удалённого вызова процедур (Remote Procedure Calls). Отвечает за маршрутизацию сообщений между всеми пирами.
* **`ZNetScene`**: Управляет сетевыми сущностями в мире. Преобразует `ZDOID` в реальные `GameObject`.
* **`ZDO` (Zero Data Object)**: Базовая сетевая единица состояния сущности в мире. Имеет уникальный идентификатор `ZDOID` (состоит из `userID` и `id`).
* **`ZPackage`**: Буфер сериализации/десериализации бинарных данных.

---

## 2. Принцип работы `ZRoutedRpc`

### 2.1 Константы адресации
* `ZRoutedRpc.Everybody = 0L`: Сообщение отправляется абсолютно всем участникам сессии.
  - **Ключевая особенность:** При вызове `InvokeRoutedRPC(0L, ...)` Valheim **мгновенно выполняет обработчик локально** у отправителя и параллельно отправляет пакет остальным игрокам. Это устраняет ощущение пинга и задержки для создателя действия.
* `ZRoutedRpc.Server = 0L` (в контексте прямого обращения к хосту).
* `peerID`: Конкретный ID удаленного игрока.

### 2.2 Регистрация RPC
Сетевые сообщения должны регистрироваться сразу после инициализации сетевого стека. Идеальное место — хук в `Game.Start()`:

```csharp
[HarmonyPatch(typeof(Game), nameof(Game.Start))]
public static class NetworkHooks
{
    private static void Postfix()
    {
        if (ZRoutedRpc.instance == null) return;

        ZRoutedRpc.instance.Register<ZPackage>("PingMaker_WorldPing", RPC_PingMaker_WorldPing);
        PingMakerPlugin.Log.LogInfo("RPC 'PingMaker_WorldPing' успешно зарегистрирован.");
    }

    private static void RPC_PingMaker_WorldPing(long sender, ZPackage pkg)
    {
        NetworkSync.HandleIncomingPing(sender, pkg);
    }
}
```

---

## 3. Сериализация и десериализация через `ZPackage`

`ZPackage` гарантирует кроссплатформенную совместимость типов и минимальный объем трафика.

### 3.1 Формат пакета пинга для `PingMaker`
Порядок записи и чтения данных обязан быть строго идентичным:

```csharp
public struct PingData
{
    public Vector3 WorldPosition;
    public ZDOID TargetCreatureZDOID;
    public int PingType;
    public Color PingColor;
    public string SenderName;
    public float Duration;

    public ZPackage Serialize()
    {
        ZPackage pkg = new ZPackage();
        pkg.Write(WorldPosition);            // Vector3: 12 байт
        pkg.Write(TargetCreatureZDOID);      // ZDOID: 16 байт
        pkg.Write(PingType);                 // int: 4 байта
        pkg.Write(PingColor.r);              // float: 4 байта
        pkg.Write(PingColor.g);              // float: 4 байта
        pkg.Write(PingColor.b);              // float: 4 байта
        pkg.Write(PingColor.a);              // float: 4 байта
        pkg.Write(SenderName ?? "");         // string с префиксом длины
        pkg.Write(Duration);                 // float: 4 байта
        return pkg;
    }

    public static PingData Deserialize(ZPackage pkg)
    {
        PingData data = new PingData();
        data.WorldPosition = pkg.ReadVector3();
        data.TargetCreatureZDOID = pkg.ReadZDOID();
        data.PingType = pkg.ReadInt();
        float r = pkg.ReadSingle();
        float g = pkg.ReadSingle();
        float b = pkg.ReadSingle();
        float a = pkg.ReadSingle();
        data.PingColor = new Color(r, g, b, a);
        data.SenderName = pkg.ReadString();
        data.Duration = pkg.ReadSingle();
        return data;
    }
}
```

Общий размер такого пакета составляет менее **60 байт**, что практически не нагружает канал даже при активных пингах во время битвы с боссом.

---

## 4. Разрешение `ZDOID` в существо (`Character`) на клиентах

Когда один игрок пингует монстра, передаётся сетевой `ZDOID`:
1. На клиентах-получателях вызывается:
   ```csharp
   if (pingData.TargetCreatureZDOID != ZDOID.None && ZNetScene.instance != null)
   {
       GameObject targetGO = ZNetScene.instance.FindInstance(pingData.TargetCreatureZDOID);
       if (targetGO != null)
       {
           Character character = targetGO.GetComponent<Character>();
           if (character != null)
           {
               // Монстр успешно найден в радиусе прорисовки клиента!
               CreatureHighlighter.ApplyHighlight(character, pingData.PingColor, pingData.Duration);
           }
       }
   }
   ```
2. Если монстр находится слишком далеко от другого игрока (вне зоны активных секторов мира `ZDO`), монстр просто не существует в сцене этого удаленного игрока, и код корректно обрабатывает это через проверку `targetGO != null`, отображая обычную 3D-метку в мировых координатах `WorldPosition`.

---

## 5. Защита от спама и проверка безопасности
1. **Rate Limiting**: Локальный клиент ограничивает частоту установки пингов (например, не чаще одного пинга в 0.3 секунды).
2. **Лимит активных меток**: Каждый клиент хранит максимум фиксированное количество активных меток от одного игрока (старые метки автоматически закрываются).
3. **Проверка корректности координат**: Игнорируются некорректные значения `float.IsNaN` или `float.IsInfinity`.
