using FireReady.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static FireReady.EditorTools.BuilderUtils;
using static FireReady.EditorTools.SetupHudBuilder;

namespace FireReady.EditorTools
{
    /// <summary>Builds the simulation HUD (design screens 8 and 9) and the completion card.</summary>
    internal static class SimulationHudBuilder
    {
        const string CanvasName = "Simulation HUD";

        static readonly Color Panel = Hex(0x0E1116, 0.86f);
        static readonly Color TextColor = Hex(0xF3F4F6);
        static readonly Color Muted = Hex(0xAEB4BE);
        static readonly Color Secondary = Hex(0xC9CED6);
        static readonly Color GuidedBlue = Hex(0x1F5FBF);
        static readonly Color Danger = Hex(0xD7261E);
        static readonly Color Blue = Hex(0x0C70FB);
        static readonly Color Track = Hex(0x2A3038);

        public static SimulationHudView Build(Sprite rounded)
        {
            s_Rounded = rounded;
            var old = GameObject.Find(CanvasName);
            if (old != null) Object.DestroyImmediate(old);

            var canvasGo = new GameObject(CanvasName, typeof(RectTransform)) { layer = 5 };
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1170, 2532);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var safe = Node("Safe Area", canvasGo.transform);
            Stretch(safe);
            safe.gameObject.AddComponent<SafeAreaFitter>();

            var hud = Node("HUD", safe);
            Stretch(hud);

