# 07 - Audio ve Müzik Sistemi (AudioManager)

Bu belgede, **ARİXON** projesi için hazırlanan kesintisiz ve tam otomatik altyapıya sahip müzik sistemi (AudioManager) detaylandırılmıştır.

---

## 1. Sistemin Amacı ve Yaklaşım

Oyun içi müziklerin (Ana menü, Lobi vb.) sahne değişimlerinde kesilmesini veya baştan başlamasını önlemek için `DontDestroyOnLoad` mimarisi kullanılmıştır.
Ayrıca, kurallar gereği (manuel sürükle-bırak yasak olduğu için) bu sistem hiçbir şekilde Unity editöründe elle bir sahneye yerleştirilmeye ihtiyaç duymaz; kod üzerinden çalışma zamanında (Runtime) kendi kendini yaratır.

---

## 2. Kullanılan Script: `AudioManager.cs`

* **Dosya Konumu:** `Assets/Scripts/Audio/AudioManager.cs`
* **Görevi:** Arka plan müziğini (BGM) bulmak, sahneye kendini eklemek ve müziğin sonsuz döngüde (Loop) çalmasını sağlamak.
* **Önemli Yetenekleri:**
  * **[RuntimeInitializeOnLoadMethod]:** Oyun başladığında (daha ilk sahne bile yüklenmeden önce) otomatik olarak tetiklenir ve `AudioManager` isminde boş bir GameObject yaratıp scripti buna atar.
  * **DontDestroyOnLoad:** Ana menüden lobiye geçişlerde müzik asla kesilmez.
  * **Sahne Takibi (SceneManager):** Oyuncu lobi aşamasını bitirip oyun alanına (`SampleScene`) giriş yaptığında müzik otomatik olarak durdurulur (`Stop()`). Oyun bittiğinde tekrar lobiye dönüldüğünde müzik otomatik olarak kaldığı yerden (veya baştan) çalmaya devam eder.
  * **Singleton Mimarisi:** Sahneye yanlışlıkla ikinci bir AudioManager eklenirse onu anında yok eder (`Destroy(gameObject)`), böylece yankı (çift ses) oluşmasını engeller.
  * **Otomatik Yükleme (Resources.Load):** Müzik dosyası `Assets/Resources/Audio/Music/TRACK_01.mp3` yolundan otomatik olarak okunur ve `AudioSource` bileşenine bağlanır.

---

## 3. Müzik Dosyası Yolu ve Kurulum

Sistem tamamen `Resources` klasöründen okuma yaptığı için, müzik dosyasının tam yeri ve ismi şu şekildedir:
* **Tam Yol:** `Assets/Resources/Audio/Music/TRACK_01.mp3`

Oyun başladığı an konsolda şu loglar görünür:
1. `[Audio] [AudioManager.Initialize] -> AudioManager otomatik olarak oluşturuldu ve DontDestroyOnLoad (kalıcı) olarak ayarlandı.`
2. `[Audio] [AudioManager.LoadAndPlayMusic] -> Müzik dosyası başarıyla yüklendi ve oynatılmaya başlandı (Dosya: TRACK_01)`
