using RimWorld;
using Verse;

namespace SecurityDoorsExpanded
{
    public static class DefRefs
    {
        private static ResearchProjectDef orbitalTech;
        private static StatDef vacuumResistance;
        private static ThingDef gravlitePanel;

        public static ResearchProjectDef OrbitalTech =>
            orbitalTech ?? (orbitalTech = DefDatabase<ResearchProjectDef>.GetNamedSilentFail("OrbitalTech"));

        public static StatDef VacuumResistance =>
            vacuumResistance ?? (vacuumResistance = DefDatabase<StatDef>.GetNamedSilentFail("VacuumResistance"));

        public static ThingDef GravlitePanel =>
            gravlitePanel ?? (gravlitePanel = DefDatabase<ThingDef>.GetNamedSilentFail("GravlitePanel"));

        public static bool VacBarrierTechUnlocked => OrbitalTech?.IsFinished == true;
    }

    [DefOf]
    public static class SDE_DefOf
    {
        [MayRequireOdyssey]
        public static JobDef SDE_InstallVacBarrierJob;

        [MayRequireOdyssey]
        public static DesignationDef SDE_InstallVacBarrier;
        
        public static JobDef SDE_OpenVehicleDoorJob;

        public static JobDef SDE_CloseVehicleDoorJob;

        public static DesignationDef SDE_OpenVehicleDoor;

        public static DesignationDef SDE_CloseVehicleDoor;

        public static SoundDef SDE_Lockdown;

        static SDE_DefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(SDE_DefOf));
        }
    }
}
