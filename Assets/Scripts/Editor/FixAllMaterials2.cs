using UnityEditor;
using UnityEngine;

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
        Shader urpShader = Shader.Find(""Universal Render Pipeline/Lit"");
        if (urpShader == null)
        {
            Debug.LogError(""ARIXON: URP Lit shader bulunamadi!"");
            return;
        }

        // Zemin Materyalini Bul ve Duzelt
        Material floorMat = AssetDatabase.LoadAssetAtPath<Material>(""Assets/Materials/LobbyFloorMat.mat"");
        if (floorMat != null)
        {
            floorMat.shader = urpShader;
            floorMat.color = new Color(0.2f, 0.2f, 0.25f);
            EditorUtility.SetDirty(floorMat);
            Debug.Log(""ARIXON: Zemin materyali URP olarak guncellendi."");
        }
        else 
        {
            Debug.LogError(""ARIXON: LobbyFloorMat.mat bulunamadi!"");
        }

        // Sahnede var olan LobbyFloor objesine manuel ata (garanti olsun diye)
        GameObject floorObj = GameObject.Find(""LobbyFloor_ARIXON"");
        if (floorObj != null && floorMat != null)
        {
            floorObj.GetComponent<Renderer>().sharedMaterial = floorMat;
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(floorObj.scene);
            Debug.Log(""ARIXON: Sahnede zemin materyali baglandi!"");
        }

        // Player Prefab icindeki materyali duzelt
        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(""Assets/Prefabs/PlayerPrefab.prefab"");
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
                    Debug.Log(""ARIXON: PlayerPrefab materyali URP olarak guncellendi."");
                }
            }
        }
        
        AssetDatabase.SaveAssets();
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        Debug.Log(""ARIXON: Tum mor materyal sorunlari cozuldu!"");
    }
}
