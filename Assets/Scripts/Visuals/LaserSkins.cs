using UnityEngine;

namespace LuminaFlow
{
    /// <summary>Cosmetics tint the outer halo only, preserving the optical color signal.</summary>
    public static class LaserSkins
    {
        public static readonly string[] Ids = { "cyan", "pink", "amber", "aurora" };
        public static readonly string[] Names = { "Nguyên bản", "Hoàng hôn", "Nắng hổ phách", "Cực quang" };
        public static readonly int[] Costs = { 0, 12, 24, 42 };
        public static bool IsUnlocked(string id, int stars)
        {
            for (int i = 0; i < Ids.Length; i++) if (Ids[i] == id) return stars >= Costs[i];
            return false;
        }
        public static Color Halo(Color opticalColor)
        {
            switch (ProgressStore.Skin)
            {
                case "pink": return NeonPalette.Pink;
                case "amber": return NeonPalette.Gold;
                case "aurora": return NeonPalette.Hex("8AF2C1");
                default: return opticalColor;
            }
        }
    }
}
