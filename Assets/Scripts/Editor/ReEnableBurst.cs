using UnityEditor;
using UnityEngine;
using System.Reflection;
using System;

namespace Arixon.Editor
{
    [InitializeOnLoad]
    public static class ReEnableBurst
    {
        static ReEnableBurst()
        {
            try
            {
                Type t = Type.GetType("Unity.Burst.Editor.BurstEditorOptions, Unity.Burst.Editor");
                if (t != null)
                {
                    PropertyInfo p = t.GetProperty("EnableBurstCompilation", BindingFlags.Static | BindingFlags.Public);
                    if (p != null)
                    {
                        bool current = (bool)p.GetValue(null);
                        if (!current)
                        {
                            p.SetValue(null, true);
                            Debug.Log("ARİXON: Burst Compilation TEKRAR AKTİFLEŞTİRİLDİ!");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("ARİXON Burst Açma Hatası: " + e.Message);
            }
        }
    }
}
