using FireReady.Core;
using FireReady.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static FireReady.EditorTools.BuilderUtils;
using static FireReady.EditorTools.SetupHudBuilder;

namespace FireReady.EditorTools
{
    /// <summary>
    /// Builds the light "menu" screens on their own canvas: Debrief (screen 11) and
    /// History (screen 12). Coordinates follow the 390 × 844 design frame.
    /// </summary>
    internal static class MenusBuilder
    {
        const string CanvasName = "Menus";

        static readonly Color Surface = Hex(0xFFFDFE);
        static readonly Color Soft = Hex(0xFEF7F7);
        static readonly Color Card = Hex(0xFAF4F4);
        static readonly Color Border = Hex(0xEAE8E7);
        static readonly Color Ink = Hex(0x3C393A);
        static readonly Color Ink2 = Hex(0x5B595A);
        static readonly Color Track = Hex(0xE6E0D9);
        static readonly Color Link = Hex(0x0062F2);
        static readonly Color Blue = Hex(0x0C70FB);
        static readonly Color Green = Hex(0x12803C);
        static readonly Color Lip = Hex(0xD4D1D1);

        public struct Result
        {
            public DebriefView Debrief;
            public HistoryView History;
            public RectTransform Canvas;
        }

        static readonly Color Red = Hex(0xD7261E);
        static readonly Color Yellow = Hex(0xF5C518);
        static readonly Color LoadingBlue = Hex(0x0062F2);

        public static Result Build(Sprite rounded, ScenarioCatalog catalog)
        {
            s_Rounded = rounded;
            var old = GameObject.Find(CanvasName);
            if (old != null) Object.DestroyImmediate(old);

            var canvasGo = new GameObject(CanvasName, typeof(RectTransform)) { layer = 5 };
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1170, 2532);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            BuildBriefing(canvasGo.transform);
            var select = BuildScenarioSelect(canvasGo.transform, catalog);
            BuildStart(canvasGo.transform, select);
            select.transform.SetAsLastSibling();
            BuildLoading(canvasGo.transform);
            var debrief = BuildDebrief(canvasGo.transform);
            var history = BuildHistory(canvasGo.transform);
            return new Result { Debrief = debrief, History = history, Canvas = (RectTransform)canvasGo.transform };
        }

        // ---- Start -------------------------------------------------------------------------

        static void BuildStart(Transform canvas, ScenarioSelectView select)
        {
            var root = FullScreen(canvas, "Start");
            var header = Node("Header", root);
            Top(header, 0, 300, stretch: true);
            var headerImage = header.gameObject.AddComponent<Image>();
            headerImage.color = Red;
            headerImage.raycastTarget = false;
            var curve = Node("Curve", root);
            curve.anchorMin = curve.anchorMax = new Vector2(0.5f, 1f);
            curve.pivot = new Vector2(0.5f, 1f);
            curve.sizeDelta = new Vector2(630 * Pt, 240 * Pt);
            curve.anchoredPosition = new Vector2(0, -236 * Pt);
            Rounded(curve, Surface, 120 * Pt).raycastTarget = false;

            var icon = Node("Icon", root);
            icon.anchorMin = icon.anchorMax = new Vector2(0.5f, 1f);
            icon.pivot = new Vector2(0.5f, 1f);
            icon.sizeDelta = new Vector2(96 * Pt, 96 * Pt);
            icon.anchoredPosition = new Vector2(0, -92 * Pt);
            Rounded(icon, Surface, 28 * Pt).raycastTarget = false;
            icon.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(0, -3 * Pt);
            var flame = Node("Flame", icon);
            flame.anchorMin = flame.anchorMax = new Vector2(0.5f, 0.5f);
            flame.sizeDelta = new Vector2(44 * Pt, 56 * Pt);
            flame.anchoredPosition = new Vector2(0, 2 * Pt);
            Rounded(flame, Red, 22 * Pt).raycastTarget = false;
            var core = Node("Core", flame);
            core.anchorMin = core.anchorMax = new Vector2(0.5f, 0f);
            core.pivot = new Vector2(0.5f, 0f);
            core.sizeDelta = new Vector2(22 * Pt, 30 * Pt);
            core.anchoredPosition = new Vector2(0, 6 * Pt);
            Rounded(core, Yellow, 11 * Pt).raycastTarget = false;

            var title = Label(root, "Title", "FireReady AR", 32, Ink, FontStyles.Bold);
            Top(title.rectTransform, 262, 36, stretch: true, margin: 24);
            title.alignment = TextAlignmentOptions.Center;
            var lead = Label(root, "Lead", "Practise putting out small fires with the right extinguisher, safely, in your own room.", 18, Ink2, FontStyles.Normal);
            Top(lead.rectTransform, 306, 56, stretch: true, margin: 24);
            lead.alignment = TextAlignmentOptions.Top;

            string[] steps =
            {
                "Choose a fire scenario and a mode.",
                "Scan the floor and place the scene in front of you.",
                "Pick the right extinguisher and use PASS: Pull, Aim, Squeeze, Sweep."
            };
            for (int i = 0; i < steps.Length; i++)
            {
                float y = 392 + i * 46;
                var dot = Node($"Step {i + 1}", root);
                dot.anchorMin = dot.anchorMax = dot.pivot = new Vector2(0, 1);
                dot.sizeDelta = new Vector2(28 * Pt, 28 * Pt);
                dot.anchoredPosition = new Vector2(24 * Pt, -y * Pt);
                Rounded(dot, Blue, 14 * Pt).raycastTarget = false;
                var n = Label(dot, "N", (i + 1).ToString(), 14, Color.white, FontStyles.Bold);
                Stretch(n.rectTransform);
                n.alignment = TextAlignmentOptions.Center;
                var text = Label(root, $"Step {i + 1} Text", steps[i], 16, Ink, FontStyles.Normal);
                text.rectTransform.anchorMin = new Vector2(0, 1);
                text.rectTransform.anchorMax = new Vector2(1, 1);
                text.rectTransform.pivot = new Vector2(0, 1);
                text.rectTransform.offsetMin = new Vector2(66 * Pt, -(y + 46) * Pt);
                text.rectTransform.offsetMax = new Vector2(-24 * Pt, -(y + 3) * Pt);
                text.alignment = TextAlignmentOptions.TopLeft;
            }

            var field = NameField(root);

            var start = Node("Start Training", root);
            Bottom(start, 136, 48, margin: 24);
            var startButton = WrapPill(start, "Start training", Blue, Color.white);
            var history = Node("Session History", root);
            Bottom(history, 72, 48, margin: 24);
            var historyButton = WrapPill(history, "Session history", Soft, Ink);
            var note = Label(root, "Disclaimer", "A training aid only. In a real fire, raise the alarm and leave if you are unsure.", 13, Ink2, FontStyles.Normal);
            Bottom(note.rectTransform, 22, 40, margin: 24);
            note.alignment = TextAlignmentOptions.Center;

            var view = canvas.gameObject.AddComponent<StartView>();
            SetRef(view, "root", root.gameObject);
            SetRef(view, "traineeName", field);
            SetRef(view, "startButton", startButton);
            SetRef(view, "historyButton", historyButton);
            SetRef(view, "scenarioSelect", select);
        }

