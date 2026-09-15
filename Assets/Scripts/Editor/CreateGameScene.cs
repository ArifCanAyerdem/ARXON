using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

public static class GameSceneCreator
{
    [MenuItem("ARIXON/Create Game Scene")]
    public static void CreateAndSetupGameScene()
    {
        string scenePath = "Assets/Scenes/GameScene.unity";
        
        // Eer sahne zaten varsa sil/yeniden olutur
        if (System.IO.File.Exists(scenePath))
        {
            Debug.Log("GameScene zaten var, build settings guncelleniyor...");
        }
        else
        {
            // Yeni sahne olutur
            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            
            // Sahneye basit bir Zemin (Arena Temsili) ekle
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "ArenaGround";
            ground.transform.localScale = new Vector3(3, 1, 5); // 30x50m arena
            ground.GetComponent<Renderer>().sharedMaterial.color = new Color(0.2f, 0.6f, 0.3f); // Yeil Zemin
            
            // Sahneye bir Gne ve Ik ayar (Varsaylan)
            // Kamera ayar (Ku bak izometrik)
            GameObject camObj = GameObject.Find("Main Camera");
            if (camObj != null)
            {
                camObj.transform.position = new Vector3(0, 15, -15);
                camObj.transform.rotation = Quaternion.Euler(45, 0, 0);
            }

            // Sahneyi kaydet
            EditorSceneManager.SaveScene(newScene, scenePath);
            Debug.Log("GameScene baaryla oluturuldu ve kaydedildi: " + scenePath);
        }

        // Build Settings'e ekle
        var originalScenes = EditorBuildSettings.scenes;
        var newScenes = new List<EditorBuildSettingsScene>();
        
        bool hasMainMenu = false;
        bool hasGameScene = false;

        foreach (var s in originalScenes)
        {
            if (s.path.Contains("MainMenu")) hasMainMenu = true;
            if (s.path.Contains("GameScene")) hasGameScene = true;
        }

        // nce MainMenu
        if (!hasMainMenu) newScenes.Add(new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true));
        // Mevcut sahneleri koru (GameScene hari, onu sona ekleyeceiz ya da yerine gre)
        foreach (var s in originalScenes)
        {
            if (!s.path.Contains("GameScene") && (!s.path.Contains("MainMenu") || hasMainMenu))
            {
                newScenes.Add(s);
            }
        }
        // Sonra GameScene
        if (!hasGameScene || true) // Her halukarda ekle
        {
            newScenes.Add(new EditorBuildSettingsScene(scenePath, true));
        }

        // Listeyi tekilletirip uygula
        EditorBuildSettings.scenes = newScenes.GroupBy(x => x.path).Select(y => y.First()).ToArray();
        
        // Eer u anki sahne MainMenu deilse MainMenu'ye dn (Oyunu test edebilmesi iin)
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);
        
        Debug.Log("GameScene, Build Settings'e eklendi ve MainMenu sahnesine geri dnld.");
    }
}
