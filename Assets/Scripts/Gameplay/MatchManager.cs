using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System;
using System.Collections.Generic;

namespace Arixon.Gameplay
{
    public enum MatchState
    {
        WaitingForPlayers, 
        Countdown,         
        Playing,           
        GoalScored,        // Gol oldu, 3 saniye ekranda kalacak
        Finished           
    }

    public struct PlayerMatchState : INetworkSerializable, IEquatable<PlayerMatchState>
    {
        public ulong ClientId;
        public ulong SteamId; // Steam Entegrasyonu için
        public Unity.Collections.FixedString64Bytes PlayerName;
        public int TeamId;
        
        public int Goals;
        public int Assists;
        public int Passes;
        public int Interceptions;
        public int Saves;
        public int Score;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ClientId);
            serializer.SerializeValue(ref SteamId);
            serializer.SerializeValue(ref PlayerName);
            serializer.SerializeValue(ref TeamId);
            serializer.SerializeValue(ref Goals);
            serializer.SerializeValue(ref Assists);
            serializer.SerializeValue(ref Passes);
            serializer.SerializeValue(ref Interceptions);
            serializer.SerializeValue(ref Saves);
            serializer.SerializeValue(ref Score);
        }

        public bool Equals(PlayerMatchState other)
        {
            return ClientId == other.ClientId && SteamId == other.SteamId && PlayerName == other.PlayerName && 
                   TeamId == other.TeamId && Goals == other.Goals && Assists == other.Assists && 
                   Passes == other.Passes && Interceptions == other.Interceptions && Saves == other.Saves && Score == other.Score;
        }
    }

    public class MatchManager : NetworkBehaviour
    {
        public static MatchManager Instance { get; private set; }

        public NetworkVariable<MatchState> CurrentState = new NetworkVariable<MatchState>(MatchState.WaitingForPlayers);
        public NetworkVariable<int> CountdownTimer = new NetworkVariable<int>(3);
        
        // --- YENİ ZAMANLAYICI VE SKOR SİSTEMİ ---
        public NetworkVariable<int> MatchTimer = new NetworkVariable<int>(180); // 3 Dakika
        public NetworkVariable<int> Team1Score = new NetworkVariable<int>(0); // MAVİ
        public NetworkVariable<int> Team2Score = new NetworkVariable<int>(0);
        public NetworkVariable<int> LastScoringTeam = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server); // KIRMIZI

        public Action<MatchState> OnStateChanged;
        public Action<int> OnCountdownTicked;
        public Action<int> OnMatchTimerTicked;

        public bool IsPlaying => CurrentState.Value == MatchState.Playing;

        // Ağ üzerinden senkronize edilen oyuncu skor tablosu (Steam uyumlu)
        public NetworkList<PlayerMatchState> PlayerStats;

        private Coroutine _matchTimerCoroutine;
        [SerializeField] private GameObject _goalExplosionPrefab;
        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            PlayerStats = new NetworkList<PlayerMatchState>();
        }

        public override void OnNetworkSpawn()
        {
            CurrentState.OnValueChanged += (oldVal, newVal) => OnStateChanged?.Invoke(newVal);
            CountdownTimer.OnValueChanged += (oldVal, newVal) => OnCountdownTicked?.Invoke(newVal);
            MatchTimer.OnValueChanged += (oldVal, newVal) => OnMatchTimerTicked?.Invoke(newVal);

            if (IsServer)
            {
                StartCoroutine(ServerMatchFlow());
            }
        }

        public override void OnNetworkDespawn()
        {
            CurrentState.OnValueChanged -= (oldVal, newVal) => OnStateChanged?.Invoke(newVal);
            CountdownTimer.OnValueChanged -= (oldVal, newVal) => OnCountdownTicked?.Invoke(newVal);
            MatchTimer.OnValueChanged -= (oldVal, newVal) => OnMatchTimerTicked?.Invoke(newVal);
            
            if (Instance == this) Instance = null;
        }

        private IEnumerator ServerMatchFlow()
        {
            Debug.Log("[Gameplay] [MatchManager.ServerMatchFlow] -> Sunucu maç akışı başlatıldı.");
            CurrentState.Value = MatchState.WaitingForPlayers;
            
            // Oyuncuları Skor Tablosuna Ekle
            InitializePlayerStats();

            yield return new WaitForSeconds(2.0f);

            yield return StartCoroutine(ServerCountdownRoutine());

            CurrentState.Value = MatchState.Playing;
            Debug.Log("[Gameplay] [MatchManager.ServerMatchFlow] -> Maç başladı! (Durum: Playing)");
            _matchTimerCoroutine = StartCoroutine(ServerMatchTimerRoutine());
        }

        private void InitializePlayerStats()
        {
            PlayerStats.Clear();
            var netMgr = Arixon.Network.ArixonNetworkManager.Instance;
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                ulong cId = client.ClientId;
                int team = netMgr != null && netMgr.PlayerTeams.ContainsKey(cId) ? netMgr.PlayerTeams[cId] : 1;
                
                PlayerStats.Add(new PlayerMatchState
                {
                    ClientId = cId,
                    SteamId = 0, // İleride Steamworks'den çekilecek
                    PlayerName = new Unity.Collections.FixedString64Bytes($"Oyuncu (ID: {cId})"),
                    TeamId = team,
                    Goals = 0
                });
            }
        }

        private IEnumerator ServerCountdownRoutine()
        {
            Debug.Log("[Gameplay] [MatchManager.ServerCountdownRoutine] -> Geri sayım başladı.");
            CurrentState.Value = MatchState.Countdown;
            CountdownTimer.Value = 3;

            while (CountdownTimer.Value > 0)
            {
                yield return new WaitForSeconds(1.0f);
                CountdownTimer.Value--;
            }
            yield return new WaitForSeconds(0.5f);
            Debug.Log("[Gameplay] [MatchManager.ServerCountdownRoutine] -> Geri sayım bitti.");
        }

        private IEnumerator ServerMatchTimerRoutine()
        {
            while (MatchTimer.Value > 0)
            {
                yield return new WaitForSeconds(1.0f);
                MatchTimer.Value--;
            }

            Debug.Log("[Gameplay] [MatchManager.ServerMatchTimerRoutine] -> Süre doldu, maç bitti!");
            CurrentState.Value = MatchState.Finished;
            SendLeaderboardClientRpc();
        }

        public void RegisterGoal(int goalTeamId, GameBall ball)
        {
            if (!IsServer || CurrentState.Value != MatchState.Playing)
            {
                Debug.LogWarning($"[Gameplay] [MatchManager.RegisterGoal] -> Başarısız: Sunucu değil veya maç oynanmıyor. (IsServer: {IsServer}, Durum: {CurrentState.Value})");
                return;
            }

            // Skoru artır (Eğer top Mavi Kaleye(1) girdiyse Kırmızı Takım(2) puan alır)
            int scoringTeam = goalTeamId == 1 ? 2 : 1;
            Debug.Log($"[Gameplay] [MatchManager.RegisterGoal] -> GOL! (TopunGirdiğiKale: {goalTeamId}, PuanıAlanTakım: {scoringTeam})");

            if (goalTeamId == 1) Team2Score.Value++;
            else if (goalTeamId == 2) Team1Score.Value++;
            LastScoringTeam.Value = scoringTeam;

            if (AudioManager.Instance != null)
            {
                PlayGoalEffectsClientRpc(ball.transform.position, scoringTeam);
            }

            // Rocket League Own Goal Mantığı:
            // Golü yiyen takımdan biri kendi kalesine atsa bile, asıl krediyi RAKİP TAKIMDAN topa en son değen kişi alır.
            ulong creditedPlayerId = 999;
            ulong assistPlayerId = 999;

            if (scoringTeam == 1) 
            {
                creditedPlayerId = ball.LastTouchedBluePlayerId;
                assistPlayerId = ball.LastPasserBlueId;
            }
            else if (scoringTeam == 2) 
            {
                creditedPlayerId = ball.LastTouchedRedPlayerId;
                assistPlayerId = ball.LastPasserRedId;
            }

            if (creditedPlayerId != 999)
            {
                for (int i = 0; i < PlayerStats.Count; i++)
                {
                    if (PlayerStats[i].ClientId == creditedPlayerId)
                    {
                        var stat = PlayerStats[i];
                        stat.Goals++;
                        stat.Score += 1000;
                        PlayerStats[i] = stat;
                        Debug.Log($"[MatchManager] Golü atan (kredi verilen) oyuncu: {creditedPlayerId}");
                        break;
                    }
                }
            }

            // Asist Kredisi
            if (assistPlayerId != 999 && assistPlayerId != creditedPlayerId)
            {
                for (int i = 0; i < PlayerStats.Count; i++)
                {
                    if (PlayerStats[i].ClientId == assistPlayerId)
                    {
                        var stat = PlayerStats[i];
                        stat.Assists++;
                        stat.Score += 500;
                        PlayerStats[i] = stat;
                        Debug.Log($"[MatchManager] Asist yapan oyuncu: {assistPlayerId}");
                        break;
                    }
                }
            }
            else
            {
                Debug.Log($"[Gameplay] [MatchManager.RegisterGoal] -> Gol oldu ama rakip takımdan topa değen kimse olmadığı için bireysel skor yazılamadı.");
            }

            StartCoroutine(ServerGoalRoutine());
        }

        public void RegisterPass(ulong playerId)
        {
            if (!IsServer) return;
            for (int i = 0; i < PlayerStats.Count; i++)
            {
                if (PlayerStats[i].ClientId == playerId)
                {
                    var stat = PlayerStats[i];
                    stat.Passes++;
                    stat.Score += 50;
                    PlayerStats[i] = stat;
                    break;
                }
            }
        }

        public void RegisterInterception(ulong playerId)
        {
            if (!IsServer) return;
            for (int i = 0; i < PlayerStats.Count; i++)
            {
                if (PlayerStats[i].ClientId == playerId)
                {
                    var stat = PlayerStats[i];
                    stat.Interceptions++;
                    stat.Score += 100;
                    PlayerStats[i] = stat;
                    break;
                }
            }
        }

        public void RegisterSave(ulong playerId)
        {
            if (!IsServer) return;
            Debug.Log($"[Gameplay] [MatchManager.RegisterSave] -> Kurtarış kaydedildi. (OyuncuId: {playerId})");
            for (int i = 0; i < PlayerStats.Count; i++)
            {
                if (PlayerStats[i].ClientId == playerId)
                {
                    var stat = PlayerStats[i];
                    stat.Saves++;
                    stat.Score += 500;
                    PlayerStats[i] = stat;
                    break;
                }
            }
        }

        private IEnumerator ServerGoalRoutine()
        {
            Debug.Log("[Gameplay] [MatchManager.ServerGoalRoutine] -> Gol sonrası akış başlatıldı.");
            if (_matchTimerCoroutine != null) StopCoroutine(_matchTimerCoroutine);

            CurrentState.Value = MatchState.GoalScored;
            
            // GOOOL! yazısı için bekle
            yield return new WaitForSeconds(3.0f);

            // Herkesi yerlerine geri ışınla
            ResetAllPlayersAndBall();

            // Yeniden 3,2,1 say
            yield return StartCoroutine(ServerCountdownRoutine());

            CurrentState.Value = MatchState.Playing;
            Debug.Log("[Gameplay] [MatchManager.ServerGoalRoutine] -> Maç kaldığı yerden devam ediyor! (Durum: Playing)");
            _matchTimerCoroutine = StartCoroutine(ServerMatchTimerRoutine());
        }

        private void ResetAllPlayersAndBall()
        {
            Debug.Log("[Gameplay] [MatchManager.ResetAllPlayersAndBall] -> Oyuncular ve top başlangıç konumlarına döndürülüyor.");
            GameBall ball = FindFirstObjectByType<GameBall>();
            if (ball != null) ball.ResetBall();
            else Debug.LogWarning("[Gameplay] [MatchManager.ResetAllPlayersAndBall] -> Başarısız: GameBall sahnede bulunamadı!");

            SpawnPointData[] spawnPoints = FindObjectsByType<SpawnPointData>(FindObjectsSortMode.None);
            List<SpawnPointData> usedSpawns = new List<SpawnPointData>();

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.ConnectedClientsList != null)
            {
                foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
                {
                    if (client == null || client.PlayerObject == null) continue;

                    if (client.PlayerObject.TryGetComponent(out PlayerController pc))
                    {
                        int teamId = pc.TeamColorID.Value;
                        SpawnPointData selectedSpawn = null;
                        
                        foreach (var sp in spawnPoints)
                        {
                            if (sp.TeamID == teamId && !usedSpawns.Contains(sp))
                            {
                                selectedSpawn = sp;
                                usedSpawns.Add(sp);
                                break;
                            }
                        }

                        Vector3 spawnPos = new Vector3(0, 1, 0);
                        Quaternion spawnRot = Quaternion.identity;

                        if (selectedSpawn != null)
                        {
                            spawnPos = selectedSpawn.transform.position;
                            spawnRot = selectedSpawn.transform.rotation;
                        }

                        pc.TargetTeleportClientRpc(spawnPos, spawnRot);
                    }
                }
            }
        }

        [ClientRpc]
        private void PlayGoalEffectsClientRpc(Vector3 ballPos, int scoringTeam)
        {
            if (AudioManager.Instance != null)
            {
                // Sesi gaza getirici düzeyde patlatmak için 3 kez üst üste çaldırıyoruz
                AudioManager.Instance.PlayGoalScore();
                AudioManager.Instance.PlayGoalScore();
                AudioManager.Instance.PlayGoalScore();
            }

            Debug.Log($"[Gameplay] [MatchManager.PlayGoalEffectsClientRpc] -> İstemcilerde patlama ve ses tetikleniyor. (Takım: {scoringTeam})");

            Color teamColor = scoringTeam == 1 ? new Color(0.2f, 0.4f, 1f) : new Color(1f, 0.2f, 0.2f);

            if (_goalExplosionPrefab != null)
            {
                GameObject explosion = Instantiate(_goalExplosionPrefab, ballPos, Quaternion.identity);
                var particleSystems = explosion.GetComponentsInChildren<ParticleSystem>();
                foreach (var ps in particleSystems)
                {
                    var main = ps.main;
                    main.startColor = teamColor;
                }
                Destroy(explosion, 5f);
            }
            else
            {
                // Fallback: Resources klasöründen patlama prefabını yükle
                GameObject resPrefab = Resources.Load<GameObject>("Effects/GoalExplosionEffect");
                if (resPrefab != null)
                {
                    GameObject explosion = Instantiate(resPrefab, ballPos, Quaternion.identity);
                    var particleSystems = explosion.GetComponentsInChildren<ParticleSystem>();
                    foreach (var ps in particleSystems)
                    {
                        var main = ps.main;
                        main.startColor = teamColor;
                    }
                    Destroy(explosion, 5f);
                }
                else
                {
                    Debug.LogWarning("[Gameplay] GoalExplosionEffect not found in Resources!");
                }
            }
        }

        [ClientRpc]
        private void SendLeaderboardClientRpc()
        {
            Debug.Log("[Gameplay] [MatchManager.SendLeaderboardClientRpc] -> MAÇ BİTTİ! Liderlik tablosu açılacak.");
            var uiController = FindFirstObjectByType<Arixon.UI.GameUIController>();
            if (uiController != null)
            {
                uiController.ShowEndGameScreen();
            }
            else
            {
                Debug.LogWarning("[Gameplay] [MatchManager.SendLeaderboardClientRpc] -> Başarısız: GameUIController bulunamadı!");
            }
        }
    }
}
