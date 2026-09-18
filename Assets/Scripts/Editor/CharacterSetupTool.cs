using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;
using System.Linq;

namespace Arixon.EditorScripts
{
    public class CharacterSetupTool : EditorWindow
    {
        [MenuItem("Arixon/Setup 3D Character & Animations (Advanced)")]
        public static void SetupCharacter()
        {
            Debug.Log("[Arixon Setup] 3D Karakter Gelişmiş Kurulumu başlatılıyor...");

            string modelsFolder = "Assets/Models";
            string animFolder = "Assets/Animations";
            if (!AssetDatabase.IsValidFolder(animFolder))
                AssetDatabase.CreateFolder("Assets", "Animations");
            
            // 1. Animator Controller Oluştur
            string controllerPath = animFolder + "/PlayerAnimator.controller";
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null || controller.layers.Length == 0 || controller.layers[0].stateMachine == null)
            {
                if (controller != null)
                {
                    AssetDatabase.DeleteAsset(controllerPath);
                }
                controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            }

            AddParameter(controller, "Speed", AnimatorControllerParameterType.Float);
            AddParameter(controller, "IsGrounded", AnimatorControllerParameterType.Bool);
            AddParameter(controller, "Kick", AnimatorControllerParameterType.Trigger);
            AddParameter(controller, "Pass", AnimatorControllerParameterType.Trigger);
            AddParameter(controller, "Fall", AnimatorControllerParameterType.Bool);

            // Gelişmiş Animasyonları Bul
            AnimationClip idle = FindAnim("idle");
            AnimationClip run = FindAnim("running") ?? FindAnim("jog forward");
            AnimationClip sprint = FindAnim("strike foward jog") ?? run;
            AnimationClip kickNormal = FindAnim("kick soccerball");
            AnimationClip kickStrong = FindAnim("soccer penalty kick") ?? kickNormal;
            AnimationClip pass = FindAnim("goalkeeper pass") ?? kickNormal;
            AnimationClip fall = FindAnim("soccer trip") ?? FindAnim("fallen idle");

            var rootStateMachine = controller.layers[0].stateMachine;
            // Eski state'leri temizle
            var existingStates = rootStateMachine.states;
            foreach (var state in existingStates)
            {
                rootStateMachine.RemoveState(state.state);
            }

            // Locomotion Blend Tree
            BlendTree blendTree;
            AnimatorState locomotion = controller.CreateBlendTreeInController("Locomotion", out blendTree);
            blendTree.blendParameter = "Speed";
            if (idle != null) blendTree.AddChild(idle, 0f);
            if (run != null) blendTree.AddChild(run, 5f);
            if (sprint != null) blendTree.AddChild(sprint, 10f);
            rootStateMachine.defaultState = locomotion;

            // States
            if (kickNormal != null) CreateTriggerState(rootStateMachine, locomotion, kickNormal, "Kick");
            if (pass != null) CreateTriggerState(rootStateMachine, locomotion, pass, "Pass");
            if (fall != null) CreateBoolState(rootStateMachine, locomotion, fall, "Fall");

