using UnityEditor;
using UnityEngine;

public class RunSetup
{
    [MenuItem("Tools/RunSetup")]
    public static void Run()
    {
        Arixon.Editor.SetupGameBall.CreateAndAssignGameBall();
        Debug.Log("SETUP FINISHED");
    }
}
