#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Arixon.Editor
{
    /// <summary>
    /// Play tuşuna basıldığında Game View penceresinin otomatik olarak 
    /// tam ekran (Maximize on Play) açılmasını sağlayan editör aracı.
    /// </summary>
    [InitializeOnLoad]
    public static class MaximizeGameViewOnPlay
    {
        static MaximizeGameViewOnPlay()
        {
            EnableMaximizeOnPlay();
        }

        [MenuItem("ARİXON/Ayarlar/Maximize on Play Aç")]
        public static void EnableMaximizeOnPlay()
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
                        maximizeProp.SetValue(gameView, true);
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
