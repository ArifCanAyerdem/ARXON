using UnityEngine;
using UnityEditor;

namespace Arixon.EditorScripts
{
    public class FixPlayerBugs : EditorWindow
    {
        [MenuItem("Arixon/Fix Player Bugs")]
        public static void Apply()
        {
            string playerPath = "Assets/Prefabs/PlayerPrefab.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(playerPath);
            if (prefab == null) return;

            using (var scope = new PrefabUtility.EditPrefabContentsScope(playerPath))
            {
                GameObject root = scope.prefabContentsRoot;

                // 1. FaceIndicator'ı tamamen sil
                Transform[] allTransforms = root.GetComponentsInChildren<Transform>(true);
                foreach (Transform t in allTransforms)
                {
                    if (t.name == "FaceIndicator")
                    {
                        DestroyImmediate(t.gameObject);
                        break; // Silindi
                    }
                }

                // 2. Yanlış Ragdoll kurulumlarını temizle (Animasyonları bozuyordu)
                Animator anim = root.GetComponentInChildren<Animator>();
                if (anim != null)
                {
                    Transform hips = anim.GetBoneTransform(HumanBodyBones.Hips);
                    if (hips != null)
                    {
                        var joints = hips.GetComponentsInChildren<CharacterJoint>(true);
                        foreach (var j in joints) DestroyImmediate(j);

                        var rbs = hips.GetComponentsInChildren<Rigidbody>(true);
                        foreach (var r in rbs) DestroyImmediate(r);

                        var cols = hips.GetComponentsInChildren<Collider>(true);
                        foreach (var c in cols) DestroyImmediate(c);
                    }
                }
            }

            Debug.Log("[Arixon] Karakter hataları düzeltildi! FaceIndicator silindi ve animasyonları bozan fizik eklentileri temizlendi.");
        }
    }
}
