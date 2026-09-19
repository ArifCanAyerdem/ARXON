using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

namespace Arixon.Network
{
    /// <summary>
    /// ARİXON Ağ Yöneticisi ve Bağlantı Kontrolcüsü.
    /// Unity Netcode for GameObjects (NGO) altyapısı üzerinde Host/Client bağlantılarını,
    /// oyuncu katılma/ayrılma bildirimlerini ve sahne senkronizasyonunu yönetir.
    /// </summary>
    public class ArixonNetworkManager : MonoBehaviour
    {
        public static ArixonNetworkManager Instance { get; private set; }

        public event Action<string> OnNetworkLog;
        public event Action<ulong> OnPlayerJoined;
        public event Action<ulong> OnPlayerLeft;

        [Header("Sahne Ayarları")]
        [Tooltip("Host başlatıldığında tüm oyuncuların aktarılacağı oyun sahnesi adı")]
        [SerializeField] private string gameSceneName = "SampleScene"; // Başlangıç sahnesini SampleScene olarak güncelledik.

        [HideInInspector]
        public System.Collections.Generic.Dictionary<ulong, int> PlayerTeams = new System.Collections.Generic.Dictionary<ulong, int>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;
                NetworkManager.Singleton.OnServerStarted += HandleServerStarted;
            }
        }

        private void OnDestroy()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
                NetworkManager.Singleton.OnServerStarted -= HandleServerStarted;
            }
        }

        /// <summary>
        /// Odayı kurar (Host: Hem sunucu hem de yerel bir oyuncu olarak başlar).
        /// </summary>
        public bool StartHost()
        {
            Debug.Log($"[Network] [ArixonNetworkManager.StartHost] -> Metot çağrıldı. (NetworkManager.Singleton var mı: {NetworkManager.Singleton != null})");
            if (NetworkManager.Singleton == null)
            {
                EmitLog("HATA: Sahnede NetworkManager bileşeni bulunamadı!");
                Debug.LogError("[Network] [ArixonNetworkManager.StartHost] -> Başarısız: NetworkManager.Singleton null!");
                return false;
            }

            if (NetworkManager.Singleton.IsListening)
            {
                EmitLog("UYARI: Ağ oturumu zaten aktif durumda.");
                Debug.LogWarning($"[Network] [ArixonNetworkManager.StartHost] -> Başarısız: Oturum zaten aktif. (IsServer: {NetworkManager.Singleton.IsServer}, IsClient: {NetworkManager.Singleton.IsClient})");
                return false;
            }

            EmitLog("Lobi lideri olarak Host başlatılıyor...");
            bool success = NetworkManager.Singleton.StartHost();

            Debug.Log($"[Network] [ArixonNetworkManager.StartHost] -> StartHost sonucu (Başarı: {success})");

            if (success)
            {
                EmitLog("Host başarıyla kuruldu! Port: 7777 (Oyuncular bekleniyor...)");
            }
            else
            {
                EmitLog("HATA: Host başlatılamadı!");
                Debug.LogError("[Network] [ArixonNetworkManager.StartHost] -> Başarısız: Unity Netcode StartHost() false döndürdü.");
            }

            return success;
        }

        /// <summary>
        /// Mevcut bir odaya katılır (Client: İstemci olarak sunucuya bağlanır).
        /// </summary>
        public bool StartClient()
        {
            Debug.Log($"[Network] [ArixonNetworkManager.StartClient] -> Metot çağrıldı. (NetworkManager.Singleton var mı: {NetworkManager.Singleton != null})");
            if (NetworkManager.Singleton == null)
            {
                EmitLog("HATA: Sahnede NetworkManager bileşeni bulunamadı!");
                Debug.LogError("[Network] [ArixonNetworkManager.StartClient] -> Başarısız: NetworkManager.Singleton null!");
                return false;
            }

            if (NetworkManager.Singleton.IsListening)
            {
                EmitLog("UYARI: Zaten bir ağ oturumundasınız.");
                Debug.LogWarning($"[Network] [ArixonNetworkManager.StartClient] -> Başarısız: Oturum zaten aktif. (IsServer: {NetworkManager.Singleton.IsServer}, IsClient: {NetworkManager.Singleton.IsClient})");
                return false;
            }

            EmitLog("Sunucuya bağlanılıyor (127.0.0.1:7777)...");
            bool success = NetworkManager.Singleton.StartClient();

            Debug.Log($"[Network] [ArixonNetworkManager.StartClient] -> StartClient sonucu (Başarı: {success})");

            if (success)
            {
                EmitLog("İstemci bağlantı isteği gönderildi...");
            }
            else
            {
                EmitLog("HATA: İstemci başlatılamadı!");
                Debug.LogError("[Network] [ArixonNetworkManager.StartClient] -> Başarısız: Unity Netcode StartClient() false döndürdü.");
            }

            return success;
        }

        /// <summary>
        /// Host tarafında tüm bağlı oyuncuları belirlenen oyun sahnesine senkronize şekilde taşır.
        /// </summary>
        public void LoadGameScene()
        {
            Debug.Log($"[Network] [ArixonNetworkManager.LoadGameScene] -> Çağrıldı. (IsServer: {(NetworkManager.Singleton != null ? NetworkManager.Singleton.IsServer : false)})");
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            {
                EmitLog($"Oyun sahnesi senkronize ediliyor: '{gameSceneName}'...");
                NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnGameSceneLoaded;
                NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
            }
            else
            {
                Debug.LogWarning("[Network] [ArixonNetworkManager.LoadGameScene] -> Başarısız: Sadece Server sahne yükleyebilir veya NetworkManager null!");
            }
        }

        private void OnGameSceneLoaded(string sceneName, LoadSceneMode loadSceneMode, System.Collections.Generic.List<ulong> clientsCompleted, System.Collections.Generic.List<ulong> clientsTimedOut)
        {
            Debug.Log($"[Network] [ArixonNetworkManager.OnGameSceneLoaded] -> Event tetiklendi. (Yüklenen Sahne: {sceneName}, Beklenen Sahne: {gameSceneName})");
            if (sceneName != gameSceneName) return;
            
            // Etkinliğe artık ihtiyacımız yok
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnGameSceneLoaded;

            EmitLog("[AĞ] Oyun sahnesi herkes için yüklendi, oyuncular takımlarına göre doğma (Spawn) noktalarına gönderiliyor...");

            // Sahnede bulunan tüm SpawnPointData nesnelerini topla
            Arixon.Gameplay.SpawnPointData[] spawnPoints = FindObjectsByType<Arixon.Gameplay.SpawnPointData>(FindObjectsSortMode.None);
            
            System.Collections.Generic.List<Arixon.Gameplay.SpawnPointData> usedSpawns = new System.Collections.Generic.List<Arixon.Gameplay.SpawnPointData>();

            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                ulong clientId = client.ClientId;
                // Eğer oyuncunun takımı belli değilse varsayılan 1 (Mavi) yapalım
                int teamId = PlayerTeams.ContainsKey(clientId) ? PlayerTeams[clientId] : 1;

                // Bu takım için boşta olan bir spawn noktası bul
                Arixon.Gameplay.SpawnPointData selectedSpawn = null;
                foreach (var sp in spawnPoints)
                {
                    if (sp.TeamID == teamId && !usedSpawns.Contains(sp))
                    {
                        selectedSpawn = sp;
                        usedSpawns.Add(sp);
                        break;
                    }
                }

                if (selectedSpawn != null)
                {
                    // Oyuncu nesnesini al
                    NetworkObject playerObj = client.PlayerObject;
                    if (playerObj != null)
                    {
                        var controller = playerObj.GetComponent<Arixon.Gameplay.PlayerController>();
                        if (controller != null)
                        {
                            EmitLog($"[Spawn] Oyuncu #{clientId} ({teamId}. Takım) -> {selectedSpawn.transform.position} konumuna ışınlanıyor.");
                            
                            // ClientRpc parametresi ile SADECE o oyuncuya komut yolluyoruz
                            ClientRpcParams rpcParams = new ClientRpcParams
                            {
                                Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { clientId } }
                            };
                            
                            controller.TargetTeleportClientRpc(selectedSpawn.transform.position, selectedSpawn.transform.rotation, rpcParams);
                            
                            // Takım rengini atayalım (Tüm client'lara senkronize olur)
                            controller.TeamColorID.Value = teamId;
                        }
                    }
                }
                else
                {
                    EmitLog($"[Spawn Hata] {teamId}. Takım için uygun bir SpawnPointData bulunamadı! (Oyuncu #{clientId})");
                }
            }
        }

        /// <summary>
        /// Mevcut ağ bağlantısını güvenli bir şekilde sonlandırır.
        /// </summary>
        public void ShutdownNetwork()
        {
            Debug.Log($"[Network] [ArixonNetworkManager.ShutdownNetwork] -> Metot çağrıldı. (IsListening: {(NetworkManager.Singleton != null ? NetworkManager.Singleton.IsListening : false)})");
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                EmitLog("Ağ oturumu kapatılıyor...");
                NetworkManager.Singleton.Shutdown();
            }
        }



        #region Ağ Olay Dinleyicileri (Callbacks)

        private void HandleServerStarted()
        {
            EmitLog("[AĞ] Sunucu hazır ve dinlemede.");
        }

        private void HandleClientConnected(ulong clientId)
        {
            EmitLog($"[AĞ] Oyuncu bağlandı! (Oyuncu ID: #{clientId})");
            OnPlayerJoined?.Invoke(clientId);
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            EmitLog($"[AĞ] Oyuncu ayrıldı. (Oyuncu ID: #{clientId})");
            OnPlayerLeft?.Invoke(clientId);
        }

        private void EmitLog(string message)
        {
            Debug.Log($"[UI-Log] [ArixonNetworkManager.EmitLog] -> (Mesaj: {message})");
            OnNetworkLog?.Invoke(message);
        }

        #endregion
    }
}
