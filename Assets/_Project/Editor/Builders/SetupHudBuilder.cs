using FireReady.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static FireReady.EditorTools.BuilderUtils;

namespace FireReady.EditorTools
{
    /// <summary>
    /// Builds the AR setup HUD to match the design canvas: dark 86% panels over the
    /// camera, blue pill buttons, Liberation Sans. The canvas is 1170 × 2532 units,
    /// so one design point is 3 units.
    /// </summary>
    internal static class SetupHudBuilder
    {
        const string CanvasName = "Setup HUD";
        internal const float Pt = 3f;

        internal static Sprite s_Rounded;

        static readonly Color Panel = Hex(0x0E1116, 0.86f);
        static readonly Color Dialog = Hex(0x171B21);
        static readonly Color TextColor = Hex(0xF3F4F6);
        static readonly Color TextSecondary = Hex(0xC9CED6);
        static readonly Color Blue = Hex(0x0C70FB);
        static readonly Color SafeText = Hex(0x6FD99A);

        public static SetupHudView Build(Sprite rounded)
        {
            s_Rounded = rounded;

            var old = GameObject.Find(CanvasName);
            if (old != null)
                Object.DestroyImmediate(old);

            // Canvas
            var canvasGo = new GameObject(CanvasName, typeof(RectTransform));
            canvasGo.layer = 5;
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1170, 2532);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var safe = Node("Safe Area", canvasGo.transform);
            Stretch(safe);
            safe.gameObject.AddComponent<SafeAreaFitter>();

            // Status pill (top left)
            var pill = Node("Status Pill", safe);
            pill.anchorMin = pill.anchorMax = pill.pivot = new Vector2(0f, 1f);
            pill.anchoredPosition = new Vector2(16 * Pt, -16 * Pt);
            pill.sizeDelta = new Vector2(0f, 44 * Pt);
            var pillImage = Rounded(pill, Panel, 22 * Pt);
            pillImage.raycastTarget = false;
            var pillLayout = pill.gameObject.AddComponent<HorizontalLayoutGroup>();
            pillLayout.padding = new RectOffset((int)(16 * Pt), (int)(16 * Pt), 0, 0);
            pillLayout.childAlignment = TextAnchor.MiddleLeft;
            pillLayout.childControlWidth = pillLayout.childControlHeight = true;
            pillLayout.childForceExpandWidth = false;
            pillLayout.childForceExpandHeight = true;
            var pillFit = pill.gameObject.AddComponent<ContentSizeFitter>();
            pillFit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            var pillLabel = Label(pill, "Label", "Searching for the floor", 16, TextColor, FontStyles.Bold);
            pillLabel.alignment = TextAlignmentOptions.MidlineLeft;
            pillLabel.textWrappingMode = TextWrappingModes.NoWrap;

            // Scan
            var scan = BottomCard(safe, "Scan Card");
            Label(scan, "Title", "Point your phone at the floor", 22, TextColor, FontStyles.Bold);
            Label(scan, "Body", "Move it slowly from side to side. Good light and a patterned floor help it find the surface faster.", 16, TextSecondary, FontStyles.Normal);

            // Place
            var place = BottomCard(safe, "Place Card");
            Label(place, "Title", "Tap to place the scene", 22, TextColor, FontStyles.Bold);
            Label(place, "Body", "Aim the ring at a clear patch of floor, at least 1 × 1 m.", 16, TextSecondary, FontStyles.Normal);
            var area = Label(place, "Area", "Aim the ring at the floor", 15, SafeText, FontStyles.Bold);
            var placeButton = Pill(place, "Place Button", "Place here", Blue, Color.white, 54);

            // Confirm
            var confirm = BottomCard(safe, "Confirm Card");
            Label(confirm, "Title", "Is the scene in a good spot?", 22, TextColor, FontStyles.Bold);
            Label(confirm, "Body", "Walk around it to check. Later, double-tap the floor to move it.", 16, TextSecondary, FontStyles.Normal);
            var row = Node("Buttons", confirm);
            var rowLayout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 10 * Pt;
            rowLayout.childControlWidth = rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = true;
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 54 * Pt;
            var moveButton = Pill(row, "Move Button", "Move", new Color(1f, 1f, 1f, 0.14f), TextColor, 54);
            var confirmButton = Pill(row, "Confirm Button", "Confirm", Blue, Color.white, 54);

