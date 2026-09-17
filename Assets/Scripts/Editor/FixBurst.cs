using UnityEditor;
using UnityEngine;

namespace Arixon.Editor
{
    [InitializeOnLoad]
    public class FixBurst
    {
        static FixBurst()
        {
            bool currentState = EditorPrefs.GetBool("BurstCompilation", false);
            if (!currentState)
            {
                EditorPrefs.SetBool("BurstCompilation", true);
                Debug.Log("ARİXON: Burst Compilation Registry ayarı ZORLA düzeltildi (true)!");
            }
        }
    }
}
