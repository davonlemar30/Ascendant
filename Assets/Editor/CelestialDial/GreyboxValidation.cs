using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEditor;
using UnityEngine;
using Ascendant.CelestialDial;

namespace Ascendant.Build
{
    // Deterministic domain checks runnable by the same pinned Editor, without adding packages.
    public static class GreyboxValidation
    {
        static readonly List<string> Passed=new List<string>();
        static void Check(bool condition,string name) { if(!condition)throw new Exception("FAIL: "+name);Passed.Add(name); }
        static DialLesson Transfer()
        {
            var lesson=new DialLesson(()=>10);
            EnterGuided(lesson);
            Answer(lesson);Answer(lesson);lesson.Continue();return lesson;
        }
        static bool LessonMessageIsLocked(DialLesson lesson)=>lesson.Message.StartsWith("Two families down. You are halfway through the wheel.");
        static void EnterGuided(DialLesson l){ while(l.Phase==LessonPhase.Encounter) l.Continue(); l.Continue(); }
        static DialEvent Answer(DialLesson lesson)
        {
            lesson.Dial.Select(Zodiac.Destination(lesson.Dial.Start),DialInput.DirectSeat);
            var result=lesson.Seal();lesson.AfterCorrect(result);return result;
        }
        static void Wrong(DialLesson lesson,int times)
        {
            for(int i=0;i<times;i++){lesson.Dial.Select(lesson.Dial.Start,DialInput.DirectSeat);lesson.Seal();}
        }
        static void ValidateCommittedSaves()
        {
            // The same subscriptions and snapshot builder as SliceView, followed by a real JSON round-trip.
            SaveData saved = null;
            SliceFlow Observe(DialLesson lesson, GridModel grid)
            {
                saved = null;
                var flow = new SliceFlow(() => 1);
                flow.Restore(new SaveData { sunSign = 1, atriumStage = 3, keyEarned = true, keys = 1 });
                flow.ObserveProgress(lesson, grid, () => saved = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(flow.CaptureProgress(lesson, grid))));
                return flow;
            }
            DialLesson RestoreLesson(SaveData save)
            {
                var flow = new SliceFlow(() => 1);
                Check(flow.Restore(save), "committed checkpoint restores its flow");
                var lesson = new DialLesson(() => 0);
                lesson.RestoreProgress(save.sunSign, save.lit, save.kin, save.keyEarned);
                lesson.RestoreGlyphs(save.glyphStage, save.glyphIndex, save.keys >= 2);
                lesson.RestoreModalities(save.litMod, save.kinMod, save.modalitiesStarted);
                lesson.SetKey3(save.keys >= 3);
                lesson.RestoreOpposites(save.polarityShown, save.oppKnown, save.oppositesStarted, save.built, save.builderEvidence, save.keys >= 4);
                return lesson;
            }
            DialLesson LitLesson(bool symbols = false, bool modalities = false)
            {
                var lesson = new DialLesson(() => 0);
                lesson.RestoreProgress(1, Enumerable.Repeat(true, 12).ToArray(), Enumerable.Repeat(true, 12).ToArray(), true);
                if (symbols) lesson.RestoreGlyphs(2, 12, true);
                if (modalities) lesson.RestoreModalities(Enumerable.Repeat(true, 12).ToArray(), Enumerable.Repeat(true, 3).ToArray(), true);
                return lesson;
            }
            var grid = new GridModel(() => 0);
            var naming = LitLesson(); var namingFlow = Observe(naming, grid); naming.BeginGlyphs();
            for (int i = 0; i < 5; i++)
            {
                naming.AnswerGlyphName(naming.CurrentGlyph);
                var restored = RestoreLesson(saved);
                Check(saved.glyphStage == 0 && saved.glyphIndex == i + 1 && restored.BeginGlyphs() && restored.CurrentGlyph == i + 1 && saved.deck[i + 12].State == ItemState.Practicing,
                    "Part A answer " + (i + 1) + " checkpoints the next card and deck evidence");
            }
            for (int i = 5; i < 12; i++) naming.AnswerGlyphName(naming.CurrentGlyph);
            var partB = RestoreLesson(saved);
            Check(saved.glyphStage == 1 && saved.glyphIndex == 0 && partB.AllNamed && partB.BeginGlyphs() && partB.Phase == LessonPhase.GlyphWheel && partB.Dial.Target == 0,
                "the last naming answer restores directly into Part B without repeating cards");
            Observe(partB, grid);
            for (int i = 0; i < 5; i++)
            {
                partB.Dial.Select(partB.Dial.Target, DialInput.DirectSeat); var answer = partB.Seal(); partB.AfterCorrect(answer);
                var restored = RestoreLesson(saved);
                Check(saved.glyphStage == 1 && saved.glyphIndex == i + 1 && restored.GlyphPlaced.SequenceEqual(partB.GlyphPlaced) && restored.BeginGlyphs() && restored.Dial.Target == i + 1 && saved.deck[i + 12].State == ItemState.Practicing,
                    "Part B answer " + (i + 1) + " restores placed symbols, next target, and deck evidence");
            }
            var elements = new DialLesson(() => 0);
            elements.RestoreProgress(1, Enumerable.Range(0, 12).Select(i => i % 4 < 2).ToArray(), Enumerable.Range(0, 12).Select(i => i % 4 < 2).ToArray(), true);
            Observe(elements, grid); elements.BeginContinuation();
            for (int i = 0; i < 4; i++)
            {
                int target = Zodiac.Destination(elements.Dial.Start); Answer(elements); var restored = RestoreLesson(saved);
                Check(restored.Lit.SequenceEqual(elements.Lit) && restored.Kin.SequenceEqual(elements.Kin) && saved.deck[target].State == ItemState.Practicing,
                    "element answer " + (i + 1) + " restores lit seats, family completion, and deck evidence");
            }
            var mod = LitLesson(true); var modFlow = Observe(mod, grid); modFlow.StartModalities(); mod.BeginModalities();
            int modAnswers = 0;
            while (!mod.ModalitiesComplete)
            {
                int target = Zodiac.Destination(mod.Dial.Start, 3); mod.Dial.Select(target, DialInput.DirectSeat); var answer = mod.Seal(); mod.AfterCorrect(answer);
                var restored = RestoreLesson(saved);
                Check(restored.LitMod.SequenceEqual(mod.LitMod) && restored.KinMod.SequenceEqual(mod.KinMod) && saved.deck[24 + target].entered && (!answer.evidence_eligible || saved.deck[24 + target].State == ItemState.Practicing),
                    "modality answer " + (++modAnswers) + " restores seats, families, and eligible evidence");
            }
            var opposite = LitLesson(true, true); opposite.SetKey3(true); var oppFlow = Observe(opposite, grid); oppFlow.MarkKey3(); oppFlow.StartOpposites(); opposite.BeginOpposites(); opposite.Continue(); opposite.Continue();
            for (int i = 0; i < 6; i++)
            {
                int pair = Zodiac.PairOf(opposite.Dial.Start); opposite.Dial.Select(Zodiac.Opposite(opposite.Dial.Start), DialInput.DirectSeat); var answer = opposite.Seal(); opposite.AfterCorrect(answer);
                var restored = RestoreLesson(saved);
                Check(restored.OppKnown.SequenceEqual(opposite.OppKnown) && saved.deck[48 + pair].entered && (!answer.evidence_eligible || saved.deck[48 + pair].State == ItemState.Practicing),
                    "opposite answer " + (i + 1) + " restores the known pair and eligible evidence");
            }
            opposite.Continue();
            for (int i = 0; i < 3; i++)
            {
                opposite.AnswerBuilderName(opposite.BuilderTarget); opposite.Dial.Select(Zodiac.Opposite(opposite.BuilderTarget), DialInput.DirectSeat); var answer = opposite.Seal(); opposite.AfterCorrect(answer);
                opposite.AnswerBuilderShare(0); opposite.AnswerBuilderShare(1);
                var restored = RestoreLesson(saved);
                Check(restored.Built == i + 1 && restored.BuilderEvidence, "builder sign " + (i + 1) + " restores its committed count and evidence");
                opposite.NextBuilderSign();
            }
            Check(saved.keys == 4 && RestoreLesson(saved).Key4Earned, "the final builder transition checkpoints Key 4 before presentation polling");
            var tableLesson = LitLesson(true, true); var table = new GridModel(() => 0); var tableFlow = Observe(tableLesson, table); tableFlow.StartGrid(); table.Begin();
            for (int i = 0; i < 12; i++)
            {
                table.Pick(i); table.Choose(GridModel.CellOf(i)); table.Seal();
                var restored = new GridModel(() => 0); restored.Restore(saved.gridPlaced, saved.gridEvidence, saved.gridStarted, saved.keys >= 3);
                Check(restored.Placed.SequenceEqual(table.Placed) && restored.Evidence && saved.deck[36 + i].State == ItemState.Practicing,
                    "table seating " + (i + 1) + " restores placement and deck evidence");
            }
            Check(saved.keys == 3, "the final seating checkpoints Key 3 before presentation polling");
            var keyFlow = new SliceFlow(() => 1); keyFlow.SkipPrologue(); keyFlow.Continue(); keyFlow.ChooseBirth("skip"); keyFlow.PickSkipSun(1); keyFlow.PickSkipMoon(3); keyFlow.PickRising(4); keyFlow.Continue(); keyFlow.Continue(); keyFlow.EnterWing(); keyFlow.EnterDial(); // Build T: the walk to the Dial
            Check(keyFlow.RevealKey() && keyFlow.Keys == 1 && keyFlow.KeysInHand == 1, "the first Key reveal immediately records one earned Key");
        }
        static void ValidateEvidenceRouting()
        {
            // Each unit, played through the production subscriptions, moves only its own item kind (task 86bc2338n).
            string Kind(ReviewDeck deck, ItemKind kind) => string.Join("|", deck.Items.Where(i => i.Kind == kind).Select(i => i.seat + ":" + i.state + ":" + i.streak + ":" + i.interval + ":" + i.dueDay + ":" + i.entered));
            string Others(ReviewDeck deck, ItemKind except) => string.Join("/", Enum.GetValues(typeof(ItemKind)).Cast<ItemKind>().Where(k => k != except).Select(k => Kind(deck, k)));
            SliceFlow Observe(DialLesson lesson, GridModel grid)
            {
                var flow = new SliceFlow(() => 1);
                flow.Restore(new SaveData { sunSign = 1, atriumStage = 3, keyEarned = true, keys = 1 });
                flow.ObserveProgress(lesson, grid, () => { });
                return flow;
            }
            DialLesson Lit(bool symbols = false, bool modalities = false)
            {
                var lesson = new DialLesson(() => 0);
                lesson.RestoreProgress(1, Enumerable.Repeat(true, 12).ToArray(), Enumerable.Repeat(true, 12).ToArray(), true);
                if (symbols) lesson.RestoreGlyphs(2, 12, true);
                if (modalities) lesson.RestoreModalities(Enumerable.Repeat(true, 12).ToArray(), Enumerable.Repeat(true, 3).ToArray(), true);
                return lesson;
            }
            var grid = new GridModel(() => 0);
            // Symbols: Part A then Part B on the wheel, every answer at Level 0.
            var symbols = Lit(); var symbolFlow = Observe(symbols, grid); symbolFlow.StartGlyphs();
            string elementsBefore = Kind(symbolFlow.Deck, ItemKind.Element), othersBefore = Others(symbolFlow.Deck, ItemKind.Glyph);
            symbols.BeginGlyphs();
            for (int i = 0; i < 12; i++) symbols.AnswerGlyphName(symbols.CurrentGlyph);
            Check(symbols.Phase == LessonPhase.GlyphWheel && Kind(symbolFlow.Deck, ItemKind.Element) == elementsBefore, "symbol Part A leaves every element item untouched");
            { int t = symbols.Dial.Target; string before = symbols.Challenge; symbols.Dial.Select(Zodiac.Wrap(t + 2), DialInput.DirectSeat);
              Check(before == Zodiac.Seats[t].Name && symbols.Challenge == before && symbols.Message == "", "Build I: the symbol challenge names its target on the wheel, fixed while the wheel turns, and Caspar does not pose it"); }
            for (int i = 0; i < 12; i++) { symbols.Dial.Select(symbols.Dial.Target, DialInput.DirectSeat); var answer = symbols.Seal(); Check(answer.correctness && answer.event_name == "answer_correct", "symbol placement " + (i + 1) + " is a correct wheel answer"); symbols.AfterCorrect(answer); }
            Check(symbols.Key2Earned && Kind(symbolFlow.Deck, ItemKind.Element) == elementsBefore && Others(symbolFlow.Deck, ItemKind.Glyph) == othersBefore, "symbol Part B (twelve answer_correct events on the wheel) leaves every element, modality, grid, and opposite item untouched");
            Check(symbolFlow.Deck.Items.Where(i => i.Kind == ItemKind.Glyph).All(i => i.entered && i.State == ItemState.Practicing && i.streak >= 1), "symbol answers advance all twelve symbol items");
            // Elements: the guided family, the transfer family, and the continuation, through the same subscription.
            var elements = new DialLesson(() => 0); elements.SetSunSign(1); var elementFlow = Observe(elements, grid);
            string glyphsBefore = Kind(elementFlow.Deck, ItemKind.Glyph); othersBefore = Others(elementFlow.Deck, ItemKind.Element);
            EnterGuided(elements); Answer(elements); Answer(elements); elements.Continue(); Answer(elements); Answer(elements); elements.BeginContinuation(); Answer(elements); Answer(elements); Answer(elements); Answer(elements);
            Check(elements.WheelComplete && Others(elementFlow.Deck, ItemKind.Element) == othersBefore && Kind(elementFlow.Deck, ItemKind.Glyph) == glyphsBefore, "element-family answers leave every symbol, modality, grid, and opposite item untouched");
            Check(elementFlow.Deck.Items.Where(i => i.Kind == ItemKind.Element).Count(i => i.State == ItemState.Practicing) >= 6, "independent element answers advance element items");
            // Modalities.
            var mod = Lit(true); var modFlow = Observe(mod, grid); modFlow.StartModalities();
            othersBefore = Others(modFlow.Deck, ItemKind.Modality); mod.BeginModalities();
            Check(mod.Challenge == "Next " + Zodiac.ModalityAt(mod.Dial.Start) + " after " + Zodiac.Seats[mod.Dial.Start].Name, "Build I: the modality challenge is the wheel's own line");
            while (!mod.ModalitiesComplete) { mod.Dial.Select(Zodiac.Destination(mod.Dial.Start, 3), DialInput.DirectSeat); var answer = mod.Seal(); mod.AfterCorrect(answer); }
            Check(Others(modFlow.Deck, ItemKind.Modality) == othersBefore && modFlow.Deck.Items.Where(i => i.Kind == ItemKind.Modality).All(i => i.entered), "modality answers move only modality items");
            // Opposites and the builder.
            var opp = Lit(true, true); opp.SetKey3(true); var oppFlow = Observe(opp, grid); oppFlow.MarkKey3(); oppFlow.StartOpposites();
            othersBefore = Others(oppFlow.Deck, ItemKind.Opposite); opp.BeginOpposites(); opp.Continue(); opp.Continue();
            for (int i = 0; i < 6; i++) { opp.Dial.Select(Zodiac.Opposite(opp.Dial.Start), DialInput.DirectSeat); var answer = opp.Seal(); opp.AfterCorrect(answer); }
            opp.Continue();
            for (int i = 0; i < 3; i++) { opp.AnswerBuilderName(opp.BuilderTarget); opp.Dial.Select(Zodiac.Opposite(opp.BuilderTarget), DialInput.DirectSeat); var answer = opp.Seal(); opp.AfterCorrect(answer); opp.AnswerBuilderShare(0); opp.AnswerBuilderShare(1); opp.NextBuilderSign(); }
            Check(opp.Key4Earned && Others(oppFlow.Deck, ItemKind.Opposite) == othersBefore && oppFlow.Deck.Items.Where(i => i.Kind == ItemKind.Opposite && i.seat < 6).All(i => i.entered), "opposite and builder answers move only the six pair items");
            // The table.
            var tableLesson = Lit(true, true); var table = new GridModel(() => 0); var tableFlow = Observe(tableLesson, table); tableFlow.StartGrid();
            othersBefore = Others(tableFlow.Deck, ItemKind.Grid); table.Begin();
            for (int i = 0; i < 12; i++) { table.Pick(i); table.Choose(GridModel.CellOf(i)); table.Seal(); }
            Check(table.Key3Earned && Others(tableFlow.Deck, ItemKind.Grid) == othersBefore && tableFlow.Deck.Items.Where(i => i.Kind == ItemKind.Grid).All(i => i.State == ItemState.Practicing), "table seatings move only grid items");
            // A review answer on the wheel records nothing through the lesson subscription; the batch records its own item.
            var review = Lit(true, true); var reviewFlow = Observe(review, grid);
            string allBefore = Others(reviewFlow.Deck, ItemKind.Grid) + Kind(reviewFlow.Deck, ItemKind.Grid);
            review.BeginReview(2, 4); review.Dial.Select(Zodiac.Destination(2), DialInput.DirectSeat); var reviewAnswer = review.Seal(); review.AfterCorrect(reviewAnswer);
            Check(reviewAnswer.correctness && review.Phase == LessonPhase.Review && Others(reviewFlow.Deck, ItemKind.Grid) + Kind(reviewFlow.Deck, ItemKind.Grid) == allBefore, "a compressed Dial review answer records no lesson evidence of any kind");
        }
        // ---- Build Z (owner, Sept 29-30; task 86bca6fmv): the Dial's room, its glow, the eye that speaks, the Wing Dial's eye ----
        public static readonly string[] EyeLines = Enumerable.Range(0, 12).SelectMany(i => new[] {
            "Next " + Zodiac.Seats[i].Element + " after " + Zodiac.Seats[i].Name, "Next " + Zodiac.ModalityAt(i) + " after " + Zodiac.Seats[i].Name,
            "Across from " + Zodiac.Seats[i].Name, "Find the symbol of\n" + Zodiac.Seats[i].Name, "Your sign: " + Zodiac.Seats[i].Name }).ToArray();
        static void ValidateBuildZ()
        {
            Slots.ArtSlot Slot(string n) => Slots.Art.FirstOrDefault(a => a.Name == n);
            Check(Slot("dial-room") is Slots.ArtSlot room && room.Width == 360 && room.Height == 800 && Slot("dial-room-light") is Slots.ArtSlot light && light.Width == 360 && light.Height == 800
                && Slot("kit-dial-worn-open") != null && Slot("kit-dial-restored-open") != null && Slot("candle") != null, "Build Z: the Dial's room and its glow are full-screen slots; the Wing Dial's open eye has a slot per state; the candle slot stays (the opening's and the Chamber's)");
            Sprite Art(string n) => Resources.Load<Sprite>("Art/" + n);
            Check(new[] { "dial-room", "dial-room-light", "seat", "dial-face", "kit-dial-worn", "kit-dial-restored", "kit-dial-worn-open", "kit-dial-restored-open" }.All(n => Art(n) != null), "Build Z: the approved art is in the Art folder: the room, its glow, the seat, the face, and the Wing Dial closed and open");
            Check(Art("dial-room").rect.width == 1200 && Art("dial-room").rect.height == 1840 && Art("dial-room-light").rect.width == 720 && Art("dial-room-light").rect.height == 1600, "Build Z: the room and its glow register on the column; since batch 2 the room is a 1200 x 1840 master with the 360 x 800 column at twice the layout in its centre, and the glow, a radial fill over the column only, keeps its 720 x 1600 file");
            Check(Art("kit-dial-worn-open").rect.size == Art("kit-dial-worn").rect.size && Art("kit-dial-restored-open").rect.size == Art("kit-dial-restored").rect.size
                && new[] { "kit-dial-worn", "kit-dial-restored", "kit-dial-worn-open", "kit-dial-restored-open" }.All(n => Slot(n).Width * 2 == (int)Art(n).rect.width && Slot(n).Height * 2 == (int)Art(n).rect.height),
                "Build Z: each open Wing Dial is the same canvas as its closed piece, so the eye opens in place; the manifest gives each Wing Dial file at half size");
            // Build AB (owner, Sept 30: the Astrolabe): the room carries the fixed rim, pointer, phoenix and eye; the ring and its glow turn
            Check(Slot("dial-ring") is Slots.ArtSlot ringSlot && ringSlot.Width == 360 && ringSlot.Height == 360 && Slot("dial-ring-light") != null && Slot("bracket") is Slots.ArtSlot frame && frame.Width == 72 && frame.Height == 88
                && Art("dial-ring") != null && Art("dial-ring").rect.width == 720 && Art("dial-ring").rect.height == 720 && Art("dial-ring-light") != null && Art("dial-ring-light").rect.size == Art("dial-ring").rect.size && Art("bracket") != null && Art("bracket").rect.width == 144 && Art("bracket").rect.height == 176,
                "Build AB: the turning ring and its glow are 360 x 360 slots drawn at twice the size; the frame is a 72 x 88 wedge (Build AC)");
            Check(typeof(ArcText).IsSubclassOf(typeof(UnityEngine.UI.BaseMeshEffect)) && DialView.NameRoom > 60 && DialView.TabletRoom > 40, "Build AB: the names bend along the band (ArcText); a name has about " + DialView.NameRoom.ToString("0") + " px of recess, a fact " + DialView.TabletRoom.ToString("0") + " px of window (Build AC)");
            var voice = Resources.Load<Font>("Fonts/EBGaramond-Bold");
            Check(voice != null && File.Exists("Assets/CelestialDial/Resources/Fonts/OFL-EBGaramond.txt"), "Build Z: the Dial's serif, EB Garamond Bold, ships with its open licence");
            Check(EyeLines.All(l => l.All(c => c == '\n' || (c >= 32 && c <= 255))), "Build Z: every line the eye can show is Latin-1 (the web font rule)");
            var settings = new TextGenerationSettings { font = voice, fontSize = DialView.EyeMax, resizeTextForBestFit = true, resizeTextMinSize = DialView.EyeMin, resizeTextMaxSize = DialView.EyeMax, lineSpacing = .92f, textAnchor = TextAnchor.MiddleCenter,
                generationExtents = DialView.EyeBox, pivot = new Vector2(.5f, .5f), horizontalOverflow = HorizontalWrapMode.Wrap, verticalOverflow = VerticalWrapMode.Truncate, scaleFactor = 1, color = Color.white, richText = false, updateBounds = true };
            var generator = new TextGenerator(); int smallest = 99; string tightest = "";
            foreach (var line in EyeLines)
            {
                generator.Populate(line, settings); int size = generator.fontSizeUsedForBestFit;
                Check(generator.lineCount <= 2 && size >= DialView.EyeMin && generator.characterCountVisible >= line.Replace("\n", "").Length, "Build Z: \"" + line.Replace("\n", " / ") + "\" fits inside the eye's glass (Build AB: " + DialView.EyeBox.x + " x " + DialView.EyeBox.y + ") on at most two lines at " + DialView.EyeMin + " px or more (" + size + " px, " + generator.lineCount + " lines)");
                if (size < smallest) { smallest = size; tightest = line; }
            }
            Check(smallest >= DialView.EyeMin, "Build Z: the eye's smallest line is " + smallest + " px (\"" + tightest.Replace("\n", " / ") + "\")");
            // Build AC (owner, Oct 1: the count word goes inside the eye, under the challenge line): while a count runs the challenge folds onto one line across the glass's widest rows, 10 to 12 px; the count word fits its own line under it
            var countSettings = settings; countSettings.generationExtents = DialView.EyeBoxCount; countSettings.resizeTextMinSize = DialView.EyeMinCount; countSettings.resizeTextMaxSize = DialView.EyeMaxCount; countSettings.fontSize = DialView.EyeMaxCount; int smallestCount = 99; string tightestCount = ""; var countMisfits = new List<string>();
            foreach (var raw in EyeLines)
            {
                var line = raw.Replace("\n", " "); generator.Populate(line, countSettings); int size = generator.fontSizeUsedForBestFit;
                if (!(generator.lineCount == 1 && size >= DialView.EyeMinCount && generator.characterCountVisible >= line.Length)) countMisfits.Add(line + " (" + size + " px, " + generator.lineCount + " lines)");
                if (size < smallestCount) { smallestCount = size; tightestCount = line; }
            }
            Check(countMisfits.Count == 0 && smallestCount >= DialView.EyeMinCount, "Build AC: while a count runs every challenge folds onto one line across the glass (" + DialView.EyeBoxCount.x + " x " + DialView.EyeBoxCount.y + ") at " + DialView.EyeMinCount + " px or more; the smallest is " + smallestCount + " px (\"" + tightestCount + "\")" + (countMisfits.Count > 0 ? "; too tight: " + string.Join(", ", countMisfits) : ""));
            var wordSettings = settings; wordSettings.resizeTextForBestFit = false; wordSettings.fontSize = DialView.CountSize; wordSettings.horizontalOverflow = HorizontalWrapMode.Overflow; wordSettings.generationExtents = new Vector2(400, 40);
            Check(new[] { "one", "two", "three", "four", "five", "six" }.All(w => generator.GetPreferredWidth(w, wordSettings) <= DialView.CountBox.x), "Build AC: every count word fits its line inside the eye (" + DialView.CountBox.x + " px)");
            // Build AC: the framed sign's name and facts ride the ribbon's arc within its plain face
            var ribbonName = wordSettings; ribbonName.fontSize = DialView.NameSize; var ribbonFacts = wordSettings; ribbonFacts.fontSize = DialView.FactSize;
            float widestName = Zodiac.Seats.Max(z => generator.GetPreferredWidth(z.Name, ribbonName));
            var factLines = (from el in new[] { "Water", "Earth", "Fire", "Air" } from mo in Zodiac.Modalities from po in new[] { "Yang", "Yin" } select el + " · " + mo + " · " + po).ToArray();
            float widestFacts = factLines.Max(l => generator.GetPreferredWidth(l, ribbonFacts));
            Check(widestName <= DialView.RibbonNameRoom && widestFacts <= DialView.RibbonFactsRoom, "Build AC: the longest name (" + widestName.ToString("0") + " px at " + DialView.NameSize + ") and the longest facts line (" + widestFacts.ToString("0") + " px at " + DialView.FactSize + ") ride the ribbon's arc within its plain face (" + DialView.RibbonNameRoom + " and " + DialView.RibbonFactsRoom + " px)");
            var zsave = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(new SaveData { revealsPlayed = new[] { "elements", "symbols" } }));
            Check(zsave.revealsPlayed.SequenceEqual(new[] { "elements", "symbols" }) && new SaveData().revealsPlayed.Length == 0, "Build Z: the save carries the patterns whose reveal has played; a new save has none");
            Check(DialView.RevealSweepSeconds + DialView.RevealNamesSeconds + DialView.RevealEyeSeconds <= 2.1f && SliceView.DialEyeSeconds <= .4f, "Build Z: the reveal is about two seconds of small built motions; the Wing Dial's eye opens in under half a second");
        }
        static void ValidateBuildW()
        {
            // ---- Build W (owner, Sept 26 note 5; task 86bca0163): DEV Mode's checkpoints, each a real save from one scripted run; Keys earned, not spent ----
            Check(DevCheckpoints.All.Select(c => c.id).SequenceEqual(new[] { "key1", "wheel", "key2", "key3", "key4", "whole" }), "Build W: six checkpoints, in play order (owner, Sept 29)");
            SaveData Round(string id) => JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(DevCheckpoints.Play(id, "Tester", 4)));
            SliceFlow Flow(SaveData save) { var f = new SliceFlow(); Check(f.Restore(save), "Build W: the " + save.keys + "-Key checkpoint restores its flow"); return f; }
            int Proven(SaveData s, ItemKind kind) => s.deck.Count(i => i.kind == (int)kind && i.entered && i.state == (int)ItemState.Practicing);
            var k1 = Round("key1"); var f1 = Flow(k1);
            Check(k1.playerName == "Tester" && k1.sunSign == 4 && k1.keys == 1 && k1.locksFilled == 1 && k1.atriumStage == 2 && !k1.wheelComplete && k1.lit.Count(v => v) == 6 && f1.AtHub && f1.KeysInHand == 0 && Proven(k1, ItemKind.Element) == 2,
                "Build W: after Key 1, the Atrium at Stage 2, six seats lit, Key 1 in the Chamber (the opening inserts it), the two independent answers proven, as a played run leaves them [name=" + k1.playerName + " sun=" + k1.sunSign + " keys=" + k1.keys + " locks=" + k1.locksFilled + " stage=" + k1.atriumStage + " wheel=" + k1.wheelComplete + " lit=" + k1.lit.Count(v => v) + " hub=" + f1.AtHub + " inHand=" + f1.KeysInHand + " proven=" + Proven(k1, ItemKind.Element) + "]");
            var wh = Round("wheel");
            Check(wh.keys == 1 && wh.wheelComplete && wh.lit.All(v => v) && wh.atriumStage == 3 && wh.glyphStage == 0, "Build W: the whole wheel lit, the Atrium at Stage 3, the Book waiting");
            var k2 = Round("key2"); var f2 = Flow(k2);
            Check(k2.keys == 2 && k2.glyphStage == 2 && k2.locksFilled == 1 && f2.KeysInHand == 1 && !k2.modalitiesStarted && Proven(k2, ItemKind.Glyph) == 12, "Build W: after Key 2, Key 2 in hand, the symbols proven, the second pattern waiting");
            var k3 = Round("key3"); var f3 = Flow(k3);
            Check(k3.keys == 3 && k3.gridPlaced.All(v => v) && k3.litMod.All(v => v) && f3.KeysInHand == 2 && !k3.oppositesStarted && Proven(k3, ItemKind.Grid) == 12, "Build W: after Key 3, Keys 2 and 3 in hand, the Table full, the last pattern waiting");
            var k4 = Round("key4"); var f4 = Flow(k4);
            Check(k4.keys == 4 && k4.built == 3 && k4.builderEvidence && k4.oppKnown.All(v => v) && f4.KeysInHand == 3 && k4.locksFilled == 1 && k4.atriumStage == 3, "Build W: after Key 4, three Keys in hand, nothing more spent");
            var wo = Round("whole"); var fw = Flow(wo);
            Check(wo.keys == 4 && wo.locksFilled == 4 && wo.atriumStage == 6 && fw.WingWhole && fw.KeysInHand == 0, "Build W: the Zodiac Wing whole, all four Keys spent, the Atrium at Stage 6");
            Check(f4.EnterChamber() && f4.SpendKey() && f4.KeysInHand == 2, "Build W: from after Key 4 the Chamber takes a Key: the checkpoint plays on");
            Check(f2.EnterWing() && f2.EnterDial() && f2.Screen == SliceScreen.Wing, "Build W: from after Key 2 the Dial opens: the checkpoint plays on");
        }
        // ---- Batch 2 (owner, Oct 1: the journal's architecture, 1b C and 1e A; Oct 2 evening: the inscription, the fonts, and the
        // Practice door an entry point only until its scope is approved) ----
        // ---- Batch 2 (owner, Oct 2 evening: the Big Three approved as proposed, unknowns A): the chart's astronomy, the places and their
        // time zones, the opening's paths and the save. References: Meeus's worked examples, JPL Horizons (apparent ecliptic-of-date
        // longitudes, fetched Oct 2), Python's zoneinfo, and a published chart. ----
        static void ValidateBirthChart()
        {
            double Off(double a, double b) => Math.Abs((a - b + 540) % 360 - 180);
            Check(Math.Abs(Sky.SunDistance(2448908.5) - 0.99760775) < 1e-7 && Off(Sky.SunLongitude(2448908.5), 199.906060) < .0001, "Meeus 25.b: the sun on 1992 Oct 13.0 TD with VSOP87 (higher accuracy): R 0.99760775 and 199.90606° (" + Sky.SunLongitude(2448908.5).ToString("0.000000") + ")");
            Check(Math.Round(Sky.MoonSum(2448724.5, out _)) == -1127527 && Off(Sky.MoonGeometricLongitude(2448724.5), 133.162655) < .00001, "Meeus 47.a: the moon's sixty terms sum to -1127527 and put it at 133.162655° on 1992 Apr 12, 0h TD");
            Check(Off(Sky.Gmst(2446895.5), 197.693195) < .00001 && Off(Sky.Gmst(2446896.30625), 128.7378734) < .00001, "Meeus 12.a and 12.b: sidereal time on 1987 Apr 10 at 0h and 19:21 UT");
            // JPL Horizons, geocentric apparent ecliptic-of-date longitudes (UT): the sun, then the moon
            var horizons = new (int y, int m, int d, int h, int mi, double sun, double moon)[] {
                (1901,1,1,0,0,279.9091006,48.3162700), (1925,6,21,12,0,89.5692301,92.1802167), (1944,11,3,3,15,220.5848335,75.2590288), (1957,2,14,18,45,325.8295573,147.0989189),
                (1969,7,20,20,17,117.9107302,187.8737519), (1977,9,5,12,56,162.8436965,72.1112967), (1986,1,28,16,39,308.4669286,161.0494549), (1990,5,1,14,30,40.9568767,128.0023066),
                (1999,12,31,23,59,279.8584970,217.2849159), (2008,3,20,5,48,359.9997861,162.0485828), (2016,8,21,9,0,148.7302400,8.2003770), (2026,10,2,12,0,189.3134739,85.3148334) };
            double worstSun = 0, worstMoon = 0;
            foreach (var r in horizons) { double jd = Sky.JulianDay(r.y, r.m, r.d, r.h + r.mi / 60.0), jde = Sky.Ephemeris(jd, r.y, r.m); worstSun = Math.Max(worstSun, Off(Sky.SunLongitude(jde), r.sun)); worstMoon = Math.Max(worstMoon, Off(Sky.MoonLongitude(jde), r.moon)); }
            Check(worstSun < .0005 && worstMoon < .005, "against JPL Horizons at twelve moments from 1901 to 2026: the sun within " + worstSun.ToString("0.0000") + "°, the moon within " + worstMoon.ToString("0.0000") + "° (the 2008 equinox included)");
            // the ascendant's formula against the horizon itself: the ecliptic's point on the eastern horizon, found by search
            double worstAsc = 0;
            foreach (var lat in new[] { -45.0, 0, 12.5, 51.5, 60 }) for (int k = 0; k < 6; k++)
                {
                    double jd = 2451545.0 + k * .1371 + lat / 100, lon = -0.1276 * k * 50, e = 23.4392911 * Math.PI / 180, phi = lat * Math.PI / 180, lst = (Sky.Gmst(jd) + lon) * Math.PI / 180, found = -1;
                    double Alt(double l, out double sinH) { l *= Math.PI / 180; double ra = Math.Atan2(Math.Sin(l) * Math.Cos(e), Math.Cos(l)), dec = Math.Asin(Math.Sin(l) * Math.Sin(e)), H = lst - ra; sinH = Math.Sin(H); return Math.Asin(Math.Sin(phi) * Math.Sin(dec) + Math.Cos(phi) * Math.Cos(dec) * Math.Cos(H)); }
                    for (int i = 0; i < 36000 && found < 0; i++) { double a0 = Alt(i / 100.0, out double s0), a1 = Alt((i + 1) / 100.0, out _); if ((a0 < 0) != (a1 < 0) && s0 < 0) found = i / 100.0 + .01 * -a0 / (a1 - a0); }
                    worstAsc = Math.Max(worstAsc, Off(Sky.Ascendant(jd, lat, lon), found));
                }
            Check(worstAsc < .02, "the rising sign's formula agrees with a search of the eastern horizon to " + worstAsc.ToString("0.000") + "°, from 45° S to 60° N");
            // the places and their time zones
            Check(Places.All.Count == 34152 && Places.ZoneCount == 356 && Places.All.All(p => p.Zone >= 0 && p.Zone < 356 && Math.Abs(p.Latitude) <= 90 && Math.Abs(p.Longitude) <= 180), "the bundled list: 34,152 places of 15,000 people or more (GeoNames), each with its place and one of 356 time zones");
            var portland = Places.Search("portland"); var saoPaulo = Places.Search("São Paulo"); var plain = Places.Search("sao paulo"); var york = Places.Search("new york");
            Check(portland.Count > 0 && portland[0].Name == "Portland, OR, United States" && saoPaulo.Count > 0 && saoPaulo[0].Name == "São Paulo, Brazil" && plain.Count > 0 && plain[0] == saoPaulo[0] && york.Count > 0 && york[0].Name == "New York City, NY, United States" && Places.Search("x").Count == 0 && Places.Search("portland", 4).Count <= 4,
                "a search finds the biggest place first, with or without its accents: " + string.Join("; ", new[] { portland, saoPaulo, york }.Select(l => l.Count > 0 ? l[0].Name : "nothing")));
            var clocks = new (string zone, int y, int m, int d, int h, int mi, int offset)[] {
                ("America/New_York",1990,7,15,12,0,-14400), ("America/New_York",1990,1,15,12,0,-18000), ("America/New_York",2007,3,11,2,30,-18000), ("America/New_York",2007,11,4,1,30,-14400),
                ("Europe/London",1970,1,1,12,0,3600), ("Europe/London",1940,5,1,12,0,3600), ("Asia/Kolkata",2000,1,1,0,0,19800), ("Australia/Sydney",2000,1,15,9,0,39600), ("America/Phoenix",1990,7,15,12,0,-25200),
                ("Europe/Berlin",1945,6,1,12,0,10800), ("Asia/Kathmandu",1990,6,1,6,0,20700), ("America/Sao_Paulo",1995,12,1,12,0,-7200), ("Pacific/Honolulu",1961,8,4,19,24,-36000), ("Asia/Shanghai",1988,7,1,12,0,32400),
                ("Africa/Lagos",1980,3,3,3,3,3600), ("America/St_Johns",2000,7,1,12,0,-9000), ("Asia/Tehran",2010,7,1,12,0,16200), ("Australia/Adelaide",2010,1,1,12,0,37800), ("America/Caracas",2010,6,1,12,0,-16200), ("America/Los_Angeles",1918,4,1,12,0,-25200), ("Europe/Moscow",2012,1,1,12,0,14400) };
            var wrong = clocks.Where(c => { int zone = Places.ZoneOf(c.zone); long local = (long)(new DateTime(c.y, c.m, c.d, 0, 0, 0, DateTimeKind.Utc) - new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMinutes + c.h * 60 + c.mi; return zone < 0 || local - Places.LocalToUt(zone, c.y, c.m, c.d, c.h * 60 + c.mi) != c.offset / 60; }).Select(c => c.zone + " " + c.y).ToList();
            Check(wrong.Count == 0, "a birth's local time becomes UT with the clocks of its year, as Python's zoneinfo has them: 21 cases, a skipped hour, a repeated one, Berlin's 1945 double summer time, Kathmandu's +5:45 and Caracas's -4:30 included" + (wrong.Count > 0 ? " (wrong: " + string.Join(", ", wrong) + ")" : ""));
            // a published chart (Astro-Databank: Barack Obama, Aug 4, 1961, 19:24 HST, Honolulu: Sun 12°33' Leo, Moon 3°21' Gemini, Ascendant 18°03' Aquarius)
            var honolulu = Places.Find("Honolulu, HI, United States"); var obama = BirthChart.Work(1961, 8, 4, 19 * 60 + 24, honolulu);
            Check(honolulu != null && obama.Sun == 4 && obama.Moon == 2 && obama.Rising == 10, "a published chart comes out right: Honolulu, Aug 4 1961 at 19:24, Leo sun, Gemini moon, Aquarius rising");
            var london = Places.Find("London, Britain (UK)"); var full = BirthChart.Work(1990, 4, 25, 14 * 60 + 30, london); var kept = BirthChart.Work(1990, 5, 2, -1, london); var changed = BirthChart.Work(1990, 5, 1, -1, london);
            Check(full.Sun == 1 && full.Moon == 1 && full.Rising == 5 && kept.Sun == 1 && kept.Moon == 4 && kept.Rising == -1 && changed.Sun == 1 && changed.Moon == -1 && changed.Rising == -1, "with no birth time (owner, A) the rising sign is unknown, and the moon is not worked out on a day it changed sign: London, May 2 1990 keeps a Leo moon; May 1 runs Cancer to Leo");
            // the cusp day (owner ruling, Oct 2 evening; canon: the sun is Uncertain that day): the player is asked, with the minute it changed on the birth place's clock
            var cusp = BirthChart.Work(1990, 4, 20, -1, london); var ingress = Places.UtOf(cusp.Ingress); // JPL Horizons: the sun reached 30° between 08:26 and 08:27 UT
            Check(cusp.Sun == -1 && cusp.SunFrom == 0 && cusp.SunTo == 1 && ingress.Hour == 8 && ingress.Minute == 27 && BirthChart.LocalMinuteOf(cusp.Ingress, london.Zone) == 9 * 60 + 27 && SliceFlow.ClockTime(9 * 60 + 27) == "9:27 am" && SliceFlow.ClockTime(0) == "12:00 am" && SliceFlow.ClockTime(13 * 60 + 5) == "1:05 pm",
                "a cusp day: London, Apr 20 1990, the sun entered Taurus at 08:27 UT (JPL Horizons' minute), 9:27 am on London's summer clock");
            SliceFlow Cusp() { var c = new SliceFlow(() => 1); c.SkipPrologue(); c.Continue(); c.ChooseBirth("chart"); c.SetBirthDate(1990, 4, 20); c.SetBirthTime(-1); c.SetBirthPlace(london); return c; }
            var asked = Cusp(); var picked = Cusp(); var unsure = Cusp();
            Check(asked.BirthStep == "cusp" && !asked.CanContinue && !asked.HasSunSign && asked.CuspQuestion == "The Sun moved from Aries into Taurus on the day you were born, at 9:27 am. Your birth time decides which side of that line you landed on. Which sign do you go by?" && SliceFlow.CuspWhy.StartsWith("The Sun reaches each sign") && !asked.PickCuspSun(2),
                "the owner's question, with the two signs and the local time: " + asked.CuspQuestion);
            Check(picked.PickCuspSun(1) && picked.BirthStep == "moon" && !picked.CanContinue && !picked.PickMoon(-1) && picked.PickMoon(0) && picked.BirthStep == "rising-pick" && !picked.CanContinue && picked.PickRising(5) && picked.SunSign == 1 && picked.SunBasis == "picked" && picked.CanContinue && picked.RisingSign == 5 && picked.RisingChosen && picked.BigThreeLine.StartsWith("☉ Taurus · ☽ ") && picked.BigThreeLine.EndsWith("↑ Virgo") && !picked.BigThreeLine.Contains("unknown"),
                "a cusp pick is the sun, flagged the player's; then the moon and the rising are picked (Oct 7, 86bced0tc: no unknowns): " + picked.BigThreeLine);
            Check(!unsure.PickCuspSun(-1) && unsure.BirthStep == "cusp" && !unsure.HasSunSign && cusp.SunNoon >= 0, "the cusp's I'm not sure is removed (owner, Oct 7, 86bced0tc): only its two signs answer; the noon sign stays readable in older saves");
            var cuspSave = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(picked.ToSave(new bool[12], new bool[12], true))); cuspSave.atriumStage = 2; var cuspBack = new SliceFlow(() => 1);
            Check(cuspSave.sunBasis == "picked" && cuspBack.Restore(cuspSave) && cuspBack.SunBasis == "picked" && cuspBack.SunSign == 1, "the save keeps the pick's flag both ways");
            BirthChart.CuspSample(out int sy, out int sm, out int sd, out var samplePlace); var sample = new SliceFlow(() => 1); sample.SkipPrologue(); sample.Continue(); sample.ChooseBirth("chart"); sample.SetBirthDate(sy, sm, sd); sample.SetBirthTime(-1); sample.SetBirthPlace(samplePlace);
            Check(sample.BirthStep == "cusp" && sample.CuspFrom == 0 && sample.CuspTo == 1, "DEV Mode's cusp-day sample lands on the question (London, Apr 20 1990, no birth time)");
            Check(Enumerable.Range(0, 12).All(sun => { BirthChart.Sample(sun, out int y, out int m, out int d, out int minute, out var place); var c = BirthChart.Work(y, m, d, minute, place); return c.Sun == sun && c.Moon >= 0 && c.Rising >= 0 && place.Name == "London, Britain (UK)"; }), "DEV Mode's sample chart (owner, Oct 2 evening) is a real chart for each sun sign: a 1990 birth mid-sign, at noon in London");
            var dev = DevCheckpoints.Play("key1", "Tester", 4);
            Check(dev.chartFrom == "chart" && dev.sunSign == 4 && dev.moonSign >= 0 && dev.risingSign >= 0 && dev.birthPlace == "London, Britain (UK)" && dev.birthMinute == 720, "DEV Mode's Jump to... carries the sample chart in its save, the player's sun sign kept");
            // the save: the chart and the birth data both ways; a save from before reads its moon and rising as unknown
            var flow = new SliceFlow(() => 1); flow.SkipPrologue(); flow.Continue(); flow.ChooseBirth("chart"); flow.SetBirthDate(1990, 4, 25); flow.SetBirthTime(14 * 60 + 30); flow.SetBirthPlace(london);
            var save = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(flow.ToSave(new bool[12], new bool[12], true))); save.atriumStage = 2; var back = new SliceFlow(() => 1);
            Check(save.chartFrom == "chart" && save.birthDate == "1990-04-25" && save.birthMinute == 870 && save.birthZone == "Europe/London" && back.Restore(save) && back.SunSign == 1 && back.MoonSign == 1 && back.RisingSign == 5 && back.BirthPlace.Name == "London, Britain (UK)" && back.BigThreeLine == "☉ Taurus · ☽ Taurus · ↑ Virgo", "the save keeps the chart with the birth data it came from, and reads it back without working it out again: " + back.BigThreeLine);
            var old = JsonUtility.FromJson<SaveData>("{\"version\":4,\"playerName\":\"Old\",\"sunSign\":1,\"atriumStage\":2,\"keyEarned\":true}"); var before = new SliceFlow(() => 1);
            Check(old.moonSign == -1 && old.risingSign == -1 && before.Restore(old) && before.BigThreeLine == "☉ Taurus · ☽ unknown · ↑ unknown", "a save from before this build shows the sun only (owner, A): " + before.BigThreeLine);
            var noTime = new SliceFlow(() => 1); noTime.SkipPrologue(); noTime.Continue(); noTime.ChooseBirth("chart"); noTime.SetBirthDate(1990, 5, 2); noTime.SetBirthTime(-1); noTime.SetBirthPlace(london);
            Check(noTime.BirthStep == "rising-pick" && !noTime.CanContinue && noTime.PickRising(5) && noTime.BigThreeLine == "☉ Taurus · ☽ Leo · ↑ Virgo" && noTime.Note == "Your sun sign is Taurus, your moon sign Leo, and your rising sign Virgo." && noTime.CanContinue && !noTime.MoonPair && noTime.RisingChosen,
                "no birth time: the sun and moon worked out, the rising picked (Oct 7, 86bced0tc), the note in full: " + noTime.BigThreeLine);
            // owner, Oct 3 (the canon flags): on a day the moon changed sign, with no birth time, the record shows both signs, "the way astrologers write it"
            Check(changed.MoonFrom == 3 && changed.MoonTo == 4 && kept.MoonFrom == 4 && kept.MoonTo == 4 && full.MoonFrom == -1 && full.MoonTo == -1, "the day's two moons are kept: London, May 1 1990 runs from Cancer into Leo; May 2 holds Leo all day; a known time works out one moon");
            // owner, Oct 3: "Pisces or Aries", now reached only by an older save (owner, Oct 7: old saves stay as they are); a version 4 save converted
            var pair = new SliceFlow(() => 1); pair.Restore(JsonUtility.FromJson<SaveData>("{\"version\":4,\"playerName\":\"Old\",\"atriumStage\":2,\"keyEarned\":true,\"chartFrom\":\"chart\",\"sunSign\":1,\"moonSign\":-1,\"moonFrom\":3,\"moonTo\":4,\"birthDate\":\"1990-05-01\",\"birthMinute\":-1,\"birthPlace\":\"London, Britain (UK)\",\"birthZone\":\"Europe/London\",\"birthLatitude\":51.5085,\"birthLongitude\":-0.1257}"));
            // the owner, Oct 7 ("Ask, like the cusp Sun"): an uncertain moon is asked; a pick is the player's moon, "I'm not sure" keeps both
            var moonPick = new SliceFlow(() => 1); moonPick.SkipPrologue(); moonPick.Continue(); moonPick.ChooseBirth("chart"); moonPick.SetBirthDate(1990, 5, 1); moonPick.SetBirthTime(-1); moonPick.SetBirthPlace(london);
            Check(moonPick.BirthStep == "moon" && !moonPick.CanContinue && moonPick.MoonQuestion == "Your Moon was in Cancer or Leo that day. Which do you go by?" && !moonPick.PickMoon(2) && !moonPick.PickMoon(-1) && moonPick.PickMoon(1) && moonPick.BirthStep == "rising-pick" && moonPick.PickRising(5) && moonPick.MoonSign == 4 && moonPick.ChoiceFor("moon").how == "picked" && moonPick.BigThreeLine == "☉ Taurus · ☽ Leo · ↑ Virgo" && moonPick.Note == "Your sun sign is Taurus, your moon sign Leo, and your rising sign Virgo." && moonPick.CanContinue,
                "Oct 7: the moon asked like the cusp sun (" + moonPick.MoonQuestion + "); a pick shows like any moon, flagged picked: " + moonPick.BigThreeLine);
            Check(pair.MoonSign == -1 && pair.MoonPair && pair.MoonWords == "Cancer or Leo" && pair.BigThreeLine == "☉ Taurus · ☽ Cancer or Leo · ↑ unknown", "owner, Oct 3: an older save's moon that changed sign that day shows both, as before: " + pair.BigThreeLine);
            var pairSave = UnityEngine.JsonUtility.FromJson<SaveData>(UnityEngine.JsonUtility.ToJson(pair.ToSave(new bool[12], new bool[12], true))); pairSave.atriumStage = 2; pairSave.sunSign = pair.SunSign; var pairBack = new SliceFlow(() => 1);
            var olderJson = System.Text.RegularExpressions.Regex.Replace(UnityEngine.JsonUtility.ToJson(pairSave), "\"moon(From|To)\":-?\\d+,?", "").Replace("\"version\":5", "\"version\":4"); var olderPair = new SliceFlow(() => 1);
            Check(pairBack.Restore(pairSave) && pairBack.MoonWords == "Cancer or Leo" && pairBack.BigThreeLine == pair.BigThreeLine && olderPair.Restore(UnityEngine.JsonUtility.FromJson<SaveData>(olderJson)) && olderPair.MoonWords == "Cancer or Leo", "the save keeps both moons and reads them back; a version 4 save from before #116 is converted once and its chart worked out again from its facts (Oct 7), so its moon now shows both signs");
            // the record's line: a long one shrinks to fit the page's calm column (274 px), never below 12 px
            var probeObject = new GameObject("Big Three probe", typeof(RectTransform)); var probe = probeObject.AddComponent<UnityEngine.UI.Text>(); probe.horizontalOverflow = HorizontalWrapMode.Overflow; var italic = Resources.Load<Font>(SliceView.ItalicFont);
            float Width(string words, float k) { float w = 0; var fonts = new[] { Resources.Load<Font>(SliceView.SunFont), Resources.Load<Font>("Fonts/NotoSansSymbols"), ButtonLook.EngravedFont }; var parts = words.Split(new[] { " \u00b7 " }, StringSplitOptions.None);
                for (int i = 0; i < 3; i++) { probe.font = fonts[i]; probe.fontSize = Mathf.RoundToInt(SliceView.BigThreeMarkSizes[i] * k); w += probe.cachedTextGeneratorForLayout.GetPreferredWidth(SliceView.BigThreeMarks[i], probe.GetGenerationSettings(Vector2.zero)) / probe.pixelsPerUnit;
                    probe.font = italic; probe.fontSize = Mathf.RoundToInt(17 * k); w += probe.cachedTextGeneratorForLayout.GetPreferredWidth(" " + parts[i].Substring(2) + (i < 2 ? " \u00b7 " : ""), probe.GetGenerationSettings(Vector2.zero)) / probe.pixelsPerUnit; }
                return w; }
            string longest = "☉ Sagittarius · ☽ Sagittarius or Capricorn · ↑ unknown"; float atFull = Width(longest, 1), atSmallest = Width(longest, SliceView.BigThreeSmallest / 17f), usual = Width(pair.BigThreeLine, 1);
            string three = "☉ Sagittarius · ☽ Sagittarius, Capricorn or Aquarius · ↑ unknown"; float threeSmallest = Width(three, SliceView.BigThreeSmallestThree / 17f), threeAtFloor = Width(three, SliceView.BigThreeSmallest / 17f); // Oct 7: a moon with three options, rarely (no time, no place)
            UnityEngine.Object.DestroyImmediate(probeObject);
            Check(threeAtFloor > SliceView.BigThreeRoom && threeSmallest <= SliceView.BigThreeRoom, "owner, Oct 7: the longest three-option moon (\"" + three + "\", " + threeAtFloor.ToString("0") + " px at 11) fits inside the page's border at its own 9 px floor: " + threeSmallest.ToString("0") + " px of " + SliceView.BigThreeRoom);
            Check(atSmallest <= SliceView.BigThreeRoom && atFull > SliceView.BigThreeRoom, "the longest such line (\"" + longest + "\", " + atFull.ToString("0") + " px at 17) fits the page's calm column at " + SliceView.BigThreeSmallest + " px (" + atSmallest.ToString("0") + " of " + SliceView.BigThreeRoom + "); this one is " + usual.ToString("0") + " px at 17");
            // the record's glyphs, each in a font that carries it (the zodiac font bakes its set: ☽ joins it, FontSetup)
            var symbols = Resources.Load<Font>("Fonts/NotoSansSymbols"); var sunFont = Resources.Load<Font>(SliceView.SunFont);
            Check(sunFont != null && sunFont.HasCharacter('☉') && symbols != null && symbols.HasCharacter('☽') && symbols.HasCharacter('♈') && ButtonLook.EngravedFont.HasCharacter('↑') && FontSetup.SymbolSet.Contains('☽'), "the Big Three's glyphs: ☉ in Noto Sans Symbols 2, ☽ in the zodiac font's set (with the twelve signs), ↑ in EB Garamond");
        }
        static bool HasTable(string path, string tag) { var b = System.IO.File.ReadAllBytes(path); int n = (b[4] << 8) | b[5]; for (int i = 0; i < n; i++) if (System.Text.Encoding.ASCII.GetString(b, 12 + i * 16, 4) == tag) return true; return false; }
        // The birth-time build (owner, Oct 7: the recommendation on 86bceb6fq approved). The save keeps the facts, the player's choices and the
        // worked-out chart apart; the record reads worked out over chosen over unknown; the game never invents a time or a place; adding a fact
        // only narrows what was worked out; nothing played changes; a version 4 save converts once.
        static SliceFlow BirthPlayer(Action<SliceFlow> opening, out SaveData save)
        {
            // the opening as a player gives it, the After Key 1 checkpoint's progress under it, restored as a second sitting, the journal on "Your Birth"
            var o = new SliceFlow(); o.SkipPrologue(); o.Continue(); opening(o);
            save = DevCheckpoints.Play("key1", "Tester", o.LessonSun); o.WriteBirth(save); save = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(save));
            var f = new SliceFlow(); Check(f.Restore(save), "a version 5 save restores (" + save.chartFrom + ")"); f.OpenJournal(); f.JournalLand(); f.OpenBirthPage(); return f;
        }
        static void ValidateBirthRecord()
        {
            var london = Places.Find("London, Britain (UK)");
            bool Within(ChartPoint inner, ChartPoint outer) => inner.signs.All(x => outer.signs.Contains(x));
            // the window: with no place, every zone in the bundled table; what holds across all of it, and nothing invented
            Places.OffsetRange(out int west, out int east);
            Check(west <= -11 * 60 && east >= 14 * 60, "the bundled zones run from UTC" + (west / 60.0).ToString("+0.#;-0.#") + " to UTC+" + (east / 60.0).ToString("0.#") + ": a birth with no place could be in any of them");
            var timed = BirthChart.Work(BirthFacts.From(1990, 5, 10, 14 * 60 + 30, london)).Worked; var noPlace = BirthChart.Work(BirthFacts.From(1990, 5, 10, 14 * 60 + 30, null)).Worked;
            var day = BirthChart.Work(BirthFacts.From(1990, 5, 10, -1, london)).Worked; var nothing = BirthChart.Work(BirthFacts.From(1990, 5, 10, -1, null)).Worked;
            Check(timed.maths == BirthChart.Maths && timed.Point("sun").status == "exact" && timed.Point("moon").status == "exact" && timed.Point("rising").status == "exact" && timed.Point("sun").from == timed.Point("sun").to,
                "an exact time and a place: every point exact, one degree each");
            Check(noPlace.Point("rising") == null && noPlace.Point("sun").status == "stable" && noPlace.Point("sun").signs.Single() == 1 && Within(timed.Point("moon"), noPlace.Point("moon")) && noPlace.Point("moon").status != "exact",
                "a time with no place: no rising (never invented), the sun stable in Taurus across every zone, the moon's options include London's (" + SliceFlow.Options(noPlace.Point("moon").signs.Length > 1 ? noPlace.Point("moon").signs : new[] { noPlace.Point("moon").signs[0], noPlace.Point("moon").signs[0] }) + ")");
            Check(day.Point("rising") == null && nothing.Point("rising") == null && Within(day.Point("moon"), nothing.Point("moon")) && Within(day.Point("sun"), nothing.Point("sun")) && BirthChart.Work(new BirthFacts()).Worked.points.Length == 0,
                "no time: the whole local day; no time and no place: the day in every zone, a wider window that keeps every sign the narrower one could be; no date: nothing worked out at all");
            Check(SliceFlow.Options(new[] { 11, 0 }) == "Pisces or Aries" && SliceFlow.Options(new[] { 8, 9, 10 }) == "Sagittarius, Capricorn or Aquarius", "an uncertain point reads as its options, \"or\" before the last");
            // the opening: "I don't know where", and a cusp day with no place (no clock to give the minute on)
            var where = new SliceFlow(); where.SkipPrologue(); where.Continue(); where.ChooseBirth("chart"); where.SetBirthDate(1990, 5, 10); where.SetBirthTime(14 * 60 + 30);
            Check(where.SetBirthPlace(null) && where.BirthStep == "moon" && where.PickMoon(0) && where.BirthStep == "rising-pick" && where.PickRising(5) && where.BirthDone && where.RisingChosen && where.Facts.ExactTime && !where.Facts.HasPlace && !where.BigThreeLine.Contains("unknown") && where.CanContinue,
                "Oct 7: I don't know where keeps the time and works out what holds in every zone; the moon and the rising are picked: " + where.BigThreeLine);
            var cuspNoPlace = new SliceFlow(); cuspNoPlace.SkipPrologue(); cuspNoPlace.Continue(); cuspNoPlace.ChooseBirth("chart"); cuspNoPlace.SetBirthDate(1990, 4, 20); cuspNoPlace.SetBirthTime(-1); cuspNoPlace.SetBirthPlace(null);
            Check(cuspNoPlace.BirthStep == "cusp" && cuspNoPlace.CuspMinute == -1 && !cuspNoPlace.CuspQuestion.Contains(" at ") && cuspNoPlace.CuspQuestion.Contains("birth time and place decide") && cuspNoPlace.PickCuspSun(0) && (cuspNoPlace.BirthStep != "moon" || cuspNoPlace.PickMoon(0)) && cuspNoPlace.BirthStep == "rising-pick" && cuspNoPlace.PickRising(0) && cuspNoPlace.SunBasis == "picked" && cuspNoPlace.BirthDone,
                "a cusp day with no place asks without a clock time; then the moon, then the rising: " + cuspNoPlace.CuspQuestion);
            // the save: three parts that round-trip, the record read from them, and the flat copies an older build reads
            var full = new SliceFlow(); full.SkipPrologue(); full.Continue(); full.ChooseBirth("chart"); full.SetBirthDate(1990, 4, 20); full.SetBirthTime(-1); full.SetBirthPlace(london); full.PickCuspSun(0); full.PickMoon(0); full.PickRising(4);
            var fullSave = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(full.ToSave(new bool[12], new bool[12], true))); fullSave.atriumStage = 2; var fullBack = new SliceFlow();
            Check(fullSave.version == 5 && fullSave.birth.date == "1990-04-20" && fullSave.birth.timeFrom == -1 && fullSave.birth.place == london.Name && fullSave.choices.Length == 3 && fullSave.choices.All(c => c.how == "picked") && fullSave.choices.First(c => c.point == "rising").sign == 4 && fullSave.chart.Point("sun").status == "uncertain" && fullSave.chart.Point("moon").status == "uncertain"
                && fullSave.sunSign == 0 && fullSave.sunBasis == "picked" && fullBack.Restore(fullSave) && fullBack.SunSign == 0 && fullBack.SunBasis == "picked" && fullBack.BigThreeLine == full.BigThreeLine,
                "version 5 keeps the facts, the choices and the chart apart (the cusp's Aries, the first moon and Leo rising picked; the sun and moon points uncertain), writes the flat copies, and reads the record back: " + fullBack.BigThreeLine);
            // a version 4 save converts once: each old path
            SaveData V4(string json) { var v = JsonUtility.FromJson<SaveData>("{\"version\":4,\"playerName\":\"Old\",\"atriumStage\":2,\"keyEarned\":true," + json + "}"); return v; }
            var known = new SliceFlow(); var chosen = new SliceFlow(); var legacy = new SliceFlow(); var chart = new SliceFlow(); var moved = new SliceFlow();
            Check(known.Restore(V4("\"chartFrom\":\"known\",\"sunSign\":4,\"moonSign\":7,\"risingSign\":-1")) && known.ChoiceFor("sun").how == "entered" && known.ChoiceFor("moon").sign == 7 && known.ChoiceFor("rising") == null && known.BigThreeLine == "☉ Leo · ☽ Scorpio · ↑ unknown" && !known.Facts.Any,
                "a version 4 known-path save keeps its signs as the player's choices, entered: " + known.BigThreeLine);
            Check(chosen.Restore(V4("\"chartFrom\":\"chosen\",\"sunSign\":9")) && chosen.ChoiceFor("sun").how == "assigned" && chosen.SunSign == 9 && legacy.Restore(V4("\"sunSign\":2")) && legacy.ChoiceFor("sun").how == "legacy" && legacy.SunSign == 2,
                "the random sun's save keeps its sun, tagged assigned; a save from before #110, legacy");
            Check(chart.Restore(V4("\"chartFrom\":\"chart\",\"sunSign\":1,\"sunBasis\":\"picked\",\"moonSign\":-1,\"birthDate\":\"1990-04-20\",\"birthMinute\":-1,\"birthPlace\":\"London, Britain (UK)\",\"birthZone\":\"Europe/London\",\"birthLatitude\":51.5085,\"birthLongitude\":-0.1257"))
                && chart.Facts.date == "1990-04-20" && chart.Chart.Point("sun").status == "uncertain" && chart.ChoiceFor("sun").how == "picked" && chart.SunSign == 1 && (chart.MoonSign >= 0 || chart.MoonPair),
                "a version 4 chart-path save moves its facts over and works the chart out again; the cusp's pick stays a choice: " + chart.BigThreeLine);
            Check(moved.Restore(V4("\"chartFrom\":\"chart\",\"sunSign\":5,\"birthDate\":\"1990-05-10\",\"birthMinute\":870,\"birthPlace\":\"London, Britain (UK)\",\"birthZone\":\"Europe/London\",\"birthLatitude\":51.5085,\"birthLongitude\":-0.1257")) && moved.SunSign == 5,
                "where the saved sign and the maths disagree, the saved sign wins (Oct 3: never change data already calculated)");
            // "Your Birth": the skip path's three picks, then the rising changed, then the facts added
            var p = BirthPlayer(o => { o.ChooseBirth("skip"); o.PickSkipSun(7); o.PickSkipMoon(3); o.PickRising(6); o.Continue(); }, out var pickedSave);
            Check(pickedSave.sunSign == 7 && pickedSave.choices.Length == 3 && pickedSave.choices.All(x => x.how == "picked") && p.HasSunSign && p.LessonSun == 7 && p.OnBirthPage && p.CanAddFacts && p.CanAddTime && p.CanChooseRising && p.RisingChosen && p.ShowsRisingWhy && p.BigThreeLine == "☉ Scorpio · ☽ Cancer · ↑ Libra",
                "the skip path's three picks survive the save, flagged picked; \"Your Birth\" offers Add, Add my birth time and Change my rising: " + p.BigThreeLine);
            int keys = p.Keys, sittings = p.Sittings; string deck = string.Join("|", p.Deck.Items.Select(i => i.seat + ":" + i.kind + ":" + i.state + ":" + i.streak + ":" + i.interval + ":" + i.dueDay + ":" + i.entered));
            Check(p.StartChooseRising() && p.Screen == SliceScreen.Birth && p.Amending && p.BirthStep == "rising-pick" && !p.CanContinue && p.PickSign(4) && p.OnBirthPage && p.BirthLines.SequenceEqual(new[] { "Leo it is." }) && p.RisingSign == 4 && p.RisingChosen && p.BigThreeLine == "☉ Scorpio · ☽ Cancer · ↑ Leo",
                "Change my rising: the twelve signs, then back on the page with \"Leo it is.\"; the record shows ↑ Leo with no label, the flag hidden");
            Check(p.StartChooseRising() && p.PickSign(6) && p.RisingSign == 6 && p.ChoiceFor("rising").how == "picked", "a chosen rising can be changed at any time, with no birth time (open call 1)");
            Check(p.StartAddFacts(true) && p.BirthStep == "date" && p.Note == SliceFlow.NeedDayAndPlace && p.CancelAmending() && p.OnBirthPage && !p.Facts.Any && p.BirthLines.Count == 0 && p.RisingSign == 6, "Add my birth time with no birthday asks the day first, with the journal's line; going back changes nothing");
            Check(p.StartAddFacts(true) && p.SetBirthDate(1990, 4, 25) && p.BirthStep == "time" && p.SetBirthTime(14 * 60 + 30) && p.BirthStep == "place" && p.SetBirthPlace(london) && p.OnBirthPage && !p.Amending,
                "Add: the opening's boxes for whatever is missing, in order (open call 2), then back to the page");
            Check(p.BirthLines.SequenceEqual(new[] { "Your rising is Virgo.", "It takes the place of the one you chose.", "Your moon is Taurus.", "Your sun is Taurus.", "Everything you've learned stays as it is." }) && p.RisingSign == 5 && !p.RisingChosen && p.ChoiceFor("rising") == null && p.ChoiceFor("sun") == null && p.LessonSun == 1,
                "the facts decide: the worked-out rising takes the chosen one's place, the moon and the sun follow the facts, a line for each: " + string.Join(" / ", p.BirthLines));
            Check(p.Keys == keys && p.Sittings == sittings && deck == string.Join("|", p.Deck.Items.Select(i => i.seat + ":" + i.kind + ":" + i.state + ":" + i.streak + ":" + i.interval + ":" + i.dueDay + ":" + i.entered)) && !p.CanAddFacts && !p.CanChooseRising && !p.ShowsRisingWhy,
                "nothing played changes: the Keys, the sittings and every deck item are as they were; with every fact in, Add and the rising choice are gone");
            // a time added to a cusp day settles the sun (open call 3): worked out beats chosen
            var c = BirthPlayer(o => { o.ChooseBirth("chart"); o.SetBirthDate(1990, 4, 20); o.SetBirthTime(-1); o.SetBirthPlace(london); o.PickCuspSun(0); o.PickMoon(0); o.PickRising(4); o.Continue(); }, out _);
            Check(c.SunSign == 0 && c.SunBasis == "picked" && c.StartAddFacts(true) && c.BirthStep == "time" && c.SetBirthTime(10 * 60) && c.OnBirthPage && c.SunSign == 1 && c.SunBasis == "" && c.BirthLines.Contains("At that minute, your sun was in Taurus.") && c.RisingSign >= 0 && c.BirthLines[0] == SliceFlow.RisingIs(c.RisingSign),
                "a birth time added to a cusp day settles the sun (10 am, after 9:27: Taurus) and works out the rising: " + string.Join(" / ", c.BirthLines));
            // Jeffrey, #123 B1: going back after the cusp question changes nothing; the picks are held until the steps finish
            var back = BirthPlayer(o => { o.ChooseBirth("skip"); o.PickSkipSun(4); o.PickSkipMoon(3); o.PickRising(6); o.Continue(); }, out _); var backChoices = string.Join("|", back.Choices.Select(x => x.point + x.sign + x.how));
            Check(back.StartAddFacts() && back.SetBirthDate(1990, 4, 20) && back.SetBirthTime(-1) && back.SetBirthPlace(london) && back.BirthStep == "cusp" && back.PickCuspSun(0) && back.BirthStep == "moon" && back.SunSign == 4 && back.CancelAmending()
                && back.SunSign == 4 && back.SunBasis == "picked" && !back.Facts.Any && string.Join("|", back.Choices.Select(x => x.point + x.sign + x.how)) == backChoices && back.BirthLines.Count == 0,
                "going back from the Moon's question after a cusp pick leaves the record as it was: Leo picked, no facts");
            Check(back.StartAddFacts() && back.SetBirthDate(1990, 4, 20) && back.SetBirthTime(-1) && back.SetBirthPlace(london) && back.PickCuspSun(0) && back.PickMoon(0) && back.SunSign == 0 && back.BirthLines.Contains("Your sun is Aries.") && back.BirthLines.Contains(SliceFlow.MoonIs(back.MoonSign)),
                "a picked sun moved by the cusp question gets its line too: " + string.Join(" / ", back.BirthLines));
            // a moon picked at the opening gives way to the one a birth time works out (worked out beats chosen)
            var mp = BirthPlayer(o => { o.ChooseBirth("chart"); o.SetBirthDate(1990, 5, 1); o.SetBirthTime(-1); o.SetBirthPlace(london); o.PickMoon(0); o.PickRising(4); o.Continue(); }, out var mpSave);
            Check(mpSave.choices.Any(x => x.point == "moon" && x.sign == 3 && x.how == "picked") && mp.MoonSign == 3 && mp.StartAddFacts(true) && mp.SetBirthTime(14 * 60 + 30) && mp.MoonSign == 4 && mp.ChoiceFor("moon") == null && mp.BirthLines.Contains("Your moon is Leo."),
                "a moon picked as Cancer gives way to the Leo a birth time works out, with its line: " + string.Join(" / ", mp.BirthLines));
            // a fact added never moves a sign already worked out, only narrows it
            var n = BirthPlayer(o => { o.ChooseBirth("chart"); o.SetBirthDate(1990, 5, 2); o.SetBirthTime(-1); o.SetBirthPlace(london); o.PickRising(4); o.Continue(); }, out _);
            var sunBefore = n.Chart.Point("sun"); var moonBefore = n.Chart.Point("moon");
            Check(n.StartAddFacts() && n.SetBirthTime(23 * 60) && n.SunSign == 1 && n.MoonSign == 4 && n.Chart.Point("sun").status == "exact" && n.Chart.Point("sun").signs[0] == sunBefore.signs[0] && n.Chart.Point("moon").signs[0] == moonBefore.signs[0] && n.BirthLines[0] == SliceFlow.RisingIs(n.RisingSign) && n.BirthLines.Last() == SliceFlow.StaysLearned && !n.BirthLines.Any(x => x.StartsWith("Your moon") || x.StartsWith("Your sun") || x.StartsWith("At that")),
                "a time added keeps the sun and moon already worked out (now exact), adds the rising, and says only what changed");
            // a time with no place: Add asks only the place
            var w = BirthPlayer(o => { o.ChooseBirth("chart"); o.SetBirthDate(1990, 5, 10); o.SetBirthTime(14 * 60 + 30); o.SetBirthPlace(null); o.PickMoon(0); o.PickRising(4); o.Continue(); }, out _);
            Check(w.CanAddFacts && !w.CanAddTime && w.StartAddFacts() && w.BirthStep == "place" && w.SetBirthPlace(london) && w.RisingSign >= 0 && w.Facts.HasPlace, "a time with no place: Add asks only the place, and the rising follows");
            // Jeffrey, #125: older saves still read as they were (owner, Oct 7: "Leave them; I'll start fresh"), and an Add on one asks what it lacks
            var declined = JsonUtility.FromJson<SaveData>("{\"version\":5,\"playerName\":\"Old\",\"atriumStage\":2,\"keyEarned\":true,\"sunSign\":-1,\"chartFrom\":\"skip\",\"choices\":[{\"point\":\"sun\",\"sign\":-1,\"how\":\"declined\"}]}"); var dFlow = new SliceFlow();
            Check(dFlow.Restore(declined) && !dFlow.HasSunSign && dFlow.LessonSun == 0 && dFlow.SunBasis == "" && dFlow.BigThreeLine == "☉ unknown · ☽ unknown · ↑ unknown", "a #123 save with a declined sun still reads: no sun, the lessons from Aries, the record as it was");
            var oldSave = DevCheckpoints.Play("key1", "Tester", 1); oldSave.version = 4; oldSave.birth = new BirthFacts(); oldSave.choices = new ChoiceRecord[0]; oldSave.chart = new WorkedChart(); oldSave.chartFrom = "chart"; oldSave.sunSign = 1; oldSave.sunBasis = "picked"; oldSave.moonSign = oldSave.risingSign = oldSave.moonFrom = oldSave.moonTo = -1;
            oldSave.birthDate = "1990-04-20"; oldSave.birthMinute = -1; oldSave.birthPlace = london.Name; oldSave.birthZone = "Europe/London"; oldSave.birthLatitude = london.Latitude; oldSave.birthLongitude = london.Longitude; var oldJson = JsonUtility.ToJson(oldSave);
            SliceFlow Old() { var f = new SliceFlow(); f.Restore(JsonUtility.FromJson<SaveData>(oldJson)); f.OpenJournal(); f.JournalLand(); f.OpenBirthPage(); return f; }
            var o1 = Old();
            Check(o1.RisingSign == -1 && o1.MoonPair && o1.CanChooseRising && o1.StartAddFacts() && o1.BirthStep == "time" && o1.SetBirthTime(-1) && o1.BirthStep == "moon" && o1.PickMoon(0) && o1.BirthStep == "rising-pick" && o1.CancelAmending() && o1.RisingSign == -1 && o1.MoonPair && o1.BirthLines.Count == 0 && !o1.Facts.HasTime,
                "an older save with an \"or\" moon and no rising: an Add asks the moon, then the rising; going back at the rising changes nothing");
            var o2 = Old();
            Check(o2.StartAddFacts() && o2.SetBirthTime(-1) && o2.PickMoon(1) && o2.PickRising(4) && o2.OnBirthPage && o2.RisingSign == 4 && o2.RisingChosen && o2.BirthLines.Contains("Leo it is.") && o2.BirthLines.Contains(SliceFlow.MoonIs(o2.MoonSign)) && !o2.BigThreeLine.Contains("unknown"),
                "the same Add, answered: the moon and the rising picked, a line for each, and no unknown left: " + string.Join(" / ", o2.BirthLines));
            // the lessons: with no sun they start from Aries and call it the starting sign, never the player's sun
            var lesson = new DialLesson(() => 0); lesson.SetSunSign(0, false); string intro = (string)typeof(DialLesson).GetMethod("IntroLine", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(lesson, new object[] { 99 });
            Check(lesson.Sun == 0 && !lesson.SunKnown && intro.StartsWith("We shall start from Aries, the first sign on the wheel.") && !intro.Contains("Your sun sign"), "with no sun, Caspar names Aries as the starting sign: " + intro);
            lesson.UpdateSun(4, true); Check(lesson.Sun == 4 && lesson.SunKnown, "a sun that arrives later is the start for lessons not yet played");
        }
        static void ValidateJournalFront()
        {
            var f = new SliceFlow(() => 1); f.SetName("Davon"); f.SkipPrologue(); f.Continue(); f.ChooseBirth("skip"); f.PickSkipSun(1); f.PickSkipMoon(3); f.PickRising(4); f.Continue(); f.Continue(); f.EnterWing(); f.EnterDial(); f.RevealKey(); f.Continue(); f.Continue(); f.InsertKey(); f.End(); f.Continue(); // the Hub, the journal in hand
            Check(!f.JournalTitled && f.OpenJournal() && f.JournalAt == JournalView.Title && !f.JournalTitled && f.CanJournalLand && !f.CanJournalContents && !f.CanJournalHome, "1e A: the journal's first-ever open shows the title page, and nothing on it leads anywhere; it counts as shown only once it lands (#128 N6)");
            var midBeat = new SliceFlow(() => 1); var beatSave = UnityEngine.JsonUtility.FromJson<SaveData>(UnityEngine.JsonUtility.ToJson(f.ToSave(new bool[12], new bool[12], true)));
            Check(!beatSave.journalTitled && midBeat.Restore(beatSave) && midBeat.OpenJournal() && midBeat.JournalAt == JournalView.Title && midBeat.JournalLand() && midBeat.JournalTitled && midBeat.InscriptionId == SliceFlow.FirstLineId && midBeat.InscriptionText == SliceFlow.Inscription("Davon"), "closed (or quit) during the title page, the next open shows the title page again, then the Oct 2 line, so it is never lost (Jeffrey, #128 N6; #131 N5)");
            Check(f.JournalLand() && f.JournalTitled && f.JournalAt == JournalView.Landing && !f.CanJournalLand && f.CanJournalContents && !f.CanJournalHome && f.CanJournalPractice && !SliceFlow.PracticeDoorLine.Any(char.IsDigit), "its beat ends on the landing: the Contents door opens, and the Practice door opens Practice (owner, Oct 3), with no count (its line, a draft: \"" + SliceFlow.PracticeDoorLine + "\")");
            Check(f.JournalToContents() && f.JournalAt == JournalView.Contents && f.CanJournalHome && !f.CanJournalContents && f.JournalToLanding() && f.JournalAt == JournalView.Landing && f.JournalToContents(), "Contents, and ‹ Your Journal back to the landing");
            Check(!f.OpenChapter("books") && f.OpenChapter("map") && f.JournalAt == JournalView.Map && f.CanJournalContents && f.JournalToContents() && f.OpenChapter("wheel") && f.JournalAt == JournalView.Wheel && f.CanJournalContents, "a row per chapter (the Wheel, the Library Map; a Sealed row opens nothing); each chapter links back to Contents");
            Check(f.SelectSeat(0) && f.OpenSelected() && f.JournalAt == JournalView.Sign && !f.CanJournalContents && f.JournalToWheel(), "a sign's page goes back to the Wheel, as before, not to Contents");
            Check(f.CloseJournal() && f.OpenJournal() && f.JournalAt == JournalView.Wheel && f.JournalSelected == 0 && f.JournalToContents() && f.CloseJournal() && f.OpenJournal() && f.JournalAt == JournalView.Contents && f.CloseJournal(), "1b C: later in the same session the journal opens where it was left");
            Check(SliceFlow.Inscription("Davon") == "What's up, Davon. I'm your journal, and I'll keep a record of what you learn from the Library." && SliceFlow.Inscription(f.DisplayName) == SliceFlow.Inscription("Davon"), "the inscription is the owner's line word for word (Oct 2 evening), the saved name in its slot");
            Check(string.Join(" ", SliceFlow.InscriptionBreaks) == SliceFlow.InscriptionRest, "the inscription's own line breaks keep its words exactly: \"" + string.Join("\" / \"", SliceFlow.InscriptionBreaks) + "\"");
            Check(f.KeysLine == "1 Key", "the Keeper's record counts the Keys collected: 1 Key, and no Book until one opens");
            var save = UnityEngine.JsonUtility.FromJson<SaveData>(UnityEngine.JsonUtility.ToJson(f.ToSave(new bool[12], new bool[12], true))); var next = new SliceFlow(() => 1);
            Check(save.journalTitled && next.Restore(save) && next.JournalTitled && next.OpenJournal() && next.JournalAt == JournalView.Landing && next.CloseJournal(), "a new session from the save: the first open is the landing; the title page never shows again");
            save.journalTitled = false; save.keys = 4; save.locksFilled = 3; var older = new SliceFlow(() => 1);
            Check(older.Restore(save) && older.OpenJournal() && older.JournalAt == JournalView.Title && older.KeysLine == "4 Keys · 1 Book", "a save from before this build shows the title page once; four Keys with Book 1's three locks filled read 4 Keys · 1 Book");
            // the fonts (owner, Oct 2 evening: "Claude sources them", open licenses only)
            var italic = Resources.Load<Font>(SliceView.ItalicFont); var sun = Resources.Load<Font>(SliceView.SunFont); var bold = ButtonLook.EngravedFont;
            var notoLicense = Resources.Load<TextAsset>("Fonts/OFL-NotoSansSymbols2"); var garamondLicense = Resources.Load<TextAsset>("Fonts/OFL-EBGaramond");
            string script = SliceFlow.Inscription("Keeper") + SliceFlow.TitlePageLine + SliceFlow.PracticeDoorLine + SliceFlow.ContentsDoorLine + SliceFlow.WheelChapterLine + SliceFlow.MapChapterLine + "0123456789 Keys Books ·";
            string upright = SliceFlow.KeeperHeading + SliceFlow.BackToJournal + SliceFlow.BackToContents + SliceFlow.WheelTitle + SliceFlow.MapTitle + SliceFlow.SealedChapter + string.Concat(TravelMenu.Rooms.Select(r => r.name));
            Check(italic != null && italic.name == "EBGaramond-Italic" && script.All(c => c == ' ' || italic.HasCharacter(c)) && bold != null && upright.All(c => c == ' ' || bold.HasCharacter(c)), "EB Garamond Italic ships under Resources and carries every script line; EB Garamond Bold carries the upright ones, ‹ included");
            Check(sun != null && sun.HasCharacter('☉') && notoLicense != null && notoLicense.text.Contains("SIL Open Font License") && notoLicense.text.Contains("Noto Project Authors") && garamondLicense != null && garamondLicense.text.Contains("EB Garamond Project Authors"), "Noto Sans Symbols 2 ships with the sun (U+2609) for the Keeper's record; each font has its SIL Open Font License beside it");
            Check(new[] { "EBGaramond-Italic", "NotoSansSymbols2-Regular" }.All(n => !HasTable("Assets/CelestialDial/Resources/Fonts/" + n + ".ttf", "fvar")), "both new fonts are static, with no variation table (a variable font drew blank on the Web on Sept 12)");
            // the art: cut from the approved board's own sources
            var files = new[] { "journal-door-frame", "journal-emblem-practice", "journal-emblem-contents", "journal-emblem-wheel", "journal-emblem-map", "journal-emblem-lock", "journal-flourish", "journal-library-plan" }; int[] scale = { 2, 3, 3, 3, 3, 3, 3, 2 };
            Check(Enumerable.Range(0, files.Length).All(i => Resources.Load<Sprite>("Art/" + files[i]) is Sprite a && Slots.Find(files[i]) is Slots.ArtSlot slot && (int)a.rect.width == slot.Width * scale[i] && (int)a.rect.height == slot.Height * scale[i]), "the landing's, Contents' and the Library Map's eight files are in: the door frame and the plan at 2x their boxes, the emblems and the flourish at 3x");
            // the layout (the 360 x 800 frame): the links' targets, the Wheel's switch and tabs, and the doors with room for the Big Three's line to come
            float linkBottom = SliceView.LinkBaseline - .3545f * 14 + ButtonLook.MinTarget / 2, closing = SliceView.KeeperFirst + SliceView.KeeperRule * 7, lastDoor = closing + SliceView.DoorBelow + SliceView.DoorStep + SliceView.DoorHeight;
            Check(linkBottom <= SliceView.SwitchY - 12 && SliceView.SwitchY + 12 <= SliceView.TabsY - 11 && SliceView.TabsY + 11 <= SliceView.WheelTop - SliceView.WheelSize / 2, "the links' 44 px target ends above the Wheel / Table switch, the switch above the tabs, the tabs above the wheel");
            Check(lastDoor <= SliceView.JournalCloseY - 24 - 8 && lastDoor <= SliceView.PageOrnamentTop, "with KEEPER, the Keys, the Big Three and three lines of inscription, the Contents door ends at " + lastDoor + ", clear of the page's lower ornaments and of Close the journal");
        }
        static void ValidateInscription()
        {
            // ---- the living inscription (owner, Oct 7: approved as scoped, all drafts, 86bcbn6w6 comment 90140263883109; the absence line counts
            // sittings, no clock, comment 90140265375051; task 86bceba0a) ----
            var lines = InscriptionBook.Data.lines; string[] groups = { "any", "key", "wing", "absence", "sun" }; int[] counts = { 8, 4, 3, 3, 6 };
            Check(lines.Length == 24 && lines.Select(l => l.id).Distinct().Count() == 24 && lines.All(l => l.draft && l.id != SliceFlow.FirstLineId && l.text != "") && Enumerable.Range(0, groups.Length).All(i => lines.Count(l => l.group == groups[i]) == counts[i]),
                "the inscription's 24 lines live in data (Resources/Journal/inscriptions.json), each with a stable id and marked a draft: 8 any time, 4 after the first Key, 3 once the Wing is whole, 3 for an absence, 6 on the sun");
            Check(lines.Where(l => l.group == "key").All(l => l.unlock == "key1") && lines.Where(l => l.group == "wing").All(l => l.unlock == "wing") && lines.Where(l => l.group == "any" || l.group == "absence").All(l => l.unlock == "")
                && lines.Where(l => l.group == "sun").Select(l => l.unlock).OrderBy(u => u).SequenceEqual(new[] { "", "key1", "key2", "key3", "key4", "modalities" }.OrderBy(u => u)),
                "each group opens with its lesson: the Key lines at Key 1, the Wing's once it is whole; the sun lines with the Elements, the Symbols, the Modalities, the Table and the Opposites, and one any time (Practice's order)");
            Check(lines.All(l => !l.text.Any(char.IsDigit)) && !SliceFlow.Inscription("Keeper").Any(char.IsDigit), "nothing on screen counts: no inscription line has a digit, so no number, date or \"since\" (Oct 7, no clock)");
            // every line fits the record's three lines, for every sun, at the shipped italic's own widths
            var italic = Resources.Load<Font>(SliceView.ItalicFont); var probeObject = new GameObject("Inscription probe", typeof(RectTransform)); var probe = probeObject.AddComponent<UnityEngine.UI.Text>(); probe.font = italic; probe.fontSize = 16; probe.horizontalOverflow = HorizontalWrapMode.Overflow;
            string Fill(string text, string name, int sun) { var z = Zodiac.Seats[sun]; return text.Replace("{name}", name).Replace("{sun}", z.Name).Replace("{element}", z.Element).Replace("{modality}", z.Modality).Replace("{opposite}", Zodiac.Seats[Zodiac.Opposite(sun)].Name); }
            int Rows(string text) => SliceView.WrapWords(probe, text, SliceView.KeeperWidth).Count;
            var filled = new[] { "Davon", "Keeper" }.SelectMany(n => Enumerable.Range(0, 12).SelectMany(z => lines.Select(l => Fill(l.text, n, z)))).Distinct().ToList();
            Check(filled.All(t => !t.Contains("{") && !t.Contains("}")) && filled.All(t => t.All(c => c == ' ' || italic.HasCharacter(c))), "every line fills in the name and the sun's element, modality and opposite for all twelve signs, and draws in EB Garamond Italic");
            int tallest = filled.Max(Rows);
            Check(tallest <= SliceFlow.InscriptionMostLines, "every line takes three lines at most at 244 px, for Davon and for Keeper and every sun (the tallest takes " + tallest + "), so the doors stay where #110 put them");
            const string longest = "Maximilian Wolfensteiner"; // the 24 characters the name box allows
            var sitOut = lines.Where(l => Enumerable.Range(0, 12).Any(z => Rows(Fill(l.text, longest, z)) > SliceFlow.InscriptionMostLines)).Select(l => l.id).ToList();
            Check(groups.All(g => lines.Any(l => l.group == g && !sitOut.Contains(l.id))), "with a 24-character name every group keeps lines; those that would take a fourth line sit out for that name (" + (sitOut.Count == 0 ? "none" : string.Join(", ", sitOut)) + ")");
            var seconds = filled.Select(t => SliceView.InkSeconds(SliceView.WrapWords(probe, t, SliceView.KeeperWidth))).ToList();
            Check(seconds.Min() >= 2f && seconds.Max() <= 3.6f && SliceView.InkFade == .15f && SliceView.InkStep == .035f && SliceView.InkDelay == .3f, "the ink: each letter fades over 0.15 s, 35 ms apart, with a rest at a comma or a full stop; a line takes " + seconds.Min().ToString("0.0") + " to " + seconds.Max().ToString("0.0") + " s, starting 0.3 s after the landing");
            UnityEngine.Object.DestroyImmediate(probeObject);
            // how a line is picked
            SliceFlow Fresh() { var f = new SliceFlow(() => 1); f.SetName("Davon"); f.SkipPrologue(); f.Continue(); f.ChooseBirth("skip"); f.PickSkipSun(1); f.PickSkipMoon(3); f.PickRising(4); f.Continue(); f.Continue(); f.EnterWing(); f.EnterDial(); f.RevealKey(); f.Continue(); f.Continue(); f.InsertKey(); f.End(); f.Continue(); return f; }
            string Saved(SliceFlow f) => JsonUtility.ToJson(f.ToSave(new bool[12], new bool[12], true));
            int seed = 0;
            SliceFlow Visit(string json, Action<SaveData> change = null) { var save = JsonUtility.FromJson<SaveData>(json); change?.Invoke(save); var f = new SliceFlow(() => 1); f.SeedInscription(++seed); return f.Restore(save) && f.OpenJournal() && f.JournalAt == JournalView.Landing ? f : null; }
            var first = Fresh();
            Check(first.OpenJournal() && first.JournalAt == JournalView.Title && first.InscriptionText == "" && first.JournalLand() && first.InscriptionId == SliceFlow.FirstLineId && first.InscriptionText == SliceFlow.Inscription("Davon") && first.RecentInscriptions.Count == 0,
                "the first-ever open: the line stays blank under the title page, then the Oct 2 line, word for word, as the journal's first words; it is never counted among the recent lines");
            var firstSave = JsonUtility.FromJson<SaveData>(Saved(first));
            Check(firstSave.inscriptionKeys == 1 && firstSave.inscriptionSittings == first.Sittings && !firstSave.inscriptionWing && firstSave.inscriptionsRecent.Length == 0, "the save keeps only the last five lines' ids and the Keys, the sittings and the Wing at the last landing; no date");
            var second = Visit(Saved(first));
            Check(second != null && second.InscriptionId != SliceFlow.FirstLineId && new[] { "any", "key", "sun" }.Contains(second.InscriptionGroup) && second.InscriptionText == second.FillInscription(lines.First(l => l.id == second.InscriptionId).text) && second.RecentInscriptions.SequenceEqual(new[] { second.InscriptionId }),
                "the next visit lands on a line from the groups unlocked so far (" + second?.InscriptionId + "), never the Oct 2 line again");
            string held = second.InscriptionId;
            Check(second.JournalToContents() && second.JournalToLanding() && second.InscriptionId == held && second.CloseJournal() && second.OpenJournal() && second.JournalAt == JournalView.Landing && second.InscriptionId == held && second.RecentInscriptions.Count == 1, "a line holds for the whole visit: back from Contents, or the journal closed and opened again, shows the same line");
            var seen = new List<string> { held }; string json = Saved(second); bool fresh = true, groupsOk = true, sunOk = true;
            for (int i = 0; i < 30; i++) { var v = Visit(json); if (v == null) { fresh = false; break; } fresh &= !seen.Skip(Math.Max(0, seen.Count - SliceFlow.RecentLines)).Contains(v.InscriptionId); groupsOk &= new[] { "any", "key", "sun" }.Contains(v.InscriptionGroup); sunOk &= v.InscriptionGroup != "sun" || v.InscriptionId == "sun-element" || v.InscriptionId == "sun-saving-room"; seen.Add(v.InscriptionId); json = Saved(v); }
            Check(fresh && groupsOk && sunOk && seen.Distinct().Count() >= 10, "over thirty visits it never repeats one of the last five lines; at one Key only the Key lines, the any-time lines and the Elements' and any-time sun lines come round (" + seen.Distinct().Count() + " different)");
            var after = Saved(second);
            var keyed = Visit(after, s => s.keys = 2); var whole = Visit(after, s => { s.keys = 4; s.locksFilled = 4; });
            Check(keyed?.InscriptionGroup == "key" && whole?.InscriptionGroup == "wing", "a group that has just unlocked goes first: a Key line after the Key count rises, the Wing's line once it turns whole (the later milestone, when both happened)");
            var away = Visit(after, s => { s.sittings += SliceFlow.AbsenceSittings; s.reviewsChecked = s.sittings; }); var near = Visit(after, s => { s.sittings += SliceFlow.AbsenceSittings - 1; s.reviewsChecked = s.sittings; }); var awayKeyed = Visit(after, s => { s.sittings += 5; s.reviewsChecked = s.sittings; s.keys = 2; });
            Check(away?.InscriptionGroup == "absence" && near?.InscriptionGroup != "absence" && awayKeyed?.InscriptionGroup == "key", "after 3 sittings or more since the last landing an absence line leads (a working choice for \"a few\"); after 2 it does not; a just-unlocked line takes the slot first");
            bool neverAway = true; string run = after; for (int i = 0; i < 20; i++) { var v = Visit(run); neverAway &= v != null && v.InscriptionGroup != "absence"; run = Saved(v); }
            Check(neverAway, "absence lines are never drawn otherwise: twenty visits with no sittings between show none");
            var older = JsonUtility.FromJson<SaveData>(after); older.inscriptionKeys = -1; older.inscriptionSittings = -1; older.inscriptionWing = false; older.inscriptionsRecent = new string[0]; older.keys = 3; older.sittings = older.reviewsChecked = 12; string olderJson = JsonUtility.ToJson(older);
            var olderGroups = Enumerable.Range(0, 12).Select(i => Visit(olderJson)?.InscriptionGroup).ToList(); var olderOnce = Visit(olderJson); var olderSaved = JsonUtility.FromJson<SaveData>(Saved(olderOnce));
            Check(olderGroups.All(g => g != null && g != "absence") && olderGroups.Any(g => g != "key") && olderSaved.inscriptionKeys == 3 && olderSaved.inscriptionSittings == 12,
                "a save from before this build announces nothing on its first landing (no absence, no Key line first: " + string.Join(" ", olderGroups.Distinct()) + "); today's Keys and sittings become the starting point");
            Check(JsonUtility.FromJson<SaveData>("{\"version\":5}").inscriptionKeys == -1 && JsonUtility.FromJson<SaveData>("{\"version\":5}").inscriptionSittings == -1, "a save with no inscription fields reads as no landing yet");
            var unSun = JsonUtility.FromJson<SaveData>(after); unSun.sunSign = -1; unSun.choices = unSun.choices.Select(c => c.point == "sun" ? new ChoiceRecord { point = "sun", sign = -1, how = "declined" } : c).ToArray(); string unSunJson = JsonUtility.ToJson(unSun);
            var unSunFlows = Enumerable.Range(0, 20).Select(i => Visit(unSunJson)).ToList();
            Check(unSunFlows.All(v => v != null && !v.HasSunSign && v.InscriptionGroup != "sun"), "an older save with no sun (a #123 declined sun) never gets a sun line");
            var narrow = JsonUtility.FromJson<SaveData>(after); var fits = new List<string>(); for (int i = 0; i < 20; i++) { var v = new SliceFlow(() => 1); v.SeedInscription(100 + i); v.InscriptionFits = t => !t.Contains("Davon"); v.Restore(JsonUtility.FromJson<SaveData>(after)); v.OpenJournal(); fits.Add(v.InscriptionText); }
            Check(fits.All(t => !t.Contains("Davon")), "a line the record can't fit in three lines for this name sits out of this player's pool");
            var keeper = new SliceFlow(() => 1); Check(keeper.FillInscription("Hello again, {name}.") == "Hello again, Keeper.", "with no saved name the line says Keeper");
            var odd = Fresh(); odd.SetName("{sun}"); Check(odd.FillInscription("Hello again, {name}.") == "Hello again, {sun}.", "a name goes in last, as typed, so it never pulls in the sun's words (Jeffrey, #128 N1)");
            Check(SliceView.InkTapBottom <= SliceView.JournalCloseY - 24, "the tap that finishes the line ends at " + SliceView.InkTapBottom + ", above Close the journal (Jeffrey, #128 B1)");
        }
        static void ValidateComeAlive()
        {
            // ---- the Table and the Book come alive (owner, Oct 7-8; task 86bcf0x71) ----
            var names = new[] { "book-room", "table-room", "table-well", "table-plate", "table-plate-gold", "letter-fire", "letter-earth", "letter-air", "letter-water" }.Concat(Enumerable.Range(0, 12).Select(SliceView.EmblemSlot)).ToArray();
            Check(names.Length == 21 && names.All(n => Slots.Find(n) != null && Slots.Image(n) != null) && Slots.Find("book-room").Width == 360 && Slots.Find("table-room").Height == 800 && Slots.Find("table-well").Width == 86 && Slots.Find("table-plate").Height == 44 && Slots.Find("emblem-aries").Width == 180,
                "the 21 come-alive slots are in the manifest and every one has its file: the Book's room and the Table's top, the well and the two plates, four fills, and an emblem for each of the twelve signs");
            Check(Enumerable.Range(0, 12).All(i => Slots.Image(SliceView.EmblemSlot(i)) is Sprite e && Mathf.Abs(e.rect.width - e.rect.height) < 1) && Enumerable.Range(0, 12).Select(SliceView.EmblemSlot).Distinct().Count() == 12,
                "an emblem per sign, square, named for its sign (emblem-aries ... emblem-pisces)");
            Check(Enumerable.Range(0, 12).All(i => SliceView.LetterSlot(i) == "letter-" + Zodiac.Seats[i].Element.ToLowerInvariant() && Slots.Image(SliceView.LetterSlot(i)) != null) && SliceView.LetterDrift(1) == Vector2.zero && SliceView.LetterDrift(0).y > 0,
                "each sign's live lettering takes its element's fill; earth holds still, fire rises");
            Check(SliceView.LetterLine.maxColorComponent < .2f && SliceView.LetterLine.r > SliceView.LetterLine.b && SliceView.LetterLine.a > .9f && SliceView.FrameGold.r > SliceView.FrameGold.g && SliceView.FrameGold.g > SliceView.FrameGold.b,
                "live lettering is edged in the Art Bible's dark-brown line on all four elements (owner, Oct 8), and the frame's names are inlaid gold");
            var shader = Resources.Load<Shader>("Shaders/LetterFill");
            Check(shader != null && shader.name == "Ascendant/LetterFill" && shader.isSupported, "the live lettering's shader ships under Resources (so the Web build carries it) and compiles");
            Check(SliceView.InkPlace.OrderBy(x => x).SequenceEqual(Enumerable.Range(0, 12)) && !SliceView.InkPlace.SequenceEqual(Enumerable.Range(0, 12)) && SliceView.InkSpots.Length == 12 && SliceView.InkSpots.Distinct().Count() == 12
                && SliceView.InkSpots.Take(6).All(p => p.x <= -24) && SliceView.InkSpots.Skip(6).All(p => p.x >= 24) && SliceView.InkSpots.All(p => Mathf.Abs(p.x) <= 126 && p.y >= 308 && p.y <= 388)
                && new[] { SliceView.InkSpots.Take(6), SliceView.InkSpots.Skip(6) }.All(page => page.All(p => page.Where(q => q != p).Min(q => Vector2.Distance(p, q)) >= SliceView.InkSize + 2)),
                "the Book's ink: twelve places on its open pages, six to a page clear of the gutter and of each other, and a sign's place is not its wheel order (owner, Oct 7, 2A)");
            Check(SliceView.InkFull > SliceView.InkFaint && SliceView.InkFaint > 0 && SliceView.DarkPlate.r < .4f && SliceView.EmblemY < SliceView.RiseFrom && SliceView.RiseFrom < 420,
                "a symbol learned alone inks fully, one shown with help faintly (1A); the answer plates are near-black (owner, Oct 8); a question rises from the pages to its place");
            var grid = new GridModel(() => 0); bool begun = grid.InputMethod == "DirectCell" && grid.Begin() && grid.Pick(0);
            grid.InputMethod = "Drag"; bool dragged = begun && grid.Choose(GridModel.CellOf(0)); grid.InputMethod = "DirectCell"; var sealedByTap = dragged ? grid.Seal() : null;
            Check(dragged && grid.Events.Any(e => e.event_name == "cell_chosen" && e.input_method == "Drag") && sealedByTap != null && sealedByTap.correctness && sealedByTap.input_method == "DirectCell",
                "a dragged plate's well logs Drag and a tap logs DirectCell; the drag grades the same (owner, Oct 7: drag and tap)");
        }
        static void ValidateTriangles()
        {
            // ---- the family triangles (owner, Oct 3: the overlay approved, mixed strength; the board on 86bcbn6w6, Oct 2) ----
            Check(FamilyTriangles.Rest == .34f && FamilyTriangles.RestGlow == .10f && FamilyTriangles.Star == .55f && FamilyTriangles.Teach == .70f && FamilyTriangles.TeachGlow == .55f && FamilyTriangles.TeachBloom == .20f && FamilyTriangles.Seat == .80f && FamilyTriangles.Flame == .88f && FamilyTriangles.Dust == .65f,
                "the owner's mixed strength: resting and teaching at level A (lines 34%, glow 10%, stars 55%; the taught triangle 70%, glow 55%, bloom 20%; its seats 80%), the flame payoff at level B (88%); dust 65% of resting on the worn Dial");
            var shader = Resources.Load<Shader>("Shaders/LightLines");
            Check(shader != null && shader.isSupported, "the light's shader (a screen blend: nothing under a line darkens) ships under Resources and compiles");
            Check(Enumerable.Range(0, 4).All(f => FamilyTriangles.Families[f] == Zodiac.Seats[f].Element && FamilyTriangles.Sides(f).All(side => side.All(seat => Zodiac.Seats[Zodiac.Wrap(seat)].Element == Zodiac.Seats[f].Element))) && FamilyTriangles.Sides(0).Select(x => Zodiac.Seats[Zodiac.Wrap(x[0])].Name).SequenceEqual(new[] { "Aries", "Leo", "Sagittarius" }),
                "each triangle joins its own family's three seats (Fire: Aries, Leo, Sagittarius; Earth, Air and Water the same)");
            Check(new[] { new Vector2(-30, 4), new Vector2(12, -3), new Vector2(55, 6) }.All(p => new[] { 117.5f, 112f, -146f }.All(r => (FamilyTriangles.Unbend(FamilyTriangles.Bend(p, r), r) - p).magnitude < .001f)), "a curved word's box is tested on its own arc: the ribbon's bend undone exactly");
            var l = new DialLesson(() => 0); l.SetSunSign(1); int lit = 0; l.WheelLit += () => lit++; var taught = new List<int>(); void Note() => taught.Add(l.TeachingFamily);
            EnterGuided(l); Note(); Answer(l); Note(); Answer(l); Note(); l.Continue(); Note(); Answer(l); Note(); Answer(l); Note(); l.BeginContinuation(); Note(); Answer(l); Answer(l); Note(); Answer(l); Answer(l); Note();
            Check(taught.SequenceEqual(new[] { 1, 1, -1, 0, 0, -1, 2, 3, -1 }) && l.Phase == LessonPhase.AllLit && lit == 1, "the taught triangle follows the lesson for a Taurus player: Earth while it is guided, none between, Fire, none at Key 1, then Air and Water in the continuation; the payoff fires once, as the whole wheel lights (" + string.Join(", ", taught) + ")");
            var restored = new DialLesson(() => 0); int litLater = 0; restored.WheelLit += () => litLater++; restored.RestoreProgress(1, Enumerable.Repeat(true, 12).ToArray(), Enumerable.Repeat(true, 12).ToArray(), true);
            Check(restored.Phase == LessonPhase.AllLit && litLater == 0 && restored.TeachingFamily == -1, "a save already past it never plays the payoff again, and teaches no family");
        }
        static void ValidatePractice()
        {
            // ---- Practice in the journal (owner, Oct 3: step 4 approved as scoped, the door's count removed; the board on 86bcbn6w6, Oct 2) ----
            var book = PracticeBook.Data; string[] ids = { "elements", "symbols", "modalities", "table", "opposites" }, unlocks = { "key1", "key2", "modalities", "key3", "key4" };
            Check(book.concepts.Select(c => c.id).SequenceEqual(ids) && book.concepts.Select(c => c.unlock).SequenceEqual(unlocks) && book.concepts.All(c => c.name != "" && c.line != "" && c.end != ""), "Practice's data (Resources/Practice/questions.json): the Elements, the Symbols, the Modalities, the Elemental Table and the Opposites, each with its name, its line on the list, the lesson that unlocks it and the line that ends its round");
            Check(book.right == "That's it." && book.wrong == "Not quite.", "an answer is followed by \"That's it.\" or \"Not quite.\", then its why (the board's drafts)");
            var all = book.concepts.SelectMany(c => c.questions.Select(q => new { c, q })).ToList();
            Check(book.concepts.All(c => c.questions.Length == 6) && all.All(x => x.q.ask.Trim() != "" && x.q.why.Trim() != "" && x.q.choices.Length >= 2 && x.q.choices.Length <= 4 && x.q.choices.Distinct().Count() == x.q.choices.Length && x.q.answer >= 0 && x.q.answer < x.q.choices.Length), "six questions a concept, " + all.Count + " in all: each with an ask, two to four different choices, the right one among them, and its why");
            Check(all.All(x => x.q.glyph == -1 || (x.c.id == "symbols" && x.q.glyph >= 0 && x.q.glyph < 12)), "only the Symbols draw a sign's symbol above a question");
            // the facts, against the game's own data wherever a question names its sign (or shows its mark) and asks one of its facts
            var signs = Zodiac.Seats.Select(z => z.Name).ToArray(); string[] elements = { "Fire", "Earth", "Air", "Water" }, polarities = { "Yang", "Yin" };
            int checkedFacts = 0; var wrongFacts = new List<string>(); var byHand = new List<string>();
            foreach (var x in all)
            {
                var named = Enumerable.Range(0, 12).Where(i => System.Text.RegularExpressions.Regex.IsMatch(x.q.ask, @"\b" + signs[i] + @"\b")).ToList(); int subject = x.q.glyph >= 0 ? x.q.glyph : named.Count == 1 ? named[0] : -1;
                string right = x.q.choices[x.q.answer], expected = null; bool allIn(string[] set) => x.q.choices.All(set.Contains);
                string ask = x.q.ask.ToLowerInvariant(); var el = elements.Where(e => x.q.ask.Contains(e)).ToList(); var mo = Zodiac.Modalities.Where(m => x.q.ask.Contains(m)).ToList();
                string ElementModality(int i) => Zodiac.Seats[i].Element + ", " + Zodiac.ModalityAt(i);
                if (x.q.glyph >= 0 && allIn(signs)) expected = signs[x.q.glyph];
                else if (named.Count == 1 && allIn(signs) && (ask.Contains("opposite") || ask.Contains("across"))) expected = signs[Zodiac.Opposite(subject)];
                else if (named.Count == 1 && allIn(signs) && ask.Contains("next") && el.Count + mo.Count == 1) { int step = el.Count == 1 ? 4 : 3; expected = signs[Zodiac.Wrap(subject + step)]; }
                else if (named.Count == 1 && x.q.choices.All(c => Enumerable.Range(0, 12).Any(i => ElementModality(i) == c))) expected = ElementModality(subject);
                else if (subject >= 0 && named.Count <= 1 && allIn(elements)) expected = Zodiac.Seats[subject].Element;
                else if (subject >= 0 && named.Count <= 1 && allIn(Zodiac.Modalities)) expected = Zodiac.ModalityAt(subject);
                else if (subject >= 0 && named.Count <= 1 && allIn(polarities)) expected = Zodiac.PolarityAt(subject);
                else if (named.Count == 2 && allIn(Zodiac.Modalities) && Zodiac.ModalityAt(named[0]) == Zodiac.ModalityAt(named[1])) expected = Zodiac.ModalityAt(named[0]);
                else if (named.Count == 2 && allIn(signs) && el.Count == 1) expected = signs.Where((n, i) => Zodiac.Seats[i].Element == el[0] && !named.Contains(i)).SingleOrDefault();
                else if (named.Count == 0 && allIn(signs) && el.Count == 1 && mo.Count == 1) expected = signs.Where((n, i) => Zodiac.Seats[i].Element == el[0] && Zodiac.ModalityAt(i) == mo[0]).SingleOrDefault();
                else if (named.Count == 0 && allIn(signs) && el.Count + mo.Count == 1) { var fits = x.q.choices.Where(c => { int i = Array.IndexOf(signs, c); return el.Count == 1 ? Zodiac.Seats[i].Element == el[0] : Zodiac.ModalityAt(i) == mo[0]; }).ToList(); expected = fits.Count == 1 ? fits[0] : "(" + fits.Count + " choices fit)"; }
                else if (named.Count == 0 && allIn(polarities) && (ask.Contains("outward") || ask.Contains("inward"))) expected = ask.Contains("outward") ? "Yang" : "Yin"; // Caspar's words (SliceFlow.PolarityWords)
                else if (ask.Contains("opposite") && ask.Contains("differ") && x.q.choices.Contains("Element")) expected = "Element"; // opposites share a modality and a polarity; only the element differs
                if (expected == null) { byHand.Add(x.q.ask); continue; }
                checkedFacts++; if (right != expected) wrongFacts.Add(x.q.ask + " -> " + right + " (the game's data: " + expected + ")");
            }
            Check(wrongFacts.Count == 0 && checkedFacts * 2 >= all.Count, "the right answers agree with the game's own data: " + checkedFacts + " of " + all.Count + " questions checked against the signs' elements, modalities, polarities, opposites and symbols" + (wrongFacts.Count > 0 ? "; WRONG: " + string.Join("; ", wrongFacts) : "") + (byHand.Count > 0 ? "; read by hand: " + string.Join(" | ", byHand) : ""));
            // the flow: the list grows with the lessons; a round asks each of its questions once, the choices shuffled; right, wrong and the why
            var f = new SliceFlow(() => 1); f.SetName("Davon"); f.SkipPrologue(); f.Continue(); f.ChooseBirth("skip"); f.PickSkipSun(1); f.PickSkipMoon(3); f.PickRising(4); f.Continue(); f.Continue(); f.EnterWing(); f.EnterDial(); f.RevealKey(); f.Continue(); f.Continue(); f.InsertKey(); f.End(); f.Continue(); f.SeedPractice(7);
            Check(f.OpenJournal() && f.JournalLand() && f.CanJournalPractice && f.PracticeConcepts.Select(c => c.id).SequenceEqual(new[] { "elements" }) && f.OpenPractice() && f.JournalAt == JournalView.Practice && f.CanJournalHome && !f.CanJournalContents, "at Key 1 the Practice door opens the list: the Elements only; ‹ Your Journal leads back");
            Check(!f.StartQuiz(1) && f.StartQuiz(0) && f.JournalAt == JournalView.Quiz && f.OnQuiz && f.QuizCount == 6 && f.QuizCounter == "1 of 6" && f.CanQuizAnswer && !f.CanQuizNext && f.QuizFeedback == "", "a concept's row starts its round: question 1 of 6, waiting for a pick (no row past the list)");
            var save = UnityEngine.JsonUtility.ToJson(f.ToSave(new bool[12], new bool[12], true)); string Deck() => string.Join("|", f.Deck.Items.Select(i => i.seat + ":" + i.kind + ":" + i.state + ":" + i.streak + ":" + i.interval + ":" + i.dueDay + ":" + i.entered));
            var deckBefore = Deck(); var asked = new List<PracticeQuestion>(); bool shuffled = true, marks = true;
            for (int n = 0; n < 6; n++)
            {
                var q = f.QuizQuestion; asked.Add(q); var shown = f.QuizChoices; int right = f.QuizRight;
                shuffled &= shown.OrderBy(c => c).SequenceEqual(q.choices.OrderBy(c => c)) && shown[right] == q.choices[q.answer];
                int pick = n % 2 == 0 ? right : (right + 1) % shown.Length;
                marks &= f.AnswerQuiz(pick) && f.QuizPicked == pick && !f.AnswerQuiz(right) && f.QuizFeedback == (pick == right ? "That's it. " : "Not quite. ") + q.why && f.CanQuizNext && f.NextQuiz();
            }
            Check(shuffled && marks, "each answer is marked once: right reads \"That's it.\" and a wrong pick \"Not quite.\", each with its why; the choices are the question's own, the right one where the game says");
            Check(asked.Distinct().Count() == 6 && f.QuizOver && f.QuizEnd == book.concepts[0].end && f.CanQuizBack && !f.CanQuizNext && !f.CanQuizAnswer && f.QuizCounter == "", "the round asks each of the six once, then ends with the journal's line: \"" + book.concepts[0].end + "\"");
            Check(f.QuizToPractice() && f.JournalAt == JournalView.Practice && !f.OnQuiz && f.JournalToLanding() && f.JournalAt == JournalView.Landing, "Back to Practice returns to the list, and ‹ Your Journal to the landing");
            Check(UnityEngine.JsonUtility.ToJson(f.ToSave(new bool[12], new bool[12], true)) == save && Deck() == deckBefore, "practising changes nothing in the save or the deck: no score, no streak, no spaced review");
            var orders = new List<string>();
            for (int seed = 1; seed <= 4; seed++) { f.SeedPractice(seed); f.OpenPractice(); f.StartQuiz(0); orders.Add(f.QuizQuestion.ask + "|" + string.Join(",", f.QuizChoices)); f.QuizToPractice(); f.JournalToLanding(); }
            Check(orders.Distinct().Count() > 1, "each round comes in a fresh order, the choices shuffled (" + orders.Distinct().Count() + " different openings in 4 rounds)");
            Check(f.OpenPractice() && f.StartQuiz(0) && f.AnswerQuiz(0) && f.QuizToPractice() && f.JournalAt == JournalView.Practice && f.StartQuiz(0) && f.QuizAt == 0 && f.QuizPicked == -1, "‹ Practice leaves a round at any time; the next round starts afresh");
            f.QuizToPractice(); f.JournalToLanding(); f.CloseJournal();
            var grown = new List<int>(); f.MarkKey2(); grown.Add(f.PracticeConcepts.Count); f.MarkModalitiesComplete(); grown.Add(f.PracticeConcepts.Count); f.MarkKey3(); grown.Add(f.PracticeConcepts.Count); f.MarkKey4(); grown.Add(f.PracticeConcepts.Count);
            Check(grown.SequenceEqual(new[] { 2, 3, 4, 5 }) && f.PracticeConcepts.Select(c => c.id).SequenceEqual(ids), "the list grows as each lesson finishes: the Symbols at Key 2, the Modalities with their lesson, the Table at Key 3, the Opposites at Key 4 (" + string.Join(", ", grown) + ")");
            var early = new SliceFlow(() => 1); early.SetName("Davon"); early.SkipPrologue(); early.Continue(); early.ChooseBirth("skip"); early.PickSkipSun(1); early.PickSkipMoon(3); early.PickRising(4); early.Continue(); early.Continue(); early.EnterWing(); early.EnterDial();
            Check(early.PracticeConcepts.Count == 0 && !early.CanJournalPractice, "before the first lesson finishes, nothing is listed and the door is unavailable");
            // the page: every line fits its place, at the fonts' own widths; every question ends above Close the journal
            var italic = Resources.Load<Font>(SliceView.ItalicFont); var bold = ButtonLook.EngravedFont; var probeObject = new GameObject("Practice probe", typeof(RectTransform)); var probe = probeObject.AddComponent<UnityEngine.UI.Text>(); probe.horizontalOverflow = HorizontalWrapMode.Overflow;
            float Width(string text, Font face, int size) { probe.font = face; probe.fontSize = size; return probe.cachedTextGeneratorForLayout.GetPreferredWidth(text, probe.GetGenerationSettings(Vector2.zero)) / probe.pixelsPerUnit; }
            int Lines(string text, Font face, int size, float width) { probe.font = face; probe.fontSize = size; return SliceView.WrapWords(probe, text, width).Count; }
            string copy = string.Concat(all.Select(x => x.q.ask + x.q.why)) + string.Concat(book.concepts.Select(c => c.line + c.end)) + book.right + book.wrong + "0123456789 of", upright = string.Concat(all.SelectMany(x => x.q.choices)) + string.Concat(book.concepts.Select(c => c.name)) + SliceFlow.PracticeTitle + SliceFlow.BackToPractice + SliceFlow.QuizNextWords + SliceFlow.QuizBackWords + "×";
            Check(copy.All(c => c == ' ' || italic.HasCharacter(c)) && upright.All(c => c == ' ' || bold.HasCharacter(c)), "every line of Practice's copy draws in the journal's fonts (the italic for the asks, the whys and the lines; EB Garamond Bold for the names and the choices)");
            float worstName = book.concepts.Max(c => Width(c.name, bold, 21)), worstLine = book.concepts.Max(c => Width(c.line, italic, 15)), worstChoice = all.SelectMany(x => x.q.choices).Max(c => Width(c, bold, 18));
            Check(worstName <= 210 && worstLine <= 210 && worstChoice <= SliceView.ChoiceWidth - 60, "the list's names and lines fit their 210 px (widest " + worstName.ToString("0") + " and " + worstLine.ToString("0") + "), and every choice its frame (widest " + worstChoice.ToString("0") + " of " + (SliceView.ChoiceWidth - 60) + ")");
            float worstBottom = 0, closest = SliceView.ChoiceStep; string tallest = ""; int worstAsk = 0, worstWhy = 0, worstEnd = book.concepts.Max(c => Lines(c.end, italic, 17, SliceView.QuizAskWidth + 10));
            foreach (var x in all)
            {
                int ask = Lines(x.q.ask, italic, 19, SliceView.QuizAskWidth), why = Math.Max(Lines(book.right + " " + x.q.why, italic, 16, SliceView.QuizAskWidth), Lines(book.wrong + " " + x.q.why, italic, 16, SliceView.QuizAskWidth));
                worstAsk = Math.Max(worstAsk, ask); worstWhy = Math.Max(worstWhy, why);
                var layout = SliceView.QuizLayout(x.q.glyph >= 0, ask, x.q.choices.Length, why); float bottom = layout.w + SliceView.ChoiceHeight; closest = Math.Min(closest, layout.z);
                if (bottom > worstBottom) { worstBottom = bottom; tallest = x.q.ask; }
            }
            UnityEngine.Object.DestroyImmediate(probeObject);
            Check(worstAsk <= 2 && worstWhy <= 3 && worstEnd <= 3 && worstBottom <= SliceView.QuizBottom + .01f && closest >= SliceView.ChoiceHeight + 2, "every ask takes two lines or fewer, every why three, every round's end three; the tallest question (\"" + tallest + "\") ends its Next at " + worstBottom.ToString("0") + ", inside the page's border (" + SliceView.QuizBottom + "), its choices " + closest.ToString("0.#") + " apart at the closest");
            Check(SliceView.PracticeRowTop + SliceView.PracticeRowHeight * 5 <= SliceView.PageOrnamentTop && SliceView.PracticeRowHeight >= ButtonLook.MinTarget && SliceView.ChoiceHeight >= ButtonLook.MinTarget && SliceView.QuizNextWidth >= ButtonLook.MinTarget, "five rows end at " + (SliceView.PracticeRowTop + SliceView.PracticeRowHeight * 5) + ", above the page's lower ornaments; the rows, the choices and Next take 44 px or more");
        }
        static void ValidateBuildJ()
        {
            // ---- Build J (owner, Sept 23): Illumination plus Ribbons. Build AA (owner, Sept 30; Sept 26 notes 7 and 14): the Wheel index with a
            // Wheel / Table switch and tabs as each pattern is learned; a seat's preview, then its page, which links the signs sharing each fact ----
            var f = new SliceFlow(() => 1); f.SkipPrologue(); f.Continue(); f.ChooseBirth("skip"); f.PickSkipSun(1); f.PickSkipMoon(3); f.PickRising(4); f.Continue(); f.Continue(); f.EnterWing(); f.EnterDial(); f.RevealKey(); f.Continue(); f.Continue(); f.InsertKey(); f.End(); f.Continue(); // the Hub, twelve element items entered
            string DeckKey() => string.Join("|", f.Deck.Items.Select(i => i.seat + ":" + i.kind + ":" + i.state + ":" + i.streak + ":" + i.interval + ":" + i.dueDay + ":" + i.entered));
            string Facts(int seat) => string.Join(", ", f.SignFacts(seat).Select(x => x[0] + ": " + x[1]));
            Check(f.OpenJournal() && f.JournalAt == JournalView.Title && f.JournalLand() && f.JournalToContents() && f.OpenChapter("wheel") && f.JournalAt == JournalView.Wheel, "batch 2: the journal's first-ever open is the title page, then the landing; the Wheel is a chapter, reached through Contents");
            Check(f.JournalAt == JournalView.Wheel && !f.JournalTableView && f.JournalSigns.Count == 12 && f.JournalLenses.SequenceEqual(new[] { JournalLens.Element }) && !f.CanJournalTable && f.JournalSelected == -1 && !f.CanOpenSelected, "Build AA: the Wheel: the twelve signs met, the Element tab only, no Table yet, nothing framed");
            Check(!f.SetJournalTable(true) && !f.SetLens(JournalLens.Modality) && !f.SelectSeat(12) && !f.OpenSelected() && !f.CanJournalWheel && !f.CanJournalNext, "nothing not yet learned can be switched on; no seat past the twelfth; no page to turn on the Wheel");
            Check(Enumerable.Range(0, 12).All(seat => f.SeatState(seat) >= 1) && f.SelectSeat(4) && f.JournalSelected == 4 && f.SeatSummary(4) == "Fire" && f.CanOpenSelected, "a tap frames Leo; its preview reads only what is learned: Fire");
            Check(f.OpenSelected() && f.JournalAt == JournalView.Sign && f.JournalSignSeat == 4 && Facts(4) == "Element: Fire" && f.SignKin(4, ItemKind.Element).SequenceEqual(new[] { 0, 8 }) && f.SignKin(4, ItemKind.Modality).Count == 0 && f.SignKin(4, ItemKind.Opposite).Count == 0, "the preview opens Leo's page: its element and the two signs that share it (Aries, Sagittarius); no link for what is not learned");
            Check(f.OpenSign(0) && f.JournalSignSeat == 0 && f.CanJournalNext && !f.CanJournalPrev && f.JournalNext() && f.JournalSignSeat == 1 && f.JournalPrev() && f.JournalToWheel() && f.JournalAt == JournalView.Wheel && f.JournalSelected == 0, "a link opens Aries' page; pages turn in zodiac order; Back to the Wheel frames the sign last read");
            Check(f.CloseJournal() && f.AtHub, "the journal closes back to the Atrium");
            f.StartGlyphs(); f.StartModalities(); f.StartGrid(); f.StartOpposites();
            Check(Facts(6) == "Element: Air, Modality: Cardinal, Polarity: Yang, Opposite: Aries" && f.SignKnows(6, ItemKind.Glyph) && f.SignKnows(6, ItemKind.Opposite), "a page grows as the player learns: Libra's modality, polarity and opposite (its pair's item), its symbol");
            Check(f.OpenJournal() && f.JournalAt == JournalView.Wheel && f.CanJournalTable && f.JournalLenses.SequenceEqual(new[] { JournalLens.Element, JournalLens.Modality, JournalLens.Polarity, JournalLens.Opposites }), "at the Wing's end the Table and all four tabs are there (the journal reopens where it was left, the Wheel, in the same session)");
            Check(f.SetJournalTable(true) && f.JournalTableView && f.SetLens(JournalLens.Opposites) && f.Lens == JournalLens.Opposites && !f.SetLens(JournalLens.Opposites) && f.SetJournalTable(false) && !f.JournalTableView, "the switch and the tabs change the view, each once");
            Check(f.SignKin(6, ItemKind.Modality).SequenceEqual(new[] { 0, 3, 9 }) && f.SignKin(6, ItemKind.Element).SequenceEqual(new[] { 2, 10 }) && f.SignKin(6, ItemKind.Opposite).SequenceEqual(new[] { 0 }) && SliceFlow.PolarityWords(6) == "outward and active" && SliceFlow.PolarityWords(1) == "inward and receptive" && SliceFlow.PairShares(0) == "shares Cardinal · Yang; only the element differs", "Libra's links: the other Air signs, the other Cardinal signs, its opposite; Caspar's words for the polarities (worksheet section 12); what a pair shares");
            Check(SliceView.SeatCentre(0, false).x < SliceView.JournalX - 90 && Mathf.Abs(SliceView.SeatCentre(0, false).y - SliceView.WheelTop) < .01f && SliceView.SeatCentre(4, true) == new Vector2(SliceView.TableColumnX(1), SliceView.TableRowY(0)) && SliceView.SeatCentre(9, true) == new Vector2(SliceView.TableColumnX(0), SliceView.TableRowY(1)) && SliceView.SeatCentre(11, true) == new Vector2(SliceView.TableColumnX(2), SliceView.TableRowY(3)), "the seats: Aries at 9 o'clock on the wheel; on the Table, Leo at Fire and Fixed, Capricorn at Earth and Cardinal, Pisces at Water and Mutable");
            Check(Enumerable.Range(0, 12).All(i => Enumerable.Range(0, 12).All(j => i == j || Vector2.Distance(SliceView.SeatCentre(i, false), SliceView.SeatCentre(j, false)) >= SliceView.SeatHit && Vector2.Distance(SliceView.SeatCentre(i, true), SliceView.SeatCentre(j, true)) >= SliceView.SeatHit)), "no two seats' 46 px tap targets overlap, on the wheel or the Table");
            Check(f.CloseJournal(), "closed");
            Check(Enumerable.Range(0, 12).All(seat => !Facts(seat).Contains("introduced") && !Facts(seat).Contains("practicing") && !f.SeatSummary(seat).Contains("introduced") && !f.SeatSummary(seat).Contains("practicing")), "no journal string carries a state word (the play fixture reads the screen itself)");
            foreach (var it in f.Deck.Items) it.dueDay = 99; // nothing due
            foreach (var it in f.SignItems(4)) { it.state = (int)ItemState.Introduced; it.streak = 0; it.interval = 0; }
            var leo = f.Deck.Item(4, ItemKind.Element);
            Check(f.SignInk(4) == SliceFlow.IntroducedInk && f.SignColour(4) == 0 && !f.SignGilt(4) && f.SignLadder(4) == 0 && !f.SignDue(4) && f.SeatState(4) == 1, "Illumination: an introduced sign is line art at about a third of its ink, no colour, a silver ring (met); its ribbon is at its shortest");
            leo.state = (int)ItemState.Practicing; leo.streak = 1; Check(f.SignInk(4) == 1 && Mathf.Approximately(f.SignColour(4), .4f) && f.SeatState(4) == 2, "a first streak brings the colour to 40% at full ink, a gold ring (practising)");
            leo.streak = 2; Check(Mathf.Approximately(f.SignColour(4), .7f) && !f.SignGilt(4) && f.SeatState(4) == 2, "70% at a streak of two");
            leo.streak = 3; Check(f.SignColour(4) == 1 && f.SignGilt(4) && f.SeatState(4) == 3, "full colour at three and on: the gold-leaf ring (mastered), today's gilt edge");
            leo.dueDay = f.Sitting; leo.interval = 3; Check(Mathf.Approximately(f.SignColour(4), SliceFlow.DueFade) && f.SignGilt(4) && f.SignDue(4), "due for practice, the colour fades a little; the gold stays");
            Check(f.SignLadder(4) == 3 && f.SignDue(4) && !f.SignDue(5) && f.SignLadder(5) == 0, "Ribbons: Leo's is three rungs long and pulled out (due for practice); Virgo's is not pulled out");
            var virgoTable = f.Deck.Item(5, ItemKind.Grid); virgoTable.dueDay = 0; virgoTable.interval = 4;
            Check(!f.SignDue(5) && f.SignLadder(5) == 0, "the table and the opposites are data only (never asked in practice), so they never pull a ribbon out or lengthen it");
            string before = DeckKey(); f.OpenJournal(); f.SelectSeat(3); f.SetJournalTable(true); f.SetLens(JournalLens.Modality); f.OpenSelected(); f.JournalNext(); f.OpenSign(9); f.JournalToWheel(); f.SetLens(JournalLens.Opposites); f.SelectSeat(6);
            Check(DeckKey() == before && f.CloseJournal(), "using every part of the journal changes no deck field");
            Check(new[] { "journal-page", "journal-cover", "journal-ribbon", "journal-wheel", "journal-seat-leaf", "journal-seat-line" }.All(n => Slots.Find(n) != null) && Slots.Find("journal-contents") == null && Slots.Find("journal-plate") == null && Slots.Find("journal-wheel").Width == 300 && Zodiac.Seats.All(z => Slots.Find("sign-" + z.Name.ToLowerInvariant()) != null && Slots.Find("sign-" + z.Name.ToLowerInvariant()).Width == 200 && Slots.Find("sign-" + z.Name.ToLowerInvariant()).Height == 200), "Build AA's slots: the page, cover, ribbon, the wheel and the two seat rings, a 200 × 200 picture per sign; the contents page and the fact plate retired");
            Check(Resources.Load<Shader>("Shaders/Illumination") != null && Resources.Load<Shader>("Shaders/Illumination").FindPropertyIndex("_Round") >= 0, "the Illumination shader is under Resources, so the Web build carries it, and clips a seat's picture round itself (_Round): a UI Mask would draw a copy of the material and freeze its colour");
            Check(System.IO.File.ReadAllLines("Assets/CelestialDial/SliceView.cs").Where(l=>l.Contains("AddComponent<Mask>()")).All(l=>l.Contains("SweptFrames.Contains(slot)")), "no journal picture sits under a UI Mask (Illumination's live values would not reach the screen); the only Mask is the launch movie's radial sweep on its two pencil layers (round 4)");
            // the titles in blackletter (owner, Sept 26): the font ships with its license and carries every letter a title uses
            var titleFont = Resources.Load<Font>(SliceView.TitleFont); var license = Resources.Load<TextAsset>("Fonts/OFL-UnifrakturMaguntia"); var titles = new[] { SliceFlow.WheelTitle, "Fire", "Earth", "Air", "Water" }.Concat(Zodiac.Seats.Select(z => z.Name)).ToList();
            Check(titleFont != null && titles.All(t => t.All(c => c == ' ' || titleFont.HasCharacter(c))) && license != null && license.text.Contains("SIL Open Font License"), "the journal's titles have their blackletter font (UnifrakturMaguntia) under Resources with its license, carrying every letter of the " + titles.Count + " titles");
            Check(new[] { SliceFlow.WheelTitle, "Open the page", "Back to the Wheel", SliceFlow.PairShares(0), SliceFlow.PolarityWords(1) }.Concat(Enumerable.Range(0, 4).Select(i => SliceFlow.LensTitle((JournalLens)i))).All(t => t.All(c => c < 256)), "every plain-font string the journal adds is Latin-1, so the Web player's built-in font draws it");
        }
        // Sept 25: a `//` inserted before the rest of a statement had made code dead for days (the Atrium doors' and the Chamber doorway's
        // taps, the Chamber floor band); it compiles and every other check passes. No line of the game's C# may carry statements after a comment.
        // Oct 8 (Jeffrey, #134 N1): the first form wanted a `);` and then a statement after a `;`, so a lone swallowed assignment (Build M's
        // grey on the Wing room's Dial label, Build W's jumpsShown) or a lone call (the Chamber's Continue hide, #131) passed it.
        static void ValidateNoSwallowedCode()
        {
            var swallowed = new List<string>();
            foreach (var file in Directory.GetFiles("Assets/CelestialDial", "*.cs").Concat(Directory.GetFiles("Assets/Editor/CelestialDial", "*.cs")))
            {
                var lines = File.ReadAllLines(file);
                for (int n = 0; n < lines.Length; n++)
                {
                    int c = CommentStart(lines[n]); if (c < 0) continue;
                    if (HidesStatement(lines[n].Substring(c + 2))) swallowed.Add(Path.GetFileName(file) + ":" + (n + 1));
                }
            }
            Check(swallowed.Count == 0, "no line of the game's C# hides statements behind a // comment" + (swallowed.Count > 0 ? ": " + string.Join(", ", swallowed) : ""));
        }
        // A comment's tail hides code if it has the first form's shape, a call (a name right against its `(`, closed by `);`), or an
        // assignment (`=`, `+=`, `-=` and the like) whose right side reads as code; a name runs through `.`, `?.`, indexers, type arguments and calls.
        // Prose like "owner (worksheet section 13);" puts a space before its `(`. Prose like "dialVoice = the box wears the Dial's blue;"
        // or "size = 340 wide;" has two plain words (or a number and a word) side by side, which code has only around a keyword
        // ("new Color", "is not null"). String and char literals are blanked first, so quoted words count as neither.
        static readonly System.Text.RegularExpressions.Regex FirstFormClose = new System.Text.RegularExpressions.Regex(@"\)\s*;");
        static readonly System.Text.RegularExpressions.Regex FirstFormStatement = new System.Text.RegularExpressions.Regex(@";\s*(?:var |string |int |float |bool )?[A-Za-z_][\w\.\[\]]*\s*(?:\(|=[^=>])");
        static readonly System.Text.RegularExpressions.Regex Literal = new System.Text.RegularExpressions.Regex(@"""(?:[^""\\]|\\.)*""|'(?:[^'\\]|\\.)'");
        const string CodeName = @"(?<![\w.])[A-Za-z_]\w*(?:\??\.[A-Za-z_]\w*|\??\[[^\]\[;]*\]|<[^<>;]*>|\([^();]*\))*";
        static readonly System.Text.RegularExpressions.Regex Call = new System.Text.RegularExpressions.Regex(CodeName + @"\((?:(?!//)[^;])*\)\s*;");
        static readonly System.Text.RegularExpressions.Regex Assignment = new System.Text.RegularExpressions.Regex(CodeName + @"\s*(?:[-+*/%&|^]|\?\?)?=(?![=>])(?<rhs>(?:(?!//)[^;])+);");
        const string Keyword = @"(?:new|is|as|not|and|or|in|out|ref|await|typeof|nameof|default|null|true|false|this|base|var|stackalloc|checked|unchecked|when|with|switch)(?![\w.])";
        static readonly System.Text.RegularExpressions.Regex PlainWords = new System.Text.RegularExpressions.Regex(@"(?<![\w.])(?!" + Keyword + @")\w[\w.]*\s+(?!" + Keyword + @")[A-Za-z_]");
        static bool HidesStatement(string tail)
        {
            if (FirstFormClose.IsMatch(tail) && FirstFormStatement.IsMatch(tail)) return true;
            var code = Literal.Replace(tail, "\"\"");
            return Call.IsMatch(code) || Assignment.Matches(code).Cast<System.Text.RegularExpressions.Match>().Any(m => !PlainWords.IsMatch(m.Groups["rhs"].Value));
        }
        static int CommentStart(string line) // the first // outside a string or char literal
        {
            bool inString = false, inChar = false;
            for (int i = 0; i < line.Length - 1; i++)
            {
                char ch = line[i];
                if (inString) { if (ch == '\\') i++; else if (ch == '"') inString = false; continue; }
                if (inChar) { if (ch == '\\') i++; else if (ch == '\'') inChar = false; continue; }
                if (ch == '"') inString = true; else if (ch == '\'') inChar = true;
                else if (ch == '/' && line[i + 1] == '/') return i;
            }
            return -1;
        }
        // The launch (owner, Oct 9, 86bcg62x3, round 3): the logo screen, the launch movie and the menu in the flow; the shot list as data; the
        // menu's and the slot list's layout; the three save slots on a stand-in store (slot 1 is today's key, so an old save is slot 1 as it stands)
        // the owner's approved screen-reader lines for the launch (Oct 9: "Approved as drafted"), one per shot in order
        static readonly string[] SliceLaunchLines={"TSG Games.","","A zodiac wheel draws itself in pencil, then adds its twelve symbols.","","The wheel lights up in gold, and the dark fills with stars.","The wheel burns with golden fire and begins to spin.","The fire turns to light. The wheel becomes Earth.","Earth, seen from space.","North America at night.","The United States at night.","New York State.","New York City, between its rivers.","Brooklyn's rooftops at night.","A street of apartment buildings, a few cars, lit shopfronts.","One building. Every window is dark but one."};
        static void ValidateLaunch()
        {
            Check((int)SliceScreen.Prologue==13 && (int)SliceScreen.Logo==14 && (int)SliceScreen.Intro==15 && (int)SliceScreen.Menu==16 && (int)SliceScreen.SaveSlots==17,"the launch's screens (Logo, Intro, Menu, SaveSlots) are appended after Prologue, so none is renumbered");
            var log=new List<string>();var flow=new SliceFlow();flow.Logged+=log.Add;
            Check(flow.BeginLaunch() && flow.Screen==SliceScreen.Logo && flow.AtLaunch && !flow.BeginLaunch() && !flow.CanContinue && !flow.Continue() && !flow.SkipPrologue() && !flow.OpenSlots("new") && log.SequenceEqual(new[]{"screen_entered:Logo"}),"a launch opens on the logo screen (screen_entered:Logo, once); Continue, the prologue's Skip and the slot list do nothing there");
            Check(flow.EndLogo() && flow.Screen==SliceScreen.Intro && !flow.EndLogo() && flow.EndIntro() && flow.Screen==SliceScreen.Menu && flow.AtMenu && !flow.AtLaunch && log.SequenceEqual(new[]{"screen_entered:Logo","screen_entered:Intro","intro_ended","screen_entered:Menu"}),"the logo gives way to the launch movie, and its end to the menu, each logged as every screen is");
            var skipped=new SliceFlow();var skipLog=new List<string>();skipped.Logged+=skipLog.Add;skipped.BeginLaunch();
            Check(skipped.SkipLaunch() && skipped.Screen==SliceScreen.Menu && !skipped.SkipLaunch() && skipLog.SequenceEqual(new[]{"screen_entered:Logo","launch_skipped","screen_entered:Menu"}),"one Skip on the logo screen goes to the menu");
            var mid=new SliceFlow();mid.BeginLaunch();mid.EndLogo();Check(mid.SkipLaunch() && mid.Screen==SliceScreen.Menu,"the same Skip in the launch movie goes to the menu");
            Check(!mid.OpenSlots("other") && mid.OpenSlots("load") && mid.Screen==SliceScreen.SaveSlots && mid.SlotsFor=="load" && !mid.OpenSlots("new") && mid.OpenMenu() && mid.Screen==SliceScreen.Menu && mid.SlotsFor=="" && mid.OpenSlots("new") && mid.SlotsFor=="new","Load Game and New Game open the slot list for their purpose; Back returns to the menu");
            var fromSettings=new SliceFlow();var settingsLog=new List<string>();fromSettings.Logged+=settingsLog.Add;
            Check(fromSettings.OpenMenu() && fromSettings.Screen==SliceScreen.Menu && settingsLog.SequenceEqual(new[]{"screen_entered:Menu"}),"Settings' Main menu comes straight to the menu, past the logo and the movie");
            var playing=new SliceFlow();playing.SkipPrologue();Check(!playing.OpenMenu() && !playing.BeginLaunch() && playing.Screen==SliceScreen.Identity,"a game under way neither launches nor opens the menu in place (the menu reloads the scene)");
            Check(new SliceFlow().Screen==SliceScreen.Prologue,"a fresh flow still starts at the prologue, where New Game, Start over and the DEV samples arrive");
            // the shot list (Dante's, the owner's beats in order); timings are working choices
            var shots=SliceView.LaunchShots;var logo=shots[0];
            Check(logo.Logo && logo.Id=="logo" && shots.Count(x=>x.Logo)==1 && logo.Steps.Length==2 && logo.Steps[0].Frame=="studio-logo" && logo.Steps[0].How=="fade" && logo.Steps[0].FadeEase=="smooth" && Mathf.Abs(logo.Steps[0].Seconds-SliceView.LogoFade)<.001f && logo.Steps[1].How=="out" && logo.Steps[1].FadeEase=="smooth" && Mathf.Abs(logo.Steps[1].At-(SliceView.LogoFade+SliceView.LogoHold))<.001f && Mathf.Abs(SliceView.LogoFade-1.2f)<.001f && Mathf.Abs(SliceView.LogoHold-3.5f)<.001f && Mathf.Abs(SliceView.LogoSeconds-5.9f)<.001f && Mathf.Abs(SliceView.Eased("smooth",.5f)-.5f)<.001f && SliceView.Eased("smooth",.1f)<.1f,"the logo screen (owner, Oct 9: \"a lil more screen time\", \"a nice fade in and fade out\"): the Signature Crown Mark eases in over 1.2 s, holds 3.5 s, eases out over 1.2 s ("+SliceView.LogoSeconds+" s)");
            var ids=shots.Select(x=>x.Id).ToArray();
            Check(ids.SequenceEqual(new[]{"logo","dark","draw","glyphs","alive","burn","transform","earth","continents","america","newyork-state","newyork-city","brooklyn","block","window"}),"the launch movie in the owner's order (round 4): darkness, the wheel drawing itself, its symbols, coming alive, burning alive, transforming into Earth, the calm Earth, then flying down: the continents, America, New York State, New York City, Brooklyn, the block, the one lit window: "+string.Join(", ",ids));
            var order=new List<string>();foreach(var x in shots)foreach(var st in x.Steps)if(st.Frame!=null && !order.Contains(st.Frame))order.Add(st.Frame);
            var layers=new[]{"intro-wheel-pencil-lines","intro-wheel-pencil-glyphs","intro-wheel-lit","intro-wheel-burning","intro-earth"};var fulls=new[]{"intro-stars","intro-continents","intro-america","intro-newyork-state","intro-newyork-city","intro-brooklyn","intro-block"};
            Check(order.SequenceEqual(new[]{"studio-logo","intro-wheel-pencil-lines","intro-wheel-pencil-glyphs","intro-stars","intro-wheel-lit","intro-wheel-burning","intro-earth","intro-continents","intro-america","intro-newyork-state","intro-newyork-city","intro-brooklyn","intro-block","prologue-city"}) && SliceView.LaunchFrames.Count()==14,"its frames in order end on the opening's own city frame, the one lit window (Oct 9: no in-between wheel-to-Earth frame)");
            Check(layers.All(f=>{var a=Slots.Find(f);return a!=null && a.Width==360 && a.Height==360 && a.MaxSize==1024;}) && fulls.All(f=>{var a=Slots.Find(f);return a!=null && a.Width==360 && a.Height==800 && a.MaxSize==2048;}) && Slots.Art.Count(a=>a.Name.StartsWith("intro-"))==12 && Slots.Find("intro-wheel-pencil")==null && Slots.Find("intro-wheel-earth")==null,"five round layers (360 x 360, 720 x 720 files, centred) over one painted sky, and the seven full frames of the sky and the descent; the single pencil frame and the baked wheel-to-Earth frame are gone (Oct 9)");
            var logoSlot=Slots.Find("studio-logo");Check(logoSlot!=null && logoSlot.Width==320 && logoSlot.Height==320 && logoSlot.MaxSize==1024,"the studio's logo is a 320 x 320 slot on the black screen, drawn whole");
            Check(SliceView.MovieSeconds>=28 && SliceView.MovieSeconds<=32,"the launch movie runs about 30 s (round 4): "+SliceView.MovieSeconds.ToString("0.0")+" s");
            Check(shots.All(x=>!x.Page && !x.Under && !x.Flash && x.ScaleFrom==1 && x.ScaleTo==1),"no comic page and no flash in the launch; the camera itself holds still");
            Check(shots.Select(x=>x.Line??"").SequenceEqual(SliceLaunchLines),"each launch shot's spoken line for the screen reader, exactly as the owner approved them (Oct 9), none for the dark and the symbols; nothing written on screen");
            Check(Slots.Sounds.Length==7,"no sound for the launch (the sound hold): still the seven sound slots");
            var draw=shots.First(x=>x.Id=="draw").Steps.Single();var glyphs=shots.First(x=>x.Id=="glyphs").Steps.Single();
            Check(draw.How=="sweep" && draw.Frame=="intro-wheel-pencil-lines" && draw.Sectors==0 && Mathf.Abs(draw.Seconds-2.5f)<.001f && glyphs.How=="sweep" && glyphs.Frame=="intro-wheel-pencil-glyphs" && glyphs.Sectors==12 && Mathf.Abs(glyphs.Seconds-12*SliceView.GlyphStep)<.001f && Mathf.Abs(SliceView.GlyphStep-.12f)<.001f,"the wheel draws itself: a radial sweep reveals its lines over about 2.5 s, then its twelve symbols appear one at a time, 0.12 s apart");
            Check(SliceView.SweepOrigin==1 && !SliceView.SweepClockwise && Mathf.Approximately(SliceView.SweepOffset,136.5f) && SliceView.GlyphOrigin==1 && !SliceView.GlyphClockwise && Mathf.Approximately(SliceView.GlyphOffset,136.5f) && Mathf.Approximately(SliceView.SweepAngle(0),136.5f) && Mathf.Approximately(SliceView.SweepAngle(.5f),316.5f),"the sweeps start at the Aries cusp (136.5 degrees, about 10:30) and run counter-clockwise, as the art lane measured");
            float[] glyphAt={148.8f,179.7f,208.8f,238.3f,268.8f,300.9f,330.0f,360.5f,389.8f,419.1f,450.1f,481.6f}; // Aries to Pisces, unwrapped from the cusp
            Check(Enumerable.Range(0,12).All(k=>glyphAt[k]>136.5f+30*k && glyphAt[k]<136.5f+30*(k+1)),"each symbol falls inside its own 30 degree step from the cusp, Aries first: step k reveals Aries to the k-th sign");
            var zoomShots=shots.Where(x=>x.WheelZoom).Select(x=>x.Id).ToArray();float zoomTotal=shots.Where(x=>x.WheelZoom).Sum(x=>x.Seconds);
            Check(zoomShots.SequenceEqual(new[]{"draw","glyphs","alive"}) && Mathf.Approximately(SliceView.WheelZoomAt(0,zoomTotal),SliceView.WheelZoomFrom) && Mathf.Approximately(SliceView.WheelZoomAt(zoomTotal,zoomTotal),1) && SliceView.WheelZoomFrom<1 && SliceView.WheelZoomFrom>=.75f && Enumerable.Range(1,20).All(i=>SliceView.WheelZoomAt(zoomTotal*i/20f,zoomTotal)>SliceView.WheelZoomAt(zoomTotal*(i-1)/20f,zoomTotal) && SliceView.WheelZoomAt(zoomTotal*i/20f,zoomTotal)<=1),"the owner's slow zoom on the wheel: from "+SliceView.WheelZoomFrom+" to 1 across the draw, the symbols and the coming alive, rising all the way, never past 1, stopping before the burn");
            var alive=shots.First(x=>x.Id=="alive").Steps;Check(alive.Any(st=>st.Frame=="intro-stars" && st.How=="fade") && alive.Any(st=>st.Frame=="intro-wheel-lit" && st.How=="fade"),"it comes alive: the lit layer crossfades in while the black behind becomes the starry sky");
            var burn=shots.First(x=>x.Id=="burn").Steps.Single();var tr=shots.First(x=>x.Id=="transform").Steps;var earthIn=tr.First(st=>st.Frame=="intro-earth");var burning=tr.First(st=>st.Frame=="intro-wheel-burning" && st.How=="cut");var away=tr.First(st=>st.How=="away");
            Check(burn.Frame=="intro-wheel-burning" && burn.How=="fade" && burn.SpinFrom==0 && burn.SpinEase=="in" && burning.SpinFrom==burn.SpinTo && burning.SpinTo==SliceView.SpinTurn && burning.SpinEase=="out" && Mathf.Abs(burning.SpinSeconds-SliceView.TransformSeconds)<.001f,"it burns alive and starts to spin (easing in), and spins on through the transform, easing to a stop");
            float vIn=2*burn.SpinTo/burn.SpinSeconds,vOut=2*(burning.SpinTo-burning.SpinFrom)/burning.SpinSeconds;
            Check(Mathf.Abs(vIn-vOut)<.01f,"the spin keeps one speed where the burn meets the transform ("+vIn.ToString("0.0")+" degrees a second)");
            Check(away.Frame=="intro-wheel-burning" && away.At==0 && Mathf.Abs(away.Seconds-3)<.001f && earthIn.How=="fade" && earthIn.Below && !earthIn.Over && earthIn.SpinFrom==0 && earthIn.SpinTo==0 && Mathf.Abs(earthIn.At-.3f)<.001f && Mathf.Abs(earthIn.Seconds-2.6f)<.001f && earthIn.At+earthIn.Seconds<=SliceView.TransformSeconds,"the transform (owner, Oct 9: \"it doenst blend that cleanly\"): the burning wheel spins on and fades away over 3 s while a level Earth fades in under it from 0.3 s over 2.6 s");
            Check(shots.SelectMany(x=>x.Steps).Where(st=>st.Frame=="intro-earth").All(st=>st.SpinFrom==0 && st.SpinTo==0),"Earth never turns: no step spins intro-earth");
            Check(shots.Where(x=>x.Id=="earth" || ids.Skip(8).Contains(x.Id)).All(x=>x.Steps.Length==1 && x.Steps[0].PushTo>1 && x.Steps[0].PushTo<=1.2f && x.Steps[0].PushSeconds<=x.Seconds+.001f) && shots.Where(x=>ids.Skip(8).Contains(x.Id)).All(x=>x.Steps[0].Over),"the calm Earth and the descent push in on each frame (at most 1.2, inside its own shot); each descent frame comes in over what shows");
            Check(Mathf.Approximately(SliceView.Cover(0),1) && Mathf.Abs(SliceView.Cover(8)-(Mathf.Cos(8*Mathf.Deg2Rad)+800f/360f*Mathf.Sin(8*Mathf.Deg2Rad)))<.0001f && SliceView.Cover(-8)==SliceView.Cover(8),"a turned full frame scales just enough to cover the column (a round layer turns as it is)");
            var window=shots.Last();Check(window.Steps[0].Frame=="prologue-city" && Vector2.Distance(window.Steps[0].PushView,SliceView.ViewOn(.581f,.237f))<.002f,"the last shot pushes toward the measured lit window (58.1% across, 23.7% down)");
            // the menu's life (owner, Oct 9): the window's pulse and the rat
            Check(SliceView.GlowLow>=.1f && SliceView.GlowHigh<=.55f && SliceView.GlowLow<SliceView.GlowStill && SliceView.GlowStill<SliceView.GlowHigh && Mathf.Abs(SliceView.GlowPeriod-3.5f)<.001f && Mathf.Approximately(SliceView.GlowAt(0),SliceView.GlowLow) && Mathf.Approximately(SliceView.GlowAt(SliceView.GlowPeriod/2),SliceView.GlowHigh) && Enumerable.Range(1,20).All(i=>SliceView.GlowAt(i*SliceView.GlowPeriod/40f)>SliceView.GlowAt((i-1)*SliceView.GlowPeriod/40f)),"the window's glow pulses slowly (a 3.5 s sine from 0.15 to 0.5, rising smoothly, never a blink); reduced motion holds it at 0.3");
            Check(Mathf.Abs(SliceView.WindowX-17.7f)<.5f && Mathf.Abs(SliceView.WindowY-274f)<1 && SliceView.GlowScale>=2.5f && SliceView.GlowScale<=3.5f,"the glow sits on menu-city's lit window, as measured (17.7 right of the middle, 274 down), about 3x its size");
            Check(Enumerable.Range(1,4).All(k=>{var a=Slots.Find("menu-rat-"+k);return a!=null && a.Width==48 && a.Height==24 && a.MaxSize==256;}) && SliceView.RatSlot(3)=="menu-rat-4","the rat's run cycle: four 48 x 24 slots (96 x 48 files, cap 256)");
            Check(SliceView.RatY-SliceView.RatHeight/2>SliceView.MenuButtonY(3)+SliceView.MenuButtonHeight/2 && SliceView.RatY+SliceView.RatHeight/2<=800 && Mathf.Abs(SliceView.RatSeconds-2.2f)<.001f && SliceView.RatFps==12 && SliceView.RatFirstMin==6 && SliceView.RatFirstMax==10 && SliceView.RatGapMin==15 && SliceView.RatGapMax==30,"the rat runs on the street below the buttons (y "+SliceView.RatY+"), across the width in 2.2 s at 12 frames a second, first after 6 to 10 s idle, then every 15 to 30 s");
            // the menu: the board's numbers, the Room look, the four words in order
            Check(SliceView.MenuWords.SequenceEqual(new[]{"Continue","New Game","Load Game","Settings"}),"the menu reads Continue, New Game, Load Game, Settings (the boards' order)");
            Check(SliceView.MenuButtonWidth==232 && SliceView.MenuButtonHeight==44 && SliceView.MenuButtonHeight>=ButtonLook.MinTarget && Mathf.Approximately(SliceView.MenuButtonY(0)-SliceView.MenuButtonHeight/2,496) && Mathf.Approximately(SliceView.MenuButtonY(1)-SliceView.MenuButtonY(0),56) && SliceView.MenuButtonY(3)+SliceView.MenuButtonHeight/2<=800-40,"the menu's buttons: 232 x 44, centred, 12 px apart from 496 down, the last clear of the bottom");
            Check(SliceView.MenuTitleY==115 && SliceView.MenuTitleWidth==270 && Mathf.Abs(SliceView.MenuTitleWidth/SliceView.MenuTitleHeight-1.38f)<.01f && Slots.Find("menu-title")!=null && Slots.Find("menu-title").Width==270 && Slots.Find("menu-title").Height==SliceView.MenuTitleHeight && Mathf.Abs(SliceView.MenuTitleY-SliceView.MenuTitleHeight/2-16)<=2 && SliceView.MenuTitleY+SliceView.MenuTitleHeight/2<SliceView.MenuButtonTop && Slots.Find("menu-city")!=null && Slots.Find("menu-city").Width==360 && Slots.Find("menu-city").Height==800,"the menu's slots: menu-city behind (360 x 800), menu-title near the top (the seal at 1.38:1, 270 wide, centred 115 down, its top about 16 px clear of the safe area, clear of the buttons)");
            Check(SliceView.SlotCardHeight>=ButtonLook.MinTarget && SliceView.SlotCardWidth<=340 && SliceView.SlotCardY(1)-SliceView.SlotCardHeight/2>SliceView.SlotsTitleY+12 && SliceView.SlotCardY(3)+SliceView.SlotCardHeight/2<SliceView.SlotsBackY-22 && SliceView.SlotCardY(2)-SliceView.SlotCardY(1)==SliceView.SlotCardHeight+SliceView.SlotCardGap,"the slot list: three cards (44 px or more) under the title, Back below them");
            Check(SliceView.ConfirmHeight>=124+22+12 && SliceView.ConfirmWidth<=340,"the question's box holds its line and its two 44 px buttons (Start, Back)");
            Check(new[]{SliceView.ReplaceLine,SliceView.MainMenuAskLine,SliceView.StartWords,SliceView.BackWords,SaveSlots.EmptyWords,SettingsMenu.MainMenuWords}.All(t=>t.All(c=>c<256) && !t.Contains("—")),"the launch's new words are plain Latin-1 (the web font draws nothing past it)");
            // the three slots, on a stand-in store
            var store=new Dictionary<string,string>();var was=SaveSlots.Current;
            SaveSlots.Current=new SaveSlots.Store{Get=k=>store.TryGetValue(k,out var v)?v:"",Set=(k,v)=>store[k]=v,Delete=k=>store.Remove(k),Has=store.ContainsKey,Flush=()=>{}};
            try
            {
                Check(SaveSlots.Count==3 && SaveSlots.Key(1)==SliceView.SaveKey && SaveSlots.Key(1)=="ascendant.v02.save" && new[]{SaveSlots.Key(2),SaveSlots.Key(3),SaveSlots.LastKey}.Distinct().Count()==3 && !new[]{SaveSlots.Key(2),SaveSlots.Key(3),SaveSlots.LastKey}.Contains(SliceView.SaveKey),"three slots: slot 1 keeps today's key, slots 2 and 3 and the last-played slot take their own");
                Check(!SaveSlots.AnySave && SaveSlots.InPlay==1 && !SaveSlots.HasLast && SaveSlots.ContinueSlot==0 && Enumerable.Range(1,3).All(i=>SaveSlots.Describe(i).Name=="Empty" && !SaveSlots.Describe(i).Filled),"a first launch: every slot reads Empty, nothing to continue, slot 1 in play");
                var old=new SaveData{version=4,playerName="Davon",chartFrom="known",sunSign=4,moonSign=7,risingSign=0,atriumStage=2,keyEarned=true,keys=2};
                store[SliceView.SaveKey]=JsonUtility.ToJson(old); // a save written before the menu, under the one key there was
                var card=SaveSlots.Describe(1);
                Check(card.Filled && SaveSlots.CanLoad(1) && SaveSlots.AnySave && card.Name=="Davon" && card.Keys=="Keeper Keys: 2" && card.BigThree=="Leo sun · Scorpio moon · Aries rising" && card.Spoken=="Slot 1: Davon, Keeper Keys: 2, Leo sun, Scorpio moon, Aries rising","an old single save shows up in slot 1 as it stands (no migration): its name, Keeper Keys and Big Three");
                Check(SaveSlots.ContinueSlot==1 && !SaveSlots.CanLoad(2) && !SaveSlots.CanLoad(3),"Continue picks slot 1 when it is the only save");
                store[SaveSlots.Key(3)]=JsonUtility.ToJson(new SaveData{playerName="Ana",sunSign=9,atriumStage=3,keyEarned=true,keys=4});
                SaveSlots.InPlay=3;Check(SaveSlots.HasLast && store[SaveSlots.LastKey]=="3" && SaveSlots.ContinueSlot==3 && SaveSlots.Describe(3).BigThree=="Capricorn sun" && SaveSlots.Describe(3).Keys=="Keeper Keys: 4","Continue picks the slot played last (a number, no date); a card shows only the signs the save holds");
                SaveSlots.InPlay=2;Check(SaveSlots.ContinueSlot==1,"if the slot played last holds nothing to load, Continue falls back to the first that does");
                store[SaveSlots.Key(2)]=JsonUtility.ToJson(new SaveData{playerName="Early",sunSign=1,atriumStage=1});
                Check(!SaveSlots.CanLoad(2) && !SaveSlots.Describe(2).Filled && SaveSlots.Describe(2).Name=="Empty" && SaveSlots.ContinueSlot==1,"a slot that would not load (as a reload today: before the Atrium's stage 2) reads Empty");
                SaveSlots.InPlay=9;Check(SaveSlots.InPlay==3,"the slot number stays 1 to 3");SaveSlots.InPlay=0;Check(SaveSlots.InPlay==1,"and never below 1");
                SaveSlots.Clear(1);Check(!SaveSlots.CanLoad(1) && !store.ContainsKey(SliceView.SaveKey) && SaveSlots.CanLoad(3),"clearing a slot clears that slot only");
                Check(SaveSlots.BigThree(-1,-1,-1)=="" && SaveSlots.BigThree(0,-1,5)=="Aries sun · Virgo rising","the Big Three's words leave out what the save doesn't hold");
            }
            finally{SaveSlots.Current=was;}
        }
        // The opening scene (owner, Oct 8, 86bcfhmha): the flow's prologue, the shot list as data, the orb's stars, and their slots
        static void ValidatePrologue()
        {
            var log=new List<string>();var flow=new SliceFlow();flow.Logged+=log.Add;
            Check(flow.AtPrologue && !flow.CanContinue && !flow.Continue() && flow.Screen==SliceScreen.Prologue && flow.OpeningAnswers==0,"the prologue plays on its own: Continue does nothing there");
            flow.ChooseBirth("skip");Check(flow.Screen==SliceScreen.Prologue && flow.BirthChoice=="","nothing of the opening's questions answers on the prologue");
            flow.OpenPrologue();flow.OpenPrologue();Check(log.Count(e=>e=="screen_entered:Prologue")==1,"screen_entered:Prologue is logged once, on the same path as every screen");
            Check(flow.EndPrologue() && flow.Screen==SliceScreen.Identity && log.SkipWhile(e=>e!="prologue_ended").Take(2).SequenceEqual(new[]{"prologue_ended","screen_entered:Identity"}) && !flow.EndPrologue() && !flow.SkipPrologue(),"the prologue's end reaches Identity, once");
            var skipped=new SliceFlow();var skipLog=new List<string>();skipped.Logged+=skipLog.Add;Check(skipped.SkipPrologue() && skipped.Screen==SliceScreen.Identity && skipLog.SequenceEqual(new[]{"prologue_skipped","screen_entered:Identity"}),"Skip reaches Identity and says it was skipped");
            var saved=new SliceFlow();Check(saved.Restore(new SaveData{sunSign=1,atriumStage=2,keyEarned=true,keys=1}) && saved.Screen==SliceScreen.Hub && !saved.AtPrologue && !saved.SkipPrologue(),"a restored save resumes at the Hub and never sees the prologue");
            Check((int)SliceScreen.Prologue==13 && (int)SliceScreen.Journal==12 && (int)SliceScreen.Identity==0,"Prologue is appended to the screens, so none is renumbered");
            // the stars follow the steps answered as they stand: the name, then the birth question and each step after it; Change my answer takes them back
            var star=new SliceFlow(()=>1);star.SkipPrologue();var counts=new List<int>{star.OpeningAnswers};star.Continue();counts.Add(star.OpeningAnswers);
            star.ChooseBirth("skip");counts.Add(star.OpeningAnswers);star.PickSkipSun(1);counts.Add(star.OpeningAnswers);star.PickSkipMoon(3);counts.Add(star.OpeningAnswers);star.PickRising(4);counts.Add(star.OpeningAnswers);
            Check(counts.SequenceEqual(new[]{0,1,2,3,4,5}),"the skip path's stars: none on the name, one once it is continued, then one per answer: "+string.Join(",",counts));
            star.ChooseBirth("");Check(star.OpeningAnswers==1,"Change my answer takes the birth's stars back, the name's stays");
            var london=Places.Find("London, Britain (UK)");var most=new SliceFlow(()=>1);most.SkipPrologue();most.Continue();most.ChooseBirth("chart");most.SetBirthDate(1990,4,20);most.SetBirthTime(-1);most.SetBirthPlace(london);
            int beforeCusp=most.OpeningAnswers;most.PickCuspSun(0);most.PickMoon(0);most.PickRising(5);
            Check(beforeCusp==5 && most.BirthDone && most.OpeningAnswers==SliceFlow.MostOpeningAnswers && SliceFlow.MostOpeningAnswers==8,"the longest path (a cusp day with no time: the date, time, place, cusp, moon and rising) gathers all eight stars");
            var amend=BirthPlayer(o=>{o.ChooseBirth("skip");o.PickSkipSun(1);o.PickSkipMoon(3);o.PickRising(4);},out _);amend.StartAddFacts();
            Check(amend.Screen==SliceScreen.Birth && amend.Amending && amend.OpeningAnswers==0,"\"Your Birth\" from the journal has no stars (the orb is the opening's)");
            // the shot list: the brief's seven shots, the sixteen frames (with the Oct 9 in-betweens), motion only inside a frame, about 45-60 s, a spoken line each
            var shots=SliceView.PrologueShots;
            Check(shots.Select(x=>x.Id).SequenceEqual(new[]{"city","desk","light","street","puzzled","turn","flash"}) && shots.Last().Flash && shots.Count(x=>x.Flash)==1,"seven shots in the brief's order, the flash last");
            var frames=new[]{"prologue-city","prologue-desk","prologue-notebook","prologue-light","prologue-look","prologue-headphones","prologue-street-above","prologue-puzzled","prologue-caspar-back","prologue-caspar-turn","prologue-caspar-face","prologue-caspar-eyes","prologue-notebook-2","prologue-notebook-3","prologue-light-up","prologue-headphones-mid"};
            Check(SliceView.PrologueFrames.OrderBy(f=>f).SequenceEqual(frames.OrderBy(f=>f)) && frames.All(f=>{var a=Slots.Find(f);return a!=null && a.Width==360 && a.Height==800 && a.MaxSize==2048;}),"the sixteen frames are the sixteen 360 x 800 slots, capped at 2048 (the owner's Oct 9 animation frames included)");
            // the owner, Oct 9: slight animations as frame swaps (ruling 4): the hand writes, the head turns, the headphones fall
            var deskShot=shots.First(x=>x.Id=="desk");var loop=deskShot.Steps.FirstOrDefault(st=>st.How=="loop");var notebookPanel=deskShot.Steps.First(st=>st.Frame=="prologue-notebook" && st.How=="panel");
            Check(loop!=null && loop.Frame=="prologue-notebook" && loop.Loop.SequenceEqual(new[]{"prologue-notebook-2","prologue-notebook-3"}) && Mathf.Abs(loop.LoopFrame-.25f)<.001f && loop.At>=notebookPanel.At+SliceView.PanelSlide-.001f && Mathf.Abs(loop.At+loop.Seconds-deskShot.Seconds)<.01f,"shot 2: once the notebook's panel has landed, the hand writes in a loop (notebook, -2, -3) at 4 frames a second to the shot's end");
            var lightShot=shots.First(x=>x.Id=="light");var ls=lightShot.Steps;
            Check(ls.Select(st=>st.Frame).SequenceEqual(new[]{"prologue-desk","prologue-light","prologue-light-up","prologue-look","prologue-headphones-mid","prologue-headphones"}) && ls[2].How=="fade" && Mathf.Abs(ls[2].Seconds-.15f)<.001f && ls[2].At-(ls[1].At+ls[1].Seconds)>=.8f-.001f && ls[2].At-(ls[1].At+ls[1].Seconds)<=1f+.001f && ls[3].How=="cut" && ls[4].Moving && !ls[5].Moving && ls[5].At-ls[4].At>=.12f-.001f && ls[5].At-ls[4].At<=.15f+.001f && Mathf.Abs(lightShot.Seconds-9f)<.01f,
                "shot 3: the head turns to the window (a 0.15 s crossfade 0.8 to 1 s after the light), the close-up, then the headphones fall look, mid, down 0.12 to 0.15 s apart (the mid frame skipped under reduced motion); the shot stays 9 s");
            Check(SliceView.PrologueSeconds>=45 && SliceView.PrologueSeconds<=60,"the prologue runs about 45-60 s: "+SliceView.PrologueSeconds+" s");
            var edges=new List<string>();
            foreach(var shot in shots){ for(int i=0;i<=20;i++){ float k=i/20f,sc=Mathf.Lerp(shot.ScaleFrom,shot.ScaleTo,k);var v=Vector2.Lerp(shot.ViewFrom,shot.ViewTo,k);var o=SliceView.CameraOffset(sc,v);
                if(sc<1 || Mathf.Abs(o.x)>180*(sc-1)+.01f || Mathf.Abs(o.y)>400*(sc-1)+.01f || Mathf.Abs(v.x)>1 || Mathf.Abs(v.y)>1) edges.Add(shot.Id+" at "+k); }
                foreach(var st in shot.Steps){ if(st.At<0 || st.At>=shot.Seconds) edges.Add(shot.Id+" step at "+st.At); if(st.How=="panel" && (Mathf.Abs(st.X)+st.Width/2>180 || st.Top-st.Height/2<0 || st.Top+st.Height/2>800)) edges.Add(shot.Id+" panel "+st.Frame);
                    float sx=float.IsNaN(st.SrcX)?st.X:st.SrcX,sy=float.IsNaN(st.SrcY)?st.Top:st.SrcY; if(st.How=="panel" && (Mathf.Abs(sx)+st.Width/2>180 || sy-st.Height/2<0 || sy+st.Height/2>800)) edges.Add(shot.Id+" panel source "+st.Frame);
                    if(st.PushTo!=0 && (st.PushTo<1 || Mathf.Abs(st.PushView.x)>1 || Mathf.Abs(st.PushView.y)>1 || st.PushSeconds<=0)) edges.Add(shot.Id+" push "+st.Frame); } }
            Check(edges.Count==0,"motion stays inside the frame: every scale 1 or more, every pan within the frame's edges, every panel inside the screen and showing a part inside its frame, every frame's own push at scale 1 or more"+(edges.Count>0?"; off: "+string.Join(", ",edges):""));
            Check(shots.Where(x=>x.Id=="desk"||x.Id=="puzzled").All(x=>x.Steps.All(st=>st.How=="panel"||st.How=="loop")) && shots.First(x=>x.Id=="desk").Steps.Count(st=>st.How=="panel")==2 && shots.First(x=>x.Id=="puzzled").Under,"shots 2 and 5 are comic panels that slide in (two on the desk's page; the puzzled look over the street, dimmed)");
            // the owner, Oct 8: the intro ends zooming into his glowing eyes, then the flash (the brief's fading glow is withdrawn)
            var turn=shots.First(x=>x.Id=="turn");var face=turn.Steps[2];var eyes=turn.Steps[3];
            Check(turn.Steps.Select(st=>st.Frame).SequenceEqual(new[]{"prologue-caspar-back","prologue-caspar-turn","prologue-caspar-face","prologue-caspar-eyes"}) && eyes.How=="fade" && eyes.At>face.At && face.PushTo>1 && eyes.PushTo>1 && face.At+face.PushSeconds>=eyes.At+eyes.Seconds && eyes.At+eyes.PushSeconds>=turn.Seconds-.01f && turn.Steps.Take(2).All(st=>st.PushTo==0) && shots[shots.Length-1].Flash && Slots.Find("prologue-caspar-face-dim")==null && SliceView.FlashBloom>1,
                "the turn: back and three-quarter held still; on the face a slow push-in toward his eyes, which carries on through the crossfade to the eyes' close-up and on to the flash, where the white blooms out of the glow (no dimmed face)");
            // the push-in targets, measured on the final art (the shares across and down that each push holds still)
            Check(Vector2.Distance(face.PushView,SliceView.ViewOn(.566f,.213f))<.002f && Vector2.Distance(eyes.PushView,SliceView.ViewOn(.507f,.471f))<.002f && Vector2.Distance(shots[0].ViewFrom,SliceView.ViewOn(.581f,.237f))<.002f && shots[0].ViewFrom==shots[0].ViewTo,"the pushes hold the measured points: the city's lit window (58.1%, 23.7%), his eyes on the face (56.6%, 21.3%), the glow on the close-up (50.7%, 47.1%)");
            var banned=new[]{"silence","performing","weight","mirror","real","really","very","truly","actually","basically","literally"};
            Check(shots.All(x=>!string.IsNullOrEmpty(x.Line) && !x.Line.Contains("—") && !x.Line.Contains("–") && !x.Line.Split(' ',',','.').Any(w=>banned.Contains(w.ToLowerInvariant())) && x.Line.Length<=110),"each shot has one plain spoken line: no dashes, none of the owner's banned words");
            Check(SliceView.SkipWords=="Skip" && SliceView.SkipWidth>=ButtonLook.MinTarget && SliceView.SkipHeight>=ButtonLook.MinTarget && Mathf.Abs(SliceView.SkipX)+SliceView.SkipWidth/2<=180 && SliceView.SkipY+SliceView.SkipHeight/2<=800,"Skip is a 44 px target inside the screen");
            // the orb and its stars: centred in the band above the question, clear of it with or without a cutout's band, eight even places
            var orbSlot=Slots.Find("orb");var starSlot=Slots.Find("orb-star");
            Check(orbSlot!=null && orbSlot.Width==120 && orbSlot.Height==120 && starSlot!=null && starSlot.Width==16 && starSlot.Height==16,"the orb (120 x 120) and the star (16 x 16) are slots");
            Check(SliceView.OrbCentreY(0)-SliceView.OrbReach>=0 && SliceView.OrbCentreY(0)+SliceView.OrbReach<=SliceView.QuestionTop && SliceView.OrbCentreY(24)-SliceView.OrbReach>=24 && SliceView.OrbCentreY(24)+SliceView.OrbReach<=SliceView.QuestionTop,"the orb, its stars and its bob fit the band above the question with no top band and with a 24 px one (centre "+SliceView.OrbCentreY(0)+", reach "+SliceView.OrbReach+")");
            var places=Enumerable.Range(0,SliceFlow.MostOpeningAnswers).Select(SliceView.OrbStarAt).ToList();
            Check(places.All(pl=>Mathf.Abs(pl.magnitude-SliceView.OrbStarRadius)<.01f && pl.magnitude-SliceView.OrbStarSize/2>=SliceView.OrbSize/2-3) && Enumerable.Range(0,places.Count).All(i=>Mathf.Abs(Vector2.Angle(places[i],places[(i+1)%places.Count])-45)<.01f) && places[0].y>0 && Mathf.Abs(places[0].x)<.01f,"eight star places evenly round the orb, the first at the top, clear of its body");
            // Jeffrey, #141 N2: the stars take the places in a spread order, so every count from two up sits balanced round the orb
            var order=SliceView.OrbStarOrder;var lean=new List<string>();
            for(int n=2;n<=order.Length;n++){var c=order.Take(n).Select(SliceView.OrbStarAt).Aggregate(Vector2.zero,(acc,v)=>acc+v)/n;if(c.magnitude>SliceView.OrbStarRadius/3+.01f)lean.Add(n+": "+c.magnitude.ToString("0.0"));}
            Check(order.OrderBy(i=>i).SequenceEqual(Enumerable.Range(0,8)) && order[0]==0 && lean.Count==0,"the stars fill the places in a spread order (top, bottom, right, left, then the diagonals), balanced at every count"+(lean.Count>0?"; leaning: "+string.Join(", ",lean):""));
            // Jeffrey, #141 N7: the comic page's panels clear the gear (its 44 px target at the top right) and Skip at the bottom, with room for a 30 px cutout band
            var pagePanels=shots.Where(x=>x.Page).SelectMany(x=>x.Steps).Where(st=>st.How=="panel").ToList();
            Check(pagePanels.Count==2 && pagePanels.All(st=>st.Top-st.Height/2>=44 && st.Top+st.Height/2+30<=SliceView.SkipY-SliceView.SkipHeight/2),"shot 2's panels sit clear of the gear and of Skip, a cutout's band included");
        }
        static void ValidateBuildF()
        {
            // ---- Build F: the practice fork on the Dial (Sept 15 ruling), the sitting rule (Sept 17), the three-strikes gate, the journal in the inventory ----
            SliceFlow Fresh() { var f = new SliceFlow(() => 1); f.SkipPrologue(); f.Continue(); f.ChooseBirth("skip"); f.PickSkipSun(1); f.PickSkipMoon(3); f.PickRising(4); f.Continue(); f.Continue(); f.EnterWing(); f.EnterDial(); f.RevealKey(); f.Continue(); f.Continue(); f.InsertKey(); f.End(); f.Continue(); return f; } // at the Hub, Stage 2, twelve element items entered
            string DeckKey(SliceFlow f) => string.Join("|", f.Deck.Items.Select(i => i.seat + ":" + i.kind + ":" + i.state + ":" + i.streak + ":" + i.interval + ":" + i.dueDay + ":" + i.entered));
            void AnswerAll(SliceFlow f) { int guard = 0; while (!f.PracticeDone && guard++ < 12) { var t = f.CurrentReview; if (t.Mode == ReviewMode.Tap) f.AnswerTap(Zodiac.Seats[t.seat].Element); else if (t.Mode == ReviewMode.TapModality) f.AnswerModalityTap(Zodiac.ModalityAt(t.seat)); else if (t.Mode == ReviewMode.Glyph) f.AnswerGlyph(t.seat); else f.FinishReview(true, true); } }
            var first = new SliceFlow(() => 1); first.SkipPrologue(); first.Continue(); first.ChooseBirth("skip"); first.PickSkipSun(1); first.PickSkipMoon(3); first.PickRising(4); first.Continue(); first.Continue(); first.EnterWing(); first.EnterDial();
            Check(first.Screen == SliceScreen.Wing && !first.PracticeAvailable && !first.CanEnterPractice && !first.CanOpenJournal, "on the first visit nothing has entered the deck: no fork, no journal");
            var lit = new DialLesson(() => 0); lit.RestoreProgress(1, Enumerable.Repeat(true, 12).ToArray(), Enumerable.Repeat(true, 12).ToArray(), true);
            var midB = new DialLesson(() => 0); midB.RestoreProgress(1, Enumerable.Repeat(true, 12).ToArray(), Enumerable.Repeat(true, 12).ToArray(), true); midB.RestoreGlyphs(1, 3, false);
            var midMod = new DialLesson(() => 0); midMod.RestoreProgress(1, Enumerable.Repeat(true, 12).ToArray(), Enumerable.Repeat(true, 12).ToArray(), true); midMod.RestoreGlyphs(2, 12, true); midMod.RestoreModalities(Enumerable.Range(0, 12).Select(i => i % 3 == 1).ToArray(), new[] { false, true, false }, true);
            var doneMod = new DialLesson(() => 0); doneMod.RestoreProgress(1, Enumerable.Repeat(true, 12).ToArray(), Enumerable.Repeat(true, 12).ToArray(), true); doneMod.RestoreGlyphs(2, 12, true); doneMod.RestoreModalities(Enumerable.Repeat(true, 12).ToArray(), Enumerable.Repeat(true, 3).ToArray(), true);
            Check(!lit.UnitInProgress && midB.UnitInProgress && midMod.UnitInProgress && !doneMod.UnitInProgress, "a unit begun and not complete is in progress after a reload (Part B mid-way, a modality family mid-way); a lit wheel or a finished unit is not, so the fork can show");
            var f = Fresh();
            Check(f.AtHub && f.PracticeAvailable && f.CanOpenJournal && !f.CanEnterPractice && f.Sittings == 0, "at the Hub the journal is in hand; practice waits for the Dial");
            Check(f.EnterWing() && !f.CanEnterPractice && f.EnterDial() && f.CanEnterPractice, "practice is offered on the Dial, not in the room");
            Check(f.EnterPractice() && f.Sittings == 1 && f.Sitting == 1 && f.AtPractice && f.ReviewQueue.Count == 6 && f.Strikes == 0, "entering practice counts one sitting and asks up to six due items");
            Check(f.LeavePractice() && f.Screen == SliceScreen.Wing && f.ReviewQueue.Count == 0 && f.Deck.DueForReview(f.Sitting).Count == 12, "leaving at once keeps every item due at the same sitting");
            Check(f.EnterPractice() && f.Sittings == 2, "re-entering counts another sitting");
            var one = f.CurrentReview; f.FinishReview(true, true); var two = f.CurrentReview; if (two.Mode == ReviewMode.Tap) f.AnswerTap(Zodiac.Seats[two.seat].Element); else f.FinishReview(true, true);
            Check(f.ReviewIndex == 2 && f.LeavePractice() && f.Deck.Item(one.seat, ItemKind.Element).dueDay == 2 + ReviewDeck.Ladder[1] && f.Deck.DueForReview(f.Sitting).Count == 10, "two answered move up the ladder; leaving early leaves the other ten due now");
            foreach (var it in f.Deck.Items) it.dueDay = 99;
            Check(f.CanEnterPractice && !f.EnterPractice() && f.Sittings == 3 && f.Note == SliceFlow.NothingDueLine && f.Screen == SliceScreen.Wing, "with nothing due the entry still counts a sitting and Caspar says so");
            foreach (var it in f.Deck.Items) if (it.Kind == ItemKind.Element) it.dueDay = 0;
            Check(f.EnterPractice() && f.Sittings == 4 && f.Strikes == 0, "fresh strikes on every entry");
            int guard = 0;
            while (!f.Gated && guard++ < 12) { var t = f.CurrentReview; if (t == null) break; if (t.Mode == ReviewMode.Tap) f.AnswerTap(Zodiac.Seats[t.seat].Element == "Fire" ? "Water" : "Fire"); else { f.RecordStrike(); f.FinishReview(false, false); } }
            Check(f.Gated && f.Strikes == SliceFlow.StrikeLimit && f.AtPractice, "the third wrong answer across the practice, on any form, gates the instrument");
            Check(f.CloseInstrument() && f.Screen == SliceScreen.WingRoom && f.Note == "gated" && !f.Gated && f.ReviewQueue.Count == 0 && f.CanOpenJournal, "the gate closes the instrument into the room, journal offered, nobody locked out");
            Check(f.EnterDial() && f.CanEnterPractice && f.EnterPractice() && f.Strikes == 0 && f.Sittings == 5, "re-entry is immediate with three fresh strikes and a new sitting");
            f.LeavePractice(); f.LeaveDial();
            Check(f.JournalLenses.SequenceEqual(new[] { JournalLens.Element }) && !f.CanJournalTable && f.JournalSigns.Count == 12, "before the symbols the journal has the Element tab only, and the twelve signs met");
            f.StartGlyphs(); f.Deck.RecordLesson(3, true, f.Sitting, ItemKind.Glyph);
            string deckBefore = DeckKey(f);
            Check(f.OpenJournal() && f.AtJournal && f.JournalFrom == SliceScreen.WingRoom && f.JournalAt == JournalView.Title && f.JournalLand() && f.JournalToContents() && f.OpenChapter("wheel") && f.JournalAt == JournalView.Wheel && !f.CanJournalPrev && !f.CanJournalNext && !f.CanJournalWheel, "the journal opens from the room (its first-ever open, the title page, then the landing); Contents leads to the Wheel");
            Check(f.SelectSeat(3) && f.OpenSelected() && f.JournalSignSeat == 3 && f.SignKnows(3, ItemKind.Glyph) && f.SeatState(3) >= 2 && f.JournalToWheel() && f.JournalAt == JournalView.Wheel, "Cancer's symbol, answered in the lesson, is on its page and its seat is no longer only met; back to the Wheel");
            Check(DeckKey(f) == deckBefore && f.CloseJournal() && f.Screen == SliceScreen.WingRoom && f.Note == "" && DeckKey(f) == deckBefore, "reading the journal changes no deck field; closing returns to the room and clears the gate note");
            Check(f.LeaveWing() && f.CanOpenJournal && f.OpenJournal() && f.JournalFrom == SliceScreen.Hub && f.CloseJournal() && f.AtHub && f.EnterChamber() && f.CanOpenJournal && f.OpenJournal() && f.CloseJournal() && f.AtChamberRoom, "the journal opens from the Atrium and the Chamber too");
            f.StartGrid(); f.StartOpposites();
            Check(f.JournalLenses.SequenceEqual(new[] { JournalLens.Element, JournalLens.Polarity, JournalLens.Opposites }) && f.CanJournalTable, "the table and the opposites show in the journal as data: the Table view, the Polarity and Opposites tabs (the modalities were skipped here, so no Modality tab)");
            Check(f.LeaveChamber() && f.ApproachDesk() && f.Note == SliceFlow.DeskLine && f.Screen == SliceScreen.Hub, "the desk is dressing: it only speaks");
            var save = UnityEngine.JsonUtility.FromJson<SaveData>(UnityEngine.JsonUtility.ToJson(f.ToSave(new bool[12], new bool[12], true))); var back = new SliceFlow(() => 1);
            Check(save.version == SaveData.Current && save.sittings == 5 && save.reviewsChecked == 5 && back.Restore(save) && back.Sittings == 5, "the save carries the sitting count both ways (version " + save.version + ")");
            var older = new SliceFlow(() => 1);
            Check(older.Restore(new SaveData { atriumStage = 3, sunSign = 1, keyEarned = true, reviewsChecked = 2 }) && older.Sittings == 2, "an older save's completed batches count as sittings");
            // Liveness (folded from the audit's deadlock task 86bc2338q): twelve items answered correctly every time; every item reaches the last interval; no entry ever stalls the clock.
            var live = Fresh(); live.EnterWing(); live.EnterDial(); int stalled = 0;
            for (int entry = 0; entry < 30; entry++) { if (live.EnterPractice()) { AnswerAll(live); live.LeavePractice(); } else stalled++; }
            Check(live.Sittings == 30 && live.Deck.Items.Where(i => i.Kind == ItemKind.Element).All(i => i.interval == ReviewDeck.Ladder.Length - 1) && stalled > 0 && stalled < 30, "liveness: thirty entries, twelve items answered correctly every time; every item reaches the last interval, the clock ticks on every entry, and the empty entries never stall it");
            // Regression: the audit's stall (every item scheduled ahead, nothing due). Under the sitting rule the empty entry still moves the clock and the next entry finds items due.
            var stall = Fresh(); stall.EnterWing(); stall.EnterDial();
            for (int k = 0; k < 2; k++) { stall.EnterPractice(); AnswerAll(stall); stall.LeavePractice(); }
            Check(stall.Sittings == 2 && stall.Deck.DueForReview(stall.Sitting).Count == 0 && !stall.EnterPractice() && stall.Sittings == 3 && stall.Deck.DueForReview(stall.Sitting).Count == 0 && stall.EnterPractice() && stall.Sittings == 4 && stall.ReviewQueue.Count == 6, "the audit's stall: with every item scheduled ahead, the empty entry still moves the clock and the next entry finds the first six due again");
        }
        [MenuItem("Ascendant/Greybox/Run mechanical validation")]
        public static void Run()
        {
            Passed.Clear();
            Check(Zodiac.Seats.Count==12 && Zodiac.Seats.Select(x=>x.Name).Distinct().Count()==12,"12 unique ordered signs");
            Check(Zodiac.Seats[0].Name=="Aries" && Zodiac.Seats[11].Name=="Pisces","Aries home and Pisces boundary");
            foreach(int start in Enumerable.Range(0,12))
            {
                int end=Zodiac.Destination(start);
                Check(Zodiac.Seats[start].Element==Zodiac.Seats[end].Element,"same-element offset from "+start);
                Check(Zodiac.Evaluate(start,4,(start+4)%12),"four forward with zero start and wrap from "+start);
                Check(!Zodiac.Evaluate(start,4,(start+3)%12),"inclusive-counting error rejected from "+start);
            }
            foreach(DialInput method in Enum.GetValues(typeof(DialInput)))
            {
                var dial=new DialModel(()=>5);dial.Begin(10,0);
                dial.Step(5,method);dial.Step(-1,method);dial.Frame();
                Check(dial.Selected==2 && dial.Attempts==0 && dial.Active,"selection and corrected overshoot do not submit: "+method);
                var answer=dial.Commit();Check(answer.correctness && answer.evidence_eligible && answer.input_method==method.ToString(),"equivalent evaluator and independent evidence: "+method);
            }
            var direct=new DialModel(()=>5);direct.Begin(11,0);direct.Select(3,DialInput.DirectSeat);
            Check(direct.Attempts==0 && direct.Selected==3,"direct tap frames without sealing");
            direct.Count();Check(direct.HintLevel==1 && direct.Counting,"Level 1 is neutral counting");
            Check(direct.Commit().evidence_eligible,"Level 1 eligible evidence");
            var errors=new DialModel(()=>5);errors.Begin(0,0);errors.Select(2,DialInput.Keyboard);errors.Commit();
            Check(errors.Selected==2 && errors.HintLevel==1 && errors.Active,"first rejection stays in place with nudge");
            errors.Commit();Check(errors.Selected==2 && errors.HintLevel==2 && !errors.CanInertia,"second rejection stays in place at Level 2 without inertia");
            errors.Count();Check(errors.HintLevel==2,"counter never downgrades Level 2");
            errors.Commit();Check(errors.Selected==2 && errors.HintLevel==3 && !errors.Active && !errors.CanInertia,"third rejection requests worked recovery");
            var silence=new DialModel(()=>0);silence.Begin(7,0);silence.Home();silence.Begin(9,0);
            Check(silence.Events.All(e=>e.event_name=="problem_started"),"automatic start and home emit no detent/frame events");
            silence.ReducedMotion=true;Check(!silence.CanInertia,"reduced motion disables inertia at Level 0");
            var neutral=new DialModel(()=>0);neutral.Begin(0,0);neutral.Count();neutral.Step(6,DialInput.Drag);
            Check(neutral.Events.Where(e=>e.event_name=="dial_rotated").Select(e=>e.movement_count).SequenceEqual(new[]{1,2,3,4,5,6}),"all counting detents use identical event path");
            var happy=Transfer();Check(happy.Lit.Count(v=>v)==4 && !happy.KeyEarned,"guided family plus transfer start, no premature Key");
            Check(happy.Dial.Events.Where(e=>e.event_name=="answer_correct").All(e=>!e.evidence_eligible),"guided rule exposure is never independent evidence");
            Answer(happy);Answer(happy);
            Check(happy.Phase==LessonPhase.Complete && happy.Lit.Count(v=>v)==6 && happy.Kin.Count(v=>v)==6,"two completed families; six lit and six dormant");
            Check(happy.KeyEarned && happy.IndependentEvidence,"Key 1 requires challenge completion and independent evidence");
            Check(happy.Dial.Selected==0,"completion returns home");
            Check(LessonMessageIsLocked(happy),"six-seat completion speaks the amended completion line");
            var assisted=Transfer();Wrong(assisted,2);var assistedAnswer=Answer(assisted);
            Check(!assistedAnswer.evidence_eligible && assisted.Dial.Start!=assistedAnswer.start_seat && assisted.Dial.HintLevel==0,"Level 2 completion queues a different fresh Level 0 problem");
            Answer(assisted);Check(assisted.KeyEarned,"fresh equivalent can earn Key after assisted completion");
            var recovery=Transfer();int firstStart=recovery.Dial.Start;Wrong(recovery,3);recovery.RevealDemonstration();recovery.AfterDemonstration();
            Check(recovery.Dial.Start!=firstStart && recovery.RecoveryEncounters==2 && recovery.Dial.HintLevel==0,"worked recovery resets to a fresh equivalent, encounter two");
            Wrong(recovery,3);recovery.RevealDemonstration();recovery.AfterDemonstration();
            Check(recovery.RecoveryEncounters==3,"third encounter uses final shared recovery slot");
            Wrong(recovery,3);recovery.RevealDemonstration();recovery.AfterDemonstration();
            Check(recovery.Phase==LessonPhase.Paused && !recovery.KeyEarned && !recovery.Dial.Active,"cap pauses without a Key or extra fourth encounter");
            Check(recovery.Dial.Events.All(e=>!e.evidence_eligible),"demonstrations never create evidence");
            happy.BeginOptional();Check(happy.Phase==LessonPhase.Optional && happy.Dial.Start==2,"optional third-family probe offered and accepted");
            Answer(happy);Check(happy.Lit.Count(v=>v)==6 && happy.Phase==LessonPhase.Complete,"optional probe preserves the completed six-seat record");
            Check(happy.Dial.Events.Count(e=>e.event_name=="key1_earned")==1,"optional probe does not award another Key");
            double time=2;var timed=new DialModel(()=>time);timed.Begin(0,0);time=5;timed.Step(4,DialInput.Keyboard);var logged=timed.Commit();
            Check(logged.response_time==3 && logged.attempt_number==1 && logged.start_sign=="Aries" && logged.destination_sign=="Leo" && logged.requested_relationship=="forward_offset_4","answer log carries timing, attempt, relationship and seats");
            var flow=new SliceFlow();Check(flow.Screen==SliceScreen.Prologue && flow.AtPrologue && flow.DisplayName=="Keeper","the opening scene (owner, Oct 8): a new game starts at the prologue, with a default Keeper name");
            ValidatePrologue();
            ValidateLaunch();
            flow.SkipPrologue();Check(flow.Screen==SliceScreen.Identity,"Skip leaves the prologue for the identity screen");
            flow.SetName("  Astra ");Check(flow.DisplayName=="Astra" && flow.Continue() && flow.Screen==SliceScreen.Birth,"name trimmed; identity continues to birth prompt");
            Check(!flow.Continue(),"birth prompt requires a choice");
            flow.ChooseBirth("chart");Check(!flow.CanContinue && !flow.SetBirthDate(1990,13,1) && !flow.SetBirthDate(1899,4,25) && !flow.SetBirthDate(1990,2,30) && flow.SetBirthDate(1990,4,25) && flow.BirthStep=="time" && !flow.CanContinue && !flow.SetBirthTime(24*60) && flow.SetBirthTime(14*60+30) && flow.BirthStep=="place" && !flow.CanContinue && flow.SetBirthPlace(Places.Find("London, Britain (UK)")) && flow.SunSign==1 && flow.MoonSign>=0 && flow.RisingSign>=0 && flow.Note.Contains("Taurus") && flow.Continue() && flow.Screen==SliceScreen.Atrium,"batch 2: the chart path takes the date, the time and the place, works out the sun, moon and rising signs, and unlocks Continue ("+flow.BigThreeLine+")");
            Check(!flow.InsertKey() && flow.Continue() && flow.Screen==SliceScreen.Hub && flow.AtriumStage==1 && !flow.CanEnterChamber && flow.EnterWing() && flow.Screen==SliceScreen.WingRoom && flow.EnterDial() && flow.Screen==SliceScreen.Wing,"Build T: the atrium continues to the Atrium at Stage 1, the Chamber shut; the Zodiac Wing, then the Dial; no key insertion outside the chamber");
            Check(flow.LeaveDial() && flow.Screen==SliceScreen.WingRoom && flow.LeaveWing() && flow.Screen==SliceScreen.Hub && flow.AtriumStage==1 && flow.EnterWing() && flow.EnterDial(),"Build T: in the first lesson the Dial and the Zodiac Wing can be left and entered again, Stage 1 kept");
            Check(!flow.Continue() && flow.RevealKey() && !flow.RevealKey() && !flow.LeaveDial() && flow.Continue() && flow.Screen==SliceScreen.AtriumReturn,"wing needs the key reveal once before continuing; once Key 1 shows, Continue (not the exit) carries the opening on");
            Check(flow.Continue() && flow.Screen==SliceScreen.Chamber && !flow.Continue(),"chamber is the last screen");
            Check(flow.InsertKey() && flow.LocksFilled==1 && !flow.InsertKey() && flow.End() && flow.Ended && !flow.End(),"one key fills one lock of three and ends the prototype once");
            // the birth-time build (owner, Oct 7): the opening has two answers; "Enter what I already know" and the random sun are retired
            // no unknown Big Three (owner, Oct 7, 86bced0tc): I'll skip it asks the sun, the moon and the rising, each from the twelve, with no I'm not sure
            var skip=new SliceFlow(()=>4);skip.SkipPrologue();skip.Continue();skip.ChooseBirth("skip");Check(!skip.CanContinue && skip.BirthStep=="sun-pick" && !skip.PickSkipSun(-1) && !skip.PickSkipSun(12) && skip.PickSkipSun(4) && skip.BirthStep=="moon-pick" && !skip.CanContinue && !skip.PickSkipMoon(-1) && skip.PickSkipMoon(7) && skip.BirthStep=="rising-pick" && !skip.CanContinue && skip.PickRising(0) && skip.CanContinue
                && skip.BigThreeLine=="\u2609 Leo \u00b7 \u263d Scorpio \u00b7 \u2191 Aries" && skip.Note=="Your sun sign is Leo, your moon sign Scorpio, and your rising sign Aries." && skip.Choices.Count==3 && skip.Choices.All(x=>x.how=="picked") && !skip.Facts.Any && skip.LessonSun==4,"Oct 7: I'll skip it asks which sign, moon sign and rising sign the player goes by; each pick shows like any other, flagged picked, and nothing is invented");
            var retired=new SliceFlow();retired.SkipPrologue();retired.Continue();retired.ChooseBirth("known");Check(retired.BirthChoice=="" && retired.BirthStep=="" && !retired.CanContinue && !retired.PickSign(4),"Oct 7: the known path is retired: choosing it opens nothing");retired.ChooseBirth("unknown");Check(retired.BirthChoice=="" && !retired.HasSunSign,"Oct 7: the random sun is retired");
            Check(Zodiac.SunSign(3,21)==0 && Zodiac.SunSign(3,20)==11 && Zodiac.SunSign(1,19)==9 && Zodiac.SunSign(1,20)==10 && Zodiac.SunSign(12,22)==9 && Zodiac.SunSign(12,21)==8 && Zodiac.SunSign(8,23)==5 && Zodiac.SunSign(2,30)==11 && Zodiac.SunSign(0,5)==-1 && Zodiac.SunSign(5,32)==-1,"sun sign date table covers every boundary and rejects bad input");
            var leo=new DialLesson(()=>0);leo.SetSunSign(4);Check(leo.Sun==4 && leo.GuidedFamily==0 && leo.SecondFamily==3 && leo.OptionalFamily==1,"sun sign picks the guided, second, and optional families");
            Check(leo.IntroStep==0 && leo.DialDormant && !leo.IntroAuto && leo.Message=="","wing entrance starts with a dormant Dial and Caspar silent (the owner cut the repeated Wing line, Sept 29)");
            leo.Continue();Check(leo.IntroStep==1 && leo.IntroAuto && leo.DialDormant,"first continue starts the wake beat, no input asked");
            leo.Continue();Check(leo.IntroStep==2 && !leo.IntroAuto && !leo.DialDormant && leo.Message.StartsWith("It responds to you"),"Dial awake, Caspar reacts");
            leo.Continue();leo.Continue();Check(leo.IntroStep==4 && leo.IntroAuto,"simulated hesitation is automatic");
            leo.Continue();Check(leo.Message.StartsWith("You... do not know astrology"),"disbelief follows the hesitation");
            leo.Continue();leo.Continue();Check(leo.IntroStep==DialLesson.IntroTeaching && leo.Lit[4] && leo.Dial.Selected==4 && leo.Message.Contains("Leo is a Fire sign"),"teaching names the player's sun sign and lights it");
            leo.Continue();Check(leo.Phase==LessonPhase.Rule && leo.Message.Contains("The Fire family is Aries, Leo, and Sagittarius"),"rule names the sun sign's family members");
            leo.Continue();Check(leo.Phase==LessonPhase.Guided && leo.Dial.Start==4 && leo.CountBeatPending,"guided problem starts at the sun sign and shows the count once");
            leo.CountBeatShown();Check(!leo.CountBeatPending,"count beat is shown once");
            leo.SetSunSign(2);Check(leo.Sun==4,"sun sign cannot change after the intro begins");
            Answer(leo);Answer(leo);Check(leo.Phase==LessonPhase.Transfer && leo.Message.Contains("Three Fire signs") && leo.Message.Contains("Now Water"),"Fire family done; Caspar hands over Water");
            leo.Continue();Check(leo.Dial.Start==3 && leo.Lit[3],"second family starts at its first sign");
            Answer(leo);Answer(leo);Check(leo.KeyEarned && leo.Lit.Count(v=>v)==6 && LessonMessageIsLocked(leo),"a Leo player also ends at six lit seats with the amended line");
            leo.Dial.Select(leo.Dial.Start,DialInput.DirectSeat);
            var wrongTwice=new DialLesson(()=>0);EnterGuided(wrongTwice);wrongTwice.CountBeatShown();wrongTwice.Seal();Check(!wrongTwice.CountBeatPending,"first wrong seal shows no count");wrongTwice.Seal();Check(wrongTwice.CountBeatPending && wrongTwice.Dial.HintLevel==2,"second wrong seal shows the count once");
            Check(wrongTwice.SeatLabel(wrongTwice.Dial.Selected).Contains(", selected") && wrongTwice.SeatLabel(Zodiac.Wrap(wrongTwice.Dial.Selected+1)).Contains(", not selected"),"screen reader labels say selected");
            // ---- v0.2: the return (Q05) ----
            var deck=new ReviewDeck();deck.IntroduceAll(0);
            Check(deck.Items.Where(i=>i.Kind==ItemKind.Element).All(i=>i.entered && i.State==ItemState.Introduced && i.dueDay==0) && deck.Items.Where(i=>i.Kind==ItemKind.Glyph).All(i=>!i.entered) && deck.Due(0).Count==12,"twelve sign-element items enter the deck as Introduced and are ready at the next check; glyph items wait");
            deck.RecordLesson(5,true,0);Check(deck.Items[5].State==ItemState.Practicing && deck.Items[5].streak==1 && deck.Practicing==1,"a Level 0/1 lesson answer makes the item Practicing and starts its streak");
            deck.RecordLesson(9,false,0);Check(deck.Items[9].State==ItemState.Introduced,"an assisted lesson answer stays Introduced");
            deck.RecordReview(5,true,true,1);Check(deck.Items[5].interval==1 && deck.Items[5].dueDay==4 && deck.Items[5].streak==2,"an eligible review success moves one interval forward: 1 to 3 sittings");
            deck.RecordReview(5,false,false,4);Check(deck.Items[5].interval==0 && deck.Items[5].dueDay==5 && deck.Items[5].streak==1,"a miss steps back one interval and one streak step");
            deck.RecordReview(9,false,false,1);Check(deck.Items[9].interval==0 && deck.Items[9].streak==0 && deck.Items[9].dueDay==2,"a miss at the first interval stays at one sitting with streak floor zero");
            for(int n=0;n<6;n++)deck.RecordReview(2,true,true,n*30);Check(deck.Items[2].interval==4,"the ladder caps at 30 sittings");
            deck.RecordReview(3,true,false,1);Check(deck.Items[3].State==ItemState.Introduced && deck.Items[3].interval==0,"an assisted correct review neither advances nor sets back");
            var loop=new SliceFlow(()=>1);loop.SkipPrologue();loop.Continue();loop.ChooseBirth("skip"); loop.PickSkipSun(1); loop.PickSkipMoon(3); loop.PickRising(4);loop.Continue();loop.Continue();loop.EnterWing();loop.EnterDial();loop.RevealKey();loop.Continue();loop.Continue();loop.InsertKey();loop.End();
            Check(loop.Screen==SliceScreen.Chamber && loop.CanContinue && loop.Continue() && loop.Screen==SliceScreen.Hub && loop.AtriumStage==2 && loop.Deck.Items.Where(i=>i.Kind==ItemKind.Element).All(i=>i.entered),"after the ending, Continue reaches the Hub in Stage 2 and the deck opens");
            Check(loop.Sitting==0 && loop.DueCount==12 && loop.EnterWing() && loop.EnterDial() && loop.CanEnterPractice && loop.EnterPractice() && loop.Sitting==1 && loop.Screen==SliceScreen.Practice && loop.ReviewQueue.Count==ReviewDeck.BatchSize,"no in-game time: the first practice already has items ready; the entry is the first sitting and a batch of six begins");
            Check(loop.ReviewQueue[0].Mode==ReviewMode.Dial && loop.ReviewQueue[1].Mode==ReviewMode.Tap,"review alternates compressed Dial and direct tap");
            loop.FinishReview(true,true);Check(loop.ReviewIndex==1 && loop.Deck.Items[loop.ReviewQueue[0].seat].State==ItemState.Practicing,"a compressed Dial review success advances its item");
            var tapTask=loop.CurrentReview;string wrong=Zodiac.Seats[tapTask.seat].Element=="Fire" ? "Water" : "Fire";
            Check(!loop.AnswerTap(wrong) && !tapTask.done && loop.Note.Contains("try once more"),"a direct-tap miss gives one nudge and a retry");
            Check(loop.AnswerTap(Zodiac.Seats[tapTask.seat].Element) && tapTask.done && tapTask.correct && loop.Deck.Items[tapTask.seat].State==ItemState.Introduced,"a correct tap after a nudge counts as correct but not as eligible evidence");
            var tap3=loop.ReviewQueue[3];loop.FinishReview(false,false);Check(loop.ReviewIndex==3,"a Dial miss moves on");
            loop.AnswerTap(Zodiac.Seats[tap3.seat].Element=="Air" ? "Water" : "Air");loop.AnswerTap(Zodiac.Seats[tap3.seat].Element=="Air" ? "Water" : "Air");
            Check(tap3.done && !tap3.correct && loop.Note.Contains("We will come back"),"two tap misses reveal the element and move on");
            loop.FinishReview(true,true);var last=loop.CurrentReview;loop.AnswerTap(Zodiac.Seats[last.seat].Element);
            Check(loop.PracticeDone && loop.PracticeSummary.EndsWith("of 6 proven at the wheel.") && loop.Sittings==1 && loop.LeavePractice() && loop.Screen==SliceScreen.Wing && loop.LeaveDial() && loop.LeaveWing() && loop.Screen==SliceScreen.Hub,"six items finish the practice with a summary; back to the Dial, the room, the Hub");
            Check(loop.EnterWing() && loop.Screen==SliceScreen.WingRoom && loop.EnterDial() && loop.Screen==SliceScreen.Wing && loop.LeaveDial() && loop.Screen==SliceScreen.WingRoom && loop.LeaveWing() && loop.Screen==SliceScreen.Hub && loop.AtriumStage==2,"the Wing can be entered from the Hub through its room and left back to it");
            loop.MarkWheelComplete();loop.EnterWing();loop.EnterDial();loop.LeaveDial();loop.LeaveWing();Check(loop.AtriumStage==3 && loop.V02Complete,"twelve lit seats and one more return complete v0.2");
            var save=loop.ToSave(new bool[12],new bool[12],true);var resumed=new SliceFlow(()=>1);
            Check(resumed.Restore(save) && resumed.Screen==SliceScreen.Hub && resumed.SunSign==1 && resumed.AtriumStage==3 && resumed.WheelComplete && resumed.Sitting==1 && resumed.Deck.Items[5].State==loop.Deck.Items[5].State && resumed.Sittings==1,"a saved session resumes at the Hub with deck, sittings, and stage");
            Check(!new SliceFlow().Restore(new SaveData{atriumStage=1,sunSign=1}),"a save from before the Hub does not resume");
            var unit=new DialLesson(()=>0);unit.SetSunSign(1);EnterGuided(unit);Answer(unit);Answer(unit);unit.Continue();Answer(unit);Answer(unit);
            Check(unit.KeyEarned && unit.FamiliesComplete==2 && unit.CanContinueUnit && unit.BeginContinuation() && unit.Phase==LessonPhase.Continuation && unit.Dial.Start==2 && unit.Lit[2] && unit.Message.Contains("done this twice"),"Unit 1.1 continues with the third family at Level 0");
            Answer(unit);Answer(unit);Check(unit.FamiliesComplete==3 && unit.Phase==LessonPhase.Continuation && unit.Dial.Start==3 && unit.Message.Contains("last of the four"),"third family done; the fourth begins on its own");
            Answer(unit);Answer(unit);Check(unit.Phase==LessonPhase.AllLit && unit.WheelComplete && unit.Lit.All(v=>v) && unit.Kin.All(v=>v) && !unit.CanContinueUnit,"twelve seats lit, four families, no Key");
            Check(unit.Dial.Events.Count(e=>e.event_name=="key1_earned")==1 && unit.Dial.Events.Any(e=>e.event_name=="wheel_completed"),"the continuation awards no second Key and logs wheel_completed");
            var rv=new DialLesson(()=>0);rv.RestoreProgress(4,Enumerable.Range(0,12).Select(i=>i%4==0).ToArray(),Enumerable.Range(0,12).Select(i=>i%4==0).ToArray(),true);
            Check(rv.Sun==4 && rv.KeyEarned && rv.Phase==LessonPhase.Complete && !rv.DialDormant && rv.FamiliesComplete==1 && rv.CanContinueUnit,"restored progress skips the intro and can continue");
            int rvSeat=-1;bool rvCorrect=false,rvEligible=false;rv.ReviewFinished+=(seat,c,e)=>{rvSeat=seat;rvCorrect=c;rvEligible=e;};
            Check(rv.BeginReview(4) && rv.Phase==LessonPhase.Review && rv.Dial.Start==4 && rv.Dial.Active && rv.Message.StartsWith("Before you turn the wheel"),"a compressed review starts framed on the item's sign");
            rv.Dial.Select(8,DialInput.DirectSeat);rv.Seal();Check(rvSeat==4 && rvCorrect && rvEligible && !rv.Dial.Active,"a correct review Seal reports eligible success and returns home");
            rv.EndReview();Check(rv.Phase==LessonPhase.Complete && rv.BeginReview(0),"review ends back in the prior phase and another can begin");
            rv.Dial.Select(1,DialInput.DirectSeat);rv.Seal();Check(rv.Dial.Active && rv.Message.Contains("try once more"),"first review miss nudges and allows a retry");
            rv.Seal();Check(rvSeat==0 && !rvCorrect && rv.Message.Contains("It is Leo"),"second review miss reveals the answer and moves on");
            // ---- v0.3: glyphs and Key 2 ----
            var glyphFont=Resources.Load<Font>("Fonts/NotoSansSymbols");Check(glyphFont!=null && Enumerable.Range(0,12).All(i=>glyphFont.HasCharacter(Zodiac.Seats[i].Glyph[0])),"the placeholder glyph font carries all twelve zodiac symbols");
            var g=new DialLesson(()=>0);g.SetSunSign(1);EnterGuided(g);Answer(g);Answer(g);g.Continue();Answer(g);Answer(g);g.BeginContinuation();Answer(g);Answer(g);Answer(g);Answer(g);
            Check(g.WheelComplete && g.CanBeginGlyphs && g.BeginGlyphs() && g.Phase==LessonPhase.GlyphNames && g.CurrentGlyph==0 && g.GlyphsShown,"glyph unit begins after the wheel is lit, Part A first");
            Check(Enumerable.Range(0,12).All(seat=>{var o=g.GlyphOptions(seat);return o.Length==4 && o.Distinct().Count()==4 && o.Contains(seat);}),"every glyph offers four distinct names including its own");
            Check(g.AnswerGlyphName(0) && g.GlyphNamed[0] && g.CurrentGlyph==1 && g.Dial.Events.Last(e=>e.event_name=="glyph_named").evidence_eligible,"a correct first tap names the glyph at Level 0");
            Check(!g.AnswerGlyphName(5) && g.GlyphMisses==1 && g.Message.Contains("Not that one") && g.CurrentGlyph==1,"first miss nudges without naming");
            Check(g.AnswerGlyphName(1) && g.CurrentGlyph==2 && g.Dial.Events.Last(e=>e.event_name=="glyph_named").evidence_eligible,"a correct tap after one nudge is Level 1 evidence");
            Check(!g.AnswerGlyphName(6) && !g.AnswerGlyphName(7) && g.GlyphNamed[2] && g.CurrentGlyph==3 && g.Message.Contains("symbol of Gemini") && !g.Dial.Events.Last(e=>e.event_name=="glyph_named").evidence_eligible,"second miss reveals the name, no evidence, and moves on");
            for(int seat=3;seat<12;seat++) g.AnswerGlyphName(seat);
            Check(g.Phase==LessonPhase.GlyphWheel && g.Dial.Active && g.Dial.Start!=0 && Math.Abs(g.Dial.Start-g.Dial.Target)>=2 && g.Dial.Target==0 && g.NamesHidden && g.SeatLabel(5).StartsWith("Symbol") && !g.SeatLabel(5).Contains("Virgo") && g.Dial.Relationship=="seat_of_sign","Part B starts with names hidden, Aries asked first with the wheel away from it, labels that do not leak names, and the seat-of-sign relationship");
            void Place(DialLesson l,int seat){l.Dial.Select(seat,DialInput.DirectSeat);var r=l.Seal();l.AfterCorrect(r);}
            Place(g,0);Check(g.GlyphPlaced[0] && g.GlyphEvidence && g.Dial.Target==1,"sealing on the target places the glyph and counts as evidence");
            g.Dial.Select(5,DialInput.DirectSeat);g.Seal();Check(g.Dial.Active && g.Dial.HintLevel==1 && g.Message.Contains("count forward"),"first wrong Seal in Part B nudges");
            g.Seal();Check(g.NameRevealed[1] && g.Dial.HintLevel==2 && !g.SeatLabel(1).StartsWith("Symbol"),"second wrong Seal reveals the name on its seat and in its label");
            g.Dial.Select(1,DialInput.DirectSeat);var placed=g.Seal();g.AfterCorrect(placed);Check(placed.correctness && !placed.evidence_eligible && g.GlyphPlaced[1] && g.Dial.Target==2,"a Level 2 placement counts for completion, not evidence");
            g.Dial.Select(8,DialInput.DirectSeat);g.Seal();g.Seal();g.Seal();Check(!g.Dial.Active && g.Dial.HintLevel==3,"third wrong Seal in Part B asks for a demonstration");
            g.RevealDemonstration();g.AfterDemonstration();Check(g.GlyphPlaced[2] && g.NameRevealed[2] && g.Dial.Target==3 && g.Dial.Active,"the demonstration places the glyph without evidence and moves to the next mark");
            for(int seat=3;seat<12;seat++) Place(g,seat);
            Check(g.Key2Earned && g.Phase==LessonPhase.Key2 && g.Keys==2 && g.GlyphsShown && !g.NamesHidden && g.Dial.Events.Count(e=>e.event_name=="key2_earned")==1,"twelve placed with at least one Level 0/1 answer earns Key 2 once");
            var noEvidence=new DialLesson(()=>0);noEvidence.SetSunSign(1);EnterGuided(noEvidence);Answer(noEvidence);Answer(noEvidence);noEvidence.Continue();Answer(noEvidence);Answer(noEvidence);noEvidence.BeginContinuation();Answer(noEvidence);Answer(noEvidence);Answer(noEvidence);Answer(noEvidence);noEvidence.BeginGlyphs();
            for(int seat=0;seat<12;seat++) noEvidence.AnswerGlyphName(seat);
            for(int seat=0;seat<12;seat++){noEvidence.Dial.Select(Zodiac.Wrap(seat+1),DialInput.DirectSeat);noEvidence.Seal();noEvidence.Seal();Place(noEvidence,seat);}
            Check(!noEvidence.Key2Earned && noEvidence.Phase==LessonPhase.Paused && noEvidence.Keys==1,"twelve assisted placements pause cleanly without Key 2");
            var gd=new ReviewDeck();gd.IntroduceAll(0);gd.IntroduceAll(0,ItemKind.Glyph);Check(gd.Items.Length==60 && gd.Due(1).Count==24 && gd.Item(3,ItemKind.Glyph).Kind==ItemKind.Glyph && gd.Item(3,ItemKind.Modality).Kind==ItemKind.Modality && !gd.Item(3,ItemKind.Modality).entered && !gd.Item(3,ItemKind.Grid).entered,"the deck holds twelve element, glyph, modality, and grid items; the modality and grid items wait");
            gd.RecordLesson(4,true,0,ItemKind.Glyph);Check(gd.Item(4,ItemKind.Glyph).State==ItemState.Practicing && gd.Item(4,ItemKind.Element).State==ItemState.Introduced,"glyph evidence advances only the glyph item");
            var gflow=new SliceFlow(()=>1);gflow.SkipPrologue();gflow.Continue();gflow.ChooseBirth("skip"); gflow.PickSkipSun(1); gflow.PickSkipMoon(3); gflow.PickRising(4);gflow.Continue();gflow.Continue();gflow.EnterWing();gflow.EnterDial();gflow.RevealKey();gflow.Continue();gflow.Continue();gflow.InsertKey();gflow.End();gflow.Continue();
            gflow.MarkWheelComplete();gflow.EnterWing();gflow.LeaveWing();gflow.StartGlyphs();Check(gflow.GlyphsStarted && gflow.Deck.Due(gflow.Sitting).Count(i=>i.Kind==ItemKind.Glyph)==12,"starting the glyph unit introduces twelve glyph items");
            gflow.MarkKey2();gflow.EnterWing();gflow.LeaveWing();Check(gflow.Keys==2 && gflow.AtriumStage==3 && gflow.KeysInHand==1,"Key 2 earned: in hand, the Atrium waits for it to be spent (Build D)");
            Check(gflow.EnterChamber() && gflow.SpendKey() && gflow.LocksFilled==2 && gflow.LeaveChamber() && gflow.AtriumStage==4 && gflow.V03Complete,"spent in the Chamber, the return completes v0.3 at Stage 4");
            Check(gflow.EnterWing() && gflow.EnterDial() && gflow.EnterPractice() && gflow.ReviewQueue.Count==6 && gflow.ReviewQueue.All(t=>t.Mode!=ReviewMode.Glyph),"element items are due before glyph items in the practice order");
            var gsave=gflow.ToSave(new bool[12],new bool[12],true);var gres=new SliceFlow(()=>1);Check(gres.Restore(gsave) && gres.Keys==2 && gres.GlyphStage==2 && gres.V03Complete && gres.Deck.Item(0,ItemKind.Glyph).entered,"a save carries Keys, glyph stage, and glyph items");
            var greview=new SliceFlow(()=>1);greview.SkipPrologue();greview.Continue();greview.ChooseBirth("skip"); greview.PickSkipSun(1); greview.PickSkipMoon(3); greview.PickRising(4);greview.Continue();greview.Continue();greview.EnterWing();greview.EnterDial();greview.RevealKey();greview.Continue();greview.Continue();greview.InsertKey();greview.End();greview.Continue();
            foreach(var it in greview.Deck.Items) if(it.Kind==ItemKind.Element){it.dueDay=99;} greview.StartGlyphs();
            Check(greview.EnterWing() && greview.EnterDial() && greview.EnterPractice() && greview.ReviewQueue.All(t=>t.Mode==ReviewMode.Glyph),"glyph items practice in the glyph form");
            var gt=greview.CurrentReview;var gopts=greview.GlyphReviewOptions(gt.seat);int wrongSeat=gopts.First(o=>o!=gt.seat);
            Check(!greview.AnswerGlyph(wrongSeat) && greview.Note.Contains("try once more") && greview.AnswerGlyph(gt.seat) && gt.done && gt.correct && greview.Note.Contains("symbol of"),"a glyph review nudges once then accepts the name");
            var grl=new DialLesson(()=>0);grl.RestoreProgress(1,Enumerable.Repeat(true,12).ToArray(),Enumerable.Repeat(true,12).ToArray(),true);grl.RestoreGlyphs(2,12,true);Check(grl.Key2Earned && grl.Keys==2 && grl.Phase==LessonPhase.Key2 && !grl.CanBeginGlyphs,"restored Key 2 does not reopen the glyph unit");
            var grl2=new DialLesson(()=>0);grl2.RestoreProgress(1,Enumerable.Repeat(true,12).ToArray(),Enumerable.Repeat(true,12).ToArray(),true);grl2.RestoreGlyphs(0,5,false);Check(grl2.CanBeginGlyphs && grl2.BeginGlyphs() && grl2.CurrentGlyph==5,"restored Part A progress resumes at the next mark");
            // ---- v0.4: tap-to-move (Q06 phase 2 lock) ----
            Check(Rooms.Visible(Room.Atrium).Count(p=>p.Walkable)==4 && Rooms.Visible(Room.Atrium).Count(p=>!p.Walkable)==1 && Rooms.Visible(Room.Wing).Count()==4 && Rooms.Visible(Room.Wing).All(p=>p.Walkable) && Rooms.Find(Room.Wing,"grid").X==-62 && Rooms.Find(Room.Wing,"grid").Label=="the table","the Atrium offers the Wing doorway, the Chamber doorway, the desk, and Caspar plus one sealed door; the Wing offers the doorway back, the table, the Dial, and the shelf");
            var walker=new Walker();var walkLog=new List<string>();walker.Logged+=walkLog.Add;
            walker.Enter(Room.Atrium,"entry");Check(walker.Room==Room.Atrium && walker.At=="entry" && !walker.Walking && walkLog.Last()=="room_entered:atrium","entering a room places the marker at a point of interest");
            Check(!walker.GoTo("sealed-left") && !walker.GoTo("dial") && !walker.Walking,"sealed doors and points in other rooms are not walkable");
            Check(walker.GoTo("desk") && walker.Walking && walker.Facing==-1 && walker.TargetX==-125 && walkLog.Last()=="walk_started:desk","walking to the desk starts a leg facing left");
            float legSeconds=walker.WalkSeconds("desk");int ticks=0;while(!walker.Tick(.05f))ticks++;
            Check(Math.Abs(legSeconds-165f/Walker.NormalSpeed)<1e-3 && ticks==(int)Math.Ceiling(legSeconds/.05f)-1 && walker.X==-125 && walker.At=="desk" && !walker.Walking && walkLog.Last()=="walk_arrived:desk","a straight-line walk at the fixed speed arrives after distance over speed");
            Check(walker.GoTo("desk") && walker.Tick(.05f) && walker.At=="desk","walking to where the marker already stands arrives on the first tick");
            Check(walker.GoTo("caspar") && walker.Facing==1 && walker.Bob==0 && walker.Tick(.05f)==false && walker.Bob>0,"the walk bob rises only while walking");
            Check(walker.Jump() && walker.X==76 && walker.At=="caspar" && !walker.Walking,"reduced motion jumps to the point of interest");
            walker.CycleSpeed();Check(walker.SpeedName=="fast" && walker.Speed==Walker.FastSpeed,"the test speed toggle cycles normal to fast");walker.CycleSpeed();Check(walker.SpeedName=="slow","then slow");walker.CycleSpeed();Check(walker.SpeedName=="normal","then normal again");
            var wflow=new SliceFlow(()=>4);var wlog=new List<string>();wflow.Logged+=wlog.Add;
            wflow.SkipPrologue(); wflow.Continue();wflow.ChooseBirth("skip"); wflow.PickSkipSun(1); wflow.PickSkipMoon(3); wflow.PickRising(4);wflow.Continue();wflow.Continue();wflow.EnterWing();wflow.EnterDial();wflow.RevealKey();wflow.Continue();wflow.Continue();wflow.InsertKey();wflow.End();wflow.Continue();
            Check(wflow.Screen==SliceScreen.Hub && wflow.Walk.Room==Room.Atrium && wflow.Walk.At=="entry","the Chamber ending leads to the Atrium with the marker where you came in");
            Check(wflow.TouchSealedDoor() && wflow.Note.StartsWith("Sealed") && wlog.Last()=="sealed_door_touched","a sealed door only says it is sealed");
            Check(wflow.ApproachCaspar() && wflow.Note.Contains("Caspar") && wlog.Last()=="caspar_approached","approaching Caspar logs the approach");
            Check(wflow.EnterWing() && wflow.Screen==SliceScreen.WingRoom && wflow.Walk.Room==Room.Wing && wflow.Walk.At=="atrium-door" && wflow.Note=="","the Wing doorway leads into the Wing room at its doorway");
            Check(!wflow.LeaveDial() && wflow.EnterDial() && wflow.Screen==SliceScreen.Wing && wflow.LeaveDial() && wflow.Screen==SliceScreen.WingRoom && wflow.Walk.At=="atrium-door","the Dial opens from the room and closes back to it");
            // v0.3 revision: the book of symbols on the shelf
            Check(!wflow.CanOpenBook && !wflow.EnterBook() && wflow.TouchDarkShelf() && wflow.Note=="shelf-dark" && wflow.Screen==SliceScreen.WingRoom,"before the wheel is lit the shelf is dark and the book does not open");
            wflow.MarkWheelComplete();Check(wflow.CanOpenBook && !wflow.TouchDarkShelf() && wflow.EnterBook() && wflow.Screen==SliceScreen.Book && wflow.Note=="" && !wflow.EnterDial() && wflow.LeaveBook() && wflow.Screen==SliceScreen.WingRoom,"with the wheel lit the book opens from the room and closes back to it; the Dial does not open from inside the book");
            var shelfLesson=new DialLesson(()=>0);shelfLesson.RestoreProgress(1,Enumerable.Repeat(true,12).ToArray(),new bool[12],true);
            Check(shelfLesson.CanBeginGlyphs && !shelfLesson.AllNamed,"a lit wheel with no symbols named is ready for the book, not the wheel");
            shelfLesson.BeginGlyphs();for(int i=0;i<12;i++)shelfLesson.AnswerGlyphName(i);
            Check(shelfLesson.AllNamed && shelfLesson.Phase==LessonPhase.GlyphWheel,"naming all twelve in the book hands the unit to the wheel");
            Check(wflow.LeaveWing() && wflow.Screen==SliceScreen.Hub && wflow.Walk.Room==Room.Atrium && wflow.Walk.At=="wing-door","leaving the Wing room places the marker at the Atrium's Wing doorway");
            Check(wflow.ApproachDesk() && wflow.Note==SliceFlow.DeskLine && wflow.EnterWing() && wflow.EnterDial() && wflow.EnterPractice() && wflow.Screen==SliceScreen.Practice,"the desk only speaks (Build F); practice opens on the Dial");
            while(!wflow.PracticeDone){var t=wflow.CurrentReview;if(t.Mode==ReviewMode.Tap)wflow.AnswerTap(Zodiac.Seats[t.seat].Element);else if(t.Mode==ReviewMode.Glyph)wflow.AnswerGlyph(t.seat);else wflow.FinishReview(true,true);}
            Check(wflow.LeavePractice() && wflow.Screen==SliceScreen.Wing && wflow.LeaveDial() && wflow.LeaveWing() && wflow.Walk.At=="wing-door","leaving practice returns to the Dial; the room and the Atrium follow");
            var wsave=wflow.ToSave(new bool[12],new bool[12],true);var wback=new SliceFlow(()=>4);Check(wback.Restore(wsave) && wback.Walk.Room==Room.Atrium && wback.Walk.At=="entry","a resumed session starts at the Atrium entry");
            // ---- v0.3 revision, build 2: Ask Caspar ----
            var ask=Transfer();Check(!ask.CanAsk,"Ask Caspar is not offered before a first miss");
            Wrong(ask,1);Check(ask.CanAsk && ask.Dial.HintLevel==1,"after a first miss the player may ask Caspar");
            Check(ask.AskCaspar() && ask.Dial.HintLevel==2 && ask.Dial.Asked && ask.Message.Contains("shares the same element after") && ask.CountBeatPending && !ask.CanAsk && ask.Dial.Events.Last().event_name=="hint_asked","asking gives the Level 2 rule reminder once, with the count, and is not a miss");
            var askedAnswer=Answer(ask);Check(askedAnswer.correctness && !askedAnswer.evidence_eligible && askedAnswer.hint_level==2,"a correct answer after asking is assisted: no evidence");
            var ask3=Transfer();Wrong(ask3,1);ask3.AskCaspar();Wrong(ask3,1);Check(ask3.Dial.HintLevel==3 && !ask3.Dial.Active && ask3.Message.StartsWith("Watch me do one"),"a miss after the asked reminder goes to the worked example, not a second reminder");
            var guided=new DialLesson(()=>0);EnterGuided(guided);guided.Dial.Select(guided.Dial.Start,DialInput.DirectSeat);guided.Seal();Check(!guided.CanAsk,"no Ask Caspar in the guided problem, which already carries the rule");
            var askGlyph=new DialLesson(()=>0);askGlyph.RestoreProgress(1,Enumerable.Repeat(true,12).ToArray(),new bool[12],true);askGlyph.BeginGlyphs();for(int i=0;i<12;i++)askGlyph.AnswerGlyphName(i);
            askGlyph.Dial.Select(5,DialInput.DirectSeat);askGlyph.Seal();Check(askGlyph.CanAsk && askGlyph.AskCaspar() && askGlyph.NameRevealed[0] && askGlyph.Dial.HintLevel==2,"in the symbols, asking reveals the name on its seat");
            askGlyph.Dial.Select(0,DialInput.DirectSeat);var askedPlace=askGlyph.Seal();Check(askedPlace.correctness && !askedPlace.evidence_eligible,"a placement after asking earns no evidence toward Key 2");
            // ---- v0.3 revision, build 3: the difficulty ramp ----
            Check(Enumerable.Range(0,12).All(seat=>{var o=DialLesson.OptionsFor(seat,true,0);return o.Length==4 && o.Distinct().Count()==4 && o.Contains(seat);}),"hard options are four distinct names that include the answer for every seat");
            Check(Enumerable.Range(0,12).All(seat=>DialLesson.OptionsFor(seat,true,0).Contains(Zodiac.Wrap(seat+4)) && DialLesson.OptionsFor(seat,true,0).Contains(Zodiac.Wrap(seat+8))),"hard options include both same-element signs");
            var ramp=new DialLesson(()=>0);ramp.RestoreProgress(1,Enumerable.Repeat(true,12).ToArray(),new bool[12],true);ramp.RestoreGlyphs(2,12,true);
            Check(!ramp.Hard && ramp.CanPractice && ramp.BeginPractice() && ramp.Practice && ramp.Phase==LessonPhase.GlyphNames && ramp.SeatAt(0)==0 && ramp.Dial.Events.Last().event_name=="symbol_practice_started","after Key 2 the book replays, in order the first time");
            for(int i=0;i<12;i++)ramp.AnswerGlyphName(ramp.CurrentGlyph);Check(ramp.Phase==LessonPhase.GlyphWheel && ramp.Dial.Target==0,"the replayed Part A hands to the wheel");
            ramp.Dial.Select(3,DialInput.DirectSeat);ramp.Seal();Check(ramp.Message.Contains("Look at the symbol before you"),"the first replay keeps the Aries anchor");
            ramp.Dial.Select(0,DialInput.DirectSeat);var p0=ramp.Seal();ramp.AfterCorrect(p0);
            for(int i=1;i<12;i++){ramp.Dial.Select(ramp.Dial.Target,DialInput.DirectSeat);var pe=ramp.Seal();ramp.AfterCorrect(pe);}
            Check(!ramp.Practice && ramp.CleanRuns==1 && ramp.Hard && ramp.Phase==LessonPhase.Key2 && ramp.Dial.Events.Count(e=>e.event_name=="key2_earned")==0 && ramp.Dial.Events.Count(e=>e.event_name=="symbol_practice_clean")==1,"a clean replay counts once, hardens the next, and never re-earns Key 2");
            Check(ramp.BeginPractice() && ramp.Hard && ramp.SeatAt(0)!=0 && Enumerable.Range(0,12).Select(i=>ramp.SeatAt(i)).Distinct().Count()==12 && ramp.GlyphOptions(ramp.CurrentGlyph).Contains(ramp.CurrentGlyph),"the hard replay shuffles all twelve and still offers the answer");
            for(int i=0;i<12;i++)ramp.AnswerGlyphName(ramp.CurrentGlyph);ramp.Dial.Select(Zodiac.Wrap(ramp.Dial.Target+2),DialInput.DirectSeat);ramp.Seal();
            Check(ramp.Phase==LessonPhase.GlyphWheel && !ramp.Message.Contains("Aries") && ramp.Message.Contains("shape"),"the hard replay drops the Aries anchor from the first hint");
            var rampFlow=new SliceFlow(()=>1);Check(rampFlow.GlyphReviewOptions(5).Contains(Zodiac.Wrap(5+3)),"review names are the usual set before a clean run");rampFlow.RecordCleanRun();
            Check(rampFlow.CleanRuns==1 && rampFlow.GlyphReviewOptions(5).Contains(Zodiac.Wrap(5+4)) && rampFlow.GlyphReviewOptions(5).Contains(5),"after a clean run the review names harden and still include the answer");
            var rampSave=rampFlow.ToSave(new bool[12],new bool[12],true);var rampBack=new SliceFlow(()=>1);rampSave.atriumStage=4;rampSave.sunSign=1;Check(rampBack.Restore(rampSave) && rampBack.CleanRuns==1,"the clean-run count survives a save");
            // ---- Sept 14 hotfix: the deck must survive the JSON save; Part B never starts on or beside the answer ----
            var jflow=new SliceFlow(()=>1);jflow.SkipPrologue();jflow.Continue();jflow.ChooseBirth("skip"); jflow.PickSkipSun(1); jflow.PickSkipMoon(3); jflow.PickRising(4);jflow.Continue();jflow.Continue();jflow.EnterWing();jflow.EnterDial();jflow.RevealKey();jflow.Continue();jflow.Continue();jflow.InsertKey();jflow.End();jflow.Continue();
            jflow.RecordLessonAnswer(5,true);var jsave=UnityEngine.JsonUtility.FromJson<SaveData>(UnityEngine.JsonUtility.ToJson(jflow.ToSave(new bool[12],new bool[12],true)));var jback=new SliceFlow(()=>1);
            Check(jsave.deck!=null && jsave.deck.Length==60 && jback.Restore(jsave) && jback.DueCount==jflow.DueCount && jback.Deck.Practicing==1,"the review deck survives the JSON save and reload with its due count and practicing items");
            var starts=new DialLesson(()=>0);starts.RestoreProgress(1,Enumerable.Repeat(true,12).ToArray(),new bool[12],true);starts.BeginGlyphs();for(int i=0;i<12;i++)starts.AnswerGlyphName(starts.CurrentGlyph);
            bool startsOk=true;for(int i=0;i<12;i++){int d=Math.Abs(starts.Dial.Start-starts.Dial.Target);if(d<2 || d>10)startsOk=false;starts.Dial.Select(starts.Dial.Target,DialInput.DirectSeat);var se=starts.Seal();starts.AfterCorrect(se);}
            Check(startsOk,"every Part B problem starts the wheel at least two seats from the answer");
            // ---- Build A: the modalities on the Dial ----
            Check(Zodiac.ModalityAt(0)=="Cardinal" && Zodiac.ModalityAt(1)=="Fixed" && Zodiac.ModalityAt(2)=="Mutable" && Zodiac.ModalityAt(3)=="Cardinal" && Zodiac.Seats[7].Modality=="Fixed","every third sign shares a modality: Aries cardinal, Taurus fixed, Gemini mutable, Cancer cardinal");
            var m3=new DialModel(()=>0);m3.Begin(1,0,-1,3);Check(m3.Forward==3 && m3.Relationship=="forward_offset_3","a problem can ask for three seats forward");
            m3.Select(5,DialInput.DirectSeat);Check(!m3.Commit().correctness,"four forward is wrong on a three-step problem");m3.Begin(1,0,-1,3);m3.Select(4,DialInput.DirectSeat);Check(m3.Commit().correctness,"three forward is right");
            var mod=new DialLesson(()=>0);mod.RestoreProgress(1,Enumerable.Repeat(true,12).ToArray(),new bool[12],true);mod.RestoreGlyphs(2,12,true);
            Check(!mod.ModalitiesComplete && mod.CanBeginModalities && mod.BeginModalities() && mod.Phase==LessonPhase.ModalityGuided && mod.Dial.Forward==3 && mod.Dial.Start==1 && mod.Dial.HintLevel==2 && mod.LitMod[1] && mod.CountBeatPending && mod.Message.Contains("second pattern") && mod.Message.Contains("Taurus, is Fixed"),"after Key 2 the modality unit opens guided at the sun sign with the three-count and names the family");
            for(int i=0;i<3;i++){mod.Dial.Select(Zodiac.Destination(mod.Dial.Start,3),DialInput.DirectSeat);var r=mod.Seal();mod.AfterCorrect(r);}
            Check(mod.KinMod[1] && mod.Phase==LessonPhase.ModalityOwn && mod.Dial.HintLevel==0 && mod.Dial.Forward==3 && mod.Message.Contains("mutable"),"the guided family completes and the next kind starts on the player's own");
            mod.Dial.Select(mod.Dial.Start,DialInput.DirectSeat);mod.Seal();Check(mod.CanAsk && mod.AskCaspar() && mod.Message.Contains("same modality") && mod.Message.Contains("one, two, three."),"Ask Caspar in the modality unit gives the three-step rule");
            for(int i=0;i<8 && !mod.ModalitiesComplete;i++){if(!mod.Dial.Active)break;mod.Dial.Select(Zodiac.Destination(mod.Dial.Start,3),DialInput.DirectSeat);var r=mod.Seal();mod.AfterCorrect(r);}
            Check(mod.ModalitiesComplete && mod.Phase==LessonPhase.ModalityComplete && mod.KinMod.All(v=>v) && !mod.Key2Earned==false && mod.Dial.Events.Count(e=>e.event_name=="modalities_completed")==1 && mod.Dial.Events.Count(e=>e.event_name.StartsWith("key"))==0,"three families of four complete the unit with no new Key");
            Check(mod.SeatLabel(4).Contains("fixed"),"lit modality seats say their kind to the screen reader");
            var mflow=new SliceFlow(()=>1);mflow.SkipPrologue();mflow.Continue();mflow.ChooseBirth("skip"); mflow.PickSkipSun(1); mflow.PickSkipMoon(3); mflow.PickRising(4);mflow.Continue();mflow.Continue();mflow.EnterWing();mflow.EnterDial();mflow.RevealKey();mflow.Continue();mflow.Continue();mflow.InsertKey();mflow.End();mflow.Continue();
            mflow.StartModalities();Check(mflow.ModalitiesStarted && mflow.Deck.Items.Count(i=>i.Kind==ItemKind.Modality && i.entered)==12 && mflow.DueCount>=12,"starting the unit introduces twelve modality items, ready at the next check");
            foreach(var it in mflow.Deck.Items) if(it.Kind!=ItemKind.Modality) it.dueDay=99;
            Check(mflow.EnterWing() && mflow.EnterDial() && mflow.EnterPractice() && mflow.ReviewQueue.All(t=>t.Mode==ReviewMode.DialModality||t.Mode==ReviewMode.TapModality),"modality items practice as a three-step Dial item or a which-kind tap");
            var mt=mflow.ReviewQueue.First(t=>t.Mode==ReviewMode.TapModality);while(mflow.CurrentReview!=mt)mflow.FinishReview(true,true);
            Check(!mflow.AnswerModalityTap(Zodiac.ModalityAt(mt.seat)=="Fixed" ? "Cardinal" : "Fixed") && mflow.AnswerModalityTap(Zodiac.ModalityAt(mt.seat)) && mt.done && mt.correct && mflow.Note.Contains(Zodiac.ModalityAt(mt.seat).ToLowerInvariant()),"a which-kind tap nudges once then accepts the kind");
            var msave=UnityEngine.JsonUtility.FromJson<SaveData>(UnityEngine.JsonUtility.ToJson(mflow.ToSave(new bool[12],new bool[12],true,mod.LitMod,mod.KinMod)));var mback=new SliceFlow(()=>1);var mlesson=new DialLesson(()=>0);
            Check(mback.Restore(msave) && mback.ModalitiesStarted && msave.litMod.All(v=>v) && msave.kinMod.All(v=>v),"the save carries the modality unit");mlesson.RestoreModalities(msave.litMod,msave.kinMod,msave.modalitiesStarted);Check(mlesson.ModalitiesComplete && mlesson.Phase==LessonPhase.ModalityComplete,"and the lesson restores it complete");
            var ab=new DialLesson(()=>0);ab.RestoreProgress(1,Enumerable.Repeat(true,12).ToArray(),new bool[12],true);ab.RestoreGlyphs(2,12,true);ab.BeginPractice();ab.AnswerGlyphName(ab.CurrentGlyph);ab.AbandonPractice();
            Check(!ab.Practice && ab.Phase==LessonPhase.Key2 && ab.AllNamed && ab.CanPractice && ab.CanBeginModalities,"closing the book mid-practice abandons the pass and leaves Key 2 and the next unit reachable");
            // ---- Build B: the table (4 × 3 grid) and Key 3 ----
            Check(Enumerable.Range(0,12).Select(GridModel.CellOf).Distinct().Count()==12 && Enumerable.Range(0,12).All(c=>GridModel.CellOf(GridModel.SeatOf(c))==c) && GridModel.CellOf(4)==1 && GridModel.SeatOf(11)==11 && GridModel.RowName(GridModel.RowOf(9))=="Earth" && GridModel.ColumnName(GridModel.ColumnOf(9))=="Cardinal","each element-modality cell holds exactly one sign: Leo in Fire fixed, Pisces in Water mutable, Capricorn in Earth cardinal");
            var grid=new GridModel(()=>0);var glog=new List<DialEvent>();grid.Logged+=glog.Add;
            Check(!grid.Active && !grid.Pick(0) && grid.Begin() && grid.Active && grid.Started && grid.Message==GridModel.IntroLine && glog.Last().event_name=="grid_unit_started","the table opens on the intro line and logs the unit start");
            Check(!grid.CanSeal && !grid.Choose(1) && grid.Pick(4) && grid.Sign==4 && !grid.CanSeal && grid.Choose(1) && grid.CanSeal && grid.Readout.Contains("Placing Leo at Fire, fixed"),"a sign in hand, then a cell: the readout names both and nothing has been sealed");
            Check(grid.Pick(0) && grid.Sign==0 && grid.Cell==-1 && grid.Pick(4) && grid.Sign==4,"before a miss the player may change the sign in hand; the cell clears");
            grid.Choose(GridModel.CellOf(4));var leoSeat=grid.Seal();
            Check(leoSeat!=null && leoSeat.correctness && leoSeat.evidence_eligible && grid.Placed[4] && grid.Evidence && grid.Sign==-1 && grid.Message.StartsWith("Yes, Leo belongs to Fire, fixed.") && grid.Message.Contains("on your own") && glog.Last().event_name=="grid_placed" && glog.Last().requested_relationship=="cell_of_sign" && glog.Last().destination_sign=="Leo" && glog.Last().evidence_eligible,"a right cell at Level 0 seats the sign with evidence and logs the placement");
            Check(!grid.CanPick(4) && !grid.CanChoose(GridModel.CellOf(4)),"a seated sign and its cell are out of play");
            grid.Pick(1);grid.Choose(GridModel.CellOf(8));var miss=grid.Seal(); // Taurus, Earth fixed, to Fire mutable: the row is wrong, so the nudge names the element
            Check(!miss.correctness && grid.HintLevel==1 && grid.Attempts==1 && grid.Rejected==GridModel.CellOf(8) && grid.Cell==-1 && grid.Message=="Not that square, acolyte. Taurus is an Earth sign. Find the Earth row." && grid.Locked && !grid.CanPick(2) && grid.CanPick(1) && grid.CanAsk && grid.CellLabel(GridModel.CellOf(8)).EndsWith("not that one"),"a first miss nudges with the element, marks the cell, locks the sign in hand, and offers Ask Caspar");
            grid.Choose(GridModel.CellOf(9));grid.Seal(); // Earth cardinal: row right, column wrong
            Check(grid.HintLevel==2 && grid.Message.StartsWith("Taurus is Earth and it is fixed, acolyte.") && !grid.CanAsk && !grid.Demonstrating && grid.Active,"a second miss states the rule: element and kind");
            grid.Choose(GridModel.CellOf(1));var taurusSeat=grid.Seal();
            Check(taurusSeat.correctness && !taurusSeat.evidence_eligible && grid.Placed[1] && grid.Assisted[1] && grid.Message.Contains("together"),"a Level 2 seating counts for the table, not as evidence");
            grid.Pick(2);grid.Choose(GridModel.CellOf(6));grid.Seal(); // Gemini, Air mutable, to Air cardinal: the column is wrong, so the nudge names the kind
            Check(grid.HintLevel==1 && grid.Message=="Not that square, acolyte. Gemini is mutable. Find the mutable column.","a wrong column nudges with the kind");
            Check(grid.Ask() && grid.Asked && grid.HintLevel==2 && grid.Message.StartsWith("Gemini is Air and it is mutable, acolyte.") && !grid.CanAsk && glog.Last().event_name=="hint_asked","asking Caspar gives the rule once and is not a miss");
            grid.Choose(GridModel.CellOf(2));var askedSeat=grid.Seal();
            Check(askedSeat.correctness && !askedSeat.evidence_eligible && grid.Placed[2],"a seating after asking earns no evidence");
            grid.Pick(3);grid.Choose(GridModel.CellOf(7));grid.Seal();grid.Ask();grid.Choose(GridModel.CellOf(11));grid.Seal();
            Check(grid.HintLevel==3 && grid.Demonstrating && !grid.Active && !grid.CanSeal && grid.DemonstrationCell==GridModel.CellOf(3) && grid.Message.StartsWith("Watch me place it, acolyte."),"a miss after the asked rule asks Caspar to seat it");
            grid.AfterDemonstration();
            Check(!grid.Demonstrating && grid.Placed[3] && grid.Assisted[3] && grid.AssistedThisSitting==1 && grid.Active && grid.Message.Contains("Cancer is placed") && glog.Count(e=>e.event_name=="grid_placed")==4 && glog.Where(e=>e.event_name=="grid_placed").Count(e=>e.evidence_eligible)==1,"the demonstration seats the sign without evidence and the table stays open");
            void Demo(GridModel g,int seat){g.Pick(seat);for(int k=0;k<3;k++){g.Choose(GridModel.CellOf(Zodiac.Wrap(seat+1)));g.Seal();}g.AfterDemonstration();}
            Demo(grid,5);Check(grid.AssistedThisSitting==2 && grid.Placed[5] && grid.Active,"three wrong cells reach the demonstration");
            Demo(grid,6);Check(grid.AssistedThisSitting==3 && grid.Phase==GridPhase.Paused && !grid.Active && grid.Message==GridModel.PausedLine && glog.Last().event_name=="grid_paused","the third demonstration in one sitting pauses the table (the cap)");
            Check(grid.Begin() && grid.Active && grid.AssistedThisSitting==0 && grid.PlacedCount==6 && grid.Message.Contains("6 of twelve placed") && glog.Last().event_name=="grid_resumed","coming back resumes the table with its seated signs and a fresh cap");
            foreach(int seat in new[]{0,7,8,9,10}){grid.Pick(seat);grid.Choose(GridModel.CellOf(seat));grid.Seal();}
            Check(grid.PlacedCount==11 && grid.Pick(11) && Enumerable.Range(0,12).Count(c=>grid.CanChoose(c))==1 && grid.HintLevel==0,"the last sign has one empty cell left, so it seats at Level 0 by elimination"); // owner question: Key 3 then follows twelve seated in every case
            grid.Choose(GridModel.CellOf(11));grid.Seal();
            Check(grid.Complete && grid.Key3Earned && grid.Phase==GridPhase.Complete && grid.Message==GridModel.Key3Line && glog.Count(e=>e.event_name=="key3_earned")==1 && !grid.Active && grid.TileLabel(11).EndsWith("placed") && grid.CellLabel(GridModel.CellOf(11))=="Water, mutable: Pisces","twelve seated with at least one Level 0/1 seating earns Key 3 once");
            Check(grid.Begin() && grid.Message==GridModel.FullLine && !grid.Active && glog.Count(e=>e.event_name=="key3_earned")==1,"a full table only shows itself; Key 3 is never re-earned");
            var fullBack=new GridModel(()=>0);fullBack.Restore(grid.Placed,true,true,true);Check(fullBack.Key3Earned && fullBack.Complete && fullBack.Phase==GridPhase.Complete && fullBack.Message==GridModel.FullLine,"a restored full table with Key 3 shows itself full");
            var half=new GridModel(()=>0);half.Restore(Enumerable.Range(0,12).Select(i=>i<5).ToArray(),true,true,false);
            Check(half.PlacedCount==5 && half.Evidence && half.Started && !half.Active && half.Begin() && half.Active && half.CanPick(5) && !half.CanPick(4) && half.Message.Contains("5 of twelve"),"a restored half table resumes with its seated signs");
            var tflow=new SliceFlow(()=>1);tflow.SkipPrologue();tflow.Continue();tflow.ChooseBirth("skip"); tflow.PickSkipSun(1); tflow.PickSkipMoon(3); tflow.PickRising(4);tflow.Continue();tflow.Continue();tflow.EnterWing();tflow.EnterDial();tflow.RevealKey();tflow.Continue();tflow.Continue();tflow.InsertKey();tflow.End();tflow.Continue();
            tflow.MarkWheelComplete();tflow.MarkKey2();
            Check(tflow.EnterWing() && !tflow.CanOpenGrid && !tflow.EnterGrid() && tflow.TouchDarkGrid() && tflow.Note=="grid-dark" && tflow.Screen==SliceScreen.WingRoom,"before the modality unit is complete the table is dark: a note, no screen");
            Check(tflow.EnterDial() && tflow.Note=="" && tflow.LeaveDial(),"entering the Dial clears the dark-object note");
            tflow.MarkModalitiesComplete();Check(tflow.ModalitiesComplete && tflow.CanOpenGrid && !tflow.TouchDarkGrid() && tflow.EnterGrid() && tflow.Screen==SliceScreen.Grid && !tflow.EnterDial() && tflow.LeaveGrid() && tflow.Screen==SliceScreen.WingRoom,"with the modality unit complete the table opens from the room and closes back to it");
            tflow.StartGrid();Check(tflow.GridStarted && tflow.Deck.Items.Count(i=>i.Kind==ItemKind.Grid && i.entered)==12 && tflow.Deck.Due(tflow.Sitting).Count(i=>i.Kind==ItemKind.Grid)==12 && tflow.Deck.DueForReview(tflow.Sitting).All(i=>i.Kind!=ItemKind.Grid) && !ReviewDeck.Reviewable(ItemKind.Grid),"starting the table introduces twelve sign → cell items as data: scheduled, never in a review batch");
            tflow.RecordGridAnswer(4,true);tflow.RecordGridAnswer(1,false);Check(tflow.Deck.Item(4,ItemKind.Grid).State==ItemState.Practicing && tflow.Deck.Item(1,ItemKind.Grid).State==ItemState.Introduced && tflow.Deck.Item(4,ItemKind.Element).State==ItemState.Introduced,"grid evidence advances only the grid item");
            Check(tflow.LeaveWing() && tflow.AtriumStage==3 && tflow.KeysInHand==1,"two Keys, one in hand: the Atrium stays at Stage 3 until it is spent");
            foreach(var it in tflow.Deck.Items) if(it.Kind!=ItemKind.Grid) it.dueDay=99;
            Check(tflow.DueCount==0 && tflow.EnterWing() && tflow.EnterDial() && tflow.CanEnterPractice && !tflow.EnterPractice() && tflow.Sittings==1 && tflow.Note==SliceFlow.NothingDueLine && tflow.LeaveDial() && tflow.LeaveWing(),"with only grid items ready, practice has nothing to ask, says so, and still counts the sitting");
            tflow.MarkKey3();Check(tflow.Keys==3 && tflow.EnterChamber() && tflow.SpendKey() && tflow.SpendKey() && !tflow.SpendKey() && tflow.BooksOpen==1 && tflow.LeaveChamber() && tflow.AtriumStage==5,"Keys 2 and 3 spent fill Book 1 and take the Atrium to Stage 5");
            var tsave=UnityEngine.JsonUtility.FromJson<SaveData>(UnityEngine.JsonUtility.ToJson(tflow.ToSave(new bool[12],new bool[12],true,Enumerable.Repeat(true,12).ToArray(),Enumerable.Repeat(true,3).ToArray(),grid.Placed,true)));var tback=new SliceFlow(()=>1);
            Check(tsave.deck.Length==60 && tsave.gridPlaced.All(v=>v) && tsave.gridEvidence && tsave.gridStarted && tback.Restore(tsave) && tback.Keys==3 && tback.AtriumStage==5 && tback.ModalitiesComplete && tback.GridStarted && tback.Deck.Item(4,ItemKind.Grid).State==ItemState.Practicing && tback.DueCount==0 && tback.EnterWing() && tback.CanOpenGrid,"the save carries the table, Key 3, Stage 5, and the grid items as data");
            // ---- Build C: polarity, the six opposite pairs, the builder, and Key 4 ----
            Check(Zodiac.Polarities.Length==2 && Zodiac.PolarityAt(0)=="Yang" && Zodiac.PolarityAt(1)=="Yin" && Enumerable.Range(0,12).All(i=>Zodiac.PolarityAt(i)==(Zodiac.Seats[i].Element=="Fire"||Zodiac.Seats[i].Element=="Air" ? "Yang" : "Yin")),"fire and air are Yang, earth and water Yin (owner, Sept 17): the polarity follows the element");
            Check(Enumerable.Range(0,12).All(i=>Zodiac.Opposite(Zodiac.Opposite(i))==i && Zodiac.ModalityAt(Zodiac.Opposite(i))==Zodiac.ModalityAt(i) && Zodiac.PolarityAt(Zodiac.Opposite(i))==Zodiac.PolarityAt(i) && Zodiac.Seats[Zodiac.Opposite(i)].Element!=Zodiac.Seats[i].Element) && Zodiac.Opposite(0)==6 && Zodiac.PairOf(9)==3 && Zodiac.PairOf(3)==3 && Zodiac.OppositePairs==6,"opposites sit six seats on, share kind and side, and differ in element; six pairs named by their lower seat");
            var m6=new DialModel(()=>0);m6.Begin(1,0,-1,6);m6.Select(4,DialInput.DirectSeat);Check(!m6.Commit().correctness,"three forward is wrong on an opposite problem");m6.Begin(1,0,-1,6);m6.Select(7,DialInput.DirectSeat);Check(m6.Commit().correctness && m6.Relationship=="forward_offset_6","six forward is right");
            bool[] all12=Enumerable.Repeat(true,12).ToArray(),all3=Enumerable.Repeat(true,3).ToArray();
            DialLesson AfterKey3(int sun){var l=new DialLesson(()=>0);l.RestoreProgress(sun,all12,all12,true);l.RestoreGlyphs(2,12,true);l.RestoreModalities(all12,all3,true);return l;}
            var op=AfterKey3(1);
            Check(!op.CanBeginOpposites && !op.BeginOpposites(),"without Key 3 the last pattern waits");
            op.SetKey3(true);Check(op.CanBeginOpposites && op.BeginOpposites() && op.Phase==LessonPhase.Polarity && !op.PolarityShown && op.Message==DialLesson.PolarityLine0 && op.Dial.Events.Last().event_name=="opposites_unit_started" && !op.IsProblem,"with Key 3 the unit opens on the polarity beat");
            op.Continue();Check(op.PolarityShown && op.Phase==LessonPhase.Polarity && op.Message.Contains("day and night") && op.Message.Contains("masculine") && op.SeatLabel(0).Contains(", Yang") && op.SeatLabel(1).Contains(", Yin"),"the second line names the older terms and the seats say their side");
            op.Continue();Check(op.Phase==LessonPhase.OppositeGuided && op.Dial.Forward==6 && op.Dial.Start==1 && op.Dial.HintLevel==2 && op.CountBeatPending && op.Message.Contains("six signs forward") && op.IsProblem && !op.CanAsk,"the first pair is guided from the sun sign with the six-count");
            op.CountBeatShown();op.Dial.Select(7,DialInput.DirectSeat);var oppFirst=op.Seal();
            Check(oppFirst.correctness && !oppFirst.evidence_eligible && op.CorrectLine(oppFirst).Contains("sits across from Taurus") && op.CorrectLine(oppFirst).Contains("Both are fixed, both are Yin") && op.CorrectLine(oppFirst).Contains("Earth against Water"),"a correct opposite names what the pair shares and what differs");
            op.AfterCorrect(oppFirst);Check(op.OppKnown[1] && op.PairsKnown==1 && op.Phase==LessonPhase.OppositeOwn && op.Dial.Start==2 && op.Dial.HintLevel==0 && op.Dial.Forward==6 && op.Message.Contains("across the wheel from Gemini"),"the guided pair is known; the next pair starts on the player's own");
            op.Dial.Select(3,DialInput.DirectSeat);op.Seal();Check(op.Dial.HintLevel==1 && op.CanAsk && op.Dial.Active,"a first miss nudges and offers Ask Caspar");
            Check(op.AskCaspar() && op.Message.Contains("six signs forward from Gemini") && op.Message.Contains("five, six") && op.CountBeatPending,"Ask Caspar gives the six-seat rule with the count");
            op.CountBeatShown();op.Dial.Select(8,DialInput.DirectSeat);var askedOpp=op.Seal();op.AfterCorrect(askedOpp);Check(askedOpp.correctness && !askedOpp.evidence_eligible && op.PairsKnown==2 && op.Dial.Start==3,"a pair found after asking counts, not as evidence");
            op.Dial.Select(3,DialInput.DirectSeat);op.Seal();op.Seal();op.Seal();Check(!op.Dial.Active && op.Dial.HintLevel==3 && op.Message.StartsWith("Watch me find it") && op.Message.Contains("the next pair"),"a third miss asks for the worked example");
            op.RevealDemonstration();op.AfterDemonstration();Check(op.PairsKnown==3 && op.OppKnown[3] && op.Phase==LessonPhase.OppositeOwn && op.Dial.Start==4 && op.Dial.Active,"the worked example marks the pair without evidence and moves on");
            void Demo6(DialLesson l){for(int k=0;k<3;k++){l.Dial.Select(l.Dial.Start,DialInput.DirectSeat);l.Seal();}l.RevealDemonstration();l.AfterDemonstration();}
            Demo6(op);Demo6(op);Check(op.Phase==LessonPhase.OppositePaused && op.PairsKnown==5 && op.Message==DialLesson.OppositesPausedLine && !op.Dial.Active && op.Dial.Events.Last().event_name=="opposites_paused","three worked examples in one sitting pause the last pattern");
            Check(op.BeginOpposites() && op.Phase==LessonPhase.OppositeOwn && op.Dial.Start==6 && op.Dial.Events.Last().event_name=="opposites_resumed","coming back resumes at the missing pair with a fresh cap");
            op.Dial.Select(0,DialInput.DirectSeat);var lastPair=op.Seal();op.AfterCorrect(lastPair);
            Check(lastPair.correctness && lastPair.evidence_eligible && op.OppositesComplete && op.Phase==LessonPhase.OppositesComplete && op.Message==DialLesson.OppositesDoneLine && op.Dial.Events.Count(e=>e.event_name=="opposites_completed")==1 && !op.Dial.Active,"six pairs complete the last pattern");
            op.Continue();Check(op.Phase==LessonPhase.BuilderName && op.BuilderStep==1 && op.BuilderTarget==1 && op.Built==0 && op.Message.Contains("Earth and fixed") && op.BuilderOptions.Length==4 && op.BuilderOptions.Distinct().Count()==4 && op.BuilderOptions.Contains(1) && op.BuilderOptions.Contains(5) && op.BuilderOptions.Contains(9) && op.BuilderOptions.Contains(4) && op.InBuilder && !op.IsProblem,"Continue opens the builder: element and kind given, four names with the two same-element signs and a same-kind one");
            Check(op.Speaker==DialLesson.DialSpeaker && op.Message.StartsWith("Build me a sign from its parts"),"Build V: the Dial asks for the sign built from its parts, not Caspar");
            Check(!op.AnswerBuilderName(5) && op.Message.Contains("Virgo is mutable") && op.Message.Contains("looking for the Earth sign that is fixed") && op.Phase==LessonPhase.BuilderName,"a wrong name of the right element is nudged with its kind");
            Check(op.Speaker==DialLesson.CasparSpeaker,"Build V: Caspar answers a wrong name; the Dial only poses the problem");
            Check(op.AnswerBuilderName(1) && op.Phase==LessonPhase.BuilderOpposite && op.BuilderStep==2 && op.Dial.Active && op.Dial.Start==1 && op.Dial.Forward==6 && op.Message.StartsWith("Yes, Taurus: Earth and fixed.") && op.Dial.Events.Last(e=>e.event_name=="builder_named").evidence_eligible,"the right name after one nudge hands to the wheel");
            op.Dial.Select(7,DialInput.DirectSeat);var built1=op.Seal();op.AfterCorrect(built1);
            Check(built1.evidence_eligible && op.Phase==LessonPhase.BuilderShare && op.BuilderStep==3 && op.Message.Contains("Taurus and Scorpio") && !op.Dial.Active && !op.IsProblem,"the opposite found, the share step asks what the two have in common");
            Check(!op.AnswerBuilderShare(2) && op.Message.Contains("Not the element") && op.Message.Contains("Taurus is Earth and Scorpio is Water") && op.BuilderStep==3,"tapping the element is nudged once");
            Check(op.AnswerBuilderShare(0) && op.Shared[0] && op.BuilderStep==3 && op.Message.Contains("What else"),"the kind is shared; one more to find");
            Check(op.AnswerBuilderShare(1) && op.Shared[1] && op.BuilderStep==0 && op.Built==1 && op.BuilderEvidence && op.Message.Contains("Same modality, same polarity") && op.Dial.Events.Last(e=>e.event_name=="builder_sign_built").evidence_eligible,"the side too: the first sign is built unassisted (one nudge per step stays Level 1)");
            op.NextBuilderSign();Check(op.Phase==LessonPhase.BuilderName && op.Built==1 && op.BuilderTarget==6 && op.Message.Contains("Air and cardinal") && op.BuilderOptions.Contains(6),"the second sign: Libra, from Air and cardinal");
            Check(!op.AnswerBuilderName(2) && !op.AnswerBuilderName(10) && op.Phase==LessonPhase.BuilderOpposite && op.Message.StartsWith("It is Libra") && !op.Dial.Events.Last(e=>e.event_name=="builder_named").evidence_eligible,"two wrong names reveal the sign and hand to the wheel, assisted");
            op.Dial.Select(1,DialInput.DirectSeat);op.Seal();op.Seal();op.Seal();Check(!op.Dial.Active && op.Message.Contains("what the two signs share"),"a third wrong turn in the builder asks for the worked example");
            op.RevealDemonstration();op.AfterDemonstration();Check(op.Phase==LessonPhase.BuilderShare && op.Message.Contains("Libra and Aries"),"the worked example on the wheel still reaches the share step");
            Check(!op.AnswerBuilderShare(2) && !op.AnswerBuilderShare(2) && op.Shared[0] && op.Shared[1] && op.Built==2 && op.BuilderStep==0 && op.Message.StartsWith("Opposites share the modality and the polarity") && !op.Dial.Events.Last(e=>e.event_name=="builder_sign_built").evidence_eligible,"two wrong taps reveal the rule; the second sign counts as assisted");
            op.NextBuilderSign();Check(op.BuilderTarget==8 && op.Message.Contains("Fire and mutable") && op.AnswerBuilderName(8) && op.Phase==LessonPhase.BuilderOpposite,"the third sign: Sagittarius, from Fire and mutable");
            op.Dial.Select(2,DialInput.DirectSeat);var built3=op.Seal();op.AfterCorrect(built3);Check(op.AnswerBuilderShare(1) && op.AnswerBuilderShare(0) && op.Built==3 && op.BuilderStep==0,"three signs built");
            op.NextBuilderSign();Check(op.Key4Earned && op.Phase==LessonPhase.Key4 && op.Message==DialLesson.Key4Line && op.Dial.Events.Count(e=>e.event_name=="key4_earned")==1 && !op.CanBeginOpposites && !op.BeginOpposites(),"three built with one unassisted earns Key 4 once; the unit closes");
            var nk=AfterKey3(4);nk.SetKey3(true);nk.RestoreOpposites(true,Enumerable.Repeat(true,6).ToArray(),true,0,false,false);
            Check(nk.BeginOpposites() && nk.Phase==LessonPhase.BuilderName && nk.BuilderTarget==4 && nk.PolarityShown,"a restored unit with six pairs known opens straight on the builder (a Leo sun builds Leo first)");
            for(int k=0;k<3;k++){int t=nk.BuilderTarget;var wrongNames=nk.BuilderOptions.Where(o=>o!=t).ToArray();nk.AnswerBuilderName(wrongNames[0]);nk.AnswerBuilderName(wrongNames[1]);nk.Dial.Select(Zodiac.Opposite(t),DialInput.DirectSeat);var r=nk.Seal();nk.AfterCorrect(r);nk.AnswerBuilderShare(0);nk.AnswerBuilderShare(1);nk.NextBuilderSign();}
            Check(nk.Built==3 && !nk.Key4Earned && nk.Phase==LessonPhase.BuilderPaused && nk.Message==DialLesson.BuilderNoEvidenceLine && nk.Dial.Events.Count(e=>e.event_name=="key4_earned")==0,"three signs built with every name revealed earn no Key");
            Check(nk.BeginOpposites() && nk.Built==0 && nk.Phase==LessonPhase.BuilderName && nk.Dial.Events.Any(e=>e.event_name=="builder_cleared"),"the next visit clears the builder for another try");
            var halfPairs=AfterKey3(1);halfPairs.SetKey3(true);halfPairs.RestoreOpposites(true,new[]{true,true,false,false,false,false},true,0,false,false);
            Check(halfPairs.BeginOpposites() && halfPairs.Phase==LessonPhase.OppositeOwn && halfPairs.Dial.Start==2 && halfPairs.PairsKnown==2 && halfPairs.PolarityShown && halfPairs.SeatLabel(4).Contains(", Yang"),"a restored half-known pattern resumes at the next pair with the sides shown");
            var mid=AfterKey3(1);mid.SetKey3(true);mid.RestoreOpposites(true,Enumerable.Repeat(true,6).ToArray(),true,2,true,false);
            Check(mid.BeginOpposites() && mid.Phase==LessonPhase.BuilderName && mid.Built==2 && mid.BuilderTarget==8,"a restored builder resumes at the third sign");
            var cflow=new SliceFlow(()=>1);cflow.SkipPrologue();cflow.Continue();cflow.ChooseBirth("skip"); cflow.PickSkipSun(1); cflow.PickSkipMoon(3); cflow.PickRising(4);cflow.Continue();cflow.Continue();cflow.EnterWing();cflow.EnterDial();cflow.RevealKey();cflow.Continue();cflow.Continue();cflow.InsertKey();cflow.End();cflow.Continue();
            cflow.MarkWheelComplete();cflow.MarkKey2();cflow.MarkModalitiesComplete();cflow.MarkKey3();
            cflow.StartOpposites();Check(cflow.OppositesStarted && cflow.Deck.Items.Count(i=>i.Kind==ItemKind.Opposite && i.entered)==6 && cflow.Deck.Items.Count(i=>i.Kind==ItemKind.Opposite)==12 && cflow.Deck.DueForReview(cflow.Sitting).All(i=>i.Kind!=ItemKind.Opposite) && !ReviewDeck.Reviewable(ItemKind.Opposite),"starting the last pattern introduces six opposite-pair items as data, never in a batch");
            cflow.RecordOppositeAnswer(7,true);Check(cflow.Deck.Item(1,ItemKind.Opposite).State==ItemState.Practicing && !cflow.Deck.Item(7,ItemKind.Opposite).entered,"a pair answer from either seat advances the pair's one item");
            cflow.EnterWing();Check(cflow.LeaveWing() && cflow.AtriumStage==3 && cflow.KeysInHand==2,"three Keys, two in hand: the Atrium waits");cflow.MarkKey4();Check(cflow.EnterChamber() && cflow.SpendKey() && cflow.SpendKey() && cflow.SpendKey() && cflow.WingWhole && cflow.LeaveChamber() && cflow.AtriumStage==6 && cflow.Keys==4,"three Keys spent: Book 1 open, Book 2 begun, the Wing whole, Stage 6");
            var csave=UnityEngine.JsonUtility.FromJson<SaveData>(UnityEngine.JsonUtility.ToJson(cflow.ToSave(new bool[12],new bool[12],true,all12,all3,all12,true,true,new[]{true,true,true,false,false,false},2,true)));var cback=new SliceFlow(()=>1);
            Check(csave.deck.Length==60 && csave.polarityShown && csave.oppKnown.Count(v=>v)==3 && csave.built==2 && csave.builderEvidence && csave.oppositesStarted && cback.Restore(csave) && cback.Keys==4 && cback.AtriumStage==6 && cback.OppositesStarted && cback.Deck.Item(1,ItemKind.Opposite).State==ItemState.Practicing,"the save carries the last pattern, the builder's progress, Key 4, and Stage 6");
            var clesson=AfterKey3(1);clesson.SetKey3(true);clesson.RestoreOpposites(csave.polarityShown,csave.oppKnown,csave.oppositesStarted,csave.built,csave.builderEvidence,csave.keys>=4);
            Check(clesson.Key4Earned && clesson.Phase==LessonPhase.Key4 && !clesson.CanBeginOpposites && clesson.PolarityShown && clesson.PairsKnown==3,"and the lesson restores it with Key 4 held");
            // ---- Build D: the finished loop (Keys spent, the Books, the Atrium by Keys spent, the Wing whole) ----
            Check(Rooms.Visible(Room.Chamber).Count()==2 && Rooms.Visible(Room.Chamber).All(p=>p.Walkable) && Rooms.Find(Room.Chamber,"books").X==0 && Rooms.Find(Room.Chamber,"atrium-door").X==-140 && Rooms.Find(Room.Atrium,"chamber-door").Walkable && Rooms.Find(Room.Atrium,"sealed-right")==null,"the Chamber is a room with the doorway back and the Books; the Atrium's right door is its doorway");
            var dflow=new SliceFlow(()=>1);var dlog=new List<string>();dflow.Logged+=dlog.Add;dflow.SkipPrologue();dflow.Continue();dflow.ChooseBirth("skip"); dflow.PickSkipSun(1); dflow.PickSkipMoon(3); dflow.PickRising(4);dflow.Continue();dflow.Continue();dflow.EnterWing();dflow.EnterDial();dflow.RevealKey();dflow.Continue();dflow.Continue();dflow.InsertKey();dflow.End();dflow.Continue();
            Check(dflow.LocksFilled==1 && dflow.KeysSpent==1 && dflow.KeysInHand==0 && dflow.BooksOpen==0 && dflow.CanEnterChamber && !dflow.CanSpend,"after the opening one lock is filled and no Key is in hand");
            Check(dflow.EnterChamber() && dflow.AtChamberRoom && dflow.Walk.Room==Room.Chamber && dflow.Walk.At=="atrium-door" && !dflow.SpendKey() && dflow.LocksFilled==1,"the Chamber opens as a room; nothing accepts a Key the player does not have");
            Check(dflow.LeaveChamber() && dflow.Screen==SliceScreen.Hub && dflow.AtriumStage==2 && dflow.Walk.At=="chamber-door","leaving the Chamber places the marker at its doorway; no stage without a Key spent");
            dflow.MarkWheelComplete();dflow.MarkKey2();dflow.EnterWing();dflow.LeaveWing();Check(dflow.AtriumStage==3 && dflow.KeysInHand==1 && !dflow.CanSpend,"Key 2 earned: in hand, the Atrium at Stage 3 until it is spent");
            Check(dflow.EnterChamber() && dflow.CanSpend && dflow.SpendKey() && dflow.LocksFilled==2 && dflow.BooksOpen==0 && dflow.KeysInHand==0 && !dflow.SpendKey() && dlog.Count(e=>e=="key_spent")==1,"Key 2 fills the second lock; the Book stays shut; no second spend without a Key");
            Check(dflow.LeaveChamber() && dflow.AtriumStage==4 && dlog.Last(e=>e.StartsWith("atrium_stage"))=="atrium_stage_4","the return after spending takes the Atrium to Stage 4");
            dflow.MarkKey3();Check(dflow.EnterChamber() && dflow.SpendKey() && dflow.LocksFilled==3 && dflow.BooksOpen==1 && !dflow.WingWhole && dlog.Contains("book_opened:1"),"Key 3 fills the third lock and Book 1 opens");
            Check(dflow.LeaveChamber() && dflow.AtriumStage==5,"Stage 5 on the return");
            dflow.MarkKey4();dflow.EnterChamber();Check(dflow.SpendKey() && dflow.LocksFilled==4 && dflow.BooksOpen==1 && dflow.WingWhole && dlog.Contains("wing_whole") && !dflow.CanSpend && dlog.Count(e=>e.StartsWith("book_opened"))==1,"Key 4 fills Book 2's first lock: the Wing is whole, no second Book open");
            Check(dflow.LeaveChamber() && dflow.AtriumStage==6,"Stage 6 on the return");
            var dsave=UnityEngine.JsonUtility.FromJson<SaveData>(UnityEngine.JsonUtility.ToJson(dflow.ToSave(new bool[12],new bool[12],true)));var dback=new SliceFlow(()=>1);
            Check(dsave.locksFilled==4 && dback.Restore(dsave) && dback.LocksFilled==4 && dback.KeysSpent==4 && dback.KeysInHand==0 && dback.BooksOpen==1 && dback.WingWhole && dback.AtriumStage==6 && dback.CanEnterChamber,"the save carries the Keys spent and the Books");
            var oldSave=new SaveData{atriumStage=6,sunSign=1,keyEarned=true,keys=4};var oldBack=new SliceFlow(()=>1);Check(oldBack.Restore(oldSave) && oldBack.LocksFilled==1 && oldBack.KeysInHand==3 && oldBack.AtriumStage==6,"a save from before Build D keeps its stage and holds its Keys in hand");
            // Platform fit, Part 1 (86bcbn6mf): on Android a built-in-font line that spills its box shrinks up to two points; a line set to overflow,
            // another font, or a line already best-fitted is left alone; off Android nothing changes. Exercised here on a scratch hierarchy.
            { var builtin=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");var root=new GameObject("Fit test");UnityEngine.UI.Text Make(string n,Font f){var g=new GameObject(n,typeof(RectTransform));g.transform.SetParent(root.transform,false);var x=g.AddComponent<UnityEngine.UI.Text>();x.font=f;x.fontSize=12;return x;}
              var plain=Make("plain",builtin);var over=Make("over",builtin);over.horizontalOverflow=HorizontalWrapMode.Overflow;var other=Make("other",Font.CreateDynamicFontFromOSFont("Arial",12));
              int offAndroid=SafeArea.FitAndroidText(root.transform,builtin);int fitted=SafeArea.FitAndroidText(root.transform,builtin,true);
              Check(offAndroid==0 && fitted==1 && plain.resizeTextForBestFit && plain.resizeTextMaxSize==12 && plain.resizeTextMinSize==10 && !over.resizeTextForBestFit && !other.resizeTextForBestFit,"Platform fit, Part 1: on Android a built-in-font line that spills its box shrinks up to two points (12 to 10); overflowing lines and other fonts are untouched; off Android nothing changes");
              UnityEngine.Object.DestroyImmediate(root); }
            // Platform fit, Part 2 (86bcbn6mf): a full-screen layer grows to the master's 600 x 920. A master file (1200 x 1840) draws whole across it;
            // today's 720 x 1600 painting keeps its column as it was and continues outward in a 3 x 3 grid; a band on the column's top grows only up and sideways.
            { var colGo=new GameObject("Bleed test",typeof(RectTransform));var col=(RectTransform)colGo.transform;col.sizeDelta=new Vector2(360,800);
              (Vector2 min,Vector2 max,int verts) Mesh(int texW,int texH,float top,float h){var go=new GameObject("Layer",typeof(RectTransform));var rt=(RectTransform)go.transform;rt.SetParent(col,false);rt.anchorMin=rt.anchorMax=new Vector2(.5f,1);rt.pivot=new Vector2(.5f,.5f);rt.sizeDelta=new Vector2(360,h);rt.anchoredPosition=new Vector2(0,-(top+h/2));
                var img=go.AddComponent<UnityEngine.UI.Image>();img.sprite=Sprite.Create(new Texture2D(texW,texH),new UnityEngine.Rect(0,0,texW,texH),new Vector2(.5f,.5f));var bleed=Bleed.Add(img,col);
                var vh=new UnityEngine.UI.VertexHelper();var r=rt.rect;vh.AddVert(new Vector3(r.xMin,r.yMin),Color.white,new Vector2(0,0));vh.AddVert(new Vector3(r.xMax,r.yMin),Color.white,new Vector2(1,0));vh.AddVert(new Vector3(r.xMax,r.yMax),Color.white,new Vector2(1,1));vh.AddVert(new Vector3(r.xMin,r.yMax),Color.white,new Vector2(0,1));vh.AddTriangle(0,1,2);vh.AddTriangle(2,3,0);
                bleed.ModifyMesh(vh);var v=new List<UIVertex>();vh.GetUIVertexStream(v);Vector2 mn=Vector2.positiveInfinity,mx=Vector2.negativeInfinity;foreach(var x in v){mn=Vector2.Min(mn,x.position);mx=Vector2.Max(mx,x.position);}int n=vh.currentVertCount;UnityEngine.Object.DestroyImmediate(go);return (mn,mx,n);}
              var master=Mesh(1200,1840,0,800);var column=Mesh(720,1600,0,800);var band=Mesh(1,64,0,140);UnityEngine.Object.DestroyImmediate(colGo);
              Check(Bleed.IsMaster(Sprite.Create(new Texture2D(1200,1840),new UnityEngine.Rect(0,0,1200,1840),Vector2.zero)) && !Bleed.IsMaster(Sprite.Create(new Texture2D(720,1600),new UnityEngine.Rect(0,0,720,1600),Vector2.zero))
                && master.verts==4 && master.min==new Vector2(-300,-460) && master.max==new Vector2(300,460) && column.verts==36 && column.min==new Vector2(-300,-460) && column.max==new Vector2(300,460)
                && band.min==new Vector2(-300,-70) && band.max==new Vector2(300,130),"Platform fit, Part 2: a full-screen layer reaches the master's 600 x 920: a 1200 x 1840 file draws whole; a 720 x 1600 painting continues outward in a 3 x 3 grid; a band on the column's top grows up and sideways only"); }
            // Batch 2 (owner, Oct 2: "Bleed masters approved"; 86bcbn6mf): the three rooms and their light and grime layers are 1200 x 1840 masters, drawn whole.
            Check(new[] { "atrium", "atrium-light", "atrium-grime", "wing", "wing-light", "wing-grime", "chamber", "chamber-light", "chamber-grime" }.Select(n => Resources.Load<Sprite>("Art/" + n)).All(a => a != null && Bleed.IsMaster(a) && (int)a.rect.width == 1200 && (int)a.rect.height == 1840),
                "Batch 2: the approved bleed masters are in: the Grand Atrium, the Zodiac Wing and the Crystal Book Chamber, and their light and grime layers, each a 1200 x 1840 master");
            // Batch 2 (owner, Oct 2: the masters approved; "Make the Dial room's worn and bright masters from the colour tables"): the Dial's final art. The
            // room is a master in all three looks (its glow is a radial fill over the column, so it keeps the column's file), the bright ring is in, and the Wing's small Dial has its bright pair.
            Check(new[] { "dial-room", "dial-room-worn", "dial-room-bright" }.Select(n => Resources.Load<Sprite>("Art/" + n)).All(a => a != null && Bleed.IsMaster(a) && (int)a.rect.width == 1200 && (int)a.rect.height == 1840)
                && new[] { "dial-room-light", "dial-room-light-worn", "dial-room-light-bright" }.Select(n => Resources.Load<Sprite>("Art/" + n)).All(a => a != null && (int)a.rect.width == 720 && (int)a.rect.height == 1600)
                && new[] { "dial-ring-worn", "dial-ring-light-worn", "dial-ring-bright", "dial-ring-light-bright" }.Select(n => Resources.Load<Sprite>("Art/" + n)).All(a => a != null && (int)a.rect.width == 720 && (int)a.rect.height == 720)
                && new[] { "kit-dial-bright", "kit-dial-bright-open", "kit-dial-worn", "kit-dial-worn-open" }.All(n => Resources.Load<Sprite>("Art/" + n) is Sprite a && (int)a.rect.width == Slots.Find(n).Width * 2 && (int)a.rect.height == Slots.Find(n).Height * 2),
                "Batch 2: the Dial's final art is in: its room is a 1200 x 1840 master in the worn, today's and bright looks, its glow 720 x 1600 in each, the turning ring has its worn and bright looks at 720 x 720, and the Wing's small Dial has its bright pair and its new worn pair at their slots' size");
            // Batch 2 (owner, Oct 1: 3b C, 3c C, 3d; Oct 2: "Buttons at 44 px, bronze on the Table and the Book too"): the button pieces are in at their slots' size,
            // and each look builds as the board shows it; pressed darkens (x .78) and sinks 1 px; unavailable is half.
            { var btnPieces=new[]{"btn-plate","btn-plate-notch","btn-plate-diamond","btn-arrow","btn-rule-left","btn-rule-right","btn-rule-centre"};
              bool btnSized=btnPieces.All(bn=>Resources.Load<Sprite>("Art/"+bn) is Sprite ba && (int)ba.rect.width==Slots.Find(bn).Width*2 && (int)ba.rect.height==Slots.Find(bn).Height*2);
              var btnCanvas=new GameObject("Button test",typeof(RectTransform),typeof(Canvas)); var btnFont=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
              UnityEngine.UI.Button BtnMake(string words,float bw,float bh){var bgo=new GameObject(words,typeof(RectTransform));bgo.transform.SetParent(btnCanvas.transform,false);((RectTransform)bgo.transform).sizeDelta=new Vector2(bw,bh);bgo.AddComponent<UnityEngine.UI.Image>();var bb=bgo.AddComponent<UnityEngine.UI.Button>();var bl=new GameObject("Label",typeof(RectTransform));bl.transform.SetParent(bgo.transform,false);var bt=bl.AddComponent<UnityEngine.UI.Text>();bt.font=btnFont;bt.text=words;return bb;}
              var btnPlate=BtnMake("SEAL",146,56); var btnPf=ButtonLook.Plate(btnPlate,31); var btnArrow=BtnMake("Next",64,56); var btnAf=ButtonLook.Arrow(btnArrow,true); var btnRule=BtnMake("Leave the Dial",216,44); var btnRf=ButtonLook.Rule(btnRule); var btnRoom=BtnMake("The Dial",300,52); var btnMf=ButtonLook.Room(btnRoom,btnFont);
              bool btnBuilt=btnPf!=null && btnAf!=null && btnRf!=null && btnMf!=null && btnPlate.GetComponent<UnityEngine.UI.Image>().color.a==0 && btnPf.Pieces.Count==3 && btnRf.Pieces.Count==3 && btnAf.Pieces.Count==1 && btnMf.Pieces.Count==1
                && btnPf.Label.font==ButtonLook.EngravedFont && btnPf.Label.color==ButtonLook.Engraved && btnPf.Label.fontSize==31 && btnAf.Pieces[0].rectTransform.localScale.x==-1 && !btnAf.Label.enabled && btnMf.Label.text=="THE DIAL" && btnMf.Label.color==ButtonLook.RoomGold;
              bool btnPressed=false,btnReleased=false,btnHalf=false;
              if(btnBuilt){ btnPf.OnPointerDown(null); btnPf.Show(); btnPressed=btnPf.Look.anchoredPosition==new Vector2(0,-1) && btnPf.Pieces.All(bp=>bp.color==new Color(.78f,.78f,.78f)) && btnPf.Label.color==ButtonLook.EngravedDown;
                btnPf.OnPointerUp(null); btnPf.Show(); btnReleased=btnPf.Look.anchoredPosition==Vector2.zero && btnPf.Pieces.All(bp=>bp.color==Color.white) && btnPf.Label.color==ButtonLook.Engraved;
                btnPlate.interactable=false; btnPf.Show(); btnHalf=btnPlate.GetComponent<CanvasGroup>().alpha==.5f; }
              UnityEngine.Object.DestroyImmediate(btnCanvas);
              Check(btnSized && btnBuilt && btnPressed && btnReleased && btnHalf && ButtonLook.MinTarget==44,"Batch 2: the button art is in at its slots' size, and each look builds as the board shows it: a see-through hit area; the plate's frame with its notch and diamond whole and the word engraved (EB Garamond, the board's gold, SEAL at 31); the arrow (Next mirrored, no word); the rule's two lines and diamond; the room's frame and small gold capitals; pressed darkens (x .78) and sinks 1 px; unavailable is half; targets 44 px or more"); }
            // The Dial's wake-up (owner, Oct 1; 86bcbn6w6: 2a A, 2b B): five steps by the Keys earned, blended from three looks: worn; halfway to today's;
            // today's; halfway to bright; bright. Every layer has its worn and bright slot, and the Wing's small Dial its bright pair (2d A).
            Check(Enumerable.Range(0,5).Select(i=>DialView.WakeBlend(i)).SequenceEqual(new[]{(0,0,0f),(0,1,.5f),(1,1,0f),(1,2,.5f),(2,2,0f)}) && DialView.WakeBlend(-1)==(0,0,0f) && DialView.WakeBlend(9)==(2,2,0f)
              && new[]{"dial-room","dial-room-light","dial-ring","dial-ring-light"}.All(b=>new[]{"-worn","-bright"}.All(l=>Slots.Art.Any(a=>a.Name==b+l && a.Width==Slots.Find(b).Width && a.Height==Slots.Find(b).Height)))
              && Slots.Find("kit-dial-bright")!=null && Slots.Find("kit-dial-bright-open")!=null,"the Dial's wake-up: five steps from three looks (worn, halfway, today's, halfway, bright), each Dial layer with its worn and bright slot at its own size, the Wing's small Dial with its bright pair");
            // Build E: art slots and sound hooks. The manifest, the URL, the loader on the shipped test set, the import settings, the cues, the size budget.
            const int artSlots=224; // the message reads the number it asserts (it said 138 from Build Z to Build AC); 151 since the Dial's wake-up (+10: its worn and bright looks); 158 since batch 2's buttons (+7: the plate, its notch and diamond, the arrow, the rule's three pieces); 166 since the journal's landing (+8: the door frame, five emblems, the flourish, the Library plan); 187 since the Table and the Book come alive (+21: the two rooms, the well and two plates, four fills, twelve emblems); 201 with the opening scene (+14: twelve frames, the orb, its star); 205 with its slight animations (+4: two writing frames, the head lifted, the headphones halfway; owner, Oct 9); 221 with the launch (+16: the studio logo, the launch movie's six round layers and seven full frames, the menu's background and title; owner, Oct 9, rounds 3 and 4); 224 after the merged launch (-1: the baked wheel-to-Earth frame; +4: the menu's rat; owner, Oct 9)
            Check(Slots.Art.Length==artSlots && Slots.Art.Select(a=>a.Name).Distinct().Count()==artSlots && Slots.Art.All(a=>a.Name.All(c=>char.IsLower(c)||char.IsDigit(c)||c=='-') && char.IsLower(a.Name[0]) && a.Width>0 && a.Height>0 && a.MaxSize>=Mathf.Max(a.Width,a.Height) && a.Where.Length>0),artSlots+" art slots with unique kebab-case names (lower-case letters, digits after the first letter, hyphens), a rect, a size cap at or above the rect, and a place");
            Check(Slots.Sounds.Select(s=>s.Name).SequenceEqual(new[]{"step","seal","miss","key","page","door","ambient"}) && Slots.Sounds.All(s=>s.When.Length>0),"seven sound slots: step, seal, miss, key, page, door, ambient");
            Check(SliceView.CasparPoses.SequenceEqual(new[]{"calm","explain","warm","wry","moved","solemn"}) && SliceView.CasparPoses.All(p=>Slots.Art.Any(a=>a.Name=="caspar-"+p && a.Width==264 && a.Height==468)) && Slots.Art.Any(a=>a.Name=="chat-box" && a.Width==324 && a.Height==240) && Slots.Art.Any(a=>a.Name=="chat-plate"),"Build P: six poses of Caspar, each a 264 x 468 slot, and the chat box with its plate");
            Check(SliceView.AtriumPoses.Length==SliceView.AtriumPages.Length && SliceView.ReturnPoses.Length==SliceView.ReturnPages.Length && SliceView.AtriumPoses.Concat(SliceView.ReturnPoses).All(p=>SliceView.CasparPoses.Contains(p)) && SliceView.AtriumPoses.SequenceEqual(new[]{"wry","warm","solemn","explain"}) && SliceView.ReturnPoses.SequenceEqual(new[]{"moved","explain"}),"Build P: every page of the opening and the return has its pose (wry, warm, solemn, explain; moved, explain)");
            Check(SliceView.ChamberPoses.Length==SliceView.ChamberPages.Length && SliceView.ChamberPoses.SequenceEqual(new[]{"explain","solemn","calm"}) && SliceView.CasparPoses.Contains(SliceView.ChamberBreathesPose) && SliceView.CasparPoses.Contains(SliceView.ChamberContinuePose) && SliceView.ChamberBreathesPose=="moved" && SliceView.ChamberContinuePose=="warm","Build R: the Chamber's introduction has a pose per page (explain, solemn, calm), and his two lines after the first Key are moved and warm");
            Check(ChatFit.BoxHeight(34,false,120)==86 && ChatFit.BoxHeight(34,true,240)==136 && ChatFit.BoxHeight(0,false,120)==76 && ChatFit.BoxHeight(400,true,240)==240,"Build X: the gold chat box fits its line: 22 above (just under the plate), 30 below (clear of the bottom diamond), 50 more for a Continue inside, at least 76, at most its old height");
            Check(Slots.InstrumentBoxHeight(16,128)==60 && Slots.InstrumentBoxHeight(34,128)==78 && Slots.InstrumentBoxHeight(200,128)==128 && Slots.InstrumentBoxHeight(0,112)==60,"Build R: the instrument box fits its line: at least 60, header and foot around the line, never past its old height");
            Check(Slots.InstrumentBoxHeight(34,150,true)==100 && Slots.InstrumentBoxHeight(200,150,true)==150 && FitBox.PageLines==3,"Build S: a paged line keeps a 22 row for its Continue and shows at most three lines a page");
            Check(FitBox.Colour("Fire, Earth, Air and Water; Aries and Fireside stay plain")=="<color=#E0643C>Fire</color>, <color=#8DB36A>Earth</color>, <color=#E8D38F>Air</color> and <color=#63A6E0>Water</color>; Aries and Fireside stay plain","Build S: each element named in Caspar's line takes its own colour, whole words only");
            Slots.ParseQuery("https://davonlemar30.github.io/Ascendant/?style",out var qSet,out var qStyle);Check(qSet=="" && qStyle,"?style asks for the style page on the Art folder");
            Slots.ParseQuery("http://127.0.0.1:8765/?art=test",out qSet,out qStyle);Check(qSet=="test" && !qStyle,"?art=test plays with the test set");
            Slots.ParseQuery("http://127.0.0.1:8765/index.html?foo=1&style=test#top",out qSet,out qStyle);Check(qSet=="test" && qStyle,"?style=test is the style page on the test set, other parameters and the hash ignored");
            Slots.ParseQuery("http://127.0.0.1:8765/?art=../Evil_Set!",out qSet,out qStyle);Check(qSet=="evilset","a set name keeps letters, digits, and hyphens only");
            Slots.ParseQuery("",out qSet,out qStyle);Check(qSet=="" && !qStyle,"no URL: the Art folder, no style page");
            Slots.Request(Slots.TestSet,null);
            Check(new[]{"atrium-light","wing-light","chamber-light"}.All(n=>Slots.Art.Any(a=>a.Name==n && a.Width==360 && a.Height==800)) && SliceView.LightSlots.Length==3,"Build H: one light overlay slot per room, full-screen");
            Check(SliceView.LightAlphaFor(0,0)==0f && SliceView.LightAlphaFor(1,0)==0f && SliceView.LightAlphaFor(2,0)==.25f && SliceView.LightAlphaFor(3,0)==.5f && SliceView.LightAlphaFor(6,4)>.59f && SliceView.LightAlphaFor(6,4)<.6f && SliceView.LightAlphaFor(6,SliceFlow.LocksTotal)==1f && SliceFlow.LocksTotal==21,"the overlay's alpha: nothing at Stage 1, a quarter at Stage 2, then half plus the arc of 21 Keys spent - about .6 with the Wing whole, full only at the last lock (owner, Sept 27)");
            Check(Slots.Set=="test" && Slots.Art.All(a=>Slots.Image(a.Name)!=null) && Slots.Sounds.All(s=>Slots.Clip(s.Name)!=null) && Slots.ArtFiles==artSlots && Slots.SoundFiles==7,"the shipped test set has a file for every slot and the loader finds each one");
            Check(Slots.Art.All(a=>Slots.Source(a.Name)=="test set") && Slots.Sounds.All(s=>Slots.SoundSource(s.Name)=="test set"),"sources on the test set read 'test set'");
            Check(Slots.Image("no-such-slot")==null && Slots.Clip("no-such-slot")==null && Slots.Source("no-such-slot")=="placeholder" && Slots.SoundSource("no-such-slot")=="silent","an unknown slot loads nothing and reads placeholder or silent");
            var atriumImporter=AssetImporter.GetAtPath(SlotImport.ArtRoot+"test/atrium.png") as TextureImporter;var lockImporter=AssetImporter.GetAtPath(SlotImport.ArtRoot+"test/lock.png") as TextureImporter;
            Check(atriumImporter!=null && atriumImporter.textureType==TextureImporterType.Sprite && !atriumImporter.mipmapEnabled && !atriumImporter.crunchedCompression && atriumImporter.textureCompression==TextureImporterCompression.Compressed && atriumImporter.maxTextureSize==2048 && lockImporter!=null && lockImporter.maxTextureSize==32,"a PNG in the slot folder imports as a sprite: no mipmaps, never crunched, the slot's size cap");
            var stepImporter=AssetImporter.GetAtPath(SlotImport.AudioRoot+"test/step.wav") as AudioImporter;var ambientImporter=AssetImporter.GetAtPath(SlotImport.AudioRoot+"test/ambient.wav") as AudioImporter;
            Check(stepImporter!=null && stepImporter.forceToMono && stepImporter.defaultSampleSettings.compressionFormat==AudioCompressionFormat.Vorbis && stepImporter.defaultSampleSettings.loadType==AudioClipLoadType.DecompressOnLoad && ambientImporter!=null && !ambientImporter.forceToMono && ambientImporter.defaultSampleSettings.loadType==AudioClipLoadType.CompressedInMemory,"a WAV in the slot folder imports as Vorbis: cues mono and decompressed on load, the loop compressed in memory");
            long testBytes=Directory.GetFiles(SlotImport.ArtRoot+"test").Concat(Directory.GetFiles(SlotImport.AudioRoot+"test")).Where(f=>!f.EndsWith(".meta")).Sum(f=>new FileInfo(f).Length);
            Check(testBytes<1000000,"the test set is under 1 MB on disk: "+(testBytes/1024)+" KB");
            Slots.Request("",null);Check(Slots.Set=="" && Slots.Image("atrium")==Resources.Load<Sprite>("Art/atrium"),"the Art folder is the default set; its files, if any, are the owner's");
            Slots.Request(null,null);
            Check(Sound.Cue("dial_rotated",false,false)=="step" && Sound.Cue("sign_picked",false,false)=="step" && Sound.Cue("cell_chosen",false,false)=="step" && Sound.Cue("answer_correct",true,false)=="seal" && Sound.Cue("answer_rejected",false,false)=="miss" && Sound.Cue("glyph_named",true,true)=="seal" && Sound.Cue("glyph_named",false,true)=="miss" && Sound.Cue("builder_named",false,true)=="miss" && Sound.Cue("builder_share_marked",false,true)=="seal" && Sound.Cue("builder_shared",true,true)==null && Sound.Cue("builder_shared",false,true)=="miss","cues follow the model: a detent or a pick is a step, a right answer a seal, a wrong one a miss");
            Check(Sound.Cue("hint_requested",false,true)=="miss" && Sound.Cue("hint_requested",false,false)==null && Sound.Cue("key2_earned",true,false)=="key" && Sound.Cue("key3_earned",true,false)=="key" && Sound.Cue("key4_earned",true,false)=="key" && Sound.Cue("key1_earned",true,false)==null && Sound.Cue("seat_framed",false,false)==null && Sound.Cue("hint_escalated",false,false)==null,"a first miss in the book or the builder is a miss, the Count button is not; Keys 2 to 4 sound when earned, Key 1 when it rises; nothing else sounds");
            bool wasMuted=Sound.Muted;Sound.ToggleMute();Check(Sound.Muted!=wasMuted && AudioListener.volume==(Sound.Muted ? 0 : 1),"the mute toggle silences the listener and back");if(Sound.Muted)Sound.ToggleMute();
            ValidateCommittedSaves();
            ValidateEvidenceRouting();
            ValidateBuildF();
            ValidateBuildJ();
            ValidateTriangles();
            ValidateJournalFront();
            ValidateInscription();
            ValidateComeAlive();
            ValidatePractice();
            ValidateBirthChart();
            ValidateBirthRecord();
            ValidateBuildW();
            ValidateBuildZ();
            ValidateNoSwallowedCode();
            Directory.CreateDirectory("Logs");File.WriteAllLines("Logs/greybox-mechanical-validation.txt",Passed);
            Debug.Log("[GreyboxValidation] PASS: "+Passed.Count+" checks. Report: Logs/greybox-mechanical-validation.txt");
        }
    }
}
