# DECISIONS.md - ARİXON Proje Kararları Kaydı

Bu dosya projede alınan önemli teknik kararları, nedenlerini ve alternatiflerin neden reddedildiğini barındırır. Yeni bir sistem yazılmadan önce geçmiş kararlar kontrol edilmelidir.

## 2026-09-17: Localization (Yerelleştirme) Mantığının Değiştirilmesi
- **Karar:** Dropdown üzerinden dil seçimi "Index (0, 1)" yerine "Language Code (en, tr)" üzerinden yapılacak.
- **Neden:** `AvailableLocales.Locales` listesindeki sıralama (index), Unity'nin projeyi yükleme sırasına veya Editor ayarlarına göre değişebiliyordu. Bu da index bazlı dil değişiminde kaymalara (desync) yol açıyordu.
- **Değerlendirilen Alternatifler:** Index sıralamasını manuel sabitlemek.
- **Neden Kullanılmadı:** Unity Localization paketinde dinamik locale eklendiğinde liste yine bozulabilir; kod bazlı (`en`/`tr`) arama yapmak her zaman %100 kesin sonuç verir.
- **Etkilenen Sistemler:** `MainMenuController.cs`, `LocalizationAutoSetup.cs`, UI Toolkit `dropdown-language`.
- **Gelecekte Dikkat Edilmesi Gerekenler:** Yeni bir dil (örn: İspanyolca) eklendiğinde Dropdown'a yeni seçenek eklenmeli ve kod içerisindeki eşleşme `es` olarak güncellenmelidir. Her arayüz metni mutlaka `GetLoc()` ile çağrılmalı, kod içine sabit "hardcoded" yazı yazılmamalıdır.

- **Tarih:** 2026-09-17
- **Karar:** Karakter hareketi 'İnsansı/Hızlı Kontrol' (Çevik) olarak, Kamera açısı ise 'Third-Person (Omuz Üstü)' olarak belirlendi.
- **Neden:** Kullanıcı, aksiyon odaklı bir oyun hissi (Brawl Stars çevikliği + Rocket League kamerası) talep etti.
- **Etkilenen Sistemler:** PlayerController (Rigidbody yerine CharacterController kullanılacak), Main Camera (Omuz üstü takip sistemi yazılacak).

- **Tarih:** 2026-09-17
- **Karar:** Karakter (PlayerPrefab) sabit bir mesh (ör. Kapsül) olmayacak; 'Modüler Kapsayıcı (Shell)' mimarisine göre tasarlandı.
- **Neden:** Gelecekte eklenecek olan Envanter (Inventory) sistemiyle karakterlerin kıyafet, silah veya dış görünüşlerinin dinamik olarak değiştirilebilmesi istendi.
- **Etkilenen Sistemler:** PlayerPrefab hiyerarşisi (Kök objede sadece Controller ve Network var, görsel öğeler 'VisualsHolder' isimli alt objede tutulacak).
