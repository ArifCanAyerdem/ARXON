using UnityEditor;
using UnityEngine;
using Unity.Netcode;
using Arixon.Gameplay;
using Arixon.Network;

namespace Arixon.Editor
{
    public static class SetupPlayerPrefab
    {
        [InitializeOnLoadMethod]
        public static void AutoRun()
        {
            if (!EditorPrefs.GetBool("ARIXON_PlayerPrefabAssigned_07_FixGhost", false))
            {
                EditorApplication.delayCall += () =>
                {
                    CreateAndAssignPlayerPrefab();
                    EditorPrefs.SetBool("ARIXON_PlayerPrefabAssigned_07_FixGhost", true);
                };
            }
        }

        [MenuItem("ARİXON/Kurulum/Adım 05: Player Prefab Oluştur ve Ata")]
        public static void CreateAndAssignPlayerPrefab()
        {
            string prefabFolder = "Assets/Prefabs";
            if (!AssetDatabase.IsValidFolder(prefabFolder))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }

            string prefabPath = prefabFolder + "/PlayerPrefab.prefab";
            
            // Ghost prefabs prevention: explicitly delete any existing file if it's broken
            if (System.IO.File.Exists(prefabPath))
            {
                // File exists physically, we will just load it
            }
            else
            {
                // File doesn't exist physically, but Unity might think it does (Ghost)
                AssetDatabase.DeleteAsset(prefabPath);
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (prefab == null)
            {
                // Sahneden BOŞ bir obje yarat (Shell Mimari - İleride Envanter sistemi için)
                GameObject playerObj = new GameObject("PlayerPrefab");

                // Bileşenleri Ekle
                playerObj.AddComponent<NetworkObject>();
                playerObj.AddComponent<ClientNetworkTransform>();
                playerObj.AddComponent<PlayerController>();

                // Character Controller düzeltmeleri
                var cc = playerObj.GetComponent<CharacterController>();
                cc.center = new Vector3(0, 1f, 0);
                cc.radius = 0.5f;
                cc.height = 2f;

                // Görsel kapsayıcı (VisualsHolder) - Envanter/Kıyafet/Silah buraya gelecek
                GameObject visualsHolder = new GameObject("VisualsHolder");
                visualsHolder.transform.SetParent(playerObj.transform);
                visualsHolder.transform.localPosition = Vector3.zero;

                // Şimdilik test için geçici bir Kapsül (DefaultModel) ekle
                GameObject defaultModel = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                defaultModel.name = "DefaultModel_Temporary";
                defaultModel.transform.SetParent(visualsHolder.transform);
                defaultModel.transform.localPosition = new Vector3(0, 1f, 0); // Yere değecek şekilde
                GameObject.DestroyImmediate(defaultModel.GetComponent<Collider>()); // Ana Collider dışındakileri sil

                // Yüz göstergesi (Face Indicator) - Nereye baktığını anlamak için
                GameObject faceIndicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
                faceIndicator.name = "FaceIndicator";
                faceIndicator.transform.SetParent(visualsHolder.transform);
                faceIndicator.transform.localPosition = new Vector3(0, 1.5f, 0.5f);
                faceIndicator.transform.localScale = new Vector3(0.6f, 0.2f, 0.2f);
                GameObject.DestroyImmediate(faceIndicator.GetComponent<Collider>());

                // URP Uyumlu Materyal Ata (Mor/Magenta Sorununu Çözmek İçin)
                Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
                if (urpShader == null) urpShader = Shader.Find("Standard");
                Material urpMat = new Material(urpShader);
                urpMat.color = Color.white;
                
                if (defaultModel.GetComponent<Renderer>() != null) defaultModel.GetComponent<Renderer>().sharedMaterial = urpMat;
                if (faceIndicator.GetComponent<Renderer>() != null)
                {
                    Material faceMat = new Material(urpShader);
                    faceMat.color = Color.black;
                    faceIndicator.GetComponent<Renderer>().sharedMaterial = faceMat;
                }

                prefab = PrefabUtility.SaveAsPrefabAsset(playerObj, prefabPath);
                GameObject.DestroyImmediate(playerObj);
                Debug.Log("ARİXON: PlayerPrefab BAŞARIYLA oluşturuldu! (" + prefabPath + ")");
            }

            // OTONOM ATAMA SİSTEMİ (Kullanıcı müdahalesi olmadan NetworkManager'a ekle)
            var nmInScene = Object.FindFirstObjectByType<NetworkManager>();
            if (nmInScene != null)
            {
                SerializedObject so = new SerializedObject(nmInScene);
                SerializedProperty configProp = so.FindProperty("NetworkConfig");
                if (configProp != null)
                {
                    SerializedProperty playerPrefabProp = configProp.FindPropertyRelative("PlayerPrefab");
                    if (playerPrefabProp != null)
                    {
                        playerPrefabProp.objectReferenceValue = prefab;
                    }

                    SerializedProperty prefabsProp = configProp.FindPropertyRelative("NetworkPrefabs");
                    if (prefabsProp == null) prefabsProp = configProp.FindPropertyRelative("Prefabs"); // İsim değişmiş olabilir

                    if (prefabsProp != null)
                    {
                        SerializedProperty m_Prefabs = prefabsProp.FindPropertyRelative("m_Prefabs");
                        if (m_Prefabs != null)
                        {
                            bool alreadyExists = false;
                            
                            // 1. Önce "Missing (Null)" olan bozuk prefabları temizleyelim ki NGO çökmesin
                            for (int i = m_Prefabs.arraySize - 1; i >= 0; i--)
                            {
                                var element = m_Prefabs.GetArrayElementAtIndex(i);
                                var prefabRef = element.FindPropertyRelative("Prefab");
                                if (prefabRef == null || prefabRef.objectReferenceValue == null)
                                {
                                    m_Prefabs.DeleteArrayElementAtIndex(i);
                                }
                            }

                            // 2. Şimdi bizim prefab listede var mı kontrol edelim
                            for (int i = 0; i < m_Prefabs.arraySize; i++)
                            {
                                var element = m_Prefabs.GetArrayElementAtIndex(i);
                                var prefabRef = element.FindPropertyRelative("Prefab");
                                if (prefabRef != null && prefabRef.objectReferenceValue == prefab)
                                {
                                    alreadyExists = true;
                                    break;
                                }
                            }

                            // 3. Yoksa listeye ekleyelim
                            if (!alreadyExists)
                            {
                                m_Prefabs.arraySize++;
                                var newElement = m_Prefabs.GetArrayElementAtIndex(m_Prefabs.arraySize - 1);
                                var prefabRef = newElement.FindPropertyRelative("Prefab");
                                if (prefabRef != null) prefabRef.objectReferenceValue = prefab;
                            }
                        }
                    }

                    // Oyuncunun Lobide (MainMenu) doğmasını İSTİYORUZ (Zemin eklendi)
                    SerializedProperty autoSpawnProp = configProp.FindPropertyRelative("AutoSpawnPlayerPrefabClientSide");
                    if (autoSpawnProp != null)
                    {
                        autoSpawnProp.boolValue = true;
                    }
                }
                    // External listeleri (NetworkPrefabsLists) temizle ve prefab'ı ekle
                    string[] listGuids = AssetDatabase.FindAssets("t:NetworkPrefabsList");
                    foreach (string guid in listGuids)
                    {
                        string listPath = AssetDatabase.GUIDToAssetPath(guid);
                        NetworkPrefabsList prefabsList = AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>(listPath);
                        if (prefabsList != null)
                        {
                            // Null/Missing olanları temizle
                            for (int i = prefabsList.PrefabList.Count - 1; i >= 0; i--)
                            {
                                if (prefabsList.PrefabList[i].Prefab == null)
                                {
                                    prefabsList.Remove(prefabsList.PrefabList[i]);
                                }
                            }
                            
                            // Eğer bizimki yoksa ekle
                            bool hasPrefab = false;
                            foreach (var p in prefabsList.PrefabList)
                            {
                                if (p.Prefab == prefab)
                                {
                                    hasPrefab = true;
                                    break;
                                }
                            }
                            
                            if (!hasPrefab)
                            {
                                prefabsList.Add(new NetworkPrefab { Prefab = prefab });
                            }
                            
                            EditorUtility.SetDirty(prefabsList);
                        }
                    }
                    
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(nmInScene);
                    UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
                    Debug.Log("ARİXON OTONOM BİLDİRİM: PlayerPrefab başarıyla NetworkManager'a atandı!");
                }
            else
            {
                Debug.LogError("ARİXON: Sahnede NetworkManager bulunamadı!");
            }
        }
    }
}
