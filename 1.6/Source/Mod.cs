using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace HideUI;

public class Mod : Verse.Mod
{
    public Mod(ModContentPack content) : base(content)
    {
        var harmony = new Harmony("HideUI");
        harmony.PatchAll();
        PatchWindowStack(harmony);
    }

    private static void PatchWindowStack(Harmony harmony)
    {
        var type = typeof(WindowStack);
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (var method in methods)
        {
            if (method.Name == "WindowStackOnGUI")
            {
                harmony.Patch(method, prefix: new HarmonyMethod(typeof(Patch_WindowStack), nameof(Patch_WindowStack.Prefix)),
                    finalizer: new HarmonyMethod(typeof(Patch_WindowStack), nameof(Patch_WindowStack.Finalizer)));
            }
        }
    }
}

public static class Util
{
    public static bool ShouldHide() => Input.GetMouseButton(2);
}

[HarmonyPatch]
public static class Patch_GizmoGridDrawer
{
    private static MethodBase _cached;

    [HarmonyPrepare]
    private static bool Prepare()
    {
        _cached = FindTarget();
        return _cached != null;
    }

    private static MethodBase TargetMethod() => _cached;

    private static MethodBase FindTarget()
    {
        try
        {
            var type = AccessTools.TypeByName("RimWorld.GizmoGridDrawer") ?? AccessTools.TypeByName("GizmoGridDrawer");
            if (type == null) return null;
            var methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var m in methods)
            {
                var ps = m.GetParameters();
                if (ps.Length >= 1 && typeof(IEnumerable<Gizmo>).IsAssignableFrom(ps[0].ParameterType))
                    return m;
            }
        }
        catch { }
        return null;
    }

    private static void Prefix(ref IEnumerable<Gizmo> __0)
    {
        if (Util.ShouldHide())
            __0 = Enumerable.Empty<Gizmo>();
    }
}

[HarmonyPatch]
public static class Patch_InspectPaneFiller
{
    private static MethodBase _cached;

    [HarmonyPrepare]
    private static bool Prepare()
    {
        _cached = FindTarget();
        return _cached != null;
    }

    private static MethodBase TargetMethod() => _cached;

    private static MethodBase FindTarget()
    {
        try
        {
            var type = AccessTools.TypeByName("RimWorld.InspectPaneFiller") ?? AccessTools.TypeByName("InspectPaneFiller");
            if (type == null) return null;
            return type.GetMethod("DoInspectPaneButtons", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        }
        catch { }
        return null;
    }

    private static bool Prefix() => !Util.ShouldHide();
}

[HarmonyPatch(typeof(Window), "WindowOnGUI")]
public static class Patch_AllWindows
{
    private static bool Prefix() => !Util.ShouldHide();
}

public static class Patch_WindowStack
{
    private static Type _mainTabWindowType;
    private static FieldInfo _windowsField;
    private static List<Window> _removedWindows;
    private static WindowStack _instance;

    public static bool Prefix(WindowStack __instance)
    {
        if (!Util.ShouldHide())
            return true;

        if (_mainTabWindowType == null)
        {
            _mainTabWindowType = AccessTools.TypeByName("RimWorld.MainTabWindow") ?? AccessTools.TypeByName("MainTabWindow");
            _windowsField = AccessTools.Field(typeof(WindowStack), "windows");
        }

        if (_mainTabWindowType == null || _windowsField == null)
            return true;

        var windows = _windowsField.GetValue(__instance) as List<Window>;
        if (windows == null)
            return true;

        _instance = __instance;
        _removedWindows = new List<Window>();
        for (int i = windows.Count - 1; i >= 0; i--)
        {
            if (_mainTabWindowType.IsInstanceOfType(windows[i]))
            {
                _removedWindows.Add(windows[i]);
                windows.RemoveAt(i);
            }
        }

        return true;
    }

    public static void Finalizer(Exception __exception)
    {
        if (_removedWindows != null && _removedWindows.Count > 0 && _instance != null)
        {
            var windows = _windowsField.GetValue(_instance) as List<Window>;
            if (windows != null)
            {
                windows.AddRange(_removedWindows);
            }
        }
        _removedWindows = null;
        _instance = null;
    }
}
