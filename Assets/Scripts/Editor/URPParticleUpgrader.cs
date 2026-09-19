using UnityEngine;
using UnityEditor;

public class URPParticleUpgrader
{
    [MenuItem("Tools/Arixon/Upgrade All Particle Materials to URP")]
    public static void UpgradeAllParticles()
    {
        string[] legacyShaders = new string[] 
        { 
            "Particles/Additive", 
            "Particles/Alpha Blended", 
            "Particles/Multiply", 
            "Legacy Shaders/Particles/Additive",
            "Legacy Shaders/Particles/Alpha Blended",
            "Legacy Shaders/Particles/Multiply",
            "Mobile/Particles/Additive",
            "Mobile/Particles/Alpha Blended",
            "Mobile/Particles/Multiply",
            "Standard" // Bazı FX'ler yanlışlıkla Standard kullanır
        };

        Shader urpParticleUnlit = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");

        if (urpParticleUnlit == null)
        {
            Debug.LogError("URP Particle Unlit shader not found! Make sure URP is installed.");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Material");
        int count = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (mat != null && mat.shader != null)
            {
                string shaderName = mat.shader.name;
                bool isLegacyParticle = false;

                foreach (string legacy in legacyShaders)
                {
                    if (shaderName == legacy)
                    {
                        isLegacyParticle = true;
                        break;
                    }
                }

                if (isLegacyParticle)
                {
                    // Update to URP Shader
                    if (shaderName == "Standard" && !path.ToLower().Contains("particle"))
                    {
                        mat.shader = urpLit;
                    }
                    else
                    {
                        mat.shader = urpParticleUnlit;
                    }
                    
                    EditorUtility.SetDirty(mat);
                    count++;
                }
                
                // Extra check for magenta standard particle shaders that might be null or hidden
                if (shaderName.Contains("Error") || shaderName.Contains("Hidden"))
                {
                    if (path.ToLower().Contains("particle") || path.ToLower().Contains("effect"))
                    {
                        mat.shader = urpParticleUnlit;
                        EditorUtility.SetDirty(mat);
                        count++;
                    }
                }
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[URP Upgrader] BaÅŸarÄ±yla {count} adet materyal URP'ye yÃ¼kseltildi!");
    }
}