            // Paused overlay (full screen, blocks taps on the scene)
            var paused = Node("Paused Overlay", canvasGo.transform);
            Stretch(paused);
            paused.gameObject.AddComponent<Image>().color = Hex(0x080A0D, 0.72f);
            var dialog = Node("Dialog", paused);
            dialog.anchorMin = dialog.anchorMax = dialog.pivot = new Vector2(0.5f, 0.5f);
            dialog.sizeDelta = new Vector2(358 * Pt, 0f);
            Rounded(dialog, Dialog, 22 * Pt);
            var dialogLayout = dialog.gameObject.AddComponent<VerticalLayoutGroup>();
            dialogLayout.padding = new RectOffset((int)(20 * Pt), (int)(20 * Pt), (int)(24 * Pt), (int)(20 * Pt));
            dialogLayout.spacing = 16 * Pt;
            dialogLayout.childControlWidth = dialogLayout.childControlHeight = true;
            dialogLayout.childForceExpandWidth = true;
            dialogLayout.childForceExpandHeight = false;
            dialog.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            Label(dialog, "Title", "Paused", 30, TextColor, FontStyles.Bold).alignment = TextAlignmentOptions.Center;
            Label(dialog, "Body", "AR tracking was lost. The fire, the timer and your extinguisher are frozen until you resume.", 16, TextSecondary, FontStyles.Normal).alignment = TextAlignmentOptions.Center;
            var statusRow = Node("Tracking Status", dialog);
            var statusBg = Rounded(statusRow, Hex(0x12803C, 0.18f), 12 * Pt);
            statusBg.raycastTarget = false;
            var statusRowLayout = statusRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            statusRowLayout.padding = new RectOffset((int)(14 * Pt), (int)(14 * Pt), (int)(12 * Pt), (int)(12 * Pt));
            statusRowLayout.childControlWidth = statusRowLayout.childControlHeight = true;
            statusRowLayout.childForceExpandWidth = true;
            var statusLabel = Label(statusRow, "Label", "Floor found again. You can resume.", 15, SafeText, FontStyles.Bold);
            var resumeButton = Pill(dialog, "Resume Button", "Resume", Blue, Color.white, 56);

            // View
            var view = canvasGo.AddComponent<SetupHudView>();
            SetRef(view, "statusBackground", pillImage);
            SetRef(view, "statusLabel", pillLabel);
            SetRef(view, "scanCard", scan.gameObject);
            SetRef(view, "placeCard", place.gameObject);
            SetRef(view, "confirmCard", confirm.gameObject);
            SetRef(view, "pausedOverlay", paused.gameObject);
            SetRef(view, "areaLabel", area);
            SetRef(view, "placeButton", placeButton);
            SetRef(view, "moveButton", moveButton);
            SetRef(view, "confirmButton", confirmButton);
            SetRef(view, "pausedTrackingLabel", statusLabel);
            SetRef(view, "pausedTrackingBackground", statusBg);
            SetRef(view, "resumeButton", resumeButton);
            return view;
        }

        // ---- Helpers ---------------------------------------------------------

        internal static RectTransform Node(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = 5 };
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        internal static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        internal static Image Rounded(RectTransform rt, Color color, float radius)
        {
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = s_Rounded;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 64f / Mathf.Max(1f, radius); // sprite corner is 64 px
            image.color = color;
            return image;
        }

        internal static RectTransform BottomCard(RectTransform safe, string name)
        {
            var card = Node(name, safe);
            card.anchorMin = new Vector2(0f, 0f);
            card.anchorMax = new Vector2(1f, 0f);
            card.pivot = new Vector2(0.5f, 0f);
            card.sizeDelta = new Vector2(-32 * Pt, 0f);
            card.anchoredPosition = new Vector2(0f, 16 * Pt);
            var bg = Rounded(card, Panel, 18 * Pt);
            bg.raycastTarget = true; // taps on the card never reach the AR view
            var layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset((int)(20 * Pt), (int)(20 * Pt), (int)(20 * Pt), (int)(20 * Pt));
            layout.spacing = 12 * Pt;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            card.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return card;
        }

        internal static TextMeshProUGUI Label(Transform parent, string name, string text, float sizePt, Color color, FontStyles style)
        {
            var rt = Node(name, parent);
            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = sizePt * Pt;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.lineSpacing = 0f;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.raycastTarget = false;
            if (TMP_Settings.defaultFontAsset != null)
                tmp.font = TMP_Settings.defaultFontAsset;
            return tmp;
        }

        internal static Button Pill(Transform parent, string name, string text, Color background, Color textColor, float heightPt)
        {
            var rt = Node(name, parent);
            var image = Rounded(rt, background, heightPt * Pt * 0.5f);
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.selectedColor = Color.white;
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.45f);
            button.colors = colors;

            var element = rt.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = heightPt * Pt;
            element.minHeight = heightPt * Pt;
            element.flexibleWidth = 1f;

            var label = Label(rt, "Label", text, 20, textColor, FontStyles.Bold);
            label.alignment = TextAlignmentOptions.Center;
            Stretch((RectTransform)label.transform);
            return button;
        }
    }
}
