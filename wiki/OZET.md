# ARİXON - Hızlı Takip & Yapılanlar Özeti (Kısa Liste)

Bu sayfa, projede yapılan her işlemi hızlıca görebilmeniz için tutulan kısa özet kayıt defteridir.

---

### 📌 [01] Zemin ve Sahne Kurulumu
* **Durum:** ✅ Tamamlandı
* **Ne Yapıldı:** `Assets/Scenes/SampleScene.unity` sahnesine `50x50` metre boyutunda düz bir `Plane` (Ground) nesnesi eklendi.
* **Önemli Ayar:** Karakterin zeminden düşmemesi için `Mesh Collider` tanımlandı ve sahne kaydedildi.
* **Detaylı Doküman:** [01_zemin_ve_sahne_kurulumu.md](./01_zemin_ve_sahne_kurulumu.md)

---

### 📌 [02] Çok Oyunculu (Multiplayer) Paketleri
* **Durum:** ✅ Tamamlandı
* **Ne Yapıldı:** Unity'nin resmi `Netcode for GameObjects (v2.2.0)` ve `Multiplayer Play Mode (v1.4.0)` paketleri kuruldu ve hatasız derlendi.
* **Amacı:** Steamworks uyumlu çok oyunculu altyapı sağlamak ve editör içinde beklemeden sanal oyuncularla test yapabilmek.
* **Detaylı Doküman:** [02_multiplayer_ve_netcode_kurulumu.md](./02_multiplayer_ve_netcode_kurulumu.md)

---

### 📌 [03] Çok Oyunculu Lobi & Canlı Mesajlaşma Sistemi (Lobby Chat)
* **Durum:** ✅ Tamamlandı
* **Ne Yapıldı:** 
  * **4 Kişilik Takım Lobi Odası:** Lider kartı (👑), oyuncu slotları, davet butonları ve oda kodu sistemi.
  * **Canlı Lobi Sohbeti (Chat):** Metin giriş alanı (`TextField`), `[GÖNDER]` butonu, `Enter` tuşu desteği ve otomatik aşağı kayan renkli mesaj akışı eklendi.
  * **Sistem Bildirimleri:** Oda kodu kopyalandığında veya Relay modu değiştiğinde otomatik sohbete düşen sistem logları bağlandı.
  * **2 Ekranlı Test & Unity Relay Desteği:** Editör araçları ve lobi içi toggle anahtarı aktif.
* **Detaylı Doküman:** [03_ui_toolkit_giris_ekrani.md](./03_ui_toolkit_giris_ekrani.md)

---

### 📌 [04] NetworkManager ve Gerçek Ağ Bağlantı Sistemi
* **Durum:** ✅ Tamamlandı
* **Ne Yapıldı:** 
  * **2 Kademeli Arayüz:** Oyun doğrudan odaya atmaz; önce Ana Sayfa (Lobi Bulucu) açılır.
  * **Oda Kodu ile Giriş:** Kullanıcı oda kodunu (`#ARX-8842`) girerek veya listedeki açık odalardan birine tıklayarak doğrudan odaya katılır.
  * **Açık & Kapalı Odalar:** Katılınabilir açık odalar (🟢) ile dolu veya maçta olan kapalı odalar (🔒) listelenir.
  * **Çift Ekran Canlı Senkronizasyonu:** Oyuncu bağlandığında Host oda verisini istemciye iletir; her iki ekranda da iki oyuncu birbirini, takım slotlarını (`2/4`) ve karşılıklı sohbet mesajlarını canlı görür.
  * **Sahne Geçişi & Çıkış:** Lobi lideri "OYUNU BAŞLAT" diyene kadar lobide beklenir; "ANA SAYFAYA DÖN" ile odadan güvenle çıkılır.
* **Detaylı Doküman:** [04_networkmanager_ve_baglanti_sistemi.md](./04_networkmanager_ve_baglanti_sistemi.md)

