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
        [SerializeField] private string gameSceneName = "GameScene";

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
            if (NetworkManager.Singleton == null)
            {
                EmitLog("HATA: Sahnede NetworkManager bileşeni bulunamadı!");
                return false;
            }

            if (NetworkManager.Singleton.IsListening)
            {
                EmitLog("UYARI: Ağ oturumu zaten aktif durumda.");
                return false;
            }

            EmitLog("Lobi lideri olarak Host başlatılıyor...");
            bool success = NetworkManager.Singleton.StartHost();

            if (success)
            {
                EmitLog("Host başarıyla kuruldu! Port: 7777 (Oyuncular bekleniyor...)");
            }
            else
            {
                EmitLog("HATA: Host başlatılamadı!");
            }

            return success;
        }

        /// <summary>
        /// Mevcut bir odaya katılır (Client: İstemci olarak sunucuya bağlanır).
        /// </summary>
        public bool StartClient()
        {
            if (NetworkManager.Singleton == null)
            {
                EmitLog("HATA: Sahnede NetworkManager bileşeni bulunamadı!");
                return false;
            }

            if (NetworkManager.Singleton.IsListening)
            {
                EmitLog("UYARI: Zaten bir ağ oturumundasınız.");
                return false;
            }

            EmitLog("Sunucuya bağlanılıyor (127.0.0.1:7777)...");
            bool success = NetworkManager.Singleton.StartClient();

            if (success)
            {
                EmitLog("İstemci bağlantı isteği gönderildi...");
            }
            else
            {
                EmitLog("HATA: İstemci başlatılamadı!");
            }

            return success;
        }

        /// <summary>
        /// Host tarafında tüm bağlı oyuncuları belirlenen oyun sahnesine senkronize şekilde taşır.
        /// </summary>
        public void LoadGameScene()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            {
                EmitLog($"Oyun sahnesi senkronize ediliyor: '{gameSceneName}'...");
                NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
            }
        }

        /// <summary>
        /// Mevcut ağ bağlantısını güvenli bir şekilde sonlandırır.
        /// </summary>
        public void ShutdownNetwork()
        {
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
            Debug.Log($"[ARİXON Network] {message}");
            OnNetworkLog?.Invoke(message);
        }

        #endregion
    }
}
