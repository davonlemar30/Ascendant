using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Ascendant.CelestialDial;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ascendant.EraDemo
{
    // The walkable-era demo's view (task 86bcg8az2; owner, Oct 9): a greybox map under an orthographic camera that follows the Keeper,
    // tap-to-move square by square, the shared chat box for talk, a Places list that reaches everyone without walking, and the way back.
    // Opened from DEV Mode over the slice; the slice's own canvases are hidden while it shows, Settings stays on top.
    public sealed class EraDemoView : MonoBehaviour
    {
        public Func<bool> Reduced; public Func<float> Speed; public Action Home; public Action<string> Logged;
        public Action Changed; // the web state republishes (the page's accessible controls follow the demo)
        public EraMap Map { get; private set; }
        public EraWalker Walker { get; private set; }
        public EraTalk Talk { get; private set; }
        public bool Leaving { get; private set; }
        public bool PlacesOpen => places != null && places.gameObject.activeSelf;
        public string Speaker => Talk.Open ? Talk.With.Speaker : "";
        public string LineShown => chat != null && chat.gameObject.activeSelf ? chatLine.text : "";
        // Where Continue and the first choice are drawn (x from the column's centre, top of the box from the column's top), for the page's focus rings.
        public float[] NextBox => new[] { ChatWidth / 2 - 70, nextTop - 17, 116, 34 };
        public float[] ChoiceBox => new[] { 0, nextTop - 17, ChatWidth - 48, 34 };
        float nextTop;
        public string PlateShown => chatSpeaker != null && chat.gameObject.activeSelf ? chatSpeaker.text : "";
        // The name plate spaced like the shipped plates (FitBox.CasparPlate): the legacy Text has no letter spacing.
        public static string Plate(string speaker) => speaker == "CASPAR" ? FitBox.CasparPlate : string.Join("   ", speaker.Split(' ').Select(w => string.Join(" ", w.ToCharArray())));
        public Camera Camera => cam;
        public readonly List<string> Log = new List<string>();

        public const float FadeSeconds = .6f;
        public const float PlacesX = -126, PlacesTop = 26, PlacesWidth = 92, PlacesHeight = 34; // the top left, where the room mini-menu's button sits in the Library
        public const float ChatTop = 588, ChatWidth = 324, ChatMax = 196;
        // Greybox colours: plain and flat, to be replaced by the art; the pre-dawn grade is only hinted.
        static readonly Color VoidColor = new Color(.07f, .07f, .09f), Seam = new Color(.11f, .1f, .12f), Floor = new Color(.52f, .47f, .4f), Stairs = new Color(.66f, .6f, .5f),
            Wall = new Color(.3f, .23f, .19f), Prop = new Color(.42f, .34f, .27f), Person = new Color(.32f, .42f, .6f), CasparColor = new Color(.45f, .2f, .22f),
            Door = new Color(.84f, .69f, .38f), KeeperColor = new Color(.08f, .08f, .09f), Bone = new Color(.93f, .89f, .8f), Gold = new Color(.84f, .69f, .38f), RowColor = new Color(.16f, .15f, .18f);
        static readonly Dictionary<char, (string name, Color color)> Props = new Dictionary<char, (string, Color)>
        {
            ['o'] = ("oven", new Color(.72f, .4f, .2f)), ['w'] = ("well", new Color(.4f, .45f, .5f)), ['s'] = ("salt", new Color(.85f, .82f, .74f)),
            ['m'] = ("camel", new Color(.66f, .52f, .34f)), ['x'] = ("trunk", new Color(.4f, .27f, .16f)), ['n'] = ("niche", new Color(.2f, .16f, .14f)),
        };

        Camera cam; Canvas canvas; RectTransform root; Font font;
        Transform world, keeper, keeperPip; readonly Dictionary<string, Transform> pointViews = new Dictionary<string, Transform>();
        RectTransform chat, places; Text chatSpeaker, chatLine; Button chatNext, placesButton; readonly List<Button> choiceButtons = new List<Button>(); Image fade;
        Sprite square, disc;

        public void Build(Font uiFont)
        {
            font = uiFont;
            Map = EraDemoContent.Build();
            Walker = new EraWalker(Map, Map.Arrival); Walker.PlaceFollower(Map.Point("caspar"));
            Talk = new EraTalk();
            Walker.Logged += Note; Talk.Logged += Note;
            Walker.Arrived += Arrived; Talk.Ended += p => ShowChat();
            square = MakeSquare(); disc = MakeDisc();
            BuildCamera(); BuildWorld(); BuildUi();
            Note("era_demo_opened");
            Place(true);
        }
        void Note(string e) { Log.Add(e); Debug.Log("[EraDemo] " + e); Logged?.Invoke(e); }

        // ---- the world: one unit to a square, x right, y up (a square's row runs down, so its y is negative) ----
        static Vector3 World(float x, float y, float z = 0) => new Vector3(x, -y, z);
        void BuildCamera()
        {
            cam = new GameObject("Era camera").AddComponent<Camera>(); cam.transform.SetParent(transform, false);
            cam.orthographic = true; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = VoidColor; cam.depth = 10; cam.transform.position = new Vector3(0, 0, -10);
        }
        void BuildWorld()
        {
            world = new GameObject("Era world").transform; world.SetParent(transform, false);
            for (int y = 0; y < Map.Height; y++) for (int x = 0; x < Map.Width; x++)
            {
                var c = new Cell(x, y); var kind = Map.Kind(c); char mark = Map.Mark(c);
                if (kind == CellKind.Void) continue;
                bool open = kind == CellKind.Floor || kind == CellKind.Stairs || kind == CellKind.Prop || char.IsLower(mark);
                if (open) { Tile("Seam " + c, World(x, y), Seam, 1, 0); Tile("Square " + c, World(x, y), kind == CellKind.Stairs ? Stairs : Floor, .92f, 1); } // the seams show the grid
                else if (mark != 'D') Tile("Wall " + c, World(x, y), Wall, 1, 1);
                if (kind == CellKind.Prop && Props.TryGetValue(mark, out var prop)) { Tile(prop.name, World(x, y), prop.color, .7f, 2); Word(prop.name, World(x, y), 3, Bone, .9f); }
            }
            foreach (var p in Map.Points)
            {
                var view = new GameObject(p.Label).transform; view.SetParent(world, false); view.position = World(p.At.X, p.At.Y);
                if (p.Portal)
                {
                    Tile("Door frame", p.At, Door, 1, 1, view); Tile("Door", p.At, new Color(.35f, .22f, .12f), .78f, 2, view);
                    Word("back", World(p.At.X, p.At.Y), 3, Bone, .9f, view);
                }
                else
                {
                    var body = Sprite("Body", disc, p.Follower ? CasparColor : Person, .8f, 4, view);
                    Word(p.Follower ? "Caspar" : p.Label.Replace("The ", ""), new Vector3(0, .62f, 0), 6, Bone, .85f, view, true);
                }
                pointViews[p.Id] = view;
            }
            keeper = new GameObject("Keeper").transform; keeper.SetParent(world, false);
            Sprite("Ring", disc, Bone, .82f, 5, keeper); Sprite("Body", disc, KeeperColor, .7f, 6, keeper);
            keeperPip = Sprite("Facing", disc, Bone, .2f, 7, keeper).transform; // a dot on the side he faces
        }
        GameObject Tile(string name, Cell c, Color color, float size, int order, Transform parent) => Tile(name, World(c.X, c.Y), color, size, order, parent, true);
        GameObject Tile(string name, Vector3 at, Color color, float size, int order, Transform parent = null, bool local = false)
        {
            var g = Sprite(name, square, color, size, order, parent ?? world).gameObject; if (local) g.transform.localPosition = Vector3.zero; else g.transform.position = at; return g;
        }
        SpriteRenderer Sprite(string name, Sprite sprite, Color color, float size, int order, Transform parent)
        {
            var r = new GameObject(name).AddComponent<SpriteRenderer>(); r.transform.SetParent(parent, false); r.sprite = sprite; r.color = color; r.sortingOrder = order;
            r.transform.localScale = Vector3.one * size; return r;
        }
        void Word(string text, Vector3 at, int order, Color color, float scale, Transform parent = null, bool local = false)
        {
            var t = new GameObject("Label " + text).AddComponent<TextMesh>(); t.transform.SetParent(parent ?? world, false);
            if (local) t.transform.localPosition = at; else t.transform.position = at;
            t.font = font; t.text = text; t.fontSize = 64; t.characterSize = .052f * scale; t.anchor = TextAnchor.MiddleCenter; t.alignment = TextAlignment.Center; t.color = color;
            var mr = t.GetComponent<MeshRenderer>(); mr.material = font.material; mr.sortingOrder = 20 + order;
        }

        // ---- the UI: the 360 x 800 column, over the world and under Settings ----
        void BuildUi()
        {
            var canvasObject = new GameObject("Era Canvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster)); canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 2; // over the slice (1), under Settings (5)
            // a tap anywhere off the controls goes to the map: the catcher covers the whole screen, under the column
            var catcher = new GameObject("Era taps", typeof(RectTransform)).GetComponent<RectTransform>(); catcher.SetParent(canvasObject.transform, false);
            catcher.anchorMin = Vector2.zero; catcher.anchorMax = Vector2.one; catcher.offsetMin = catcher.offsetMax = Vector2.zero;
            var catchImage = catcher.gameObject.AddComponent<Image>(); catchImage.color = new Color(0, 0, 0, 0); catchImage.canvasRenderer.cullTransparentMesh = false; // an invisible tap area takes taps only with culling off
            catcher.gameObject.AddComponent<TapCatcher>().Tapped = OnTap;
            root = new GameObject("Era Portrait", typeof(RectTransform)).GetComponent<RectTransform>(); root.SetParent(canvasObject.transform, false);
            root.anchorMin = root.anchorMax = root.pivot = new Vector2(.5f, .5f); root.sizeDelta = new Vector2(360, 800);
            var title = Label(root, EraDemoContent.Title, 0, PlacesTop, 150, 20, 12); title.color = new Color(Bone.r, Bone.g, Bone.b, .7f); SafeArea.Top(title);
            placesButton = MakeButton(root, "Places", PlacesX, PlacesTop, PlacesWidth, PlacesHeight, TogglePlaces); placesButton.name = "Places"; SafeArea.Corner(placesButton, -1);
            BuildPlaces(); BuildChat();
            fade = Rect("Fade", canvasObject.transform, 0, 0, 0, 0).gameObject.AddComponent<Image>(); var fr = fade.rectTransform; fr.anchorMin = Vector2.zero; fr.anchorMax = Vector2.one; fr.offsetMin = fr.offsetMax = Vector2.zero;
            fade.color = new Color(0, 0, 0, 0); fade.raycastTarget = false;
        }
        void BuildPlaces()
        {
            var rows = Map.Points.OrderBy(p => p.Portal ? 1 : 0).ToList(); // people first, the way back last
            float height = 62 + rows.Count * 46 + 56;
            places = Rect("Places box", root, 0, 400, 280, height);
            var box = places.gameObject.AddComponent<Image>(); box.sprite = Slots.InstrumentBoxSprite(); box.type = Image.Type.Sliced; box.pixelsPerUnitMultiplier = 2;
            places.gameObject.AddComponent<Button>().transition = Selectable.Transition.None; // a tap on the box itself does not close it
            var heading = Label(places, EraDemoContent.PlacesTitle, 0, 24, 240, 18, 12); heading.color = Gold; heading.fontStyle = FontStyle.Bold;
            float y = 62;
            foreach (var p in rows) { string id = p.Id; var b = MakeButton(places, p.Label, 0, y, 232, 40, () => { ClosePlaces(); GoTo(id); }); b.name = "Place " + id; y += 46; }
            var close = MakeButton(places, "Close", 0, y + 10, 232, 40, ClosePlaces); close.GetComponentInChildren<Text>().color = Gold;
            places.gameObject.SetActive(false);
        }
        void BuildChat()
        {
            chat = Rect("Era chat", root, 0, ChatTop + ChatMax / 2, ChatWidth, ChatMax);
            var panel = chat.gameObject.AddComponent<Image>(); panel.color = new Color(.045f, .025f, .03f, .92f);
            chat.gameObject.AddComponent<Button>().transition = Selectable.Transition.None; // a tap on the box itself continues (below), not the map
            chat.GetComponent<Button>().onClick.AddListener(() => Next());
            if (Slots.DressChatBox(panel, null, font)) chatSpeaker = chat.GetComponentsInChildren<Text>(true).FirstOrDefault(t => t.gameObject.name == "CASPAR");
            chatLine = Label(chat, "", 0, 0, ChatWidth - 48, 80, 15); chatLine.alignment = TextAnchor.MiddleLeft; chatLine.horizontalOverflow = HorizontalWrapMode.Wrap; chatLine.verticalOverflow = VerticalWrapMode.Overflow;
            chatNext = MakeButton(chat, "Continue", ChatWidth / 2 - 70, 0, 116, 34, () => Next()); chatNext.name = "Continue";
            for (int i = 0; i < 2; i++) { int k = i; var b = MakeButton(chat, "", 0, 0, ChatWidth - 48, 34, () => Choose(k)); b.name = "Choice " + (i + 1); choiceButtons.Add(b); }
            chat.gameObject.SetActive(false);
        }

        // ---- play ----
        void OnTap(Vector2 screen)
        {
            if (Leaving) return;
            if (PlacesOpen) { ClosePlaces(); return; }
            if (Talk.Open) { Next(); return; } // a tap anywhere continues the talk, as the box's Continue does
            var w = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 10));
            Tap(new Cell(Mathf.RoundToInt(w.x), Mathf.RoundToInt(-w.y)));
        }
        // The taps and the Places list do the same thing through the same walk (Q06 phase 2, decision 5).
        public bool Tap(Cell c) { if (Leaving || Talk.Open) return false; bool ok = Walker.Tap(c); if (ok && Reduced != null && Reduced()) Walker.Jump(); Changed?.Invoke(); return ok; }
        public bool GoTo(string id) { if (Leaving || Talk.Open) return false; bool ok = Walker.GoTo(id); if (ok && Reduced != null && Reduced()) Walker.Jump(); Changed?.Invoke(); return ok; }
        void Arrived(string id)
        {
            var p = Map.Point(id);
            if (p != null && p.Portal) StartCoroutine(GoHome());
            else if (p != null) { ClosePlaces(); if (Talk.Begin(p)) ShowChat(); } // a talk closes Places, so the first tap continues it (Jeffrey, #150 N3)
            Changed?.Invoke();
        }
        public bool Next() { if (!Talk.Open || Talk.Waiting) return false; Talk.Next(); ShowChat(); return true; }
        public bool Choose(int i) { if (!Talk.Choose(i)) return false; ShowChat(); return true; }
        public void TogglePlaces() { if (PlacesOpen) ClosePlaces(); else if (!Talk.Open && !Leaving) { places.gameObject.SetActive(true); Changed?.Invoke(); } }
        public void ClosePlaces() { if (places == null || !places.gameObject.activeSelf) return; places.gameObject.SetActive(false); Changed?.Invoke(); }
        void ShowChat()
        {
            var line = Talk.Current; chat.gameObject.SetActive(line != null); placesButton.interactable = line == null && !Leaving;
            Changed?.Invoke();
            if (line == null) return;
            if (chatSpeaker != null) chatSpeaker.text = Plate(Talk.With.Speaker);
            chatLine.text = line.Text;
            int choices = line.Choices.Length; chatNext.gameObject.SetActive(choices == 0);
            for (int i = 0; i < choiceButtons.Count; i++) { bool shown = i < choices; choiceButtons[i].gameObject.SetActive(shown); if (shown) choiceButtons[i].GetComponentInChildren<Text>().text = line.Choices[i]; }
            // the box grows to its text, from the same top: the name plate, the line, then Continue or the choices
            var settings = chatLine.GetGenerationSettings(new Vector2(chatLine.rectTransform.rect.width, 0)); settings.resizeTextForBestFit = false;
            float text = Mathf.Max(20, chatLine.cachedTextGeneratorForLayout.GetPreferredHeight(chatLine.text, settings) / chatLine.pixelsPerUnit);
            float buttons = choices == 0 ? 44 : choices * 40 + 4, height = Mathf.Min(ChatMax + 60, 26 + text + 10 + buttons + 22);
            chat.sizeDelta = new Vector2(ChatWidth, height); chat.anchoredPosition = new Vector2(0, -(800 - 18 - height / 2)); // its bottom 18 above the column's
            chatLine.rectTransform.anchoredPosition = new Vector2(0, -(26 + text / 2)); chatLine.rectTransform.sizeDelta = new Vector2(ChatWidth - 48, text);
            float by = 26 + text + 10; nextTop = 800 - 18 - height + by + 17; // a button's centre, from the column's top
            if (choices == 0) chatNext.GetComponent<RectTransform>().anchoredPosition = new Vector2(ChatWidth / 2 - 70, -(by + 17));
            for (int i = 0; i < choices; i++) choiceButtons[i].GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -(by + 17 + i * 40));
        }
        IEnumerator GoHome()
        {
            Leaving = true; ClosePlaces(); placesButton.interactable = false; fade.raycastTarget = true; Note("era_demo_left"); Changed?.Invoke();
            float t = 0, seconds = Reduced != null && Reduced() ? 0 : FadeSeconds; // reduced motion: an instant fade (Q06 lock 4)
            while (t < seconds) { t += Time.unscaledDeltaTime; fade.color = new Color(0, 0, 0, Mathf.Clamp01(t / seconds)); yield return null; }
            fade.color = Color.black;
            Home?.Invoke();
        }

        void Update()
        {
            if (Walker == null) return;
            if (canvas.pixelRect.width >= 1) canvas.scaleFactor = Mathf.Min(canvas.pixelRect.width / 360f, canvas.pixelRect.height / 800f);
            if (Speed != null && !Walker.Walking) Walker.SetSpeed(Speed());
            if (Walker.Walking) { if (Reduced != null && Reduced()) Walker.Jump(); else Walker.Tick(Time.unscaledDeltaTime); }
            Place(false);
        }
        // The Keeper at his place, a small step bob, the facing dot; Caspar glides one square behind; the camera follows, held to the map.
        void Place(bool snap)
        {
            bool still = snap || (Reduced != null && Reduced());
            float bob = still ? 0 : Walker.StepFrame == 0 ? 0 : (Walker.StepFrame == 1 ? .05f : 0);
            keeper.position = World(Walker.X, Walker.Y - bob);
            var f = Walker.Facing; keeperPip.localPosition = f == Facing.Up ? new Vector3(0, .26f) : f == Facing.Down ? new Vector3(0, -.26f) : f == Facing.Left ? new Vector3(-.26f, 0) : new Vector3(.26f, 0);
            foreach (var p in Map.Points.Where(p => p.Follower))
            {
                var view = pointViews[p.Id]; var target = World(p.At.X, p.At.Y);
                view.position = still ? target : Vector3.MoveTowards(view.position, target, Walker.Speed / EraMap.SquarePx * 1.1f * Time.unscaledDeltaTime);
            }
            if (cam.pixelRect.height < 1) return;
            float scale = Mathf.Min(cam.pixelRect.width / 360f, cam.pixelRect.height / 800f), size = cam.pixelRect.height / scale / 2 / EraMap.SquarePx; // one square is 40 layout px, as on the column
            cam.orthographicSize = size; float half = size * cam.aspect;
            float x = Walker.X, y = -Walker.Y, minX = -.5f + half, maxX = Map.Width - .5f - half, minY = -(Map.Height - .5f) + size, maxY = .5f - size;
            x = minX > maxX ? (Map.Width - 1) / 2f : Mathf.Clamp(x, minX, maxX); y = minY > maxY ? -(Map.Height - 1) / 2f : Mathf.Clamp(y, minY, maxY);
            cam.transform.position = new Vector3(x, y, -10);
        }
        // Where a square is on the screen, in layout units from the column's top left (for checks that tap the map).
        public Vector2 ScreenOf(Cell c) => cam.WorldToScreenPoint(World(c.X, c.Y));

        // ---- building helpers (the slice's 360 x 800 convention: x from the parent's centre, top = the centre's distance below its top edge) ----
        static RectTransform Rect(string name, Transform parent, float x, float top, float width, float height)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1); rect.pivot = new Vector2(.5f, .5f); rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(x, -top); return rect;
        }
        Button MakeButton(Transform parent, string text, float x, float top, float width, float height, UnityEngine.Events.UnityAction action)
        {
            var rect = Rect(text == "" ? "Button" : text, parent, x, top, width, height);
            var image = rect.gameObject.AddComponent<Image>(); image.color = RowColor; image.canvasRenderer.cullTransparentMesh = false;
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.onClick.AddListener(action);
            var label = Label(rect, text, 0, height / 2, width - 12, height, 14); label.raycastTarget = false;
            return button;
        }
        Text Label(Transform parent, string text, float x, float top, float width, float height, int size)
        {
            var label = Rect("Label", parent, x, top, width, height).gameObject.AddComponent<Text>();
            label.font = font; label.text = text; label.fontSize = size; label.color = Bone; label.alignment = TextAnchor.MiddleCenter; label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Overflow; return label;
        }
        static Sprite MakeSquare()
        {
            var t = new Texture2D(4, 4, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point }; var px = new Color[16]; for (int i = 0; i < 16; i++) px[i] = Color.white; t.SetPixels(px); t.Apply();
            return UnityEngine.Sprite.Create(t, new UnityEngine.Rect(0, 0, 4, 4), new Vector2(.5f, .5f), 4);
        }
        static Sprite MakeDisc()
        {
            const int n = 64; var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++) { float d = new Vector2(x + .5f - n / 2f, y + .5f - n / 2f).magnitude; t.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(n / 2f - d))); }
            t.Apply(); return UnityEngine.Sprite.Create(t, new UnityEngine.Rect(0, 0, n, n), new Vector2(.5f, .5f), n);
        }
    }

    // A tap on the map (the screen under the column's controls), in screen pixels.
    public sealed class TapCatcher : MonoBehaviour, IPointerClickHandler
    {
        public Action<Vector2> Tapped;
        public void OnPointerClick(PointerEventData e) => Tapped?.Invoke(e.position);
    }
}
