#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using System.Reflection;

public static class ScanUIBindings
{
    [MenuItem("Tools/Scan UI Bindings")]
    public static void Scan()
    {
        Debug.Log("==== Scan UI Bindings Begin ====");

        // 1) Buttons
        foreach (var btn in Object.FindObjectsByType<Button>(FindObjectsSortMode.None))
        {
            var evt = btn.onClick;
            int count = evt.GetPersistentEventCount();
            if (count == 0) continue;

            for (int i = 0; i < count; i++)
            {
                var target = evt.GetPersistentTarget(i);
                var method = evt.GetPersistentMethodName(i);
                Debug.Log($"[Button] {GetPath(btn.transform)} -> {target}.{method}");
            }
        }

        // 2) EventTriggers
        foreach (var et in Object.FindObjectsByType<EventTrigger>(FindObjectsSortMode.None))
        {
            if (et.triggers == null) continue;

            foreach (var entry in et.triggers)
            {
                if (entry.callback == null) continue;
                int count = entry.callback.GetPersistentEventCount();
                if (count == 0) continue;

                for (int i = 0; i < count; i++)
                {
                    var target = entry.callback.GetPersistentTarget(i);
                    var method = entry.callback.GetPersistentMethodName(i);
                    Debug.Log($"[EventTrigger:{entry.eventID}] {GetPath(et.transform)} -> {target}.{method}");
                }
            }
        }

        Debug.Log("==== Scan UI Bindings End ====");
    }

    static string GetPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}
#endif