using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Netcode;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization;
using Unity.Collections;
using Arixon.Network;
#if UNITY_EDITOR
using Unity.Multiplayer.Playmode;
#endif

namespace Arixon.UI
{
    /// <summary>
    /// ARİXON Cartoon Ana Sayfa, Dinamik Canlı Oda Keşfi ve Lobi Kontrolcüsü.
    /// - Oda listesi yalnızca bir oyuncu oda kurduğunda dinamik olarak dolar.
    /// - Kullanıcılar kendi oyuncu isimlerini belirleyebilir (varsayılan: Oyuncu_01 / Oyuncu_02).
    /// - Çift ekran iki yönlü slot doluluğunu ve sohbetini anlık senkronize eder.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class MainMenuController : MonoBehaviour
    {
        private UIDocument _uiDocument;

        // Ekran Görünümleri
        private VisualElement _viewHome;
        private VisualElement _viewLobby;

        // --- ANA SAYFA ELEMANLARI ---
        private TextField _inputPlayerName;
        private Button _btnHomeCreateRoom;
        private TextField _inputRoomCode;
        private Button _btnHomeJoinCode;
        private Label _homeStatusMsg;
        private ScrollView _roomsScroll;
        private VisualElement _emptyRoomsBox;
        private Button _btnHomeQuit;
        
        // --- AYARLAR (SETTINGS) ELEMANLARI ---
        private VisualElement _viewSettings;
        private Button _btnHomeSettings;
        private Button _btnCloseSettings;
        private Slider _sliderMasterVolume;
        private Slider _sliderMusicVolume;
        private Toggle _toggleFullscreen;
        private Toggle _toggleCameraShake;
        private DropdownField _dropdownQuality;
        private DropdownField _dropdownLanguage;

        // Ayarlar Sekmeleri
        private Button _btnTabAudio;
        private Button _btnTabGraphics;
        private Button _btnTabGameplay;
        private VisualElement _contentAudio;
        private VisualElement _contentGraphics;
        private VisualElement _contentGameplay;

        // --- LOBİ ODASI ELEMANLARI ---
        private Label _lobbyBadge;
        private Label _roomCodeVal;
        private Button _btnCopyCode;
        private Label _rosterTitle;
        private Label _lblTeamRed;
        private Label _lblTeamBlue;
        private Label _lblLobbyChat;
        private Label _lblPing;
        
        // Home Screen Labels
        private Label _homeRigLogo;
        private Label _homeIdLabel;
        private Label _homeBadgeRoomCenter;
        private Label _homeDescCreate;
        private Label _homeRoomCodeLbl;
        private Label _homeBadgeLiveFields;
        private Label _homeDescLive;
        private Label _homeEmptyTitle;
        private Label _homeEmptyDesc;
        private Label _lobbyLogoLbl;
        
        private Button _btnStartGame;
        private Button _btnLeaveLobby;

        // Slotlar
        private VisualElement _slot1Card;
        private Label _slot1Name;
        private VisualElement _slot2Card;
        private Label _slot2Name;
        private Label _slot2AvatarTag;
        private VisualElement _slot2Circle;
        private VisualElement _slot1Ready;
        private Label _slot1ReadyLbl;
        private VisualElement _slot2Ready;
        private Label _slot2ReadyLbl;
        private VisualElement _slot3Card, _slot3Circle, _slot3Ready;
        private Label _slot3Name, _slot3AvatarTag, _slot3ReadyLbl;
        private VisualElement _slot4Card, _slot4Circle, _slot4Ready;
        private Label _slot4Name, _slot4AvatarTag, _slot4ReadyLbl;
        private bool _isLocalPlayerReady = false;
        private System.Collections.Generic.Dictionary<ulong, bool> _playerReadyStates = new System.Collections.Generic.Dictionary<ulong, bool>();
        private System.Collections.Generic.Dictionary<ulong, string> _playerNames = new System.Collections.Generic.Dictionary<ulong, string>();
        private System.Collections.Generic.Dictionary<ulong, string> _playerTeams = new System.Collections.Generic.Dictionary<ulong, string>();
                private const string ROSTER_CHANNEL = "ArixonRosterSync";
        private const string READY_CHANNEL_C2S = "ArixonReadySync_C2S";
        private const string READY_CHANNEL_S2C = "ArixonReadySync_S2C";
        private const string SYNC_NAMES_CHANNEL = "ArixonSyncNames";

        private Button _btnInvite1;

        // Lobi Sohbet
        private ScrollView _chatScroll;
        private TextField _chatInput;
        private Button _btnChatSend;

        // Durum Değişkenleri
        private string _localPlayerName = "Oyuncu_01";
        private string _hostPlayerName = "Oyuncu_01";
        private bool _isHost = true;
        private string _currentRoomCode = "";
        private float _roomRefreshTimer = 0f;
        private float _heartbeatTimer = 0f;

        private const string CHAT_CHANNEL = "ArixonLobbyChat";

        private void Awake()
        {
            _uiDocument = GetComponent<UIDocument>();

#if UNITY_EDITOR
            // Main Editor Play Mode'a yeni girdiğinde önceki oturumlardan kalan bayat odaları temizle
            if (CurrentPlayer.IsMainEditor)
            {
                ArixonRoomDiscovery.ClearAll();
            }
#endif
        }

        private void OnEnable()
        {
            var root = _uiDocument.rootVisualElement;
            if (root == null) return;
                        
            // Görünümler
            _viewHome = root.Q<VisualElement>("view-home");
            _viewLobby = root.Q<VisualElement>("view-lobby");

            // Ana Sayfa Elemanları
            _inputPlayerName = root.Q<TextField>("input-player-name");
            _btnHomeCreateRoom = root.Q<Button>("btn-home-create-room");
            _inputRoomCode = root.Q<TextField>("input-room-code");
            _btnHomeJoinCode = root.Q<Button>("btn-home-join-code");
            _homeStatusMsg = root.Q<Label>("home-status-msg");
            _roomsScroll = root.Q<ScrollView>("rooms-scroll");
            _emptyRoomsBox = root.Q<VisualElement>("empty-rooms-box");
            _btnHomeQuit = root.Q<Button>("btn-home-quit");
            _btnHomeSettings = root.Q<Button>("btn-home-settings");

            // Ayarlar Elemanları
            _viewSettings = root.Q<VisualElement>("view-settings");
            _btnCloseSettings = root.Q<Button>("btn-close-settings");
            _sliderMasterVolume = root.Q<Slider>("slider-master-volume");
            _sliderMusicVolume = root.Q<Slider>("slider-music-volume");
            _toggleFullscreen = root.Q<Toggle>("toggle-fullscreen");
            _toggleCameraShake = root.Q<Toggle>("toggle-camera-shake");
            _dropdownQuality = root.Q<DropdownField>("dropdown-quality");
            _dropdownLanguage = root.Q<DropdownField>("dropdown-language");

            if (_dropdownLanguage != null)
            {
                _dropdownLanguage.RegisterValueChangedCallback(evt =>
                {
                    ChangeLanguage(evt.newValue);
                });
            }

            // Ayarlar Sekmeleri
            _btnTabAudio = root.Q<Button>("btn-tab-audio");
            _btnTabGraphics = root.Q<Button>("btn-tab-graphics");
            _btnTabGameplay = root.Q<Button>("btn-tab-gameplay");
            _contentAudio = root.Q<VisualElement>("content-audio");
            _contentGraphics = root.Q<VisualElement>("content-graphics");
            _contentGameplay = root.Q<VisualElement>("content-gameplay");

            if (_btnTabAudio != null) _btnTabAudio.clicked += OnTabAudioClicked;
            if (_btnTabGraphics != null) _btnTabGraphics.clicked += OnTabGraphicsClicked;
            if (_btnTabGameplay != null) _btnTabGameplay.clicked += OnTabGameplayClicked;

            // Lobi Elemanları
            _lobbyBadge = root.Q<Label>("lobby-badge");
            _roomCodeVal = root.Q<Label>("room-code-val");
            _btnCopyCode = root.Q<Button>("btn-copy-code");
            _rosterTitle = root.Q<Label>("roster-title");
            _lblTeamRed = root.Q<Label>("lbl-team-red");
            _lblTeamBlue = root.Q<Label>("lbl-team-blue");
            _lblLobbyChat = root.Q<Label>("lbl-lobby-chat");
            _lblPing = root.Q<Label>("ping-label");
            
            _homeRigLogo = root.Q<Label>("home-rig-logo");
            _homeIdLabel = root.Q<Label>("home-id-label");
            _homeBadgeRoomCenter = root.Q<Label>("home-badge-room-center");
            _homeDescCreate = root.Q<Label>("home-desc-create");
            _homeRoomCodeLbl = root.Q<Label>("home-room-code-lbl");
            _homeBadgeLiveFields = root.Q<Label>("home-badge-live-fields");
            _homeDescLive = root.Q<Label>("home-desc-live");
            _homeEmptyTitle = root.Q<Label>("home-empty-title");
            _homeEmptyDesc = root.Q<Label>("home-empty-desc");
            _lobbyLogoLbl = root.Q<Label>("lobby-logo-lbl");
            
            _btnStartGame = root.Q<Button>("btn-start-game");
            _btnLeaveLobby = root.Q<Button>("btn-leave-lobby");

            // Slotlar
            _slot1Card = root.Q<VisualElement>("slot-1-card");
            _slot1Name = root.Q<Label>("slot-1-name");
            _slot2Card = root.Q<VisualElement>("slot-2-card");
            _slot2Name = root.Q<Label>("slot-2-name");
            _slot2AvatarTag = root.Q<Label>("slot-2-avatar-tag");
            _slot2Circle = root.Q<VisualElement>("slot-2-circle");
            _slot1Ready = root.Q<VisualElement>("slot-1-ready");
            _slot1ReadyLbl = root.Q<Label>("slot-1-ready-lbl");
            _slot2Ready = root.Q<VisualElement>("slot-2-ready");
            _slot2ReadyLbl = root.Q<Label>("slot-2-ready-lbl");
            _slot3Card = root.Q<VisualElement>("slot-3-card");
            _slot3Name = root.Q<Label>("slot-3-name");
            _slot3AvatarTag = root.Q<Label>("slot-3-avatar-tag");
            _slot3Circle = root.Q<VisualElement>("slot-3-circle");
            _slot3Ready = root.Q<VisualElement>("slot-3-ready");
            _slot3ReadyLbl = root.Q<Label>("slot-3-ready-lbl");
            _slot4Card = root.Q<VisualElement>("slot-4-card");
            _slot4Name = root.Q<Label>("slot-4-name");
            _slot4AvatarTag = root.Q<Label>("slot-4-avatar-tag");
            _slot4Circle = root.Q<VisualElement>("slot-4-circle");
            _slot4Ready = root.Q<VisualElement>("slot-4-ready");
            _slot4ReadyLbl = root.Q<Label>("slot-4-ready-lbl");

            _btnInvite1 = root.Q<Button>("btn-invite-1");

            // Sohbet
            _chatScroll = root.Q<ScrollView>("chat-scroll");
            _chatInput = root.Q<TextField>("chat-input");
            _btnChatSend = root.Q<Button>("btn-chat-send");

            // --- OLAY BAĞLANTILARI ---
            if (_btnHomeCreateRoom != null) _btnHomeCreateRoom.clicked += OnCreateRoomClicked;
            if (_btnHomeJoinCode != null) _btnHomeJoinCode.clicked += OnJoinWithCodeClicked;
            if (_btnHomeQuit != null) _btnHomeQuit.clicked += OnQuitClicked;
            if (_btnHomeSettings != null) _btnHomeSettings.clicked += OpenSettings;
            if (_btnCloseSettings != null) _btnCloseSettings.clicked += CloseSettings;

            if (_btnStartGame != null) _btnStartGame.clicked += OnStartGameClicked;
            if (_btnLeaveLobby != null) _btnLeaveLobby.clicked += OnLeaveLobbyClicked;
            if (_btnCopyCode != null) _btnCopyCode.clicked += OnCopyCodeClicked;
            if (_btnChatSend != null) _btnChatSend.clicked += OnChatSendClicked;

            if (_inputPlayerName != null)
            {
                _inputPlayerName.RegisterValueChangedCallback(evt =>
                {
                    if (!string.IsNullOrWhiteSpace(evt.newValue))
                    {
                        _localPlayerName = evt.newValue.Trim();
                    }
                });
            }

            if (_chatInput != null)
            {
                _chatInput.RegisterCallback<KeyDownEvent>(evt =>
                {
                    if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                    {
                        OnChatSendClicked();
                        evt.StopPropagation();
                        
                        // Mesaj gittikten sonra odak tekrar inputta kalsın (ard arda mesaj yazabilmek için gecikmeli odak)
                        _chatInput.schedule.Execute(() => _chatInput.Focus()).StartingIn(50);
                    }
                }, TrickleDown.TrickleDown);
            }

            InitializePlayerIdentity();
            SwitchView(false); // Başlangıçta daima Ana Sayfa
            RefreshRoomsListUI();

            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        }

        private void Start()
        {
            if (ArixonNetworkManager.Instance != null)
            {
                ArixonNetworkManager.Instance.OnNetworkLog += OnNetworkLogReceived;
            }

            RegisterNetworkHandlers();

            // Kayıtlı dili yükle
            StartCoroutine(SetLocaleRoutine(PlayerPrefs.GetString("LanguageCode", "tr")));
        }

        private void Update()
        {
            // 1. Ana Sayfadayken odalar listesini periyodik olarak canlı tara
            if (_viewHome != null && _viewHome.style.display != DisplayStyle.None)
            {
                _roomRefreshTimer += Time.deltaTime;
                if (_roomRefreshTimer >= 0.7f)
                {
                    _roomRefreshTimer = 0f;
                    RefreshRoomsListUI();
                }
            }

            // 2. Lobi ekranındayken ağ durumunu anlık kontrol et ve slotları canlı tut
            if (_viewLobby != null && _viewLobby.style.display != DisplayStyle.None)
            {
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                {
                    if (NetworkManager.Singleton.IsServer)
                    {
                        // Kalp atışı gönder (odanın keşif listesinde canlı kalması için)
                        _heartbeatTimer += Time.deltaTime;
                        if (_heartbeatTimer >= 2f)
                        {
                            _heartbeatTimer = 0f;
                            ArixonRoomDiscovery.KeepAlive(_currentRoomCode);
                        }

                        int playerCount = NetworkManager.Singleton.ConnectedClientsList.Count;
                        if (_rosterTitle != null) _rosterTitle.text = $"{GetLoc("ROSTER_TITLE")} ({playerCount}/4)";
                        
                        if (playerCount < 2) _playerReadyStates.Clear();
                        
                        CheckAllReadyAndEnableStart();
                        UpdateRosterReadyUI();

                        // Discovery listesindeki oyuncu sayısını senkronize tut
                        ArixonRoomDiscovery.UpdatePlayerCount(_currentRoomCode, playerCount);
                    }
                }
            }
        }

        private void OnDestroy()
        {
            if (Unity.Netcode.NetworkManager.Singleton != null)
            {
                Unity.Netcode.NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
                Unity.Netcode.NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            }
        }

        private void OnDisable()
        {
            if (_btnHomeCreateRoom != null) _btnHomeCreateRoom.clicked -= OnCreateRoomClicked;
            if (_btnHomeJoinCode != null) _btnHomeJoinCode.clicked -= OnJoinWithCodeClicked;
            if (_btnHomeQuit != null) _btnHomeQuit.clicked -= OnQuitClicked;
            if (_btnHomeSettings != null) _btnHomeSettings.clicked -= OpenSettings;
            if (_btnCloseSettings != null) _btnCloseSettings.clicked -= CloseSettings;
            
            if (_btnTabAudio != null) _btnTabAudio.clicked -= OnTabAudioClicked;
            if (_btnTabGraphics != null) _btnTabGraphics.clicked -= OnTabGraphicsClicked;
            if (_btnTabGameplay != null) _btnTabGameplay.clicked -= OnTabGameplayClicked;

            if (_btnStartGame != null) _btnStartGame.clicked -= OnStartGameClicked;
            if (_btnLeaveLobby != null) _btnLeaveLobby.clicked -= OnLeaveLobbyClicked;
            if (_btnCopyCode != null) _btnCopyCode.clicked -= OnCopyCodeClicked;
            if (_btnChatSend != null) _btnChatSend.clicked -= OnChatSendClicked;

            if (ArixonNetworkManager.Instance != null)
            {
                ArixonNetworkManager.Instance.OnNetworkLog -= OnNetworkLogReceived;
            }

            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;

            if (_isHost && !string.IsNullOrEmpty(_currentRoomCode))
            {
                ArixonRoomDiscovery.UnpublishRoom(_currentRoomCode);
            }
        }

        private void OnApplicationQuit()
        {
            if (_isHost && !string.IsNullOrEmpty(_currentRoomCode))
            {
                ArixonRoomDiscovery.UnpublishRoom(_currentRoomCode);
            }
        }

        private void InitializePlayerIdentity()
        {
            // [STEAM ENTEGRASYON HAZIRLIĞI]
            // İleride Steamworks.NET eklendiğinde burası: SteamFriends.GetPersonaName() olacak.
            // Şimdilik çakışmayı önlemek için geçici benzersiz (unique) isimler üretiyoruz.
            
            bool isMainEditor = true;
#if UNITY_EDITOR
            isMainEditor = CurrentPlayer.IsMainEditor;
#endif

            if (isMainEditor)
            {
                int randomId = UnityEngine.Random.Range(1000, 9999);
                _localPlayerName = $"SteamUser_{randomId}";
                _isHost = true;
            }
            else
            {
                _localPlayerName = "MPPM_Clone";
                _isHost = false;
            }

            if (_inputPlayerName != null) 
            {
                _inputPlayerName.value = _localPlayerName;
            }
        }

        private void SwitchView(bool toLobby)
        {
            SwitchView(toLobby ? "lobby" : "home");
        }

        private void SwitchView(string viewName)
        {
            if (_viewHome != null) _viewHome.style.display = (viewName == "home") ? DisplayStyle.Flex : DisplayStyle.None;
            if (_viewLobby != null) _viewLobby.style.display = (viewName == "lobby") ? DisplayStyle.Flex : DisplayStyle.None;
            if (_viewSettings != null) _viewSettings.style.display = (viewName == "settings") ? DisplayStyle.Flex : DisplayStyle.None;

#if UNITY_EDITOR
            if (Application.isPlaying) CaptureAgentVision(viewName);
#endif
        }

#if UNITY_EDITOR
        private void CaptureAgentVision(string pageName)
        {
            StartCoroutine(CaptureAgentVisionRoutine(pageName));
        }
        
        private System.Collections.IEnumerator CaptureAgentVisionRoutine(string pageName)
        {
            yield return new WaitForSeconds(0.2f); // UI'ın renderlanmasını (yerleşmesini) bekle
            string basePath = System.IO.Directory.GetParent(Application.dataPath).FullName;
            string dir = System.IO.Path.Combine(basePath, "AgentVision");
            if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
            
            string path = System.IO.Path.Combine(dir, $"Page_{pageName}.png");
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log($"[Agent Vision] {pageName} sayfasının görüntüsü başarıyla alındı: {path}");
        }
#endif

        #region Ayarlar (Settings) Menüsü

        private void OpenSettings()
        {
            SwitchView("settings");
            SwitchSettingsTab("AUDIO"); // Varsayılan olarak Ses sekmesiyle başla
            
            // Mevcut ayarları yükle
            if (_sliderMasterVolume != null) _sliderMasterVolume.value = PlayerPrefs.GetFloat("MasterVolume", 100f);
            if (_sliderMusicVolume != null) _sliderMusicVolume.value = PlayerPrefs.GetFloat("MusicVolume", 80f);
            if (_toggleFullscreen != null) _toggleFullscreen.value = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
            if (_toggleCameraShake != null) _toggleCameraShake.value = PlayerPrefs.GetInt("CameraShake", 1) == 1;
            if (_dropdownQuality != null) _dropdownQuality.index = PlayerPrefs.GetInt("QualityIndex", 2);
            if (_dropdownLanguage != null) 
            {
                string savedLang = PlayerPrefs.GetString("LanguageCode", "tr");
                if (savedLang == "en")
                    _dropdownLanguage.index = 0;
                else
                    _dropdownLanguage.index = 1;
            }
        }

        private void CloseSettings()
        {
            // Yeni ayarları kaydet
            if (_sliderMasterVolume != null) PlayerPrefs.SetFloat("MasterVolume", _sliderMasterVolume.value);
            if (_sliderMusicVolume != null) PlayerPrefs.SetFloat("MusicVolume", _sliderMusicVolume.value);
            if (_toggleFullscreen != null) PlayerPrefs.SetInt("Fullscreen", _toggleFullscreen.value ? 1 : 0);
            if (_toggleCameraShake != null) PlayerPrefs.SetInt("CameraShake", _toggleCameraShake.value ? 1 : 0);
            if (_dropdownQuality != null) PlayerPrefs.SetInt("QualityIndex", _dropdownQuality.index);
            // Dil değişikliği anında (ChangeLanguage) yapıldığı için burada sadece diğerleri kaydediliyor
            
            PlayerPrefs.Save();
            ApplySettings();
            
            SwitchView("home");
        }

        private void OnTabAudioClicked() => SwitchSettingsTab("AUDIO");
        private void OnTabGraphicsClicked() => SwitchSettingsTab("GRAPHICS");
        private void OnTabGameplayClicked() => SwitchSettingsTab("GAMEPLAY");

        private void SwitchSettingsTab(string tabName)
        {
            if (_contentAudio != null) _contentAudio.style.display = (tabName == "AUDIO") ? DisplayStyle.Flex : DisplayStyle.None;
            if (_contentGraphics != null) _contentGraphics.style.display = (tabName == "GRAPHICS") ? DisplayStyle.Flex : DisplayStyle.None;
            if (_contentGameplay != null) _contentGameplay.style.display = (tabName == "GAMEPLAY") ? DisplayStyle.Flex : DisplayStyle.None;

            if (_btnTabAudio != null)
            {
                if (tabName == "AUDIO") _btnTabAudio.AddToClassList("active-tab");
                else _btnTabAudio.RemoveFromClassList("active-tab");
            }
            if (_btnTabGraphics != null)
            {
                if (tabName == "GRAPHICS") _btnTabGraphics.AddToClassList("active-tab");
                else _btnTabGraphics.RemoveFromClassList("active-tab");
            }
            if (_btnTabGameplay != null)
            {
                if (tabName == "GAMEPLAY") _btnTabGameplay.AddToClassList("active-tab");
                else _btnTabGameplay.RemoveFromClassList("active-tab");
            }
        }

        private void ApplySettings()
        {
            // Tam Ekran
            bool isFullscreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
            Screen.fullScreen = isFullscreen;
            
            // Kalite
            int qualityIndex = PlayerPrefs.GetInt("QualityIndex", 2);
            QualitySettings.SetQualityLevel(qualityIndex, true);
            
            // Ana Ses
            float masterVol = PlayerPrefs.GetFloat("MasterVolume", 100f) / 100f;
            AudioListener.volume = masterVol;
        }

        private void ChangeLanguage(string languageName)
        {
            Debug.Log($"[Localization] Arayüzden dil seçimi değişti: {languageName}");
            string code = (languageName == "English") ? "en" : "tr";
            StartCoroutine(SetLocaleRoutine(code));
        }

        private System.Collections.IEnumerator SetLocaleRoutine(string code)
        {
            Debug.Log($"[Localization] Dil yükleniyor... Hedef Kod: {code}");
            yield return LocalizationSettings.InitializationOperation;
            
            if (LocalizationSettings.AvailableLocales != null)
            {
                var locales = LocalizationSettings.AvailableLocales.Locales;
                bool found = false;
                for (int i = 0; i < locales.Count; i++)
                {
                    if (locales[i].Identifier.Code == code)
                    {
                        LocalizationSettings.SelectedLocale = locales[i];
                        PlayerPrefs.SetString("LanguageCode", code);
                        PlayerPrefs.Save();
                        Debug.Log($"[Localization] Dil başarıyla değiştirildi! Yeni Dil: {code}");
                        found = true;
                        break;
                    }
                }
                if (!found) Debug.LogError($"[Localization] HATA: '{code}' kodlu dil bulunamadı!");
            }
        }

        private void OnLocaleChanged(Locale locale)
        {
            UpdateLocalizedTexts();
        }

        private string GetLoc(string key, params object[] args)
        {
            try 
            {
                var str = LocalizationSettings.StringDatabase.GetLocalizedString("UITexts", key, arguments: args);
                if (string.IsNullOrEmpty(str) || str.StartsWith("No translation"))
                {
                    return key;
                }
                return str;
            }
            catch 
            {
                return key; 
            }
        }

        private void UpdateLocalizedTexts()
        {
            var root = _uiDocument.rootVisualElement;
            if (root == null) return;

            // Ayarlar
            var settingsTitle = root.Q<Label>(className: "settings-main-title");
            if (settingsTitle != null) settingsTitle.text = GetLoc("SETTINGS");

            if (_btnCloseSettings != null) _btnCloseSettings.text = GetLoc("BACK");
            if (_btnTabAudio != null) _btnTabAudio.text = GetLoc("AUDIO");
            if (_btnTabGraphics != null) _btnTabGraphics.text = GetLoc("GRAPHICS");
            if (_btnTabGameplay != null) _btnTabGameplay.text = GetLoc("GAMEPLAY");
            
            // Home Screen Translations
            if (_homeRigLogo != null) _homeRigLogo.text = GetLoc("HOME_ARENA_TITLE");
            if (_homeIdLabel != null) _homeIdLabel.text = GetLoc("HOME_ID_CARD");
            if (_homeBadgeRoomCenter != null) _homeBadgeRoomCenter.text = GetLoc("HOME_ROOM_CENTER");
            if (_homeDescCreate != null) _homeDescCreate.text = GetLoc("HOME_DESC_CREATE");
            if (_btnHomeCreateRoom != null) _btnHomeCreateRoom.text = GetLoc("HOME_CREATE_MATCH");
            if (_homeRoomCodeLbl != null) _homeRoomCodeLbl.text = GetLoc("ROOM_CODE");
            if (_btnHomeJoinCode != null) _btnHomeJoinCode.text = GetLoc("JOIN_BTN");
            if (_homeBadgeLiveFields != null) _homeBadgeLiveFields.text = GetLoc("HOME_LIVE_FIELDS");
            if (_homeDescLive != null) _homeDescLive.text = GetLoc("HOME_DESC_LIVE");
            if (_homeEmptyTitle != null) _homeEmptyTitle.text = GetLoc("HOME_EMPTY_TITLE");
            if (_homeEmptyDesc != null) _homeEmptyDesc.text = GetLoc("HOME_EMPTY_DESC");
            if (_btnHomeQuit != null) _btnHomeQuit.text = GetLoc("HOME_QUIT");
            if (_btnHomeSettings != null) _btnHomeSettings.text = GetLoc("SETTINGS");

            var rowLabels = root.Query<Label>(className: "settings-row-label").ToList();
            if (rowLabels.Count >= 6)
            {
                rowLabels[0].text = GetLoc("MASTER_VOLUME");
                rowLabels[1].text = GetLoc("MUSIC_VOLUME");
                rowLabels[2].text = GetLoc("FULLSCREEN");
                rowLabels[3].text = GetLoc("GRAPHICS_QUALITY");
                rowLabels[4].text = GetLoc("LANGUAGE_TITLE");
                rowLabels[5].text = GetLoc("CAMERA_SHAKE");
            }
            
            if (_dropdownQuality != null)
            {
                _dropdownQuality.choices = new System.Collections.Generic.List<string> {
                    GetLoc("QUALITY_LOW"),
                    GetLoc("QUALITY_MEDIUM"),
                    GetLoc("QUALITY_HIGH"),
                    GetLoc("QUALITY_ULTRA")
                };
                
                // Seçili olan indexin text'ini güncellemek için
                if (_dropdownQuality.index >= 0 && _dropdownQuality.index < _dropdownQuality.choices.Count)
                    _dropdownQuality.value = _dropdownQuality.choices[_dropdownQuality.index];
            }

            // Lobi
            if (_lobbyBadge != null) 
            {
                if (_isHost) _lobbyBadge.text = GetLoc("LOBBY_LEADER");
                else _lobbyBadge.text = GetLoc("PARTICIPANT");
            }
            
            var crownLabels = root.Query<Label>(className: "crown-text").ToList();
            foreach (var crownLbl in crownLabels)
            {
                crownLbl.text = GetLoc("LOBBY_LEADER");
            }
            
            if (_lblTeamRed != null) _lblTeamRed.text = GetLoc("TEAM_RED");
            if (_lblTeamBlue != null) _lblTeamBlue.text = GetLoc("TEAM_BLUE");
            if (_lblLobbyChat != null) _lblLobbyChat.text = GetLoc("LOBBY_CHAT");
            if (_btnCopyCode != null) _btnCopyCode.text = GetLoc("COPY_BTN");
            if (_lblPing != null) _lblPing.text = GetLoc("PING_CONNECTING");
            if (_lobbyLogoLbl != null) _lobbyLogoLbl.text = GetLoc("LOBBY_LOGO");
            if (_btnChatSend != null) _btnChatSend.text = GetLoc("CHAT_SEND");
            if (_btnLeaveLobby != null) _btnLeaveLobby.text = GetLoc("LEAVE_LOBBY_BTN");

            var roomCodeLbls = root.Query<Label>(className: "room-code-label").ToList();
            if (roomCodeLbls.Count > 0) roomCodeLbls[0].text = GetLoc("ROOM_CODE");

            if (_btnCopyCode != null) _btnCopyCode.text = GetLoc("COPY");

            var teamRed = root.Q<VisualElement>(className: "team-badge-red")?.Q<Label>();
            if (teamRed != null) teamRed.text = GetLoc("TEAM_RED");
            
            var teamBlue = root.Q<VisualElement>(className: "team-badge-blue")?.Q<Label>();
            if (teamBlue != null) teamBlue.text = GetLoc("TEAM_BLUE");

            if (_btnLeaveLobby != null) _btnLeaveLobby.text = GetLoc("BACK_HOME");
            
            if (_btnStartGame != null)
            {
                if (_isHost) _btnStartGame.text = GetLoc("START_GAME");
                else _btnStartGame.text = GetLoc("WAITING_LEADER");
            }

            // Sohbet
            var chatBadge = root.Q<VisualElement>(className: "pink-badge")?.Q<Label>();
            if (chatBadge != null) chatBadge.text = GetLoc("LOBBY_CHAT");

            var sysSender = root.Q<Label>(className: "sender-system");
            if (sysSender != null) sysSender.text = GetLoc("SYSTEM");

            var sysMsg = root.Q<Label>(className: "msg-text-system");
            if (sysMsg != null) sysMsg.text = GetLoc("WELCOME_CHAT");
            
            if (_btnChatSend != null) _btnChatSend.text = GetLoc("SEND");

            // Roster Title Force Update
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                int playerCount = NetworkManager.Singleton.ConnectedClientsList.Count;
                if (_rosterTitle != null) _rosterTitle.text = $"{GetLoc("ROSTER_TITLE")} ({playerCount}/4)";
            }
        }

