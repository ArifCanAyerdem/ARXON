using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Arixon.EditorScripts
{
    public class ApplyEnhancements : EditorWindow
    {
        [MenuItem("Arixon/Apply Enhancements")]
        public static void Apply()
        {
            ApplyPlayerFixes();
            ApplyBallFixes();
            ApplyMapFixes();
            Debug.Log("[Arixon] Tüm iyileştirmeler başarıyla uygulandı!");
        }

        private static void ApplyPlayerFixes()
        {
            string playerPath = "Assets/Prefabs/PlayerPrefab.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(playerPath);
            if (prefab == null) return;

            using (var scope = new PrefabUtility.EditPrefabContentsScope(playerPath))
            {
                GameObject root = scope.prefabContentsRoot;

                // 1. Karakteri Büyüt
                root.transform.localScale = new Vector3(1.4f, 1.4f, 1.4f);

                // 2. Siyah Kutu (FaceIndicator) Gizle
                Transform faceIndicator = root.transform.Find("FaceIndicator");
                if (faceIndicator != null)
                {
                    faceIndicator.gameObject.SetActive(false);
                }

                // 3. Animasyon Root Motion Fix
                Animator anim = root.GetComponentInChildren<Animator>();
                if (anim != null)
                {
                    anim.applyRootMotion = false;
                }

                // 4. Ragdoll Kurulumu
                SetupRagdoll(anim);
            }
        }

        private static void SetupRagdoll(Animator anim)
        {
            if (anim == null) return;

            Transform hips = anim.GetBoneTransform(HumanBodyBones.Hips);
            if (hips == null) return;

            // Önceki ragdoll bileşenlerini temizle (varsa)
            var joints = hips.GetComponentsInChildren<CharacterJoint>();
            foreach (var j in joints) DestroyImmediate(j);
            var rbs = hips.GetComponentsInChildren<Rigidbody>();
            foreach (var r in rbs) DestroyImmediate(r);
            var cols = hips.GetComponentsInChildren<Collider>();
            foreach (var c in cols) DestroyImmediate(c);

            // Basit Ragdoll Kemik Haritası
            Dictionary<HumanBodyBones, float> boneRadius = new Dictionary<HumanBodyBones, float>()
            {
                { HumanBodyBones.Hips, 0.2f },
                { HumanBodyBones.Spine, 0.2f },
                { HumanBodyBones.Chest, 0.2f },
                { HumanBodyBones.Head, 0.15f },
                { HumanBodyBones.LeftUpperLeg, 0.1f },
                { HumanBodyBones.LeftLowerLeg, 0.1f },
                { HumanBodyBones.RightUpperLeg, 0.1f },
                { HumanBodyBones.RightLowerLeg, 0.1f },
                { HumanBodyBones.LeftUpperArm, 0.08f },
                { HumanBodyBones.LeftLowerArm, 0.08f },
                { HumanBodyBones.RightUpperArm, 0.08f },
                { HumanBodyBones.RightLowerArm, 0.08f }
            };

            foreach (var kvp in boneRadius)
            {
                Transform t = anim.GetBoneTransform(kvp.Key);
                if (t != null)
                {
                    var rb = t.gameObject.AddComponent<Rigidbody>();
                    rb.mass = 10f;
                    rb.isKinematic = true; // Varsayılan olarak kapalı

                    var col = t.gameObject.AddComponent<CapsuleCollider>();
                    col.radius = kvp.Value;
                    col.height = kvp.Value * 4f;
                    col.direction = 0; // X axis usually for Mixamo
                }
            }

            // Basit Joint bağlantıları (Çok temel, detaylı RagdollBuilder kadar mükemmel olmayabilir ama iş görür)
            ConnectJoint(anim, HumanBodyBones.Spine, HumanBodyBones.Hips);
            ConnectJoint(anim, HumanBodyBones.Chest, HumanBodyBones.Spine);
            ConnectJoint(anim, HumanBodyBones.Head, HumanBodyBones.Chest);
            ConnectJoint(anim, HumanBodyBones.LeftUpperLeg, HumanBodyBones.Hips);
            ConnectJoint(anim, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftUpperLeg);
            ConnectJoint(anim, HumanBodyBones.RightUpperLeg, HumanBodyBones.Hips);
            ConnectJoint(anim, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightUpperLeg);
            ConnectJoint(anim, HumanBodyBones.LeftUpperArm, HumanBodyBones.Chest);
            ConnectJoint(anim, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftUpperArm);
            ConnectJoint(anim, HumanBodyBones.RightUpperArm, HumanBodyBones.Chest);
            ConnectJoint(anim, HumanBodyBones.RightLowerArm, HumanBodyBones.RightUpperArm);
        }

        private static void ConnectJoint(Animator anim, HumanBodyBones child, HumanBodyBones parent)
        {
            Transform tChild = anim.GetBoneTransform(child);
            Transform tParent = anim.GetBoneTransform(parent);

            if (tChild != null && tParent != null)
            {
                var rbParent = tParent.GetComponent<Rigidbody>();
                if (rbParent != null)
                {
                    var joint = tChild.gameObject.AddComponent<CharacterJoint>();
                    joint.connectedBody = rbParent;
                    joint.enableProjection = true;
                }
            }
        }

        private static void ApplyBallFixes()
        {
            string ballPath = "Assets/Prefabs/GameBall.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ballPath);
            if (prefab == null) return;

            using (var scope = new PrefabUtility.EditPrefabContentsScope(ballPath))
            {
                GameObject root = scope.prefabContentsRoot;
                root.transform.localScale = new Vector3(1.3f, 1.3f, 1.3f);
            }
        }

        private static void ApplyMapFixes()
        {
            string scenePath = "Assets/Scenes/GameScene.unity";
            if (UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path != scenePath)
            {
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
            }

            GameObject arena = GameObject.Find("ArenaGround");
            if (arena != null)
            {
                float scaleMultiplier = 1.35f;

                // Ground'u büyüt
                arena.transform.localScale = new Vector3(
                    arena.transform.localScale.x * scaleMultiplier, 
                    arena.transform.localScale.y, 
                    arena.transform.localScale.z * scaleMultiplier);

                // Duvar ve kaleleri taşı/büyüt
                string[] objsToAdjust = { "Wall_North", "Wall_South", "Wall_East", "Wall_West", "Goal_Red", "Goal_Blue" };
                foreach (string objName in objsToAdjust)
                {
                    GameObject obj = GameObject.Find(objName);
                    if (obj != null)
                    {
                        Vector3 pos = obj.transform.position;
                        pos.x *= scaleMultiplier;
                        pos.z *= scaleMultiplier;
                        obj.transform.position = pos;

                        if (obj.name.StartsWith("Wall"))
                        {
                            Vector3 scale = obj.transform.localScale;
                            // Duvarların uzunluğunu da artır
                            if (obj.name.Contains("North") || obj.name.Contains("South"))
                                scale.x *= scaleMultiplier;
                            else
                                scale.z *= scaleMultiplier;
                            obj.transform.localScale = scale;
                        }
                    }
                }

                UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            }
        }
    }
}