        static TMP_InputField NameField(RectTransform root)
        {
            var outer = Node("Trainee Name", root);
            Top(outer, 546, 54, stretch: true, margin: 24);
            Rounded(outer, Border, 8 * Pt).raycastTarget = false;
            var inner = Node("Field", outer);
            Stretch(inner);
            inner.offsetMin = new Vector2(1.5f * Pt, 1.5f * Pt);
            inner.offsetMax = new Vector2(-1.5f * Pt, -1.5f * Pt);
            var innerImage = Rounded(inner, Surface, 7 * Pt);

            var caption = Label(inner, "Caption", "Trainee name", 11, Ink2, FontStyles.Normal);
            caption.rectTransform.anchorMin = new Vector2(0, 1);
            caption.rectTransform.anchorMax = new Vector2(1, 1);
            caption.rectTransform.pivot = new Vector2(0, 1);
            caption.rectTransform.offsetMin = new Vector2(16 * Pt, -22 * Pt);
            caption.rectTransform.offsetMax = new Vector2(-16 * Pt, -7 * Pt);

            var area = Node("Text Area", inner);
            area.anchorMin = Vector2.zero;
            area.anchorMax = Vector2.one;
            area.offsetMin = new Vector2(16 * Pt, 4 * Pt);
            area.offsetMax = new Vector2(-16 * Pt, -20 * Pt);
            area.gameObject.AddComponent<RectMask2D>();
            var placeholder = Label(area, "Placeholder", "Your name", 18, Hex(0xA8A4A5), FontStyles.Normal);
            Stretch(placeholder.rectTransform);
            placeholder.alignment = TextAlignmentOptions.MidlineLeft;
            placeholder.textWrappingMode = TextWrappingModes.NoWrap;
            var text = Label(area, "Text", "", 18, Color.black, FontStyles.Normal);
            Stretch(text.rectTransform);
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.textWrappingMode = TextWrappingModes.NoWrap;

            var input = inner.gameObject.AddComponent<TMP_InputField>();
            input.targetGraphic = innerImage;
            input.textViewport = area;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.characterLimit = 30;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.pointSize = 18 * Pt;
            if (TMP_Settings.defaultFontAsset != null) input.fontAsset = TMP_Settings.defaultFontAsset;
            return input;
        }

        // ---- Scenario select ------------------------------------------------------------

