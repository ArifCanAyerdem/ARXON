using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using Arixon.Gameplay;
using UnityEngine.InputSystem;

namespace Arixon.UI
{
    public class GameUIController : MonoBehaviour
    {
        private UIDocument _uiDocument;
        private VisualElement _matchOverlay;
        private Label _statusText;
        private Label _countdownText;
        
        private GhostEnergyBar _boostGauge;

        private Label _matchTimerText;
        private Label _scoreRedText;
        private Label _scoreBlueText;
        private VisualElement _leaderboardPanel;

        private VisualElement _tabScoreboardPanel;
        private ScrollView _blueTeamList;
        private ScrollView _redTeamList;

        // Maç Sonu UI Elemanları
        private Label _winnerText;
        private Button _returnToLobbyButton;

        private void Awake()
        {
            _uiDocument = GetComponent<UIDocument>();
            if (_uiDocument != null && _uiDocument.rootVisualElement != null)
            {
                var root = _uiDocument.rootVisualElement;
                _matchOverlay = root.Q<VisualElement>("match-overlay");
                _statusText = root.Q<Label>("status-text");
                _countdownText = root.Q<Label>("countdown-text");

                _boostGauge = root.Q<GhostEnergyBar>("boost-gauge");

                _matchTimerText = root.Q<Label>("match-timer");
                _scoreRedText = root.Q<Label>("score-red");
                _scoreBlueText = root.Q<Label>("score-blue");
                _leaderboardPanel = root.Q<VisualElement>("leaderboard-panel");

                _tabScoreboardPanel = root.Q<VisualElement>("tab-scoreboard-panel");
                _blueTeamList = root.Q<ScrollView>("blue-team-list");
                _redTeamList = root.Q<ScrollView>("red-team-list");

                // Maç Sonu Elemanlarını Kodla Oluştur
                _winnerText = new Label("MAÇ BİTTİ!");
                _winnerText.style.fontSize = 50;
                _winnerText.style.color = Color.white;
                _winnerText.style.unityTextAlign = TextAnchor.MiddleCenter;
                _winnerText.style.marginTop = 20;

                _returnToLobbyButton = new Button();
                _returnToLobbyButton.text = "LOBİYE DÖN";
                _returnToLobbyButton.style.marginTop = 30;
                _returnToLobbyButton.style.paddingTop = 15;
                _returnToLobbyButton.style.paddingBottom = 15;
                _returnToLobbyButton.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f);
                _returnToLobbyButton.style.color = Color.white;
                _returnToLobbyButton.style.fontSize = 24;

                _returnToLobbyButton.clicked += () => 
                {
                    if (Unity.Netcode.NetworkManager.Singleton != null)
                    {
                        Unity.Netcode.NetworkManager.Singleton.Shutdown();
                    }
                    // Lobi sahnesinin adının MainMenu veya Lobby olduğunu varsayıyoruz (Arixon için Lobi veya 1. sahne)
                    UnityEngine.SceneManagement.SceneManager.LoadScene(0); 
                };

                if (_leaderboardPanel != null)
                {
                    _leaderboardPanel.Add(_winnerText);
                    _leaderboardPanel.Add(_returnToLobbyButton);
                }
            }
        }

        private void Start()
        {
            if (MatchManager.Instance != null)
            {
                MatchManager.Instance.OnStateChanged += HandleStateChanged;
                MatchManager.Instance.OnCountdownTicked += HandleCountdown;
                MatchManager.Instance.OnMatchTimerTicked += HandleMatchTimer;
                MatchManager.Instance.Team1Score.OnValueChanged += (oldVal, newVal) => UpdateScore(1, newVal);
                MatchManager.Instance.Team2Score.OnValueChanged += (oldVal, newVal) => UpdateScore(2, newVal);
                
                // İlk değerleri al
                HandleStateChanged(MatchManager.Instance.CurrentState.Value);
                HandleMatchTimer(MatchManager.Instance.MatchTimer.Value);
                UpdateScore(1, MatchManager.Instance.Team1Score.Value);
                UpdateScore(2, MatchManager.Instance.Team2Score.Value);
            }
            else
            {
                StartCoroutine(HideOverlayRoutine());
            }
        }

        private void OnDestroy()
        {
            if (MatchManager.Instance != null)
            {
                MatchManager.Instance.OnStateChanged -= HandleStateChanged;
                MatchManager.Instance.OnCountdownTicked -= HandleCountdown;
                MatchManager.Instance.OnMatchTimerTicked -= HandleMatchTimer;
                // Score eventleri zaten lambda, onlara gerek yok
            }
        }