        #endregion

        #region Dinamik Canlı Oda Keşif Sistemi (Room Discovery)

        /// <summary>
        /// Ağda gerçekten açılmış olan odaları dinamik olarak arayüze döker.
        /// </summary>
        private void RefreshRoomsListUI()
        {
            if (_roomsScroll == null) return;

            var activeRooms = ArixonRoomDiscovery.GetActiveRooms();

            if (activeRooms == null || activeRooms.Count == 0)
            {
                // Oda yoksa sevimli cartoon boş kutusunu göster
                if (_emptyRoomsBox != null) _emptyRoomsBox.style.display = DisplayStyle.Flex;

                for (int i = _roomsScroll.childCount - 1; i >= 0; i--)
                {
                    var child = _roomsScroll[i];
                    if (child != _emptyRoomsBox)
                    {
                        _roomsScroll.RemoveAt(i);
                    }
                }
                return;
            }

            // Oda varsa boş kutusunu gizle
            if (_emptyRoomsBox != null) _emptyRoomsBox.style.display = DisplayStyle.None;

            // Önceki dinamik oda kartlarını temizle
            for (int i = _roomsScroll.childCount - 1; i >= 0; i--)
            {
                var child = _roomsScroll[i];
                if (child != _emptyRoomsBox)
                {
                    _roomsScroll.RemoveAt(i);
                }
            }

            // Her aktif oda için canlı cartoon oda kartı üret
            foreach (var room in activeRooms)
            {
                var item = new VisualElement();
                item.AddToClassList("room-item");
                bool isOpen = !room.isGameStarted && room.currentPlayers < room.maxPlayers;
                item.AddToClassList(isOpen ? "room-open" : "room-closed");

                // Sol Bilgiler (Rozet, İsim, Kod)
                var left = new VisualElement();
                left.AddToClassList("room-item-left");

                var badge = new Label(isOpen ? GetLoc("ROOM_OPEN") : (room.isGameStarted ? GetLoc("ROOM_IN_GAME") : GetLoc("ROOM_FULL_BADGE")));
                badge.AddToClassList("room-badge-status");
                badge.AddToClassList(isOpen ? "badge-open" : "badge-closed");

                var nameLbl = new Label(string.Format(GetLoc("ROOM_NAME_FORMAT"), room.hostName));
                nameLbl.AddToClassList("room-item-name");

                var codeLbl = new Label(room.roomCode);
                codeLbl.AddToClassList("room-item-code");

                left.Add(badge);
                left.Add(nameLbl);
                left.Add(codeLbl);

                // Sağ Bilgiler (Oyuncu Sayısı, Katıl Butonu)
                var right = new VisualElement();
                right.AddToClassList("room-item-right");

                var countLbl = new Label($"👥 {room.currentPlayers}/{room.maxPlayers}");
                countLbl.AddToClassList("room-item-players");
                right.Add(countLbl);

                if (isOpen)
                {
                    var joinBtn = new Button();
                    joinBtn.text = GetLoc("JOIN_BTN");
                    joinBtn.AddToClassList("btn-room-join");
                    string targetCode = room.roomCode;
                    string targetHost = room.hostName;
                    joinBtn.clicked += () => JoinRoom(targetCode, targetHost);
                    right.Add(joinBtn);
                }

                item.Add(left);
                item.Add(right);

                _roomsScroll.Add(item);
            }
        }

