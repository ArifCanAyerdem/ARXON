using UnityEngine;
using UnityEngine.UIElements;

namespace Arixon.UI
{
    /// <summary>
    /// ARİXON Çok Oyunculu Lobi ve Sohbet Kontrolcüsü.
    /// Lobi odasındaki başlat, katıl, davet et, oda kodu kopyalama ve canlı mesajlaşma sistemini yönetir.
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
        private Toggle _toggleRelay;
        private Label _roomCodeVal;

        // Lobi Sohbet Elemanları
        private ScrollView _chatScroll;
        private TextField _chatInput;
        private Button _btnChatSend;

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
            _toggleRelay = root.Q<Toggle>("toggle-relay");

            // Davet Butonları
            _btnInvite1 = root.Q<Button>("btn-invite-1");
            _btnInvite2 = root.Q<Button>("btn-invite-2");
            _btnInvite3 = root.Q<Button>("btn-invite-3");

            // Sohbet Elemanları
            _chatScroll = root.Q<ScrollView>("chat-scroll");
            _chatInput = root.Q<TextField>("chat-input");
            _btnChatSend = root.Q<Button>("btn-chat-send");

            // Olay Bağlantıları
            if (_btnHost != null) _btnHost.clicked += OnStartGameClicked;
            if (_btnJoin != null) _btnJoin.clicked += OnJoinClicked;
            if (_btnSettings != null) _btnSettings.clicked += OnSettingsClicked;
            if (_btnQuit != null) _btnQuit.clicked += OnLeaveLobbyClicked;
            if (_btnCopyCode != null) _btnCopyCode.clicked += OnCopyCodeClicked;
            if (_btnInvite1 != null) _btnInvite1.clicked += () => OnInviteClicked(1);
            if (_btnInvite2 != null) _btnInvite2.clicked += () => OnInviteClicked(2);
            if (_btnInvite3 != null) _btnInvite3.clicked += () => OnInviteClicked(3);

            if (_toggleRelay != null)
            {
                _toggleRelay.RegisterValueChangedCallback(OnRelayToggleChanged);
            }

            // Sohbet Gönderme Butonu ve Enter Tuşu Dinleyicisi
            if (_btnChatSend != null) _btnChatSend.clicked += OnChatSendClicked;
            if (_chatInput != null)
            {
                _chatInput.RegisterCallback<KeyDownEvent>(evt =>
                {
                    if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                    {
                        OnChatSendClicked();
                        evt.StopPropagation();
                    }
                });
            }
        }

        private void OnDisable()
        {
            if (_btnHost != null) _btnHost.clicked -= OnStartGameClicked;
            if (_btnJoin != null) _btnJoin.clicked -= OnJoinClicked;
            if (_btnSettings != null) _btnSettings.clicked -= OnSettingsClicked;
            if (_btnQuit != null) _btnQuit.clicked -= OnLeaveLobbyClicked;
            if (_btnCopyCode != null) _btnCopyCode.clicked -= OnCopyCodeClicked;
            if (_btnChatSend != null) _btnChatSend.clicked -= OnChatSendClicked;

            if (_toggleRelay != null)
            {
                _toggleRelay.UnregisterValueChangedCallback(OnRelayToggleChanged);
            }
        }

        /// <summary>
        /// Mesaj gönderme mantığı.
        /// </summary>
        private void OnChatSendClicked()
        {
            if (_chatInput == null || string.IsNullOrWhiteSpace(_chatInput.value)) return;

            string messageText = _chatInput.value.Trim();
            _chatInput.value = "";

            AddMessageToChat("OYUNCU_01", messageText, false);

            // Metin kutusuna tekrar odaklan
            _chatInput.Focus();
        }

        /// <summary>
        /// Sohbet kutusuna görsel yeni mesaj satırı ekler.
        /// </summary>
        public void AddMessageToChat(string sender, string message, bool isSystem)
        {
            if (_chatScroll == null) return;

            var entry = new VisualElement();
            entry.AddToClassList("chat-entry");
            if (isSystem) entry.AddToClassList("system-entry");

            var senderLabel = new Label(isSystem ? $"[{sender}] " : $"{sender}: ");
            senderLabel.AddToClassList(isSystem ? "sender-system" : "sender-me");

            var messageLabel = new Label(message);
            messageLabel.AddToClassList(isSystem ? "msg-text-system" : "msg-text");

            entry.Add(senderLabel);
            entry.Add(messageLabel);

            _chatScroll.Add(entry);

            // En son mesaja otomatik kaydır
            _chatScroll.schedule.Execute(() =>
            {
                _chatScroll.ScrollTo(entry);
            });
        }

        private void OnRelayToggleChanged(ChangeEvent<bool> evt)
        {
            Debug.Log($"[LOBİ AYARI] Unity Relay Modu: {(evt.newValue ? "AÇIK (İnternet Relay)" : "KAPALI (Yerel Ağ / UTP)")}");
            AddMessageToChat("SİSTEM", $"Bağlantı modu değişti: {(evt.newValue ? "Unity Relay (İnternet)" : "Yerel Ağ (UTP)")}", true);
        }

        private void OnStartGameClicked()
        {
            bool isRelay = _toggleRelay != null && _toggleRelay.value;
            Debug.Log($"[LOBİ] Oyunu Başlat butonuna basıldı! Mod: {(isRelay ? "Unity Relay" : "Yerel Ağ")}");
            AddMessageToChat("SİSTEM", "Lobi lideri oyunu başlatıyor...", true);
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
                AddMessageToChat("SİSTEM", $"Oda kodu kopyalandı: {_roomCodeVal.text}", true);
            }
        }

        private void OnInviteClicked(int slotIndex)
        {
            Debug.Log($"[LOBİ] Slot {slotIndex} için arkadaş daveti tetiklendi.");
            AddMessageToChat("SİSTEM", $"Slot {slotIndex} için davet bağlantısı oluşturuldu.", true);
        }
    }
}
