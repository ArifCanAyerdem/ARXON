# AGENTS.md - ARİXON Proje Kuralları ve Geliştirme Yönergesi

Bu dosya, Antigravity AI asistanının ARİXON projesinde her etkileşimde, her girdi aldığında ve herhangi bir kod/işlem yapmadan önce **ZORUNLU** olarak okuyup uyması gereken kuralları tanımlar.

---

## 1. TEMEL KURAL: HER ŞEYİ KONTROL ET (Code & Scene Verification)
* **Önce Kontrol, Sonra Eylem:** Herhangi bir kod yazmadan, Unity'de nesne değiştirmeden veya yeni bir sistem kurmadan önce:
  1. Bu `AGENTS.md` dosyasındaki kuralları kontrol et.
  2. `wiki/` klasöründeki mevcut proje dokümantasyonunu ve son durumu oku.
  3. Sahnedeki nesneleri, hiyerarşiyi ve mevcut C# scriptlerini kontrol et.
  4. Yapılacak değişikliğin var olan sistemlerle çakışmadığından emin ol.
* **Varsayım Yapma:** Unity API'leri, bileşen isimleri veya sahne durumu hakkında tahmin yürütme; doğrudan kontrol et.

---

## 2. EĞİTİCİ VE DETAYLI ANLATIM (Beginner-Friendly Pedagogical Approach)
* **Kullanıcı Seviyesi:** Geliştirici oyun yapımına **sıfırdan** başlamaktadır.
* **Açıklayıcı Rehberlik:** Yapılan her işlemi neden yapıldığıyla birlikte adım adım açıkla:
  - Kod bloklarının satır satır ne işe yaradığını anlat.
  - Unity terimlerini (Inspector, Transform, Rigidbody, Collider, Prefab vb.) ilk geçtiği yerde sade bir dille açıkla.
  - Editörde tıklanması gereken butonları, pencereleri ve klavye kısayollarını net olarak belirt.
* **Ezber Değil Mantık:** Sadece "şunu yap" deme; "bunu şu sebeple yapıyoruz, alternatifleri de şunlardır" şeklinde vizyon kazandır.

---

## 3. WİKİ ODAKLI GELİŞTİRME VE DÜZEN SİSTEMİ (Wiki-Driven Architecture & Living System)
* **Wiki Bir Arşiv Değil, Sistemin Pusulasıdır:** Wiki "işi yaptım bitti, kenara yazayım" mantığıyla tutulmaz. Tam tersine, projenin **teknik anayasası ve mimari kılavuzudur**.
* **Wikiye Göre Kodlama İlkesi:** 
  - Yazılan her C# kodu, fonksiyon, veri yapısı ve Unity sahne hiyerarşisi **birebir wiki'de tanımlanan düzene ve mimariye göre** inşa edilecektir.
  - Kod yazmadan önce wiki taranacak, projenin o anki düzeni anlaşılacak ve yeni kod bu düzene tam uyumlu yazılacaktır.
* **Sürekli ve Yaşayan Güncelleme (Continuous Sync):**
  - Bir mekanik genişletildiğinde, bir parametre eklendiğinde veya sistemde bir revizyon yapıldığında ilgili wiki sayfası da anında revize edilecek; kod ile wiki asla birbirinden kopmayacaktır.
  - `wiki/README.md` projenin tüm sistem haritasını her an güncel ve düzenli tutacaktır.
* **Wiki Belge Standartları:**
  - **Sistem Mimarisi:** Sistem nasıl tasarlandı, hangi mantıkla çalışıyor?
  - **Veri & Değişken Düzeni:** Hangi değişkenler/parametreler tanımlandı ve ne amaçla kullanılıyor?
  - **Unity Hiyerarşi & Bileşen Düzeni:** Sahnedeki nesne ağacı, atanan bileşenler ve Inspector değerleri.
  - **Kod Yapısı & Fonksiyon Haritası:** Yazılan scriptlerin iç yapısı ve metotların görevleri.
  - **Gelecek Entegrasyonlar:** Bu sistemin ileride bağlanacağı diğer sistemler (örn: Envanter -> Karakter Kontrolü).

---

## 4. KODLAMA VE MİMARİ STANDARTLARI
* **C# / Unity Standartları:** Temiz, modüler, okunabilir ve Unity en iyi pratiklerine (Clean Code, SOLID, Component-based) uygun kod yaz.
* **İsimlendirme:**
  - Sınıf ve Metot isimleri: `PascalCase` (örn: `PlayerController`, `MoveCharacter`)
  - Değişkenler: `camelCase` (örn: `movementSpeed`, `isGrounded`)
  - Private serialize alanlar: `[SerializeField] private float speed;`
