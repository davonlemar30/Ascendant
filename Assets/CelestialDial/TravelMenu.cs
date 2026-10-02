using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Ascendant.CelestialDial
{
    // The room mini-menu (owner, Oct 1; task 86bca07wv: Option A, the room list, v2 spacing). A round gold button at the top left mirrors the
    // Settings gear and drops a small panel headed TRAVEL: the rooms open to travel, each with a line icon, the room you are in marked
    // "here" in amber, and one dim Sealed row with a lock (the Library has more to wake). Rooms only: the instruments keep their own exits.
    // Travel only: Your journal stays a room button. Doors stay the main way to move; a row is the quick way. The panel wears the slim box.
    public sealed class TravelMenu : MonoBehaviour
    {
        public static readonly (string id, string name)[] Rooms = { ("atrium", "The Grand Atrium"), ("wing", "The Zodiac Wing"), ("chamber", "The Crystal Book Chamber") };
        public const string SealedName = "Sealed";
        // The concept's measures on the 360 x 800 layout: the button mirrors the gear (x -158, 22 from the top, 36 x 36); the panel's left edge
        // at 17, 280 wide, from 47 down; rows 44 apart from 52 into the panel, the icon at 29 and the name at 51 from its left, "here" at its right.
        public const float ButtonX = -158, ButtonTop = 22, ButtonSize = 36, PanelLeft = 17, PanelWidth = 280, PanelTop = 47, FirstRow = 52, RowStep = 44;
        public Func<string> Here; public Func<string, bool> IsOpen; public Func<bool> Busy; public Action<string> Go; public Action Changed;
        public bool Open { get; private set; }
        public bool ButtonShown => button != null && button.gameObject.activeSelf;
        public Vector2 ButtonAt => button != null && button.GetComponent<SafeTop>() != null ? button.GetComponent<SafeTop>().At : new Vector2(ButtonX, ButtonTop); // Part 2: where the button sits
        public string[] Rows => rows.Where(r => r.Root.gameObject.activeSelf).Select(r => r.Name + (r.Here ? ", here" : "")).ToArray();
        public float PanelHeight => FirstRow + RowStep * (Mathf.Max(1, rows.Count(r => r.Root.gameObject.activeSelf)) - 1) + 28;
        static readonly Color Gold = new Color(.84f, .69f, .38f), Bone = new Color(.94f, .91f, .86f), Amber = new Color(.89f, .64f, .29f), HereFill = new Color(.23f, .19f, .15f, .92f), SealedInk = new Color(.62f, .57f, .53f, .55f);
        sealed class Row { public string Id, Name; public RectTransform Root; public Image Fill, Bar, Icon; public Text Label, Mark; public Button Button; public bool Here; }
        readonly List<Row> rows = new List<Row>();
        Button button; RectTransform panel, catcher; Font font;

        public void Build(RectTransform root, Font uiFont)
        {
            font = uiFont;
            // Tapping anywhere off the panel closes it; the catcher covers the frame under the panel and over the room.
            catcher = Rect("Travel catcher", root, 0, 400, 360, 800); var veil = catcher.gameObject.AddComponent<Image>(); veil.color = new Color(0, 0, 0, 0); veil.canvasRenderer.cullTransparentMesh = false;
            var close = catcher.gameObject.AddComponent<Button>(); close.transition = Selectable.Transition.None; close.onClick.AddListener(Close); catcher.gameObject.SetActive(false);
            // The panel hangs from its top edge, so its rows can come and go without moving it; it and the button sit below the cutout's band, like the gear.
            panel = Rect("Travel", root, PanelLeft + PanelWidth / 2 - 180, PanelTop, PanelWidth, 240); panel.pivot = new Vector2(.5f, 1); var panelTop = panel.gameObject.AddComponent<SafeTop>();
            var box = panel.gameObject.AddComponent<Image>(); box.sprite = Slots.InstrumentBoxSprite(); box.type = Image.Type.Sliced; box.pixelsPerUnitMultiplier = 2;
            panel.gameObject.AddComponent<Button>().transition = Selectable.Transition.None; // a tap on the panel itself does not close it
            var heading = Label(panel, "T R A V E L", 15 + 60 - PanelWidth / 2, 15, 120, 16, 11); heading.alignment = TextAnchor.MiddleLeft; heading.color = Gold; heading.fontStyle = FontStyle.Bold;
            var rule = Rect("Rule", panel, 15 + 28 - PanelWidth / 2, 27, 56, 1); rule.gameObject.AddComponent<Image>().color = new Color(Gold.r, Gold.g, Gold.b, .8f);
            foreach (var (id, name) in Rooms) rows.Add(MakeRow(id, name, RoomIcon(id)));
            rows.Add(MakeRow("sealed", SealedName, LockSprite()));
            button = MakeButton(root, "Travel", ButtonX, ButtonTop, ButtonSize, ButtonSize, Toggle); button.name = "Travel button"; // over the panel
            var face = button.GetComponent<Image>(); face.sprite = ButtonSprite(); face.color = Color.white; SafeArea.Corner(button, -1); panelTop.Follow = button.GetComponent<SafeTop>(); // the panel hangs under its button wherever the button sits
            ButtonLook.HitArea(button); // batch 2 (3d): the button takes taps over 44 x 44; it is drawn at 36
            panel.gameObject.SetActive(false); button.gameObject.SetActive(false);
        }
        Row MakeRow(string id, string name, Sprite icon)
        {
            var row = new Row { Id = id, Name = name };
            row.Root = Rect(name, panel, 0, 0, PanelWidth - 16, RowStep);
            row.Fill = Rect("Here", row.Root, 0, RowStep / 2, PanelWidth - 16, 40).gameObject.AddComponent<Image>(); row.Fill.color = HereFill; row.Fill.raycastTarget = false;
            row.Bar = Rect("Bar", row.Root, -(PanelWidth - 16) / 2 - 1.5f, RowStep / 2, 3, 40).gameObject.AddComponent<Image>(); row.Bar.color = Amber; row.Bar.raycastTarget = false;
            row.Icon = Rect("Icon", row.Root, 29 - 8 - (PanelWidth - 16) / 2, RowStep / 2, 24, 24).gameObject.AddComponent<Image>(); row.Icon.sprite = icon; row.Icon.raycastTarget = false;
            row.Label = Label(row.Root, name, 51 - 8 + 100 - (PanelWidth - 16) / 2, RowStep / 2, 200, 24, 15); row.Label.alignment = TextAnchor.MiddleLeft;
            row.Mark = Label(row.Root, "here", (PanelWidth - 16) / 2 - 10 - 20, RowStep / 2, 40, 20, 13); row.Mark.alignment = TextAnchor.MiddleRight; row.Mark.color = Amber;
            if (id != "sealed")
            {
                var hit = row.Root.gameObject.AddComponent<Image>(); hit.color = new Color(0, 0, 0, 0); hit.canvasRenderer.cullTransparentMesh = false; // an invisible tap area takes taps only with culling off
                row.Button = row.Root.gameObject.AddComponent<Button>(); row.Button.targetGraphic = hit; row.Button.transition = Selectable.Transition.None; row.Button.onClick.AddListener(() => Pick(id));
            }
            return row;
        }
        // Rooms only, and only while nothing else runs; any other screen closes the panel.
        public void Refresh(bool roomScreen)
        {
            if (button == null) return;
            button.gameObject.SetActive(roomScreen); button.interactable = roomScreen && !(Busy?.Invoke() ?? false);
            if (!roomScreen) { if (Open) Close(); return; }
            string here = Here?.Invoke() ?? ""; int shown = 0;
            foreach (var row in rows)
            {
                bool open = row.Id == "sealed" || (IsOpen?.Invoke(row.Id) ?? true); // the one Sealed row always shows (the Library has more to wake); a room not yet open is folded into it
                row.Root.gameObject.SetActive(open); if (!open) continue;
                row.Here = row.Id == here; row.Fill.enabled = row.Bar.enabled = row.Mark.enabled = row.Here;
                bool sealedRow = row.Id == "sealed"; row.Label.color = sealedRow ? SealedInk : Bone; row.Icon.color = sealedRow ? SealedInk : Gold;
                row.Root.anchoredPosition = new Vector2(0, -(FirstRow + RowStep * shown)); shown++;
            }
            panel.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        }
        public void Toggle() { if (Open) Close(); else if (ButtonShown && button.interactable) { Open = true; panel.gameObject.SetActive(true); catcher.gameObject.SetActive(true); Changed?.Invoke(); } }
        public void Close() { if (!Open) return; Open = false; panel.gameObject.SetActive(false); catcher.gameObject.SetActive(false); Changed?.Invoke(); }
        public void Pick(string id)
        {
            if (!Open || id == "sealed") return; string here = Here?.Invoke() ?? "";
            Close(); if (id != here && (IsOpen?.Invoke(id) ?? false) && !(Busy?.Invoke() ?? false)) Go?.Invoke(id);
        }

        // ---- building ----
        // x from the parent's centre, top = the centre's distance below the parent's top edge (the slice's 360 x 800 convention).
        static RectTransform Rect(string name, Transform parent, float x, float top, float width, float height)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1); rect.pivot = new Vector2(.5f, .5f); rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(x, -top); return rect;
        }
        Button MakeButton(Transform parent, string name, float x, float top, float width, float height, UnityEngine.Events.UnityAction action)
        {
            var rect = Rect(name, parent, x, top, width, height); var image = rect.gameObject.AddComponent<Image>(); image.canvasRenderer.cullTransparentMesh = false;
            var b = rect.gameObject.AddComponent<Button>(); b.targetGraphic = image; b.onClick.AddListener(action);
            var colors = b.colors; colors.pressedColor = new Color(.78f, .78f, .78f); colors.disabledColor = Color.white; b.colors = colors; return b;
        }
        Text Label(Transform parent, string text, float x, float top, float width, float height, int size)
        {
            var label = Rect("Label", parent, x, top, width, height).gameObject.AddComponent<Text>();
            label.font = font; label.text = text; label.fontSize = size; label.color = Bone; label.alignment = TextAnchor.MiddleCenter; label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Overflow; return label;
        }

        // ---- the marks, drawn once in code at 2x (the web font has no such glyphs), white so an Image's color tints them ----
        // Each shape is strokes and dots on a 24 x 24 grid (y down), rasterised with a half-pixel edge.
        sealed class Shape { public readonly List<(Vector2 a, Vector2 b)> Lines = new List<(Vector2, Vector2)>(); public readonly List<(Vector2 c, float r, float from, float to)> Arcs = new List<(Vector2, float, float, float)>(); public readonly List<(Vector2 c, float r)> Dots = new List<(Vector2, float)>(); public float Stroke = 1.4f; }
        static Sprite Draw(Shape s, int px = 48, Color? disc = null, float discRadius = 0, Color? ring = null, float ringRadius = 0)
        {
            var texture = new Texture2D(px, px, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear }; float k = px / 24f;
            for (int y = 0; y < px; y++) for (int x = 0; x < px; x++)
            {
                var p = new Vector2((x + .5f) / k, 24 - (y + .5f) / k); float d = float.MaxValue;
                foreach (var (a, b) in s.Lines) { var ab = b - a; float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f)); d = Mathf.Min(d, (p - (a + t * ab)).magnitude); }
                foreach (var (c, r, from, to) in s.Arcs)
                {
                    var v = p - c; float ang = Mathf.Atan2(-v.y, v.x) * Mathf.Rad2Deg; if (ang < 0) ang += 360; // degrees counter-clockwise from 3 o'clock, screen up
                    bool inside = from <= to ? ang >= from && ang <= to : ang >= from || ang <= to;
                    d = inside ? Mathf.Min(d, Mathf.Abs(v.magnitude - r)) : Mathf.Min(d, Mathf.Min((p - (c + r * Dir(from))).magnitude, (p - (c + r * Dir(to))).magnitude));
                }
                float line = Mathf.Clamp01((s.Stroke / 2 - d) * k + .5f), dot = 0;
                foreach (var (c, r) in s.Dots) dot = Mathf.Max(dot, Mathf.Clamp01((r - (p - c).magnitude) * k + .5f));
                float mark = Mathf.Max(line, dot); var col = new Color(1, 1, 1, mark);
                if (disc.HasValue)
                {
                    float dist = (p - new Vector2(12, 12)).magnitude, inDisc = Mathf.Clamp01((discRadius - dist) * k + .5f), inRing = Mathf.Clamp01((.9f - Mathf.Abs(dist - ringRadius)) * k + .5f);
                    var gold = ring ?? Color.white; var dc = disc.Value;
                    col = Color.Lerp(new Color(dc.r, dc.g, dc.b, dc.a * inDisc), new Color(gold.r, gold.g, gold.b, 1), Mathf.Max(mark, inRing));
                    col.a = Mathf.Max(dc.a * inDisc, Mathf.Max(mark, inRing));
                }
                texture.SetPixel(x, y, col);
            }
            texture.Apply(); return Sprite.Create(texture, new UnityEngine.Rect(0, 0, px, px), new Vector2(.5f, .5f), 100);
        }
        static Vector2 Dir(float deg) => new Vector2(Mathf.Cos(deg * Mathf.Deg2Rad), -Mathf.Sin(deg * Mathf.Deg2Rad));
        static readonly Dictionary<string, Sprite> marks = new Dictionary<string, Sprite>();
        static Sprite Mark(string key, Func<Sprite> make) { if (!marks.TryGetValue(key, out var s) || s == null) marks[key] = s = make(); return s; }
        // The button: the gear's dark disc with a gold ring, and three linked stars, a small constellation.
        static Sprite ButtonSprite() => Mark("button", () =>
        {
            var s = new Shape { Stroke = 1.1f }; Vector2 a = new Vector2(7, 15.5f), b = new Vector2(12, 8.5f), c = new Vector2(17, 15.5f);
            s.Lines.Add((a, b)); s.Lines.Add((b, c)); s.Dots.Add((a, 1.6f)); s.Dots.Add((b, 1.6f)); s.Dots.Add((c, 1.6f));
            return Draw(s, 72, new Color(.086f, .078f, .094f, .85f), 11.4f, Gold, 10.4f);
        });
        static Sprite RoomIcon(string id) => Mark(id, () =>
        {
            var s = new Shape();
            if (id == "atrium") // the dome on its colonnade
            {
                s.Arcs.Add((new Vector2(12, 11), 7, 0, 180)); s.Lines.Add((new Vector2(3.5f, 11), new Vector2(20.5f, 11))); s.Lines.Add((new Vector2(4.5f, 13), new Vector2(19.5f, 13)));
                foreach (float x in new[] { 6.5f, 10, 14, 17.5f }) s.Lines.Add((new Vector2(x, 13), new Vector2(x, 19))); s.Lines.Add((new Vector2(3.5f, 20.2f), new Vector2(20.5f, 20.2f)));
            }
            else if (id == "wing") // the wheel: rim, hub and eight spokes
            {
                s.Arcs.Add((new Vector2(12, 12), 8.5f, 0, 360)); s.Arcs.Add((new Vector2(12, 12), 3.5f, 0, 360));
                for (int i = 0; i < 8; i++) { var d = Dir(i * 45); s.Lines.Add((new Vector2(12, 12) + d * 3.5f, new Vector2(12, 12) + d * 8.5f)); }
            }
            else // the crystal: a tall gem, its girdle and its keel
            {
                Vector2 top = new Vector2(12, 2.5f), left = new Vector2(6.5f, 9), right = new Vector2(17.5f, 9), bottom = new Vector2(12, 21.5f);
                s.Lines.Add((top, right)); s.Lines.Add((right, bottom)); s.Lines.Add((bottom, left)); s.Lines.Add((left, top)); s.Lines.Add((left, right)); s.Lines.Add((new Vector2(12, 9), bottom));
            }
            return Draw(s);
        });
        static Sprite LockSprite() => Mark("lock", () =>
        {
            var s = new Shape(); s.Arcs.Add((new Vector2(12, 10), 4, 0, 180)); s.Lines.Add((new Vector2(8, 10), new Vector2(8, 12))); s.Lines.Add((new Vector2(16, 10), new Vector2(16, 12)));
            s.Lines.Add((new Vector2(6, 12), new Vector2(18, 12))); s.Lines.Add((new Vector2(18, 12), new Vector2(18, 20.5f))); s.Lines.Add((new Vector2(18, 20.5f), new Vector2(6, 20.5f))); s.Lines.Add((new Vector2(6, 20.5f), new Vector2(6, 12)));
            return Draw(s);
        });
    }
}
