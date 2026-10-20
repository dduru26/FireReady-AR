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

        public static Result Build(Sprite rounded)
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

            var debrief = BuildDebrief(canvasGo.transform);
            var history = BuildHistory(canvasGo.transform);
            return new Result { Debrief = debrief, History = history, Canvas = (RectTransform)canvasGo.transform };
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
