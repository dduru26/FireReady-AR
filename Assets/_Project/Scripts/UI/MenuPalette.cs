using UnityEngine;

namespace FireReady.UI
{
    /// <summary>Light menu colours (Headspace-style surfaces) used by Debrief, History and the menus.</summary>
    public static class MenuPalette
    {
        public static readonly Color Surface = UiPalette.Hex(0xFFFDFE);
        public static readonly Color Soft = UiPalette.Hex(0xFEF7F7);
        public static readonly Color Card = UiPalette.Hex(0xFAF4F4);
        public static readonly Color Border = UiPalette.Hex(0xEAE8E7);
        public static readonly Color Text = UiPalette.Hex(0x3C393A);
        public static readonly Color TextSecondary = UiPalette.Hex(0x5B595A);
        public static readonly Color Track = UiPalette.Hex(0xE6E0D9);
        public static readonly Color Link = UiPalette.Hex(0x0062F2);
        public static readonly Color DangerText = UiPalette.Hex(0xB3201A);

        public static Color OutcomeColor(string outcome) => outcome switch
        {
            "Extinguished" => UiPalette.Safe,
            "EvacuatedCorrectly" => Link,
            "EvacuatedEarly" => UiPalette.Hex(0xB45309),
            "Injured" => DangerText,
            "OutOfControl" => DangerText,
            _ => TextSecondary
        };

        public static string OutcomeLabel(string outcome) => outcome switch
        {
            "Extinguished" => "Extinguished",
            "EvacuatedCorrectly" => "Evacuated safely",
            "EvacuatedEarly" => "Evacuated early",
            "OutOfControl" => "Out of control",
            "Injured" => "Electric shock",
            "Abandoned" => "Abandoned",
            _ => outcome
        };
    }
}
