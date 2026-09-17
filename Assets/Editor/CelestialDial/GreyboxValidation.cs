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
            var keyFlow = new SliceFlow(() => 1); keyFlow.Continue(); keyFlow.ChooseBirth("known"); keyFlow.SetKnownSign(1); keyFlow.Continue(); keyFlow.Continue();
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
        static void ValidateBuildF()
        {
            // ---- Build F: the practice fork on the Dial (Sept 15 ruling), the sitting rule (Sept 17), the three-strikes gate, the journal in the inventory ----
            SliceFlow Fresh() { var f = new SliceFlow(() => 1); f.Continue(); f.ChooseBirth("known"); f.SetKnownSign(1); f.Continue(); f.Continue(); f.RevealKey(); f.Continue(); f.Continue(); f.InsertKey(); f.End(); f.Continue(); return f; } // at the Hub, Stage 2, twelve element items entered
            string DeckKey(SliceFlow f) => string.Join("|", f.Deck.Items.Select(i => i.seat + ":" + i.kind + ":" + i.state + ":" + i.streak + ":" + i.interval + ":" + i.dueDay + ":" + i.entered));
            void AnswerAll(SliceFlow f) { int guard = 0; while (!f.PracticeDone && guard++ < 12) { var t = f.CurrentReview; if (t.Mode == ReviewMode.Tap) f.AnswerTap(Zodiac.Seats[t.seat].Element); else if (t.Mode == ReviewMode.TapModality) f.AnswerModalityTap(Zodiac.ModalityAt(t.seat)); else if (t.Mode == ReviewMode.Glyph) f.AnswerGlyph(t.seat); else f.FinishReview(true, true); } }
            var first = new SliceFlow(() => 1); first.Continue(); first.ChooseBirth("known"); first.SetKnownSign(1); first.Continue(); first.Continue();
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
            Check(f.JournalSections.SequenceEqual(new[] { ItemKind.Element }), "before the symbols the journal has one section");
            f.StartGlyphs(); f.Deck.RecordLesson(3, true, f.Sitting, ItemKind.Glyph);
            string deckBefore = DeckKey(f);
            Check(f.OpenJournal() && f.AtJournal && f.JournalFrom == SliceScreen.WingRoom && f.JournalKind == ItemKind.Element && f.JournalEntries(ItemKind.Element).Count == 12 && f.JournalEntries(ItemKind.Element)[0] == "Aries — Fire · " + SliceFlow.StateWord(f.Deck.Item(0, ItemKind.Element)) && !f.CanJournalPrev && f.CanJournalNext, "the journal opens from the room on the elements, twelve entries in wheel order");
            Check(f.JournalNext() && f.JournalKind == ItemKind.Glyph && f.JournalEntries(ItemKind.Glyph).Count == 12 && f.JournalEntries(ItemKind.Glyph)[3].EndsWith("practicing") && f.JournalEntries(ItemKind.Glyph)[4].EndsWith("introduced") && !f.CanJournalNext && !f.JournalNext() && f.JournalPrev() && f.JournalKind == ItemKind.Element && !f.JournalPrev(), "the symbols are a second section; pages turn both ways and stop at the ends; the state word follows the deck");
            Check(DeckKey(f) == deckBefore && f.CloseJournal() && f.Screen == SliceScreen.WingRoom && f.Note == "" && DeckKey(f) == deckBefore, "reading the journal changes no deck field; closing returns to the room and clears the gate note");
            Check(f.LeaveWing() && f.CanOpenJournal && f.OpenJournal() && f.JournalFrom == SliceScreen.Hub && f.CloseJournal() && f.AtHub && f.EnterChamber() && f.CanOpenJournal && f.OpenJournal() && f.CloseJournal() && f.AtChamberRoom, "the journal opens from the Atrium and the Chamber too");
            f.StartGrid(); f.StartOpposites();
            Check(f.JournalSections.SequenceEqual(new[] { ItemKind.Element, ItemKind.Glyph, ItemKind.Grid, ItemKind.Opposite }) && f.JournalEntries(ItemKind.Grid)[0] == "Aries — Fire · Cardinal · introduced" && f.JournalEntries(ItemKind.Opposite).Count == 6 && f.JournalEntries(ItemKind.Opposite)[0].StartsWith("Aries and Libra"), "the table and the opposites show in the journal as data, in curriculum order");
            Check(f.LeaveChamber() && f.ApproachDesk() && f.Note == SliceFlow.DeskLine && f.Screen == SliceScreen.Hub, "the desk is dressing: it only speaks");
            var save = UnityEngine.JsonUtility.FromJson<SaveData>(UnityEngine.JsonUtility.ToJson(f.ToSave(new bool[12], new bool[12], true))); var back = new SliceFlow(() => 1);
            Check(save.version == 4 && save.sittings == 5 && save.reviewsChecked == 5 && back.Restore(save) && back.Sittings == 5, "the save carries the sitting count both ways");
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
            var flow=new SliceFlow();Check(flow.Screen==SliceScreen.Identity && flow.DisplayName=="Keeper","slice starts at identity with a default Keeper name");
            flow.SetName("  Astra ");Check(flow.DisplayName=="Astra" && flow.Continue() && flow.Screen==SliceScreen.Birth,"name trimmed; identity continues to birth prompt");
            Check(!flow.Continue(),"birth prompt requires a choice");
            flow.ChooseBirth("chart");Check(!flow.CanContinue && !flow.SetBirthDate(13,1) && flow.SetBirthDate(4,25) && flow.SunSign==1 && flow.Note.Contains("Taurus") && flow.Continue() && flow.Screen==SliceScreen.Atrium,"birth date derives the sun sign and unlocks continue");
            Check(!flow.InsertKey() && flow.Continue() && flow.Screen==SliceScreen.Wing,"atrium continues to the wing; no key insertion outside the chamber");
            Check(!flow.Continue() && flow.RevealKey() && !flow.RevealKey() && flow.Continue() && flow.Screen==SliceScreen.AtriumReturn,"wing needs the key reveal once before continuing");
            Check(flow.Continue() && flow.Screen==SliceScreen.Chamber && !flow.Continue(),"chamber is the last screen");
            Check(flow.InsertKey() && flow.LocksFilled==1 && !flow.InsertKey() && flow.End() && flow.Ended && !flow.End(),"one key fills one lock of three and ends the prototype once");
            var unknown=new SliceFlow(()=>4);unknown.Continue();unknown.ChooseBirth("unknown");Check(unknown.SunSign==4 && unknown.Note.Contains("choose one for you") && unknown.Note.Contains("Leo") && unknown.CanContinue,"I don't know assigns a sun sign and Caspar says so");
            var known=new SliceFlow();known.Continue();known.ChooseBirth("known");Check(!known.CanContinue && known.SetKnownSign(7) && known.SunSign==7 && known.CanContinue,"a known sign is accepted directly");
            Check(Zodiac.SunSign(3,21)==0 && Zodiac.SunSign(3,20)==11 && Zodiac.SunSign(1,19)==9 && Zodiac.SunSign(1,20)==10 && Zodiac.SunSign(12,22)==9 && Zodiac.SunSign(12,21)==8 && Zodiac.SunSign(8,23)==5 && Zodiac.SunSign(2,30)==11 && Zodiac.SunSign(0,5)==-1 && Zodiac.SunSign(5,32)==-1,"sun sign date table covers every boundary and rejects bad input");
            var leo=new DialLesson(()=>0);leo.SetSunSign(4);Check(leo.Sun==4 && leo.GuidedFamily==0 && leo.SecondFamily==3 && leo.OptionalFamily==1,"sun sign picks the guided, second, and optional families");
            Check(leo.IntroStep==0 && leo.DialDormant && !leo.IntroAuto,"wing entrance starts with a dormant Dial");
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
            var loop=new SliceFlow(()=>1);loop.Continue();loop.ChooseBirth("known");loop.SetKnownSign(1);loop.Continue();loop.Continue();loop.RevealKey();loop.Continue();loop.Continue();loop.InsertKey();loop.End();
            Check(loop.Screen==SliceScreen.Chamber && loop.CanContinue && loop.Continue() && loop.Screen==SliceScreen.Hub && loop.AtriumStage==2 && loop.Deck.Items.Where(i=>i.Kind==ItemKind.Element).All(i=>i.entered),"after the ending, Continue reaches the Hub in Stage 2 and the deck opens");
            Check(loop.Sitting==0 && loop.DueCount==12 && loop.EnterWing() && loop.EnterDial() && loop.CanEnterPractice && loop.EnterPractice() && loop.Sitting==1 && loop.Screen==SliceScreen.Practice && loop.ReviewQueue.Count==ReviewDeck.BatchSize,"no in-game time: the first practice already has items ready; the entry is the first sitting and a batch of six begins");
            Check(loop.ReviewQueue[0].Mode==ReviewMode.Dial && loop.ReviewQueue[1].Mode==ReviewMode.Tap,"review alternates compressed Dial and direct tap");
            loop.FinishReview(true,true);Check(loop.ReviewIndex==1 && loop.Deck.Items[loop.ReviewQueue[0].seat].State==ItemState.Practicing,"a compressed Dial review success advances its item");
            var tapTask=loop.CurrentReview;string wrong=Zodiac.Seats[tapTask.seat].Element=="Fire" ? "Water" : "Fire";
            Check(!loop.AnswerTap(wrong) && !tapTask.done && loop.Note.Contains("Try once more"),"a direct-tap miss gives one nudge and a retry");
            Check(loop.AnswerTap(Zodiac.Seats[tapTask.seat].Element) && tapTask.done && tapTask.correct && loop.Deck.Items[tapTask.seat].State==ItemState.Introduced,"a correct tap after a nudge counts as correct but not as eligible evidence");
            var tap3=loop.ReviewQueue[3];loop.FinishReview(false,false);Check(loop.ReviewIndex==3,"a Dial miss moves on");
            loop.AnswerTap(Zodiac.Seats[tap3.seat].Element=="Air" ? "Water" : "Air");loop.AnswerTap(Zodiac.Seats[tap3.seat].Element=="Air" ? "Water" : "Air");
            Check(tap3.done && !tap3.correct && loop.Note.Contains("We will come back"),"two tap misses reveal the element and move on");
            loop.FinishReview(true,true);var last=loop.CurrentReview;loop.AnswerTap(Zodiac.Seats[last.seat].Element);
            Check(loop.PracticeDone && loop.PracticeSummary.EndsWith("of 6 remembered.") && loop.Sittings==1 && loop.LeavePractice() && loop.Screen==SliceScreen.Wing && loop.LeaveDial() && loop.LeaveWing() && loop.Screen==SliceScreen.Hub,"six items finish the practice with a summary; back to the Dial, the room, the Hub");
            Check(loop.EnterWing() && loop.Screen==SliceScreen.WingRoom && loop.EnterDial() && loop.Screen==SliceScreen.Wing && loop.LeaveDial() && loop.Screen==SliceScreen.WingRoom && loop.LeaveWing() && loop.Screen==SliceScreen.Hub && loop.AtriumStage==2,"the Wing can be entered from the Hub through its room and left back to it");
            loop.MarkWheelComplete();loop.EnterWing();loop.EnterDial();loop.LeaveDial();loop.LeaveWing();Check(loop.AtriumStage==3 && loop.V02Complete,"twelve lit seats and one more return complete v0.2");
            var save=loop.ToSave(new bool[12],new bool[12],true);var resumed=new SliceFlow(()=>1);
            Check(resumed.Restore(save) && resumed.Screen==SliceScreen.Hub && resumed.SunSign==1 && resumed.AtriumStage==3 && resumed.WheelComplete && resumed.Sitting==1 && resumed.Deck.Items[5].State==loop.Deck.Items[5].State && resumed.Sittings==1,"a saved session resumes at the Hub with deck, sittings, and stage");
            Check(!new SliceFlow().Restore(new SaveData{atriumStage=1,sunSign=1}),"a save from before the Hub does not resume");
            var unit=new DialLesson(()=>0);unit.SetSunSign(1);EnterGuided(unit);Answer(unit);Answer(unit);unit.Continue();Answer(unit);Answer(unit);
            Check(unit.KeyEarned && unit.FamiliesComplete==2 && unit.CanContinueUnit && unit.BeginContinuation() && unit.Phase==LessonPhase.Continuation && unit.Dial.Start==2 && unit.Lit[2] && unit.Message.Contains("done this twice"),"Unit 1.1 continues with the third family at Level 0");
            Answer(unit);Answer(unit);Check(unit.FamiliesComplete==3 && unit.Phase==LessonPhase.Continuation && unit.Dial.Start==3 && unit.Message.Contains("last family"),"third family done; the fourth begins on its own");
            Answer(unit);Answer(unit);Check(unit.Phase==LessonPhase.AllLit && unit.WheelComplete && unit.Lit.All(v=>v) && unit.Kin.All(v=>v) && !unit.CanContinueUnit,"twelve seats lit, four families, no Key");
            Check(unit.Dial.Events.Count(e=>e.event_name=="key1_earned")==1 && unit.Dial.Events.Any(e=>e.event_name=="wheel_completed"),"the continuation awards no second Key and logs wheel_completed");
            var rv=new DialLesson(()=>0);rv.RestoreProgress(4,Enumerable.Range(0,12).Select(i=>i%4==0).ToArray(),Enumerable.Range(0,12).Select(i=>i%4==0).ToArray(),true);
            Check(rv.Sun==4 && rv.KeyEarned && rv.Phase==LessonPhase.Complete && !rv.DialDormant && rv.FamiliesComplete==1 && rv.CanContinueUnit,"restored progress skips the intro and can continue");
            int rvSeat=-1;bool rvCorrect=false,rvEligible=false;rv.ReviewFinished+=(seat,c,e)=>{rvSeat=seat;rvCorrect=c;rvEligible=e;};
            Check(rv.BeginReview(4) && rv.Phase==LessonPhase.Review && rv.Dial.Start==4 && rv.Dial.Active && rv.Message.StartsWith("Find the next sign"),"a compressed review starts framed on the item's sign");
            rv.Dial.Select(8,DialInput.DirectSeat);rv.Seal();Check(rvSeat==4 && rvCorrect && rvEligible && !rv.Dial.Active,"a correct review Seal reports eligible success and returns home");
            rv.EndReview();Check(rv.Phase==LessonPhase.Complete && rv.BeginReview(0),"review ends back in the prior phase and another can begin");
            rv.Dial.Select(1,DialInput.DirectSeat);rv.Seal();Check(rv.Dial.Active && rv.Message.Contains("Try once more"),"first review miss nudges and allows a retry");
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
            var gflow=new SliceFlow(()=>1);gflow.Continue();gflow.ChooseBirth("known");gflow.SetKnownSign(1);gflow.Continue();gflow.Continue();gflow.RevealKey();gflow.Continue();gflow.Continue();gflow.InsertKey();gflow.End();gflow.Continue();
            gflow.MarkWheelComplete();gflow.EnterWing();gflow.LeaveWing();gflow.StartGlyphs();Check(gflow.GlyphsStarted && gflow.Deck.Due(gflow.Sitting).Count(i=>i.Kind==ItemKind.Glyph)==12,"starting the glyph unit introduces twelve glyph items");
            gflow.MarkKey2();gflow.EnterWing();gflow.LeaveWing();Check(gflow.Keys==2 && gflow.AtriumStage==3 && gflow.KeysInHand==1,"Key 2 earned: in hand, the Atrium waits for it to be spent (Build D)");
            Check(gflow.EnterChamber() && gflow.SpendKey() && gflow.LocksFilled==2 && gflow.LeaveChamber() && gflow.AtriumStage==4 && gflow.V03Complete,"spent in the Chamber, the return completes v0.3 at Stage 4");
            Check(gflow.EnterWing() && gflow.EnterDial() && gflow.EnterPractice() && gflow.ReviewQueue.Count==6 && gflow.ReviewQueue.All(t=>t.Mode!=ReviewMode.Glyph),"element items are due before glyph items in the practice order");
            var gsave=gflow.ToSave(new bool[12],new bool[12],true);var gres=new SliceFlow(()=>1);Check(gres.Restore(gsave) && gres.Keys==2 && gres.GlyphStage==2 && gres.V03Complete && gres.Deck.Item(0,ItemKind.Glyph).entered,"a save carries Keys, glyph stage, and glyph items");
            var greview=new SliceFlow(()=>1);greview.Continue();greview.ChooseBirth("known");greview.SetKnownSign(1);greview.Continue();greview.Continue();greview.RevealKey();greview.Continue();greview.Continue();greview.InsertKey();greview.End();greview.Continue();
            foreach(var it in greview.Deck.Items) if(it.Kind==ItemKind.Element){it.dueDay=99;} greview.StartGlyphs();
            Check(greview.EnterWing() && greview.EnterDial() && greview.EnterPractice() && greview.ReviewQueue.All(t=>t.Mode==ReviewMode.Glyph),"glyph items practice in the glyph form");
            var gt=greview.CurrentReview;var gopts=greview.GlyphReviewOptions(gt.seat);int wrongSeat=gopts.First(o=>o!=gt.seat);
            Check(!greview.AnswerGlyph(wrongSeat) && greview.Note.Contains("Try once more") && greview.AnswerGlyph(gt.seat) && gt.done && gt.correct && greview.Note.Contains("symbol of"),"a glyph review nudges once then accepts the name");
            var grl=new DialLesson(()=>0);grl.RestoreProgress(1,Enumerable.Repeat(true,12).ToArray(),Enumerable.Repeat(true,12).ToArray(),true);grl.RestoreGlyphs(2,12,true);Check(grl.Key2Earned && grl.Keys==2 && grl.Phase==LessonPhase.Key2 && !grl.CanBeginGlyphs,"restored Key 2 does not reopen the glyph unit");
            var grl2=new DialLesson(()=>0);grl2.RestoreProgress(1,Enumerable.Repeat(true,12).ToArray(),Enumerable.Repeat(true,12).ToArray(),true);grl2.RestoreGlyphs(0,5,false);Check(grl2.CanBeginGlyphs && grl2.BeginGlyphs() && grl2.CurrentGlyph==5,"restored Part A progress resumes at the next mark");
            // ---- v0.4: tap-to-move (Q06 phase 2 lock) ----
            Check(Rooms.Visible(Room.Atrium).Count(p=>p.Walkable)==4 && Rooms.Visible(Room.Atrium).Count(p=>!p.Walkable)==1 && Rooms.Visible(Room.Wing).Count()==4 && Rooms.Visible(Room.Wing).All(p=>p.Walkable) && Rooms.Find(Room.Wing,"grid").X==-60 && Rooms.Find(Room.Wing,"grid").Label=="the table","the Atrium offers the Wing doorway, the Chamber doorway, the desk, and Caspar plus one sealed door; the Wing offers the doorway back, the table, the Dial, and the shelf");
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
            wflow.Continue();wflow.ChooseBirth("known");wflow.SetKnownSign(1);wflow.Continue();wflow.Continue();wflow.RevealKey();wflow.Continue();wflow.Continue();wflow.InsertKey();wflow.End();wflow.Continue();
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
            Check(ask.AskCaspar() && ask.Dial.HintLevel==2 && ask.Dial.Asked && ask.Message.Contains("Find the next elemental sign after") && ask.CountBeatPending && !ask.CanAsk && ask.Dial.Events.Last().event_name=="hint_asked","asking gives the Level 2 rule reminder once, with the count, and is not a miss");
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
            ramp.Dial.Select(3,DialInput.DirectSeat);ramp.Seal();Check(ramp.Message.Contains("Find Aries first"),"the first replay keeps the Aries anchor");
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
            var jflow=new SliceFlow(()=>1);jflow.Continue();jflow.ChooseBirth("known");jflow.SetKnownSign(1);jflow.Continue();jflow.Continue();jflow.RevealKey();jflow.Continue();jflow.Continue();jflow.InsertKey();jflow.End();jflow.Continue();
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
            Check(!mod.ModalitiesComplete && mod.CanBeginModalities && mod.BeginModalities() && mod.Phase==LessonPhase.ModalityGuided && mod.Dial.Forward==3 && mod.Dial.Start==1 && mod.Dial.HintLevel==2 && mod.LitMod[1] && mod.CountBeatPending && mod.Message.Contains("second pattern") && mod.Message.Contains("Taurus is Fixed"),"after Key 2 the modality unit opens guided at the sun sign with the three-count and names the family");
            for(int i=0;i<3;i++){mod.Dial.Select(Zodiac.Destination(mod.Dial.Start,3),DialInput.DirectSeat);var r=mod.Seal();mod.AfterCorrect(r);}
            Check(mod.KinMod[1] && mod.Phase==LessonPhase.ModalityOwn && mod.Dial.HintLevel==0 && mod.Dial.Forward==3 && mod.Message.Contains("mutable"),"the guided family completes and the next kind starts on the player's own");
            mod.Dial.Select(mod.Dial.Start,DialInput.DirectSeat);mod.Seal();Check(mod.CanAsk && mod.AskCaspar() && mod.Message.Contains("same kind") && mod.Message.Contains("one, two, three."),"Ask Caspar in the modality unit gives the three-step rule");
            for(int i=0;i<8 && !mod.ModalitiesComplete;i++){if(!mod.Dial.Active)break;mod.Dial.Select(Zodiac.Destination(mod.Dial.Start,3),DialInput.DirectSeat);var r=mod.Seal();mod.AfterCorrect(r);}
            Check(mod.ModalitiesComplete && mod.Phase==LessonPhase.ModalityComplete && mod.KinMod.All(v=>v) && !mod.Key2Earned==false && mod.Dial.Events.Count(e=>e.event_name=="modalities_completed")==1 && mod.Dial.Events.Count(e=>e.event_name.StartsWith("key"))==0,"three families of four complete the unit with no new Key");
            Check(mod.SeatLabel(4).Contains("fixed"),"lit modality seats say their kind to the screen reader");
            var mflow=new SliceFlow(()=>1);mflow.Continue();mflow.ChooseBirth("known");mflow.SetKnownSign(1);mflow.Continue();mflow.Continue();mflow.RevealKey();mflow.Continue();mflow.Continue();mflow.InsertKey();mflow.End();mflow.Continue();
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
            Check(!grid.CanSeal && !grid.Choose(1) && grid.Pick(4) && grid.Sign==4 && !grid.CanSeal && grid.Choose(1) && grid.CanSeal && grid.Readout.Contains("Leo to Fire, fixed"),"a sign in hand, then a cell: the readout names both and nothing has been sealed");
            Check(grid.Pick(0) && grid.Sign==0 && grid.Cell==-1 && grid.Pick(4) && grid.Sign==4,"before a miss the player may change the sign in hand; the cell clears");
            grid.Choose(GridModel.CellOf(4));var leoSeat=grid.Seal();
            Check(leoSeat!=null && leoSeat.correctness && leoSeat.evidence_eligible && grid.Placed[4] && grid.Evidence && grid.Sign==-1 && grid.Message.StartsWith("Yes. Leo: Fire, fixed.") && grid.Message.Contains("on your own") && glog.Last().event_name=="grid_placed" && glog.Last().requested_relationship=="cell_of_sign" && glog.Last().destination_sign=="Leo" && glog.Last().evidence_eligible,"a right cell at Level 0 seats the sign with evidence and logs the placement");
            Check(!grid.CanPick(4) && !grid.CanChoose(GridModel.CellOf(4)),"a seated sign and its cell are out of play");
            grid.Pick(1);grid.Choose(GridModel.CellOf(8));var miss=grid.Seal(); // Taurus, Earth fixed, to Fire mutable: the row is wrong, so the nudge names the element
            Check(!miss.correctness && grid.HintLevel==1 && grid.Attempts==1 && grid.Rejected==GridModel.CellOf(8) && grid.Cell==-1 && grid.Message=="Not that cell. Taurus is an Earth sign." && grid.Locked && !grid.CanPick(2) && grid.CanPick(1) && grid.CanAsk && grid.CellLabel(GridModel.CellOf(8)).EndsWith("not that one"),"a first miss nudges with the element, marks the cell, locks the sign in hand, and offers Ask Caspar");
            grid.Choose(GridModel.CellOf(9));grid.Seal(); // Earth cardinal: row right, column wrong
            Check(grid.HintLevel==2 && grid.Message.StartsWith("Taurus is Earth; it is fixed.") && !grid.CanAsk && !grid.Demonstrating && grid.Active,"a second miss states the rule: element and kind");
            grid.Choose(GridModel.CellOf(1));var taurusSeat=grid.Seal();
            Check(taurusSeat.correctness && !taurusSeat.evidence_eligible && grid.Placed[1] && grid.Assisted[1] && grid.Message.Contains("together"),"a Level 2 seating counts for the table, not as evidence");
            grid.Pick(2);grid.Choose(GridModel.CellOf(6));grid.Seal(); // Gemini, Air mutable, to Air cardinal: the column is wrong, so the nudge names the kind
            Check(grid.HintLevel==1 && grid.Message=="Not that cell. Gemini is mutable.","a wrong column nudges with the kind");
            Check(grid.Ask() && grid.Asked && grid.HintLevel==2 && grid.Message.StartsWith("Gemini is Air; it is mutable.") && !grid.CanAsk && glog.Last().event_name=="hint_asked","asking Caspar gives the rule once and is not a miss");
            grid.Choose(GridModel.CellOf(2));var askedSeat=grid.Seal();
            Check(askedSeat.correctness && !askedSeat.evidence_eligible && grid.Placed[2],"a seating after asking earns no evidence");
            grid.Pick(3);grid.Choose(GridModel.CellOf(7));grid.Seal();grid.Ask();grid.Choose(GridModel.CellOf(11));grid.Seal();
            Check(grid.HintLevel==3 && grid.Demonstrating && !grid.Active && !grid.CanSeal && grid.DemonstrationCell==GridModel.CellOf(3) && grid.Message.StartsWith("Watch me seat it."),"a miss after the asked rule asks Caspar to seat it");
            grid.AfterDemonstration();
            Check(!grid.Demonstrating && grid.Placed[3] && grid.Assisted[3] && grid.AssistedThisSitting==1 && grid.Active && grid.Message.Contains("Cancer is seated") && glog.Count(e=>e.event_name=="grid_placed")==4 && glog.Where(e=>e.event_name=="grid_placed").Count(e=>e.evidence_eligible)==1,"the demonstration seats the sign without evidence and the table stays open");
            void Demo(GridModel g,int seat){g.Pick(seat);for(int k=0;k<3;k++){g.Choose(GridModel.CellOf(Zodiac.Wrap(seat+1)));g.Seal();}g.AfterDemonstration();}
            Demo(grid,5);Check(grid.AssistedThisSitting==2 && grid.Placed[5] && grid.Active,"three wrong cells reach the demonstration");
            Demo(grid,6);Check(grid.AssistedThisSitting==3 && grid.Phase==GridPhase.Paused && !grid.Active && grid.Message==GridModel.PausedLine && glog.Last().event_name=="grid_paused","the third demonstration in one sitting pauses the table (the cap)");
            Check(grid.Begin() && grid.Active && grid.AssistedThisSitting==0 && grid.PlacedCount==6 && grid.Message.Contains("6 of twelve seated") && glog.Last().event_name=="grid_resumed","coming back resumes the table with its seated signs and a fresh cap");
            foreach(int seat in new[]{0,7,8,9,10}){grid.Pick(seat);grid.Choose(GridModel.CellOf(seat));grid.Seal();}
            Check(grid.PlacedCount==11 && grid.Pick(11) && Enumerable.Range(0,12).Count(c=>grid.CanChoose(c))==1 && grid.HintLevel==0,"the last sign has one empty cell left, so it seats at Level 0 by elimination"); // owner question: Key 3 then follows twelve seated in every case
            grid.Choose(GridModel.CellOf(11));grid.Seal();
            Check(grid.Complete && grid.Key3Earned && grid.Phase==GridPhase.Complete && grid.Message==GridModel.Key3Line && glog.Count(e=>e.event_name=="key3_earned")==1 && !grid.Active && grid.TileLabel(11).EndsWith("seated") && grid.CellLabel(GridModel.CellOf(11))=="Water, mutable: Pisces","twelve seated with at least one Level 0/1 seating earns Key 3 once");
            Check(grid.Begin() && grid.Message==GridModel.FullLine && !grid.Active && glog.Count(e=>e.event_name=="key3_earned")==1,"a full table only shows itself; Key 3 is never re-earned");
            var fullBack=new GridModel(()=>0);fullBack.Restore(grid.Placed,true,true,true);Check(fullBack.Key3Earned && fullBack.Complete && fullBack.Phase==GridPhase.Complete && fullBack.Message==GridModel.FullLine,"a restored full table with Key 3 shows itself full");
            var half=new GridModel(()=>0);half.Restore(Enumerable.Range(0,12).Select(i=>i<5).ToArray(),true,true,false);
            Check(half.PlacedCount==5 && half.Evidence && half.Started && !half.Active && half.Begin() && half.Active && half.CanPick(5) && !half.CanPick(4) && half.Message.Contains("5 of twelve"),"a restored half table resumes with its seated signs");
            var tflow=new SliceFlow(()=>1);tflow.Continue();tflow.ChooseBirth("known");tflow.SetKnownSign(1);tflow.Continue();tflow.Continue();tflow.RevealKey();tflow.Continue();tflow.Continue();tflow.InsertKey();tflow.End();tflow.Continue();
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
            Check(Zodiac.Polarities.Length==2 && Zodiac.PolarityAt(0)=="day" && Zodiac.PolarityAt(1)=="night" && Enumerable.Range(0,12).All(i=>Zodiac.PolarityAt(i)==(Zodiac.Seats[i].Element=="Fire"||Zodiac.Seats[i].Element=="Air" ? "day" : "night")),"fire and air are day signs, earth and water night signs: the side follows the element");
            Check(Enumerable.Range(0,12).All(i=>Zodiac.Opposite(Zodiac.Opposite(i))==i && Zodiac.ModalityAt(Zodiac.Opposite(i))==Zodiac.ModalityAt(i) && Zodiac.PolarityAt(Zodiac.Opposite(i))==Zodiac.PolarityAt(i) && Zodiac.Seats[Zodiac.Opposite(i)].Element!=Zodiac.Seats[i].Element) && Zodiac.Opposite(0)==6 && Zodiac.PairOf(9)==3 && Zodiac.PairOf(3)==3 && Zodiac.OppositePairs==6,"opposites sit six seats on, share kind and side, and differ in element; six pairs named by their lower seat");
            var m6=new DialModel(()=>0);m6.Begin(1,0,-1,6);m6.Select(4,DialInput.DirectSeat);Check(!m6.Commit().correctness,"three forward is wrong on an opposite problem");m6.Begin(1,0,-1,6);m6.Select(7,DialInput.DirectSeat);Check(m6.Commit().correctness && m6.Relationship=="forward_offset_6","six forward is right");
            bool[] all12=Enumerable.Repeat(true,12).ToArray(),all3=Enumerable.Repeat(true,3).ToArray();
            DialLesson AfterKey3(int sun){var l=new DialLesson(()=>0);l.RestoreProgress(sun,all12,all12,true);l.RestoreGlyphs(2,12,true);l.RestoreModalities(all12,all3,true);return l;}
            var op=AfterKey3(1);
            Check(!op.CanBeginOpposites && !op.BeginOpposites(),"without Key 3 the last pattern waits");
            op.SetKey3(true);Check(op.CanBeginOpposites && op.BeginOpposites() && op.Phase==LessonPhase.Polarity && !op.PolarityShown && op.Message==DialLesson.PolarityLine0 && op.Dial.Events.Last().event_name=="opposites_unit_started" && !op.IsProblem,"with Key 3 the unit opens on the polarity beat");
            op.Continue();Check(op.PolarityShown && op.Phase==LessonPhase.Polarity && op.Message.Contains("day and night") && op.Message.Contains("masculine") && op.SeatLabel(0).Contains(", day") && op.SeatLabel(1).Contains(", night"),"the second line names the older terms and the seats say their side");
            op.Continue();Check(op.Phase==LessonPhase.OppositeGuided && op.Dial.Forward==6 && op.Dial.Start==1 && op.Dial.HintLevel==2 && op.CountBeatPending && op.Message.Contains("six seats on") && op.IsProblem && !op.CanAsk,"the first pair is guided from the sun sign with the six-count");
            op.CountBeatShown();op.Dial.Select(7,DialInput.DirectSeat);var oppFirst=op.Seal();
            Check(oppFirst.correctness && !oppFirst.evidence_eligible && op.CorrectLine(oppFirst).Contains("sits across from Taurus") && op.CorrectLine(oppFirst).Contains("Both fixed, both night") && op.CorrectLine(oppFirst).Contains("Earth against Water"),"a correct opposite names what the pair shares and what differs");
            op.AfterCorrect(oppFirst);Check(op.OppKnown[1] && op.PairsKnown==1 && op.Phase==LessonPhase.OppositeOwn && op.Dial.Start==2 && op.Dial.HintLevel==0 && op.Dial.Forward==6 && op.Message.Contains("across from Gemini"),"the guided pair is known; the next pair starts on the player's own");
            op.Dial.Select(3,DialInput.DirectSeat);op.Seal();Check(op.Dial.HintLevel==1 && op.CanAsk && op.Dial.Active,"a first miss nudges and offers Ask Caspar");
            Check(op.AskCaspar() && op.Message.Contains("six seats on from Gemini") && op.Message.Contains("five, six") && op.CountBeatPending,"Ask Caspar gives the six-seat rule with the count");
            op.CountBeatShown();op.Dial.Select(8,DialInput.DirectSeat);var askedOpp=op.Seal();op.AfterCorrect(askedOpp);Check(askedOpp.correctness && !askedOpp.evidence_eligible && op.PairsKnown==2 && op.Dial.Start==3,"a pair found after asking counts, not as evidence");
            op.Dial.Select(3,DialInput.DirectSeat);op.Seal();op.Seal();op.Seal();Check(!op.Dial.Active && op.Dial.HintLevel==3 && op.Message.StartsWith("Watch me find it.") && op.Message.Contains("the next pair"),"a third miss asks for the worked example");
            op.RevealDemonstration();op.AfterDemonstration();Check(op.PairsKnown==3 && op.OppKnown[3] && op.Phase==LessonPhase.OppositeOwn && op.Dial.Start==4 && op.Dial.Active,"the worked example marks the pair without evidence and moves on");
            void Demo6(DialLesson l){for(int k=0;k<3;k++){l.Dial.Select(l.Dial.Start,DialInput.DirectSeat);l.Seal();}l.RevealDemonstration();l.AfterDemonstration();}
            Demo6(op);Demo6(op);Check(op.Phase==LessonPhase.OppositePaused && op.PairsKnown==5 && op.Message==DialLesson.OppositesPausedLine && !op.Dial.Active && op.Dial.Events.Last().event_name=="opposites_paused","three worked examples in one sitting pause the last pattern");
            Check(op.BeginOpposites() && op.Phase==LessonPhase.OppositeOwn && op.Dial.Start==6 && op.Dial.Events.Last().event_name=="opposites_resumed","coming back resumes at the missing pair with a fresh cap");
            op.Dial.Select(0,DialInput.DirectSeat);var lastPair=op.Seal();op.AfterCorrect(lastPair);
            Check(lastPair.correctness && lastPair.evidence_eligible && op.OppositesComplete && op.Phase==LessonPhase.OppositesComplete && op.Message==DialLesson.OppositesDoneLine && op.Dial.Events.Count(e=>e.event_name=="opposites_completed")==1 && !op.Dial.Active,"six pairs complete the last pattern");
            op.Continue();Check(op.Phase==LessonPhase.BuilderName && op.BuilderStep==1 && op.BuilderTarget==1 && op.Built==0 && op.Message.Contains("Earth, fixed") && op.BuilderOptions.Length==4 && op.BuilderOptions.Distinct().Count()==4 && op.BuilderOptions.Contains(1) && op.BuilderOptions.Contains(5) && op.BuilderOptions.Contains(9) && op.BuilderOptions.Contains(4) && op.InBuilder && !op.IsProblem,"Continue opens the builder: element and kind given, four names with the two same-element signs and a same-kind one");
            Check(!op.AnswerBuilderName(5) && op.Message.Contains("Virgo is mutable") && op.Message.Contains("Find the Earth sign that is fixed") && op.Phase==LessonPhase.BuilderName,"a wrong name of the right element is nudged with its kind");
            Check(op.AnswerBuilderName(1) && op.Phase==LessonPhase.BuilderOpposite && op.BuilderStep==2 && op.Dial.Active && op.Dial.Start==1 && op.Dial.Forward==6 && op.Message.StartsWith("Yes. Taurus: Earth, fixed.") && op.Dial.Events.Last(e=>e.event_name=="builder_named").evidence_eligible,"the right name after one nudge hands to the wheel");
            op.Dial.Select(7,DialInput.DirectSeat);var built1=op.Seal();op.AfterCorrect(built1);
            Check(built1.evidence_eligible && op.Phase==LessonPhase.BuilderShare && op.BuilderStep==3 && op.Message.Contains("Taurus and Scorpio") && !op.Dial.Active && !op.IsProblem,"the opposite found, the share step asks what the two have in common");
            Check(!op.AnswerBuilderShare(2) && op.Message.Contains("Not the element") && op.Message.Contains("Taurus is Earth; Scorpio is Water") && op.BuilderStep==3,"tapping the element is nudged once");
            Check(op.AnswerBuilderShare(0) && op.Shared[0] && op.BuilderStep==3 && op.Message.Contains("What else"),"the kind is shared; one more to find");
            Check(op.AnswerBuilderShare(1) && op.Shared[1] && op.BuilderStep==0 && op.Built==1 && op.BuilderEvidence && op.Message.Contains("Same kind, same side") && op.Dial.Events.Last(e=>e.event_name=="builder_sign_built").evidence_eligible,"the side too: the first sign is built unassisted (one nudge per step stays Level 1)");
            op.NextBuilderSign();Check(op.Phase==LessonPhase.BuilderName && op.Built==1 && op.BuilderTarget==6 && op.Message.Contains("Air, cardinal") && op.BuilderOptions.Contains(6),"the second sign: Libra, from Air and cardinal");
            Check(!op.AnswerBuilderName(2) && !op.AnswerBuilderName(10) && op.Phase==LessonPhase.BuilderOpposite && op.Message.StartsWith("It is Libra") && !op.Dial.Events.Last(e=>e.event_name=="builder_named").evidence_eligible,"two wrong names reveal the sign and hand to the wheel, assisted");
            op.Dial.Select(1,DialInput.DirectSeat);op.Seal();op.Seal();op.Seal();Check(!op.Dial.Active && op.Message.Contains("what the two share"),"a third wrong turn in the builder asks for the worked example");
            op.RevealDemonstration();op.AfterDemonstration();Check(op.Phase==LessonPhase.BuilderShare && op.Message.Contains("Libra and Aries"),"the worked example on the wheel still reaches the share step");
            Check(!op.AnswerBuilderShare(2) && !op.AnswerBuilderShare(2) && op.Shared[0] && op.Shared[1] && op.Built==2 && op.BuilderStep==0 && op.Message.StartsWith("Opposites share the kind and the side") && !op.Dial.Events.Last(e=>e.event_name=="builder_sign_built").evidence_eligible,"two wrong taps reveal the rule; the second sign counts as assisted");
            op.NextBuilderSign();Check(op.BuilderTarget==8 && op.Message.Contains("Fire, mutable") && op.AnswerBuilderName(8) && op.Phase==LessonPhase.BuilderOpposite,"the third sign: Sagittarius, from Fire and mutable");
            op.Dial.Select(2,DialInput.DirectSeat);var built3=op.Seal();op.AfterCorrect(built3);Check(op.AnswerBuilderShare(1) && op.AnswerBuilderShare(0) && op.Built==3 && op.BuilderStep==0,"three signs built");
            op.NextBuilderSign();Check(op.Key4Earned && op.Phase==LessonPhase.Key4 && op.Message==DialLesson.Key4Line && op.Dial.Events.Count(e=>e.event_name=="key4_earned")==1 && !op.CanBeginOpposites && !op.BeginOpposites(),"three built with one unassisted earns Key 4 once; the unit closes");
            var nk=AfterKey3(4);nk.SetKey3(true);nk.RestoreOpposites(true,Enumerable.Repeat(true,6).ToArray(),true,0,false,false);
            Check(nk.BeginOpposites() && nk.Phase==LessonPhase.BuilderName && nk.BuilderTarget==4 && nk.PolarityShown,"a restored unit with six pairs known opens straight on the builder (a Leo sun builds Leo first)");
            for(int k=0;k<3;k++){int t=nk.BuilderTarget;var wrongNames=nk.BuilderOptions.Where(o=>o!=t).ToArray();nk.AnswerBuilderName(wrongNames[0]);nk.AnswerBuilderName(wrongNames[1]);nk.Dial.Select(Zodiac.Opposite(t),DialInput.DirectSeat);var r=nk.Seal();nk.AfterCorrect(r);nk.AnswerBuilderShare(0);nk.AnswerBuilderShare(1);nk.NextBuilderSign();}
            Check(nk.Built==3 && !nk.Key4Earned && nk.Phase==LessonPhase.BuilderPaused && nk.Message==DialLesson.BuilderNoEvidenceLine && nk.Dial.Events.Count(e=>e.event_name=="key4_earned")==0,"three signs built with every name revealed earn no Key");
            Check(nk.BeginOpposites() && nk.Built==0 && nk.Phase==LessonPhase.BuilderName && nk.Dial.Events.Any(e=>e.event_name=="builder_cleared"),"the next visit clears the builder for another try");
            var halfPairs=AfterKey3(1);halfPairs.SetKey3(true);halfPairs.RestoreOpposites(true,new[]{true,true,false,false,false,false},true,0,false,false);
            Check(halfPairs.BeginOpposites() && halfPairs.Phase==LessonPhase.OppositeOwn && halfPairs.Dial.Start==2 && halfPairs.PairsKnown==2 && halfPairs.PolarityShown && halfPairs.SeatLabel(4).Contains(", day"),"a restored half-known pattern resumes at the next pair with the sides shown");
            var mid=AfterKey3(1);mid.SetKey3(true);mid.RestoreOpposites(true,Enumerable.Repeat(true,6).ToArray(),true,2,true,false);
            Check(mid.BeginOpposites() && mid.Phase==LessonPhase.BuilderName && mid.Built==2 && mid.BuilderTarget==8,"a restored builder resumes at the third sign");
            var cflow=new SliceFlow(()=>1);cflow.Continue();cflow.ChooseBirth("known");cflow.SetKnownSign(1);cflow.Continue();cflow.Continue();cflow.RevealKey();cflow.Continue();cflow.Continue();cflow.InsertKey();cflow.End();cflow.Continue();
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
            var dflow=new SliceFlow(()=>1);var dlog=new List<string>();dflow.Logged+=dlog.Add;dflow.Continue();dflow.ChooseBirth("known");dflow.SetKnownSign(1);dflow.Continue();dflow.Continue();dflow.RevealKey();dflow.Continue();dflow.Continue();dflow.InsertKey();dflow.End();dflow.Continue();
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
            // Build E: art slots and sound hooks. The manifest, the URL, the loader on the shipped test set, the import settings, the cues, the size budget.
            Check(Slots.Art.Length==30 && Slots.Art.Select(a=>a.Name).Distinct().Count()==30 && Slots.Art.All(a=>a.Name.All(c=>char.IsLower(c)||c=='-') && a.Width>0 && a.Height>0 && a.MaxSize>=Mathf.Max(a.Width,a.Height) && a.Where.Length>0),"28 art slots with unique kebab-case names, a rect, a size cap at or above the rect, and a place");
            Check(Slots.Sounds.Select(s=>s.Name).SequenceEqual(new[]{"step","seal","miss","key","page","door","ambient"}) && Slots.Sounds.All(s=>s.When.Length>0),"seven sound slots: step, seal, miss, key, page, door, ambient");
            Slots.ParseQuery("https://davonlemar30.github.io/Ascendant/?style",out var qSet,out var qStyle);Check(qSet=="" && qStyle,"?style asks for the style page on the Art folder");
            Slots.ParseQuery("http://127.0.0.1:8765/?art=test",out qSet,out qStyle);Check(qSet=="test" && !qStyle,"?art=test plays with the test set");
            Slots.ParseQuery("http://127.0.0.1:8765/index.html?foo=1&style=test#top",out qSet,out qStyle);Check(qSet=="test" && qStyle,"?style=test is the style page on the test set, other parameters and the hash ignored");
            Slots.ParseQuery("http://127.0.0.1:8765/?art=../Evil_Set!",out qSet,out qStyle);Check(qSet=="evilset","a set name keeps letters, digits, and hyphens only");
            Slots.ParseQuery("",out qSet,out qStyle);Check(qSet=="" && !qStyle,"no URL: the Art folder, no style page");
            Slots.Request(Slots.TestSet,null);
            Check(Slots.Set=="test" && Slots.Art.All(a=>Slots.Image(a.Name)!=null) && Slots.Sounds.All(s=>Slots.Clip(s.Name)!=null) && Slots.ArtFiles==30 && Slots.SoundFiles==7,"the shipped test set has a file for every slot and the loader finds each one");
            Check(Slots.Art.All(a=>Slots.Source(a.Name)=="test set") && Slots.Sounds.All(s=>Slots.SoundSource(s.Name)=="test set"),"sources on the test set read 'test set'");
            Check(Slots.Image("no-such-slot")==null && Slots.Clip("no-such-slot")==null && Slots.Source("no-such-slot")=="placeholder" && Slots.SoundSource("no-such-slot")=="silent","an unknown slot loads nothing and reads placeholder or silent");
            var atriumImporter=AssetImporter.GetAtPath(SlotImport.ArtRoot+"test/atrium.png") as TextureImporter;var lockImporter=AssetImporter.GetAtPath(SlotImport.ArtRoot+"test/lock.png") as TextureImporter;
            Check(atriumImporter!=null && atriumImporter.textureType==TextureImporterType.Sprite && !atriumImporter.mipmapEnabled && atriumImporter.crunchedCompression && atriumImporter.textureCompression==TextureImporterCompression.Compressed && atriumImporter.maxTextureSize==2048 && lockImporter!=null && lockImporter.maxTextureSize==32,"a PNG in the slot folder imports as a sprite: no mipmaps, crunched compression, the slot's size cap");
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
            Directory.CreateDirectory("Logs");File.WriteAllLines("Logs/greybox-mechanical-validation.txt",Passed);
            Debug.Log("[GreyboxValidation] PASS: "+Passed.Count+" checks. Report: Logs/greybox-mechanical-validation.txt");
        }
    }
}
