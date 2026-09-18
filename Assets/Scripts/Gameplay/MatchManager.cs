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

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ClientId);
            serializer.SerializeValue(ref SteamId);
            serializer.SerializeValue(ref PlayerName);
            serializer.SerializeValue(ref TeamId);
            serializer.SerializeValue(ref Goals);
        }

        public bool Equals(PlayerMatchState other)
        {
            return ClientId == other.ClientId && SteamId == other.SteamId && PlayerName == other.PlayerName && TeamId == other.TeamId && Goals == other.Goals;
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
        public NetworkVariable<int> Team2Score = new NetworkVariable<int>(0); // KIRMIZI

        public Action<MatchState> OnStateChanged;
        public Action<int> OnCountdownTicked;
        public Action<int> OnMatchTimerTicked;

        public bool IsPlaying => CurrentState.Value == MatchState.Playing;

        // Ağ üzerinden senkronize edilen oyuncu skor tablosu (Steam uyumlu)
        public NetworkList<PlayerMatchState> PlayerStats;

        private Coroutine _matchTimerCoroutine;

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
            CurrentState.Value = MatchState.WaitingForPlayers;
            
            // Oyuncuları Skor Tablosuna Ekle
            InitializePlayerStats();

            yield return new WaitForSeconds(2.0f);

            yield return StartCoroutine(ServerCountdownRoutine());

            CurrentState.Value = MatchState.Playing;
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
            CurrentState.Value = MatchState.Countdown;
            CountdownTimer.Value = 3;

            while (CountdownTimer.Value > 0)
            {
                yield return new WaitForSeconds(1.0f);
                CountdownTimer.Value--;
            }
            yield return new WaitForSeconds(0.5f);
        }

        private IEnumerator ServerMatchTimerRoutine()
        {
            while (MatchTimer.Value > 0)
            {
                yield return new WaitForSeconds(1.0f);
                MatchTimer.Value--;
            }

            CurrentState.Value = MatchState.Finished;
            SendLeaderboardClientRpc();
        }

        public void RegisterGoal(int goalTeamId, ulong scorerId)
        {
            if (!IsServer || CurrentState.Value != MatchState.Playing) return;

            // Skoru artır (Eğer top Mavi Kaleye(1) girdiyse Kırmızı Takım(2) puan alır)
            if (goalTeamId == 1) Team2Score.Value++;
            else if (goalTeamId == 2) Team1Score.Value++;

            // Oyuncu istatistiğini artır
            if (scorerId != 999) // 999=Kendi kendine girdiyse
            {
                for (int i = 0; i < PlayerStats.Count; i++)
                {
                    if (PlayerStats[i].ClientId == scorerId)
                    {
                        var stat = PlayerStats[i];
                        stat.Goals++;
                        PlayerStats[i] = stat; // Struct olduğu için geri atamak zorunlu
                        break;
                    }
                }
            }

            StartCoroutine(ServerGoalRoutine());
        }

        private IEnumerator ServerGoalRoutine()
        {
            if (_matchTimerCoroutine != null) StopCoroutine(_matchTimerCoroutine);

            CurrentState.Value = MatchState.GoalScored;
            
            // GOOOL! yazısı için bekle
            yield return new WaitForSeconds(3.0f);

            // Herkesi yerlerine geri ışınla
            ResetAllPlayersAndBall();

            // Yeniden 3,2,1 say
            yield return StartCoroutine(ServerCountdownRoutine());

            CurrentState.Value = MatchState.Playing;
            _matchTimerCoroutine = StartCoroutine(ServerMatchTimerRoutine());
        }

        private void ResetAllPlayersAndBall()
        {
            GameBall ball = FindFirstObjectByType<GameBall>();
            if (ball != null) ball.ResetBall();

            SpawnPointData[] spawnPoints = FindObjectsByType<SpawnPointData>(FindObjectsSortMode.None);
            List<SpawnPointData> usedSpawns = new List<SpawnPointData>();

            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (client.PlayerObject != null && client.PlayerObject.TryGetComponent(out PlayerController pc))
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

        [ClientRpc]
        private void SendLeaderboardClientRpc()
        {
            Debug.Log("[MatchManager] MAÇ BİTTİ! Liderlik tablosu açılacak.");
            var uiController = FindFirstObjectByType<Arixon.UI.GameUIController>();
            if (uiController != null)
            {
                uiController.ShowEndGameScreen();
            }
        }
    }
}
