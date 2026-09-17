using UnityEditor;
using UnityEngine;
using Unity.Netcode;
using Arixon.Gameplay;

namespace Arixon.Editor
{
    public static class SetupGameBall
    {
        [MenuItem("ARİXON/Kurulum/Adım 07: Oyun Topu (Game Ball) Kurulumu")]
        public static void CreateAndAssignGameBall()
        {
            string prefabFolder = "Assets/Prefabs";
            if (!AssetDatabase.IsValidFolder(prefabFolder)) AssetDatabase.CreateFolder("Assets", "Prefabs");
            
            string matFolder = "Assets/Materials";
            if (!AssetDatabase.IsValidFolder(matFolder)) AssetDatabase.CreateFolder("Assets", "Materials");

            // 1. Fizik Materyali (PhysicMaterial) oluştur (Sekme ve sürtünme için)
            string physMatPath = "Assets/Materials/BallPhysicMat.physicMaterial";
            PhysicsMaterial ballPhysMat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(physMatPath);
            if (ballPhysMat == null)
            {
                ballPhysMat = new PhysicsMaterial("BallPhysicMat");
                ballPhysMat.bounciness = 0.8f; // Rocket League / Fall Guys zıplama hissi
                ballPhysMat.bounceCombine = PhysicsMaterialCombine.Maximum;
                ballPhysMat.dynamicFriction = 0.4f;
                ballPhysMat.staticFriction = 0.4f;
                AssetDatabase.CreateAsset(ballPhysMat, physMatPath);
            }

            // 2. Görsel Materyal (URP) oluştur
            string matPath = "Assets/Materials/BallMat.mat";
            Material ballMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (ballMat == null)
            {
                Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
                if (urpShader == null) urpShader = Shader.Find("Standard");
                ballMat = new Material(urpShader);
                ballMat.color = new Color(1f, 0.4f, 0f); // Turuncu (Rocket League tarzı)
                ballMat.SetFloat("_Smoothness", 0.7f); // Biraz parlak
                AssetDatabase.CreateAsset(ballMat, matPath);
            }

            // 3. Prefab oluştur
            string prefabPath = prefabFolder + "/GameBall.prefab";
            if (System.IO.File.Exists(prefabPath))
            {
                AssetDatabase.DeleteAsset(prefabPath);
            }

            GameObject ballObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ballObj.name = "GameBall";
            ballObj.transform.localScale = new Vector3(2f, 2f, 2f); // Devasa top

            // Bileşenler
            var renderer = ballObj.GetComponent<Renderer>();
            renderer.sharedMaterial = ballMat;

            var collider = ballObj.GetComponent<SphereCollider>();
            collider.sharedMaterial = ballPhysMat;

            var rb = ballObj.AddComponent<Rigidbody>();
            rb.mass = 5f; // Biraz ağır olsun, uçup gitmesin
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            ballObj.AddComponent<NetworkObject>();
            var nt = ballObj.AddComponent<Unity.Netcode.Components.NetworkTransform>();
            nt.SyncPositionX = true;
            nt.SyncPositionY = true;
            nt.SyncPositionZ = true;
            nt.Interpolate = true;

            ballObj.AddComponent<GameBall>(); // Oyun içi mantık scripti

            bool success;
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(ballObj, prefabPath, out success);
            if (!success || prefab == null)
            {
                Debug.LogError("ARİXON: GameBall Prefab kaydedilemedi! Yol: " + prefabPath);
            }
            else
            {
                Debug.Log("ARİXON: GameBall Prefab diske başarıyla kaydedildi!");
            }
            GameObject.DestroyImmediate(ballObj);

            // 4. NetworkManager'a (NetworkPrefabs listesine) Ekle
            var nmInScene = Object.FindFirstObjectByType<NetworkManager>();
            if (nmInScene == null)
            {
                Debug.LogWarning("ARİXON: Sahnede NetworkManager bulunamadı, GameBall NetworkManager listesine ŞİMDİLİK eklenmedi! (MainMenu açıldığında tekrar denenecek veya otomatik eklenecek).");
                return; // erken çık
            }
            SerializedObject so = new SerializedObject(nmInScene);
            SerializedProperty configProp = so.FindProperty("NetworkConfig");
                if (configProp != null)
                {
                    SerializedProperty prefabsProp = configProp.FindPropertyRelative("NetworkPrefabs");
                    if (prefabsProp == null) prefabsProp = configProp.FindPropertyRelative("Prefabs");

                    if (prefabsProp != null)
                    {
                        SerializedProperty m_Prefabs = prefabsProp.FindPropertyRelative("m_Prefabs");
                        if (m_Prefabs != null)
                        {
                            bool alreadyExists = false;
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

                            if (!alreadyExists)
                            {
                                m_Prefabs.arraySize++;
                                var newElement = m_Prefabs.GetArrayElementAtIndex(m_Prefabs.arraySize - 1);
                                var prefabRef = newElement.FindPropertyRelative("Prefab");
                                if (prefabRef != null) prefabRef.objectReferenceValue = prefab;
                            }
                        }
                    }
                }
                
                // NetworkPrefabsList assetlerine de ekle
                string[] listGuids = AssetDatabase.FindAssets("t:NetworkPrefabsList");
                foreach (string guid in listGuids)
                {
                    string listPath = AssetDatabase.GUIDToAssetPath(guid);
                    NetworkPrefabsList prefabsList = AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>(listPath);
                    if (prefabsList != null)
                    {
                        bool hasPrefab = false;
                        foreach (var p in prefabsList.PrefabList)
                        {
                            if (p.Prefab == prefab) hasPrefab = true;
                        }
                        if (!hasPrefab)
                        {
                            prefabsList.Add(new NetworkPrefab { Prefab = prefab });
                            EditorUtility.SetDirty(prefabsList);
                        }
                    }
                }

                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(nmInScene);
                UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
                Debug.Log("ARİXON: GameBall Prefab başarıyla oluşturuldu ve NetworkManager'a eklendi!");
        }
    }
}
