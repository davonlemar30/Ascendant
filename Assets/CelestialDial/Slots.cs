using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Ascendant.CelestialDial
{
    // Build E: named doors for art and sound (Sept 13 scope ruling, decision 4: presentation wiring only).
    // An art slot is a PNG named after it in Assets/CelestialDial/Resources/Art; a sound slot a WAV in .../Resources/Audio.
    // A file present is drawn in the placeholder's rect or played on its action; a file missing leaves the grey box and the
    // silence. The shipped test set lives in the "test" subfolders and is used only when asked for: ?art=test on the Web,
    // -artSet test in batch, the Editor menu in play. The manifest below is the list of slot names; add a slot when a new object appears.
    public static class Slots
    {
        public sealed class ArtSlot
        {
            public readonly string Name, Where; public readonly int Width, Height, MaxSize;
            public ArtSlot(string name, int width, int height, int maxSize, string where) { Name = name; Width = width; Height = height; MaxSize = maxSize; Where = where; }
        }
        public sealed class SoundSlot
        {
            public readonly string Name, When;
            public SoundSlot(string name, string when) { Name = name; When = when; }
        }
        // Width and height are the placeholder's rect on the 360 x 800 reference layout; MaxSize caps the imported texture.
        public static readonly ArtSlot[] Art =
        {
            new ArtSlot("atrium", 360, 800, 2048, "the Grand Atrium, behind the opening, the return, and the room"),
            new ArtSlot("wing", 360, 800, 2048, "the Zodiac Wing's shell (Build M): the restored room's architecture only; the kit pieces, grime, and light layer over it"),
            new ArtSlot("chamber", 360, 800, 2048, "the Crystal Book Chamber, first visit and room"),
            new ArtSlot("caspar", 64, 112, 256, "Caspar standing in the Atrium"),
            new ArtSlot("keeper-idle", 44, 100, 256, "the Keeper standing; faces the way it last walked"),
            new ArtSlot("keeper-walk", 44, 100, 256, "the Keeper mid-step; alternates with idle every step while walking"),
            new ArtSlot("dial-face", 332, 332, 1024, "the Dial's face under the twelve seats; also the Dial seen from the Wing room (200 x 200)"),
            new ArtSlot("seat", 52, 52, 256, "one seat tile, twelve times; dimmed while dormant or unlit"),
            new ArtSlot("bracket", 58, 58, 256, "the fixed focus bracket over the framed seat"),
            new ArtSlot("floor-markings", 320, 320, 1024, "the faded floor pattern under the wheel; brightens as the wheel wakes"),
            new ArtSlot("shelf", 50, 36, 256, "the collapsed bookshelf, in the Wing room and beside the wheel"),
            new ArtSlot("chair", 44, 36, 256, "the covered chair beside the wheel"),
            new ArtSlot("table", 60, 30, 256, "the table with its board of twelve, in the Wing room"),
            new ArtSlot("shelf-book", 10, 26, 64, "a book on a shelf: three on the Wing's, three that return to the Atrium's at Stage 4"),
            new ArtSlot("book-cover", 140, 140, 512, "the book on the shelf, closed"),
            new ArtSlot("book-page", 140, 140, 512, "the book's open page behind each symbol"),
            new ArtSlot("journal-page", 360, 800, 2048, "the journal's open page, behind its entries (Build F)"),
            new ArtSlot("journal-cover", 60, 60, 256, "the journal's cover, at the head of its page (Build F)"),
            new ArtSlot("shelves", 60, 180, 512, "the Atrium's shelves, mostly empty (60 x 120 in the room)"),
            new ArtSlot("furniture-covered", 120, 70, 512, "the covered furniture of the opening"),
            new ArtSlot("desk", 70, 30, 256, "the desk, uncovered, in the Atrium room"),
            new ArtSlot("lamp", 8, 22, 64, "a wall lamp, four in the Atrium; dark until its stage"),
            new ArtSlot("candle", 8, 20, 64, "a candle: the opening's one, the Wing's, the Chamber's nine; dark until lit"),
            new ArtSlot("door-open", 64, 128, 512, "an open door leaf filling a painted arch: the Wing's (72 x 128) and the Chamber's (62 x 128) in the Atrium, the doorways back (36 x 140 in the Wing, 30 x 124 in the Chamber)"),
            new ArtSlot("door-sealed", 64, 128, 512, "the sealed door leaf filling the Atrium's left arch (62 x 128)"),
            new ArtSlot("mechanism", 110, 110, 512, "the Chamber's old mechanism; turns one degree at the first Key"),
            new ArtSlot("crystal-book", 34, 70, 256, "one Crystal Book, seven times; brightens when it opens"),
            new ArtSlot("crystal-page", 26, 58, 256, "the page that rises from an open Book"),
            new ArtSlot("lock", 7, 7, 32, "one lock, three per Book; lights when filled"),
            new ArtSlot("keeper-key", 84, 40, 256, "the Keeper Key rising from the Dial"),
            // Build H (the Sept 17 lighting decision, Option C): one transparent golden-hour overlay per room, over the dormant background and
            // under everything else, faded by the Atrium stage from nothing at Stage 1 to full at Stages 5–6.
            new ArtSlot("atrium-light", 360, 800, 2048, "the Atrium's golden-hour light: shafts through the arches, glowing dust, warmth on the stone; over the background, faded by stage"),
            new ArtSlot("wing-light", 360, 800, 2048, "the Zodiac Wing room's golden-hour light, over the background, faded by stage"),
            new ArtSlot("atrium-grime", 360, 800, 2048, "the Atrium's grime (Build N): dust, cobwebs, a cold tint over the shell; fades out by the Atrium's stage"),
            new ArtSlot("akit-banner-restored", 34, 80, 1024, "the Atrium kit (Build N): banner-restored; drawn at half the file's size from SliceView.AtriumKit / AddDoor"),
            new ArtSlot("akit-banner-worn", 40, 88, 1024, "the Atrium kit (Build N): banner-worn; drawn at half the file's size from SliceView.AtriumKit / AddDoor"),
            new ArtSlot("akit-bench-restored", 96, 52, 1024, "the Atrium kit (Build N): bench-restored; drawn at half the file's size from SliceView.AtriumKit / AddDoor"),
            new ArtSlot("akit-bench-worn", 104, 40, 1024, "the Atrium kit (Build N): bench-worn; drawn at half the file's size from SliceView.AtriumKit / AddDoor"),
            new ArtSlot("akit-bust-restored", 28, 76, 1024, "the Atrium kit (Build N): bust-restored; drawn at half the file's size from SliceView.AtriumKit / AddDoor"),
            new ArtSlot("akit-bust-worn", 44, 68, 1024, "the Atrium kit (Build N): bust-worn; drawn at half the file's size from SliceView.AtriumKit / AddDoor"),
            new ArtSlot("akit-candlestand-restored", 28, 90, 1024, "the Atrium kit (Build N): candlestand-restored; drawn at half the file's size from SliceView.AtriumKit / AddDoor"),
            new ArtSlot("akit-candlestand-worn", 36, 88, 1024, "the Atrium kit (Build N): candlestand-worn; drawn at half the file's size from SliceView.AtriumKit / AddDoor"),
            new ArtSlot("akit-chandelier-restored", 80, 90, 1024, "the Atrium kit (Build N): chandelier-restored; drawn at half the file's size from SliceView.AtriumKit / AddDoor"),
            new ArtSlot("akit-chandelier-worn", 80, 94, 1024, "the Atrium kit (Build N): chandelier-worn; drawn at half the file's size from SliceView.AtriumKit / AddDoor"),
            new ArtSlot("akit-chart-restored", 34, 46, 1024, "the Atrium kit (Build N): chart-restored; drawn at half the file's size from SliceView.AtriumKit / AddDoor"),
            new ArtSlot("akit-chart-worn", 38, 48, 1024, "the Atrium kit (Build N): chart-worn; drawn at half the file's size from SliceView.AtriumKit / AddDoor"),
            new ArtSlot("akit-desk-restored", 146, 96, 1024, "the Atrium kit (Build N): desk-restored; drawn at half the file's size from SliceView.AtriumKit / AddDoor"),
            new ArtSlot("akit-desk-worn", 142, 78, 1024, "the Atrium kit (Build N): desk-worn; drawn at half the file's size from SliceView.AtriumKit / AddDoor"),
            new ArtSlot("akit-door-closed", 70, 136, 1024, "the Atrium kit (Build N): door-closed; drawn at half the file's size from SliceView.AtriumKit / AddDoor"),
            new ArtSlot("akit-door-locked", 70, 136, 1024, "the Atrium kit (Build N): door-locked; drawn at half the file's size from SliceView.AtriumKit / AddDoor"),
            new ArtSlot("akit-door-open", 70, 136, 1024, "the Atrium kit (Build N): door-open; drawn at half the file's size from SliceView.AtriumKit / AddDoor"),
            new ArtSlot("akit-lamp-restored", 20, 40, 1024, "the Atrium kit (Build N): lamp-restored; drawn at half the file's size from SliceView.AtriumKit / AddDoor"),
            new ArtSlot("akit-lamp-worn", 20, 42, 1024, "the Atrium kit (Build N): lamp-worn; drawn at half the file's size from SliceView.AtriumKit / AddDoor"),
            new ArtSlot("akit-plant-restored", 36, 46, 1024, "the Atrium kit (Build N): plant-restored; drawn at half the file's size from SliceView.AtriumKit / AddDoor"),
            new ArtSlot("akit-plant-worn", 34, 48, 1024, "the Atrium kit (Build N): plant-worn; drawn at half the file's size from SliceView.AtriumKit / AddDoor"),
            new ArtSlot("akit-plate-clean", 64, 28, 1024, "the Atrium kit (Build N): plate-clean; drawn at half the file's size from SliceView.AtriumKit / AddDoor"),
            new ArtSlot("akit-plate-locked", 64, 28, 1024, "the Atrium kit (Build N): plate-locked; drawn at half the file's size from SliceView.AtriumKit / AddDoor"),
            new ArtSlot("akit-rug-restored", 316, 420, 1024, "the Atrium kit (Build N): rug-restored; drawn at half the file's size from SliceView.AtriumKit / AddDoor"),
            new ArtSlot("akit-rug-worn", 200, 108, 1024, "the Atrium kit (Build N): rug-worn; drawn at half the file's size from SliceView.AtriumKit / AddDoor"),
            new ArtSlot("akit-shelf-restored", 80, 300, 1024, "the Atrium kit (Build N): shelf-restored; drawn at half the file's size from SliceView.AtriumKit / AddDoor"),
            new ArtSlot("akit-shelf-worn", 82, 300, 1024, "the Atrium kit (Build N): shelf-worn; drawn at half the file's size from SliceView.AtriumKit / AddDoor"),
            new ArtSlot("wing-grime", 360, 800, 2048, "the Wing's grime (Build M): dust, cobwebs, cracks, a cold dark tint over the shell; fades out as the Keys restore the room"),
            new ArtSlot("kit-window-worn", 76, 192, 1024, "the Wing kit (Build M): the window, worn; drawn at the file's half size, bottom-centred on its placement in SliceView.WingKit"),
            new ArtSlot("kit-window-restored", 76, 192, 1024, "the Wing kit (Build M): the window, restored; drawn at the file's half size, bottom-centred on its placement in SliceView.WingKit"),
            new ArtSlot("kit-carpet-worn", 352, 110, 1024, "the Wing kit (Build M): the carpet, worn; drawn at the file's half size, bottom-centred on its placement in SliceView.WingKit"),
            new ArtSlot("kit-carpet-restored", 352, 110, 1024, "the Wing kit (Build M): the carpet, restored; drawn at the file's half size, bottom-centred on its placement in SliceView.WingKit"),
            new ArtSlot("kit-chandelier-worn", 84, 106, 1024, "the Wing kit (Build M): the chandelier, worn; drawn at the file's half size, bottom-centred on its placement in SliceView.WingKit"),
            new ArtSlot("kit-chandelier-restored", 84, 106, 1024, "the Wing kit (Build M): the chandelier, restored; drawn at the file's half size, bottom-centred on its placement in SliceView.WingKit"),
            new ArtSlot("kit-banner-worn", 26, 130, 1024, "the Wing kit (Build M): the banner, worn; drawn at the file's half size, bottom-centred on its placement in SliceView.WingKit"),
            new ArtSlot("kit-banner-restored", 26, 130, 1024, "the Wing kit (Build M): the banner, restored; drawn at the file's half size, bottom-centred on its placement in SliceView.WingKit"),
            new ArtSlot("kit-orrery-worn", 80, 106, 1024, "the Wing kit (Build M): the orrery, worn; drawn at the file's half size, bottom-centred on its placement in SliceView.WingKit"),
            new ArtSlot("kit-orrery-restored", 80, 106, 1024, "the Wing kit (Build M): the orrery, restored; drawn at the file's half size, bottom-centred on its placement in SliceView.WingKit"),
            new ArtSlot("kit-armillary-worn", 66, 80, 1024, "the Wing kit (Build M): the armillary, worn; drawn at the file's half size, bottom-centred on its placement in SliceView.WingKit"),
            new ArtSlot("kit-armillary-restored", 66, 80, 1024, "the Wing kit (Build M): the armillary, restored; drawn at the file's half size, bottom-centred on its placement in SliceView.WingKit"),
            new ArtSlot("kit-lectern-worn", 76, 90, 1024, "the Wing kit (Build M): the lectern, worn; drawn at the file's half size, bottom-centred on its placement in SliceView.WingKit"),
            new ArtSlot("kit-lectern-restored", 76, 90, 1024, "the Wing kit (Build M): the lectern, restored; drawn at the file's half size, bottom-centred on its placement in SliceView.WingKit"),
            new ArtSlot("kit-globe-worn", 34, 50, 1024, "the Wing kit (Build M): the globe, worn; drawn at the file's half size, bottom-centred on its placement in SliceView.WingKit"),
            new ArtSlot("kit-globe-restored", 34, 50, 1024, "the Wing kit (Build M): the globe, restored; drawn at the file's half size, bottom-centred on its placement in SliceView.WingKit"),
            new ArtSlot("kit-shelf-worn", 96, 290, 1024, "the Wing kit (Build M): the shelf, worn; drawn at the file's half size, bottom-centred on its placement in SliceView.WingKit"),
            new ArtSlot("kit-shelf-restored", 96, 290, 1024, "the Wing kit (Build M): the shelf, restored; drawn at the file's half size, bottom-centred on its placement in SliceView.WingKit"),
            new ArtSlot("kit-dial-worn", 188, 206, 1024, "the Wing kit (Build M): the dial, worn; drawn at the file's half size, bottom-centred on its placement in SliceView.WingKit"),
            new ArtSlot("kit-dial-restored", 188, 206, 1024, "the Wing kit (Build M): the dial, restored; drawn at the file's half size, bottom-centred on its placement in SliceView.WingKit"),
            new ArtSlot("kit-telescope-worn", 66, 116, 1024, "the Wing kit (Build M): the telescope, worn; drawn at the file's half size, bottom-centred on its placement in SliceView.WingKit"),
            new ArtSlot("kit-telescope-restored", 66, 116, 1024, "the Wing kit (Build M): the telescope, restored; drawn at the file's half size, bottom-centred on its placement in SliceView.WingKit"),
            new ArtSlot("kit-table-worn", 108, 90, 1024, "the Wing kit (Build M): the table, worn; drawn at the file's half size, bottom-centred on its placement in SliceView.WingKit"),
            new ArtSlot("kit-table-restored", 108, 90, 1024, "the Wing kit (Build M): the table, restored; drawn at the file's half size, bottom-centred on its placement in SliceView.WingKit"),
            new ArtSlot("kit-candles-worn", 16, 40, 1024, "the Wing kit (Build M): the candles, worn; drawn at the file's half size, bottom-centred on its placement in SliceView.WingKit"),
            new ArtSlot("kit-candles-restored", 16, 40, 1024, "the Wing kit (Build M): the candles, restored; drawn at the file's half size, bottom-centred on its placement in SliceView.WingKit"),
            new ArtSlot("kit-books-worn", 68, 46, 1024, "the Wing kit (Build M): the books, worn; drawn at the file's half size, bottom-centred on its placement in SliceView.WingKit"),
            new ArtSlot("kit-books-restored", 68, 46, 1024, "the Wing kit (Build M): the books, restored; drawn at the file's half size, bottom-centred on its placement in SliceView.WingKit"),
            new ArtSlot("kit-chair-worn", 50, 82, 1024, "the Wing kit (Build M): the chair, worn; drawn at the file's half size, bottom-centred on its placement in SliceView.WingKit"),
            new ArtSlot("kit-chair-restored", 50, 82, 1024, "the Wing kit (Build M): the chair, restored; drawn at the file's half size, bottom-centred on its placement in SliceView.WingKit"),
            new ArtSlot("kit-plate-worn", 64, 28, 1024, "the Wing kit (Build M): the plate, worn; drawn at the file's half size, bottom-centred on its placement in SliceView.WingKit"),
            new ArtSlot("kit-plate-restored", 64, 28, 1024, "the Wing kit (Build M): the plate, restored; drawn at the file's half size, bottom-centred on its placement in SliceView.WingKit"),
            new ArtSlot("chamber-light", 360, 800, 2048, "the Crystal Book Chamber's golden-hour light, over the background, faded by stage"),
        };
        public static readonly SoundSlot[] Sounds =
        {
            new SoundSlot("step", "one wheel detent, by button, drag, keyboard, or a count beat; a tile or cell picked on the table"),
            new SoundSlot("seal", "a Seal or a tap answer accepted"),
            new SoundSlot("miss", "a Seal or a tap answer rejected"),
            new SoundSlot("key", "a Key earned; a Key spent on a lock"),
            new SoundSlot("page", "Caspar's page turned; the book opened or closed; a Book's page rising"),
            new SoundSlot("door", "a doorway crossed"),
            new SoundSlot("ambient", "the room loop, from the first screen"),
        };
        public const string TestSet = "test";
        public static ArtSlot Find(string name) => Art.FirstOrDefault(a => a.Name == name);
        public static SoundSlot FindSound(string name) => Sounds.FirstOrDefault(s => s.Name == name);

        // ---- which set, and whether the style page was asked for ----
        static string requestedSet; static bool? requestedStyle; static string resolvedSet; static bool resolvedStyle, resolved;
        // A test fixture (or the Editor menu) chooses the set and the page in play; null clears the request.
        public static void Request(string set, bool? style)
        {
            requestedSet = set; requestedStyle = style; resolved = false; images.Clear(); clips.Clear();
        }
        public static string Set { get { Resolve(); return requestedSet ?? resolvedSet; } }
        public static bool StyleRequested { get { Resolve(); return requestedStyle ?? resolvedStyle; } }
        static void Resolve()
        {
            if (resolved) return; resolved = true;
            ParseQuery(Application.absoluteURL, out resolvedSet, out resolvedStyle);
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-artSet" && i + 1 < args.Length) resolvedSet = args[i + 1];
                if (args[i] == "-style") resolvedStyle = true;
            }
#if UNITY_EDITOR
            if (!Application.isBatchMode)
            {
                string menuSet = UnityEditor.SessionState.GetString("AscendantArtSet", ""); if (menuSet != "") resolvedSet = menuSet;
                if (UnityEditor.SessionState.GetBool("AscendantStylePage", false)) resolvedStyle = true;
            }
#endif
        }
        // The page's URL chooses the set (?art=test) and the style page (?style, or ?style=test for both). Pure, for the checks.
        public static void ParseQuery(string url, out string set, out bool style)
        {
            set = ""; style = false;
            if (string.IsNullOrEmpty(url)) return;
            int mark = url.IndexOf('?'); if (mark < 0) return;
            string query = url.Substring(mark + 1); int hash = query.IndexOf('#'); if (hash >= 0) query = query.Substring(0, hash);
            foreach (var pair in query.Split('&'))
            {
                int eq = pair.IndexOf('='); string key = eq < 0 ? pair : pair.Substring(0, eq), value = eq < 0 ? "" : pair.Substring(eq + 1);
                if (key == "art") set = Clean(value);
                else if (key == "style") { style = true; if (value != "") set = Clean(value); }
            }
        }
        static string Clean(string value) => new string(value.ToLowerInvariant().Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());

        // ---- loading: one lookup per slot per set; null means no file ----
        static readonly Dictionary<string, Sprite> images = new Dictionary<string, Sprite>();
        static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        static string PathOf(string kind, string slot) => kind + "/" + (string.IsNullOrEmpty(Set) ? "" : Set + "/") + slot;
        public static Sprite Image(string slot)
        {
            string key = Set + "/" + slot; if (images.TryGetValue(key, out var cached)) return cached;
            var sprite = Resources.Load<Sprite>(PathOf("Art", slot));
            if (sprite == null) { var texture = Resources.Load<Texture2D>(PathOf("Art", slot)); if (texture != null) sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect); }
            images[key] = sprite; return sprite;
        }
        public static AudioClip Clip(string slot)
        {
            string key = Set + "/" + slot; if (clips.TryGetValue(key, out var cached)) return cached;
            var clip = Resources.Load<AudioClip>(PathOf("Audio", slot)); clips[key] = clip; return clip;
        }
        public static string Source(string slot) => Image(slot) != null ? (string.IsNullOrEmpty(Set) ? "file" : Set + " set") : "placeholder";
        public static string SoundSource(string slot) => Clip(slot) != null ? (string.IsNullOrEmpty(Set) ? "file" : Set + " set") : "silent";
        public static int ArtFiles => Art.Count(a => Image(a.Name) != null);
        public static int SoundFiles => Sounds.Count(s => Clip(s.Name) != null);

        // ---- dressing: a placeholder Image takes its slot's file when there is one ----
        public sealed class Dressing { public string Slot; public Image Image; }
        public static readonly List<Dressing> Dressed = new List<Dressing>(); // test evidence: which placeholders took a file
        public static bool Dress(Image image, string slot)
        {
            var sprite = Image(slot);
            Dressed.RemoveAll(d => d.Image == null); Dressed.Add(new Dressing { Slot = slot, Image = image });
            if (sprite == null) return false;
            image.sprite = sprite; image.type = UnityEngine.UI.Image.Type.Simple; image.preserveAspect = false; image.color = Color.white;
            return true;
        }
        public static int DressedCount { get { Dressed.RemoveAll(d => d.Image == null); return Dressed.Count(d => d.Image.sprite != null); } }
        public static bool IsDressed(string slot) { Dressed.RemoveAll(d => d.Image == null); return Dressed.Any(d => d.Slot == slot && d.Image.sprite != null && d.Image.isActiveAndEnabled); }
        // A state color: the placeholder's own color, or the file at a brightness (the placeholder's alpha carries over).
        public static void Paint(Image image, Color placeholder, float brightness)
        {
            if (image == null) return;
            image.color = image.sprite != null ? new Color(brightness, brightness, brightness, placeholder.a) : placeholder;
        }
    }
}
