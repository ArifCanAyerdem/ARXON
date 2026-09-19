using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class IntegrateStarterAssetsCharacter
{
    [MenuItem("Tools/Arixon/Integrate StarterAssets Character")]
    public static void Integrate()
    {
        string baseControllerPath = "Assets/Starter Assets/Runtime/ThirdPersonController/Character/Animations/StarterAssetsThirdPerson.controller";
        string newControllerPath = "Assets/Animations/ArixonPlayerController.overrideController";
        
        // Wait, an AnimatorOverrideController is easier!
        // I will just create an AnimatorOverrideController that overrides the Jump or something?
        // No, I need new states (Kick, Pass). We must copy the controller!
        
        string targetControllerPath = "Assets/Animations/ArixonPlayerController.controller";
        AssetDatabase.CopyAsset(baseControllerPath, targetControllerPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        AnimatorController newController = AssetDatabase.LoadAssetAtPath<AnimatorController>(targetControllerPath);
        
        // Add Parameters
        newController.AddParameter("Kick", AnimatorControllerParameterType.Trigger);
        newController.AddParameter("Pass", AnimatorControllerParameterType.Trigger);

        // Load mixamo animations
        AnimationClip kickAnim = null;
        AnimationClip passAnim = null;
        
        var oldController = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Animations/PlayerAnimator.controller");
        if (oldController != null) {
            foreach(var state in oldController.layers[0].stateMachine.states) {
                if (state.state.name == "Kick") kickAnim = state.state.motion as AnimationClip;
                if (state.state.name == "Pass") passAnim = state.state.motion as AnimationClip;
            }
        }

        var rootStateMachine = newController.layers[0].stateMachine;

        // Create Kick State
        var kickState = rootStateMachine.AddState("Kick");
        kickState.motion = kickAnim;
        
        var anyToKick = rootStateMachine.AddAnyStateTransition(kickState);
        anyToKick.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0, "Kick");
        anyToKick.duration = 0.1f;
        
        var kickToExit = kickState.AddTransition(rootStateMachine.defaultState);
        kickToExit.hasExitTime = true;
        kickToExit.exitTime = 0.8f;

        // Create Pass State
        var passState = rootStateMachine.AddState("Pass");
        passState.motion = passAnim;
        
        var anyToPass = rootStateMachine.AddAnyStateTransition(passState);
        anyToPass.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0, "Pass");
        anyToPass.duration = 0.1f;
        
        var passToExit = passState.AddTransition(rootStateMachine.defaultState);
        passToExit.hasExitTime = true;
        passToExit.exitTime = 0.8f;

        EditorUtility.SetDirty(newController);
        AssetDatabase.SaveAssets();

        // 2. Assign to PlayerPrefab
        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PlayerPrefab.prefab");
        Animator animator = playerPrefab.GetComponentInChildren<Animator>();
        if (animator != null)
        {
            animator.runtimeAnimatorController = newController;
            EditorUtility.SetDirty(playerPrefab);
            PrefabUtility.SavePrefabAsset(playerPrefab);
            Debug.Log("Assigned ArixonPlayerController to PlayerPrefab!");
        }
        else 
        {
            Debug.LogError("No Animator found in PlayerPrefab!");
        }
    }
}
