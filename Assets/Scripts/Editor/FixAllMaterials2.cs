using UnityEditor;
using UnityEngine;

namespace Arixon.Editor
{
    public static class FixAllMaterials2
    {
        [InitializeOnLoadMethod]
        public static void AutoRun()
        {
            if (!EditorPrefs.GetBool("ARIXON_MagentaFixed_01", false))
            {
                EditorApplication.delayCall += () =>
                {
                    RunFix();
                    EditorPrefs.SetBool("ARIXON_MagentaFixed_01", true);
                };
            }
        }

        public static void RunFix()
        {
            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpShader == null)
            {
                Debug.LogError("[Editor] [FixAllMaterials2.RunFix] -> URP Lit shader bulunamadı!");
                return;
            }

            // Zemin Materyalini Bul ve Düzelt
            Material floorMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/LobbyFloorMat.mat");
            if (floorMat != null)
            {
                floorMat.shader = urpShader;
                floorMat.color = new Color(0.2f, 0.2f, 0.25f);
                EditorUtility.SetDirty(floorMat);
                Debug.Log("[Editor] [FixAllMaterials2.RunFix] -> Zemin materyali URP olarak güncellendi.");
            }
            else 
            {
                Debug.LogError("[Editor] [FixAllMaterials2.RunFix] -> LobbyFloorMat.mat bulunamadı!");
            }

            // Sahnede var olan LobbyFloor objesine ata (garanti olsun diye)
            GameObject floorObj = GameObject.Find("LobbyFloor_ARIXON");
            if (floorObj != null && floorMat != null)
            {
                Renderer rend = floorObj.GetComponent<Renderer>();
                if (rend != null)
                {
                    rend.sharedMaterial = floorMat;
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(floorObj.scene);
                    Debug.Log("[Editor] [FixAllMaterials2.RunFix] -> Sahnede zemin materyali bağlandı!");
                }
            }

            // Player Prefab içindeki materyali düzelt
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PlayerPrefab.prefab");
            if (playerPrefab != null)
            {
                Renderer[] renderers = playerPrefab.GetComponentsInChildren<Renderer>(true);
                foreach (Renderer rend in renderers)
                {
                    if (rend.sharedMaterial != null)
                    {
                        rend.sharedMaterial.shader = urpShader;
                        EditorUtility.SetDirty(rend.sharedMaterial);
                        EditorUtility.SetDirty(playerPrefab);
                        Debug.Log($"[Editor] [FixAllMaterials2.RunFix] -> PlayerPrefab materyali ({rend.sharedMaterial.name}) URP olarak güncellendi.");
                    }
                }
            }
            
            AssetDatabase.SaveAssets();
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            Debug.Log("[Editor] [FixAllMaterials2.RunFix] -> Tüm mor materyal sorunları başarıyla çözüldü!");
        }
    }
}