        private void HandleStateChanged(MatchState newState)
        {
            if (_matchOverlay == null) return;

            switch (newState)
            {
                case MatchState.WaitingForPlayers:
                    Debug.Log("[UI] [GameUIController.HandleStateChanged] -> Durum değişti: WaitingForPlayers. Ara yüz güncelleniyor.");
                    _matchOverlay.RemoveFromClassList("overlay-hidden");
                    if (_statusText != null) _statusText.style.display = DisplayStyle.Flex;
                    if (_countdownText != null) _countdownText.style.display = DisplayStyle.None;
                    break;
                case MatchState.Countdown:
                    Debug.Log("[UI] [GameUIController.HandleStateChanged] -> Durum değişti: Countdown. Geri sayım arayüzü gösteriliyor.");
                    if (_statusText != null) _statusText.style.display = DisplayStyle.None;
                    if (_countdownText != null) 
                    {
                        _countdownText.style.display = DisplayStyle.Flex;
                        _countdownText.text = "3";
                    }
                    break;
                case MatchState.Playing:
                    Debug.Log("[UI] [GameUIController.HandleStateChanged] -> Durum değişti: Playing. Overlay gizleniyor.");
                    if (_countdownText != null)
                    {
                        _countdownText.text = "GO!";
                        _countdownText.style.color = new StyleColor(new Color32(46, 204, 113, 255)); // Yeşil
                        StartCoroutine(AnimateAndHideCountdown());
                    }
                    StartCoroutine(HideOverlayRoutine());
                    break;
                case MatchState.GoalScored:
                    Debug.Log("[UI] [GameUIController.HandleStateChanged] -> Durum değişti: GoalScored. GOL yazısı gösteriliyor.");
                    _matchOverlay.RemoveFromClassList("overlay-hidden");
                    _matchOverlay.style.display = DisplayStyle.Flex;
                    if (_statusText != null)
                    {
                        _statusText.style.display = DisplayStyle.Flex;
                        if (MatchManager.Instance != null)
                        {
                            int scoringTeam = MatchManager.Instance.LastScoringTeam.Value;
                            
                            // Yerel oyuncunun takımını bul
                            int localTeam = 0;
                            ulong localId = Unity.Netcode.NetworkManager.Singleton != null ? Unity.Netcode.NetworkManager.Singleton.LocalClientId : 0;
                            for (int i = 0; i < MatchManager.Instance.PlayerStats.Count; i++)
                            {
                                if (MatchManager.Instance.PlayerStats[i].ClientId == localId)
                                {
                                    localTeam = MatchManager.Instance.PlayerStats[i].TeamId;
                                    break;
                                }
                            }
                            
                            if (localTeam == scoringTeam)
                            {
                                // Bizim takım attı!
                                _statusText.text = "GOL ATTIK!";
                                _statusText.style.color = new StyleColor(new Color32(46, 204, 113, 255)); // Yeşil (Sevinç)
                                _statusText.style.fontSize = 72;
                            }
                            else
                            {
                                // Rakip attı
                                _statusText.text = "GOL YEDİK!";
                                _statusText.style.color = new StyleColor(new Color32(231, 76, 60, 255)); // Kırmızı (Üzüntü)
                                _statusText.style.fontSize = 72;
                            }
                        }
                        else
                        {
                            _statusText.text = "GOOOL!";
                            _statusText.style.color = new StyleColor(new Color32(255, 215, 0, 255)); // Altın
                        }
                    }
                    if (_countdownText != null) _countdownText.style.display = DisplayStyle.None;
                    break;
                case MatchState.Finished:
                    Debug.Log("[UI] [GameUIController.HandleStateChanged] -> Durum değişti: Finished. Liderlik tablosu açılıyor.");
                    
                    if (_winnerText != null && MatchManager.Instance != null)
                    {
                        int t1 = MatchManager.Instance.Team1Score.Value;
                        int t2 = MatchManager.Instance.Team2Score.Value;
                        if (t1 > t2)
                        {
                            _winnerText.text = "MAVİ TAKIM KAZANDI!";
                            _winnerText.style.color = new Color(0.2f, 0.4f, 1f);
                        }
                        else if (t2 > t1)
                        {
                            _winnerText.text = "KIRMIZI TAKIM KAZANDI!";
                            _winnerText.style.color = new Color(1f, 0.2f, 0.2f);
                        }
                        else
                        {
                            _winnerText.text = "BERABERE!";
                            _winnerText.style.color = Color.white;
                        }
                    }

                    if (_leaderboardPanel != null)
                    {
                        _leaderboardPanel.style.display = DisplayStyle.Flex;
                    }
                    break;
            }
        }

        private void HandleMatchTimer(int timeInSeconds)
        {
            if (_matchTimerText == null) return;
            
            int m = timeInSeconds / 60;
            int s = timeInSeconds % 60;
            _matchTimerText.text = string.Format("{0:00}:{1:00}", m, s);
        }

        private void UpdateScore(int teamId, int score)
        {
            if (teamId == 1 && _scoreBlueText != null)
                _scoreBlueText.text = score.ToString();
            else if (teamId == 2 && _scoreRedText != null)
                _scoreRedText.text = score.ToString();
        }

