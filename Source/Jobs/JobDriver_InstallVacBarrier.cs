using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace SecurityDoorsExpanded
{
    public class JobDriver_InstallVacBarrier : JobDriver
    {
        private Thing DoorThing => job.GetTarget(TargetIndex.A).Thing;

        private CompVacDoor CompVacBarrier => (DoorThing as Building_VacDoor)?.VacBarrier;

        private bool NeedsPanels => job.GetTarget(TargetIndex.B).IsValid;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (!pawn.Reserve(job.GetTarget(TargetIndex.A), job, 1, -1, null, errorOnFailed))
            {
                return false;
            }

            return !NeedsPanels || pawn.Reserve(job.GetTarget(TargetIndex.B), job, 1, job.count, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOn(() =>
            {
                var comp = CompVacBarrier;
                return comp == null || comp.vacBarrierInstalled ||
                       DoorThing.Map.designationManager.DesignationOn(
                           DoorThing, SDE_DefOf.SDE_InstallVacBarrier) == null;
            });

            if (NeedsPanels)
            {
                yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch)
                    .FailOnDespawnedNullOrForbidden(TargetIndex.B);
                yield return Toils_Haul.StartCarryThing(TargetIndex.B, putRemainderInQueue: false,
                    subtractNumTakenFromJobCount: false, failIfStackCountLessThanJobCount: true);
                yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

                yield return Toils_General.Do(delegate
                {
                    // Use materials upon delivery
                    pawn.carryTracker.CarriedThing?.Destroy();
                    CompVacBarrier.panelsDelivered = true;
                });
            }
            else
            {
                yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            }

            yield return WorkToil();
        }
        
        private Toil WorkToil()
        {
            var toil = ToilMaker.MakeToil("InstallVacBarrier");
            toil.tickAction = delegate
            {
                var comp = CompVacBarrier;
                var actor = toil.actor;
                comp.installWorkDone += actor.GetStatValue(StatDefOf.ConstructionSpeed) * 1.7f;
                actor.skills?.Learn(SkillDefOf.Construction, 0.05f);
                if (comp.installWorkDone < CompVacDoor.InstallWork) return;
                comp.CompleteInstall();
                ReadyForNextToil();
            };
            toil.defaultCompleteMode = ToilCompleteMode.Never;
            toil.activeSkill = () => SkillDefOf.Construction;

            return toil
                .WithProgressBar(TargetIndex.A, () => CompVacBarrier?.InstallProgress ?? 0f)
                .WithEffect(EffecterDefOf.ConstructMetal, TargetIndex.A)
                .FailOnDespawnedNullOrForbidden(TargetIndex.A)
                .FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
        }
    }
}
