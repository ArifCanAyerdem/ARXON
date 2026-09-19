using UnityEngine;
using UnityEditor;

namespace Arixon.EditorScripts
{
    public static class CheckAnimator
    {
        [MenuItem("Arixon/Check Animator")]
        public static void Check()
        {
            string playerPath = "Assets/Prefabs/PlayerPrefab.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(playerPath);
            if (prefab == null) return;
            
            Animator anim = prefab.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                Debug.Log("[CheckAnimator] Animator bulundu. Avatar: " + (anim.avatar != null ? anim.avatar.name : "NULL (Eksik!)"));
                Debug.Log("[CheckAnimator] Controller: " + (anim.runtimeAnimatorController != null ? anim.runtimeAnimatorController.name : "NULL (Eksik!)"));
                Debug.Log("[CheckAnimator] ApplyRootMotion: " + anim.applyRootMotion);
            }
            else
            {
                Debug.Log("[CheckAnimator] Animator bulunamadı!");
            }
        }
    }
}
