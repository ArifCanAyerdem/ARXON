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

            // 1. Modelleri Humanoid yap
            string[] fbxFiles = Directory.GetFiles(modelsFolder, "*.fbx", SearchOption.AllDirectories);
            bool modelsUpdated = false;
            foreach (string file in fbxFiles)
            {
                string assetPath = file.Replace("\\", "/");
                ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
                if (importer != null && importer.animationType != ModelImporterAnimationType.Human)
                {
                    importer.animationType = ModelImporterAnimationType.Human;
                    importer.SaveAndReimport();
                    modelsUpdated = true;
                }
            }
            if (modelsUpdated) AssetDatabase.Refresh();

            // 2. Animator Controller
            string controllerPath = animFolder + "/PlayerAnimator.controller";
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

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
            AnimationClip pass = FindAnim("goalkeeper pass") ?? kickNormal; // Şimdilik ayak pası olarak kick kullan
            AnimationClip fall = FindAnim("soccer trip") ?? FindAnim("fallen idle");

            var rootStateMachine = controller.layers[0].stateMachine;
            rootStateMachine.states = new ChildAnimatorState[0]; // Temizle

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

            // 3. Karakteri Entegre Et
            string prefabPath = "Assets/Prefabs/PlayerPrefab.prefab";
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (playerPrefab != null)
            {
                Transform visuals = playerPrefab.transform.Find("Visuals");
                if (visuals != null)
                {
                    MeshFilter mf = visuals.GetComponent<MeshFilter>();
                    MeshRenderer mr = visuals.GetComponent<MeshRenderer>();
                    if (mf != null) DestroyImmediate(mf, true);
                    if (mr != null) DestroyImmediate(mr, true);

                    // Tam olarak "character.fbx" dosyasını bulmaya çalış
                    GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/character.fbx");
                    if (modelPrefab == null) modelPrefab = FindModelWithMesh(modelsFolder);

                    if (modelPrefab != null)
                    {
                        foreach (Transform child in visuals)
                        {
                            if (child.GetComponent<Animator>() != null || child.name.Contains(modelPrefab.name))
                                DestroyImmediate(child.gameObject, true);
                        }

                        GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab);
                        inst.transform.SetParent(visuals);
                        inst.transform.localPosition = new Vector3(0, -1f, 0);
                        inst.transform.localRotation = Quaternion.identity;

                        Animator anim = inst.GetComponent<Animator>();
                        if (anim != null)
                        {
                            anim.runtimeAnimatorController = controller;
                            anim.applyRootMotion = false;
                        }
                        Debug.Log($"[Arixon Setup] Model {modelPrefab.name} başarıyla eklendi!");
                    }
                    else
                    {
                        Debug.LogError("[Arixon Setup] 'character.fbx' veya Mesh içeren bir model bulunamadı!");
                    }
                }
                EditorUtility.SetDirty(playerPrefab);
                PrefabUtility.SavePrefabAsset(playerPrefab);
            }
            Debug.Log("[Arixon Setup] Tamamlandı!");
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
            string[] guids = AssetDatabase.FindAssets("t:AnimationClip", new[] { "Assets/Models" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.ToLower().Contains(keyword.ToLower()) && !path.Contains("__preview__"))
                {
                    AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                    if (clip != null) return clip;
                }
            }
            return null;
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
