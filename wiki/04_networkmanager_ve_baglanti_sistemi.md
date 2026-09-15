# 04 - NetworkManager ve GerÃ§ek AÄŸ BaÄŸlantÄ± Sistemi

Bu belgede, **ARÄ°XON** Ã§ok oyunculu oyun projesinde oyuncularÄ±n birbirine baÄŸlanmasÄ±nÄ± saÄŸlayan **NetworkManager**, **UnityTransport (UTP)** ve aÄŸ Ã¼zerinden sahne senkronizasyonu sistemi detaylandÄ±rÄ±lmÄ±ÅŸtÄ±r.

---

## 1. Sistem Mimarisi ve Temel Kavramlar

Ã‡ok oyunculu oyunlarda her bilgisayarÄ±n birbirini anlayabilmesi iÃ§in bir "trafik polisine" ve "iletiÅŸim protokolÃ¼ne" ihtiyaÃ§ vardÄ±r.

```
                    â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”�
                    â”‚     NetworkManager     â”‚
                    â”‚   (Merkezi YÃ¶netici)   â”‚
                    â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
                                â”‚
                    â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â–¼â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”�
                    â”‚     UnityTransport     â”‚
                    â”‚  (Port 7777 / UTP UDP) â”‚
                    â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
                                â”‚
        â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”´â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”�
        â–¼                                               â–¼
â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”�                               â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”�
â”‚     HOST      â”‚                               â”‚    CLIENT     â”‚
â”‚ (Lider / Oda) â”‚ â—„â”€â”€â”€â”€â”€â”€â”€â”€(AÄŸ AkÄ±ÅŸÄ±)â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€ â”‚ (KatÄ±lÄ±mcÄ±)   â”‚
â””â”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”˜                               â””â”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”˜
        â”‚                                               â”‚
        â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â–º NetworkSceneManager â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
                    (SampleScene Otomatik YÃ¼kleme)
```

### Temel Roller:
1. **Host (Sunucu + Oyuncu):**
   * OdayÄ± kuran kiÅŸidir (Lobi Lideri).
   * Hem sunucu yetkilerine sahiptir (otorite) hem de kendisi oyunu oynayan bir istemcidir.
   * `NetworkManager.Singleton.StartHost()` Ã§aÄŸrÄ±sÄ±yla baÅŸlar.
2. **Client (Ä°stemci):**
   * Odaya sonradan katÄ±lan oyuncudur.
   * Sunucuya baÄŸlanarak sunucudan gelen emirleri ve nesne konumlarÄ±nÄ± dinler.
   * `NetworkManager.Singleton.StartClient()` Ã§aÄŸrÄ±sÄ±yla baÄŸlanÄ±r.
3. **UnityTransport (UTP):**
   * Verilerin paketler halinde internet veya yerel aÄŸ Ã¼zerinden hÄ±zlÄ±ca (UDP protokolÃ¼ ile) gidip gelmesini saÄŸlayan taÅŸÄ±ma motorudur.
   * VarsayÄ±lan port: `7777`, test adresi: `127.0.0.1 (Localhost)`.
4. **NetworkSceneManager (AÄŸ Sahne YÃ¶neticisi):**
   * Host bir sahne deÄŸiÅŸtirdiÄŸinde (`SampleScene`), odaya baÄŸlÄ± olan tÃ¼m Client'larÄ±n da ekranÄ±nÄ± otomatik olarak senkronize edip aynÄ± sahneyi yÃ¼kleyen NGO mekanizmasÄ±dÄ±r.

---

## 2. YazÄ±lan Scriptler ve GÃ¶rev DaÄŸÄ±lÄ±mÄ±

### A. `ArixonNetworkManager.cs` (`Assets/Scripts/Network/`)
* **GÃ¶revi:** AÄŸÄ±n yaÅŸam dÃ¶ngÃ¼sÃ¼nÃ¼, singleton yapÄ±sÄ±nÄ± ve olay dinleyicilerini yÃ¶netir.
* **Ã–nemli Fonksiyonlar:**
  * `StartHost()`: OdayÄ± kurar, baÅŸarÄ±lÄ±ysa `NetworkSceneManager.LoadScene("SampleScene")` ile oyunu baÅŸlatÄ±r.
  * `StartClient()`: Belirtilen IP ve porta (`127.0.0.1:7777`) baÄŸlanÄ±r.
  * `[RuntimeInitializeOnLoadMethod]`: Sahnede NetworkManager nesnesi unutulsa bile oyun baÅŸladÄ±ÄŸÄ±nda otomatik olarak arka planda oluÅŸturur ve yapÄ±landÄ±rÄ±r (sÄ±fÄ±r hata gÃ¼vencesi).
  * `HandleClientConnected(clientId)` / `HandleClientDisconnected(clientId)`: Oyuncu katÄ±ldÄ±ÄŸÄ±nda veya Ã§Ä±ktÄ±ÄŸÄ±nda tetiklenir.

