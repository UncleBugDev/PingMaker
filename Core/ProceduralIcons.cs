using System.Collections.Generic;
using PingMaker.Configuration;
using UnityEngine;

namespace PingMaker.Core
{
    /// <summary>
    /// Генерирует четкие процедурные спрайты в оперативной памяти без внешних ассетов.
    /// </summary>
    public static class ProceduralIcons
    {
        private static readonly Dictionary<string, Sprite> s_spriteCache = new Dictionary<string, Sprite>();

        public static Sprite GetPingSprite(PingCategory category)
        {
            string key = $"ping_{category}";
            if (s_spriteCache.TryGetValue(key, out Sprite cached) && cached != null)
            {
                return cached;
            }

            Sprite generated = (category == PingCategory.Creature) ? GenerateCreaturePingSprite() : GenerateWorldPingSprite();
            s_spriteCache[key] = generated;
            return generated;
        }

        public static Sprite GetPointerArrowSprite()
        {
            const string key = "pointer_arrow";
            if (s_spriteCache.TryGetValue(key, out Sprite cached) && cached != null)
            {
                return cached;
            }

            Sprite generated = GenerateArrowSprite();
            s_spriteCache[key] = generated;
            return generated;
        }

        public static Sprite GetColorPaletteRingSprite()
        {
            const string key = "palette_ring";
            if (s_spriteCache.TryGetValue(key, out Sprite cached) && cached != null)
            {
                return cached;
            }

            Sprite generated = GeneratePaletteRingSprite();
            s_spriteCache[key] = generated;
            return generated;
        }

        public static Sprite GetColorCircleSprite()
        {
            const string key = "color_circle";
            if (s_spriteCache.TryGetValue(key, out Sprite cached) && cached != null)
            {
                return cached;
            }

            Sprite generated = GenerateCircleSprite();
            s_spriteCache[key] = generated;
            return generated;
        }

        public static Sprite GetCheckmarkSprite()
        {
            const string key = "checkmark";
            if (s_spriteCache.TryGetValue(key, out Sprite cached) && cached != null)
            {
                return cached;
            }

            Sprite generated = GenerateCheckmarkSprite();
            s_spriteCache[key] = generated;
            return generated;
        }

        public static Sprite GetPlayerArrowMarkerSprite()
        {
            const string key = "player_arrow_marker";
            if (s_spriteCache.TryGetValue(key, out Sprite cached) && cached != null)
            {
                return cached;
            }

            Sprite generated = GeneratePlayerArrowMarkerSprite();
            s_spriteCache[key] = generated;
            return generated;
        }

