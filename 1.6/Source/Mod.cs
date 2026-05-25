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
            if (method.Name.Contains("Window") && method.Name.Contains("GUI"))
            {
                harmony.Patch(method, prefix: new HarmonyMethod(typeof(Patch_WindowStack), nameof(Patch_WindowStack.Prefix)));
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
public static class Patch_MainButtonsRoot
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
            var type = AccessTools.TypeByName("RimWorld.MainButtonsRoot") ?? AccessTools.TypeByName("MainButtonsRoot");
            if (type == null) return null;
            return type.GetMethod("MainButtonsOnGUI", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        }
        catch { }
        return null;
    }

    private static bool Prefix() => !Util.ShouldHide();
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

        for (int i = windows.Count - 1; i >= 0; i--)
        {
            if (_mainTabWindowType.IsInstanceOfType(windows[i]))
            {
                windows.RemoveAt(i);
            }
        }

        return true;
    }
}
