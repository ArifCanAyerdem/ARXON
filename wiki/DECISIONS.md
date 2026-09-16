# DECISIONS.md - ARİXON Proje Kararları Kaydı

Bu dosya projede alınan önemli teknik kararları, nedenlerini ve alternatiflerin neden reddedildiğini barındırır. Yeni bir sistem yazılmadan önce geçmiş kararlar kontrol edilmelidir.

## 2026-09-17: Localization (Yerelleştirme) Mantığının Değiştirilmesi
- **Karar:** Dropdown üzerinden dil seçimi "Index (0, 1)" yerine "Language Code (en, tr)" üzerinden yapılacak.
- **Neden:** `AvailableLocales.Locales` listesindeki sıralama (index), Unity'nin projeyi yükleme sırasına veya Editor ayarlarına göre değişebiliyordu. Bu da index bazlı dil değişiminde kaymalara (desync) yol açıyordu.
- **Değerlendirilen Alternatifler:** Index sıralamasını manuel sabitlemek.
- **Neden Kullanılmadı:** Unity Localization paketinde dinamik locale eklendiğinde liste yine bozulabilir; kod bazlı (`en`/`tr`) arama yapmak her zaman %100 kesin sonuç verir.
- **Etkilenen Sistemler:** `MainMenuController.cs`, `LocalizationAutoSetup.cs`, UI Toolkit `dropdown-language`.
- **Gelecekte Dikkat Edilmesi Gerekenler:** Yeni bir dil (örn: İspanyolca) eklendiğinde Dropdown'a yeni seçenek eklenmeli ve kod içerisindeki eşleşme `es` olarak güncellenmelidir. Her arayüz metni mutlaka `GetLoc()` ile çağrılmalı, kod içine sabit "hardcoded" yazı yazılmamalıdır.
