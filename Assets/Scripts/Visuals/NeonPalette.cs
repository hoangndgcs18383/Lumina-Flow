using UnityEngine;

namespace LuminaFlow
{
    public static class NeonPalette
    {
        public static readonly Color Background = Hex("080E20");
        public static readonly Color Panel = Hex("101B32");
        public static readonly Color Cyan = Hex("4DE8EF");
        public static readonly Color Pink = Hex("F067D4");
        public static readonly Color Gold = Hex("FFCE7A");
        public static readonly Color Text = Hex("E8F5FF");
        public static readonly Color Muted = Hex("8196B4");
        public static Color Hex(string hex) { ColorUtility.TryParseHtmlString("#" + hex.TrimStart('#'), out var color); return color; }
        public static Color Beam(string name)
        {
            switch ((name ?? "cyan").ToLowerInvariant())
            {
                case "red": return Hex("FF617E");
                case "green": return Hex("68F4AE");
                case "blue": return Hex("66A6FF");
                case "amber": case "yellow": return Gold;
                case "orange": return Hex("FFAA68");
                case "magenta": case "pink": case "purple": return Pink;
                case "white": return Color.white;
                case "cyan": return Cyan;
                default: return ColorUtility.TryParseHtmlString(name, out var color) ? color : Cyan;
            }
        }
        public static Color Alpha(Color color, float alpha) { color.a = alpha; return color; }
        public static Color HDR(Color color, float intensity) => new Color(color.r * intensity, color.g * intensity, color.b * intensity, color.a);
    }
}