        #endregion

        #region Ana Sayfa İşlemleri (Oda Kur / Kodla Katıl)

        private void OnCreateRoomClicked()
        {
            if (_inputPlayerName != null && !string.IsNullOrWhiteSpace(_inputPlayerName.value))
            {
                _localPlayerName = _inputPlayerName.value.Trim();
            }

            _currentRoomCode = ArixonRoomDiscovery.GenerateRoomCode();
            _isHost = true;
            _hostPlayerName = _localPlayerName;

            SetHomeStatus("Oda kuruluyor (Host başlatılıyor)...");

            int assignedPort = 7777;
            if (Unity.Netcode.NetworkManager.Singleton != null)
            {
                var transport = Unity.Netcode.NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
                if (transport != null)
                {
                    assignedPort = UnityEngine.Random.Range(7000, 8999);
                    transport.SetConnectionData("127.0.0.1", (ushort)assignedPort);
                }
            }

            if (ArixonNetworkManager.Instance != null)
            {
                ArixonNetworkManager.Instance.StartHost();
            }
            else if (Unity.Netcode.NetworkManager.Singleton != null)
            {
                Unity.Netcode.NetworkManager.Singleton.StartHost();
            }

            // KULAKLIKLARI (DİNLEYİCİLERİ) AĞ BAŞLADIKTAN HEMEN SONRA TAK!
            RegisterNetworkHandlers();

            ArixonRoomDiscovery.PublishRoom(_currentRoomCode, _localPlayerName, assignedPort);

            // HOST kendini manuel olarak listeye eklesin
            ulong hostId = Unity.Netcode.NetworkManager.ServerClientId;
            _playerTeams[hostId] = "RED";
            _playerReadyStates[hostId] = false;
            _playerNames[hostId] = _localPlayerName;

            PrepareLobbyViewAsHost();
            SwitchView(true);
            
            BroadcastRoster(); // Kendi bilgisini lobidekilere ve kendi ekranına yansıt
            
            AddMessageToChat(GetLoc("SYSTEM"), $"{GetLoc("HOST_STARTING")} {GetLoc("ROOM_CODE")} {_currentRoomCode}", true);
        }

