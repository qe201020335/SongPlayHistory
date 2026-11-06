using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace SongPlayHistory.Patches;

//Modified from MappingExtensions
[HarmonyPatch(typeof(BeatmapObjectsInTimeRowProcessor), nameof(BeatmapObjectsInTimeRowProcessor.HandleCurrentTimeSliceAllNotesAndSlidersDidFinishTimeSlice))]
public static class BeatmapLoadingPatch
{
    private static readonly MethodInfo ClampMethod = AccessTools.Method(typeof(Math), nameof(Math.Clamp),
        new[] { typeof(int), typeof(int), typeof(int) });

    private static readonly MethodInfo ClampMethod2 = AccessTools.Method(typeof(Mathf), nameof(Mathf.Clamp),
        new[] { typeof(int), typeof(int), typeof(int) });

    [HarmonyPrepare]
    // MappingExtensions' patch will throw if we happen to patch it first
    // Proactively check if NoodleExtensions is installed too
    private static bool Prepare()
    {
        if (Plugin.Instance.MappingExtensionsInstalled || Plugin.Instance.NoodleExtensionsInstalled)
        {
            Plugin.Log.Debug(
                "MappingExtensions or NoodleExtensions is installed, not patching BeatmapObjectsInTimeRowProcessor");
            return false;
        }

        return true;
    }

    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var instructs = new List<CodeInstruction>(instructions);
        var patched =
            instructs.Exists(instruction => instruction.Calls(ClampMethod) || instruction.Calls(ClampMethod2));
        if (patched)
        {
            Plugin.Log.Debug("BeatmapObjectsInTimeRowProcessor is already patched. Skipping SPH's patch");
            return instructs;
        }

        Plugin.Log.Debug("Patching BeatmapObjectsInTimeRowProcessor");
        // Prevents an IndexOutOfRangeException when processing precise line indexes.
        var matcher = new CodeMatcher(instructs);
        matcher.MatchEndForward(
                new CodeMatch(OpCodes.Ldloc_S),
                new CodeMatch(OpCodes.Callvirt),
                new CodeMatch(OpCodes.Ldelem_Ref))
            .ThrowIfInvalid("Failed to patch BeatmapObjectsInTimeRowProcessor.")
            .Insert(
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Ldc_I4_3),
                new CodeInstruction(OpCodes.Call, ClampMethod));

        Plugin.Log.Debug("BeatmapObjectsInTimeRowProcessor patched");
        return matcher.InstructionEnumeration();
    }
}