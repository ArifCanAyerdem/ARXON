#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Arixon.Network;

namespace Arixon.Editor
{
    /// <summary>
    /// MainMenu sahnesine NetworkManager, UnityTransport ve ArixonNetworkManager
    /// bileşenlerini otomatik olarak kuran ve kaydeden editör aracı.
    /// </summary>
    [InitializeOnLoad]
    public static class NetworkManagerSetupTool
    {
        static NetworkManagerSetupTool()
        {
            EditorApplication.delayCall += CheckAndSetupNetworkManager;
        }

        [MenuItem("ARİXON/Ağ/NetworkManager'ı MainMenu Sahnesine Kur ve Kaydet", false, 1)]
        public static void ForceSetupNetworkManager()
        {
            SetupInScene(true);
        }

        [MenuItem("ARİXON/Ağ/SampleScene Zeminini (Ground) Doğrula ve Kur", false, 2)]
        public static void ForceSetupGroundInSampleScene()
        {
            EnsureGroundInSampleScene(true);
        }

        [MenuItem("ARİXON/Ağ/Tüm Sahneleri Doğrula ve Kur (MainMenu + SampleScene)", false, 0)]
        public static void SetupAllScenes()
        {
            SetupInScene(true);
            EnsureGroundInSampleScene(true);
            // Return to MainMenu scene as default start
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);
            Debug.Log("<color=green>[ARİXON]</color> Tüm sahneler (MainMenu ve SampleScene) başarıyla doğrulandı ve hazır!");
        }

        private static void CheckAndSetupNetworkManager()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            SetupInScene(false);
        }

        private static void SetupInScene(bool openSceneIfClosed)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            Scene currentScene = SceneManager.GetActiveScene();
            string targetPath = "Assets/Scenes/MainMenu.unity";

            if (!currentScene.name.Equals("MainMenu", System.StringComparison.OrdinalIgnoreCase))
            {
                if (openSceneIfClosed)
                {
                    currentScene = EditorSceneManager.OpenScene(targetPath, OpenSceneMode.Single);
                }
                else
                {
                    return;
                }
            }

            // Sahnede zaten NetworkManager var mı?
            NetworkManager existingNetManager = Object.FindFirstObjectByType<NetworkManager>();
            if (existingNetManager != null)
            {
                // ArixonNetworkManager bileşeni de var mı kontrol et
                if (existingNetManager.GetComponent<ArixonNetworkManager>() == null)
                {
                    existingNetManager.gameObject.AddComponent<ArixonNetworkManager>();
                    EditorUtility.SetDirty(existingNetManager.gameObject);
                    EditorSceneManager.MarkSceneDirty(currentScene);
                    EditorSceneManager.SaveScene(currentScene);
                    Debug.Log("<color=green>[ARİXON]</color> ArixonNetworkManager mevcut NetworkManager nesnesine eklendi ve kaydedildi!");
                }
                return;
            }

            // Yeni NetworkManager GameObject oluştur
            GameObject netObj = new GameObject("NetworkManager");
            NetworkManager netManager = netObj.AddComponent<NetworkManager>();
            UnityTransport transport = netObj.AddComponent<UnityTransport>();

            // UTP Yerel Ağ (Localhost: 127.0.0.1 : 7777) varsayılan ayarları
            transport.SetConnectionData("127.0.0.1", 7777, "0.0.0.0");

            // NetworkManager yapılandırması
            netManager.NetworkConfig.NetworkTransport = transport;
            netManager.NetworkConfig.EnableSceneManagement = true;

            // Arixon Network Manager scripti
            netObj.AddComponent<ArixonNetworkManager>();

            // Sahneyi kaydet
            EditorUtility.SetDirty(netObj);
            EditorSceneManager.MarkSceneDirty(currentScene);
            EditorSceneManager.SaveScene(currentScene);

            Debug.Log("<color=green>[ARİXON]</color> NetworkManager, UnityTransport ve ArixonNetworkManager MainMenu sahnesine kuruldu ve kaydedildi!");
        }

        private static void EnsureGroundInSampleScene(bool openSceneIfClosed)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            Scene currentScene = SceneManager.GetActiveScene();
            string targetPath = "Assets/Scenes/SampleScene.unity";

            if (!currentScene.name.Equals("SampleScene", System.StringComparison.OrdinalIgnoreCase))
            {
                if (openSceneIfClosed)
                {
                    currentScene = EditorSceneManager.OpenScene(targetPath, OpenSceneMode.Single);
                }
                else
                {
                    return;
                }
            }

            GameObject existingGround = GameObject.Find("Ground");
            if (existingGround == null)
            {
                GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = "Ground";
                ground.transform.position = Vector3.zero;
                ground.transform.localScale = new Vector3(5f, 1f, 5f); // 50x50 metre

                EditorUtility.SetDirty(ground);
                EditorSceneManager.MarkSceneDirty(currentScene);
                EditorSceneManager.SaveScene(currentScene);
                Debug.Log("<color=green>[ARİXON]</color> Ground (50x50 Plane) SampleScene sahnesine eklendi ve kaydedildi!");
            }
        }
    }
}
#endif