            // 2. Karakteri Prefab'a Entegre Et
            string prefabPath = "Assets/Prefabs/PlayerPrefab.prefab";
            GameObject playerPrefabInst = PrefabUtility.LoadPrefabContents(prefabPath);
            if (playerPrefabInst != null)
            {
                try
                {
                    Transform faceIndicator = playerPrefabInst.transform.Find("FaceIndicator");
                    if (faceIndicator != null)
                    {
                        DestroyImmediate(faceIndicator.gameObject, true);
                    }

                    Transform visualsHolder = playerPrefabInst.transform.Find("VisualsHolder");
                    if (visualsHolder != null)
                    {
                        string modelPath = "Assets/Models/character.fbx";
                        GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
                        if (modelPrefab == null) modelPrefab = FindModelWithMesh(modelsFolder);

                        if (modelPrefab != null)
                        {
                            // Eski objeleri temizle
                            for (int i = visualsHolder.childCount - 1; i >= 0; i--)
                            {
                                Transform child = visualsHolder.GetChild(i);
                                if (child.GetComponent<Animator>() != null || child.name.Contains(modelPrefab.name) || child.name.Contains("DefaultModel") || child.name.Contains("character"))
                                {
                                    DestroyImmediate(child.gameObject, true);
                                }
                            }

                            // Modeli yerleştir - Normal Instantiate kullanıyoruz ki iç içe prefab hatası olmasın
                            GameObject inst = UnityEngine.Object.Instantiate(modelPrefab, visualsHolder);
                            inst.name = modelPrefab.name; // "(Clone)" yazısını sil
                            inst.transform.localPosition = Vector3.zero; // Pivot 0'da olmalı
                            inst.transform.localRotation = Quaternion.identity;

                            Animator anim = inst.GetComponent<Animator>();
                            if (anim != null)
                            {
                                anim.runtimeAnimatorController = controller;
                                anim.applyRootMotion = false;

                                // _animator referansını PlayerController'a otomatik bağla
                                var pc = playerPrefabInst.GetComponent<Arixon.Gameplay.PlayerController>();
                                if (pc != null)
                                {
                                    SerializedObject so = new SerializedObject(pc);
                                    so.Update();
                                    SerializedProperty animProp = so.FindProperty("_animator");
                                    if (animProp != null)
                                    {
                                        animProp.objectReferenceValue = anim;
                                        so.ApplyModifiedProperties();
                                    }
                                }
                            }
                            Debug.Log($"[Arixon Setup] Model {modelPrefab.name} başarıyla eklendi ve Animator bağlandı!");
                        }
                        else
                        {
                            Debug.LogError("[Arixon Setup] 'character.fbx' veya Mesh içeren bir model bulunamadı!");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("[Arixon Setup] PlayerPrefab'da 'VisualsHolder' alt objesi bulunamadı!");
                    }

                    PrefabUtility.SaveAsPrefabAsset(playerPrefabInst, prefabPath);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(playerPrefabInst);
                }
            }
            
            Debug.Log("[Arixon Setup] Kurulum İşlemi Bitti!");
        }

        private static void AddParameter(AnimatorController ctrl, string name, AnimatorControllerParameterType type)
        {
            if (!ctrl.parameters.Any(p => p.name == name)) ctrl.AddParameter(name, type);
        }

        private static void CreateTriggerState(AnimatorStateMachine sm, AnimatorState returnState, AnimationClip clip, string trigger)
        {
            AnimatorState state = sm.AddState(trigger);
            state.motion = clip;
            var trans = returnState.AddTransition(state);
            trans.AddCondition(AnimatorConditionMode.If, 0, trigger);
            trans.hasExitTime = false;
            trans.duration = 0.1f;

            var retTrans = state.AddTransition(returnState);
            retTrans.hasExitTime = true;
            retTrans.exitTime = 0.8f;
            retTrans.duration = 0.2f;
        }

        private static void CreateBoolState(AnimatorStateMachine sm, AnimatorState returnState, AnimationClip clip, string boolParam)
        {
            AnimatorState state = sm.AddState(boolParam);
            state.motion = clip;
            var trans = sm.AddAnyStateTransition(state);
            trans.AddCondition(AnimatorConditionMode.If, 0, boolParam);
            trans.hasExitTime = false; trans.duration = 0.1f;

            var retTrans = state.AddTransition(returnState);
            retTrans.AddCondition(AnimatorConditionMode.IfNot, 0, boolParam);
            retTrans.hasExitTime = false; retTrans.duration = 0.2f;
        }

        private static AnimationClip FindAnim(string keyword)
        {
            string[] guids = AssetDatabase.FindAssets("t:AnimationClip", new[] { "Assets/Models", "Assets/Animations" });
            AnimationClip partialMatch = null;
            foreach (var guid in guids)
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = System.IO.Path.GetFileNameWithoutExtension(p).ToLower();
                
                // Tam eşleşme (Örn: Sadece "idle")
                if (fileName == keyword.ToLower())
                {
                    return AssetDatabase.LoadAssetAtPath<AnimationClip>(p);
                }
                
                // Kısmi eşleşme (Ama fallen idle olmasın, eğer sadece idle aranıyorsa)
                if (fileName.Contains(keyword.ToLower()))
                {
                    if (keyword.ToLower() == "idle" && fileName.Contains("fallen")) continue;
                    
                    if (partialMatch == null)
                    {
                        partialMatch = AssetDatabase.LoadAssetAtPath<AnimationClip>(p);
                    }
                }
            }
            return partialMatch;
        }

        private static GameObject FindModelWithMesh(string folder)
        {
            string[] guids = AssetDatabase.FindAssets("t:Model", new[] { folder });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (obj != null && obj.GetComponentInChildren<SkinnedMeshRenderer>() != null) return obj;
            }
            return null;
        }
    }
}
