# SYSTEMS.md - Proje Sistem Haritası

Bu dosya ARİXON projesindeki sistemlerin birbiriyle olan bağlantılarını, ana scriptleri ve bağımlılıkları tanımlar.

## 1. Network & Multiplayer Sistemi
- **Görevi:** Oyuncuların ağ üzerinden eşleşmesini (Room), senkronizasyonunu ve Netcode for GameObjects (NGO) yönetimini sağlar.
- **Ana Scriptler:** `ArixonNetworkManager.cs`, `ArixonRoomDiscovery.cs`.
- **Bağımlılıklar:** Unity Netcode for GameObjects (NGO), Unity Transport.
- **Bağlı Sistemler:** Lobi Sistemi, Chat Sistemi.
- **Gelecekteki Entegrasyonlar:** Steamworks (Steam Matchmaking/P2P) ve Lobi Arayüzüne Server Listesi entegrasyonu.

## 2. Main Menu & Lobi UI Sistemi
- **Görevi:** Ana menü, ayarlar sekmesi, oda oluşturma (Host) ve odaya katılma (Client) ekranlarını yönetmek. Lobi içi hazır olma (Ready) durumu, takım seçimi ve takım listesinin (Roster) güncellenmesi.
- **Ana Scriptler:** `MainMenuController.cs`.
- **Bağımlılıklar:** UI Toolkit (UXML/USS).
- **Bağlı Sistemler:** Network Sistemi (CustomMessagingManager üzerinden C2S/S2C mesajlaşma), Localization Sistemi.
- **Scene Bağlantıları:** `Assets/Scenes/MainMenu.unity`.

## 3. Localization (Yerelleştirme) Sistemi
- **Görevi:** Oyun içi tüm metinlerin İngilizce/Türkçe gibi dillere dinamik olarak çevrilmesi.
- **Ana Scriptler:** `LocalizationAutoSetup.cs` (Editor script, tabloları otomatik günceller), `MainMenuController.GetLoc()` (Çalışma zamanı çeviri metodu).
- **Bağımlılıklar:** Unity Localization Paketi, Addressables Paketi.
- **Bağlı Sistemler:** Lobi UI Sistemi, Chat Sistemi.

## 4. Vision Bridge (Otonom Analiz) Sistemi
- **Görevi:** Oyun test edilirken yapay zekanın arayüzdeki (UI) hataları ve düzeni görsel olarak tespit edebilmesi için otomatik ekran görüntüsü alır.
- **Ana Scriptler:** `AntigravityVisionBridge.cs`.
- **Bağımlılıklar:** System.IO (AgentVision klasörü oluşturma).
- **Bağlı Sistemler:** Antigravity AI (Görüntü analiz yeteneği).
