using UnityEditor;
using UnityEngine;

namespace Arixon.Editor
{
    [InitializeOnLoad]
    public static class ForceAutoSetup
    {
        static ForceAutoSetup()
        {
            EditorApplication.update += OnUpdate;
        }

        private static void OnUpdate()
        {
            bool needsSetup = !System.IO.File.Exists("Assets/Prefabs/GameBall.prefab") || !System.IO.File.Exists("Assets/Scenes/GameScene.unity");
            
            if (needsSetup)
            {
                // 1. Eğer Play Modundaysak, sahneyi kaydedemeyeceğimiz için Play modunu zorla durdur!
                if (EditorApplication.isPlaying)
                {
                    Debug.LogWarning("ARİXON: Kurulum yapmak için Play modu otomatik olarak durduruluyor...");
                    EditorApplication.isPlaying = false;
                    return; // Editör durana kadar bekle
                }
                
                // 2. Editör durduğunda işlemleri tam otomatik çalıştır
                try
                {
                    SetupGameBall.CreateAndAssignGameBall();
                    SetupGameArena.BuildArena();
                    
                    Debug.Log("ARİXON: Top ve Arena TAM OTOMATİK olarak kuruldu! Artık oyunu başlatabilirsiniz.");
                }
                catch (System.Exception e)
                {
                    Debug.LogError("ARİXON Kurulum Hatası: " + e.Message);
                }
                
                // Görev bittikten sonra Update döngüsünden çık
                EditorApplication.update -= OnUpdate;
            }
        }
    }
}
