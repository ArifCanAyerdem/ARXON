using UnityEngine;
using UnityEngine.SceneManagement;

namespace Arixon.Gameplay
{
    public static class AutoFixGroundRuntime
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnRuntimeMethodLoad()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            FixAllGroundsInActiveScene();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            FixAllGroundsInActiveScene();
        }

        private static void FixAllGroundsInActiveScene()
        {
            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpShader == null) return;

            Renderer[] allRenderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Renderer rend in allRenderers)
            {
                if (rend.sharedMaterial != null)
                {
                    // Eğer materyal bozuksa (Magenta), Standard ise veya varsayılan materyalse
                    if (rend.sharedMaterial.shader.name == "Hidden/InternalErrorShader" || 
                        rend.sharedMaterial.shader.name == "Standard" || 
                        rend.sharedMaterial.name == "Default-Material")
                    {
                        Material fixedMat = new Material(urpShader);

                        // Objelerin ismine göre renk verelim
                        string objName = rend.gameObject.name.ToLower();
                        if (objName.Contains("floor") || objName.Contains("ground") || objName.Contains("arena"))
                        {
                            fixedMat.color = new Color(0.2f, 0.2f, 0.25f); // Lobi zemini / Oyun zemini için renk
                            Debug.Log($"[Gameplay] [AutoFixGroundRuntime] -> {rend.gameObject.name} zemin materyali oyunda anında düzeltildi!");
                        }
                        else
                        {
                            fixedMat.color = Color.gray;
                        }

                        // Yeni materyali ata (rend.material kullanarak clone'luyoruz, böylece Default Material hatası vermez)
                        rend.material = fixedMat;
                    }
                }
            }
        }
    }
}
