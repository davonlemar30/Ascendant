using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace Ascendant.CelestialDial
{
    // Batch 2 (owner, Oct 2 evening: the Big Three approved as proposed, unknowns A; task 86bcbn6w6, comment 90140263824229). The opening's
    // birth chart: the sun, the moon and the rising sign, worked out once from the birth date, time and place, then fixed for the
    // playthrough. The astronomy is Meeus (Astronomical Algorithms, 2nd ed.): the sun's apparent longitude (chapters 25 and 32, VSOP87 truncated, about an arc-second),
    // the moon's (chapter 47, the full table, about 0.003°), sidereal time (chapter 12) and the ascendant; the mechanical checks hold it
    // to Meeus's worked examples, to JPL Horizons and to a published chart. Pure C#: no network, no library.
    public static class Sky
    {
        const double Rad = Math.PI / 180;
        static double Norm(double x) { x %= 360; return x < 0 ? x + 360 : x; }
        public static int SignOf(double longitude) => (int)Math.Floor(Norm(longitude) / 30) % 12; // the tropical zodiac: 0 Aries ... 11 Pisces
        // Meeus 7.1 (Gregorian), at a UT hour of the day
        public static double JulianDay(int year, int month, int day, double hours)
        {
            if (month <= 2) { year--; month += 12; }
            int a = year / 100, b = 2 - a + a / 4;
            return Math.Floor(365.25 * (year + 4716)) + Math.Floor(30.6001 * (month + 1)) + day + hours / 24 + b - 1524.5;
        }
        // TT - UT in seconds: Espenak and Meeus's polynomials (NASA), 1900 to 2050; a few seconds of it move the moon by an arc-second
        public static double DeltaT(double year)
        {
            double t;
            if (year < 1920) { t = year - 1900; return -2.79 + 1.494119 * t - 0.0598939 * t * t + 0.0061966 * t * t * t - 0.000197 * t * t * t * t; }
            if (year < 1941) { t = year - 1920; return 21.20 + 0.84493 * t - 0.076100 * t * t + 0.0020936 * t * t * t; }
            if (year < 1961) { t = year - 1950; return 29.07 + 0.407 * t - t * t / 233 + t * t * t / 2547; }
            if (year < 1986) { t = year - 1975; return 45.45 + 1.067 * t - t * t / 260 - t * t * t / 718; }
            if (year < 2005) { t = year - 2000; return 63.86 + 0.3345 * t - 0.060374 * t * t + 0.0017275 * t * t * t + 0.000651814 * t * t * t * t + 0.00002373599 * t * t * t * t * t; }
            t = year - 2000; return 62.92 + 0.32217 * t + 0.005589 * t * t;
        }
        // the nutation in longitude and the true obliquity, from the main terms (Meeus 22)
        static void Nutation(double T, out double dpsi, out double eps)
        {
            double om = (125.04452 - 1934.136261 * T) * Rad, L = (280.4665 + 36000.7698 * T) * Rad, Lp = (218.3165 + 481267.8813 * T) * Rad;
            dpsi = (-17.20 * Math.Sin(om) - 1.32 * Math.Sin(2 * L) - 0.23 * Math.Sin(2 * Lp) + 0.21 * Math.Sin(2 * om)) / 3600;
            double deps = (9.20 * Math.Cos(om) + 0.57 * Math.Cos(2 * L) + 0.10 * Math.Cos(2 * Lp) - 0.09 * Math.Cos(2 * om)) / 3600;
            eps = 23.4392911 - 0.0130042 * T - 1.64e-7 * T * T + 5.04e-7 * T * T * T + deps;
        }
        // the sun's apparent longitude at a Julian Ephemeris Day, with higher accuracy (Meeus 25 and 32, Appendix III: VSOP87 for the Earth,
        // truncated): about an arc-second, so the minute the sun enters a sign comes out right (the cusp question shows it)
        static readonly double[,] L0 = { {175347046,0,0}, {3341656,4.6692568,6283.07585}, {34894,4.6261,12566.1517}, {3497,2.7441,5753.3849}, {3418,2.8289,3.5231}, {3136,3.6277,77713.7715}, {2676,4.4181,7860.4194}, {2343,6.1352,3930.2097}, {1324,0.7425,11506.7698}, {1273,2.0371,529.691}, {1199,1.1096,1577.3435}, {990,5.233,5884.927}, {902,2.045,26.298}, {857,3.508,398.149}, {780,1.179,5223.694}, {753,2.533,5507.553}, {505,4.583,18849.228}, {492,4.205,775.523}, {357,2.92,0.067}, {317,5.849,11790.629}, {284,1.899,796.298}, {271,0.315,10977.079}, {243,0.345,5486.778}, {206,4.806,2544.314}, {205,1.869,5573.143}, {202,2.458,6069.777}, {156,0.833,213.299}, {132,3.411,2942.463}, {126,1.083,20.775}, {115,0.645,0.98}, {103,0.636,4694.003}, {102,0.976,15720.839}, {102,4.267,7.114}, {99,6.21,2146.17}, {98,0.68,155.42}, {86,5.98,161000.69}, {85,1.3,6275.96}, {85,3.67,71430.7}, {80,1.81,17260.15}, {79,3.04,12036.46}, {75,1.76,5088.63}, {74,3.5,3154.69}, {74,4.68,801.82}, {70,0.83,9437.76}, {62,3.98,8827.39}, {61,1.82,7084.9}, {57,2.78,6286.6}, {56,4.39,14143.5}, {56,3.47,6279.55}, {52,0.19,12139.55}, {52,1.33,1748.02}, {51,0.28,5856.48}, {49,0.49,1194.45}, {41,5.37,8429.24}, {41,2.4,19651.05}, {39,6.17,10447.39}, {37,6.04,10213.29}, {37,2.57,1059.38}, {36,1.71,2352.87}, {36,1.78,6812.77}, {33,0.59,17789.85}, {30,0.44,83996.85}, {30,2.74,1349.87}, {25,3.16,4690.48} };
        static readonly double[,] L1 = { {628331966747,0,0}, {206059,2.678235,6283.07585}, {4303,2.6351,12566.1517}, {425,1.59,3.523}, {119,5.796,26.298}, {109,2.966,1577.344}, {93,2.59,18849.23}, {72,1.14,529.69}, {68,1.87,398.15}, {67,4.41,5507.55}, {59,2.89,5223.69}, {56,2.17,155.42}, {45,0.4,796.3}, {36,0.47,775.52}, {29,2.65,7.11}, {21,5.34,0.98}, {19,1.85,5486.78}, {19,4.97,213.3}, {17,2.99,6275.96}, {16,0.03,2544.31}, {16,1.43,2146.17}, {15,1.21,10977.08}, {12,2.83,1748.02}, {12,3.26,5088.63}, {12,5.27,1194.45}, {12,2.08,4694.0}, {11,0.77,553.57}, {10,1.3,6286.6}, {10,4.24,1349.87}, {9,2.7,242.73}, {9,5.64,951.72}, {8,5.3,2352.87}, {6,2.65,9437.76}, {6,4.67,4690.48} };
        static readonly double[,] L2 = { {52919,0,0}, {8720,1.0721,6283.0758}, {309,0.867,12566.152}, {27,0.05,3.52}, {16,5.19,26.3}, {16,3.68,155.42}, {10,0.76,18849.23}, {9,2.06,77713.77}, {7,0.83,775.52}, {5,4.66,1577.34}, {4,1.03,7.11}, {4,3.44,5573.14}, {3,5.14,796.3}, {3,6.05,5507.55}, {3,1.19,242.73}, {3,6.12,529.69}, {3,0.31,398.15}, {3,2.28,553.57}, {2,4.38,5223.69}, {2,3.75,0.98} };
        static readonly double[,] L3 = { {289,5.844,6283.076}, {35,0,0}, {17,5.49,12566.15}, {3,5.2,155.42}, {1,4.72,3.52}, {1,5.3,18849.23}, {1,5.97,242.73} };
        static readonly double[,] L4 = { {114,3.142,0}, {8,4.13,6283.08}, {1,3.84,12566.15} };
        static readonly double[,] L5 = { {1,3.14,0} };
        static readonly double[,] B0 = { {280,3.199,84334.662}, {102,5.422,5507.553}, {80,3.88,5223.69}, {44,3.7,2352.87}, {32,4.0,1577.34} };
        static readonly double[,] B1 = { {9,3.9,5507.55}, {6,1.73,5223.69} };
        static readonly double[,] R0 = { {100013989,0,0}, {1670700,3.0984635,6283.07585}, {13956,3.05525,12566.1517}, {3084,5.1985,77713.7715}, {1628,1.1739,5753.3849}, {1576,2.8469,7860.4194}, {925,5.453,11506.77}, {542,4.564,3930.21}, {472,3.661,5884.927}, {346,0.964,5507.553}, {329,5.9,5223.694}, {307,0.299,5573.143}, {243,4.273,11790.629}, {212,5.847,1577.344}, {186,5.022,10977.079}, {175,3.012,18849.228}, {110,5.055,5486.778}, {98,0.89,6069.78}, {86,5.69,15720.84}, {86,1.27,161000.69}, {65,0.27,17260.15}, {63,0.92,529.69}, {57,2.01,83996.85}, {56,5.24,71430.7}, {49,3.25,2544.31}, {47,2.58,775.52}, {45,5.54,9437.76}, {43,6.01,6275.96}, {39,5.36,4694.0}, {38,2.39,8827.39}, {37,0.83,19651.05}, {37,4.9,12139.55}, {36,1.67,12036.46}, {35,1.84,2942.46}, {33,0.24,7084.9}, {32,0.18,5088.63}, {32,1.78,398.15}, {28,1.21,6286.6}, {28,1.9,6279.55}, {26,4.59,10447.39} };
        static readonly double[,] R1 = { {103019,1.10749,6283.07585}, {1721,1.0644,12566.1517}, {702,3.142,0}, {32,1.02,18849.23}, {31,2.84,5507.55}, {25,1.32,5223.69}, {18,1.42,1577.34}, {10,5.91,10977.08}, {9,1.42,6275.96}, {9,0.27,5486.78} };
        static readonly double[,] R2 = { {4359,5.7846,6283.0758}, {124,5.579,12566.152}, {12,3.14,0}, {9,3.63,77713.77}, {6,1.87,5573.14}, {3,5.47,18849.23} };
        static readonly double[,] R3 = { {145,4.273,6283.076}, {7,3.92,12566.15} };
        static readonly double[,] R4 = { {4,2.56,6283.08} };
        static double Series(double[,] terms, double tau) { double sum = 0; for (int i = 0; i < terms.GetLength(0); i++) sum += terms[i, 0] * Math.Cos(terms[i, 1] + terms[i, 2] * tau); return sum; }
        public static double SunDistance(double jde) { double tau = (jde - 2451545.0) / 365250; return (Series(R0, tau) + Series(R1, tau) * tau + Series(R2, tau) * tau * tau + Series(R3, tau) * tau * tau * tau + Series(R4, tau) * tau * tau * tau * tau) / 1e8; }
        public static double SunLongitude(double jde)
        {
            double tau = (jde - 2451545.0) / 365250, T = tau * 10;
            double L = (Series(L0, tau) + Series(L1, tau) * tau + Series(L2, tau) * tau * tau + Series(L3, tau) * tau * tau * tau + Series(L4, tau) * tau * tau * tau * tau + Series(L5, tau) * tau * tau * tau * tau * tau) / 1e8;
            double B = (Series(B0, tau) + Series(B1, tau) * tau) / 1e8, lam = L / Rad + 180, beta = -B / Rad, lp = (lam - 1.397 * T - 0.00031 * T * T) * Rad;
            lam += -0.09033 / 3600 + 0.03916 / 3600 * (Math.Cos(lp) + Math.Sin(lp)) * Math.Tan(beta * Rad); // to the FK5 frame
            Nutation(T, out double dpsi, out _);
            return Norm(lam + dpsi - 20.4898 / 3600 / SunDistance(jde)); // nutation and aberration
        }
        // Meeus Table 47.A: the arguments D, M, M', F and the longitude's coefficient in millionths of a degree
        static readonly int[,] MoonTerms = {
            {0,0,1,0,6288774},{2,0,-1,0,1274027},{2,0,0,0,658314},{0,0,2,0,213618},{0,1,0,0,-185116},{0,0,0,2,-114332},{2,0,-2,0,58793},{2,-1,-1,0,57066},
            {2,0,1,0,53322},{2,-1,0,0,45758},{0,1,-1,0,-40923},{1,0,0,0,-34720},{0,1,1,0,-30383},{2,0,0,-2,15327},{0,0,1,2,-12528},{0,0,1,-2,10980},{4,0,-1,0,10675},
            {0,0,3,0,10034},{4,0,-2,0,8548},{2,1,-1,0,-7888},{2,1,0,0,-6766},{1,0,-1,0,-5163},{1,1,0,0,4987},{2,-1,1,0,4036},{2,0,2,0,3994},{4,0,0,0,3861},{2,0,-3,0,3665},
            {0,1,-2,0,-2689},{2,0,-1,2,-2602},{2,-1,-2,0,2390},{1,0,1,0,-2348},{2,-2,0,0,2236},{0,1,2,0,-2120},{0,2,0,0,-2069},{2,-2,-1,0,2048},{2,0,1,-2,-1773},{2,0,0,2,-1595},
            {4,-1,-1,0,1215},{0,0,2,2,-1110},{3,0,-1,0,-892},{2,1,1,0,-810},{4,-1,-2,0,759},{0,2,-1,0,-713},{2,2,-1,0,-700},{2,1,-2,0,691},{2,-1,0,-2,596},{4,0,1,0,549},
            {0,0,4,0,537},{4,-1,0,0,520},{1,0,-2,0,-487},{2,1,0,-2,-399},{0,0,2,-2,-381},{1,1,1,0,351},{3,0,-2,0,-340},{4,0,-3,0,330},{2,-1,2,0,327},{0,2,1,0,-323},
            {1,1,-1,0,299},{2,0,3,0,294} };
        // the sum of the periodic terms (Meeus 47's Σl, with its three additive terms), in millionths of a degree
        public static double MoonSum(double jde, out double meanLongitude)
        {
            double T = (jde - 2451545.0) / 36525;
            double Lp = 218.3164477 + 481267.88123421 * T - 0.0015786 * T * T + T * T * T / 538841 - T * T * T * T / 65194000;
            double D = 297.8501921 + 445267.1114034 * T - 0.0018819 * T * T + T * T * T / 545868 - T * T * T * T / 113065000;
            double M = 357.5291092 + 35999.0502909 * T - 0.0001536 * T * T + T * T * T / 24490000;
            double Mp = 134.9633964 + 477198.8675055 * T + 0.0087414 * T * T + T * T * T / 69699 - T * T * T * T / 14712000;
            double F = 93.2720950 + 483202.0175233 * T - 0.0036539 * T * T - T * T * T / 3526000 + T * T * T * T / 863310000;
            double A1 = 119.75 + 131.849 * T, A2 = 53.09 + 479264.290 * T, E = 1 - 0.002516 * T - 0.0000074 * T * T, sum = 0;
            for (int i = 0; i < MoonTerms.GetLength(0); i++)
            {
                int m = MoonTerms[i, 1]; double e = Math.Abs(m) == 1 ? E : Math.Abs(m) == 2 ? E * E : 1;
                sum += MoonTerms[i, 4] * e * Math.Sin((MoonTerms[i, 0] * D + m * M + MoonTerms[i, 2] * Mp + MoonTerms[i, 3] * F) * Rad);
            }
            sum += 3958 * Math.Sin(A1 * Rad) + 1962 * Math.Sin((Lp - F) * Rad) + 318 * Math.Sin(A2 * Rad);
            meanLongitude = Lp; return sum;
        }
        public static double MoonGeometricLongitude(double jde) { double sum = MoonSum(jde, out double Lp); return Norm(Lp + sum / 1e6); }
        public static double MoonLongitude(double jde) { Nutation((jde - 2451545.0) / 36525, out double dpsi, out _); return Norm(MoonGeometricLongitude(jde) + dpsi); }
        // Greenwich mean sidereal time at a UT Julian Day, in degrees (Meeus 12.4)
        public static double Gmst(double jdUt) { double T = (jdUt - 2451545.0) / 36525; return Norm(280.46061837 + 360.98564736629 * (jdUt - 2451545.0) + 0.000387933 * T * T - T * T * T / 38710000); }
        // the ascendant: the ecliptic's point rising on the eastern horizon, at a latitude and an east longitude (degrees)
        public static double Ascendant(double jdUt, double latitude, double longitude)
        {
            Nutation((jdUt - 2451545.0) / 36525, out _, out double eps); double ramc = Norm(Gmst(jdUt) + longitude) * Rad, e = eps * Rad, phi = latitude * Rad;
            return Norm(Math.Atan2(Math.Cos(ramc), -(Math.Sin(ramc) * Math.Cos(e) + Math.Tan(phi) * Math.Sin(e))) / Rad);
        }
        // a UT moment as a Julian Day and its Julian Ephemeris Day
        public static double Ephemeris(double jdUt, int year, int month) => jdUt + DeltaT(year + (month - .5) / 12) / 86400;
    }

    // A town or city from the bundled list (GeoNames, CC BY 4.0; Places/SOURCES.txt).
    public sealed class Place { public string Name, Key; public float Latitude, Longitude; public int Zone; }

    // The bundled places and their time zones: a birth's local time to UT, with the clock changes in force that year.
    public static class Places
    {
        static List<Place> all; static string[] zoneNames; static long[][] changeAt; static int[][] offsetAfter; static int[] offsetFrom;
        static void Load()
        {
            if (all != null) return;
            all = new List<Place>(); var zones = new List<string>(); var at = new List<long[]>(); var after = new List<int[]>(); var from = new List<int>();
            var zoneText = Resources.Load<TextAsset>("Places/zones"); var placeText = Resources.Load<TextAsset>("Places/places");
            if (zoneText != null) foreach (var line in zoneText.text.Split('\n'))
                {
                    if (line.Length == 0) continue; var f = line.Split('\t'); zones.Add(f[0]); from.Add(int.Parse(f[1], CultureInfo.InvariantCulture));
                    var changes = f.Length > 2 && f[2].Length > 0 ? f[2].Split(' ') : new string[0]; var t = new long[changes.Length]; var o = new int[changes.Length];
                    for (int i = 0; i < changes.Length; i++) { var p = changes[i].Split(':'); t[i] = long.Parse(p[0], CultureInfo.InvariantCulture); o[i] = int.Parse(p[1], CultureInfo.InvariantCulture); }
                    at.Add(t); after.Add(o);
                }
            if (placeText != null) foreach (var line in placeText.text.Split('\n'))
                {
                    if (line.Length == 0) continue; var f = line.Split('\t');
                    all.Add(new Place { Name = f[0], Key = f[1].Length > 0 ? f[1] : f[0].Split(',')[0].ToLowerInvariant(), Latitude = float.Parse(f[2], CultureInfo.InvariantCulture), Longitude = float.Parse(f[3], CultureInfo.InvariantCulture), Zone = int.Parse(f[4], CultureInfo.InvariantCulture) });
                }
            zoneNames = zones.ToArray(); changeAt = at.ToArray(); offsetAfter = after.ToArray(); offsetFrom = from.ToArray();
        }
        public static IReadOnlyList<Place> All { get { Load(); return all; } }
        public static int ZoneCount { get { Load(); return zoneNames.Length; } }
        public static string ZoneName(int zone) { Load(); return zone >= 0 && zone < zoneNames.Length ? zoneNames[zone] : ""; }
        public static int ZoneOf(string name) { Load(); return Array.IndexOf(zoneNames, name); }
        public static Place Find(string name) => All.FirstOrDefault(p => p.Name == name);
        // lower-case ASCII, accents dropped (Latin letters; the search keys were folded the same way when the list was made)
        public static string Fold(string text)
        {
            const string from = "àáâãäåāăąçćčďèéêëēęěìíîïīıñńňòóôõöøōőŕřśšşțťùúûüūůűýÿźżžßłđ", to = "aaaaaaaaacccdeeeeeeeiiiiiinnnoooooooorrsssttuuuuuuuyyzzzsld"; // aligned letter for letter
            var chars = (text ?? "").Trim().ToLowerInvariant().ToCharArray();
            for (int i = 0; i < chars.Length; i++) { int k = from.IndexOf(chars[i]); if (k >= 0) chars[i] = to[k]; }
            return new string(chars);
        }
        // up to max places whose name (or a word of it) starts with the words typed, the biggest first
        public static List<Place> Search(string text, int max = 4)
        {
            var q = Fold(text); var found = new List<Place>(); if (q.Length < 2) return found;
            foreach (var p in All) if (p.Key.StartsWith(q, StringComparison.Ordinal)) { found.Add(p); if (found.Count >= max) return found; }
            foreach (var p in All) if (!p.Key.StartsWith(q, StringComparison.Ordinal) && p.Key.Contains(" " + q)) { found.Add(p); if (found.Count >= max) break; }
            return found;
        }
        // the zone's UTC offset (seconds) at a UT moment, in minutes since 1900-01-01 00:00 UTC
        public static int OffsetAt(int zone, long minute)
        {
            Load(); var t = changeAt[zone]; int lo = 0, hi = t.Length - 1, last = -1;
            while (lo <= hi) { int mid = (lo + hi) / 2; if (t[mid] <= minute) { last = mid; lo = mid + 1; } else hi = mid - 1; }
            return last < 0 ? offsetFrom[zone] : offsetAfter[zone][last];
        }
        static readonly DateTime Epoch = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        // a local clock time at the place to UT (minutes since 1900): a time the clocks skipped takes the offset before the change, and an
        // hour the clocks repeated takes its first pass (Python's zoneinfo with fold 0, which wrote the table)
        public static long LocalToUt(int zone, int year, int month, int day, int minuteOfDay)
        {
            long local = (long)(new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc) - Epoch).TotalMinutes + minuteOfDay; long best = long.MaxValue;
            foreach (var probe in new[] { local - 26 * 60, local + 26 * 60, local })
            {
                int offset = OffsetAt(zone, probe); long ut = local - (long)Math.Floor(offset / 60.0);
                if (OffsetAt(zone, ut) == offset && ut < best) best = ut; // a consistent reading; the earliest is the first pass
            }
            if (best != long.MaxValue) return best;
            return local - (long)Math.Floor(OffsetAt(zone, local - 26 * 60) / 60.0); // skipped: the offset before the change
        }
        public static DateTime UtOf(long minute) => Epoch.AddMinutes(minute);
        public static long MinuteOf(int year, int month, int day) => (long)(new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc) - Epoch).TotalMinutes;
        // the westmost and eastmost UTC offsets anywhere in the bundled table, in minutes: a birth with no place could be in any of them
        static int westmost = int.MaxValue, eastmost = int.MinValue;
        public static void OffsetRange(out int west, out int east)
        {
            Load();
            if (westmost == int.MaxValue)
                for (int z = 0; z < zoneNames.Length; z++)
                {
                    westmost = Math.Min(westmost, offsetFrom[z]); eastmost = Math.Max(eastmost, offsetFrom[z]);
                    foreach (var o in offsetAfter[z]) { westmost = Math.Min(westmost, o); eastmost = Math.Max(eastmost, o); }
                }
            west = (int)Math.Floor(westmost / 60.0); east = (int)Math.Ceiling(eastmost / 60.0);
        }
    }

    // The birth-time build (owner, Oct 7: the recommendation on 86bceb6fq approved, comment 90140265369479). The save keeps three things
    // apart, so a later system can weigh them differently with no migration (owner, Oct 7): what the player told the game (BirthFacts), what
    // the player chose where the facts can't decide (ChoiceRecord), and what the game worked out from the facts (WorkedChart). The signs on
    // screen are read from them each time: worked out (exact or stable) beats chosen, and chosen beats unknown.
    [Serializable]
    public sealed class BirthFacts
    {
        public string date = "";                     // YYYY-MM-DD; "" not given
        public int timeFrom = -1, timeTo = -1;       // minutes after local midnight: equal for an exact time; -1 not given, so the whole local day
        public string place = "", zone = "";         // the bundled place's name and its IANA zone; "" not given
        public float latitude, longitude;
        public bool HasDate => date != null && date.Length == 10;
        public bool HasTime => timeFrom >= 0 && timeTo >= timeFrom;
        public bool ExactTime => HasTime && timeFrom == timeTo;
        public bool HasPlace => !string.IsNullOrEmpty(place);
        public bool Any => HasDate || HasTime || HasPlace;
        public int Year => HasDate ? int.Parse(date.Substring(0, 4), CultureInfo.InvariantCulture) : 0;
        public int Month => HasDate ? int.Parse(date.Substring(5, 2), CultureInfo.InvariantCulture) : 0;
        public int Day => HasDate ? int.Parse(date.Substring(8, 2), CultureInfo.InvariantCulture) : 0;
        public int Minute => ExactTime ? timeFrom : -1;
        public void SetDate(int year, int month, int day) { date = year.ToString("0000") + "-" + month.ToString("00") + "-" + day.ToString("00"); }
        public void SetTime(int minuteOfDay) { timeFrom = timeTo = minuteOfDay < 0 ? -1 : minuteOfDay; }
        public void SetPlace(Place p)
        {
            if (p == null) { place = zone = ""; latitude = longitude = 0; return; }
            place = p.Name; zone = Places.ZoneName(p.Zone); latitude = p.Latitude; longitude = p.Longitude;
        }
        public Place AsPlace => HasPlace ? new Place { Name = place, Key = "", Latitude = latitude, Longitude = longitude, Zone = Places.ZoneOf(zone ?? "") } : null;
        public BirthFacts Clone() => (BirthFacts)MemberwiseClone();
        public static BirthFacts From(int year, int month, int day, int minuteOfDay, Place place)
        {
            var f = new BirthFacts(); if (year > 0) f.SetDate(year, month, day); f.SetTime(minuteOfDay); f.SetPlace(place); return f;
        }
    }

    // A sign the player chose for a point the facts can't decide, and how: picked (a cusp day, the skip path, the rising), noon (the cusp's
    // "I'm not sure": the sign at the window's middle, flagged so a later screen can offer a fix), declined (the skip path's "I'm not sure": no
    // sign, but the player was asked); and from older saves, converted once: entered ("Enter what I already know"), assigned (the random sun
    // retired Oct 7), legacy (a sun-only save from before #110).
    [Serializable]
    public sealed class ChoiceRecord { public string point = ""; public int sign = -1; public string how = ""; }

    // A point the game worked out from the facts: every sign it could be in, its ecliptic longitude across the window, and how sure it is:
    // exact (an exact time and a place), stable (one sign across the whole window), uncertain (more than one).
    [Serializable]
    public sealed class ChartPoint { public string point = ""; public int[] signs = new int[0]; public float from, to; public string status = ""; public bool Settled => status == "exact" || status == "stable"; }

    // the record's three parts in one object, for the web state's test evidence
    [Serializable]
    public sealed class BirthRecordState { public BirthFacts birth; public ChoiceRecord[] choices; public WorkedChart chart; }

    // The worked-out chart, and the version of the maths that wrote it. Written at the opening and rewritten only when a fact is added.
    [Serializable]
    public sealed class WorkedChart
    {
        public int maths; public ChartPoint[] points = new ChartPoint[0];
        public ChartPoint Point(string name) => points == null ? null : points.FirstOrDefault(p => p.point == name);
    }

    // The Big Three, as the opening fixes it: a sign per body, or -1 when it can't be known.
    public sealed class BirthChart
    {
        public int Sun = -1, Moon = -1, Rising = -1;
        public int SunFrom = -1, SunTo = -1, SunNoon = -1; // with no birth time: the sun's sign at the day's start, end and local noon (start and end differ on a cusp day)
        public int MoonFrom = -1, MoonTo = -1; // with no birth time: the moon's sign at the day's start and end (they differ on a day it changed sign: owner, Oct 3, "Pisces or Aries")
        public long Ingress = -1; // on a cusp day, the UT minute (since 1900) the sun entered SunTo
        public int[] MoonSigns = new int[0]; // every sign the moon could be in across the window, in order (one when it is known)
        public WorkedChart Worked = new WorkedChart(); // the save's record of it (owner, Oct 7)
        public const int Maths = 1; // the version of the maths that writes a WorkedChart; a later fix bumps it and never moves a settled sign
        static double Jd(DateTime ut) => Sky.JulianDay(ut.Year, ut.Month, ut.Day, ut.Hour + ut.Minute / 60.0 + ut.Second / 3600.0);
        static double SunAt(long minute) { var ut = Places.UtOf(minute); return Sky.SunLongitude(Sky.Ephemeris(Jd(ut), ut.Year, ut.Month)); }
        static double MoonAt(long minute) { var ut = Places.UtOf(minute); return Sky.MoonLongitude(Sky.Ephemeris(Jd(ut), ut.Year, ut.Month)); }
        // The birth-time build (owner, Oct 7): the chart from the facts, whatever is known. The game never invents a time or a place: with one
        // missing it works across every UT minute the birth could have been, and keeps only what holds across all of them. No date, nothing
        // worked out. With no time, the window is the whole local day; with no place, that local time in every zone of the bundled table.
        // The sun and the moon only move one way, so the signs between the window's ends are every sign they could be in (sampled every hour
        // as #110 did, and both ends read); the rising sign is worked out only from an exact time and a place (owner, Oct 3: no invented angles).
        public static BirthChart Work(BirthFacts facts)
        {
            var chart = new BirthChart { Worked = new WorkedChart { maths = Maths } };
            if (facts == null || !facts.HasDate) return chart;
            int year = facts.Year, month = facts.Month, day = facts.Day, tFrom = facts.HasTime ? facts.timeFrom : 0, tTo = facts.HasTime ? facts.timeTo : 24 * 60 - 1;
            var place = facts.AsPlace; bool placed = place != null && place.Zone >= 0; long start, end;
            if (placed) { start = Places.LocalToUt(place.Zone, year, month, day, tFrom); end = Places.LocalToUt(place.Zone, year, month, day, tTo); }
            else { Places.OffsetRange(out int west, out int east); long local = Places.MinuteOf(year, month, day); start = local + tFrom - east; end = local + tTo - west; }
            var points = new List<ChartPoint>();
            ChartPoint Body(string name, Func<long, double> at, out int[] signs)
            {
                var seen = new List<int>();
                for (long minute = start; ; minute = Math.Min(minute + 60, end)) { int s = Sky.SignOf(at(minute)); if (seen.Count == 0 || seen[seen.Count - 1] != s) seen.Add(s); if (minute >= end) break; }
                signs = seen.ToArray();
                return new ChartPoint { point = name, signs = signs, from = (float)at(start), to = (float)at(end), status = start == end ? "exact" : signs.Length == 1 ? "stable" : "uncertain" };
            }
            var sun = Body("sun", SunAt, out int[] sunSigns); var moon = Body("moon", MoonAt, out int[] moonSigns); points.Add(sun); points.Add(moon);
            chart.Sun = sun.Settled ? sunSigns[0] : -1; chart.Moon = moon.Settled ? moonSigns[0] : -1; chart.MoonSigns = moonSigns;
            if (start != end) { chart.SunFrom = sunSigns[0]; chart.SunTo = sunSigns[sunSigns.Length - 1]; chart.MoonFrom = moonSigns[0]; chart.MoonTo = moonSigns[moonSigns.Length - 1]; } // a window: the signs at its two ends (#110, #116)
            if (placed && facts.ExactTime)
            {
                var ut = Places.UtOf(start); double asc = Sky.Ascendant(Jd(ut), place.Latitude, place.Longitude); chart.Rising = Sky.SignOf(asc);
                points.Add(new ChartPoint { point = "rising", signs = new[] { chart.Rising }, from = (float)asc, to = (float)asc, status = "exact" });
            }
            // "I'm not sure" on a cusp day takes the sign at the window's middle: local noon with a place (owner, Oct 2), the middle without one
            long middle = placed && !facts.HasTime ? Places.LocalToUt(place.Zone, year, month, day, 12 * 60) : start + (end - start) / 2; chart.SunNoon = Sky.SignOf(SunAt(middle));
            if (!sun.Settled) // the minute it changed: the sun moves one way, so the first minute in the new sign is found by halving
            {
                long lo = start, hi = end;
                while (hi - lo > 1) { long mid = (lo + hi) / 2; if (Sky.SignOf(SunAt(mid)) == chart.SunFrom) lo = mid; else hi = mid; }
                chart.Ingress = hi;
            }
            chart.Worked.points = points.ToArray(); return chart;
        }
        // the chart from the birth date, the local time (minutes after midnight, or -1 when unknown) and the place (#110's call, kept for its checks)
        public static BirthChart Work(int year, int month, int day, int minuteOfDay, Place place) => place == null ? new BirthChart() : Work(BirthFacts.From(year, month, day, minuteOfDay, place));
        // a UT minute as the local clock time at the zone (minutes after local midnight), for the cusp question
        public static int LocalMinuteOf(long ut, int zone) { long local = ut + (long)Math.Floor(Places.OffsetAt(zone, ut) / 60.0); return (int)(((local % 1440) + 1440) % 1440); }
        // DEV Mode's cusp-day sample (owner, Oct 2 evening): London, Apr 20 1990, no birth time; the sun entered Taurus at 9:27 am local time
        public static void CuspSample(out int year, out int month, out int day, out Place place)
        {
            year = 1990; month = 4; day = 20; place = Places.Find("London, Britain (UK)");
        }
        // DEV Mode's sample chart (owner, Oct 2 evening): a real chart worked out for a sample birth in the sign's middle, at noon in London
        public static readonly int[] SampleDays = { 95, 125, 156, 188, 219, 250, 281, 311, 341, 5, 35, 64 }; // 1990's day of the year with the sun nearest 15° into each sign, Aries first
        public static void Sample(int sun, out int year, out int month, out int day, out int minute, out Place place)
        {
            var date = new DateTime(1990, 1, 1).AddDays(SampleDays[Zodiac.Wrap(sun)] - 1); year = date.Year; month = date.Month; day = date.Day; minute = 12 * 60;
            place = Places.Find("London, Britain (UK)") ?? new Place { Name = "London, Britain (UK)", Key = "london", Latitude = 51.5085f, Longitude = -0.1257f, Zone = Places.ZoneOf("Europe/London") };
        }
    }
}
