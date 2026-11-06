Codex draft (goal 03); owner rewrites. `{student}` alternates pupil / apprentice in teaching lines.

| ID | Draft |
| --- | --- |
| Title | The Hall of Houses |
| BackToAtrium | The Grand Atrium |
| Door | Another plate has answered, seeker. The Hall of Houses is open. |
| First | A horizon in brass, {student}, and a meridian over it. The instrument waits in the middle. |
| Instrument | The local sky |
| AnglesIntro | Four labels for the local sky, {student}. Set AC at the eastern horizon, DC at the western horizon, MC at the upper meridian and IC at the lower meridian. |
| AnglesAsk | Place {angle} on the local-sky diagram. |
| AngleNudge | Find the horizon or meridian that belongs to this label, {student}. |
| AngleRule | AC is east, on the left; DC is west, on the right. MC is the upper meridian, at the top; IC is the lower meridian, at the bottom, {student}. |
| AngleShow | The label sits here, {student}. Keep its place as we turn the zodiac over it. |
| OverlayIntro | Now the zodiac, {student}. Turn the overlay until the named sign rises at the eastern horizon, then Seal. |
| OverlayAsk | Bring {sign} to the eastern horizon, then Seal. |
| OverlayNudge | Look to the left-hand horizon, {student}. That is where the sign rises on this chart. |
| OverlayRule | The Ascendant names the sign rising on the eastern horizon, {student}. Turn the overlay to bring the named sign there. |
| OverlayShow | Here is {sign} at the eastern horizon, {student}. Set it there and Seal. |
| Key9 | The local sky is set, initiate. Keeper Key 9 is yours. The third Book has a final lock waiting. |
| Paused9 | My hand found each rising sign, seeker. The Key will wait. We can turn the overlay again when you are ready. |
| NeedRising | This task uses your rising sign. Open Your Birth in the journal before we assign the houses, {student}. |
| HousesIntro | The angles come from the local sky, {student}. Whole Sign Houses assign one whole sign to each house. The sign containing the Ascendant is the first; count the next signs in zodiac order. |
| Ascendant | Ascendant: {sign} |
| HouseAsk | Turn to the {house} house, then Seal. |
| HouseNudge | Start with the rising sign as the first house, {student}, and count the signs from there. |
| HouseRule | House n is the rising sign plus n minus one, wrapping through twelve signs, {student}. Each house holds a whole sign of thirty degrees. |
| HouseShow | For this Ascendant, the {house} house is {sign}, {student}. Turn there and Seal. |
| TopicsIntro | House is where, sign is how, planet is what, {student}. We shall place the topics in axis pairs, starting from the angles. |
| TopicAsk | Which house holds {topic}? |
| TopicWhy | House {house}: {topics}. |
| TopicNudge | Return to the axis pairs, {student}. Think of the topic that belongs in each house. |
| GroupsIntro | One more sort, {student}. Houses 1, 4, 7 and 10 are angular; 2, 5, 8 and 11 are succedent; 3, 6, 9 and 12 are cadent. |
| GroupAsk | House {house}. Which group? |
| GroupNudge | Count the houses from each angle, {student}. Each three-house stretch has one of each group. |
| GroupWhy | House {house} is {group}. |
| Example | An unnamed example, {student}: London, 1 January 1900, 14:00 local time. The Ascendant degree falls inside the first whole-sign house. The MC is calculated separately, where the ecliptic crosses the upper meridian. |
| ExamplePositions | AC {ac} {rising}; MC {mc} {mcSign} |
| ExampleAsk | Which house holds this chart's MC? |
| ExampleWhy | The Ascendant is in {rising}; the MC is in {mc}. Counting whole signs puts this MC in house {house}. |
| DistinctionAsk | How do the 7th house and Libra differ? |
| DistinctionRight | The 7th house is a place counted from the rising sign; Libra is a sign. |
| DistinctionWrong1 | The 7th house is Libra in every chart. |
| DistinctionWrong2 | The first house is Aries in every chart. |
| DistinctionWrong3 | Every house contains only part of a sign. |
| DistinctionWhy | The rising sign sets the first whole-sign house. The seventh is six signs from it. Houses are places in a chart; signs describe how. |
| Key10 | Twelve places, and you have found your way around them, seeker. Keeper Key 10 is yours. Carry it to the Chamber. |
| Paused10 | My hand found every house this time, initiate. The Key will wait for a place you count yourself. |
| Book3 | The third Book opens, initiate. Three of seven. The Hall has another task for you when you return. |
| Hub9 | Nine Keys, seeker. The third Book is waiting for the one you carry. |
| Hub10 | Ten Keys, initiate. The next Book has a place for the one in your hand. |
| End | Ten locks turned, seeker. Three Books are open. The Hall of Houses is set; the remaining rooms stay sealed. |
| Chapter | The Houses |
| JournalIntro | Whole Sign Houses: the sign containing the Ascendant is the 1st house; each next sign in zodiac order is the next house. Each house is a whole sign of 30 degrees. The Ascendant degree falls inside the 1st house. The MC is calculated separately and can fall outside the 10th whole-sign house. House is where, sign is how, planet is what. |
| Angular | Angular |
| Succedent | Succedent |
| Cadent | Cadent |

