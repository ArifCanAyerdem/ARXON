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
        
        private RadialGauge _boostGauge;
        private Label _boostText;

        private Label _matchTimerText;
        private Label _scoreRedText;
        private Label _scoreBlueText;
        private VisualElement _leaderboardPanel;

        private VisualElement _tabScoreboardPanel;
        private ScrollView _blueTeamList;
        private ScrollView _redTeamList;

        private void Awake()
        {
            _uiDocument = GetComponent<UIDocument>();
            if (_uiDocument != null && _uiDocument.rootVisualElement != null)
            {
                var root = _uiDocument.rootVisualElement;
                _matchOverlay = root.Q<VisualElement>("match-overlay");
                _statusText = root.Q<Label>("status-text");
                _countdownText = root.Q<Label>("countdown-text");

                _boostGauge = root.Q<RadialGauge>("boost-gauge");
                _boostText = root.Q<Label>("boost-text");

                _matchTimerText = root.Q<Label>("match-timer");
                _scoreRedText = root.Q<Label>("score-red");
                _scoreBlueText = root.Q<Label>("score-blue");
                _leaderboardPanel = root.Q<VisualElement>("leaderboard-panel");

                _tabScoreboardPanel = root.Q<VisualElement>("tab-scoreboard-panel");
                _blueTeamList = root.Q<ScrollView>("blue-team-list");
                _redTeamList = root.Q<ScrollView>("red-team-list");
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
                    _matchOverlay.RemoveFromClassList("overlay-hidden");
                    if (_statusText != null) _statusText.style.display = DisplayStyle.Flex;
                    if (_countdownText != null) _countdownText.style.display = DisplayStyle.None;
                    break;
                case MatchState.Countdown:
                    if (_statusText != null) _statusText.style.display = DisplayStyle.None;
                    if (_countdownText != null) 
                    {
                        _countdownText.style.display = DisplayStyle.Flex;
                        _countdownText.text = "3";
                    }
                    break;
                case MatchState.Playing:
                    if (_countdownText != null)
                    {
                        _countdownText.text = "GO!";
                        _countdownText.style.color = new StyleColor(new Color32(46, 204, 113, 255)); // Yeşil
                        StartCoroutine(AnimateAndHideCountdown());
                    }
                    StartCoroutine(HideOverlayRoutine());
                    break;
                case MatchState.GoalScored:
                    _matchOverlay.RemoveFromClassList("overlay-hidden");
                    _matchOverlay.style.display = DisplayStyle.Flex;
                    if (_statusText != null)
                    {
                        _statusText.style.display = DisplayStyle.Flex;
                        _statusText.text = "GOOOL!";
                        _statusText.style.color = new StyleColor(new Color32(255, 215, 0, 255)); // Altın
                    }
                    if (_countdownText != null) _countdownText.style.display = DisplayStyle.None;
                    break;
                case MatchState.Finished:
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

            if (_boostText != null)
            {
                int boostAmount = Mathf.RoundToInt(percentage * 100f);
                _boostText.text = boostAmount.ToString();
                
                // Renk değişimi
                if (percentage < 0.25f)
                {
                    _boostText.style.color = new StyleColor(new Color32(255, 50, 50, 255));
                    _boostText.style.textShadow = new StyleTextShadow(new TextShadow { color = new Color(1f, 0.2f, 0.2f, 0.8f), blurRadius = 15f });
                }
                else if (percentage < 0.5f)
                {
                    _boostText.style.color = new StyleColor(new Color32(255, 150, 0, 255));
                    _boostText.style.textShadow = new StyleTextShadow(new TextShadow { color = new Color(1f, 0.6f, 0f, 0.8f), blurRadius = 15f });
                }
                else
                {
                    _boostText.style.color = new StyleColor(new Color32(0, 255, 255, 255));
                    _boostText.style.textShadow = new StyleTextShadow(new TextShadow { color = new Color(0f, 1f, 1f, 0.8f), blurRadius = 15f });
                }
            }
        }

        public void ShowEndGameScreen()
        {
            if (_leaderboardPanel != null)
            {
                _leaderboardPanel.style.display = DisplayStyle.Flex;
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
                    RefreshTabScoreboard();
                }
                else if (Keyboard.current.tabKey.wasReleasedThisFrame)
                {
                    _tabScoreboardPanel.style.display = DisplayStyle.None;
                }
            }
        }

        private void RefreshTabScoreboard()
        {
            if (_blueTeamList == null || _redTeamList == null || MatchManager.Instance == null) return;

            _blueTeamList.Clear();
            _redTeamList.Clear();

            foreach (var stat in MatchManager.Instance.PlayerStats)
            {
                VisualElement row = new VisualElement();
                row.AddToClassList("player-stat-row");

                Label nameLabel = new Label(stat.PlayerName.ToString());
                nameLabel.AddToClassList("player-stat-name");

                Label scoreLabel = new Label(stat.Goals.ToString());
                scoreLabel.AddToClassList("player-stat-score");

                row.Add(nameLabel);
                row.Add(scoreLabel);

                if (stat.TeamId == 1) // Mavi
                    _blueTeamList.Add(row);
                else
                    _redTeamList.Add(row);
            }
        }
    }
}
