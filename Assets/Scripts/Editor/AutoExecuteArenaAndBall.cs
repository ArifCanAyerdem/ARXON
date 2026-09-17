using UnityEditor;
using UnityEngine;

namespace Arixon.Editor
{
    public static class AutoExecuteArenaAndBall
    {
        [InitializeOnLoadMethod]
        public static void RunOnce()
        {
            if (!EditorPrefs.GetBool("ARIXON_ArenaAndBall_Built", false))
            {
                EditorApplication.delayCall += () =>
                {
                    if (EditorApplication.isPlaying) return;

                    // 1. Topu oluştur
                    SetupGameBall.CreateAndAssignGameBall();
                    
                    // 2. Arenayı oluştur
                    SetupGameArena.BuildArena();
                    
                    EditorPrefs.SetBool("ARIXON_ArenaAndBall_Built", true);
                    Debug.Log("ARİXON: Top ve Arena otomatik olarak inşa edildi!");
                };
            }
        }
    }
}