            // ---- Top bar: title + timer, mode pill
            var topBar = TopBlock(hud, "Top Bar", 8, 44);
            var titlePill = Node("Title Pill", topBar);
            Place(titlePill, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0), new Vector2(-96 * Pt, 0), new Vector2(0, 0.5f));
            Rounded(titlePill, Panel, 22 * Pt).raycastTarget = false;
            var title = Label(titlePill, "Title", "Office waste bin", 16, TextColor, FontStyles.Bold);
            Place(title.rectTransform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(14 * Pt, 0), new Vector2(-84 * Pt, 0), new Vector2(0, 0.5f));
            title.alignment = TextAlignmentOptions.MidlineLeft;
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.overflowMode = TextOverflowModes.Ellipsis;
            var timer = Label(titlePill, "Timer", "00:00", 16, Secondary, FontStyles.Normal);
            Place(timer.rectTransform, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-14 * Pt, 0), new Vector2(70 * Pt, 0), new Vector2(1, 0.5f));
            timer.alignment = TextAlignmentOptions.MidlineRight;

            var modePill = Node("Mode Pill", topBar);
            Place(modePill, new Vector2(1, 0), new Vector2(1, 1), Vector2.zero, new Vector2(88 * Pt, 0), new Vector2(1, 0.5f));
            Rounded(modePill, GuidedBlue, 22 * Pt).raycastTarget = false;
            var mode = Label(modePill, "Label", "Guided", 15, Color.white, FontStyles.Bold);
            Stretch(mode.rectTransform);
            mode.alignment = TextAlignmentOptions.Center;

            // ---- Intensity card
            var intensity = TopBlock(hud, "Intensity Card", 62, 76);
            Rounded(intensity, Panel, 16 * Pt).raycastTarget = false;
            var intensityLabel = Label(intensity, "Label", "Fire intensity", 14, Muted, FontStyles.Bold);
            Place(intensityLabel.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(16 * Pt, -11 * Pt), new Vector2(180 * Pt, 22 * Pt), new Vector2(0, 1));
            intensityLabel.alignment = TextAlignmentOptions.MidlineLeft;
            var value = Label(intensity, "Value", "30", 18, TextColor, FontStyles.Bold);
            Place(value.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-16 * Pt, -10 * Pt), new Vector2(40 * Pt, 24 * Pt), new Vector2(1, 1));
            value.alignment = TextAlignmentOptions.MidlineRight;
            var badge = Node("Trend Badge", intensity);
            Place(badge, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-64 * Pt, -11 * Pt), new Vector2(88 * Pt, 22 * Pt), new Vector2(1, 1));
            var badgeImage = Rounded(badge, Track, 6 * Pt);
            badgeImage.raycastTarget = false;
            var badgeLabel = Label(badge, "Label", "Steady", 13, Color.white, FontStyles.Bold);
            Stretch(badgeLabel.rectTransform);
            badgeLabel.alignment = TextAlignmentOptions.Center;

            var bar = Node("Bar", intensity);
            Place(bar, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -42 * Pt), new Vector2(-32 * Pt, 8 * Pt), new Vector2(0.5f, 1));
            Rounded(bar, Track, 4 * Pt).raycastTarget = false;
            var fill = Node("Fill", bar);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(0.3f, 1f);
            fill.offsetMin = fill.offsetMax = Vector2.zero;
            Rounded(fill, Danger, 4 * Pt).raycastTarget = false;
            var marker = Node("Threshold", bar);
            Place(marker, new Vector2(0.7f, 0.5f), new Vector2(0.7f, 0.5f), Vector2.zero, new Vector2(2 * Pt, 14 * Pt), new Vector2(0.5f, 0.5f));
            marker.gameObject.AddComponent<Image>().color = Hex(0xF5C518);
            var thresholdLabel = Label(bar, "Threshold Label", "Evacuate above 70", 12, Secondary, FontStyles.Normal);
            Place(thresholdLabel.rectTransform, new Vector2(0.7f, 0), new Vector2(0.7f, 0), new Vector2(0, -6 * Pt), new Vector2(160 * Pt, 16 * Pt), new Vector2(0.5f, 1));
            thresholdLabel.alignment = TextAlignmentOptions.Center;

            // ---- Step card and warning card (same slot)
            var step = TopBlock(hud, "Step Card", 148, 112);
            Rounded(step, GuidedBlue, 16 * Pt).raycastTarget = false;
            var counter = Label(step, "Counter", "STEP 1 OF 6", 13, Hex(0xDCE6F7), FontStyles.Bold);
            Place(counter.rectTransform, new Vector2(0, 1), new Vector2(0.5f, 1), new Vector2(16 * Pt, -14 * Pt), new Vector2(0, 18 * Pt), new Vector2(0, 1));
            counter.characterSpacing = 4f;
            var next = Label(step, "Next", "Next: pick an extinguisher", 13, Hex(0xDCE6F7), FontStyles.Bold);
            Place(next.rectTransform, new Vector2(0.4f, 1), new Vector2(1, 1), new Vector2(-16 * Pt, -14 * Pt), new Vector2(0, 18 * Pt), new Vector2(1, 1));
            next.alignment = TextAlignmentOptions.TopRight;
            next.textWrappingMode = TextWrappingModes.NoWrap;
            next.overflowMode = TextOverflowModes.Ellipsis;
            var stepTitle = Label(step, "Title", "Raise the alarm", 20, Color.white, FontStyles.Bold);
            Place(stepTitle.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -36 * Pt), new Vector2(-32 * Pt, 52 * Pt), new Vector2(0.5f, 1));
            stepTitle.alignment = TextAlignmentOptions.TopLeft;
            var segments = Node("Segments", step);
            Place(segments, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 14 * Pt), new Vector2(-32 * Pt, 4 * Pt), new Vector2(0.5f, 0));
            var segLayout = segments.gameObject.AddComponent<HorizontalLayoutGroup>();
            segLayout.spacing = 4 * Pt;
            segLayout.childControlWidth = segLayout.childControlHeight = true;
            segLayout.childForceExpandWidth = segLayout.childForceExpandHeight = true;
            var segmentImages = new Image[6];
            for (int i = 0; i < 6; i++)
            {
                var seg = Node($"Segment {i + 1}", segments);
                segmentImages[i] = Rounded(seg, new Color(1, 1, 1, i == 0 ? 0.6f : 0.25f), 2 * Pt);
                segmentImages[i].raycastTarget = false;
            }

            var warning = TopBlock(hud, "Warning Card", 148, 112);
            Rounded(warning, Hex(0xB91C1C, 0.96f), 16 * Pt).raycastTarget = false;
            var warnTitle = Label(warning, "Title", "Water spreads burning oil", 20, Color.white, FontStyles.Bold);
            Place(warnTitle.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -14 * Pt), new Vector2(-32 * Pt, 26 * Pt), new Vector2(0.5f, 1));
            warnTitle.textWrappingMode = TextWrappingModes.NoWrap;
            warnTitle.overflowMode = TextOverflowModes.Ellipsis;
            var warnAdvice = Label(warning, "Advice", "Stop spraying and put it back.", 15, Hex(0xFFE4E1), FontStyles.Normal);
            Place(warnAdvice.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -44 * Pt), new Vector2(-32 * Pt, 60 * Pt), new Vector2(0.5f, 1));
            warnAdvice.alignment = TextAlignmentOptions.TopLeft;

            // ---- Technique chips
            var chips = TopBlock(hud, "Technique Chips", 270, 50);
            var chipLayout = chips.gameObject.AddComponent<HorizontalLayoutGroup>();
            chipLayout.spacing = 8 * Pt;
            chipLayout.childControlWidth = chipLayout.childControlHeight = true;
            chipLayout.childForceExpandWidth = chipLayout.childForceExpandHeight = true;
            var distance = Chip(chips, "Distance", "-- m", "Distance", out var distanceValue, out var distanceLabel);
            var aim = Chip(chips, "Aim", "Aim", "On the base", out _, out var aimLabel);
            var sweep = Chip(chips, "Sweep", "Sweep", "Side to side", out _, out var sweepLabel);

            // ---- Bottom: caption, extinguisher card, actions
            var actions = BottomBlock(hud, "Actions", 8, 124);
            var alarm = CircleAction(actions, "Alarm", "!", "Raise alarm", 0.17f, out var alarmLabel, out var alarmCircle);
            var evacuate = CircleAction(actions, "Evacuate", "EXIT", "Evacuate", 0.83f, out _, out _);
            var squeeze = SqueezeButton(actions);

            var card = BottomBlock(hud, "Extinguisher Card", 140, 64);
            Rounded(card, Panel, 16 * Pt).raycastTarget = true;
            var emptyRow = Node("Empty Hand", card);
            Stretch(emptyRow);
            var emptyText = Label(emptyRow, "Text", "Tap an extinguisher on the rack to pick it up", 15, Secondary, FontStyles.Normal);
            Stretch(emptyText.rectTransform);
            emptyText.margin = new Vector4(14 * Pt, 0, 14 * Pt, 0);
            emptyText.alignment = TextAlignmentOptions.Center;

            var heldRow = Node("Held", card);
            Stretch(heldRow);
            var swatch = Node("Band", heldRow);
            Place(swatch, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(14 * Pt, 0), new Vector2(18 * Pt, 40 * Pt), new Vector2(0, 0.5f));
            var swatchImage = Rounded(swatch, Danger, 6 * Pt);
            swatchImage.raycastTarget = false;
            var agentName = Label(heldRow, "Name", "Water", 16, TextColor, FontStyles.Bold);
            Place(agentName.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(44 * Pt, -9 * Pt), new Vector2(120 * Pt, 22 * Pt), new Vector2(0, 1));
            agentName.textWrappingMode = TextWrappingModes.NoWrap;
            var status = Label(heldRow, "Status", "Pin in · 12.0 s left", 12, Secondary, FontStyles.Normal);
            Place(status.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(44 * Pt, -31 * Pt), new Vector2(-(44 + 186) * Pt, 16 * Pt), new Vector2(0, 1));
            status.textWrappingMode = TextWrappingModes.NoWrap;
            status.overflowMode = TextOverflowModes.Ellipsis;
            status.alignment = TextAlignmentOptions.TopLeft;
            var track = Node("Remaining", heldRow);
            Place(track, new Vector2(0, 0), new Vector2(1, 0), new Vector2(44 * Pt, 10 * Pt), new Vector2(-(44 + 186) * Pt, 6 * Pt), new Vector2(0, 0));
            Rounded(track, Track, 3 * Pt).raycastTarget = false;
            var remaining = Node("Fill", track);
            remaining.anchorMin = Vector2.zero;
            remaining.anchorMax = Vector2.one;
            remaining.offsetMin = remaining.offsetMax = Vector2.zero;
            Rounded(remaining, TextColor, 3 * Pt).raycastTarget = false;

            var pullPin = SmallPill(heldRow, "Pull Pin", "Pull pin", Hex(0xF5C518), Hex(0x1A1A1A), new Vector2(-96 * Pt, 0));
            var putBack = SmallPill(heldRow, "Put Back", "Put back", new Color(1, 1, 1, 0.14f), TextColor, new Vector2(-8 * Pt, 0));

            var caption = BottomBlock(hud, "Caption", 214, 34);
            var captionPill = Node("Pill", caption);
            Place(captionPill, new Vector2(0.5f, 0), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, 0), new Vector2(0.5f, 0.5f));
            Rounded(captionPill, Panel, 17 * Pt).raycastTarget = false;
            var captionLayout = captionPill.gameObject.AddComponent<HorizontalLayoutGroup>();
            captionLayout.padding = new RectOffset((int)(14 * Pt), (int)(14 * Pt), 0, 0);
            captionLayout.childAlignment = TextAnchor.MiddleCenter;
            captionLayout.childControlWidth = captionLayout.childControlHeight = true;
            captionLayout.childForceExpandWidth = false;
            captionLayout.childForceExpandHeight = true;
            captionPill.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            var captionLabel = Label(captionPill, "Text", "Fire crackling", 14, Hex(0xE4E7EB), FontStyles.Normal);
            captionLabel.alignment = TextAlignmentOptions.Center;
            captionLabel.textWrappingMode = TextWrappingModes.NoWrap;

            // ---- Completion card
            var completion = Node("Completion", safe);
            Place(completion, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(358 * Pt, 0), new Vector2(0.5f, 0.5f));
            Rounded(completion, Hex(0x171B21), 22 * Pt);
            var compLayout = completion.gameObject.AddComponent<VerticalLayoutGroup>();
            compLayout.padding = new RectOffset((int)(20 * Pt), (int)(20 * Pt), (int)(24 * Pt), (int)(20 * Pt));
            compLayout.spacing = 12 * Pt;
            compLayout.childControlWidth = compLayout.childControlHeight = true;
            compLayout.childForceExpandWidth = true;
            compLayout.childForceExpandHeight = false;
            completion.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var compTitle = Label(completion, "Title", "Fire extinguished", 28, TextColor, FontStyles.Bold);
            compTitle.alignment = TextAlignmentOptions.Center;
            var compMeta = Label(completion, "Meta", "Office waste bin · Guided · 00:00", 14, Muted, FontStyles.Normal);
            compMeta.alignment = TextAlignmentOptions.Center;
            var compBody = Label(completion, "Body", "You put the fire out.", 16, Secondary, FontStyles.Normal);
            compBody.alignment = TextAlignmentOptions.Center;
            var restart = Pill(completion, "Restart Button", "Restart", Blue, Color.white, 54);

            // ---- View
            var view = canvasGo.AddComponent<SimulationHudView>();
            SetRef(view, "hudRoot", hud.gameObject);
            SetRef(view, "completionCard", completion.gameObject);
            SetRef(view, "scenarioTitle", title);
            SetRef(view, "timerLabel", timer);
            SetRef(view, "trendBadge", badgeImage);
            SetRef(view, "trendLabel", badgeLabel);
            SetRef(view, "intensityValue", value);
            SetRef(view, "intensityFill", fill);
            SetRef(view, "thresholdMarker", marker);
            SetRef(view, "thresholdLabel", thresholdLabel);
            SetRef(view, "stepCard", step.gameObject);
            SetRef(view, "stepCounter", counter);
            SetRef(view, "stepNext", next);
            SetRef(view, "stepTitle", stepTitle);
            var so = new UnityEditor.SerializedObject(view);
            var segProp = so.FindProperty("stepSegments");
            segProp.arraySize = segmentImages.Length;
            for (int i = 0; i < segmentImages.Length; i++)
                segProp.GetArrayElementAtIndex(i).objectReferenceValue = segmentImages[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            SetRef(view, "warningCard", warning.gameObject);
            SetRef(view, "warningTitle", warnTitle);
            SetRef(view, "warningAdvice", warnAdvice);
            SetRef(view, "chipsRow", chips.gameObject);
            SetRef(view, "distanceChip", distance);
            SetRef(view, "distanceValue", distanceValue);
            SetRef(view, "distanceLabel", distanceLabel);
            SetRef(view, "aimChip", aim);
            SetRef(view, "aimLabel", aimLabel);
            SetRef(view, "sweepChip", sweep);
            SetRef(view, "sweepLabel", sweepLabel);
            SetRef(view, "captionLabel", captionLabel);
            SetRef(view, "emptyHandRow", emptyRow.gameObject);
            SetRef(view, "heldRow", heldRow.gameObject);
            SetRef(view, "bandSwatch", swatchImage);
            SetRef(view, "agentName", agentName);
            SetRef(view, "agentStatus", status);
            SetRef(view, "remainingFill", remaining);
            SetRef(view, "pullPinButton", pullPin);
            SetRef(view, "putBackButton", putBack);
            SetRef(view, "alarmButton", alarm);
            SetRef(view, "alarmLabel", alarmLabel);
            SetRef(view, "alarmIcon", alarmCircle);
            SetRef(view, "squeezeButton", squeeze);
            SetRef(view, "evacuateButton", evacuate);
            SetRef(view, "completionTitle", compTitle);
            SetRef(view, "completionBody", compBody);
            SetRef(view, "completionMeta", compMeta);
            SetRef(view, "restartButton", restart);
            return view;
        }

        // ---- Layout helpers --------------------------------------------------------------

        static void Place(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, Vector2 pivot)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.sizeDelta = size;
            rt.anchoredPosition = position;
        }

        static RectTransform TopBlock(RectTransform parent, string name, float topPt, float heightPt)
        {
            var rt = Node(name, parent);
            Place(rt, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -topPt * Pt), new Vector2(-32 * Pt, heightPt * Pt), new Vector2(0.5f, 1));
            return rt;
        }

        static RectTransform BottomBlock(RectTransform parent, string name, float bottomPt, float heightPt)
        {
            var rt = Node(name, parent);
            Place(rt, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, bottomPt * Pt), new Vector2(-32 * Pt, heightPt * Pt), new Vector2(0.5f, 0));
            return rt;
        }

        static Image Chip(RectTransform parent, string name, string value, string label, out TextMeshProUGUI valueText, out TextMeshProUGUI labelText)
        {
            var chip = Node(name, parent);
            var image = Rounded(chip, Panel, 12 * Pt);
            image.raycastTarget = false;
            valueText = Label(chip, "Value", value, 15, Color.white, FontStyles.Bold);
            Place(valueText.rectTransform, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(12 * Pt, 1 * Pt), new Vector2(-16 * Pt, 18 * Pt), new Vector2(0, 0));
            valueText.textWrappingMode = TextWrappingModes.NoWrap;
            labelText = Label(chip, "Label", label, 12, Color.white, FontStyles.Normal);
            Place(labelText.rectTransform, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(12 * Pt, -1 * Pt), new Vector2(-16 * Pt, 16 * Pt), new Vector2(0, 1));
            labelText.textWrappingMode = TextWrappingModes.NoWrap;
            labelText.overflowMode = TextOverflowModes.Ellipsis;
            return image;
        }

        static Button CircleAction(RectTransform parent, string name, string glyph, string label, float x,
            out TextMeshProUGUI labelText, out Image circle)
        {
            var root = Node(name, parent);
            Place(root, new Vector2(x, 1), new Vector2(x, 1), Vector2.zero, new Vector2(96 * Pt, 92 * Pt), new Vector2(0.5f, 1));
            var circleRt = Node("Circle", root);
            Place(circleRt, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -8 * Pt), new Vector2(60 * Pt, 60 * Pt), new Vector2(0.5f, 1));
            circle = Rounded(circleRt, Panel, 30 * Pt);
            var button = circleRt.gameObject.AddComponent<Button>();
            button.targetGraphic = circle;
            var glyphText = Label(circleRt, "Glyph", glyph, glyph.Length > 1 ? 14 : 26, TextColor, FontStyles.Bold);
            Stretch(glyphText.rectTransform);
            glyphText.alignment = TextAlignmentOptions.Center;
            labelText = Label(root, "Label", label, 13, TextColor, FontStyles.Bold);
            Place(labelText.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -72 * Pt), new Vector2(0, 18 * Pt), new Vector2(0.5f, 1));
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.textWrappingMode = TextWrappingModes.NoWrap;
            return button;
        }

        static HoldButton SqueezeButton(RectTransform parent)
        {
            var root = Node("Squeeze", parent);
            Place(root, new Vector2(0.5f, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(96 * Pt, 96 * Pt), new Vector2(0.5f, 1));
            var ring = Rounded(root, new Color(1, 1, 1, 0.9f), 48 * Pt);
            var inner = Node("Inner", root);
            Place(inner, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(88 * Pt, 88 * Pt), new Vector2(0.5f, 0.5f));
            var innerImage = Rounded(inner, Danger, 44 * Pt);
            innerImage.raycastTarget = false;
            var top = Label(inner, "Hold", "HOLD TO", 11, Color.white, FontStyles.Bold);
            Place(top.rectTransform, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0, 2 * Pt), new Vector2(0, 14 * Pt), new Vector2(0.5f, 0));
            top.alignment = TextAlignmentOptions.Center;
            top.characterSpacing = 6f;
            var main = Label(inner, "Squeeze", "Squeeze", 20, Color.white, FontStyles.Bold);
            Place(main.rectTransform, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0, 2 * Pt), new Vector2(0, 24 * Pt), new Vector2(0.5f, 1));
            main.alignment = TextAlignmentOptions.Center;
            var hold = root.gameObject.AddComponent<HoldButton>();
            hold.targetGraphic = ring;
            var colors = hold.colors;
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            hold.colors = colors;
            return hold;
        }

        static Button SmallPill(RectTransform parent, string name, string text, Color background, Color textColor, Vector2 rightOffset)
        {
            var rt = Node(name, parent);
            Place(rt, new Vector2(1, 0.5f), new Vector2(1, 0.5f), rightOffset, new Vector2(84 * Pt, 36 * Pt), new Vector2(1, 0.5f));
            var image = Rounded(rt, background, 18 * Pt);
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var label = Label(rt, "Label", text, 14, textColor, FontStyles.Bold);
            Stretch(label.rectTransform);
            label.alignment = TextAlignmentOptions.Center;
            return button;
        }
    }
}
