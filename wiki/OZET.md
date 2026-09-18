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

---

### 📌 [05] Karakter Kontrolü ve Kaos Mekanikleri (Stamina & Boost)
* **Durum:** ✅ Tamamlandı
* **Ne Yapıldı:** 
  * **Stamina Sistemi:** Shift tuşuna basılı tutulduğunda sprint atma ve enerji tüketimi.
  * **Hava Sıçraması (Jetpack):** Zıpladıktan sonra havadayken Shift ile yukarı doğru sürekli roketleme.
  * **Sert Vuruş:** Hızlı koşarken topa çarpıldığında standart şutun 3 katı şiddetli vurma.
  * **UI:** Ekrana dolup boşalan Stamina (Enerji) Barı eklendi.

---

### 📌 [06] Maç Sistemi ve Özgün Skor Tablosu
* **Durum:** ✅ Tamamlandı
* **Ne Yapıldı:** 
  * **Süre (Timer):** 3 Dakikalık (180s) merkezi sunucu sayacı ve ekran ortasında devasa hologram saat.
  * **Arayüz (HUD):** Klasikleşik yapılar yerine Mavi Takım skoru Sol Üst köşede, Kırmızı Takım skoru Sağ Üst köşede neon tarzı konumlandırıldı.
  * **Gol ve Reset:** Top kaleye girdiğinde oyun 3 saniye durur, "GOOOL" yazısı çıkar ve herkes başlangıç noktasına ışınlanır.
  * **Maç Sonu:** 3 Dakika dolduğunda Liderlik Tablosu (Leaderboard) çıkar ve kimin kaç gol attığı gösterilir.
* **Detaylı Doküman:** [06_mac_sistemi_ve_skorboard.md](./06_mac_sistemi_ve_skorboard.md)
