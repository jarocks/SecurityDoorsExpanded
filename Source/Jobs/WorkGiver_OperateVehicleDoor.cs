using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace SecurityDoorsExpanded
{
    public abstract class WorkGiver_OperateVehicleDoor : WorkGiver_Scanner
    {
        protected abstract DesignationDef Designation { get; }

        protected abstract JobDef Job { get; }

        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public override Danger MaxPathDanger(Pawn pawn) => Danger.Deadly;

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            return pawn.Map.designationManager.SpawnedDesignationsOfDef(Designation)
                .Select(designation => designation.target.Thing);
        }

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            return !pawn.Map.designationManager.AnySpawnedDesignationOfDef(Designation);
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!(t is Building_VehicleDoor) ||
                t.Map.designationManager.DesignationOn(t, Designation) == null)
            {
                return false;
            }

            return pawn.CanReserve(t, 1, -1, null, forced);
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return JobMaker.MakeJob(Job, t);
        }
    }

    public class WorkGiver_OpenVehicleDoor : WorkGiver_OperateVehicleDoor
    {
        protected override DesignationDef Designation => SDE_DefOf.SDE_OpenVehicleDoor;

        protected override JobDef Job => SDE_DefOf.SDE_OpenVehicleDoorJob;
    }

    public class WorkGiver_CloseVehicleDoor : WorkGiver_OperateVehicleDoor
    {
        protected override DesignationDef Designation => SDE_DefOf.SDE_CloseVehicleDoor;

        protected override JobDef Job => SDE_DefOf.SDE_CloseVehicleDoorJob;
    }
}
