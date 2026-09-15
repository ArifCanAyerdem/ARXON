# 04 - NetworkManager ve Gerçek Ağ Bağlantı Sistemi

Bu belgede, **ARİXON** çok oyunculu oyun projesinde oyuncuların birbirine bağlanmasını sağlayan **NetworkManager**, **UnityTransport (UTP)** ve ağ üzerinden sahne senkronizasyonu sistemi detaylandırılmıştır.

---

## 1. Sistem Mimarisi ve Temel Kavramlar

Çok oyunculu oyunlarda her bilgisayarın birbirini anlayabilmesi için bir "trafik polisine" ve "iletişim protokolüne" ihtiyaç vardır.

```
                    ┌────────────────────────┐
                    │     NetworkManager     │
                    │   (Merkezi Yönetici)   │
                    └───────────┬────────────┘
                                │
                    ┌───────────▼────────────┐
                    │     UnityTransport     │
                    │  (Port 7777 / UTP UDP) │
                    └───────────┬────────────┘
                                │
        ┌───────────────────────┴───────────────────────┐
        ▼                                               ▼
┌───────────────┐                               ┌───────────────┐
│     HOST      │                               │    CLIENT     │
│ (Lider / Oda) │ ◄────────(Ağ Akışı)────────── │ (Katılımcı)   │
└───────┬───────┘                               └───────┬───────┘
        │                                               │
        └──────────────► NetworkSceneManager ───────────┘
                    (SampleScene Otomatik Yükleme)
```

### Temel Roller:
1. **Host (Sunucu + Oyuncu):**
   * Odayı kuran kişidir (Lobi Lideri).
   * Hem sunucu yetkilerine sahiptir (otorite) hem de kendisi oyunu oynayan bir istemcidir.
   * `NetworkManager.Singleton.StartHost()` çağrısıyla başlar.
2. **Client (İstemci):**
   * Odaya sonradan katılan oyuncudur.
   * Sunucuya bağlanarak sunucudan gelen emirleri ve nesne konumlarını dinler.
   * `NetworkManager.Singleton.StartClient()` çağrısıyla bağlanır.
3. **UnityTransport (UTP):**
   * Verilerin paketler halinde internet veya yerel ağ üzerinden hızlıca (UDP protokolü ile) gidip gelmesini sağlayan taşıma motorudur.
   * Varsayılan port: `7777`, test adresi: `127.0.0.1 (Localhost)`.
4. **NetworkSceneManager (Ağ Sahne Yöneticisi):**
   * Host bir sahne değiştirdiğinde (`SampleScene`), odaya bağlı olan tüm Client'ların da ekranını otomatik olarak senkronize edip aynı sahneyi yükleyen NGO mekanizmasıdır.

---

## 2. Yazılan Scriptler ve Görev Dağılımı

### A. `ArixonNetworkManager.cs` (`Assets/Scripts/Network/`)
* **Görevi:** Ağın yaşam döngüsünü, singleton yapısını ve olay dinleyicilerini yönetir.
* **Önemli Fonksiyonlar:**
  * `StartHost()`: Odayı kurar, başarılıysa `NetworkSceneManager.LoadScene("SampleScene")` ile oyunu başlatır.
  * `StartClient()`: Belirtilen IP ve porta (`127.0.0.1:7777`) bağlanır.
  * `[RuntimeInitializeOnLoadMethod]`: Sahnede NetworkManager nesnesi unutulsa bile oyun başladığında otomatik olarak arka planda oluşturur ve yapılandırır (sıfır hata güvencesi).
  * `HandleClientConnected(clientId)` / `HandleClientDisconnected(clientId)`: Oyuncu katıldığında veya çıktığında tetiklenir.

### B. `MainMenuController.cs` (`Assets/Scripts/UI/`)
* **Görevi:** Cartoon tarzı Ana Sayfa (view-home) ile Lobi Odası (view-lobby) arasındaki akışı, profil takma adını, oda kodu ile giriş yapmayı, dinamik sunucu listesini ve çift ekranlı ağ senkronizasyonunu yönetir.
* **Önemli Özellikler:**
  * **Cartoon UI & 3D Butonlar:** Canlı turuncu, altın sarısı, fıstık yeşili ve gökyüzü mavisi renk paletine sahip 3D basılabilir çizgi film butonları ve tombul kartlar.
  * **Dinamik Profil İsmi:** Oyuncular kendi kullanıcı adlarını (örn: "Sucu", "Oyuncu_01", "Komutan") doğrudan Ana Sayfadaki kutucuktan belirleyebilir.
  * **Sıfırdan Canlı Oda İlanı:** Oyun açıldığında sağdaki liste tertemizdir ("🏝️ ŞU ANDA AÇIK ODA YOK!"). Bir oda YALNIZCA VE YALNIZCA bir oyuncu `[👑 + YENİ ODA KUR]` butonuna bastığında dinamik rastgele bir kodla (`#ARX-XXXX`) ilana çıkar.
  * **Oda Kodu ile Giriş:** Oyuncu isterse sağdaki listeden tek tıkla `[KATIL ▶]` der, isterse kodu yazıp `[GİR ▶]` ile katılır.
  * **Lobi İçi Canlı Slotlar:**
    * Slot 1: Odayı kuran Lider (👑)
    * Slot 2: Katılan Oyuncu (🎮)
  * **Ağ Sohbeti (Networked Live Chat):** NGO `CustomMessagingManager` ile iki pencere arasında anlık mesajlaşma sağlar.
  * **Lobi Kontrolü:** Host lobideyken `[🚀 OYUNU BAŞLAT]` diyene kadar oyuncular lobide bekler; basıldığında `SampleScene` oyun alanına senkronize aktarılırlar. `[⮌ ANA SAYFAYA DÖN]` dendiğinde oda ilandan silinir.

