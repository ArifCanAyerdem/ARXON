# 02 - Multiplayer ve Netcode Kurulumu

Bu belgede, projemizi çok oyunculu (multiplayer) bir yapıya dönüştüren temel Unity paketlerinin kurulumu ve ne işe yaradıkları açıklanmaktadır.

---

## 1. Neden Bu Paketleri Kurduk?

Geleneksel tek oyunculu oyunlarda her şey sadece sizin bilgisayarınızda çalışır. Ancak çok oyunculu bir oyunda:
* Birden fazla oyuncunun aynı anda bağlanabilmesi,
* Bir oyuncunun hareket ettiğinde diğer oyuncuların ekranında da hareketinin görünmesi,
* Sunucu (Server) ve İstemci (Client) arasındaki veri transferinin güvenli ve hızlı olması gerekir.

Bu ihtiyaçları karşılamak için Unity'nin resmi iki paketini projemize kurduk:
1. **Netcode for GameObjects (NGO) - Sürüm 2.2.0**
2. **Multiplayer Play Mode (MPPM) - Sürüm 1.4.0**

---

## 2. Kurulan Paketler Ne İşe Yarar?

### A. Netcode for GameObjects (`com.unity.netcode.gameobjects`)
* **Nedir?** Unity'nin modern, resmi ağ kütüphanesidir.
* **Ne Sağlar?**
  * `MonoBehaviour` yerine kullanacağımız `NetworkBehaviour` sınıfını sunar.
  * Karakterlerin uzaydaki yerini internet üzerinden diğer oyunculara aktaran `NetworkTransform` bileşenini içerir.
  * Can puanı, skor, takım gibi değişkenleri otomatik eşitleyen `NetworkVariable` yapısını sağlar.
  * Steamworks / Steam P2P entegrasyonuyla tam uyumlu çalışır.

### B. Multiplayer Play Mode (`com.unity.multiplayer.playmode`)
* **Nedir?** Unity içinde çok oyunculu testleri saniyeler içinde yapmayı sağlayan araçtır.
* **Eski Yöntem vs Yeni Yöntem:**
  * *Eski Yöntem:* Her test için oyunun .exe dosyasını (Build) almak 5-10 dakika sürerdi.
  * *MPPM ile Yeni Yöntem:* Unity Editöründe tek tıkla sanal oyuncu (Virtual Player) pencereleri açılır. Biri "Host (Kurucu)", diğeri "Client (Katılımcı)" olarak anında test edilir.

---

## 3. Unity Editöründe Nereden Görebilirsiniz?

1. **Paketleri Görmek İçin:**
   * Üst menüden **Window > Package Management > Package Manager** penceresini açın.
   * Sol üstteki filtreden **"In Project"** seçtiğinizde:
     * `Netcode for GameObjects`
     * `Multiplayer Play Mode`
     paketlerinin yeşil tik ile yüklendiğini görebilirsiniz.

2. **Multiplayer Play Mode Penceresini Açmak İçin:**
   * Üst menüden **Window > Multiplayer > Multiplayer Play Mode** seçeneğine tıklayın.
   * Açılan pencerede kaç adet sanal oyuncu penceresi çalıştırmak istediğinizi seçebilirsiniz (Örn: 1 Virtual Player).

---

## 4. Sıradaki Adım: NetworkManager Kurulumu
Çok oyunculu oyunların kalbi sayılan **`NetworkManager`** nesnesini sahnemize ekleyeceğiz ve basit bir "Host Başlat / Client Katıl" arayüzü oluşturacağız.