        private void OnJoinWithCodeClicked()
        {
            string code = _inputRoomCode != null ? _inputRoomCode.value.Trim() : "";
            if (string.IsNullOrEmpty(code))
            {
                SetHomeStatus(GetLoc("ENTER_VALID_CODE"));
                return;
            }

            // Odanın gerçekten var olup olmadığını kontrol et
            var room = ArixonRoomDiscovery.FindRoom(code);
            if (room == null)
            {
                SetHomeStatus($"{GetLoc("ROOM_NOT_FOUND")} '{code}'");
                return;
            }

            if (room.isGameStarted)
            {
                SetHomeStatus($"{GetLoc("ROOM_STARTED")} '{code}'!");
                return;
            }

            if (room.currentPlayers >= room.maxPlayers)
            {
                SetHomeStatus($"{GetLoc("ROOM_FULL")} '{code}'");
                return;
            }

            JoinRoom(code, room.hostName);
        }

        private void JoinRoom(string roomCode, string hostName = "Oyuncu_01")
        {
            if (_inputPlayerName != null && !string.IsNullOrWhiteSpace(_inputPlayerName.value))
            {
                _localPlayerName = _inputPlayerName.value.Trim();
            }

            _currentRoomCode = roomCode;
            _isHost = false;
            _hostPlayerName = hostName;

            SetHomeStatus($"'{roomCode}' odasına bağlanılıyor...");

            var room = ArixonRoomDiscovery.FindRoom(roomCode);
            int targetPort = (room != null) ? room.port : 7777;

            if (Unity.Netcode.NetworkManager.Singleton != null)
            {
                var transport = Unity.Netcode.NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
                if (transport != null)
                {
                    transport.SetConnectionData(room != null ? room.ipAddress : "127.0.0.1", (ushort)targetPort);
                }
            }

            if (ArixonNetworkManager.Instance != null)
            {
                ArixonNetworkManager.Instance.StartClient();
            }
            else if (Unity.Netcode.NetworkManager.Singleton != null)
            {
                Unity.Netcode.NetworkManager.Singleton.StartClient();
            }

            // KULAKLIKLARI (DİNLEYİCİLERİ) AĞ BAŞLADIKTAN SONRA TAK!
            RegisterNetworkHandlers();

            PrepareLobbyViewAsClient();
            SwitchView(true);

            AddMessageToChat(GetLoc("SYSTEM"), GetLoc("CHAT_SYSTEM_JOIN", _currentRoomCode, _localPlayerName), true);
        }

