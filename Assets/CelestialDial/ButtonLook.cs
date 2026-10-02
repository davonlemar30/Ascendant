using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ascendant.CelestialDial
{
    // Batch 2: the button art, by place (owner, Oct 1: 3b C, 3c C, 3d; Oct 2: "Buttons at 44 px, bronze on the Table and the Book too";
    // task 86bcbn6w6; the art lane's board, Oct 2). On the instruments (the Dial, the Elemental Table, the Book of Symbols and practice):
    // bronze. An action is a plate with its word engraved, Previous and Next are bronze arrows, and the way out is gold lettering on a rule.
    // In the rooms, the chat box's language: a see-through dark fill, a gold hairline and small gold capitals.
    // The states (3d): pressed darkens (x .78) and sinks 1 px; unavailable dims to half.
    // A button whose pieces have no file keeps today's look, so the art is a file drop like the rest.
    public static class ButtonLook
    {
        public const float MinTarget = 44; // 3d: every target stays at least 44 px (owner, Oct 2: "Buttons at 44 px")
        public static readonly Color Engraved = new Color32(0xf4, 0xcf, 0x7f, 0xff), EngravedDown = new Color32(0xbe, 0xa1, 0x63, 0xff); // the board's engraved gold, and pressed
        public static readonly Color RoomGold = new Color32(0xd6, 0xb0, 0x61, 0xff), RoomGoldDown = new Color32(0xa7, 0x89, 0x4c, 0xff);
        static Font engraved; static Sprite roomFrame; static readonly Dictionary<string, Sprite> sliced = new Dictionary<string, Sprite>();
        public static Font EngravedFont { get { if (engraved == null) engraved = Resources.Load<Font>("Fonts/EBGaramond-Bold"); return engraved; } }

        // An action on an instrument: the plate's frame nine-sliced to the button; its top notch and bottom diamond stay whole at the centre.
        public static ButtonFeel Plate(Button button, int size = 0)
        {
            var frame = Sliced("btn-plate", new Vector4(40, 40, 40, 40)); var notch = Slots.Image("btn-plate-notch"); var diamond = Slots.Image("btn-plate-diamond");
            if (frame == null || notch == null || diamond == null || EngravedFont == null) return null;
            var feel = Begin(button, "plate"); var rect = ((RectTransform)button.transform).rect;
            Piece(feel, "Plate", frame, true, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Piece(feel, "Notch", notch, false, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(-6, -12), new Vector2(6, 0));
            Piece(feel, "Diamond", diamond, false, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(-8, 1), new Vector2(8, 19));
            EngravedLabel(feel, size > 0 ? size : Mathf.Min(19, Mathf.RoundToInt(rect.height * .38f)), new Vector2(8, 4), new Vector2(-8, -4));
            return feel;
        }
        // Previous or Next on an instrument: the bronze arrow (Next is Previous mirrored, exactly as the art lane drew it); no word.
        public static ButtonFeel Arrow(Button button, bool next)
        {
            var arrow = Slots.Image("btn-arrow"); if (arrow == null) return null;
            var feel = Begin(button, next ? "arrow-next" : "arrow-previous");
            var image = Piece(feel, "Arrow", arrow, false, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            if (next) image.rectTransform.localScale = new Vector3(-1, 1, 1);
            if (feel.Label != null) feel.Label.enabled = false; // the semantic layer and the web state keep the word
            return feel;
        }
        // The way out of an instrument: gold lettering on a rule. The rule's two lines stretch to the button; its diamond stays whole.
        public static ButtonFeel Rule(Button button, int size = 19)
        {
            var left = Sliced("btn-rule-left", new Vector4(24, 0, 0, 0)); var right = Sliced("btn-rule-right", new Vector4(0, 0, 24, 0)); var centre = Slots.Image("btn-rule-centre");
            if (left == null || right == null || centre == null || EngravedFont == null) return null;
            var feel = Begin(button, "rule");
            Piece(feel, "Rule left", left, true, new Vector2(0, 0), new Vector2(.5f, 0), new Vector2(0, 2), new Vector2(-8, 16));
            Piece(feel, "Rule right", right, true, new Vector2(.5f, 0), new Vector2(1, 0), new Vector2(8, 2), new Vector2(0, 16));
            Piece(feel, "Rule diamond", centre, false, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(-8, 2), new Vector2(8, 16));
            EngravedLabel(feel, size, new Vector2(4, 14), new Vector2(-4, 0));
            return feel;
        }
        // A button in a room: the chat box's see-through dark fill and gold hairline, nine-sliced, and its words in small gold capitals.
        public static ButtonFeel Room(Button button, Font font)
        {
            var feel = Begin(button, "room");
            Piece(feel, "Frame", RoomFrameSprite(), true, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var label = feel.Label; if (label != null)
            {
                label.text = label.text.ToUpperInvariant(); label.font = font; label.fontStyle = FontStyle.Bold; label.fontSize = 13; label.color = RoomGold;
                label.resizeTextForBestFit = true; label.resizeTextMaxSize = 13; label.resizeTextMinSize = 9; label.alignment = TextAnchor.MiddleCenter;
                label.horizontalOverflow = HorizontalWrapMode.Wrap; label.verticalOverflow = VerticalWrapMode.Truncate;
                var t = (RectTransform)label.transform; t.anchorMin = Vector2.zero; t.anchorMax = Vector2.one; t.offsetMin = new Vector2(8, 3); t.offsetMax = new Vector2(-8, -3);
                label.gameObject.AddComponent<Tracking>();
                feel.LabelUp = RoomGold; feel.LabelDown = RoomGoldDown; feel.Upper = true;
            }
            return feel;
        }
        // 3d's 44 px for an icon button (the gear, the mini-menu's button): a clear hit area of 44 x 44 about its centre takes the taps for it
        // (a tap on a child reaches its button); the icon keeps its own drawn size.
        public static Image HitArea(Button button, float size = MinTarget) => HitArea(button, size, size);
        // ... and for any tap target (a room's tappable art): at least minWidth x minHeight about its centre, never smaller than itself
        public static Image HitArea(Component target, float minWidth, float minHeight)
        {
            var parent = (RectTransform)target.transform; var size = parent.rect.size;
            var r = new GameObject("Hit area", typeof(RectTransform)).GetComponent<RectTransform>(); r.SetParent(parent, false);
            r.anchorMin = r.anchorMax = new Vector2(.5f, .5f); r.sizeDelta = new Vector2(Mathf.Max(minWidth, size.x), Mathf.Max(minHeight, size.y)); r.SetAsFirstSibling();
            var hit = r.gameObject.AddComponent<Image>(); hit.color = new Color(1, 1, 1, 0); hit.canvasRenderer.cullTransparentMesh = false; return hit; // a culled mesh takes no taps
        }
        // the target a button offers: its own rect, or its hit area when that is larger
        public static Vector2 Target(Button button) { var r = ((RectTransform)button.transform).rect.size; var hit = button.transform.Find("Hit area") as RectTransform; return hit != null ? Vector2.Max(r, hit.rect.size) : r; }
        // the buttons on screen, for the web state and the checks: "words:look:width x height", the target's size (its hit area when larger)
        public static string[] OnScreen(params Transform[] roots)
        {
            var list = new List<string>();
            foreach (var root in roots) { if (root == null) continue; foreach (var b in root.GetComponentsInChildren<Button>()) {
                    var canvas = b.GetComponentInParent<Canvas>(); if (canvas == null || !canvas.enabled) continue;
                    var label = b.GetComponentInChildren<Text>(); string words = label != null && label.enabled && label.text != "" ? label.text : b.gameObject.name;
                    var kind = KindOf(b); if (kind == "room") words = words.ToUpperInvariant(); // a room's capitals are drawn this frame (ButtonFeel), whatever the words were set to
                    var size = Target(b); list.Add(words.Replace(":", " ") + ":" + kind + ":" + Mathf.RoundToInt(size.x) + "x" + Mathf.RoundToInt(size.y)); } }
            return list.ToArray();
        }
        public static string KindOf(Button button) { var feel = button.GetComponent<ButtonFeel>(); return feel != null ? feel.Kind : "plain"; } // not ??: the Editor's fake null

        // ---- the parts ----
        static ButtonFeel Begin(Button button, string kind)
        {
            var image = button.GetComponent<Image>(); image.color = new Color(1, 1, 1, 0); image.canvasRenderer.cullTransparentMesh = false; // the hit area: see-through, and still hit (a culled mesh takes no taps)
            foreach (var effect in button.GetComponents<Shadow>()) { if (Application.isPlaying) Object.Destroy(effect); else Object.DestroyImmediate(effect); } // an old Outline on the hit area would draw its copies in its own colour
            var look = new GameObject("Look", typeof(RectTransform)).GetComponent<RectTransform>(); look.SetParent(button.transform, false);
            look.anchorMin = Vector2.zero; look.anchorMax = Vector2.one; look.offsetMin = look.offsetMax = Vector2.zero; look.SetAsFirstSibling();
            var feel = button.gameObject.AddComponent<ButtonFeel>(); feel.Kind = kind; feel.Look = look;
            feel.Label = button.GetComponentInChildren<Text>(true); if (feel.Label != null) feel.Label.transform.SetParent(look, true);
            button.transition = Selectable.Transition.None; // the look's own states, below (ButtonFeel)
            return feel;
        }
        static Image Piece(ButtonFeel feel, string name, Sprite sprite, bool slice, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); r.SetParent(feel.Look, false);
            r.anchorMin = anchorMin; r.anchorMax = anchorMax; r.offsetMin = offsetMin; r.offsetMax = offsetMax; r.SetSiblingIndex(feel.Pieces.Count);
            var image = r.gameObject.AddComponent<Image>(); image.sprite = sprite; image.raycastTarget = false;
            if (slice) { image.type = Image.Type.Sliced; image.pixelsPerUnitMultiplier = 2; } // the art is drawn at 2x
            feel.Pieces.Add(image); return image;
        }
        static void EngravedLabel(ButtonFeel feel, int size, Vector2 offsetMin, Vector2 offsetMax)
        {
            var label = feel.Label; if (label == null) return;
            label.font = EngravedFont; label.fontStyle = FontStyle.Normal; label.color = Engraved; label.alignment = TextAnchor.MiddleCenter;
            label.fontSize = size; label.resizeTextForBestFit = true; label.resizeTextMaxSize = size; label.resizeTextMinSize = Mathf.Min(size, 12);
            label.horizontalOverflow = HorizontalWrapMode.Wrap; label.verticalOverflow = VerticalWrapMode.Truncate;
            var t = (RectTransform)label.transform; t.anchorMin = Vector2.zero; t.anchorMax = Vector2.one; t.offsetMin = offsetMin; t.offsetMax = offsetMax;
            // the board's engraving: a dark edge on all four sides (0.9 px) and a soft shadow under it (1.2 px)
            var shadow = label.gameObject.AddComponent<Shadow>(); shadow.effectColor = new Color(0, 0, 0, .6f); shadow.effectDistance = new Vector2(0, -1.2f);
            var edge = label.gameObject.AddComponent<Outline>(); edge.effectColor = new Color(20 / 255f, 13 / 255f, 8 / 255f, .9f); edge.effectDistance = new Vector2(.9f, .9f);
            feel.LabelUp = Engraved; feel.LabelDown = EngravedDown;
        }
        // an imported piece with its nine-slice borders (left, bottom, right, top, in the file's 2x pixels), made once
        static Sprite Sliced(string slot, Vector4 border)
        {
            var source = Slots.Image(slot); if (source == null) return null; string key = slot + "@" + source.GetInstanceID();
            if (sliced.TryGetValue(key, out var made) && made != null) return made;
            made = Sprite.Create(source.texture, source.rect, new Vector2(.5f, .5f), source.pixelsPerUnit, 0, SpriteMeshType.FullRect, border); made.name = slot;
            sliced[key] = made; return made;
        }
        // the art lane's room frame (the board, Oct 2): the CASPAR box's shape at 2x, 64 x 64, radius 10, a 2 px gold hairline, the chat box's see-through fill
        public static Sprite RoomFrameSprite()
        {
            if (roomFrame != null) return roomFrame;
            const int size = 64; const float radius = 10, stroke = 2; var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            Color fill = new Color(.086f, .078f, .094f, .88f), gold = new Color(.84f, .69f, .38f, .96f);
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float px = Mathf.Abs(x + .5f - size / 2f) - (size / 2f - .5f - radius), py = Mathf.Abs(y + .5f - size / 2f) - (size / 2f - .5f - radius);
                float d = new Vector2(Mathf.Max(px, 0), Mathf.Max(py, 0)).magnitude + Mathf.Min(Mathf.Max(px, py), 0) - radius;
                float inside = Mathf.Clamp01(.5f - d), line = Mathf.Clamp01(stroke / 2 + .5f - Mathf.Abs(d + stroke / 2));
                Color c = Color.Lerp(fill, gold, Mathf.Min(1, line / Mathf.Max(inside, .0001f))); c.a = Mathf.Max(fill.a * inside, gold.a * line);
                texture.SetPixel(x, y, c);
            }
            texture.Apply();
            roomFrame = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, new Vector4(14, 14, 14, 14)); roomFrame.name = "room-frame";
            return roomFrame;
        }
    }

    // 3d's states for a button wearing its look. Pressed: every piece darkens (x .78), the words take their darker gold, and the look sinks 1 px.
    // Unavailable: the whole button at half. Keyboard focus keeps today's warm tint; a tap leaves no focus behind (no lingering tint).
    public sealed class ButtonFeel : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public string Kind; public RectTransform Look; public Text Label; public Color LabelUp = Color.white, LabelDown = Color.white; public bool Upper; // Upper: a room's capitals, kept when the words change
        public readonly List<Image> Pieces = new List<Image>();
        static readonly Color Pressed = new Color(.78f, .78f, .78f), Focus = new Color(.85f, .7f, .55f); // the art lane's pressed tint; today's selected tint
        Selectable selectable; CanvasGroup group; bool down; int shown = -1; string seen;
        public bool Down => down;
        void Init() { if (selectable == null) selectable = GetComponent<Selectable>(); if (group == null) { group = GetComponent<CanvasGroup>(); if (group == null) group = gameObject.AddComponent<CanvasGroup>(); } }
        void Awake() => Init();
        public void OnPointerDown(PointerEventData e) { Init(); if (selectable == null || selectable.interactable) down = true; }
        public void OnPointerUp(PointerEventData e) { down = false; if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject) EventSystem.current.SetSelectedGameObject(null); }
        public void OnPointerExit(PointerEventData e) { down = false; }
        void LateUpdate() => Show();
        // the look for the button's state now (the mechanical checks call it directly)
        public void Show()
        {
            Init();
            if (Upper && Label != null && !ReferenceEquals(Label.text, seen)) { var caps = Label.text.ToUpperInvariant(); if (caps != Label.text) Label.text = caps; seen = Label.text; }
            bool available = selectable == null || selectable.interactable; if (!available) down = false;
            bool focused = EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject;
            int state = !available ? 0 : down ? 2 : focused ? 3 : 1; if (state == shown) return; shown = state;
            if (group != null) group.alpha = available ? 1 : .5f;
            var tint = state == 2 ? Pressed : state == 3 ? Focus : Color.white; foreach (var p in Pieces) if (p != null) p.color = tint;
            if (Label != null) Label.color = state == 2 ? LabelDown : LabelUp;
            if (Look != null) Look.anchoredPosition = state == 2 ? new Vector2(0, -1) : Vector2.zero;
        }
    }

    // The room buttons' small capitals are tracked like the board's (2.2 px at 13 px). Unity's Text emits four vertices per visible glyph
    // (none for a space), in order, so each glyph's quad moves along the line by its place in the line; the line stays centred. A label
    // that wraps keeps plain spacing, and so does any mesh that doesn't match its letters (the safe fallback).
    [RequireComponent(typeof(Text))]
    public sealed class Tracking : BaseMeshEffect
    {
        public float Spacing = 2.2f;
        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || Spacing == 0) return; var text = graphic as Text; if (text == null || string.IsNullOrEmpty(text.text) || text.text.IndexOf('\n') >= 0) return;
            var gen = text.cachedTextGenerator; if (gen != null && gen.lineCount > 1) return;
            string s = text.text; var places = new List<int>(); for (int i = 0; i < s.Length; i++) if (!char.IsWhiteSpace(s[i])) places.Add(i);
            if (vh.currentVertCount != places.Count * 4) return;
            float centre = (s.Length - 1) / 2f; UIVertex v = default;
            for (int q = 0; q < places.Count; q++) { float dx = (places[q] - centre) * Spacing; for (int k = 0; k < 4; k++) { vh.PopulateUIVertex(ref v, q * 4 + k); v.position.x += dx; vh.SetUIVertex(v, q * 4 + k); } }
        }
    }
}
