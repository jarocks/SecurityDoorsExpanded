using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace SecurityDoorsExpanded
{
    public class WorkGiver_InstallVacBarrier : WorkGiver_Scanner
    {
        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public override Danger MaxPathDanger(Pawn pawn) => Danger.Deadly;

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            return pawn.Map.designationManager.SpawnedDesignationsOfDef(SDE_DefOf.SDE_InstallVacBarrier)
                .Select(designation => designation.target.Thing);
        }

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            return !pawn.Map.designationManager.AnySpawnedDesignationOfDef(SDE_DefOf.SDE_InstallVacBarrier);
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            var comp = t.TryGetComp<CompVacDoor>();
            if (comp?.vacBarrierInstalled == true ||
                t.Map.designationManager.DesignationOn(t, SDE_DefOf.SDE_InstallVacBarrier) == null ||
                t.IsForbidden(pawn) || t.IsBurning() || !pawn.CanReserve(t, 1, -1, null, forced))
            {
                return false;
            }

            // Handle resuming construction
            if (comp.panelsDelivered || FindPanels(pawn, comp.Props.panelCost) != null)
            {
                return true;
            }

            JobFailReason.Is("SDE_NeedGravlitePanels".Translate(comp.Props.panelCost));
            return false;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            var comp = t.TryGetComp<CompVacDoor>();
            if (comp == null)
            {
                return null;
            }

            if (comp.panelsDelivered)
            {
                return JobMaker.MakeJob(SDE_DefOf.SDE_InstallVacBarrierJob, t);
            }

            var needed = comp.Props.panelCost;
            var panels = FindPanels(pawn, needed);
            if (panels == null)
            {
                return null;
            }

            var job = JobMaker.MakeJob(SDE_DefOf.SDE_InstallVacBarrierJob, t, panels);
            job.count = needed;
            return job;
        }

        private static Thing FindPanels(Pawn pawn, int count)
        {
            return GenClosest.ClosestThingReachable(
                pawn.Position, pawn.Map,
                ThingRequest.ForDef(DefRefs.GravlitePanel),
                PathEndMode.ClosestTouch,
                TraverseParms.For(pawn),
                9999f,
                thing => !thing.IsForbidden(pawn) && thing.stackCount >= count &&
                         pawn.CanReserve(thing, 1, count));
        }
    }
}