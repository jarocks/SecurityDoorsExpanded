using System.Reflection;
using HarmonyLib;
using Verse;

namespace SecurityDoorsExpanded
{
    [HarmonyPatch]
    public static class VehicleFramework_ImpassableForVehicles_Patch
    {
        public static bool Prepare(MethodBase method)
        {
            if (method != null)
            {
                return true;
            }

            if (!ModLister.AnyModActiveNoSuffix(new[] { "SmashPhil.VehicleFramework" }))
            {
                return false;
            }

            if (TargetMethod() != null)
            {
                return true;
            }

            // Should only fire in the event of major upstream changes to VF (so ideally never)
            Log.Error("[SDE] Vehicle Framework: Vehicles.GenGridVehicles:ImpassableForVehicles could not be found.");
            return false;
        }

        public static MethodBase TargetMethod() => AccessTools.Method("Vehicles.GenGridVehicles:ImpassableForVehicles");

        public static void Postfix(ThingDef __0, ref bool __result)
        {
            // Make hangar doors passable for vehicles
            if (__result && __0 != null && typeof(Building_VehicleDoor).IsAssignableFrom(__0.thingClass))
            {
                __result = false;
            }
        }
    }
}