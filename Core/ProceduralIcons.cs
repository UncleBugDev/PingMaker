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