        static ScenarioSelectView BuildScenarioSelect(Transform canvas, ScenarioCatalog catalog)
        {
            var root = FullScreen(canvas, "Scenario Select");
            var back = BackButton(root);
            var title = Label(root, "Title", "Choose a scenario", 26, Ink, FontStyles.Bold);
            Top(title.rectTransform, 102, 30, stretch: true, margin: 24);

            var options = new ScenarioSelectView.Option[3];
            for (int i = 0; i < 3; i++)
                options[i] = ScenarioOption(root, i, 150 + i * 88);

            var modeTitle = Label(root, "Mode Title", "Mode", 18, Ink, FontStyles.Bold);
            Top(modeTitle.rectTransform, 424, 24, stretch: true, margin: 24);
            var seg = Node("Mode", root);
            Top(seg, 458, 48, stretch: true, margin: 24);
            Rounded(seg, Card, 24 * Pt).raycastTarget = false;
            var guided = Segment(seg, "Guided", 0f, out var guidedBg, out var guidedLabel);
            var assessment = Segment(seg, "Assessment", 0.5f, out var assessBg, out var assessLabel);
            var help = Label(root, "Mode Help", "Guided shows each step as you go. Assessment hides the prompts and scores you.", 14, Ink2, FontStyles.Normal);
            Top(help.rectTransform, 514, 42, stretch: true, margin: 24);

            var warn = Node("Safety Note", root);
            Bottom(warn, 128, 72, margin: 24);
            Rounded(warn, Hex(0xF5C518), 12 * Pt).raycastTarget = false;
            var warnInner = Node("Inner", warn);
            Stretch(warnInner);
            warnInner.offsetMin = new Vector2(1.5f * Pt, 1.5f * Pt);
            warnInner.offsetMax = new Vector2(-1.5f * Pt, -1.5f * Pt);
            Rounded(warnInner, Hex(0xFFF7D6), 10.5f * Pt).raycastTarget = false;
            var warnText = Label(warnInner, "Text", "<b>Before you start:</b> clear a 2 × 2 m space and keep an eye on your surroundings while you move.", 15, Ink, FontStyles.Normal);
            Stretch(warnText.rectTransform);
            warnText.margin = new Vector4(16 * Pt, 10 * Pt, 16 * Pt, 10 * Pt);
            warnText.alignment = TextAlignmentOptions.MidlineLeft;

            var cont = Node("Continue", root);
            Bottom(cont, 56, 48, margin: 24);
            var continueButton = WrapPill(cont, "Continue", Blue, Color.white);

            var view = root.gameObject.AddComponent<ScenarioSelectView>();
            SetRef(view, "root", root.gameObject);
            SetRef(view, "catalog", catalog);
            SetRef(view, "guidedButton", guided);
            SetRef(view, "assessmentButton", assessment);
            SetRef(view, "guidedBackground", guidedBg);
            SetRef(view, "assessmentBackground", assessBg);
            SetRef(view, "guidedLabel", guidedLabel);
            SetRef(view, "assessmentLabel", assessLabel);
            SetRef(view, "backButton", back);
            SetRef(view, "continueButton", continueButton);
            var so = new SerializedObject(view);
            var prop = so.FindProperty("options");
            prop.arraySize = options.Length;
            for (int i = 0; i < options.Length; i++)
            {
                var el = prop.GetArrayElementAtIndex(i);
                el.FindPropertyRelative("button").objectReferenceValue = options[i].button;
                el.FindPropertyRelative("background").objectReferenceValue = options[i].background;
                el.FindPropertyRelative("radio").objectReferenceValue = options[i].radio;
                el.FindPropertyRelative("radioDot").objectReferenceValue = options[i].radioDot;
                el.FindPropertyRelative("title").objectReferenceValue = options[i].title;
                el.FindPropertyRelative("tagline").objectReferenceValue = options[i].tagline;
                el.FindPropertyRelative("badge").objectReferenceValue = options[i].badge;
                el.FindPropertyRelative("badgeLabel").objectReferenceValue = options[i].badgeLabel;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        static ScenarioSelectView.Option ScenarioOption(RectTransform root, int index, float top)
        {
            var outer = Node($"Scenario {index + 1}", root);
            Top(outer, top, 76, stretch: true, margin: 24);
            Rounded(outer, Border, 38 * Pt);
            var inner = Node("Inner", outer);
            Stretch(inner);
            inner.offsetMin = new Vector2(1.5f * Pt, 1.5f * Pt);
            inner.offsetMax = new Vector2(-1.5f * Pt, -1.5f * Pt);
            var bg = Rounded(inner, Surface, 36.5f * Pt);
            bg.raycastTarget = false;
            var button = outer.gameObject.AddComponent<Button>();
            button.targetGraphic = outer.GetComponent<Image>();

            var badge = Node("Badge", inner);
            badge.anchorMin = badge.anchorMax = badge.pivot = new Vector2(0, 0.5f);
            badge.anchoredPosition = new Vector2(14 * Pt, 0);
            badge.sizeDelta = new Vector2(48 * Pt, 48 * Pt);
            var badgeImage = Rounded(badge, Ink, 24 * Pt);
            badgeImage.raycastTarget = false;
            var badgeLabel = Label(badge, "Label", "A", 22, Color.white, FontStyles.Bold);
            Stretch(badgeLabel.rectTransform);
            badgeLabel.alignment = TextAlignmentOptions.Center;

            var title = Label(inner, "Title", "Office waste bin", 18, Ink, FontStyles.Bold);
            title.rectTransform.anchorMin = new Vector2(0, 0.5f);
            title.rectTransform.anchorMax = new Vector2(1, 1);
            title.rectTransform.offsetMin = new Vector2(76 * Pt, 0);
            title.rectTransform.offsetMax = new Vector2(-52 * Pt, -10 * Pt);
            title.alignment = TextAlignmentOptions.BottomLeft;
            title.textWrappingMode = TextWrappingModes.NoWrap;
            var tagline = Label(inner, "Tagline", "Burning paper.", 14, Ink2, FontStyles.Normal);
            tagline.rectTransform.anchorMin = new Vector2(0, 0);
            tagline.rectTransform.anchorMax = new Vector2(1, 0.5f);
            tagline.rectTransform.offsetMin = new Vector2(76 * Pt, 6 * Pt);
            tagline.rectTransform.offsetMax = new Vector2(-52 * Pt, -2 * Pt);
            tagline.alignment = TextAlignmentOptions.TopLeft;

            var radio = Node("Radio", inner);
            radio.anchorMin = radio.anchorMax = radio.pivot = new Vector2(1, 0.5f);
            radio.anchoredPosition = new Vector2(-16 * Pt, 0);
            radio.sizeDelta = new Vector2(24 * Pt, 24 * Pt);
            var radioImage = Rounded(radio, Track, 12 * Pt);
            radioImage.raycastTarget = false;
            var dot = Node("Dot", radio);
            dot.anchorMin = dot.anchorMax = new Vector2(0.5f, 0.5f);
            dot.sizeDelta = new Vector2(10 * Pt, 10 * Pt);
            Rounded(dot, Color.white, 5 * Pt).raycastTarget = false;

            return new ScenarioSelectView.Option
            {
                button = button, background = bg, radio = radioImage, radioDot = dot.gameObject,
                title = title, tagline = tagline, badge = badgeImage, badgeLabel = badgeLabel
            };
        }

        static Button Segment(RectTransform seg, string text, float x, out Image background, out TextMeshProUGUI label)
        {
            var rt = Node(text, seg);
            rt.anchorMin = new Vector2(x, 0);
            rt.anchorMax = new Vector2(x + 0.5f, 1);
            rt.offsetMin = new Vector2(4 * Pt, 4 * Pt);
            rt.offsetMax = new Vector2(-4 * Pt, -4 * Pt);
            background = Rounded(rt, Ink, 20 * Pt);
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            label = Label(rt, "Label", text, 16, Color.white, FontStyles.Bold);
            Stretch(label.rectTransform);
            label.alignment = TextAlignmentOptions.Center;
            return button;
        }

        // ---- Loading ---------------------------------------------------------------------

        static void BuildLoading(Transform canvas)
        {
            var root = FullScreen(canvas, "Loading");
            root.GetComponent<Image>().color = LoadingBlue;

            var badge = Node("Badge", root);
            badge.anchorMin = badge.anchorMax = new Vector2(0.5f, 1f);
            badge.pivot = new Vector2(0.5f, 1f);
            badge.sizeDelta = new Vector2(56 * Pt, 56 * Pt);
            badge.anchoredPosition = new Vector2(0, -210 * Pt);
            Rounded(badge, Surface, 28 * Pt).raycastTarget = false;
            var badgeLabel = Label(badge, "Label", "A", 24, Ink, FontStyles.Bold);
            Stretch(badgeLabel.rectTransform);
            badgeLabel.alignment = TextAlignmentOptions.Center;

            var subtitle = Label(root, "Subtitle", "Scenario 1 · Guided", 15, Hex(0xDCE6F7), FontStyles.Normal);
            Top(subtitle.rectTransform, 282, 20, stretch: true, margin: 24);
            subtitle.alignment = TextAlignmentOptions.Center;
            var title = Label(root, "Title", "Preparing the office bin fire…", 28, Color.white, FontStyles.Bold);
            Top(title.rectTransform, 310, 70, stretch: true, margin: 24);
            title.alignment = TextAlignmentOptions.Top;

            var bar = Node("Bar", root);
            Top(bar, 400, 8, stretch: true, margin: 24);
            Rounded(bar, new Color(1, 1, 1, 0.28f), 4 * Pt).raycastTarget = false;
            var fill = Node("Fill", bar);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(0f, 1f);
            fill.offsetMin = fill.offsetMax = Vector2.zero;
            Rounded(fill, Color.white, 4 * Pt).raycastTarget = false;
            var caption = Label(root, "Caption", "Loading scenario", 15, Hex(0xDCE6F7), FontStyles.Normal);
            Top(caption.rectTransform, 418, 20, stretch: true, margin: 24);
            var percent = Label(root, "Percent", "0%", 15, Color.white, FontStyles.Bold);
            Top(percent.rectTransform, 418, 20, stretch: true, margin: 24);
            percent.alignment = TextAlignmentOptions.TopRight;

            var items = new TextMeshProUGUI[3];
            var ticks = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                float y = 460 + i * 44;
                var tick = Node($"Tick {i + 1}", root);
                tick.anchorMin = tick.anchorMax = tick.pivot = new Vector2(0, 1);
                tick.sizeDelta = new Vector2(14 * Pt, 14 * Pt);
                tick.anchoredPosition = new Vector2(28 * Pt, -(y + 13) * Pt);
                ticks[i] = Rounded(tick, new Color(1, 1, 1, 0.3f), 7 * Pt);
                ticks[i].raycastTarget = false;
                items[i] = Label(root, $"Item {i + 1}", "", 16, Hex(0xDCE6F7), FontStyles.Normal);
                items[i].rectTransform.anchorMin = new Vector2(0, 1);
                items[i].rectTransform.anchorMax = new Vector2(1, 1);
                items[i].rectTransform.pivot = new Vector2(0, 1);
                items[i].rectTransform.offsetMin = new Vector2(56 * Pt, -(y + 40) * Pt);
                items[i].rectTransform.offsetMax = new Vector2(-24 * Pt, -y * Pt);
                items[i].alignment = TextAlignmentOptions.MidlineLeft;
            }

            var curve = Node("Curve", root);
            curve.anchorMin = curve.anchorMax = new Vector2(0.5f, 0f);
            curve.pivot = new Vector2(0.5f, 0f);
            curve.sizeDelta = new Vector2(630 * Pt, 300 * Pt);
            curve.anchoredPosition = new Vector2(0, -156 * Pt);
            Rounded(curve, Surface, 150 * Pt).raycastTarget = false;
            var cancel = Node("Cancel", root);
            Bottom(cancel, 50, 48, margin: 24);
            var cancelButton = WrapPill(cancel, "Cancel", Soft, Ink);

            var view = canvas.gameObject.AddComponent<LoadingView>();
            SetRef(view, "root", root.gameObject);
            SetRef(view, "badgeLabel", badgeLabel);
            SetRef(view, "subtitle", subtitle);
            SetRef(view, "title", title);
            SetRef(view, "barFill", fill);
            SetRef(view, "percent", percent);
            SetRef(view, "cancelButton", cancelButton);
            var so = new SerializedObject(view);
            var ip = so.FindProperty("items");
            var tp = so.FindProperty("itemTicks");
            ip.arraySize = 3;
            tp.arraySize = 3;
            for (int i = 0; i < 3; i++)
            {
                ip.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
                tp.GetArrayElementAtIndex(i).objectReferenceValue = ticks[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---- Briefing (dark, over the camera) --------------------------------------------

        static void BuildBriefing(Transform canvas)
        {
            var root = Node("Briefing", canvas);
            Stretch(root);

            var card = Node("Card", root);
            Top(card, 55, 0, stretch: true, margin: 16);
            Rounded(card, Hex(0x0E1116, 0.9f), 18 * Pt).raycastTarget = true;
            var vl = card.gameObject.AddComponent<VerticalLayoutGroup>();
            vl.padding = new RectOffset((int)(20 * Pt), (int)(20 * Pt), (int)(20 * Pt), (int)(20 * Pt));
            vl.spacing = 12 * Pt;
            vl.childControlWidth = vl.childControlHeight = true;
            vl.childForceExpandWidth = true;
            vl.childForceExpandHeight = false;
            card.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var tagRow = Node("Tag Row", card);
            tagRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 26 * Pt;
            var tag = Node("Class Tag", tagRow);
            tag.anchorMin = tag.anchorMax = tag.pivot = new Vector2(0, 0.5f);
            tag.sizeDelta = new Vector2(96 * Pt, 26 * Pt);
            Rounded(tag, Hex(0xF3F4F6), 6 * Pt).raycastTarget = false;
            var tagLabel = Label(tag, "Label", "CLASS A", 13, Hex(0x0E1116), FontStyles.Bold);
            Stretch(tagLabel.rectTransform);
            tagLabel.alignment = TextAlignmentOptions.Center;
            var sub = Label(tagRow, "Subtitle", "SCENARIO 1 · GUIDED", 14, Hex(0xAEB4BE), FontStyles.Bold);
            Stretch(sub.rectTransform);
            sub.rectTransform.offsetMin = new Vector2(106 * Pt, 0);
            sub.alignment = TextAlignmentOptions.MidlineLeft;
            sub.characterSpacing = 4f;

            var title = Label(card, "Title", "Office waste bin", 28, Hex(0xF3F4F6), FontStyles.Bold);
            var desc = Label(card, "Description", "", 16, Hex(0xC9CED6), FontStyles.Normal);
            var divider = Node("Divider", card);
            divider.gameObject.AddComponent<LayoutElement>().preferredHeight = 1 * Pt;
            divider.gameObject.AddComponent<Image>().color = new Color(1, 1, 1, 0.1f);
            Label(card, "Tasks Title", "Your tasks", 14, Hex(0xAEB4BE), FontStyles.Bold);

            string[] tasks =
            {
                "Raise the alarm", "Pick a suitable extinguisher", "Pull the pin",
                "Stand 1.2 to 3 m away, aim at the base", "Squeeze and sweep side to side",
                "Put it out, or evacuate if it gets too big"
            };
            TextMeshProUGUI first = null;
            for (int i = 0; i < tasks.Length; i++)
            {
                var row = Node($"Task {i + 1}", card);
                row.gameObject.AddComponent<LayoutElement>().preferredHeight = 24 * Pt;
                var n = Node("N", row);
                n.anchorMin = n.anchorMax = n.pivot = new Vector2(0, 0.5f);
                n.sizeDelta = new Vector2(24 * Pt, 24 * Pt);
                Rounded(n, Blue, 12 * Pt).raycastTarget = false;
                var nl = Label(n, "Label", (i + 1).ToString(), 13, Color.white, FontStyles.Bold);
                Stretch(nl.rectTransform);
                nl.alignment = TextAlignmentOptions.Center;
                var t = Label(row, "Text", tasks[i], 16, Hex(0xF3F4F6), FontStyles.Normal);
                Stretch(t.rectTransform);
                t.rectTransform.offsetMin = new Vector2(36 * Pt, 0);
                t.alignment = TextAlignmentOptions.MidlineLeft;
                t.textWrappingMode = TextWrappingModes.NoWrap;
                t.overflowMode = TextOverflowModes.Ellipsis;
                if (i == 0) first = t;
            }

            var start = Node("Start Scenario", root);
            Bottom(start, 48, 56, margin: 16);
            var startButton = Pill(start, "Button", "Start scenario", Blue, Color.white, 56);
            Stretch((RectTransform)startButton.transform);

            var view = canvas.gameObject.AddComponent<BriefingView>();
            SetRef(view, "root", root.gameObject);
            SetRef(view, "classTag", tagLabel);
            SetRef(view, "subtitle", sub);
            SetRef(view, "title", title);
            SetRef(view, "description", desc);
            SetRef(view, "firstTask", first);
            SetRef(view, "startButton", startButton);
        }

        static Button BackButton(RectTransform root)
        {
            var back = Node("Back", root);
            back.anchorMin = back.anchorMax = back.pivot = new Vector2(0, 1);
            back.anchoredPosition = new Vector2(12 * Pt, -52 * Pt);
            back.sizeDelta = new Vector2(80 * Pt, 44 * Pt);
            var backImage = back.gameObject.AddComponent<Image>();
            backImage.color = new Color(1, 1, 1, 0f);
            var button = back.gameObject.AddComponent<Button>();
            button.targetGraphic = backImage;
            var label = Label(back, "Label", "< Back", 16, Link, FontStyles.Bold);
            Stretch(label.rectTransform);
            label.alignment = TextAlignmentOptions.MidlineLeft;
            return button;
        }

        /// <summary>A pill button that fills an already-positioned slot.</summary>
        static Button WrapPill(RectTransform slot, string text, Color bg, Color fg)
        {
            var button = LightPill(slot, "Button", text, bg, fg);
            Stretch((RectTransform)button.transform);
            return button;
        }

        // ---- Debrief ----------------------------------------------------------------------

        static DebriefView BuildDebrief(Transform canvas)
        {
            var root = FullScreen(canvas, "Debrief");

            var header = Node("Header", root);
            Top(header, 0, 230, stretch: true);
            var headerImage = header.gameObject.AddComponent<Image>();
            headerImage.color = Green;
            headerImage.raycastTarget = false;
            var curve = Node("Curve", root);
            curve.anchorMin = curve.anchorMax = new Vector2(0.5f, 1f);
            curve.pivot = new Vector2(0.5f, 1f);
            curve.sizeDelta = new Vector2(630 * Pt, 200 * Pt);
            curve.anchoredPosition = new Vector2(0, -186 * Pt);
            Rounded(curve, Surface, 100 * Pt).raycastTarget = false;

            var title = Label(root, "Title", "Fire extinguished", 28, Color.white, FontStyles.Bold);
            Top(title.rectTransform, 92, 34, stretch: true, margin: 24);
            title.alignment = TextAlignmentOptions.Center;
            var meta = Label(root, "Meta", "Office waste bin · Guided · 2:41", 15, Hex(0xDDF5E6), FontStyles.Normal);
            Top(meta.rectTransform, 130, 20, stretch: true, margin: 24);
            meta.alignment = TextAlignmentOptions.Center;

            // Score row
            var scoreRow = Node("Score Row", root);
            Top(scoreRow, 214, 56, stretch: true, margin: 24);
            var hl = scoreRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.childAlignment = TextAnchor.MiddleCenter;
            hl.spacing = 8 * Pt;
            hl.childControlWidth = hl.childControlHeight = true;
            hl.childForceExpandWidth = hl.childForceExpandHeight = false;
            var score = Label(scoreRow, "Score", "86", 56, Ink, FontStyles.Bold);
            score.textWrappingMode = TextWrappingModes.NoWrap;
            score.alignment = TextAlignmentOptions.Bottom;
            var max = Label(scoreRow, "Max", "/ 100", 20, Ink2, FontStyles.Bold);
            max.textWrappingMode = TextWrappingModes.NoWrap;
            var pill = Node("New Best", scoreRow);
            var pillLe = pill.gameObject.AddComponent<LayoutElement>();
            pillLe.preferredWidth = 104 * Pt;
            pillLe.preferredHeight = 28 * Pt;
            Rounded(pill, Hex(0xE3EEFF), 14 * Pt).raycastTarget = false;
            var pillText = Label(pill, "Label", "New best", 14, Link, FontStyles.Bold);
            Stretch(pillText.rectTransform);
            pillText.alignment = TextAlignmentOptions.Center;

            // Criteria card
            var card = Node("Criteria", root);
            Top(card, 284, 0, stretch: true, margin: 24);
            Rounded(card, Card, 12 * Pt).raycastTarget = false;
            var vl = card.gameObject.AddComponent<VerticalLayoutGroup>();
            vl.padding = new RectOffset((int)(16 * Pt), (int)(16 * Pt), (int)(2 * Pt), (int)(2 * Pt));
            vl.childControlWidth = vl.childControlHeight = true;
            vl.childForceExpandWidth = true;
            vl.childForceExpandHeight = false;
            card.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var rows = new DebriefView.RuleRow[6];
            for (int i = 0; i < rows.Length; i++)
                rows[i] = RuleRow(card, i, i < rows.Length - 1);

            // Buttons
            var buttons = Node("Buttons", root);
            Bottom(buttons, 72, 48, margin: 24);
            var bl = buttons.gameObject.AddComponent<HorizontalLayoutGroup>();
            bl.spacing = 12 * Pt;
            bl.childControlWidth = bl.childControlHeight = true;
            bl.childForceExpandWidth = bl.childForceExpandHeight = true;
            var mainMenu = LightPill(buttons, "Main Menu", "Main menu", Soft, Ink);
            var restart = LightPill(buttons, "Restart", "Restart", Blue, Color.white);

            var historyLink = Node("History Link", root);
            Bottom(historyLink, 24, 40, margin: 24);
            var linkImage = historyLink.gameObject.AddComponent<Image>();
            linkImage.color = new Color(1, 1, 1, 0f);
            var historyButton = historyLink.gameObject.AddComponent<Button>();
            historyButton.targetGraphic = linkImage;
            var linkText = Label(historyLink, "Label", "View session history", 16, Link, FontStyles.Bold);
            Stretch(linkText.rectTransform);
            linkText.alignment = TextAlignmentOptions.Center;

            var view = canvas.gameObject.AddComponent<DebriefView>();
            SetRef(view, "root", root.gameObject);
            SetRef(view, "header", headerImage);
            SetRef(view, "title", title);
            SetRef(view, "meta", meta);
            SetRef(view, "score", score);
            SetRef(view, "maxScore", max);
            SetRef(view, "newBestPill", pill.gameObject);
            SetRef(view, "mainMenuButton", mainMenu);
            SetRef(view, "restartButton", restart);
            SetRef(view, "historyButton", historyButton);

            var so = new SerializedObject(view);
            var prop = so.FindProperty("rows");
            prop.arraySize = rows.Length;
            for (int i = 0; i < rows.Length; i++)
            {
                var el = prop.GetArrayElementAtIndex(i);
                el.FindPropertyRelative("root").objectReferenceValue = rows[i].root;
                el.FindPropertyRelative("name").objectReferenceValue = rows[i].name;
                el.FindPropertyRelative("points").objectReferenceValue = rows[i].points;
                el.FindPropertyRelative("fill").objectReferenceValue = rows[i].fill;
                el.FindPropertyRelative("fillImage").objectReferenceValue = rows[i].fillImage;
                el.FindPropertyRelative("feedback").objectReferenceValue = rows[i].feedback;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        static DebriefView.RuleRow RuleRow(RectTransform card, int index, bool divider)
        {
            var row = Node($"Rule {index + 1}", card);
            var vl = row.gameObject.AddComponent<VerticalLayoutGroup>();
            vl.padding = new RectOffset(0, 0, (int)(9 * Pt), (int)(9 * Pt));
            vl.spacing = 6 * Pt;
            vl.childControlWidth = vl.childControlHeight = true;
            vl.childForceExpandWidth = true;
            vl.childForceExpandHeight = false;

            var head = Node("Header", row);
            head.gameObject.AddComponent<LayoutElement>().preferredHeight = 20 * Pt;
            var name = Label(head, "Name", "Extinguisher choice", 16, Ink, FontStyles.Bold);
            Stretch(name.rectTransform);
            name.alignment = TextAlignmentOptions.MidlineLeft;
            var points = Label(head, "Points", "30 / 30", 16, Ink, FontStyles.Bold);
            Stretch(points.rectTransform);
            points.alignment = TextAlignmentOptions.MidlineRight;

            var bar = Node("Bar", row);
            bar.gameObject.AddComponent<LayoutElement>().preferredHeight = 6 * Pt;
            Rounded(bar, Track, 3 * Pt).raycastTarget = false;
            var fill = Node("Fill", bar);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = Vector2.one;
            fill.offsetMin = fill.offsetMax = Vector2.zero;
            var fillImage = Rounded(fill, Green, 3 * Pt);
            fillImage.raycastTarget = false;

            var feedback = Label(row, "Feedback", "", 14, Ink2, FontStyles.Normal);
            feedback.gameObject.SetActive(false);

            if (divider)
            {
                var line = Node("Divider", card);
                line.gameObject.AddComponent<LayoutElement>().preferredHeight = 1 * Pt;
                line.gameObject.AddComponent<Image>().color = Border;
            }

            return new DebriefView.RuleRow
            {
                root = row.gameObject, name = name, points = points, fill = fill, fillImage = fillImage, feedback = feedback
            };
        }

        // ---- History ---------------------------------------------------------------------

        static HistoryView BuildHistory(Transform canvas)
        {
            var root = FullScreen(canvas, "History");

            var back = Node("Back", root);
            back.anchorMin = back.anchorMax = back.pivot = new Vector2(0, 1);
            back.anchoredPosition = new Vector2(12 * Pt, -52 * Pt);
            back.sizeDelta = new Vector2(80 * Pt, 44 * Pt);
            var backImage = back.gameObject.AddComponent<Image>();
            backImage.color = new Color(1, 1, 1, 0f);
            var backButton = back.gameObject.AddComponent<Button>();
            backButton.targetGraphic = backImage;
            var backLabel = Label(back, "Label", "< Back", 16, Link, FontStyles.Bold);
            Stretch(backLabel.rectTransform);
            backLabel.alignment = TextAlignmentOptions.MidlineLeft;

            var title = Label(root, "Title", "Session history", 26, Ink, FontStyles.Bold);
            Top(title.rectTransform, 102, 30, stretch: true, margin: 24);
            var subtitle = Label(root, "Subtitle", "Saved on this phone · newest first", 15, Ink2, FontStyles.Normal);
            Top(subtitle.rectTransform, 136, 20, stretch: true, margin: 24);

            var stats = Node("Stats", root);
            Top(stats, 168, 64, stretch: true, margin: 24);
            var sl = stats.gameObject.AddComponent<HorizontalLayoutGroup>();
            sl.spacing = 8 * Pt;
            sl.childControlWidth = sl.childControlHeight = true;
            sl.childForceExpandWidth = sl.childForceExpandHeight = true;
            var count = Stat(stats, "Sessions");
            var best = Stat(stats, "Best score");
            var putOut = Stat(stats, "Put out");

            var list = Node("List", root);
            Top(list, 248, 0, stretch: true, margin: 24);
            var ll = list.gameObject.AddComponent<VerticalLayoutGroup>();
            ll.spacing = 8 * Pt;
            ll.childControlWidth = ll.childControlHeight = true;
            ll.childForceExpandWidth = true;
            ll.childForceExpandHeight = false;
            list.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var template = HistoryRowTemplate(list);

            var empty = Label(root, "Empty", "No sessions yet. Finish a scenario and it will appear here.", 16, Ink2, FontStyles.Normal);
            Top(empty.rectTransform, 260, 60, stretch: true, margin: 24);
            empty.alignment = TextAlignmentOptions.Center;

            var delete = Node("Delete All", root);
            Bottom(delete, 32, 44, margin: 24);
            var deleteImage = delete.gameObject.AddComponent<Image>();
            deleteImage.color = new Color(1, 1, 1, 0f);
            var deleteButton = delete.gameObject.AddComponent<Button>();
            deleteButton.targetGraphic = deleteImage;
            var deleteLabel = Label(delete, "Label", "Delete all sessions", 16, Hex(0xB3201A), FontStyles.Bold);
            Stretch(deleteLabel.rectTransform);
            deleteLabel.alignment = TextAlignmentOptions.Center;

            var view = canvas.gameObject.AddComponent<HistoryView>();
            SetRef(view, "root", root.gameObject);
            SetRef(view, "backButton", backButton);
            SetRef(view, "countValue", count);
            SetRef(view, "bestValue", best);
            SetRef(view, "putOutValue", putOut);
            SetRef(view, "listParent", list);
            SetRef(view, "rowTemplate", template);
            SetRef(view, "emptyLabel", empty);
            SetRef(view, "deleteAllButton", deleteButton);
            SetRef(view, "deleteAllLabel", deleteLabel);
            var so = new SerializedObject(view);
            so.FindProperty("maxRows").intValue = 6;
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        static TextMeshProUGUI Stat(RectTransform parent, string label)
        {
            var card = Node(label, parent);
            Rounded(card, Card, 12 * Pt).raycastTarget = false;
            var value = Label(card, "Value", "0", 24, Ink, FontStyles.Bold);
            value.rectTransform.anchorMin = new Vector2(0, 0.5f);
            value.rectTransform.anchorMax = new Vector2(1, 1);
            value.rectTransform.offsetMin = new Vector2(12 * Pt, -2 * Pt);
            value.rectTransform.offsetMax = new Vector2(-8 * Pt, -8 * Pt);
            value.alignment = TextAlignmentOptions.BottomLeft;
            value.textWrappingMode = TextWrappingModes.NoWrap;
            var text = Label(card, "Label", label, 13, Ink2, FontStyles.Normal);
            text.rectTransform.anchorMin = new Vector2(0, 0);
            text.rectTransform.anchorMax = new Vector2(1, 0.5f);
            text.rectTransform.offsetMin = new Vector2(12 * Pt, 8 * Pt);
            text.rectTransform.offsetMax = new Vector2(-8 * Pt, 0);
            text.alignment = TextAlignmentOptions.TopLeft;
            return value;
        }

        static HistoryRow HistoryRowTemplate(RectTransform list)
        {
            var row = Node("Row Template", list);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 64 * Pt;
            Rounded(row, Border, 32 * Pt).raycastTarget = false;
            var inner = Node("Inner", row);
            Stretch(inner);
            inner.offsetMin = new Vector2(1.5f * Pt, 1.5f * Pt);
            inner.offsetMax = new Vector2(-1.5f * Pt, -1.5f * Pt);
            Rounded(inner, Surface, 30.5f * Pt).raycastTarget = false;

            var badge = Node("Badge", inner);
            badge.anchorMin = badge.anchorMax = new Vector2(0, 0.5f);
            badge.pivot = new Vector2(0, 0.5f);
            badge.anchoredPosition = new Vector2(10 * Pt, 0);
            badge.sizeDelta = new Vector2(40 * Pt, 40 * Pt);
            var badgeImage = Rounded(badge, Ink, 20 * Pt);
            badgeImage.raycastTarget = false;
            var badgeLabel = Label(badge, "Label", "A", 18, Color.white, FontStyles.Bold);
            Stretch(badgeLabel.rectTransform);
            badgeLabel.alignment = TextAlignmentOptions.Center;

            var title = Label(inner, "Title", "Office waste bin", 16, Ink, FontStyles.Bold);
            title.rectTransform.anchorMin = new Vector2(0, 0.5f);
            title.rectTransform.anchorMax = new Vector2(1, 1);
            title.rectTransform.offsetMin = new Vector2(62 * Pt, 0);
            title.rectTransform.offsetMax = new Vector2(-110 * Pt, -10 * Pt);
            title.alignment = TextAlignmentOptions.BottomLeft;
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.overflowMode = TextOverflowModes.Ellipsis;
            var subtitle = Label(inner, "Subtitle", "Guided · Today, 14:32", 13, Ink2, FontStyles.Normal);
            subtitle.rectTransform.anchorMin = new Vector2(0, 0);
            subtitle.rectTransform.anchorMax = new Vector2(1, 0.5f);
            subtitle.rectTransform.offsetMin = new Vector2(62 * Pt, 10 * Pt);
            subtitle.rectTransform.offsetMax = new Vector2(-110 * Pt, -2 * Pt);
            subtitle.alignment = TextAlignmentOptions.TopLeft;
            subtitle.textWrappingMode = TextWrappingModes.NoWrap;

            var score = Label(inner, "Score", "86", 18, Ink, FontStyles.Bold);
            score.rectTransform.anchorMin = new Vector2(1, 0.5f);
            score.rectTransform.anchorMax = new Vector2(1, 1);
            score.rectTransform.pivot = new Vector2(1, 0.5f);
            score.rectTransform.offsetMin = new Vector2(-120 * Pt, 0);
            score.rectTransform.offsetMax = new Vector2(-16 * Pt, -10 * Pt);
            score.alignment = TextAlignmentOptions.BottomRight;
            var outcome = Label(inner, "Outcome", "Extinguished", 12, Green, FontStyles.Bold);
            outcome.rectTransform.anchorMin = new Vector2(1, 0);
            outcome.rectTransform.anchorMax = new Vector2(1, 0.5f);
            outcome.rectTransform.offsetMin = new Vector2(-120 * Pt, 10 * Pt);
            outcome.rectTransform.offsetMax = new Vector2(-16 * Pt, -2 * Pt);
            outcome.alignment = TextAlignmentOptions.TopRight;
            outcome.textWrappingMode = TextWrappingModes.NoWrap;

            var component = row.gameObject.AddComponent<HistoryRow>();
            component.badge = badgeImage;
            component.badgeLabel = badgeLabel;
            component.title = title;
            component.subtitle = subtitle;
            component.score = score;
            component.outcome = outcome;
            return component;
        }

        // ---- Helpers ---------------------------------------------------------------------

        static RectTransform FullScreen(Transform canvas, string name)
        {
            var root = Node(name, canvas);
            Stretch(root);
            var bg = root.gameObject.AddComponent<Image>();
            bg.color = Surface; // opaque: hides the camera and blocks taps
            return root;
        }

        static void Top(RectTransform rt, float top, float height, bool stretch, float margin = 0)
        {
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.sizeDelta = new Vector2(-2 * margin * Pt, height * Pt);
            rt.anchoredPosition = new Vector2(0, -top * Pt);
        }

        static void Bottom(RectTransform rt, float bottom, float height, float margin = 0)
        {
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(0.5f, 0);
            rt.sizeDelta = new Vector2(-2 * margin * Pt, height * Pt);
            rt.anchoredPosition = new Vector2(0, bottom * Pt);
        }

        static Button LightPill(RectTransform parent, string name, string text, Color bg, Color fg)
        {
            var button = Pill(parent, name, text, bg, fg, 48);
            var shadow = button.gameObject.AddComponent<Shadow>();
            shadow.effectColor = Lip;
            shadow.effectDistance = new Vector2(0, -2 * Pt);
            var label = button.GetComponentInChildren<TextMeshProUGUI>();
            label.fontSize = 17 * Pt;
            return button;
        }
    }
}
