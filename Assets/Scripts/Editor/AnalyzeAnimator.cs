using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

namespace Arixon.EditorScripts
{
    public static class AnalyzeAnimator
    {
        [MenuItem("Arixon/Analyze Animator")]
        public static void Analyze()
        {
            string path = "Assets/Animations/PlayerAnimator.controller";
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null)
            {
                Debug.LogError("Animator Controller bulunamadı: " + path);
                return;
            }

            Debug.Log($"--- Animator Analizi: {controller.name} ---");
            foreach (var param in controller.parameters)
            {
                Debug.Log($"Parametre: {param.name} ({param.type})");
            }

            foreach (var layer in controller.layers)
            {
                Debug.Log($"Katman: {layer.name}");
                var stateMachine = layer.stateMachine;
                foreach (var stateNode in stateMachine.states)
                {
                    var state = stateNode.state;
                    Debug.Log($"  Durum: {state.name} (Motion: {(state.motion != null ? state.motion.name : "Yok")})");

                    // BlendTree kontrolü
                    if (state.motion is BlendTree blendTree)
                    {
                        Debug.Log($"    -> Bu bir BlendTree. Parametre: {blendTree.blendParameter}");
                        foreach (var child in blendTree.children)
                        {
                            Debug.Log($"       - Motion: {(child.motion != null ? child.motion.name : "Yok")}, Threshold: {child.threshold}");
                        }
                    }

                    foreach (var trans in state.transitions)
                    {
                        string condStr = "";
                        foreach(var cond in trans.conditions) condStr += $"{cond.parameter} {cond.mode} {cond.threshold}, ";
                        Debug.Log($"    -> Geçiş: {trans.destinationState?.name ?? "Exit"} | Süre: {trans.duration}s | Şartlar: {condStr}");
                    }
                }
            }
        }
    }
}
