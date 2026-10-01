using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace Ascendant.CelestialDial
{
    // Build R (owner, Sept 29): the instrument screens' Caspar box fits its line. Its top stays pinned; whenever the line changes it
    // takes the height the line needs (Slots.InstrumentBoxHeight), at most its old height, past which the text's best fit shrinks it.
    // Build S (owner, APK playtest, Sept 29): a long line is cut at its sentences into pages of at most PageLines lines, and a small
    // Continue inside the box turns them; the wheel stays live meanwhile. The element words take their colours on the page shown.
    // The code keeps writing the whole line to Line.text; Source keeps it, so web state and the checks still read the whole line.
    public sealed class FitBox : MonoBehaviour
    {
        public const int PageLines = 3;
        public Text Line; public float Max; public Button More;
        public Text Name; // Build V: the speaker's plate
        public const string CasparPlate = "C A S P A R", DialPlate = "T H E   C E L E S T I A L   D I A L"; // spaced: the legacy Text has no letter spacing
        public Image Panel, Rule, Eye; public float NameX; // Build Z: the box's frame, the rule under the plate, and the Dial's eye mark
        static readonly Color Gold = new Color(.84f, .69f, .38f);
        bool? speakerShown;
        // Build V: the plate names the speaker; Build Z (owner, Sept 30): the Dial's voice is sea blue with its eye mark, Caspar's gold.
        public void SetSpeaker(bool dial)
        {
            if (Name == null) return; string plate = dial ? DialPlate : CasparPlate; if (Name.text != plate) Name.text = plate;
            if (speakerShown == dial) return; speakerShown = dial;
            Name.color = dial ? Slots.DialVoice : Gold;
            if (Eye != null) { Eye.gameObject.SetActive(dial); Name.rectTransform.anchoredPosition = new Vector2(NameX + (dial ? 21 : 0), Name.rectTransform.anchoredPosition.y); }
            if (Rule != null) Rule.color = dial ? new Color(Slots.DialVoice.r, Slots.DialVoice.g, Slots.DialVoice.b, .6f) : new Color(Gold.r, Gold.g, Gold.b, .55f);
            if (Panel != null) Panel.sprite = Slots.InstrumentBoxSprite(dial);
        }
        public bool DialVoiceShown => speakerShown == true; // for the web state
        public string Source { get; private set; } = "";
        public int Page { get; private set; }
        public int Pages => pages.Count;
        public string Shown => pages.Count > 0 ? Colour(pages[Mathf.Min(Page, pages.Count - 1)]) : ""; // the page drawn, colour tags and all
        static readonly List<FitBox> all = new List<FitBox>();
        readonly List<string> pages = new List<string>();
        string shown; float lastWidth;
        void OnEnable() { all.Add(this); }
        void OnDisable() { all.Remove(this); }
        // The box on screen now (one instrument screen shows at a time), or null.
        public static FitBox Visible { get { foreach (var box in all) if (box.isActiveAndEnabled && box.Line != null && box.Line.gameObject.activeInHierarchy && box.Source != "") return box; return null; } }
        // The whole line behind a paged box's text; any other Text's own text.
        public static string Whole(Text line) { var box = line != null ? line.GetComponentInParent<FitBox>() : null; return box != null && box.Line == line && box.shown == line.text ? box.Source : line != null ? line.text : ""; }
        public void Turn() { if (Page >= pages.Count - 1) return; Page++; Show(); }
        void LateUpdate()
        {
            if (Line == null) return;
            float width = Line.rectTransform.rect.width;
            bool rewritten = Line.text != shown;
            if (!rewritten && width == lastWidth) return;
            if (rewritten && Line.text != Source) { Source = Line.text ?? ""; Page = 0; } // a new line; the same line written again keeps its page
            lastWidth = width; Paginate(); Show();
        }
        void Paginate()
        {
            pages.Clear();
            float limit = Height("A") * PageLines + 1;
            string page = "";
            foreach (var sentence in Sentences(Source))
            {
                string joined = page == "" ? sentence : page + " " + sentence;
                if (page != "" && Height(joined) > limit) { pages.Add(page); page = sentence; } else page = joined;
            }
            pages.Add(page);
            if (Page >= pages.Count) Page = pages.Count - 1;
        }
        void Show()
        {
            bool more = Page < pages.Count - 1;
            string text = pages.Count > 0 ? pages[Page] : "";
            Line.supportRichText = true; Line.text = shown = Colour(text);
            if (More != null) More.gameObject.SetActive(more);
            var rect = (RectTransform)transform;
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, Slots.InstrumentBoxHeight(Height(text), Max, more));
        }
        float Height(string text) => Line.cachedTextGeneratorForLayout.GetPreferredHeight(Colour(text), Line.GetGenerationSettings(new Vector2(Line.rectTransform.rect.width, 0))) / Line.pixelsPerUnit;
        // A sentence ends at . ? ! (and any closing quote) before a space; Caspar's "..." pauses end one too.
        static IEnumerable<string> Sentences(string text)
        {
            foreach (var part in Regex.Split((text ?? "").Trim(), @"(?<=[.?!]['""’”)]?)\s+(?=\S)")) if (part.Length > 0) yield return part;
        }
        // Build S (owner, Sept 29): each element in its own colour wherever Caspar names it (Claude's working colours, Sept 29).
        public static readonly (string word, string hex)[] Elements = { ("Fire", "#E0643C"), ("Earth", "#8DB36A"), ("Air", "#E8D38F"), ("Water", "#63A6E0") };
        public static string Colour(string text)
        {
            foreach (var (word, hex) in Elements) text = Regex.Replace(text ?? "", @"\b" + word + @"\b", "<color=" + hex + ">" + word + "</color>");
            return text;
        }
    }
}
