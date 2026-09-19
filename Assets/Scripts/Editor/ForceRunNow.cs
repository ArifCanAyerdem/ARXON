using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class ForceRunNow
{
    static ForceRunNow()
    {
        EditorApplication.delayCall += () => {
            if (!SessionState.GetBool("ForceRunNow_Executed", false))
            {
                SessionState.SetBool("ForceRunNow_Executed", true);
                if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
                Debug.Log("[Arixon Setup] FORCE RUN NOW tetikleniyor...");
                Arixon.EditorScripts.CharacterSetupTool.SetupCharacter();
            }
        };
    }
}
