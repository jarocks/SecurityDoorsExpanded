using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace SecurityDoorsExpanded
{
    public class DefModExtension_VehicleDoor : DefModExtension
    {
        public ThingDef blockerDef;
    }

    [StaticConstructorOnStartup]
    public class Building_VehicleDoor : Building_VacDoor
    {
        [Unsaved] private Thing blockerInt;
        [Unsaved] private bool blockerDefResolved;
        [Unsaved] private ThingDef blockerDefInt;

        private ThingDef BlockerDef
        {
            get
            {
                if (blockerDefResolved) return blockerDefInt;

                blockerDefResolved = true;
                return blockerDefInt = def.GetModExtension<DefModExtension_VehicleDoor>()?.blockerDef;
            }
        }

        private static readonly Texture2D OpenIcon = TexCommand.HoldOpen;

        private static readonly Texture2D CloseIcon = ContentFinder<Texture2D>.Get("UI/Commands/SDE_Close");

        private static readonly Texture2D CancelIcon = ContentFinder<Texture2D>.Get("UI/Designators/Cancel");

        private bool Moving => Open ? TicksTillFullyOpened > 0 : OpenPct > 0f;

        public override bool PawnCanOpen(Pawn p) => false;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (StuckOpen)
            {
                holdOpenInt = true;
            }

            blockerInt = FindBlocker();
            if (respawningAfterLoad)
            {
                LongEventHandler.ExecuteWhenFinished(delegate
                {
                    if (Spawned)
                    {
                        UpdateBlocker();
                    }
                });
                return;
            }

            UpdateBlocker();
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            DespawnBlocker();
            base.DeSpawn(mode);
        }

        protected override void Notify_LockdownBegan()
        {
            base.Notify_LockdownBegan();
            holdOpenInt = false;
        }

        protected override void Tick()
        {
            base.Tick();
            if (!lockedDown && holdOpenInt != Open)
            {
                if (holdOpenInt)
                {
                    DoorOpen();
                }
                else
                {
                    DoorTryClose();
                }
            }

            UpdateBlocker();
        }

        private void UpdateBlocker()
        {
            var shouldBlock = !(holdOpenInt && Open && TicksTillFullyOpened <= 0);
            var current = blockerInt != null && blockerInt.Spawned && !blockerInt.Destroyed;
            if (shouldBlock == current) return;

            if (shouldBlock)
            {
                SpawnBlocker();
            }
            else
            {
                DespawnBlocker();
            }
        }

        private Thing FindBlocker()
        {
            if (BlockerDef == null || !Spawned)
            {
                return null;
            }
            return Position.GetThingList(Map).FirstOrDefault(t => t.def == BlockerDef);
        }

        private void SpawnBlocker()
        {
            if (BlockerDef == null || !Spawned) return;

            // Rescan to avoid spawning duplicate blockers
            blockerInt = FindBlocker();
            if (blockerInt != null && blockerInt.Spawned) return;
            
            blockerInt = GenSpawn.Spawn(ThingMaker.MakeThing(BlockerDef), Position, Map, Rotation);
        }

        private void DespawnBlocker()
        {
            var blocker = blockerInt ?? FindBlocker();
            blockerInt = null;
            if (blocker != null && blocker.Spawned && !blocker.Destroyed)
            {
                blocker.Destroy();
            }
        }

        private Designation ManualOrder =>
            Map.designationManager.DesignationOn(this, SDE_DefOf.SDE_OpenVehicleDoor)
            ?? Map.designationManager.DesignationOn(this, SDE_DefOf.SDE_CloseVehicleDoor);

        public bool ManualOrderPending => ManualOrder != null;

        private void OrderManualOperation()
        {
            if (ManualOrderPending) return;
            Map.designationManager.AddDesignation(new Designation(this,
                holdOpenInt ? SDE_DefOf.SDE_CloseVehicleDoor : SDE_DefOf.SDE_OpenVehicleDoor));
        }

        public void CancelManualOrder()
        {
            // If door has conflicting orders, cancel both
            Map.designationManager.DesignationOn(this, SDE_DefOf.SDE_OpenVehicleDoor)?.Delete();
            Map.designationManager.DesignationOn(this, SDE_DefOf.SDE_CloseVehicleDoor)?.Delete();
        }

        public void Notify_ManualOrderComplete()
        {
            var order = ManualOrder;
            if (order == null) return;
            holdOpenInt = order.def == SDE_DefOf.SDE_OpenVehicleDoor;
            CancelManualOrder();
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var gizmo in base.GetGizmos())
            {
                if (gizmo is Command_Toggle toggle && toggle.icon == TexCommand.HoldOpen)
                {
                    continue;
                }
                
                yield return gizmo;
            }

            if (Faction != Faction.OfPlayer || ManualOrderPending)
            {
                yield break;
            }

            var pending = Moving || holdOpenInt != Open;
            var command = new Command_Action
            {
                defaultLabel = (pending
                    ? "SDE_VehicleDoorCancel"
                    : (holdOpenInt ? "SDE_VehicleDoorClose" : "SDE_VehicleDoorOpen")).Translate(),
                icon = pending ? CancelIcon : (holdOpenInt ? CloseIcon : OpenIcon),
                hotKey = KeyBindingDefOf.Misc3,
                action = delegate
                {
                    if (!DoorPowerOn)
                    {
                        OrderManualOperation();
                    }
                    else
                    {
                        holdOpenInt = !holdOpenInt;
                    }
                }
            };

            if (StuckOpen || this.IsBrokenDown())
            {
                command.Disable();
            }
            yield return command;
        }


        public override string GetInspectString()
        {
            string state = null;
            if (Moving)
            {
                state = (holdOpenInt ? "SDE_VehicleDoorStateOpening" : "SDE_VehicleDoorStateClosing").Translate();
            }
            else if (!holdOpenInt && Open)
            {
                state = "SDE_VehicleDoorStateBlocked".Translate();
            }

            return new StringBuilder(base.GetInspectString()).AppendInNewLine(state).ToString();
        }
    }
}