### C. `ArixonRoomDiscovery.cs` (`Assets/Scripts/Network/`)
* **Görevi:** Odaların sahte ve statik değil, yalnızca biri oda kurduğunda canlı olarak listeye düşmesini sağlayan dinamik keşif motorudur.
* **Önemli Yetenekleri:**
  * **Dinamik Kod Üretimi:** `#ARX-` + 4 haneli rastgele benzersiz kod üretir (`GenerateRoomCode`).
  * **Oda İlanı:** Sadece buton tıklandığında odayı kurucunun adıyla kaydeder (`PublishRoom`).
  * **Kalp Atışı (Heartbeat):** 2 saniyede bir odayı canlı tutar (`KeepAlive`), 7 saniye sinyal gelmezse veya host ayrılırsa odayı otomatik temizler.
  * **Bayat Oturum Temizliği:** Play Mode başlatıldığında eski test oturumlarından kalan bayat odaları temizler (`ClearAll`).

---

## 3. Multiplayer Play Mode (MPPM) İle 2 Ekran Test Rehberi

Unity'den Build almadan 2 sanal oyuncu ile saniyeler içinde test yapmak için:

1. **Test Sahnesini Açın:**
   * `Assets/Scenes/MainMenu.unity` sahnesinde olduğunuza emin olun.
2. **Play (Oynat) Tuşuna Basın:**
   * İki pencere yan yana açılır:
     - **Ekran 1:** Ana Editör (Oyuncu_01)
     - **Ekran 2:** Sanal Oyuncu (Oyuncu_02)
   * **ÖNEMLİ:** Sağdaki oda listesinde başlangıçta HİÇBİR ODA YOKTUR ("🏝️ ŞU ANDA AÇIK ODA YOK!").
3. **Oda Kurulması:**
   * **Ekran 1'de:** İsterseniz oyuncu adınızı yazın ve **`[👑 + YENİ ODA KUR]`** butonuna basın.
   * Ekran 1 Lobiye geçer, Slot 1'de Lider olarak yer alır.
