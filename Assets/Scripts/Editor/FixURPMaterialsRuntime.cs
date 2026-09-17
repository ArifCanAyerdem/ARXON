using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Arixon.Editor
{
    public static class FixURPMaterialsRuntime
    {
        [InitializeOnLoadMethod]
        public static void AutoRun()
        {
            if (!EditorPrefs.GetBool("ARIXON_MagentaFixed_03", false))
            {
                EditorApplication.delayCall += () =>
                {
                    RunFix();
                    EditorPrefs.SetBool("ARIXON_MagentaFixed_03", true);
                };
            }
        }

        [MenuItem("ARİXON/Kurulum/Adım 06: Magenta (Mor) Sorununu Çöz")]
        public static void RunFix()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            {
                AssetDatabase.CreateFolder("Assets", "Materials");
            }

            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpShader == null)
            {
                Debug.LogError("ARİXON: URP Shader bulunamadı! Lütfen URP'nin kurulu olduğundan emin olun.");
                return;
            }

            // 1. Player Prefab Materyallerini Düzelt
            FixPlayerMaterials(urpShader);

            // 2. GameScene Zemin Materyalini Düzelt
            FixGameSceneMaterials(urpShader);

            // 3. MainMenu Zemin Materyalini Düzelt
            FixMainMenuMaterials(urpShader);

            AssetDatabase.SaveAssets();
            Debug.Log("[Editor] [FixURPMaterialsRuntime] -> Tüm Mor/Magenta URP Materyal Sorunları Giderildi!");
        }

        private static void FixMainMenuMaterials(Shader urpShader)
        {
            Scene currentScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            if (currentScene.name == "MainMenu")
            {
                GameObject ground = GameObject.Find("LobbyFloor_ARIXON");
                if (ground != null)
                {
                    Material lobbyMat = GetOrCreateMaterial("Assets/Materials/LobbyFloorMat.mat", urpShader, new Color(0.2f, 0.2f, 0.25f));
                    Renderer rend = ground.GetComponent<Renderer>();
                    if (rend != null)
                    {
                        rend.sharedMaterial = lobbyMat;
                        EditorUtility.SetDirty(rend);
                        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(currentScene);
                        Debug.Log("[Editor] [FixURPMaterialsRuntime] -> MainMenu'deki LobbyFloor_ARIXON URP materyali atandı.");
                    }
                }
            }
            else
            {
                string menuScenePath = "Assets/Scenes/MainMenu.unity";
                if (System.IO.File.Exists(menuScenePath))
                {
                    string oldScenePath = currentScene.path;
                    UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
                    Scene menuScene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(menuScenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);
                    
                    GameObject ground = GameObject.Find("LobbyFloor_ARIXON");
                    if (ground != null)
                    {
                        Material lobbyMat = GetOrCreateMaterial("Assets/Materials/LobbyFloorMat.mat", urpShader, new Color(0.2f, 0.2f, 0.25f));
                        Renderer rend = ground.GetComponent<Renderer>();
                        if (rend != null)
                        {
                            rend.sharedMaterial = lobbyMat;
                            EditorUtility.SetDirty(rend);
                            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(menuScene);
                            Debug.Log("[Editor] [FixURPMaterialsRuntime] -> MainMenu'deki LobbyFloor_ARIXON URP materyali atandı.");
                        }
                    }
                    
                    if (!string.IsNullOrEmpty(oldScenePath))
                    {
                        UnityEditor.SceneManagement.EditorSceneManager.OpenScene(oldScenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);
                    }
                }
            }
        }

        private static void FixPlayerMaterials(Shader urpShader)
        {
            string prefabPath = "Assets/Prefabs/PlayerPrefab.prefab";
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            
            if (playerPrefab != null)
            {
                // Vücut Materyali Oluştur
                Material bodyMat = GetOrCreateMaterial("Assets/Materials/PlayerBodyMat.mat", urpShader, Color.white);
                
                // Yüz Materyali Oluştur
                Material faceMat = GetOrCreateMaterial("Assets/Materials/PlayerFaceMat.mat", urpShader, Color.black);

                // Prefab içindeki Renderer'ları bul ve ata
                Renderer[] renderers = playerPrefab.GetComponentsInChildren<Renderer>(true);
                foreach (Renderer rend in renderers)
                {
                    if (rend.gameObject.name.Contains("Face"))
                    {
                        rend.sharedMaterial = faceMat;
                    }
                    else
                    {
                        rend.sharedMaterial = bodyMat;
                    }
                    EditorUtility.SetDirty(rend);
                }
                
                EditorUtility.SetDirty(playerPrefab);
                Debug.Log("[Editor] [FixURPMaterialsRuntime] -> PlayerPrefab URP materyalleri atandı.");
            }
        }

        private static void FixGameSceneMaterials(Shader urpShader)
        {
            // Eğer GameScene açıksa oradaki zemini de düzelt
            Scene currentScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            if (currentScene.name == "GameScene")
            {
                GameObject ground = GameObject.Find("ArenaGround");
                if (ground != null)
                {
                    Material arenaMat = GetOrCreateMaterial("Assets/Materials/ArenaGroundMat.mat", urpShader, new Color(0.2f, 0.6f, 0.3f));
                    Renderer rend = ground.GetComponent<Renderer>();
                    if (rend != null)
                    {
                        rend.sharedMaterial = arenaMat;
                        EditorUtility.SetDirty(rend);
                        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(currentScene);
                        Debug.Log("[Editor] [FixURPMaterialsRuntime] -> GameScene'deki ArenaGround URP materyali atandı.");
                    }
                }
            }
            else
            {
                // GameScene açık değilse açıp düzeltelim ve geri dönelim
                string gameScenePath = "Assets/Scenes/GameScene.unity";
                if (System.IO.File.Exists(gameScenePath))
                {
                    string oldScenePath = currentScene.path;
                    
                    // Açmadan önce mevcudu kaydet
                    UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
                    
                    // GameScene'i aç
                    Scene gameScene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(gameScenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);
                    
                    GameObject ground = GameObject.Find("ArenaGround");
                    if (ground != null)
                    {
                        Material arenaMat = GetOrCreateMaterial("Assets/Materials/ArenaGroundMat.mat", urpShader, new Color(0.2f, 0.6f, 0.3f));
                        Renderer rend = ground.GetComponent<Renderer>();
                        if (rend != null)
                        {
                            rend.sharedMaterial = arenaMat;
                            EditorUtility.SetDirty(rend);
                            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(gameScene);
                            Debug.Log("[Editor] [FixURPMaterialsRuntime] -> GameScene'deki ArenaGround URP materyali atandı.");
                        }
                    }
                    
                    // Geri dön
                    if (!string.IsNullOrEmpty(oldScenePath))
                    {
                        UnityEditor.SceneManagement.EditorSceneManager.OpenScene(oldScenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);
                    }
                }
            }
        }

        private static Material GetOrCreateMaterial(string path, Shader shader, Color color)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                mat.color = color;
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.shader = shader;
                mat.color = color;
                EditorUtility.SetDirty(mat);
            }
            return mat;
        }
    }
}
