using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class ExecuteNow8
{
    static ExecuteNow8()
    {
        if (!SessionState.GetBool("ExecuteNow8_Done", false))
        {
            SessionState.SetBool("ExecuteNow8_Done", true);
            EditorApplication.delayCall += () => {
                Debug.Log(">>> YAPAY ZEKA ZORUNLU KURULUMU BAÞLATIYOR 8 <<<");
                Arixon.EditorScripts.CharacterSetupTool.SetupCharacter();
            };
        }
    }
}
