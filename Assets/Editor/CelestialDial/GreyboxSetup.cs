using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Ascendant.CelestialDial;

namespace Ascendant.Build
{
    public static class GreyboxSetup
    {
        [MenuItem("Ascendant/Greybox/Create scene and configure Web template")]
        public static void Configure()
        {
            const string path="Assets/Scenes/CelestialDial.unity";
            if (!System.IO.File.Exists(path))
            {
                var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                var camera=new GameObject("Main Camera",typeof(Camera));camera.tag="MainCamera";
                camera.GetComponent<Camera>().clearFlags=CameraClearFlags.SolidColor;
                camera.GetComponent<Camera>().backgroundColor=new Color(.075f,.075f,.09f);
                new GameObject("CelestialDial",typeof(DialView));
                EditorSceneManager.SaveScene(scene,path);
            }
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(path,true)};
            PlayerSettings.WebGL.template="PROJECT:CelestialDial";
            AssetDatabase.SaveAssets();
            Debug.Log("[GreyboxSetup] Scene and Web template configured through Unity.");
        }
    }
}
