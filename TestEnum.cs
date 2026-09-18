using UnityEditor;
using System;
public class TestEnum {
    public static void Main() {
        foreach (var name in Enum.GetNames(typeof(ModelImporterMaterialLocation))) {
            Console.WriteLine(name);
        }
    }
}
