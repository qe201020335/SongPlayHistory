using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace SongPlayHistory.Patches;

//Modified from MappingExtensions
[HarmonyPatch(typeof(BeatmapObjectsInTimeRowProcessor), nameof(BeatmapObjectsInTimeRowProcessor.HandleCurrentTimeSliceAllNotesAndSlidersDidFinishTimeSlice))]
public static class BeatmapLoadingPatch
{
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        // Prevents an IndexOutOfRangeException when processing precise line indexes.
        var matcher = new CodeMatcher(instructions);
        matcher.MatchEndForward(
                new CodeMatch(OpCodes.Ldloc_S),
                new CodeMatch(OpCodes.Callvirt),
                new CodeMatch(OpCodes.Ldelem_Ref));
        if (matcher.IsInvalid)
        {
            Plugin.Log.Warn("Failed to patch BeatmapObjectsInTimeRowProcessor. Ignore this if MappingExtensions is installed");
            return matcher.InstructionEnumeration();
        }
        
        matcher.Insert(
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Ldc_I4_3),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Math), nameof(Math.Clamp), new[] { typeof(int), typeof(int), typeof(int) })));
        
        Plugin.Log.Debug("BeatmapObjectsInTimeRowProcessor patched");
        return matcher.InstructionEnumeration();
    }
}