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
    private static readonly string[] WindowNames = new[]
    {
        "MainTabWindow_Animals",
        "MainTabWindow_Architect",
        "MainTabWindow_Assign",
        "MainTabWindow_Factions",
        "MainTabWindow_History",
        "MainTabWindow_Ideos",
        "MainTabWindow_Inspect",
        "MainTabWindow_Mechs",
        "MainTabWindow_Menu",
        "MainTabWindow_PawnTable",
        "MainTabWindow_Quests",
        "MainTabWindow_Research",
        "MainTabWindow_Schedule",
        "MainTabWindow_Wildlife",
        "MainTabWindow_Work",
    };

    public Mod(ModContentPack content) : base(content)
    {
        var harmony = new Harmony("HideUI");
        harmony.PatchAll();

        var prefix = new HarmonyMethod(typeof(Patch_MainTabWindow), nameof(Patch_MainTabWindow.Prefix));
        foreach (var name in WindowNames)
        {
            var type = AccessTools.TypeByName("RimWorld." + name) ?? AccessTools.TypeByName(name);
            if (type == null) continue;
            var method = type.GetMethod("DoWindowContents", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (method != null)
            {
                harmony.Patch(method, prefix: prefix);
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

public static class Patch_MainTabWindow
{
    public static bool Prefix() => !Util.ShouldHide();
}
