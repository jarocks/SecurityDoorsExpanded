using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace SecurityDoorsExpanded
{
    public class JobDriver_OperateVehicleDoor : JobDriver
    {
        private const int WorkTicks = 15;

        private Building_VehicleDoor Door => job.GetTarget(TargetIndex.A).Thing as Building_VehicleDoor;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A)
                .FailOn(() => Door?.ManualOrderPending != true);

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            yield return Toils_General.Wait(WorkTicks, TargetIndex.A)
                .FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);

            var finalize = ToilMaker.MakeToil("OperateVehicleDoor");
            finalize.initAction = delegate
            {
                Door?.Notify_ManualOrderComplete();
            };
            finalize.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return finalize;
        }
    }
}
