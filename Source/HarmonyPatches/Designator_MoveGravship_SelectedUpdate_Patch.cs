using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace SecurityDoorsExpanded
{
    [HarmonyPatch(typeof(Designator_MoveGravship), nameof(Designator_MoveGravship.SelectedUpdate))]
    public static class Designator_MoveGravship_SelectedUpdate_Patch
    {
        public static bool Prepare(MethodBase method) => ModsConfig.OdysseyActive;
        
        public static void Postfix(Designator_MoveGravship __instance)
        {
            var marker = __instance.marker;
            var gravship = marker?.gravship;
            if (gravship == null) return;

            var root = PrefabUtility.GetRoot(__instance.AdjustedMouseCell, gravship.Bounds.Size, 
                marker.GravshipRotation);
            DockingPointOverlay.DrawFor(gravship, root);
        }
    }
}