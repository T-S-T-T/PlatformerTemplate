using System;
using UnityEngine;
using UnityEngine.UI;

namespace PlatformerTemplate
{
    /// <summary>Builds simple uGUI elements in code so the template needs no UI prefabs or fonts.</summary>
    public static class UIFactory
    {
        public static readonly Color PanelColor = new Color(0.07f, 0.08f, 0.13f, 0.95f);
        public static readonly Color ButtonColor = new Color(0.20f, 0.25f, 0.38f, 1f);
        public static readonly Color AccentColor = new Color(1f, 0.82f, 0.3f, 1f);
        public static readonly Color TextColor = new Color(0.95f, 0.96f, 1f, 1f);

        static Font font;

        public static Font DefaultFont
        {
            get
            {
                if (font == null)
                {
                    font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 16);
                }
                return font;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { font = null; }

        public static GameObject Create(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        public static RectTransform Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static Image MakeImage(Transform parent, string name, Color color)
        {
            var go = Create(name, parent);
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        public static Text MakeLabel(Transform parent, string text, int size, TextAnchor anchor, Color color)
        {
            var go = Create("Label", parent);
            var t = go.AddComponent<Text>();
            t.font = DefaultFont;
            t.text = text;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = color;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        public static Button MakeButton(Transform parent, string label, Action onClick, float height = 48f)
        {
            var go = Create(label + " Button", parent);
            var img = go.AddComponent<Image>();
            img.color = ButtonColor;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.35f, 1.35f, 1.35f, 1f);
            colors.selectedColor = new Color(1.5f, 1.4f, 1.1f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.6f);
            btn.colors = colors;

            var le = go.AddComponent<LayoutElement>();
            le.minHeight = height;
            le.preferredHeight = height;

            var text = MakeLabel(go.transform, label, 22, TextAnchor.MiddleCenter, TextColor);
            Stretch(text.rectTransform);

            btn.onClick.AddListener(() =>
            {
                GameEvents.PlaySfx(SfxId.UiClick);
                if (onClick != null) onClick();
            });
            return btn;
        }

        /// <summary>
        /// A dimmed full-screen panel with a centred, auto-sizing column. Returns the column; root is the whole panel.
        /// </summary>
        public static RectTransform MakeMenu(Transform parent, string name, string title, float width, out GameObject root)
        {
            root = Create(name, parent);
            Stretch(root.GetComponent<RectTransform>());
            var dim = root.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.6f);

            var col = Create("Column", root.transform);
            var rt = col.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(width, 0f);

            var bg = col.AddComponent<Image>();
            bg.color = PanelColor;

            var v = col.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(28, 28, 24, 24);
            v.spacing = 12f;
            v.childAlignment = TextAnchor.UpperCenter;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;

            var fit = col.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            if (!string.IsNullOrEmpty(title))
            {
                var t = MakeLabel(col.transform, title, 34, TextAnchor.MiddleCenter, AccentColor);
                var le = t.gameObject.AddComponent<LayoutElement>();
                le.minHeight = 56f;
                le.preferredHeight = 56f;
            }
            return rt;
        }

        public static void MakeSpacer(Transform parent, float height)
        {
            var go = Create("Spacer", parent);
            var le = go.AddComponent<LayoutElement>();
            le.minHeight = height;
            le.preferredHeight = height;
        }

        /// <summary>Label with  -  value  +  buttons.</summary>
        public static void MakeStepper(Transform parent, string label, Func<string> getValue, Action decrease, Action increase)
        {
            var row = Create(label + " Row", parent);
            var h = row.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 8f;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = true;
            var rowLe = row.AddComponent<LayoutElement>();
            rowLe.minHeight = 44f;
            rowLe.preferredHeight = 44f;

            var name = MakeLabel(row.transform, label, 20, TextAnchor.MiddleLeft, TextColor);
            name.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            Text valueText = null;
            var minus = MakeButton(row.transform, "-", () => { decrease(); valueText.text = getValue(); }, 44f);
            minus.GetComponent<LayoutElement>().preferredWidth = 48f;

            valueText = MakeLabel(row.transform, getValue(), 20, TextAnchor.MiddleCenter, AccentColor);
            valueText.gameObject.AddComponent<LayoutElement>().preferredWidth = 80f;

            var plus = MakeButton(row.transform, "+", () => { increase(); valueText.text = getValue(); }, 44f);
            plus.GetComponent<LayoutElement>().preferredWidth = 48f;
        }

        /// <summary>Label with an On/Off button.</summary>
        public static void MakeToggle(Transform parent, string label, Func<bool> get, Action<bool> set)
        {
            var row = Create(label + " Row", parent);
            var h = row.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 8f;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = true;
            var rowLe = row.AddComponent<LayoutElement>();
            rowLe.minHeight = 44f;
            rowLe.preferredHeight = 44f;

            var name = MakeLabel(row.transform, label, 20, TextAnchor.MiddleLeft, TextColor);
            name.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            Button btn = null;
            btn = MakeButton(row.transform, get() ? "On" : "Off", () =>
            {
                set(!get());
                btn.GetComponentInChildren<Text>().text = get() ? "On" : "Off";
            }, 44f);
            btn.GetComponent<LayoutElement>().preferredWidth = 120f;
        }
    }
}
