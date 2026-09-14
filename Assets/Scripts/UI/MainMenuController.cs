using UnityEngine;
using UnityEngine.UIElements;

namespace Arixon.UI
{
    /// <summary>
    /// ARİXON Çok Oyunculu Lobi Kontrolcüsü.
    /// Lobi odasındaki başlat, katıl, davet et ve oda kodu kopyalama butonlarını yönetir.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class MainMenuController : MonoBehaviour
    {
        private UIDocument _uiDocument;
        private Button _btnHost;
        private Button _btnJoin;
        private Button _btnSettings;
        private Button _btnQuit;
        private Button _btnCopyCode;
        private Button _btnInvite1;
        private Button _btnInvite2;
        private Button _btnInvite3;
        private Label _roomCodeVal;

        private void Awake()
        {
            _uiDocument = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            var root = _uiDocument.rootVisualElement;
            if (root == null) return;

            // Ana Aksiyon Butonları
            _btnHost = root.Q<Button>("btn-host");
            _btnJoin = root.Q<Button>("btn-join");
            _btnSettings = root.Q<Button>("btn-settings");
            _btnQuit = root.Q<Button>("btn-quit");
            _btnCopyCode = root.Q<Button>("btn-copy-code");
            _roomCodeVal = root.Q<Label>("room-code-val");

            // Davet Butonları
            _btnInvite1 = root.Q<Button>("btn-invite-1");
            _btnInvite2 = root.Q<Button>("btn-invite-2");
            _btnInvite3 = root.Q<Button>("btn-invite-3");

            // Olay Bağlantıları
            if (_btnHost != null) _btnHost.clicked += OnStartGameClicked;
            if (_btnJoin != null) _btnJoin.clicked += OnJoinClicked;
            if (_btnSettings != null) _btnSettings.clicked += OnSettingsClicked;
            if (_btnQuit != null) _btnQuit.clicked += OnLeaveLobbyClicked;
            if (_btnCopyCode != null) _btnCopyCode.clicked += OnCopyCodeClicked;
            if (_btnInvite1 != null) _btnInvite1.clicked += () => OnInviteClicked(1);
            if (_btnInvite2 != null) _btnInvite2.clicked += () => OnInviteClicked(2);
            if (_btnInvite3 != null) _btnInvite3.clicked += () => OnInviteClicked(3);
        }

        private void OnDisable()
        {
            if (_btnHost != null) _btnHost.clicked -= OnStartGameClicked;
            if (_btnJoin != null) _btnJoin.clicked -= OnJoinClicked;
            if (_btnSettings != null) _btnSettings.clicked -= OnSettingsClicked;
            if (_btnQuit != null) _btnQuit.clicked -= OnLeaveLobbyClicked;
            if (_btnCopyCode != null) _btnCopyCode.clicked -= OnCopyCodeClicked;
        }

        private void OnStartGameClicked()
        {
            Debug.Log("[LOBİ] Oyunu Başlat butonuna basıldı! Host sunucusu kuruluyor...");
            // TODO: NetworkManager.Singleton.StartHost() ve sahne geçişi
        }

        private void OnJoinClicked()
        {
            Debug.Log("[LOBİ] Oda Ara / Katıl butonuna basıldı!");
        }

        private void OnSettingsClicked()
        {
            Debug.Log("[LOBİ] Ayarlar açıldı.");
        }

        private void OnLeaveLobbyClicked()
        {
            Debug.Log("[LOBİ] Lobiden ayrılınıyor...");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnCopyCodeClicked()
        {
            if (_roomCodeVal != null)
            {
                GUIUtility.systemCopyBuffer = _roomCodeVal.text;
                Debug.Log($"[LOBİ] Oda kodu panoya kopyalandı: {_roomCodeVal.text}");
            }
        }

        private void OnInviteClicked(int slotIndex)
        {
            Debug.Log($"[LOBİ] Slot {slotIndex} için Steam arkadaş davet penceresi tetiklendi.");
        }
    }
}