        private static Sprite GeneratePlayerArrowMarkerSprite()
        {
            const int size = 48;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Color32[] pixels = new Color32[size * size];
            Color32 clear = new Color32(0, 0, 0, 0);
            Color32 white = new Color32(255, 255, 255, 255);
            Color32 darkOutline = new Color32(18, 16, 14, 250);
            Color32 shadow = new Color32(0, 0, 0, 140);

            for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;

            float center = (size - 1) * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;

                    // Tip at Y=18, base wings at Y=-13, inner notch at Y=-5
                    if (dy <= 18.5f && dy >= -13.5f)
                    {
                        float maxX = (18f - dy) * (13.5f / 31.0f);
                        float minY = -5.0f - (Mathf.Abs(dx) * 8.0f / 13.5f);

                        if (Mathf.Abs(dx) <= maxX && dy >= minY)
                        {
                            pixels[y * size + x] = white;
                        }
                        else if (Mathf.Abs(dx) <= maxX + 2.0f && dy >= minY - 2.0f && dy <= 19.5f)
                        {
                            pixels[y * size + x] = darkOutline;
                        }
                        else if (Mathf.Abs(dx) <= maxX + 3.2f && dy >= minY - 3.2f && dy <= 20.5f)
                        {
                            pixels[y * size + x] = shadow;
                        }
                    }
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Texture2D? s_vikingWindowTex;
        private static Texture2D? s_vikingBtnNormalTex;
        private static Texture2D? s_vikingBtnHoverTex;
        private static Texture2D? s_vikingBtnActiveTex;
        private static Texture2D? s_vikingBannerTex;
        private static Texture2D? s_vikingBoxTex;

        public static Texture2D GetVikingWindowTexture()
        {
            if (s_vikingWindowTex != null) return s_vikingWindowTex;

            const int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Color32[] pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int distLeft = x;
                    int distRight = size - 1 - x;
                    int distBottom = y;
                    int distTop = size - 1 - y;
                    int borderDist = Mathf.Min(Mathf.Min(distLeft, distRight), Mathf.Min(distBottom, distTop));

                    if (borderDist <= 1)
                    {
                        pixels[y * size + x] = new Color32(38, 30, 22, 255);
                    }
                    else if (borderDist == 2 || borderDist == 3)
                    {
                        pixels[y * size + x] = new Color32(205, 164, 88, 255);
                    }
                    else if (borderDist == 4)
                    {
                        pixels[y * size + x] = new Color32(18, 14, 10, 255);
                    }
                    else
                    {
                        byte grain = (byte)((x % 4 == 0) ? 2 : 0);
                        byte vFade = (byte)(Mathf.Clamp((y / (float)size) * 12f, 0f, 15f));
                        byte r = (byte)(22 + grain + vFade);
                        byte g = (byte)(18 + grain + vFade);
                        byte b = (byte)(14 + grain + vFade);
                        pixels[y * size + x] = new Color32(r, g, b, 248);
                    }
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            s_vikingWindowTex = tex;
            return tex;
        }

        public static Texture2D GetVikingButtonTexture(bool hover, bool active)
        {
            if (active && s_vikingBtnActiveTex != null) return s_vikingBtnActiveTex;
            if (hover && s_vikingBtnHoverTex != null) return s_vikingBtnHoverTex;
            if (!hover && !active && s_vikingBtnNormalTex != null) return s_vikingBtnNormalTex;

            const int w = 64;
            const int h = 32;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Color32[] pixels = new Color32[w * h];

            Color32 borderColor = active
                ? new Color32(229, 184, 92, 255)
                : (hover ? new Color32(255, 215, 68, 255) : new Color32(138, 105, 54, 255));

            Color32 bodyColor = active
                ? new Color32(26, 20, 15, 255)
                : (hover ? new Color32(52, 42, 32, 255) : new Color32(36, 28, 22, 255));

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int borderDist = Mathf.Min(Mathf.Min(x, w - 1 - x), Mathf.Min(y, h - 1 - y));

                    if (borderDist <= 1)
                    {
                        pixels[y * w + x] = borderColor;
                    }
                    else if (borderDist == 2)
                    {
                        pixels[y * w + x] = new Color32(18, 14, 10, 255);
                    }
                    else
                    {
                        pixels[y * w + x] = bodyColor;
                    }
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();

            if (active) s_vikingBtnActiveTex = tex;
            else if (hover) s_vikingBtnHoverTex = tex;
            else s_vikingBtnNormalTex = tex;

            return tex;
        }

        public static Texture2D GetVikingBannerTexture()
        {
            if (s_vikingBannerTex != null) return s_vikingBannerTex;

            const int w = 64;
            const int h = 28;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[w * h];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int borderDist = Mathf.Min(Mathf.Min(x, w - 1 - x), Mathf.Min(y, h - 1 - y));
                    if (borderDist <= 1)
                    {
                        pixels[y * w + x] = new Color32(180, 140, 68, 255);
                    }
                    else
                    {
                        pixels[y * w + x] = new Color32(42, 32, 20, 240);
                    }
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            s_vikingBannerTex = tex;
            return tex;
        }

        public static Texture2D GetVikingPreviewBoxTexture()
        {
            if (s_vikingBoxTex != null) return s_vikingBoxTex;

            const int w = 64;
            const int h = 32;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[w * h];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int borderDist = Mathf.Min(Mathf.Min(x, w - 1 - x), Mathf.Min(y, h - 1 - y));
                    if (borderDist <= 1)
                    {
                        pixels[y * w + x] = new Color32(95, 75, 48, 255);
                    }
                    else
                    {
                        pixels[y * w + x] = new Color32(16, 13, 10, 255);
                    }
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            s_vikingBoxTex = tex;
            return tex;
        }

        private static Sprite GenerateWorldPingSprite()
        {
            const int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Color32[] pixels = new Color32[size * size];
            Color32 clear = new Color32(0, 0, 0, 0);
            Color32 white = new Color32(255, 255, 255, 255);
            Color32 shadow = new Color32(0, 0, 0, 200);

            for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;

            float center = (size - 1) * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    // Outer ring (radius 18 to 25) + center diamond (radius <= 7)
                    if ((dist >= 18f && dist <= 25f) || (dist <= 7f))
                    {
                        pixels[y * size + x] = white;
                    }
                    else if ((dist >= 16f && dist <= 27f) || (dist <= 9f))
                    {
                        pixels[y * size + x] = shadow;
                    }
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite GenerateCreaturePingSprite()
        {
            const int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Color32[] pixels = new Color32[size * size];
            Color32 clear = new Color32(0, 0, 0, 0);
            Color32 white = new Color32(255, 255, 255, 255);
            Color32 shadow = new Color32(0, 0, 0, 200);

            for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;

            float center = (size - 1) * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    // Skull / Danger Crosshair: Outer brackets + skull shape
                    bool draw = false;

                    // Four corner brackets
                    if (dist >= 21f && dist <= 27f)
                    {
                        if (Mathf.Abs(dx) >= 6f || Mathf.Abs(dy) >= 6f)
                        {
                            draw = true;
                        }
                    }
                    // Skull head circle
                    float skullDist = Mathf.Sqrt(dx * dx + (dy - 4f) * (dy - 4f));
                    if (skullDist <= 13f)
                    {
                        // Eye sockets
                        bool leftEye = (dx >= -7f && dx <= -2f && dy >= 2f && dy <= 7f);
                        bool rightEye = (dx >= 2f && dx <= 7f && dy >= 2f && dy <= 7f);
                        if (!leftEye && !rightEye)
                        {
                            draw = true;
                        }
                    }
                    // Jaw
                    if (Mathf.Abs(dx) <= 7f && dy >= -13f && dy <= -5f)
                    {
                        // Teeth slots
                        if (Mathf.Abs(dx) != 2f && Mathf.Abs(dx) != 5f)
                        {
                            draw = true;
                        }
                    }

                    int idx = y * size + x;
                    if (draw)
                    {
                        pixels[idx] = white;
                    }
                    else if (dist <= 29f && dist >= 10f)
                    {
                        // outline
                        pixels[idx] = shadow;
                    }
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite GenerateArrowSprite()
        {
            const int size = 48;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Color32[] pixels = new Color32[size * size];
            Color32 clear = new Color32(0, 0, 0, 0);
            Color32 white = new Color32(255, 255, 255, 255);
            Color32 shadow = new Color32(0, 0, 0, 200);

            for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;

            float center = (size - 1) * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;

                    float tipY = 20f;
                    float baseY = -14f;
                    float baseHalfW = 14f;

                    if (dy >= baseY && dy <= tipY)
                    {
                        float progress = (dy - baseY) / (tipY - baseY);
                        float maxDx = (1f - progress) * baseHalfW;

                        if (Mathf.Abs(dx) <= maxDx)
                        {
                            pixels[y * size + x] = white;
                        }
                        else if (Mathf.Abs(dx) <= maxDx + 2.5f && dy >= baseY - 2f && dy <= tipY + 2f)
                        {
                            pixels[y * size + x] = shadow;
                        }
                    }
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite GeneratePaletteRingSprite()
        {
            const int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[size * size];
            float center = (size - 1) * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist >= 44f && dist <= 61f)
                    {
                        pixels[y * size + x] = new Color32(18, 22, 28, 225); // Dark panel background
                    }
                    else if (dist >= 42f && dist <= 63f)
                    {
                        pixels[y * size + x] = new Color32(255, 255, 255, 90); // Subtle metallic border
                    }
                    else
                    {
                        pixels[y * size + x] = new Color32(0, 0, 0, 0);
                    }
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite GenerateCircleSprite()
        {
            const int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[size * size];
            float center = (size - 1) * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist <= 26f)
                    {
                        pixels[y * size + x] = new Color32(255, 255, 255, 255); // Filled circle
                    }
                    else if (dist <= 28.5f)
                    {
                        pixels[y * size + x] = new Color32(0, 0, 0, 160); // Dark border
                    }
                    else
                    {
                        pixels[y * size + x] = new Color32(0, 0, 0, 0);
                    }
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite GenerateCheckmarkSprite()
        {
            const int size = 32;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(0, 0, 0, 0);

            // Draw clean checkmark
            for (int i = 0; i <= 6; i++)
            {
                int x = 7 + i;
                int y = 14 - i;
                SetPixelThick(pixels, size, x, y, new Color32(20, 20, 20, 255));
            }
            for (int i = 0; i <= 12; i++)
            {
                int x = 13 + i;
                int y = 8 + i;
                SetPixelThick(pixels, size, x, y, new Color32(20, 20, 20, 255));
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static void SetPixelThick(Color32[] pixels, int size, int x, int y, Color32 color)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    int px = x + dx;
                    int py = y + dy;
                    if (px >= 0 && px < size && py >= 0 && py < size)
                    {
                        pixels[py * size + px] = color;
                    }
                }
            }
        }
    }
}