4. **Canlı Listeden Odaya Katılma:**
   * Ekran 1 odayı kurduğu anda, **Ekran 2'nin sağ tarafındaki canlı oda listesinde** saniyesinde `[Oyuncu_01]'in Odası (#ARX-XXXX)` belirir!
   * **Ekran 2'de:** Doğrudan **`[KATIL ▶]`** butonuna tıklayın (veya kodu sol tarafa yazıp `[GİR ▶]` deyin).
5. **Çift Ekran Senkronizasyonu:**
   * Her iki ekranda da Takım Üyeleri `(2/4)` olur. Slot 1 ve Slot 2 dolar.
   * Sohbet kutusundan mesaj yazıp iki ekran arasında anlık sohbet edin.
6. **Oyunu Başlatma:**
   * Ekran 1'de **`[🚀 OYUNU BAŞLAT]`** butonuna basın. Her iki oyuncu da eşzamanlı olarak `SampleScene` 3D alanına aktarılır!
   * **Ekran 2 (Sanal Oyuncu):** Sol alttaki **"🌐 ODA BUL / KATIL"** butonuna basın.
     * Sohbet ekranında *"Sunucuya bağlanılıyor..."* yazar.
     * İstemci saniyeler içinde Host'a bağlanır ve otomatik olarak Host'un bulunduğu `SampleScene` sahnesine ışınlanır!

---

## 4. Sıradaki Aşama: Adım 05 - Multiplayer Karakter Kontrolü
Ağ bağlantısı ve sahne geçişi tamamlandı. Sırada:
* Ağ üzerinden çoğaltılan (`NetworkObject`) oyuncu prefab'ı (Karakter) oluşturma.
* Oyuncunun sadece kendi karakterini kontrol edebilmesi (`IsOwner` kontrolü).
* Karakterin hareket ve dönüşlerini senkronize eden `NetworkTransform` bileşeni.

 # #   5 .   L o b i   H a z 1r   ( R e a d y )   S i s t e m i   v e   B a _l a t m a   K o n t r o l � 
 A r i x o n   o y u n u n d a   l i d e r   ( H o s t )   o d a y 1  k u r d u k t a n   s o n r a   d o r u d a n   o y u n u   b a _l a t a m a z .   L o b i   s i s t e m i n d e   b i r   * * R e a d y   ( H a z 1r   O l ) * *   k o n t r o l   m e k a n i z m a s 1  v a r d 1r : 
 *   * * C u s t o m   M e s s a g e   ( A r i x o n R e a d y S y n c ) : * *   0s t e m c i   ( P l a y e r   2 )   l o b i y e   g i r d i i n d e   s a   a l t t a   * * \  
 H A Z I R  
 O L \ * *   b u t o n u n u   g � r � r .   B u n a   b a s t 11n d a   A r i x o n R e a d y S y n c   k a n a l 1  � z e r i n d e n   s u n u c u y a   ( L i d e r e )   h a z 1r   o l d u u n u   b i l d i r i r . 
 *   * * S u n u c u   K o n t r o l � : * *   S u n u c u   _ p l a y e r R e a d y S t a t e s   s � z l � �   � z e r i n d e n   t � m   o y u n c u l a r 1  t a k i p   e d e r .   S u n u c u   ( L i d e r )   h e r   z a m a n   h a z 1r   k a b u l   e d i l i r .   O d a d a   b u l u n a n   * d i e r   t � m   o y u n c u l a r *   h a z 1r   o l d u u n d a ,   l i d e r i n   * * \ O Y U N U  
 B A ^L A T \ * *   b u t o n u   a k t i f   ( E n a b l e d )   h a l e   g e l i r . 
 *   * * U I   S e n k r o n i z a s y o n u : * *   H a z 1r   o l m a   d u r u m l a r 1  s u n u c u d a n   t � m   o y u n c u l a r a   B r o a d c a s t   ( Y a y 1n )   e d i l i r   v e   U I ' d a k i   k a r t l a r 1n   � s t � n d e   y e _i l   * * '�  H A Z I R * *   v e y a   k 1r m 1z 1  * * L'  B E K L E N 0Y O R * *   r o z e t l e r i   d i n a m i k   o l a r a k   d e i _i r . 
  
 
 # #   6 .   D i n a m i k   4   K i _i l i k   L o b i   v e   0s i m   S e n k r o n i z a s y o n u 
 A r i x o n   l o b i s i   t a m   4   k i _i l i k   ( S l o t   1 - 4 )   k a p a s i t e y e   g � r e   t a s a r l a n m 1_t 1r .   O d a d a   o y u n c u   s a y 1s 1  4 ' e   u l a _t 11n d a ,   o d a   a r a m a   e k r a n 1n d a   o t o m a t i k   o l a r a k   \  
 =�� 
 D O L U \   u y a r 1s 1  � 1k a r   v e   y e n i   k a t 1l 1m l a r   e n g e l l e n i r . 
 *   * * D i n a m i k   R o s t e r   ( U p d a t e R o s t e r R e a d y U I ) : * *   B a l a n a n   h e r   o y u n c u ,   \ C o n n e c t e d C l i e n t s L i s t \   s 1r a s 1n a   g � r e   U I   � z e r i n d e k i   b o _  s l o t l a r a   ( S l o t   2 ,   3 ,   4 )   o t o m a t i k   a t a n 1r . 
 *   * * 0s i m   S e n k r o n i z a s y o n u   ( A r i x o n S y n c N a m e s ) : * *   O y u n c u l a r   l o b i y e   k a t 1l d 1k l a r 1n d a   k e n d i   y e r e l   i s i m l e r i n i   ( � r n e i n   O y u n c u _ 1 2 3 4 )   s u n u c u y a   b i l d i r i r .   S u n u c u   \ _ p l a y e r N a m e s \   s � z l � � n �   g � n c e l l e y i p   t � m   k u l l a n 1c 1l a r a   B r o a d c a s t   ( y a y 1n )   y a p a r . 
 *   * * O d a   B a _l a t m a   ^a r t 1: * *   L i d e r i n   o y u n u   b a _l a t a b i l m e s i   i � i n   o d a d a   k e n d i s i   d 1_1n d a   e n   a z   1   k i _i   d a h a   ( t o p l a m   2 )   o l m a l 1  v e   o d a d a k i   * * T � M   O Y U N C U L A R   ( 2 ,   3   v e y a   4   k i _i   d e   o l s a ) * *   H a z 1r   ( R e a d y )   b u t o n u n a   b a s m 1_  o l m a l 1d 1r .   H e r   o y u n c u   k e n d i   s l o t u n d a k i   y e _i l   \ '� 
 H A Z I R \   d u r u m u n u   d o r u d a n   g � r � r . 
  
 