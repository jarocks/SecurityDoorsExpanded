using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace SecurityDoorsExpanded
{
    [HarmonyPatch(typeof(GravshipLandingMarker), "DrawAt")]
    public static class GravshipLandingMarker_DrawAt_Patch
    {
        private static readonly Func<GravshipLandingMarker, bool> Visible = ResolveVisible();

        private static Func<GravshipLandingMarker, bool> ResolveVisible()
        {
            var getter = AccessTools.PropertyGetter(typeof(GravshipLandingMarker), "Visible");
            return getter == null ? null : AccessTools.MethodDelegate<Func<GravshipLandingMarker, bool>>(getter);
        }
        
        public static bool Prepare(MethodBase method) => ModsConfig.OdysseyActive && Visible != null;

        public static void Postfix(GravshipLandingMarker __instance)
        {
            if (!Visible(__instance)) return;

            Graphic_DockingPointOverlay.DrawFor(__instance.gravship, __instance.Position);
        }
    }
}
