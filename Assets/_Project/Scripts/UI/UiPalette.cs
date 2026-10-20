using UnityEngine;

namespace FireReady.UI
{
    /// <summary>Colour tokens from the FireReady design (ISO 7010 meanings for safety colours).</summary>
    public static class UiPalette
    {
        public static readonly Color PrimaryBlue = Hex(0x0C70FB);
        public static readonly Color Danger = Hex(0xD7261E);
        public static readonly Color Warning = Hex(0xF5C518);
        public static readonly Color Safe = Hex(0x12803C);
        public static readonly Color SafeText = Hex(0x6FD99A);
        public static readonly Color WarningText = Hex(0xF5C518);
        public static readonly Color Panel = Hex(0x0E1116, 0.86f);
        public static readonly Color Text = Hex(0xF3F4F6);
        public static readonly Color TextSecondary = Hex(0xC9CED6);
        public static readonly Color TextMuted = Hex(0xAEB4BE);

        public static Color Hex(int rgb, float alpha = 1f) =>
            new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, alpha);
    }
}