* **Türkçe ve İngilizce Dengesi:** Kodlar ve değişken isimleri evrensel standart olan **İngilizce** olmalı; açıklamalar, yorum satırları ve dokümanlar **Türkçe** olmalıdır.

---

## 5. KOD YAZIM YETKİSİ: SADECE YAPAY ZEKA KOD YAZACAKTIR (AI-Only Code Generation)
* **Kullanıcı Kod Yazmaz:** Kullanıcı kesinlikle hiçbir C# kodu yazmayacak, düzenlemeyecek veya kopyala-yapıştır yapmayacaktır.
* **Tam Otomasyon:** Tüm scriptler, sistemler, kod değişiklikleri, hata çözümleri (debugging) ve dosya oluşturma işlemleri **istisnasız doğrudan yapay zeka (Antigravity)** tarafından yazılacaktır.
* **Asla Kullanıcıdan Kod İstenmeyecek:** Kullanıcıya "şu kodu şu dosyaya yapıştır", "şuradaki satırı değiştir" gibi komutlar verilmeyecek; ilgili dosya doğrudan araçlarla asistan tarafından güncellenecektir.
* **Kullanıcının Rolü:** Kullanıcı oyunun yönetmeni ve testçisidir. Yalnızca isteklerini belirtecek, Unity Editöründe test edecek (Play tuşuna basacak) ve görsel/tasarımsal geri bildirimler verecektir.

---

## 6. ÇOK OYUNCULU MİMARİ VE TEST STANDARTLARI (Multiplayer, Netcode & Steam)
* **Temel Altyapı - Unity Netcode for GameObjects (NGO):**
  - Projedeki tüm karakter hareketleri, durum senkronizasyonları ve oyun içi etkileşimler en baştan itibaren Unity Netcode for GameObjects (NGO) standartlarına uygun olarak kodlanacaktır.
  - Asla saf "tek oyunculu (singleplayer)" scripti yazılmayacak; `MonoBehaviour` yerine ağ nesneleri için `NetworkBehaviour` kullanılacaktır.
  - `IsOwner`, `IsServer`, `IsClient`, `NetworkVariable`, `ServerRpc` ve `ClientRpc` yapıları doğru yetkilendirme (Server-authoritative veya Client-driven with server validation) ile uygulanacaktır.
* **Steam Entegrasyonu Uyumluluğu:**
  - Ağ taşıma katmanı (Transport), Steamworks / Steam P2P (Steam Relay) ile kolayca entegre olabilecek modüler bir yapıda kurulacaktır.
  - Lobi yönetimi, oyuncu ID'leri ve eşleştirme mimarisi Steamworks standartlarını destekleyecek şekilde tasarlanacaktır.
* **Unity Multiplayer Play Mode (MPPM) Uyumluluğu:**
  - Testler Unity'nin resmi **Multiplayer Play Mode** aracıyla (Virtual Player / Sanal Oyuncular açılarak) gerçekleştirilecektir.
  - Kodlar, birden fazla editör/oyuncu penceresi aynı anda açıldığında çakışmayacak (kamera, ses dinleyicisi - AudioListener, yerel kullanıcı girdileri sadece `IsOwner` olan oyuncuda aktif olacak) şekilde yazılacaktır.

---

## 7. KULLANICI ARAYÜZÜ (UI) STANDARDI: UNITY UI TOOLKIT (UXML & USS)
* **Eski Canvas/uGUI Kullanılmayacak:** Projedeki tüm arayüzler (Giriş ekranı, Ana Menü, Ayarlar, Lobi Arayüzü, Oyun İçi HUD vb.) Unity'nin modern **UI Toolkit** sistemiyle inşa edilecektir.
* **Dosya ve Yapı Standartları:**
  - **Arayüz Hiyerarşisi:** `.uxml` (XML tabanlı şablon) dosyaları içinde tanımlanacaktır.
  - **Görsel Stil ve Tasarım:** `.uss` (Unity Style Sheet - CSS benzeri) dosyaları ile ayrıştırılacaktır.
  - **Mantık ve Etkileşim:** `UIDocument` bileşeni üzerinden `rootVisualElement.Q<Button>("...")` sorguları yapılarak C# Controller scriptleri ile yönetilecektir.
* **Responsive ve Ölçeklenebilir:** UI Toolkit Flexbox motoru kullanılarak farklı ekran çözünürlüklerine tam uyumlu, modern ve yüksek performanslı arayüzler geliştirilecektir.



