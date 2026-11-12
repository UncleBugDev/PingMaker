using System;
using System.Collections.Generic;
using UnityEngine;

namespace PingMaker.Configuration
{
    public enum PingCategory
    {
        World = 0,     // General world ping (terrain, structure, resource)
        Creature = 1   // Monster / Animal / Boss
    }

    public struct PlayerColorDefinition
    {
        public int Id;
        public string Name;
        public Color Color;

        public PlayerColorDefinition(int id, string name, Color color)
        {
            Id = id;
            Name = name;
            Color = color;
        }
    }

    public static class PlayerColors
    {
        public static readonly PlayerColorDefinition[] Palette = new PlayerColorDefinition[]
        {
            new PlayerColorDefinition(0, "Жёлтый",       new Color(1.00f, 0.90f, 0.05f, 1.0f)), // Bright Gold/Yellow
            new PlayerColorDefinition(1, "Оранжевый",    new Color(1.00f, 0.52f, 0.00f, 1.0f)), // Vibrant Orange
            new PlayerColorDefinition(2, "Красный",      new Color(1.00f, 0.20f, 0.20f, 1.0f)), // Bright Crimson
            new PlayerColorDefinition(3, "Розовый",      new Color(1.00f, 0.25f, 0.65f, 1.0f)), // Neon Pink
            new PlayerColorDefinition(4, "Фиолетовый",   new Color(0.68f, 0.32f, 1.00f, 1.0f)), // Royal Purple
            new PlayerColorDefinition(5, "Синий",        new Color(0.12f, 0.55f, 1.00f, 1.0f)), // Electric Blue
            new PlayerColorDefinition(6, "Голубой",      new Color(0.05f, 0.92f, 1.00f, 1.0f)), // Bright Cyan
            new PlayerColorDefinition(7, "Лайм",         new Color(0.20f, 1.00f, 0.35f, 1.0f)), // Neon Lime Green
        };

        public static PlayerColorDefinition GetColorById(int id)
        {
            if (id >= 0 && id < Palette.Length)
            {
                return Palette[id];
            }
            return Palette[0]; // Default Yellow
        }

        public static Color GetActiveColor()
        {
            int savedIndex = ModConfig.ActiveColorIndex.Value;
            if (savedIndex >= 0 && savedIndex < Palette.Length)
            {
                return Palette[savedIndex].Color;
            }
            return ModConfig.CustomPlayerColor.Value;
        }

        public static string GetActiveColorName()
        {
            int savedIndex = ModConfig.ActiveColorIndex.Value;
            if (savedIndex >= 0 && savedIndex < Palette.Length)
            {
                return Palette[savedIndex].Name;
            }
            return "Свой цвет";
        }
    }
}
