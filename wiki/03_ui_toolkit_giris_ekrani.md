# 03 - Çok Oyunculu Lobi Arayüzü (Multiplayer Lobby Hub)

Bu belgede, kuru ve klasik menü yerine gerçek bir çok oyunculu oyunun kalbi olan **4 Kişilik Takım Lobi Odası** mimarisi açıklanmaktadır.

---

## 1. Yeni Lobi Tasarım Felsefesi
Multiplayer oyunlarda (Lethal Company, Phasmophobia, CS2 vb.) oyuncular oyuna girmeden önce bir **Lobi Odasında (Lobby Room)** toplanır. Arayüz tamamen bu mantığa göre yeniden inşa edildi:

* **Zengin Renk Paleti:** Karbon siyahı ve gece mavisi zemin, kehribar sarısı (`#FFB800`) lobi vurguları, zümrüt yeşili (`#00F08C`) durum rozetleri.
* **Canlı Oda Bilgisi:** Arkadaşların katılması için anlık oda kodu (`#ARX-8842`) ve tek tıkla kopyalama butonu.
* **Takım Slotları:** Oyuncuların karakter kartları, lobi lideri (👑) ve boş slotlar için davet butonları.

---

## 2. Arayüz Hiyerarşisi

```
MainMenu.uxml (Lobi Odası)
├── header-bar
│    ├── ARİXON Logosu + MULTIPLAYER LOBBY rozeti
│    ├── Oda Kodu (#ARX-8842) + [KOPYALA] Butonu
│    └── Bölge & Ping (TR-ISTANBUL • 18ms)
├── main-section
│    ├── roster-panel (4 Kişilik Oyuncu Slotları)
│    │    ├── Slot 1: Lobi Lideri (Host - Oyuncu 01, [HAZIR])
│    │    ├── Slot 2: [ + DAVET ET ]
│    │    ├── Slot 3: [ + DAVET ET ]
│    │    └── Slot 4: [ + DAVET ET ]
│    └── side-panel
│         ├── Oda Ayarları Kartı (Harita: Test Arena, Mod: Co-Op)
│         └── Lobi Bildirimleri / Chat Kutusu
└── footer-bar
     ├── [LOBİDEN AYRIL], [AYARLAR], [ODA BUL / KATIL]
     └── [ OYUNU BAŞLAT ▶ ] (Büyük Kehribar Buton)
```

---

## 3. Kod ve Kontrolcü Mantığı (`MainMenuController.cs`)
* `btn-host`: "Oyunu Başlat" tıklandığında Netcode Host başlatma sürecini tetikler.
* `btn-copy-code`: Oda kodunu işletim sistemi panosuna (`GUIUtility.systemCopyBuffer`) kopyalar.
* `btn-invite-1/2/3`: Oyuncu slotu için arkadaş davetini tetikler.
