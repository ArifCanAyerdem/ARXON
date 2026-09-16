# ERRORS.md - Hata Kayıtları ve Kök Neden Analizi

Bir hata çözüldüğünde bu dosyaya kaydedilir. Aynı hatanın tekrar etmesini önlemek ve çözümü belgelendirmek esastır.

## HATA: Lobide Çık-Gir Yapınca "Hayalet Oyuncu" (Ghost Player) Kalması
- **Belirtiler:** Bir oyuncu lobiden çıkıp başka bir lobiye girdiğinde, önceki lobideki oyuncular sanki o odada da varmış gibi görünüyordu (Boş odada P2, P3 gözükmesi).
- **Gerçek Nedeni:** `_playerNames`, `_playerReadyStates`, `_playerTeams` gibi Dictionary koleksiyonları, oyuncu odadan ayrıldığında veya oda değiştiğinde `Clear()` komutu ile temizlenmiyordu.
- **Kaynaklanan Sistem:** `MainMenuController.cs` (Room Management).
- **Yanlış Yapılan Varsayım:** Unity Netcode odadan çıkıldığında UI verilerinin de kendiliğinden resetleneceği varsayılmıştı.
- **Uygulanan Çözüm:** `OnLeaveLobbyClicked()` içerisine Dictionary'leri temizleyen kod eklendi. `FillSlot()` fonksiyonu, o slota denk gelen bir oyuncu verisi yoksa ("empty") arayüzü varsayılan + (bekleniyor) haline dönüştürecek şekilde baştan yazıldı.
- **Neden İşe Yaradı:** UI'ın veriyi "varsa göster, yoksa gizle/bekleniyora al" (Data-Driven UI) yaklaşımı ile çalışması sağlandı.
- **Gelecek İçin Kural:** State Machine (Durum Makinesi) ve Liste/Dictionary kullanan tüm network arayüzlerinde `OnDisable` veya `OnLeave` tarzı çıkış anlarında VERİLER KESİNLİKLE TEMİZLENECEK (Reset State).

## HATA: Dil İngilizce Seçilmesine Rağmen Bazı Metinlerin Türkçe Kalması
- **Belirtiler:** UI'ın büyük kısmı İngilizce olmasına rağmen "OYUNU BAŞLAT", "[SİSTEM]" ve "LİDER" gibi kelimeler Türkçe kaldı.
- **Gerçek Nedeni:** Bu metinler `MainMenuController.cs` içerisinde koda sabit (hardcoded) olarak yazılmıştı (`_btnStartGame.text = "OYUNU BAŞLAT";`). Localization tablolarından çekilmiyordu. Ayrıca Unity Editor'ünün Game View sekmesindeki Locale Seçici, varsayılan olarak English ile başlatıyordu.
- **Kaynaklanan Sistem:** `MainMenuController.cs` (UI Update Logic).
- **Uygulanan Çözüm:** Tüm hardcoded string'ler kaldırılarak `GetLoc("START_GAME")`, `GetLoc("SYSTEM")`, `GetLoc("LOBBY_LEADER")` şekline dönüştürüldü. Eksik localization key'leri (GET_READY, SYSTEM vs.) `LocalizationAutoSetup.cs` tablosuna eklendi.
- **Gelecek İçin Kural:** Asla C# tarafına `UI.text = "Örnek Yazı"` şeklinde hardcode metin yazılmayacak. İlgili metinler `LocalizationAutoSetup`a eklenecek ve daima `GetLoc(key)` ile çağrılacaktır.

## HATA: Ayarlar Menüsünde Dil ve Kalite Seçeneklerinin Hatalı Görünmesi
- **Belirtiler:** Oyun Türkçe başlamasına rağmen Ayarlar açıldığında dil 'English' olarak gözüküyor ve Grafikler 'Low, Medium, High' şeklinde İngilizce kalıyordu.
- **Gerçek Nedeni:** Dil dropdown'unun indexi LocalizationSettings.SelectedLocale'e göre ayarlanıyordu. Unity Editör toolbar'ı bunu manipüle edebildiği için oyun içi UI ile gerçek tercih uyuşmazlığı yaşanıyordu. Grafikler dropdown'u ise UXML'de hardcoded 'Low,Medium...' şeklindeydi.
- **Uygulanan Çözüm:** Dil dropdown'u doğrudan PlayerPrefs.GetString('LanguageCode') üzerinden değer almaya başladı. Kalite seçenekleri ise UpdateLocalizedTexts içerisinde GetLoc çağrılarıyla dinamik bir listeyle güncellendi.
- **Gelecek İçin Kural:** UI bileşenlerinin (Dropdown) başlangıç durumu, Unity Editor'ün geçici test verilerine (SelectedLocale) değil, uygulamanın kalıcı verilerine (PlayerPrefs/Save) dayandırılmalıdır. Ayrıca Dropdown seçimleri (Choices) dinamik oluşturulmalıdır.

## HATA: Çok Oyunculu Ekranda Takım ve Sohbet İsimlerinin İngilizce Görünmemesi
- **Belirtiler:** Oyun İngilizce seçilse dahi lobide Kırmızı Takım, Mavi Takım, Lobi Sohbeti gibi metinler Türkçe kalıyordu. Ayrıca oda ismi '[Oyuncu]'in Odası' şeklinde yarı İngilizce yarı Türkçe (SteamUser'in Odası) oluyordu.
- **Gerçek Nedeni:** Takım butonları ve sohbet kutusu başlığı UI Toolkit (UXML) dosyasında hardcoded kalmıştı ve bir isimleri (name) yoktu. Oda ismi ise ArixonRoomDiscovery.cs içinde hardcoded oluşturulup JSON olarak kaydediliyordu.
- **Uygulanan Çözüm:** UXML içindeki etiketlere name (lbl-team-red, lbl-lobby-chat vb.) atamaları yapılıp MainMenuController içinde çevirileri aktif edildi. ArixonRoomDiscovery den gelen oda ismi ROOM_NAME_FORMAT ({0}'s Room veya {0}'in Odası) lokalizasyon anahtarı kullanılarak dinamikleştirildi. Ayrıca yanlış argüman alan LEADER_LEFT referans hatası düzeltildi.
- **Gelecek İçin Kural:** Network üzerinden giden veriler (oda ismi vb.) ham data taşımalı (sadece HostName), arayüzde gösterilirken ise formatlanıp çevrilmelidir. Hiçbir UXML elementi 'name' etiketi olmadan bırakılmamalıdır.
