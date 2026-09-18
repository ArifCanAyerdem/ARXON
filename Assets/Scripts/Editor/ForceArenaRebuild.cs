using UnityEditor;
using UnityEngine;

namespace Arixon.Editor
{
    [InitializeOnLoad]
    public static class ForceArenaRebuild
    {
        static ForceArenaRebuild()
        {
            if (!EditorPrefs.GetBool("ARIXON_ForceArenaRebuild_v2_Done", false))
            {
                EditorApplication.delayCall += RebuildOnce;
            }
        }

        private static void RebuildOnce()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            
            EditorPrefs.SetBool("ARIXON_ForceArenaRebuild_v2_Done", true);
            Debug.Log("Arena kalelerin renklenmesi için OTOMATİK olarak yeniden inşa ediliyor...");
            Arixon.Editor.SetupGameArena.BuildArena();
        }

        [MenuItem("ARİXON/Fix/Arenayı Yeniden Boya")]
        public static void RebuildManual()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("Play modundayken sahne düzenlenemez. Lütfen oyunu durdurun.");
                return;
            }
            Debug.Log("Arena kalelerin renklenmesi için MANUEL olarak yeniden inşa ediliyor...");
            Arixon.Editor.SetupGameArena.BuildArena();
        }
    }
}
