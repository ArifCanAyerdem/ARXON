#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Arixon.Editor
{
    /// <summary>
    /// Editörde multiplayer testlerini tek tıkla yönetmeyi sağlayan ARİXON geliştirici aracı.
    /// </summary>
    public static class ArixonMultiplayerTools
    {
        private const string RELAY_PREF_KEY = "Arixon_UseUnityRelay";

        /// <summary>
        /// Unity Relay'in aktif olup olmadığını kontrol eder / ayarlar.
        /// </summary>
        public static bool UseUnityRelay
        {
            get => EditorPrefs.GetBool(RELAY_PREF_KEY, false);
            set
            {
                EditorPrefs.SetBool(RELAY_PREF_KEY, value);
                Debug.Log($"[ARİXON Multiplayer] Unity Relay Modu: {(value ? "AKTİF (İnternet/Relay)" : "DEVRE DIŞI (Yerel Ağ / 127.0.0.1)")}");
            }
        }

        [MenuItem("ARİXON/Multiplayer/Unity Relay Modunu Aç-Kapat")]
        public static void ToggleRelay()
        {
            UseUnityRelay = !UseUnityRelay;
            EditorUtility.DisplayDialog(
                "ARİXON Bağlantı Modu",
                UseUnityRelay 
                    ? "Unity Relay modu AKTİF edildi!\nOyun artık Unity Relay sunucuları üzerinden test edilecek."
                    : "Yerel Ağ (Localhost) modu AKTİF edildi!\nOyun en hızlı şekilde yerel ağ (127.0.0.1) üzerinden test edilecek.",
                "Tamam"
            );
        }

        [MenuItem("ARİXON/Multiplayer/Unity Relay Modunu Aç-Kapat", true)]
        public static bool ToggleRelayValidate()
        {
            Menu.SetChecked("ARİXON/Multiplayer/Unity Relay Modunu Aç-Kapat", UseUnityRelay);
            return true;
        }

        [MenuItem("ARİXON/Multiplayer/Multiplayer Play Mode (2 Ekran Penceresi) Aç")]
        public static void OpenMPPMWindow()
        {
            EditorApplication.ExecuteMenuItem("Window/Multiplayer/Multiplayer Play Mode");
        }
    }
}
#endif