        private void SetHomeStatus(string message)
        {
            if (_homeStatusMsg != null)
            {
                _homeStatusMsg.text = message;
            }
        }

        #endregion

        #region Lobi Ekranı Yapılandırması

        private void PrepareLobbyViewAsHost()
        {
            if (_roomCodeVal != null) _roomCodeVal.text = _currentRoomCode;
            if (_lobbyBadge != null) _lobbyBadge.text = _isHost ? GetLoc("LOBBY_LEADER") : GetLoc("PARTICIPANT");
            if (_slot1Name != null) _slot1Name.text = $"{_localPlayerName}{GetLoc("YOU_POSTFIX")}";
            if (_rosterTitle != null) _rosterTitle.text = $"{GetLoc("ROSTER_TITLE")} (1/4)";

            if (_btnStartGame != null)
            {
                _btnStartGame.text = GetLoc("START_GAME");
                _btnStartGame.SetEnabled(true);
            }

            _playerReadyStates.Clear();
            CheckAllReadyAndEnableStart();
            UpdateRosterReadyUI();
        }

        private void PrepareLobbyViewAsClient()
        {
            if (_roomCodeVal != null) _roomCodeVal.text = _currentRoomCode;
            if (_lobbyBadge != null) _lobbyBadge.text = GetLoc("PARTICIPANT");
            if (_slot1Name != null) _slot1Name.text = $"{_hostPlayerName}{GetLoc("LEADER_POSTFIX")}";
            if (_rosterTitle != null) _rosterTitle.text = $"{GetLoc("ROSTER_TITLE")} (2/4)";

            if (_btnStartGame != null)
            {
                _btnStartGame.text = GetLoc("WAITING_LEADER");
                _btnStartGame.SetEnabled(false);
            }

            _isLocalPlayerReady = false;
            UpdateClientReadyButtonUI();
        }