Practice draft questions and explanations:

{
  "id": "angles",
  "name": "The Angles",
  "line": "The horizon and the meridian",
  "unlock": "key9",
  "end": "The horizon and meridian will be here when you return.",
  "questions": [
    {
      "ask": "Where is the Ascendant on a conventional chart?",
      "glyph": -1,
      "planet": -1,
      "choices": [
        "The eastern horizon, on the left.",
        "The western horizon, on the right.",
        "The upper meridian, at the top.",
        "The lower meridian, at the bottom."
      ],
      "answer": 0,
      "why": "AC is the sign rising on the eastern horizon. Charts place east on the left."
    },
    {
      "ask": "Which label belongs to the western horizon?",
      "glyph": -1,
      "planet": -1,
      "choices": [
        "DC",
        "AC",
        "MC",
        "IC"
      ],
      "answer": 0,
      "why": "The Descendant is setting at the western horizon, on the right of the chart."
    },
    {
      "ask": "What is the Midheaven?",
      "glyph": -1,
      "planet": -1,
      "choices": [
        "Where the ecliptic crosses the upper meridian.",
        "The Sun's position.",
        "The eastern horizon.",
        "The whole tenth sign in every chart."
      ],
      "answer": 0,
      "why": "The MC comes from the local meridian. It is calculated separately from the Sun and from whole-sign house assignment."
    },
    {
      "ask": "Which label belongs at the lower meridian?",
      "glyph": -1,
      "planet": -1,
      "choices": [
        "IC",
        "MC",
        "AC",
        "DC"
      ],
      "answer": 0,
      "why": "IC, the Imum Coeli, is at the lower meridian, drawn at the bottom."
    },
    {
      "ask": "Do the angles and Whole Sign Houses come from the same assignment?",
      "glyph": -1,
      "planet": -1,
      "choices": [
        "Angles come from the local sky; Whole Sign Houses assign signs.",
        "Every angle is a whole house.",
        "The first house is always Aries.",
        "The MC is always the tenth whole sign."
      ],
      "answer": 0,
      "why": "The rising sign begins the Whole Sign House count. The angles belong to the local horizon and meridian."
    },
    {
      "ask": "What does the Ascendant degree tell you in Whole Sign Houses?",
      "glyph": -1,
      "planet": -1,
      "choices": [
        "It lies somewhere inside the first house.",
        "It always begins Aries.",
        "It divides every sign into two houses.",
        "It is always the Sun's degree."
      ],
      "answer": 0,
      "why": "The entire sign containing the Ascendant is the first whole-sign house."
    }
  ]
}
{
  "id": "houses",
  "name": "The Houses",
  "line": "Twelve places in the chart",
  "unlock": "key10",
  "end": "The twelve places are set. You can visit them again whenever you like.",
  "questions": [
    {
      "ask": "With Cancer rising, which sign is the first whole-sign house?",
      "glyph": -1,
      "planet": -1,
      "choices": [
        "Cancer",
        "Aries",
        "Capricorn",
        "Libra"
      ],
      "answer": 0,
      "why": "The sign containing the Ascendant is the first house. The first house changes with the rising sign."
    },
    {
      "ask": "Are houses and signs the same thing?",
      "glyph": -1,
      "planet": -1,
      "choices": [
        "House is where; sign is how.",
        "Every house is named after one fixed sign.",
        "The seventh house is always Libra.",
        "The first house is always Aries."
      ],
      "answer": 0,
      "why": "Whole Sign Houses assign the signs to places counted from the rising sign. A house and a sign make different claims."
    },
    {
      "ask": "Which short topic belongs to the eighth house?",
      "glyph": -1,
      "planet": -1,
      "choices": [
        "Shared resources",
        "Only death",
        "Friends",
        "Career"
      ],
      "answer": 0,
      "why": "The eighth house covers shared resources, debts, endings and inheritance. Its topic includes more than death."
    },
    {
      "ask": "Which short topic belongs to the twelfth house?",
      "glyph": -1,
      "planet": -1,
      "choices": [
        "Retreat",
        "Bad things",
        "Possessions",
        "Partners"
      ],
      "answer": 0,
      "why": "The twelfth house covers retreat, solitude and what is hidden. These are topics of the twelfth house."
    },
    {
      "ask": "Can the MC fall outside the tenth whole-sign house?",
      "glyph": -1,
      "planet": -1,
      "choices": [
        "Yes. Its meridian position is calculated separately.",
        "No. It is defined as the tenth whole sign.",
        "Only if the Sun changes signs.",
        "Only if every house is less than thirty degrees."
      ],
      "answer": 0,
      "why": "The unnamed London example puts the computed MC in the ninth whole-sign house. The ninth, tenth and eleventh are examples of whole-sign houses the MC can occupy."
    },
    {
      "ask": "Which set contains the angular houses?",
      "glyph": -1,
      "planet": -1,
      "choices": [
        "1, 4, 7, 10",
        "2, 5, 8, 11",
        "3, 6, 9, 12",
        "1, 2, 3, 4"
      ],
      "answer": 0,
      "why": "The angular houses are 1, 4, 7 and 10. Succedent houses are 2, 5, 8 and 11; cadent houses are 3, 6, 9 and 12."
    }
  ]
}
