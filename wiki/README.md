# ARİXON Proje Wiki ve Geliştirme Günlüğü

Bu klasör, ARİXON çok oyunculu (Multiplayer) oyun projesinin sıfırdan geliştirilme sürecindeki tüm aşamaları, eklenen sistemleri, sahne yapılarını ve kodları adım adım belgeleyen resmi wiki dizinidir.

> ⚡ **Hızlı Takip İçin:** Yapılan tüm adımların kısa ve tek sayfalık özeti için **[OZET.md](./OZET.md)** sayfasına bakabilirsiniz.

---

## 📚 İçindekiler / Geliştirme Adımları

| No | Konu Başlığı | Açıklama | Dosya Bağlantısı | Durum |
|---|---|---|---|---|
| 01 | **Zemin ve Sahne Kurulumu** | Karakterin üstünde duracağı zemin (Plane), Transform ve Collider ayarları | [01_zemin_ve_sahne_kurulumu.md](./01_zemin_ve_sahne_kurulumu.md) | ✅ Tamamlandı |
| 02 | **Multiplayer Altyapısı (Netcode & MPPM Kurulumu)** | Unity Netcode for GameObjects (NGO) ve Multiplayer Play Mode paketlerinin projeye entegrasyonu | [02_multiplayer_ve_netcode_kurulumu.md](./02_multiplayer_ve_netcode_kurulumu.md) | ✅ Tamamlandı |
| 03 | **UI Toolkit Giriş Ekranı (MainMenu)** | Modern UI Toolkit (UXML + USS + C#) ile oyun başlığı, Host/Join butonları ve sahne kurulumu | [03_ui_toolkit_giris_ekrani.md](./03_ui_toolkit_giris_ekrani.md) | ✅ Tamamlandı |
| 04 | **NetworkManager ve Bağlantı Sistemi** | Giriş ekranından tetiklenen Host/Client bağlantı yöneticisi ve sahne geçişi | [04_networkmanager_ve_baglanti_sistemi.md](./04_networkmanager_ve_baglanti_sistemi.md) | ✅ Tamamlandı |
| 05 | **Multiplayer Karakter Kontrolü** | NetworkObject, NetworkTransform ve IsOwner uyumlu oyuncu hareketi | *(Sırada)* | ⏳ Bekliyor |
| 06 | **Steam Entegrasyonu** | Steamworks / Steam P2P Transport ve Lobi sistemi | *(Planlandı)* | ⏳ Bekliyor |

---

## 🛠 Proje Genel Bilgileri
* **Unity Sürümü:** 6000.3.10f1 (Unity 6)
* **Arayüz (UI) Mimarisi:** Unity UI Toolkit (UXML + USS + UIDocument)
* **Ağ Mimarisi:** Unity Netcode for GameObjects (NGO v2.2.0)
* **Test Ortamı:** Unity Multiplayer Play Mode (MPPM v1.4.0)
* **Hedef Platform / Entegrasyon:** Steamworks (Steam P2P / Relay)
* **Sahneler:** 
  1. `Assets/Scenes/MainMenu.unity` (Giriş Ekranı - Index 0)
  2. `Assets/Scenes/SampleScene.unity` (Zemin & Oyun Alanı - Index 1)
* **Kural Dosyası:** [AGENTS.md](../AGENTS.md)
