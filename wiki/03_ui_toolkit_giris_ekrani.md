# 03 - Çok Oyunculu Lobi ve Canlı Sohbet Sistemi (Lobby Hub & Chat)

Bu belgede, çok oyunculu oyunumuzun lobi odası ve canlı mesajlaşma (chat) sistemi mimarisi açıklanmaktadır.

---

## 1. Lobi Mesajlaşma (Chat) Mimarisi
Lobi odasında oyuncuların oyun başlamadan önce taktik yapabilmesi, sohbet edebilmesi ve sistem bildirimlerini görebilmesi için canlı bir sohbet bileşeni entegre edildi:

* **Mesaj Akışı (`ScrollView`):** Oyuncuların ve sistemin gönderdiği mesajları listeler. Yeni mesaj geldiğinde otomatik olarak en alta (`ScrollTo`) kayar.
* **Giriş Alanı (`TextField`):** Kullanıcının klavyeden metin yazdığı alan.
* **Gönderme Tetikleyicisi:** Hem ekrandaki `[GÖNDER]` butonuyla hem de klavyeden `Enter / Return` tuşuna basıldığında mesaj anında odaya iletilir.
* **Mesaj Türleri:**
  * `[SİSTEM]` (Açık Mavi): Oda kodu kopyalama, bağlantı modu değişimi, lobiye katılma gibi durum bildirimleri.
  * `OYUNCU` (Altın Sarısı / Beyaz): Oyuncuların kendi aralarında yazdıkları sohbet mesajları.

---

## 2. Arayüz Hiyerarşisi

```
MainMenu.uxml (Lobi Odası)
├── header-bar (Logo + Oda Kodu + Ping)
├── main-section
│    ├── roster-panel (4 Kişilik Takım Slotları)
│    └── side-panel
│         ├── Bağlantı Ayarları (Unity Relay Toggle)
│         └── LOBİ SOHBET KUTUSU (LOBBY CHAT)
│              ├── chat-header-row (Başlık + CANLI Rozeti)
│              ├── chat-scroll (Mesaj Akışı)
│              └── chat-input-row (TextField + [GÖNDER] Butonu)
└── footer-bar ([LOBİDEN AYRIL], [AYARLAR], [OYUNU BAŞLAT])
```

---

## 3. Kod Yapısı (`MainMenuController.cs`)
* `_chatInput.RegisterCallback<KeyDownEvent>`: `Enter` tuşunu dinleyerek pratik gönderim sağlar.
* `AddMessageToChat(sender, message, isSystem)`: Mesajı dinamik olarak oluşturup lobi paneline ekler ve metin kutusunu sıfırlar.
