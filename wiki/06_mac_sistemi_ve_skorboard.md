# 06 - Maç Sistemi ve Skorboard

Oyunun temel fizikleri oturtulduktan sonra, maçın rekabetçi ve tamamlanabilir bir akışa sahip olması için **Zamanlayıcı, Skor Takibi ve Maç Sonu Tablosu** entegre edilmiştir.

## 1. Mimari Kararlar
- **Teliften Kaçınma (Özgünlük):** Piyasada bilinen "Rocket League" tarzı bitişik ve tepede yer alan standart UI yerine; skorlar ekranın köşelerine ayrıldı (Savaş/Dövüş oyunları mantığı), zamanlayıcı ise hologram tarzında tam merkeze yerleştirildi.
- **Sunucu Otoritesi:** Sürenin sayılması, golün geçerliliği ve skorun artması işlemlerinin tamamı YALNIZCA Sunucu (Server) üzerinden işlenir. İstemciler (Clients) sadece `NetworkVariable` ile gönderilen güncellemeleri arayüzlerine çizer.

## 2. Kullanılan Sistemler
* `MatchManager.cs`: Süreyi tutar (180 saniye). Maç durumlarını (Waiting, Countdown, Playing, GoalScored, Finished) yönetir. Takım skorlarını ve maç sonu gol krallığını senkronize eder.
* `GameBall.cs`: Topa her vurulduğunda veya dokunulduğunda `LastTouchedPlayerId` değerini kaydeder. Böylece gol olduğunda kimin attığı bulunur.
* `GoalTrigger.cs`: Top ağlara girdiğinde sunucuya sinyal yollar. Sunucu `MatchManager` üzerinden skoru artırır ve Restart dizisini başlatır.
* `GameUIController.cs`: Maç durumuna göre Hologram Saati, Skorları ve Maç Sonu Liderlik tablosunu UI Toolkit üzerinden ekrana yansıtır.

## 3. Maç Döngüsü (Flow)
1. **Yükleme:** Siyah "SAHAYA İNİLİYOR" ekranı (Işınlanma gizlenir).
2. **Geri Sayım:** "3.. 2.. 1.. BAŞLA!" (Bu sırada hareket kilitlidir).
3. **Oyun:** 3 Dakikalık süre başlar. Oyuncular hareket edebilir.
4. **Gol:** Biri gol attığında ekran 3 saniyeliğine durur, "GOOOL" yazar ve herkes başlangıca sıfırlanır (Top dahil).
5. **Maç Bitişi:** Süre "00:00" olduğunda hareket kilitlenir. "MAÇ BİTTİ" ekranı ve Liderlik Tablosu çıkar.
