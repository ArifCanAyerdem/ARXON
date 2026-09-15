using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Arixon.Network
{
    [Serializable]
    public class RoomData
    {
        public string roomCode;
        public string roomName;
        public string hostName;
        public int currentPlayers;
        public int maxPlayers = 4;
        public bool isGameStarted;
        public string ipAddress = "127.0.0.1";
        public int port = 7777;
        public long lastHeartbeatTicks;
    }

    [Serializable]
    public class RoomListContainer
    {
        public List<RoomData> rooms = new List<RoomData>();
    }

    /// <summary>
    /// ARİXON Dinamik Oda Keşif ve İlan Sistemi.
    /// Host oda kurduğunda odayı anında listeye ekler,
    /// oyuncular çıktığında veya maç başladığında listeyi canlı günceller.
    /// MPPM sanal oyuncuları ve yerel testler için ortak bir keşif kanalı kullanır.
    /// </summary>
    public static class ArixonRoomDiscovery
    {
        private static readonly string DiscoveryFilePath = Path.Combine(Path.GetTempPath(), "arixon_rooms_registry.json");
        private static readonly object FileLock = new object();

        /// <summary>
        /// 4 haneli rastgele benzersiz bir oda kodu üretir.
        /// </summary>
        public static string GenerateRoomCode()
        {
            int rand = UnityEngine.Random.Range(1000, 9999);
            return $"#ARX-{rand}";
        }

        /// <summary>
        /// Yeni bir odayı sisteme kaydeder / ilan eder.
        /// Sadece Oda Kur butonuna basıldığında çağrılır!
        /// </summary>
        public static void PublishRoom(string roomCode, string hostName, int port = 7777)
        {
            lock (FileLock)
            {
                var container = LoadContainer();

                // Varsa eskisini kaldır (Sadece oda kodu aynı olanı sil, hostName'e göre SİLME ki aynı isimde farklı odalar kurulabilsin)
                container.rooms.RemoveAll(r => r.roomCode == roomCode);

                var newRoom = new RoomData
                {
                    roomCode = roomCode,
                    roomName = $"{hostName}'in Odası",
                    hostName = hostName,
                    currentPlayers = 1,
                    maxPlayers = 4,
                    isGameStarted = false,
                    ipAddress = "127.0.0.1",
                    port = port,
                    lastHeartbeatTicks = DateTime.UtcNow.Ticks
                };

                container.rooms.Add(newRoom);
                SaveContainer(container);
                Debug.Log($"[ARİXON Discovery] 🟢 Yeni oda ilana eklendi: {roomCode} ({newRoom.roomName})");
            }
        }

        /// <summary>
        /// Odanın canlı olduğunu belirten kalp atışı (Heartbeat).
        /// </summary>
        public static void KeepAlive(string roomCode)
        {
            lock (FileLock)
            {
                var container = LoadContainer();
                var room = container.rooms.Find(r => r.roomCode == roomCode);
                if (room != null)
                {
                    room.lastHeartbeatTicks = DateTime.UtcNow.Ticks;
                    SaveContainer(container);
                }
            }
        }

        /// <summary>
        /// Odadaki oyuncu sayısını günceller.
        /// </summary>
        public static void UpdatePlayerCount(string roomCode, int count)
        {
            lock (FileLock)
            {
                var container = LoadContainer();
                var room = container.rooms.Find(r => r.roomCode == roomCode);
                if (room != null)
                {
                    room.currentPlayers = count;
                    room.lastHeartbeatTicks = DateTime.UtcNow.Ticks;
                    SaveContainer(container);
                }
            }
        }

        /// <summary>
        /// Odanın maç durumunu günceller (Maç başladıysa kapalı görünür).
        /// </summary>
        public static void UpdateGameStarted(string roomCode, bool started)
        {
            lock (FileLock)
            {
                var container = LoadContainer();
                var room = container.rooms.Find(r => r.roomCode == roomCode);
                if (room != null)
                {
                    room.isGameStarted = started;
                    room.lastHeartbeatTicks = DateTime.UtcNow.Ticks;
                    SaveContainer(container);
                }
            }
        }

        /// <summary>
        /// Odayı yayından kaldırır.
        /// </summary>
        public static void UnpublishRoom(string roomCode)
        {
            lock (FileLock)
            {
                var container = LoadContainer();
                int removed = container.rooms.RemoveAll(r => r.roomCode == roomCode);
                if (removed > 0)
                {
                    SaveContainer(container);
                    Debug.Log($"[ARİXON Discovery] 🔴 Oda yayından kaldırıldı: {roomCode}");
                }
            }
        }

        /// <summary>
        /// Tüm aktif odaların listesini döner (7 saniyeden eski sinyalleri temizler).
        /// </summary>
        public static List<RoomData> GetActiveRooms()
        {
            lock (FileLock)
            {
                var container = LoadContainer();
                long now = DateTime.UtcNow.Ticks;
                // 7 saniye sinyal gelmeyen (kapanan) odaları otomatik temizle
                long timeoutTicks = TimeSpan.FromSeconds(7).Ticks;

                int initialCount = container.rooms.Count;
                container.rooms.RemoveAll(r => (now - r.lastHeartbeatTicks) > timeoutTicks);

                if (container.rooms.Count != initialCount)
                {
                    SaveContainer(container);
                }

                return container.rooms;
            }
        }

        /// <summary>
        /// Belirli bir oda kodunun aktif olup olmadığını kontrol eder.
        /// </summary>
        public static RoomData FindRoom(string roomCode)
        {
            var rooms = GetActiveRooms();
            return rooms.Find(r => string.Equals(r.roomCode.Trim(), roomCode.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Tüm kayıtları sıfırlar.
        /// </summary>
        public static void ClearAll()
        {
            lock (FileLock)
            {
                try
                {
                    if (File.Exists(DiscoveryFilePath))
                    {
                        File.Delete(DiscoveryFilePath);
                        Debug.Log("[ARİXON Discovery] 🧹 Eski oda kayıtları temizlendi.");
                    }
                }
                catch { }
            }
        }

        private static RoomListContainer LoadContainer()
        {
            try
            {
                if (File.Exists(DiscoveryFilePath))
                {
                    string json = File.ReadAllText(DiscoveryFilePath);
                    return JsonUtility.FromJson<RoomListContainer>(json) ?? new RoomListContainer();
                }
            }
            catch
            {
                // Okuma hatası olursa boş döner
            }

            return new RoomListContainer();
        }

        private static void SaveContainer(RoomListContainer container)
        {
            try
            {
                string json = JsonUtility.ToJson(container, true);
                File.WriteAllText(DiscoveryFilePath, json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ARİXON Discovery] Oda kaydedilemedi: {ex.Message}");
            }
        }
    }
}
