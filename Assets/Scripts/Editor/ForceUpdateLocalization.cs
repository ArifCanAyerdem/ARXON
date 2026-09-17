using UnityEditor;
using UnityEngine;

namespace Arixon.Editor
{
    [InitializeOnLoad]
    public class ForceUpdateLocalization
    {
        static ForceUpdateLocalization()
        {
            EditorApplication.delayCall += () =>
            {
                EditorApplication.ExecuteMenuItem("ARİXON/Kurulum/Adım 07: Localization Tablosunu Güncelle");
            };
        }
    }
}