### B. `MainMenuController.cs` (`Assets/Scripts/UI/`)
* **GÃ¶revi:** Cartoon tarzÄ± Ana Sayfa (view-home) ile Lobi OdasÄ± (view-lobby) arasÄ±ndaki akÄ±ÅŸÄ±, profil takma adÄ±nÄ±, oda kodu ile giriÅŸ yapmayÄ±, dinamik sunucu listesini ve Ã§ift ekranlÄ± aÄŸ senkronizasyonunu yÃ¶netir.
* **Ã–nemli Ã–zellikler:**
  * **Cartoon UI & 3D Butonlar:** CanlÄ± turuncu, altÄ±n sarÄ±sÄ±, fÄ±stÄ±k yeÅŸili ve gÃ¶kyÃ¼zÃ¼ mavisi renk paletine sahip 3D basÄ±labilir Ã§izgi film butonlarÄ± ve tombul kartlar.
  * **Dinamik Profil Ä°smi:** Oyuncular kendi kullanÄ±cÄ± adlarÄ±nÄ± (Ã¶rn: "Sucu", "Oyuncu_01", "Komutan") doÄŸrudan Ana Sayfadaki kutucuktan belirleyebilir.
  * **SÄ±fÄ±rdan CanlÄ± Oda Ä°lanÄ±:** Oyun aÃ§Ä±ldÄ±ÄŸÄ±nda saÄŸdaki liste tertemizdir ("gŸ��ï¸� Å�U ANDA AÃ‡IK ODA YOK!"). Bir oda YALNIZCA VE YALNIZCA bir oyuncu `[gŸ‘‘ + YENÄ° ODA KUR]` butonuna bastÄ±ÄŸÄ±nda dinamik rastgele bir kodla (`#ARX-XXXX`) ilana Ã§Ä±kar.
  * **Oda Kodu ile GiriÅŸ:** Oyuncu isterse saÄŸdaki listeden tek tÄ±kla `[KATIL â–¶]` der, isterse kodu yazÄ±p `[GÄ°R â–¶]` ile katÄ±lÄ±r.
  * **Lobi Ä°Ã§i CanlÄ± Slotlar:**
    * Slot 1: OdayÄ± kuran Lider (gŸ‘‘)
    * Slot 2: KatÄ±lan Oyuncu (gŸ�®)
  * **AÄŸ Sohbeti (Networked Live Chat):** NGO `CustomMessagingManager` ile iki pencere arasÄ±nda anlÄ±k mesajlaÅŸma saÄŸlar.
  * **Lobi KontrolÃ¼:** Host lobideyken `[gŸš€ OYUNU BAÅ�LAT]` diyene kadar oyuncular lobide bekler; basÄ±ldÄ±ÄŸÄ±nda `SampleScene` oyun alanÄ±na senkronize aktarÄ±lÄ±rlar. `[â®Œ ANA SAYFAYA DÃ–N]` dendiÄŸinde oda ilandan silinir.

### C. `ArixonRoomDiscovery.cs` (`Assets/Scripts/Network/`)
* **GÃ¶revi:** OdalarÄ±n sahte ve statik deÄŸil, yalnÄ±zca biri oda kurduÄŸunda canlÄ± olarak listeye dÃ¼ÅŸmesini saÄŸlayan dinamik keÅŸif motorudur.
* **Ã–nemli Yetenekleri:**
  * **Dinamik Kod Ãœretimi:** `#ARX-` + 4 haneli rastgele benzersiz kod Ã¼retir (`GenerateRoomCode`).
  * **Oda Ä°lanÄ±:** Sadece buton tÄ±klandÄ±ÄŸÄ±nda odayÄ± kurucunun adÄ±yla kaydeder (`PublishRoom`).
  * **Kalp AtÄ±ÅŸÄ± (Heartbeat):** 2 saniyede bir odayÄ± canlÄ± tutar (`KeepAlive`), 7 saniye sinyal gelmezse veya host ayrÄ±lÄ±rsa odayÄ± otomatik temizler.
  * **Bayat Oturum TemizliÄŸi:** Play Mode baÅŸlatÄ±ldÄ±ÄŸÄ±nda eski test oturumlarÄ±ndan kalan bayat odalarÄ± temizler (`ClearAll`).

