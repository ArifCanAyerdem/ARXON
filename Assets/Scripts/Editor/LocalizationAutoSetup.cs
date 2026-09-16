using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEditor.Localization;
using UnityEngine.Localization.Tables;

namespace Arixon.Editor
{
    public static class LocalizationAutoSetup
    {
        [InitializeOnLoadMethod]
        [MenuItem("ARİXON/Tam Otomatik Kurulum/Localization Tablosunu Güncelle")]
        public static void AutoSetupLocalization()
        {
            Debug.Log("ARİXON: Localization Kurulum/Güncelleme Başlıyor...");

            if (!AssetDatabase.IsValidFolder("Assets/Locales"))
                AssetDatabase.CreateFolder("Assets", "Locales");

            if (!AssetDatabase.IsValidFolder("Assets/Settings"))
                AssetDatabase.CreateFolder("Assets", "Settings");

            var enLocale = Locale.CreateLocale(System.Globalization.CultureInfo.GetCultureInfo("en"));
            var trLocale = Locale.CreateLocale(System.Globalization.CultureInfo.GetCultureInfo("tr"));

            if (LocalizationEditorSettings.ActiveLocalizationSettings == null)
            {
                AssetDatabase.CreateAsset(enLocale, "Assets/Locales/English (en).asset");
                AssetDatabase.CreateAsset(trLocale, "Assets/Locales/Turkish (tr).asset");

                var settings = ScriptableObject.CreateInstance<LocalizationSettings>();
                settings.name = "Localization Settings";
                AssetDatabase.CreateAsset(settings, "Assets/Settings/Localization Settings.asset");
                LocalizationEditorSettings.ActiveLocalizationSettings = settings;

                var availableLocales = settings.GetAvailableLocales();
                if (availableLocales != null)
                {
                    availableLocales.Locales.Add(enLocale);
                    availableLocales.Locales.Add(trLocale);
                }

                settings.SetSelectedLocale(enLocale);
                AssetDatabase.SaveAssets();
            }
            else
            {
                // Mevcut locale'leri bul
                var locales = LocalizationEditorSettings.ActiveLocalizationSettings.GetAvailableLocales().Locales;
                if (locales.Count >= 2)
                {
                    enLocale = locales[0];
                    trLocale = locales[1];
                }
            }

            AssetDatabase.Refresh();
            
            try
            {
                var collection = LocalizationEditorSettings.GetStringTableCollection("UITexts");
                if (collection == null)
                {
                    collection = LocalizationEditorSettings.CreateStringTableCollection("UITexts", "Assets/Locales", new List<Locale> { enLocale, trLocale });
                }

                if (collection != null && collection.StringTables.Count >= 2)
                {
                    var translations = new Dictionary<string, (string en, string tr)>
                    {
                        { "HOME_TITLE", ("⚡ ARIXON ARENA", "⚡ ARİXON ARENA") },
                        { "ID_CARD", ("[ ID CARD ]", "[ ID KARTI ]") },
                        { "ROOM_CENTER", ("🏆 ROOM CENTER", "🏆 ODA MERKEZİ") },
                        { "ROOM_DESC", ("Create a new field and lead your team or enter a code to join instantly!", "Yeni bir saha kurup takımına liderlik et veya koda girip maça hemen katıl!") },
                        { "CREATE_ROOM", ("CREATE NEW MATCH", "YENİ MAÇ KUR") },
                        { "ROOM_CODE", ("ROOM CODE:", "ODA KODU:") },
                        { "JOIN", ("JOIN", "GİR") },
                        { "LIVE_FIELDS", ("📡 LIVE FIELDS", "📡 CANLI SAHALAR") },
                        { "LIVE_FIELDS_DESC", ("Active rooms are listed here. Join with one click!", "Ağdaki aktif odalar burada anlık listelenir. Tek tıkla odaya katılabilirsin!") },
                        { "EMPTY_FIELD_TITLE", ("FIELD IS EMPTY", "SAHA ŞU AN BOŞ") },
                        { "EMPTY_FIELD_DESC", ("Create a new match and drop the ball!", "Hemen yeni bir maç kur ve topu sahaya indir!") },
                        { "QUIT", ("QUIT", "ÇIKIŞ") },
                        { "SETTINGS", ("SETTINGS", "AYARLAR") },
                        { "LOBBY_ROOM", ("👑 LOBBY ROOM", "👑 LOBİ ODASI") },
                        { "COPY", ("📋 COPY", "📋 KOPYALA") },
                        { "ROSTER_TITLE", ("👥 TEAM MEMBERS", "👥 TAKIM ÜYELERİ") },
                        { "TEAM_RED", ("🔴 RED TEAM", "🔴 KIRMIZI TAKIM") },
                        { "TEAM_BLUE", ("🔵 BLUE TEAM", "🔵 MAVİ TAKIM") },
                        { "WAITING_PLAYER", ("WAITING FOR PLAYER...", "OYUNCU BEKLENİYOR...") },
                        { "NOT_READY", ("❌ WAITING", "❌ BEKLENİYOR") },
                        { "LOBBY_CHAT", ("💬 LOBBY CHAT", "💬 LOBİ SOHBETİ") },
                        { "SYSTEM", ("[SYSTEM]", "[SİSTEM]") },
                        { "WELCOME_CHAT", ("Welcome to the lobby! Discuss tactics here before the match starts.", "Lobiye hoş geldiniz! Maç başlamadan önce buradan taktik konuşabilirsiniz.") },
                        { "TYPE_MESSAGE", ("Type message...", "Mesajını yaz...") },
                        { "SEND", ("SEND ▶", "GÖNDER ▶") },
                        { "BACK_HOME", ("⮌ BACK TO HOME", "⮌ ANA SAYFAYA DÖN") },
                        { "START_GAME", ("🚀 START GAME", "🚀 OYUNU BAŞLAT") },
                        { "BACK", ("BACK", "GERİ") },
                        { "AUDIO", ("AUDIO", "SES") },
                        { "GRAPHICS", ("GRAPHICS", "GRAFİK") },
                        { "GAMEPLAY", ("GAMEPLAY", "OYNANIŞ") },
                        { "MASTER_VOLUME", ("MASTER VOLUME", "ANA SES") },
                        { "MUSIC_VOLUME", ("MUSIC VOLUME", "MÜZİK SESİ") },
                        { "FULLSCREEN", ("FULLSCREEN", "TAM EKRAN") },
                        { "GRAPHICS_QUALITY", ("GRAPHICS QUALITY", "GRAFİK KALİTESİ") },
                        { "CAMERA_SHAKE", ("CAMERA SHAKE", "KAMERA SARSINTISI") },
                        { "LANGUAGE_TITLE", ("LANGUAGE (DİL)", "LANGUAGE (DİL)") },
                        { "CONNECTING", ("Connecting...", "Bağlanıyor...") },
                        { "READY_STATE", ("✔ READY", "✔ HAZIR") },
                        { "CANCEL_READY", ("✖ CANCEL (READY)", "✖ İPTAL (HAZIR)") },
                        { "GET_READY", ("✔ GET READY", "✔ HAZIR OL") },
                        { "LEADER_POSTFIX", (" (Leader)", " (Lider)") },
                        { "YOU_POSTFIX", (" (You)", " (Sen)") },
                        { "CHAT_SYSTEM_JOIN", ("Connected to room ({0})! Welcome {1}.", "Odaya bağlanıldı ({0})! Hoş geldin {1}.") },
                        { "CHAT_GAME_STARTING", ("Game is starting! Loading scene...", "Oyun başlatılıyor! Sahne yükleniyor...") },
                        { "WAITING_PLAYERS_START", ("⏳ WAITING FOR PLAYERS...", "⏳ OYUNCULAR BEKLENİYOR...") },
                        { "HOST_STARTING", ("Creating room (Host starting)...", "Oda kuruluyor (Host başlatılıyor)...") },
                        { "ENTER_VALID_CODE", ("Please enter a valid room code!", "Lütfen geçerli bir oda kodu yazın!") },
                        { "ROOM_NOT_FOUND", ("ERROR: No active room found for code", "HATA: Koduna ait aktif bir oda bulunamadı!") },
                        { "ROOM_STARTED", ("WARNING: Match has already started in room", "UYARI: Odadaki maç zaten başladı!") },
                        { "ROOM_FULL", ("WARNING: Room is completely full (4/4)!", "UYARI: Oda tamamen dolu (4/4)!") },
                        { "ROOM_JOINING", ("Connecting to room", "Odasınına bağlanılıyor...") },
                        { "LOBBY_LEADER", ("👑 LOBBY LEADER", "👑 LOBİ LİDERİ") },
                        { "PARTICIPANT", ("🎮 PARTICIPANT", "🎮 KATILIMCI") },
                        { "WAITING_LEADER", ("⏳ WAITING FOR LEADER...", "⏳ LİDER BEKLENİYOR...") },
                        { "ROOM_OPEN", ("🟢 OPEN", "🟢 AÇIK") },
                        { "ROOM_IN_GAME", ("⚔ IN GAME", "⚔ MAÇTA") },
                        { "ROOM_FULL_BADGE", ("🔒 FULL", "🔒 DOLU") },
                        { "JOIN_BTN", ("JOIN ▶", "KATIL ▶") },
                        { "QUALITY_LOW", ("Low", "Düşük") },
                        { "QUALITY_MEDIUM", ("Medium", "Orta") },
                        { "QUALITY_HIGH", ("High", "Yüksek") },
                        { "QUALITY_ULTRA", ("Ultra", "Ultra") },
                        { "LEADER_LEFT", ("Leader left or disconnected.", "Lider odadan ayrıldı veya bağlantı koptu.") },
                        { "ROOM_NAME_FORMAT", ("{0}'s Room", "{0}'in Odası") },
                        { "COPY_BTN", ("📋 COPY", "📋 KOPYALA") },
                        { "PING_CONNECTING", ("📡 Ping: Connecting...", "📡 Ping: Bağlanıyor...") },
                        { "HOME_ARENA_TITLE", ("⚡ ARIXON ARENA", "⚡ ARİXON ARENA") },
                        { "HOME_ID_CARD", ("[ ID CARD ]", "[ ID KARTI ]") },
                        { "HOME_ROOM_CENTER", ("🏆 ROOM CENTER", "🏆 ODA MERKEZİ") },
                        { "HOME_DESC_CREATE", ("Create a new field and lead your team or enter a code to join instantly!", "Yeni bir saha kurup takımına liderlik et veya koda girip maça hemen katıl!") },
                        { "HOME_CREATE_MATCH", ("CREATE NEW MATCH", "YENİ MAÇ KUR") },
                        { "HOME_LIVE_FIELDS", ("📡 LIVE FIELDS", "📡 CANLI SAHALAR") },
                        { "HOME_DESC_LIVE", ("Active rooms are listed here. Join with one click!", "Ağdaki aktif odalar burada anlık listelenir. Tek tıkla odaya katılabilirsin!") },
                        { "HOME_EMPTY_TITLE", ("FIELD IS EMPTY", "SAHA ŞU AN BOŞ") },
                        { "HOME_EMPTY_DESC", ("Create a new match and bring the ball to the field!", "Hemen yeni bir maç kur ve topu sahaya indir!") },
                        { "HOME_QUIT", ("QUIT", "ÇIKIŞ") },
                        { "LOBBY_LOGO", ("⚡ ARIXON", "⚡ ARİXON") },
                        { "CHAT_SEND", ("SEND ➡", "GÖNDER ➡") },
                        { "LEAVE_LOBBY_BTN", ("⬅ BACK TO HOME", "⬅ ANA SAYFAYA DÖN") }
                    };

                    foreach (var kvp in translations)
                    {
                        collection.StringTables[0].AddEntry(kvp.Key, kvp.Value.en);
                        collection.StringTables[1].AddEntry(kvp.Key, kvp.Value.tr);
                    }
                    
                    EditorUtility.SetDirty(collection);
                    EditorUtility.SetDirty(collection.SharedData);
                    EditorUtility.SetDirty(collection.StringTables[0]);
                    EditorUtility.SetDirty(collection.StringTables[1]);
                    AssetDatabase.SaveAssets();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("ARİXON: Localization tablo güncelleme sırasında uyarı alındı: " + e.Message);
            }

            Debug.Log("ARİXON: Localization Tabloları Başarıyla Güncellendi!");
        }
    }
}
