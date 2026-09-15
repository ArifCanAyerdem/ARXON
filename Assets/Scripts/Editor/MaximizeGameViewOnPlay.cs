#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Arixon.Editor
{
    /// <summary>
    /// Çok oyunculu testlerde Game View'ın tam ekran büyütülmesini kapatarak
    /// iki pencerenin (Host ve Sanal Oyuncu) yan yana rahatça görünmesini sağlar.
    /// </summary>
    [InitializeOnLoad]
    public static class MaximizeGameViewOnPlay
    {
        static MaximizeGameViewOnPlay()
        {
            // Çok oyunculu çift ekran testleri için varsayılan olarak Maximize on Play'i kapatıyoruz
            DisableMaximizeOnPlay();
        }

        [MenuItem("ARİXON/Pencereler/Maximize on Play Kapat (Çift Ekran İçin)")]
        public static void DisableMaximizeOnPlay()
        {
            SetMaximizeOnPlay(false);
        }

        [MenuItem("ARİXON/Pencereler/Maximize on Play Aç")]
        public static void EnableMaximizeOnPlay()
        {
            SetMaximizeOnPlay(true);
        }

        private static void SetMaximizeOnPlay(bool enabled)
        {
            try
            {
                Type gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
                if (gameViewType == null) return;

                EditorWindow gameView = EditorWindow.GetWindow(gameViewType, false, null, false);
                if (gameView != null)
                {
                    PropertyInfo maximizeProp = gameViewType.GetProperty("maximizeOnPlay", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (maximizeProp != null)
                    {
                        maximizeProp.SetValue(gameView, enabled);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MaximizeGameViewOnPlay] Ayar uygulanırken uyarı: {ex.Message}");
            }
        }
    }
}
#endif