        // SetSlot2State mantığı 4 oyunculu FillSlot ve UpdateRosterReadyUI içerisine taşındığı için kaldırıldı.

        #endregion

        #region Canlı Ağ Mesajlaşması ve Sohbet (NGO CustomMessagingManager)

        private void RegisterNetworkHandlers()
        {
            if (NetworkManager.Singleton == null || NetworkManager.Singleton.CustomMessagingManager == null) return;

            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;


            // Çoklu kayıtları önlemek için önce temizleyelim

            NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler(ROSTER_CHANNEL);
            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(ROSTER_CHANNEL, (senderClientId, reader) =>
            {
                reader.ReadValueSafe(out byte msgType);
                if (msgType == 1 && NetworkManager.Singleton.IsClient) // 1 = Roster Update (Server'dan Client'a)
                {
                    reader.ReadValueSafe(out int count);
                    _playerNames.Clear();
                    _playerTeams.Clear();
                    _playerReadyStates.Clear();
                    for(int i = 0; i < count; i++)
                    {
                        reader.ReadValueSafe(out ulong cId);
                        reader.ReadValueSafe(out string pName);
                        reader.ReadValueSafe(out string pTeam);
                        reader.ReadValueSafe(out bool cReady);
                        
                        _playerNames[cId] = pName;
                        _playerTeams[cId] = pTeam;
                        _playerReadyStates[cId] = cReady;
                    }
                    UpdateRosterReadyUI(); // Arayüzü güncelle
                }
                else if (msgType == 2 && NetworkManager.Singleton.IsServer) // 2 = Switch Team Request (Client'tan Server'a)
                {
                    string current = _playerTeams.ContainsKey(senderClientId) ? _playerTeams[senderClientId] : "RED";
                    _playerTeams[senderClientId] = current == "RED" ? "BLUE" : "RED";
                    BroadcastRoster(); // Değişen takımı herkese bildir
                }
            });
            
            NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler(READY_CHANNEL_C2S);
            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(READY_CHANNEL_C2S, (senderClientId, reader) =>
            {
                if (NetworkManager.Singleton.IsServer)
                {
                    // İstemci durumunu sunucuya atıyor
                    reader.ReadValueSafe(out bool isReady);
                    _playerReadyStates[senderClientId] = isReady;
                    CheckAllReadyAndEnableStart();
                    BroadcastReadyStates();
                }
            });

            NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler(READY_CHANNEL_S2C);
            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(READY_CHANNEL_S2C, (senderClientId, reader) =>
            {
                if (NetworkManager.Singleton.IsClient)
                {
                    // Sunucu tüm kadroyu istemciye atıyor
                    reader.ReadValueSafe(out int count);
                    _playerReadyStates.Clear();
                    for(int i=0; i<count; i++) {
                        reader.ReadValueSafe(out ulong cId);
                        reader.ReadValueSafe(out bool cReady);
                        _playerReadyStates[cId] = cReady;
                    }
                    UpdateRosterReadyUI();
                }
            });
            
            NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler(SYNC_NAMES_CHANNEL);
            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(SYNC_NAMES_CHANNEL, (senderClientId, reader) =>
            {
                if (NetworkManager.Singleton.IsServer)
                {
                    // Yeni bağlanan Client ismini yolladı
                    reader.ReadValueSafe(out string pName);
                    _playerNames[senderClientId] = pName;
                    BroadcastRoster();
                }
            });

            NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler(CHAT_CHANNEL);

            // Sohbet Mesajı Alındığında
            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(CHAT_CHANNEL, (senderClientId, reader) =>
            {
                reader.ReadValueSafe(out string senderName);
                reader.ReadValueSafe(out string messageText);
                reader.ReadValueSafe(out bool isSystem);

                AddMessageToChat(senderName, messageText, isSystem);

                // Eğer Server isek mesajı tüm client'lara dağıt (Relay/Broadcast)
                if (NetworkManager.Singleton.IsServer)
                {
                    BroadcastChatMessage(senderName, messageText, isSystem);
                }
            });
        }

