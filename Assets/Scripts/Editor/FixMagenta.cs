using UnityEditor;
using UnityEngine;
using Arixon.Editor;

public static class FixMagenta
{
    public static void Run()
    {
        // Zemini sil
        GameObject oldFloor = GameObject.Find("LobbyFloor_ARIXON");
        if (oldFloor != null) GameObject.DestroyImmediate(oldFloor);
        
        // Yeniden oluştur
        SetupLobbyEnvironment.CreateLobbyFloor();
        
        // Prefabı sil
        string prefabPath = "Assets/Prefabs/PlayerPrefab.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
        {
            AssetDatabase.DeleteAsset(prefabPath);
        }
        
        // Prefabı yeniden oluştur
        SetupPlayerPrefab.CreateAndAssignPlayerPrefab();
        
        Debug.Log("Magenta fixer ran successfully!");
    }
}
