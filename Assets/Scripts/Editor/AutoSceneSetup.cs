using UnityEngine;
using UnityEditor;
using Arixon.Gameplay;

namespace Arixon.EditorScripts
{
    [InitializeOnLoad]
    public static class AutoSceneSetup
    {
        static AutoSceneSetup()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // EditMode'dan çıkarken değil, tam olarak PlayMode'a girildiğinde çalıştır ki Unity değişiklikleri sıfırlamasın!
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                SetupGoals();
            }
        }

        [MenuItem("Arixon/Auto Setup Scene (Fix Missing Scripts)")]
        public static void SetupGoals()
        {
            GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            int goalsFixed = 0;

            foreach (var go in allObjects)
            {
                string lowerName = go.name.ToLower();
                
                // Eğer ismi kale veya goal içeriyorsa
                if (lowerName.Contains("kale") || lowerName.Contains("goal"))
                {
                    // Eğer sahne objesiyse (Prefab değilse)
                    if (go.scene.IsValid())
                    {
                        GoalTrigger trigger = go.GetComponent<GoalTrigger>();
                        if (trigger == null)
                        {
                            trigger = go.AddComponent<GoalTrigger>();
                            Debug.LogWarning($"[AutoSetup] '{go.name}' objesine GoalTrigger scripti otomatik eklendi!");
                            goalsFixed++;
                        }
                    }
                }
            }

            if (goalsFixed > 0)
            {
                Debug.Log($"[AutoSetup] Toplam {goalsFixed} adet kaleye eksik GoalTrigger scripti başarıyla atandı. Kullanıcı müdahalesi gereksinimi ortadan kaldırıldı.");
            }
        }
    }
}