        private void OnChatSendClicked()
        {
            if (_chatInput == null) return;
            string text = _chatInput.value.Trim();
            if (string.IsNullOrEmpty(text)) return;

            // Yazıyı anında sıfırla (Çift tıklama vb durumları engellemek için)
            _chatInput.SetValueWithoutNotify("");

            // Ağ üzerinden gönder
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                if (NetworkManager.Singleton.IsServer)
                {
                    // HOST (Player 1) isek:
                    // Mesajı kendi yerel ekranımıza ekleriz ve "sadece diğerlerine" (Broadcast) dağıtırız.
                    // Böylece SendNamedMessageToAll yüzünden kendi handler'ımızın tekrar tetiklenmesini ve 2 kere görünmesini engelleriz!
                    AddMessageToChat(_localPlayerName, text, false);
                    BroadcastChatMessage(_localPlayerName, text, false);
                }
                else if (NetworkManager.Singleton.IsClient)
                {
                    // CLIENT (Player 2) isek:
                    // Sadece Sunucu'ya gönderiyoruz. Yerel ekrana eklemiyoruz.
                    var writer = new FastBufferWriter(1024, Allocator.Temp);
                    using (writer)
                    {
                        writer.WriteValueSafe(_localPlayerName);
                        writer.WriteValueSafe(text);
                        writer.WriteValueSafe(false); // isSystem = false
                        NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(CHAT_CHANNEL, NetworkManager.ServerClientId, writer);
                    }
                }
            }
        }

        private void BroadcastChatMessage(string senderName, string messageText, bool isSystem, ulong excludeClientId = Unity.Netcode.NetworkManager.ServerClientId)
        {
            if (Unity.Netcode.NetworkManager.Singleton == null || !Unity.Netcode.NetworkManager.Singleton.IsServer || Unity.Netcode.NetworkManager.Singleton.CustomMessagingManager == null) return;

            // Mesajı tüm client'lara dağıtıyoruz.
            // Client'lar kendi mesajlarını yerelde basmadıkları için, bu mesajı aldıklarında çizecekler.
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                // Sunucu zaten kendi mesajını bastı, bu yüzden sunucuyu atla.
                if (client.ClientId == NetworkManager.ServerClientId) continue;

                var writer = new FastBufferWriter(1024, Allocator.Temp);
                using (writer)
                {
                    writer.WriteValueSafe(senderName);
                    writer.WriteValueSafe(messageText);
                    writer.WriteValueSafe(isSystem);
                    NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(CHAT_CHANNEL, client.ClientId, writer);
                }
            }
        }

        private void AddMessageToChat(string sender, string message, bool isSystem)
        {
            if (_chatScroll == null) return;

            var entry = new VisualElement();
            entry.AddToClassList("chat-entry");

            var senderLbl = new Label();
            var textLbl = new Label(message);

            if (isSystem)
            {
                entry.AddToClassList("system-entry");
                senderLbl.text = GetLoc("SYSTEM");
                senderLbl.AddToClassList("sender-system");
                textLbl.AddToClassList("msg-text-system");
            }
            else
            {
                senderLbl.text = $"{sender}:";
                senderLbl.AddToClassList(sender.Contains("01") || sender == _hostPlayerName ? "sender-p1" : "sender-p2");
                textLbl.style.color = new StyleColor(Color.white);
            }

            entry.Add(senderLbl);
            entry.Add(textLbl);
            _chatScroll.Add(entry);

            // En son mesaja kaydır
            _chatScroll.ScrollTo(entry);
        }

        #endregion

        
        
        private void OnClientConnected(ulong clientId)
        {
            if (Unity.Netcode.NetworkManager.Singleton.IsServer)
            {
                // Yeni oyuncu bağlandı (Server)
                _playerReadyStates[clientId] = false;
                
                // Müsait takımı bul
                int redCount = 0; int blueCount = 0;
                foreach(var kvp in _playerTeams) {
                    if (kvp.Value == "RED") redCount++;
                    else blueCount++;
                }
                _playerTeams[clientId] = redCount <= blueCount ? "RED" : "BLUE";

                BroadcastRoster();
            }
            else if (Unity.Netcode.NetworkManager.Singleton.IsClient && clientId == Unity.Netcode.NetworkManager.Singleton.LocalClientId)
            {
                // Client odaya katıldığında KENDİ ADINI Sunucuya bildirsin
                var writer = new Unity.Netcode.FastBufferWriter(64, Unity.Collections.Allocator.Temp);
                using (writer)
                {
                    writer.WriteValueSafe(_localPlayerName);
                    Unity.Netcode.NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(SYNC_NAMES_CHANNEL, Unity.Netcode.NetworkManager.ServerClientId, writer);
                }
            }
        }

        private void OnClientDisconnect(ulong clientId)
        {
            if (Unity.Netcode.NetworkManager.Singleton.IsServer)
            {
                // Birisi koptuysa (Server isek listelerden çıkar ve duyur)
                if (_playerNames.ContainsKey(clientId)) _playerNames.Remove(clientId);
                if (_playerTeams.ContainsKey(clientId)) _playerTeams.Remove(clientId);
                if (_playerReadyStates.ContainsKey(clientId)) _playerReadyStates.Remove(clientId);
                BroadcastRoster();
            }
            else
            {
                // Eğer Client isek ve lider koptuysa ana menüye dön
                if (clientId == Unity.Netcode.NetworkManager.ServerClientId || clientId == Unity.Netcode.NetworkManager.Singleton.LocalClientId)
                {
                    OnLeaveLobbyClicked();
                    SetHomeStatus(GetLoc("LEADER_LEFT"));
                }
            }
        }

        private void OnCardClicked(int slotIndex)
        {
            if (Unity.Netcode.NetworkManager.Singleton == null || !Unity.Netcode.NetworkManager.Singleton.IsListening) return;
            ulong myId = Unity.Netcode.NetworkManager.Singleton.LocalClientId;
            
            if (_isLocalPlayerReady)
            {
                _isLocalPlayerReady = false;
                UpdateClientReadyButtonUI();
            }

            if (Unity.Netcode.NetworkManager.Singleton.IsServer)
            {
                string current = _playerTeams.ContainsKey(myId) ? _playerTeams[myId] : "RED";
                _playerTeams[myId] = current == "RED" ? "BLUE" : "RED";
                BroadcastRoster();
            }
            else
            {
                var writer = new Unity.Netcode.FastBufferWriter(32, Unity.Collections.Allocator.Temp);
                using (writer)
                {
                    writer.WriteValueSafe((byte)2); // 2 = Switch Team Request
                    Unity.Netcode.NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage("ArixonRosterSync", Unity.Netcode.NetworkManager.ServerClientId, writer);
                }
            }
        }

        private void BroadcastRoster()
        {
            UpdateRosterReadyUI();
            if (Unity.Netcode.NetworkManager.Singleton == null || !Unity.Netcode.NetworkManager.Singleton.IsServer || Unity.Netcode.NetworkManager.Singleton.CustomMessagingManager == null) return;
            
            var writer = new Unity.Netcode.FastBufferWriter(1024, Unity.Collections.Allocator.Temp);
            using (writer)
            {
                writer.WriteValueSafe((byte)1); // 1 = Roster Update
                
                int count = _playerTeams.Count;
                writer.WriteValueSafe(count);
                
                foreach (var kvp in _playerTeams)
                {
                    ulong cId = kvp.Key;
                    writer.WriteValueSafe(cId);
                    writer.WriteValueSafe(_playerNames.ContainsKey(cId) ? _playerNames[cId] : $"Oyuncu_{cId}");
                    writer.WriteValueSafe(kvp.Value); // Takım bilgisi (RED / BLUE)
                    writer.WriteValueSafe(_playerReadyStates.ContainsKey(cId) && _playerReadyStates[cId]);
                }
                
                Unity.Netcode.NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll(ROSTER_CHANNEL, writer);
            }
        }

        private void BroadcastReadyStates()
        {
            UpdateRosterReadyUI();
            if (Unity.Netcode.NetworkManager.Singleton == null || !Unity.Netcode.NetworkManager.Singleton.IsServer || Unity.Netcode.NetworkManager.Singleton.CustomMessagingManager == null) return;
            var writer = new FastBufferWriter(1024, Allocator.Temp);
            using (writer)
            {
                writer.WriteValueSafe(_playerReadyStates.Count);
                foreach(var kvp in _playerReadyStates) {
                    writer.WriteValueSafe(kvp.Key);
                    writer.WriteValueSafe(kvp.Value);
                }
                NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll(READY_CHANNEL_S2C, writer);
            }
        }

        private void CheckAllReadyAndEnableStart()
        {
            if (_btnStartGame == null) return;
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening) return;

            bool allReady = true;
            int clientCount = NetworkManager.Singleton.ConnectedClientsList.Count;
            
            // Tek başına (Solo) test edebilmek için <2 kısıtlamasını kaldırdık.
            // allReady, diğer oyuncular hazır mı diye bakar.
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (client.ClientId == NetworkManager.ServerClientId) continue; // Host zaten hazırdır
                if (!_playerReadyStates.ContainsKey(client.ClientId) || !_playerReadyStates[client.ClientId])
                {
                    allReady = false;
                    break;
                }
            }

            if (allReady)
            {
                _btnStartGame.SetEnabled(true);
                _btnStartGame.text = GetLoc("START_GAME");
            }
            else
            {
                _btnStartGame.SetEnabled(false);
                _btnStartGame.text = GetLoc("WAITING_PLAYERS_START");
            }
        }

        
        
        private void UpdateRosterReadyUI()
        {
            if (Unity.Netcode.NetworkManager.Singleton == null || !Unity.Netcode.NetworkManager.Singleton.IsListening) return;

            var allCards = new[] { _slot1Card, _slot2Card, _slot3Card, _slot4Card };
            var allNames = new[] { _slot1Name, _slot2Name, _slot3Name, _slot4Name };
            var allReadys = new[] { _slot1Ready, _slot2Ready, _slot3Ready, _slot4Ready };
            var allReadyLbls = new[] { _slot1ReadyLbl, _slot2ReadyLbl, _slot3ReadyLbl, _slot4ReadyLbl };

            for(int k=0; k<4; k++)
            {
                if (allCards[k] != null) {
                    allCards[k].RemoveFromClassList("active-card");
                    allCards[k].RemoveFromClassList("local-player-card");
                    allCards[k].RemoveFromClassList("active-ready-card");
                    allCards[k].RemoveFromClassList("team-red-card");
                    allCards[k].RemoveFromClassList("team-blue-card");
                    allCards[k].AddToClassList("empty-card");

                    var avatarTag = allCards[k].Q<UnityEngine.UIElements.Label>($"slot-{k + 1}-avatar-tag");
                    if (avatarTag != null) avatarTag.text = "+";

                    var circle = allCards[k].Q<UnityEngine.UIElements.VisualElement>($"slot-{k + 1}-circle");
                    if (circle != null)
                    {
                        circle.RemoveFromClassList("card-avatar");
                        circle.RemoveFromClassList("avatar-p1");
                        circle.RemoveFromClassList("avatar-p2");
                        circle.RemoveFromClassList("avatar-p3");
                        circle.RemoveFromClassList("avatar-p4");
                        circle.AddToClassList("empty-avatar-circle");
                    }
                }
                if (allNames[k] != null) allNames[k].text = GetLoc("WAITING_PLAYER");
                if (allReadys[k] != null) allReadys[k].style.display = UnityEngine.UIElements.DisplayStyle.None;
            }
            
            var redIds = new System.Collections.Generic.List<ulong>();
            var blueIds = new System.Collections.Generic.List<ulong>();
            
            // Client'lar diğer client'ları ConnectedClientsList üzerinden göremez!
            // Bu yüzden lobideki kartları çizmek için ağdan gelen _playerTeams sözlüğünü kullanıyoruz.
            foreach (var kvp in _playerTeams)
            {
                ulong cId = kvp.Key;
                string t = kvp.Value;
                
                if (t == "RED") redIds.Add(cId);
                else blueIds.Add(cId);
            }

            for(int k=0; k<2; k++)
            {
                if (k < redIds.Count) FillSlot(k, redIds[k], "RED");
                if (k < blueIds.Count) FillSlot(k + 2, blueIds[k], "BLUE");
            }
            
            int playerCount = _playerTeams.Count;
            if (_rosterTitle != null) _rosterTitle.text = $"{GetLoc("ROSTER_TITLE")} ({playerCount}/4)";
        }

        private void FillSlot(int slotIndex, ulong clientId, string team)
        {
            var allCards = new[] { _slot1Card, _slot2Card, _slot3Card, _slot4Card };
            var allNames = new[] { _slot1Name, _slot2Name, _slot3Name, _slot4Name };
            var allReadys = new[] { _slot1Ready, _slot2Ready, _slot3Ready, _slot4Ready };
            var allReadyLbls = new[] { _slot1ReadyLbl, _slot2ReadyLbl, _slot3ReadyLbl, _slot4ReadyLbl };

            var card = allCards[slotIndex];
            if (card == null) return;

            string pName = _playerNames.ContainsKey(clientId) ? _playerNames[clientId] : $"Oyuncu_{clientId}";
            bool isReady = _playerReadyStates.ContainsKey(clientId) && _playerReadyStates[clientId];
            bool isMe = (clientId == Unity.Netcode.NetworkManager.Singleton.LocalClientId);
            bool isHost = (clientId == Unity.Netcode.NetworkManager.ServerClientId);

            if (isMe && isHost) pName += GetLoc("YOU_POSTFIX");
            else if (isMe) pName += GetLoc("YOU_POSTFIX");
            if (allNames[slotIndex] != null) allNames[slotIndex].text = pName;

            // KART GÖRÜNÜRLÜĞÜ VE TAKIM RENKLERİ
            card.RemoveFromClassList("empty-card");
            card.AddToClassList("active-card"); // ÇOK ÖNEMLİ: KARTIN GÖRÜNMESİNİ SAĞLAYAN CLASS!
            
            card.RemoveFromClassList("team-red-card");
            card.RemoveFromClassList("team-blue-card");
            if (team == "RED") card.AddToClassList("team-red-card");
            else card.AddToClassList("team-blue-card");

            if (isMe) card.AddToClassList("local-player-card");
            else card.RemoveFromClassList("local-player-card");
            
            if (isReady) card.AddToClassList("active-ready-card");
            else card.RemoveFromClassList("active-ready-card");

            // TAÇ KONTROLÜ
            var crown = card.Q<UnityEngine.UIElements.VisualElement>($"slot-{slotIndex + 1}-crown");
            if (crown != null)
            {
                crown.style.display = isHost ? UnityEngine.UIElements.DisplayStyle.Flex : UnityEngine.UIElements.DisplayStyle.None;
            }

            // FOTOĞRAF (AVATAR) VE P1/P2 ETİKETİ
            var circle = card.Q<UnityEngine.UIElements.VisualElement>($"slot-{slotIndex + 1}-circle");
            var avatarTag = card.Q<UnityEngine.UIElements.Label>($"slot-{slotIndex + 1}-avatar-tag");

            if (circle != null)
            {
                // ESKİ VE ÇALIŞAN ORİJİNAL MANTIK
                circle.style.backgroundColor = new UnityEngine.UIElements.StyleColor(UnityEngine.Color.clear); // Benim eklediğim bozucu düz renk siliniyor
                circle.RemoveFromClassList("empty-avatar-circle");
                circle.AddToClassList("card-avatar");
                
                circle.RemoveFromClassList("avatar-p1");
                circle.RemoveFromClassList("avatar-p2");
                circle.RemoveFromClassList("avatar-p3");
                circle.RemoveFromClassList("avatar-p4");
                
                // Oyuncu Slot'una göre avatar ekle
                circle.AddToClassList($"avatar-p{slotIndex + 1}");
            }
            if (avatarTag != null)
            {
                avatarTag.text = $"P{slotIndex + 1}";
            }

            // HAZIR / BEKLENİYOR BUTONU GÖRÜNÜMÜ
            if (allReadys[slotIndex] != null)
            {
                allReadys[slotIndex].style.display = UnityEngine.UIElements.DisplayStyle.Flex;
                
                if (isReady)
                {
                    allReadys[slotIndex].style.backgroundColor = new UnityEngine.UIElements.StyleColor(new UnityEngine.Color32(46, 204, 113, 255));
                    if (allReadyLbls[slotIndex] != null) allReadyLbls[slotIndex].text = GetLoc("READY_STATE");
                }
                else
                {
                    allReadys[slotIndex].style.backgroundColor = new UnityEngine.UIElements.StyleColor(new UnityEngine.Color32(231, 76, 60, 255));
                    if (allReadyLbls[slotIndex] != null) allReadyLbls[slotIndex].text = GetLoc("NOT_READY");
                }
            }
        }

        private void UpdateClientReadyButtonUI()
        {
            if (_btnStartGame != null)
            {
                if (_isHost)
                {
                    _btnStartGame.text = GetLoc("START_GAME");
                }
                else
                {
                    _btnStartGame.SetEnabled(true);
                    if (_isLocalPlayerReady)
                    {
                        _btnStartGame.text = GetLoc("CANCEL_READY");
                    }
                    else
                    {
                        _btnStartGame.text = GetLoc("GET_READY");
                    }
                }
            }
        }

        private void OnStartGameClicked()
        {
            if (_isHost)
            {
                ArixonRoomDiscovery.UpdateGameStarted(_currentRoomCode, true);
                AddMessageToChat(GetLoc("SYSTEM"), GetLoc("CHAT_GAME_STARTING"), true);
                if (ArixonNetworkManager.Instance != null)
                {
                    ArixonNetworkManager.Instance.LoadGameScene();
                }
            }
            else
            {
                // Lider değilsek, BAŞLAT butonu aslında HAZIR OL butonudur!
                _isLocalPlayerReady = !_isLocalPlayerReady;
                UpdateClientReadyButtonUI();

                if (Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsListening)
                {
                    var writer = new Unity.Netcode.FastBufferWriter(32, Unity.Collections.Allocator.Temp);
                    using (writer)
                    {
                        writer.WriteValueSafe(_isLocalPlayerReady);
                        Unity.Netcode.NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(READY_CHANNEL_C2S, Unity.Netcode.NetworkManager.ServerClientId, writer);
                    }
                }
            }
        }

        private void OnLeaveLobbyClicked()
        {
            if (_isHost && !string.IsNullOrEmpty(_currentRoomCode))
            {
                ArixonRoomDiscovery.UnpublishRoom(_currentRoomCode);
            }

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.Shutdown();
            }

            // Önceki odadan kalan oyuncu verilerini (Hayalet oyuncuları) ve sohbeti temizle
            _playerNames.Clear();
            _playerTeams.Clear();
            _playerReadyStates.Clear();
            _isLocalPlayerReady = false;
            _isHost = false;
            _currentRoomCode = "";
            
            if (_chatScroll != null) _chatScroll.Clear();

            SwitchView(false);
            SetHomeStatus(GetLoc("HOME_TITLE")); // Veya "Lobiden ayrıldınız" mesajı
            RefreshRoomsListUI();
        }

        private void OnCopyCodeClicked()
        {
            if (!string.IsNullOrEmpty(_currentRoomCode))
            {
                GUIUtility.systemCopyBuffer = _currentRoomCode;
                if (_btnCopyCode != null) _btnCopyCode.text = "✓ KOPYALANDI!";
                Invoke(nameof(ResetCopyButtonText), 2f);
            }
        }

        private void ResetCopyButtonText()
        {
            if (_btnCopyCode != null) _btnCopyCode.text = "📋 KOPYALA";
        }

        private void OnQuitClicked()
        {
            if (_isHost && !string.IsNullOrEmpty(_currentRoomCode))
            {
                ArixonRoomDiscovery.UnpublishRoom(_currentRoomCode);
            }

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnNetworkLogReceived(string log)
        {
            SetHomeStatus(log);
        }
    }
}
