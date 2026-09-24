using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Ascendant.CelestialDial;

namespace Ascendant.Build
{
    // Build E: import settings for the art and sound slots, applied to any file dropped into the slot folders, so the
    // phone build stays small (no mipmaps, crunched compression, a size cap per slot) and a PNG is a sprite without a click.
    public sealed class SlotImport : AssetPostprocessor
    {
        public const string ArtRoot = "Assets/CelestialDial/Resources/Art/", AudioRoot = "Assets/CelestialDial/Resources/Audio/";
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArtRoot)) return;
            var importer = (TextureImporter)assetImporter; string slot = Path.GetFileNameWithoutExtension(assetPath); var entry = Slots.Find(slot);
            if (entry == null) Debug.LogWarning("[ArtSlots] " + assetPath + " is not a slot name, so nothing draws it. Slots: " + string.Join(", ", Slots.Art.Select(a => a.Name)));
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false; importer.alphaIsTransparency = true; importer.sRGBTexture = true; importer.isReadable = false;
            importer.npotScale = TextureImporterNPOTScale.None; importer.wrapMode = TextureWrapMode.Clamp; importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = entry != null ? entry.MaxSize : 512;
            importer.textureCompression = TextureImporterCompression.Compressed; importer.crunchedCompression = !slot.EndsWith("-light"); importer.compressionQuality = 50; // the light overlays are not crunched: crunch crashes on them in the Linux CI Editor (main's run after PR #36) and bands their soft gradients
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; settings.spriteGenerateFallbackPhysicsShape = false; importer.SetTextureSettings(settings);
        }
        void OnPostprocessTexture(Texture2D texture)
        {
            if (!assetPath.StartsWith(ArtRoot)) return;
            if (texture.width % 4 != 0 || texture.height % 4 != 0) Debug.LogWarning("[ArtSlots] " + assetPath + " is " + texture.width + " x " + texture.height + "; sides that are multiples of 4 compress, others stay uncompressed and larger.");
        }
        void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(AudioRoot)) return;
            var importer = (AudioImporter)assetImporter; string slot = Path.GetFileNameWithoutExtension(assetPath); bool loop = slot == "ambient";
            if (Slots.FindSound(slot) == null) Debug.LogWarning("[ArtSlots] " + assetPath + " is not a sound slot, so nothing plays it. Slots: " + string.Join(", ", Slots.Sounds.Select(s => s.Name)));
            var settings = importer.defaultSampleSettings;
            settings.loadType = loop ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.Vorbis; settings.quality = .5f; settings.sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate; settings.preloadAudioData = true;
            importer.defaultSampleSettings = settings; importer.forceToMono = !loop; importer.loadInBackground = false;
        }
    }

    // In the Editor the URL is not there, so the menu chooses: play with the test set, or play the style page. Both stay on for the session.
    public static class SlotMenus
    {
        const string SetMenu = "Ascendant/Greybox/Play with the test art set", StyleMenu = "Ascendant/Greybox/Play the style page";
        [MenuItem(SetMenu)] static void ToggleSet() { bool on = SessionState.GetString("AscendantArtSet", "") == Slots.TestSet; SessionState.SetString("AscendantArtSet", on ? "" : Slots.TestSet); Menu.SetChecked(SetMenu, !on); Slots.Request(null, null); }
        [MenuItem(SetMenu, true)] static bool ValidateSet() { Menu.SetChecked(SetMenu, SessionState.GetString("AscendantArtSet", "") == Slots.TestSet); return true; }
        [MenuItem(StyleMenu)] static void ToggleStyle() { bool on = SessionState.GetBool("AscendantStylePage", false); SessionState.SetBool("AscendantStylePage", !on); Menu.SetChecked(StyleMenu, !on); Slots.Request(null, null); }
        [MenuItem(StyleMenu, true)] static bool ValidateStyle() { Menu.SetChecked(StyleMenu, SessionState.GetBool("AscendantStylePage", false)); return true; }
        public static void ClearForFixture() { SessionState.EraseString("AscendantArtSet"); SessionState.EraseBool("AscendantStylePage"); }
    }
}
