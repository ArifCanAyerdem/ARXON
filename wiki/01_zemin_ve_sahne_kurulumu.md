# 01 - Zemin ve Sahne Kurulumu

Bu belgede, oyunumuzun ilk temeli olan zemin nesnesinin oluşturulması ve sahne ayarları detaylıca açıklanmaktadır.

---

## 1. Neden Bir Zemine İhtiyacımız Var?
Oyun dünyasında karakterlerin veya nesnelerin havada asılı kalmayıp yerçekimi etkisiyle üzerinde durabilmesi, yürüyebilmesi veya koşabilmesi için fiziksel bir yüzeye ihtiyaç vardır. Bu yüzeye **Zemin (Ground)** denir.

---

## 2. Sahneye Eklenen Nesne Detayları

Unity hiyerarşisine eklenen zemin nesnesi:
* **Nesne Adı:** `Ground`
* **Geometri Türü:** `Plane` (Düzlem)

### Transform Ayarları
Unity'de her 3D nesnenin uzaydaki yerini, açısını ve boyutunu belirleyen bileşene **Transform** denir:
* **Position (Pozisyon - Konum):** `(X: 0, Y: 0, Z: 0)`
  * Dünyanın tam merkez noktasına (orijin) yerleştirildi.
* **Rotation (Dönme - Açı):** `(X: 0, Y: 0, Z: 0)`
  * Zemin tamamen düz ve yere paralel durması için sıfırlandı.
* **Scale (Ölçek - Boyut):** `(X: 5, Y: 1, Z: 5)`
  * Unity'de varsayılan bir `Plane` 10 metre x 10 metre alan kaplar.
  * X ve Z eksenlerinde ölçeği 5 yaparak **50 metre x 50 metre** genişliğinde ferah bir test alanı elde ettik.

---

## 3. Eklenen Bileşenler (Components) ve Görevleri

Unity nesneleri **Bileşen (Component)** tabanlı bir mantıkla çalışır. Ground nesnemiz üzerindeki bileşenler:

1. **Transform:** Nesnenin konumu, rotasyonu ve büyüklüğünü tutar. (Her nesnede zorunludur).
2. **Mesh Filter:** Zeminin 3 boyutlu geometrik şeklini (üçgenler ve köşeler ağı) hafızada tutar.
3. **Mesh Renderer:** Bu geometrinin ekranda görünmesini, ışık almasını ve renginin/materyalinin çizilmesini sağlar.
4. **Mesh Collider (Çarpışma Algılayıcı):** 
   * **Çok Önemli:** Eğer bu bileşen olmazsa, karakterimiz zeminin içinden geçip sonsuz boşluğa düşer.
   * `Collider`, nesnenin etrafına görünmez katı bir fiziksel sınır çizer. Karakterin ayağı bu sınıra çarptığında yere basmış olur.

---

## 4. Sıradaki Adım: Karakter Kurulumu
1. Sahneye karakteri temsil edecek bir model (veya prototip olarak bir `Capsule`) eklemek.
2. Karaktere fizik kurallarına uyması için bir `Rigidbody` ve `Collider` atamak.
3. Klavyeden (`W, A, S, D` veya ok tuşları) gelen girdileri dinleyip karakteri hareket ettiren ilk C# scriptimizi (`PlayerController.cs`) yazmak.
