using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Arixon.Editor
{
    public static class SetupLobbyEnvironment
    {
        [InitializeOnLoadMethod]
        public static void AutoRun()
        {
            if (!EditorPrefs.GetBool("ARIXON_LobbyFloorCreated_07", false))
            {
                EditorApplication.delayCall += () =>
                {
                    CreateLobbyFloor();
                    EditorPrefs.SetBool("ARIXON_LobbyFloorCreated_07", true);
                };
            }
        }

        [MenuItem("ARİXON/Kurulum/Adım 06: Lobi Zemini Oluştur")]
        public static void CreateLobbyFloor()
        {
            Scene activeScene = EditorSceneManager.GetActiveScene();
            
            // Eğer MainMenu sahnesinde değilsek veya sahnede NetworkManager yoksa, uyar.
            var nm = Object.FindFirstObjectByType<Unity.Netcode.NetworkManager>();
            if (nm == null)
            {
                Debug.LogWarning("ARİXON: Sahnede NetworkManager bulunamadı. Lobi zemini eklenmedi.");
                return;
            }

            // Zaten var mı kontrol et
            GameObject floor = GameObject.Find("LobbyFloor_ARIXON");
            if (floor == null)
            {
                // Yeni bir Plane yarat
                floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
                floor.name = "LobbyFloor_ARIXON";
                
                // Konumlandır (Kameranın ve varsayılan doğma noktasının altına)
                floor.transform.position = new Vector3(0, 0, 0);
                floor.transform.localScale = new Vector3(10, 1, 10); // 100x100 genişliğinde devasa bir zemin
            }

            // Yeşillik veya koyu bir zemin rengi verelim
            Renderer rend = floor.GetComponent<Renderer>();
            if (rend != null)
            {
                string matPath = "Assets/Materials/LobbyFloorMat.mat";
                
                // Klasör yoksa oluştur
                if (!AssetDatabase.IsValidFolder("Assets/Materials"))
                {
                    AssetDatabase.CreateFolder("Assets", "Materials");
                }

                Material floorMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                
                if (floorMat == null)
                {
                    Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                    if (shader == null)
                    {
                        shader = Shader.Find("Standard");
                    }
                    
                    floorMat = new Material(shader);
                    floorMat.color = new Color(0.2f, 0.2f, 0.25f); // Koyu lacivert/gri tonunda modern bir zemin
                    AssetDatabase.CreateAsset(floorMat, matPath);
                }
                else
                {
                    // Şader kopmuşsa düzelt
                    if (floorMat.shader.name == "Hidden/InternalErrorShader" || floorMat.shader.name == "Standard")
                    {
                        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                        if (shader != null) floorMat.shader = shader;
                    }
                }
                
                rend.sharedMaterial = floorMat;
            }

            // Çarpışmaları (Karakterin düşmesini) engelleyecek Collider zaten Plane'de var (MeshCollider).
            
            // Ayrıca çevreye şık bir Işık ekleyelim (Eğer yoksa)
            if (Object.FindFirstObjectByType<Light>() == null)
            {
                GameObject dirLight = new GameObject("Directional Light_Lobby");
                Light lightComp = dirLight.AddComponent<Light>();
                lightComp.type = LightType.Directional;
                lightComp.color = Color.white;
                lightComp.intensity = 1.0f;
                dirLight.transform.rotation = Quaternion.Euler(50, -30, 0);
            }

            // Sahneyi kaydet
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveOpenScenes();

            Debug.Log("ARİXON: Lobi Zemini (Floor) başarıyla eklendi! Artık karakterler boşluğa düşmeyecek.");
        }
    }
}
