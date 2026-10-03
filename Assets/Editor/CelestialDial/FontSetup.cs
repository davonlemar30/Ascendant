using UnityEditor;

namespace Ascendant.Build
{
    // Batch 2 (the Big Three, owner Oct 2 evening). The zodiac font bakes its characters into a texture (a custom set: the variable
    // font drew blank on the Web, Sept 12), so a character it draws must be in the set. The Keeper's record adds the moon's crescent,
    // ☽ (U+263D), which the font carries. Run once after changing the set (batch: -executeMethod Ascendant.Build.FontSetup.Apply).
    public static class FontSetup
    {
        public const string SymbolFont = "Assets/CelestialDial/Resources/Fonts/NotoSansSymbols.ttf", SymbolSet = "♈♉♊♋♌♍♎♏♐♑♒♓☽";
        [MenuItem("Ascendant/Greybox/Bake the symbol font's characters")]
        public static void Apply()
        {
            var importer = (TrueTypeFontImporter)AssetImporter.GetAtPath(SymbolFont);
            if (importer.fontTextureCase == FontTextureCase.CustomSet && importer.customCharacters == SymbolSet) return;
            importer.fontTextureCase = FontTextureCase.CustomSet; importer.customCharacters = SymbolSet; importer.SaveAndReimport();
        }
    }
}
