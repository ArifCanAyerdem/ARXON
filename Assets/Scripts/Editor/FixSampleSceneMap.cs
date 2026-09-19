using UnityEngine;
using UnityEditor;

namespace Arixon.EditorScripts
{
    public class FixSampleSceneMap : EditorWindow
    {
        [MenuItem("Arixon/Apply SampleScene Fixes")]
        public static void Apply()
        {
            string scenePath = "Assets/Scenes/SampleScene.unity";
            if (UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path != scenePath)
            {
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
            }

            GameObject arena = GameObject.Find("ArenaGround");
            if (arena != null)
            {
                // Zaten büyütüldüyse tekrar büyütme ihtimaline karşı kontrol
                if (arena.transform.localScale.x < 1.1f)
                {
                    float scaleMultiplier = 1.35f;

                    // Ground'u büyüt
                    arena.transform.localScale = new Vector3(
                        arena.transform.localScale.x * scaleMultiplier, 
                        arena.transform.localScale.y, 
                        arena.transform.localScale.z * scaleMultiplier);

                    // Duvar ve kaleleri taşı/büyüt
                    string[] objsToAdjust = { "Wall_North", "Wall_South", "Wall_East", "Wall_West", "Goal_Red", "Goal_Blue" };
                    foreach (string objName in objsToAdjust)
                    {
                        GameObject obj = GameObject.Find(objName);
                        if (obj != null)
                        {
                            Vector3 pos = obj.transform.position;
                            pos.x *= scaleMultiplier;
                            pos.z *= scaleMultiplier;
                            obj.transform.position = pos;

                            if (obj.name.StartsWith("Wall"))
                            {
                                Vector3 scale = obj.transform.localScale;
                                if (obj.name.Contains("North") || obj.name.Contains("South"))
                                    scale.x *= scaleMultiplier;
                                else
                                    scale.z *= scaleMultiplier;
                                obj.transform.localScale = scale;
                            }
                        }
                    }
                    UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
                    Debug.Log("[Arixon] SampleScene (Asıl Harita) başarıyla büyütüldü!");
                }
            }

            // Test için kullanıcıyı tekrar MainMenu'ye döndür
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
            Debug.Log("[Arixon] Ana menü sahnesine dönüldü. Artık Play'e basabilirsiniz.");
        }
    }
}