---

## 3. Multiplayer Play Mode (MPPM) Ä°le 2 Ekran Test Rehberi

Unity'den Build almadan 2 sanal oyuncu ile saniyeler iÃ§inde test yapmak iÃ§in:

1. **Test Sahnesini AÃ§Ä±n:**
   * `Assets/Scenes/MainMenu.unity` sahnesinde olduÄŸunuza emin olun.
2. **Play (Oynat) TuÅŸuna BasÄ±n:**
   * Ä°ki pencere yan yana aÃ§Ä±lÄ±r:
     - **Ekran 1:** Ana EditÃ¶r (Oyuncu_01)
     - **Ekran 2:** Sanal Oyuncu (Oyuncu_02)
   * **Ã–NEMLÄ°:** SaÄŸdaki oda listesinde baÅŸlangÄ±Ã§ta HÄ°Ã‡BÄ°R ODA YOKTUR ("gŸ��ï¸� Å�U ANDA AÃ‡IK ODA YOK!").
3. **Oda KurulmasÄ±:**
   * **Ekran 1'de:** Ä°sterseniz oyuncu adÄ±nÄ±zÄ± yazÄ±n ve **`[gŸ‘‘ + YENÄ° ODA KUR]`** butonuna basÄ±n.
   * Ekran 1 Lobiye geÃ§er, Slot 1'de Lider olarak yer alÄ±r.
