using UnityEditor;
using UnityEngine;
using System.IO;

namespace Arixon.EditorScripts
{
    /// <summary>
    /// Antigravity AI'ın Unity içerisinden ekran görüntüsü almasını,
    /// oyunu görsel olarak kontrol etmesini ve hataları görmesini sağlayan köprü sistemi.
    /// </summary>
    [InitializeOnLoad]
    public static class AntigravityVisionBridge
    {
        private static string targetDir;
        private static string triggerFile;

        static AntigravityVisionBridge()
        {
            // Unity projesinin ana dizininde bir klasör oluşturulur
            string basePath = Directory.GetParent(Application.dataPath).FullName;
            targetDir = Path.Combine(basePath, "AgentVision");
            triggerFile = Path.Combine(targetDir, "Capture.txt");

            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            EditorApplication.update += CheckForTrigger;
            Application.logMessageReceived += OnLogMessage;
        }

        private static void CheckForTrigger()
        {
            // Yapay zeka Capture.txt dosyasını oluşturduğunda ekran görüntüsü alır
            if (File.Exists(triggerFile))
            {
                try { File.Delete(triggerFile); } catch { return; }
                
                string path = Path.Combine(targetDir, "ManualCapture.png");
                ScreenCapture.CaptureScreenshot(path);
                Debug.Log($"[Agent Vision] Yapay Zeka oyun ekranını kontrol etmek için görüntü aldı: {path}");
            }
        }

        private static void OnLogMessage(string logString, string stackTrace, LogType type)
        {
            // Oyundayken kritik bir hata çıkarsa anında kanıt olarak SS al
            if ((type == LogType.Exception || type == LogType.Error) && Application.isPlaying)
            {
                string path = Path.Combine(targetDir, "LastError.png");
                ScreenCapture.CaptureScreenshot(path);
            }
        }
    }
}
