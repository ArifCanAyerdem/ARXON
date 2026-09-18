using UnityEditor;
using UnityEngine;

namespace Arixon.EditorScripts
{
    [InitializeOnLoad]
    public class RunSetupTrigger
    {
        static RunSetupTrigger()
        {
            // Sadece bir kere çalışmasını garantilemek için SessionState kullanıyoruz
            if (!SessionState.GetBool("AutoSetupRun_v3", false))
            {
                SessionState.SetBool("AutoSetupRun_v3", true);
                Debug.Log("[AutoTrigger] Karakter kurulumu yapay zeka tarafından (v3) tetikleniyor...");
                CharacterSetupTool.SetupCharacter();
            }
        }
    }
}
