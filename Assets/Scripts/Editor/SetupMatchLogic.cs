using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Arixon.Gameplay;
using Arixon.UI;

namespace Arixon.Editor
{
    public static class SetupMatchLogic
    {
        [InitializeOnLoadMethod]
        private static void AutoRun()
        {
            if (!EditorPrefs.GetBool("ARIXON_MatchHUD_Setup_Done", false))
            {
                EditorApplication.delayCall += () =>
                {
                    if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                    Setup();
                    EditorPrefs.SetBool("ARIXON_MatchHUD_Setup_Done", true);
                };
            }
        }

        [MenuItem("ARİXON/Kurulum/Adım 09: MatchManager ve GameHUD Ekle")]
        public static void Setup()
        {
            string scenePath = "Assets/Scenes/SampleScene.unity";
            if (!System.IO.File.Exists(scenePath))
            {
                Debug.LogError("SampleScene bulunamadı!");
                return;
            }

            Scene gameScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            // MatchManager Ekle
            GameObject matchObj = GameObject.Find("MatchManager");
            if (matchObj == null)
            {
                matchObj = new GameObject("MatchManager");
                matchObj.AddComponent<Unity.Netcode.NetworkObject>();
                matchObj.AddComponent<MatchManager>();
            }

            // GameHUD Ekle
            GameObject hudObj = GameObject.Find("GameHUD");
            if (hudObj == null)
            {
                hudObj = new GameObject("GameHUD");
                var uiDoc = hudObj.AddComponent<UIDocument>();
                
                var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/GameHUD.uxml");
                uiDoc.visualTreeAsset = visualTree;

                string[] psGuids = AssetDatabase.FindAssets("t:PanelSettings");
                if (psGuids.Length > 0)
                {
                    string psPath = AssetDatabase.GUIDToAssetPath(psGuids[0]);
                    var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(psPath);
                    if (panelSettings != null) uiDoc.panelSettings = panelSettings;
                }
                
                hudObj.AddComponent<GameUIController>();
            }

            EditorSceneManager.SaveScene(gameScene);
            Debug.Log("ARİXON OTONOM: MatchManager ve GameHUD başarıyla SampleScene'e eklendi!");

            // Lobiye dön
            string mainMenuPath = "Assets/Scenes/MainMenu.unity";
            if (System.IO.File.Exists(mainMenuPath))
            {
                EditorSceneManager.OpenScene(mainMenuPath, OpenSceneMode.Single);
            }
        }
    }
}