4. **CanlÄ± Listeden Odaya KatÄ±lma:**
   * Ekran 1 odayÄ± kurduÄŸu anda, **Ekran 2'nin saÄŸ tarafÄ±ndaki canlÄ± oda listesinde** saniyesinde `[Oyuncu_01]'in OdasÄ± (#ARX-XXXX)` belirir!
   * **Ekran 2'de:** DoÄŸrudan **`[KATIL â–¶]`** butonuna tÄ±klayÄ±n (veya kodu sol tarafa yazÄ±p `[GÄ°R â–¶]` deyin).
5. **Ã‡ift Ekran Senkronizasyonu:**
   * Her iki ekranda da TakÄ±m Ãœyeleri `(2/4)` olur. Slot 1 ve Slot 2 dolar.
   * Sohbet kutusundan mesaj yazÄ±p iki ekran arasÄ±nda anlÄ±k sohbet edin.
6. **Oyunu BaÅŸlatma:**
   * Ekran 1'de **`[gŸš€ OYUNU BAÅ�LAT]`** butonuna basÄ±n. Her iki oyuncu da eÅŸzamanlÄ± olarak `SampleScene` 3D alanÄ±na aktarÄ±lÄ±r!
   * **Ekran 2 (Sanal Oyuncu):** Sol alttaki **"gŸŒ� ODA BUL / KATIL"** butonuna basÄ±n.
     * Sohbet ekranÄ±nda *"Sunucuya baÄŸlanÄ±lÄ±yor..."* yazar.
     * Ä°stemci saniyeler iÃ§inde Host'a baÄŸlanÄ±r ve otomatik olarak Host'un bulunduÄŸu `SampleScene` sahnesine Ä±ÅŸÄ±nlanÄ±r!

---

## 4. SÄ±radaki AÅŸama: AdÄ±m 05 - Multiplayer Karakter KontrolÃ¼
AÄŸ baÄŸlantÄ±sÄ± ve sahne geÃ§iÅŸi tamamlandÄ±. SÄ±rada:
* AÄŸ Ã¼zerinden Ã§oÄŸaltÄ±lan (`NetworkObject`) oyuncu prefab'Ä± (Karakter) oluÅŸturma.
* Oyuncunun sadece kendi karakterini kontrol edebilmesi (`IsOwner` kontrolÃ¼).
* Karakterin hareket ve dÃ¶nÃ¼ÅŸlerini senkronize eden `NetworkTransform` bileÅŸeni.

 # #   5 .   L o b i   H a z 1r   ( R e a d y )   S i s t e m i   v e   B a _l a t m a   K o n t r o l u 
 A r i x o n   o y u n u n d a   l i d e r   ( H o s t )   o d a y 1  k u r d u k t a n   s o n r a   d o r u d a n   o y u n u   b a _l a t a m a z .   L o b i   s i s t e m i n d e   b i r   * * R e a d y   ( H a z 1r   O l ) * *   k o n t r o l   m e k a n i z m a s 1  v a r d 1r : 
 *   * * C u s t o m   M e s s a g e   ( A r i x o n R e a d y S y n c ) : * *   0s t e m c i   ( P l a y e r   2 )   l o b i y e   g i r d i i n d e   s a   a l t t a   * * \ 
 
 H A Z I R 
 
 O L \ * *   b u t o n u n u   g o r u r .   B u n a   b a s t 11n d a   A r i x o n R e a d y S y n c   k a n a l 1  u z e r i n d e n   s u n u c u y a   ( L i d e r e )   h a z 1r   o l d u u n u   b i l d i r i r . 
 *   * * S u n u c u   K o n t r o l u : * *   S u n u c u   _ p l a y e r R e a d y S t a t e s   s o z l u u   u z e r i n d e n   t u m   o y u n c u l a r 1  t a k i p   e d e r .   S u n u c u   ( L i d e r )   h e r   z a m a n   h a z 1r   k a b u l   e d i l i r .   O d a d a   b u l u n a n   * d i e r   t u m   o y u n c u l a r *   h a z 1r   o l d u u n d a ,   l i d e r i n   * * \ O Y U N U 
 
 B A ^L A T \ * *   b u t o n u   a k t i f   ( E n a b l e d )   h a l e   g e l i r . 
 *   * * U I   S e n k r o n i z a s y o n u : * *   H a z 1r   o l m a   d u r u m l a r 1  s u n u c u d a n   t u m   o y u n c u l a r a   B r o a d c a s t   ( Y a y 1n )   e d i l i r   v e   U I ' d a k i   k a r t l a r 1n   u s t u n d e   y e _i l   * * 's  H A Z I R * *   v e y a   k 1r m 1z 1  * * L'  B E K L E N 0Y O R * *   r o z e t l e r i   d i n a m i k   o l a r a k   d e i _i r . 
 
 
 
 # #   6 .   D i n a m i k   4   K i _i l i k   L o b i   v e   0s i m   S e n k r o n i z a s y o n u 
 A r i x o n   l o b i s i   t a m   4   k i _i l i k   ( S l o t   1 - 4 )   k a p a s i t e y e   g o r e   t a s a r l a n m 1_t 1r .   O d a d a   o y u n c u   s a y 1s 1  4 ' e   u l a _t 11n d a ,   o d a   a r a m a   e k r a n 1n d a   o t o m a t i k   o l a r a k   \ 
 
 =ØI
 
 D O L U \   u y a r 1s 1  c 1k a r   v e   y e n i   k a t 1l 1m l a r   e n g e l l e n i r . 
 *   * * D i n a m i k   R o s t e r   ( U p d a t e R o s t e r R e a d y U I ) : * *   B a l a n a n   h e r   o y u n c u ,   \ C o n n e c t e d C l i e n t s L i s t \   s 1r a s 1n a   g o r e   U I   u z e r i n d e k i   b o _  s l o t l a r a   ( S l o t   2 ,   3 ,   4 )   o t o m a t i k   a t a n 1r . 
 *   * * 0s i m   S e n k r o n i z a s y o n u   ( A r i x o n S y n c N a m e s ) : * *   O y u n c u l a r   l o b i y e   k a t 1l d 1k l a r 1n d a   k e n d i   y e r e l   i s i m l e r i n i   ( o r n e i n   O y u n c u _ 1 2 3 4 )   s u n u c u y a   b i l d i r i r .   S u n u c u   \ _ p l a y e r N a m e s \   s o z l u u n u   g u n c e l l e y i p   t u m   k u l l a n 1c 1l a r a   B r o a d c a s t   ( y a y 1n )   y a p a r . 
 *   * * O d a   B a _l a t m a   ^a r t 1: * *   L i d e r i n   o y u n u   b a _l a t a b i l m e s i   i c i n   o d a d a   k e n d i s i   d 1_1n d a   e n   a z   1   k i _i   d a h a   ( t o p l a m   2 )   o l m a l 1  v e   o d a d a k i   * * T U M   O Y U N C U L A R   ( 2 ,   3   v e y a   4   k i _i   d e   o l s a ) * *   H a z 1r   ( R e a d y )   b u t o n u n a   b a s m 1_  o l m a l 1d 1r .   H e r   o y u n c u   k e n d i   s l o t u n d a k i   y e _i l   \ 's
 
 H A Z I R \   d u r u m u n u   d o r u d a n   g o r u r . 
 
 
 

### C. Ag Iletisim Mimarisi: Submit ve Broadcast Kanallari
* Unity Netcode cift telsiz frekansli mimari ile loopback korumasi saglar.