        private void HandleCountdown(int time)
        {
            if (_countdownText == null) return;
            
            if (time > 0)
            {
                _countdownText.text = time.ToString();
                StartCoroutine(AnimateNumber());
            }
            else if (time == 0)
            {
                _countdownText.text = "GO!";
                _countdownText.style.color = new StyleColor(new Color32(46, 204, 113, 255)); // Yeşil
            }
        }

        private IEnumerator AnimateNumber()
        {
            _countdownText.AddToClassList("countdown-animate");
            yield return new WaitForSeconds(0.05f); // Küçük gecikme ile CSS transition tetiklenir
            _countdownText.RemoveFromClassList("countdown-animate");
        }

        private IEnumerator AnimateAndHideCountdown()
        {
            StartCoroutine(AnimateNumber());
            yield return new WaitForSeconds(1f);
            if (_countdownText != null) _countdownText.style.display = DisplayStyle.None;
        }

        private IEnumerator HideOverlayRoutine()
        {
            if (_matchOverlay != null)
            {
                _matchOverlay.AddToClassList("overlay-hidden");
                yield return new WaitForSeconds(0.6f); // Fade süresi
                _matchOverlay.style.display = DisplayStyle.None;
            }
        }

        public void UpdateStamina(float percentage)
        {
            if (_boostGauge != null)
            {
                _boostGauge.Progress = percentage * 100f;
            }
        }

        public void ShowEndGameScreen()
        {
            Debug.Log("[UI] [GameUIController.ShowEndGameScreen] -> Oyun sonu ekranı (Leaderboard) gösteriliyor.");
            if (_leaderboardPanel != null)
            {
                _leaderboardPanel.style.display = DisplayStyle.Flex;
            }
            else
            {
                Debug.LogWarning("[UI] [GameUIController.ShowEndGameScreen] -> Başarısız: _leaderboardPanel null!");
            }
        }

        private void Update()
        {
            // TAB tuşuna basılı tutulduğunda tabloyu göster
            if (Keyboard.current != null && _tabScoreboardPanel != null)
            {
                if (Keyboard.current.tabKey.wasPressedThisFrame)
                {
                    _tabScoreboardPanel.style.display = DisplayStyle.Flex;
                    UpdateTabScoreboard();
                }
                else if (Keyboard.current.tabKey.wasReleasedThisFrame)
                {
                    _tabScoreboardPanel.style.display = DisplayStyle.None;
                }
            }
        }

        private void UpdateTabScoreboard()
        {
            if (MatchManager.Instance == null) return;

            var homeList = _tabScoreboardPanel.Q<ScrollView>("home-team-list");
            var awayList = _tabScoreboardPanel.Q<ScrollView>("away-team-list");
            var homeTotal = _tabScoreboardPanel.Q<Label>("home-total-score");
            var awayTotal = _tabScoreboardPanel.Q<Label>("away-total-score");

            if (homeList == null || awayList == null) return;

            homeList.Clear();
            awayList.Clear();

            int hTotal = 0;
            int aTotal = 0;

            foreach (var stat in MatchManager.Instance.PlayerStats)
            {
                var row = new VisualElement();
                row.AddToClassList("player-row");

                // Oyuncu Bilgi Kısmı (Avatar + İsim)
                var infoContainer = new VisualElement();
                infoContainer.AddToClassList("player-info-container");

                var avatar = new VisualElement();
                avatar.AddToClassList("player-avatar");

                var nameLabel = new Label(stat.PlayerName.ToString());
                nameLabel.AddToClassList("player-name");

                infoContainer.Add(avatar);
                infoContainer.Add(nameLabel);
                row.Add(infoContainer);

                // İstatistik Sütunları (Goal, Assist, Pass, Interception, Save, Score)
                row.Add(CreateStatLabel(stat.Goals.ToString()));
                row.Add(CreateStatLabel(stat.Assists.ToString()));
                row.Add(CreateStatLabel(stat.Passes.ToString()));
                row.Add(CreateStatLabel(stat.Interceptions.ToString()));
                row.Add(CreateStatLabel(stat.Saves.ToString()));
                row.Add(CreateStatLabel(stat.Score.ToString("N0")));

                if (stat.TeamId == 1) // HOME (Mavi)
                {
                    homeList.Add(row);
                    hTotal += stat.Score;
                }
                else // AWAY (Kırmızı)
                {
                    awayList.Add(row);
                    aTotal += stat.Score;
                }
            }

            if (homeTotal != null) homeTotal.text = hTotal.ToString("N0");
            if (awayTotal != null) awayTotal.text = aTotal.ToString("N0");
        }

        private Label CreateStatLabel(string text)
        {
            var lbl = new Label(text);
            lbl.AddToClassList("player-stat");
            return lbl;
        }
    }
}
