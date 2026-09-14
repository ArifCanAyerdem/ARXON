# ARİXON Project Rules (Always Active)

1. HER ŞEYİ KONTROL ET: Herhangi bir kod yazmadan veya sahne değişikliği yapmadan önce AGENTS.md, wiki/ dokümanları, sahne hiyerarşisi ve C# scriptleri kontrol edilmelidir.
2. SIFIRDAN BAŞLAYAN ANLATIM: Kullanıcı oyun geliştirmeye sıfırdan başlamaktadır. Yapılan her işlem, her kavram (Inspector, Component, Transform vb.) ve kod satırı detaylı, öğretici ve adım adım açıklanmalıdır.
3. SADECE YAPAY ZEKA KOD YAZACAKTIR: Kullanıcı hiçbir kod yazmayacak, kopyala-yapıştır yapmayacaktır. Tüm C# scriptleri, değişiklikler, düzeltmeler istisnasız yapay zeka tarafından doğrudan yazılacaktır. Kullanıcı sadece talimat verecek ve editörde test edecektir.
4. MULTIPLAYER & STEAM & UNITY MPPM: Proje başından itibaren Unity Netcode for GameObjects (NGO) ve Steamworks uyumlu olacaktır. Ağ nesneleri için NetworkBehaviour, IsOwner, NetworkVariable kullanılacaktır. Testler Unity Multiplayer Play Mode (Virtual Players) ile yapılacaktır; kamera/girdi çakışmaları en baştan engellenecektir.
5. UI STANDARDI - UI TOOLKIT: Eski Canvas/uGUI yerine tamamen Unity UI Toolkit (UXML hiyerarşi + USS stil + UIDocument/C# controller) kullanılacaktır.
6. WİKİ ODAKLI GELİŞTİRME & DÜZEN: Wiki "yaptım bitti" arşivi değil; projenin mimari şartnamesi ve pusulasıdır. Kodlar doğrudan wiki'de belirlenen standart ve düzene göre yazılacaktır. Sistemler geliştikçe wiki belgeleri anında senkronize ve güncel tutulacaktır.
7. KOD KONTROLÜ: Kullanıcı yeni bir talepte bulunduğunda önce AGENTS.md ve wiki/ incelenip kod ona göre yazılacaktır.
