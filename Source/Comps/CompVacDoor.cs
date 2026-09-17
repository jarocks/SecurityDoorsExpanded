using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace SecurityDoorsExpanded
{
    public class CompProperties_VacDoor : CompProperties
    {
        // Base values
        public int panelCost = 3;
        public float barrierPowerDraw = 75f;
        
        public GraphicData barrierGraphicData;

        public CompProperties_VacDoor()
        {
            compClass = typeof(CompVacDoor);
        }
    }
    
    [StaticConstructorOnStartup]
    public sealed class CompVacDoor : ThingComp
    {
        public CompProperties_VacDoor Props => (CompProperties_VacDoor)props;

        public bool vacBarrierInstalled;
        public bool panelsDelivered;
        public float installWorkDone;

        public const float InstallWork = 900f;

        private CompPowerTrader powerComp;

        public bool PowerOn
        {
            get
            {
                var power = powerComp ?? (powerComp = parent.GetComp<CompPowerTrader>());
                return power?.PowerOn == true;
            }
        }

        public bool VacBarrierActive => vacBarrierInstalled && PowerOn;

        private Graphic BarrierGraphic => Props.barrierGraphicData?.Graphic;

        private bool InstallDesignated => parent.MapHeld?.designationManager.DesignationOn(parent, SDE_DefOf.SDE_InstallVacBarrier) != null;

        private bool InstallInProgress => !vacBarrierInstalled && (panelsDelivered || installWorkDone > 0f);
        
        private bool CanInstallVacBarrier => !vacBarrierInstalled && (DefRefs.VacBarrierTechUnlocked || DebugSettings.godMode);

        public float InstallProgress => Mathf.Clamp01(installWorkDone / InstallWork);

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref vacBarrierInstalled, "vacBarrierInstalled");
            Scribe_Values.Look(ref panelsDelivered, "panelsDelivered");
            Scribe_Values.Look(ref installWorkDone, "installWorkDone");
        }

        public override void CompTick()
        {
            base.CompTick();
            if (parent.IsHashIntervalTick(250))
            {
                UpdatePowerDraw();
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            UpdatePowerDraw();
        }

        private void UpdatePowerDraw()
        {
            var power = powerComp ?? (powerComp = parent.GetComp<CompPowerTrader>());
            if (power == null) return;

            var draw = power.Props.PowerConsumption + (vacBarrierInstalled ? Props.barrierPowerDraw : 0f);
            power.PowerOutput = 0f - draw;
        }

        public override void ReceiveCompSignal(string signal)
        {
            base.ReceiveCompSignal(signal);
            if (signal != CompPowerTrader.PowerTurnedOnSignal && signal != CompPowerTrader.PowerTurnedOffSignal) return;

            // Used by vac barrier and fail-secure lockout
            DirtyVacuum();
            ClearReachability();
        }

        private void RemoveDesignation()
        {
            var designation = parent.MapHeld?.designationManager.DesignationOn(parent, SDE_DefOf.SDE_InstallVacBarrier);
            if (designation != null)
            {
                parent.MapHeld.designationManager.RemoveDesignation(designation);
            }
        }

        public void CancelInstall()
        {
            RemoveDesignation();
            if (panelsDelivered && parent.MapHeld != null)
            {
                var refund = ThingMaker.MakeThing(DefRefs.GravlitePanel);
                refund.stackCount = Props.panelCost;
                GenPlace.TryPlaceThing(refund, parent.Position, parent.MapHeld, ThingPlaceMode.Near);
            }

            panelsDelivered = false;
            installWorkDone = 0f;
            UpdatePowerDraw();
        }

        public void CompleteInstall()
        {
            vacBarrierInstalled = true;
            panelsDelivered = false;
            installWorkDone = 0f;
            RemoveDesignation();
            UpdatePowerDraw();
            DirtyVacuum();
            Messages.Message("SDE_VacBarrierInstalled".Translate(parent.LabelShort),
                parent, MessageTypeDefOf.PositiveEvent);
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
            if (previousMap == null || (!vacBarrierInstalled && !panelsDelivered)) return;
            var effectiveMode = !vacBarrierInstalled && mode == DestroyMode.Deconstruct ? DestroyMode.Cancel : mode;

            var count = PanelsLeftBy(parent, effectiveMode, Props.panelCost);
            
            while (count > 0)
            {
                var panels = ThingMaker.MakeThing(DefRefs.GravlitePanel);
                panels.stackCount = Mathf.Min(count, DefRefs.GravlitePanel.stackLimit);
                count -= panels.stackCount;
                GenPlace.TryPlaceThing(panels, parent.Position, previousMap, ThingPlaceMode.Near);
            }
        }

        private static int PanelsLeftBy(Thing thing, DestroyMode mode, int count)
        {
            if (count <= 0 || !GenLeaving.CanBuildingLeaveResources(thing, mode))
            {
                return 0;
            }

            switch (mode)
            {
                case DestroyMode.KillFinalize:
                    return GenMath.RoundRandom(count * 0.25f);
                case DestroyMode.Deconstruct:
                    return Mathf.Min(
                        GenMath.RoundRandom(count * thing.def.resourcesFractionWhenDeconstructed), count);
                case DestroyMode.FailConstruction:
                    return Mathf.Max(GenMath.RoundRandom(count * 0.5f), 1);
                case DestroyMode.Cancel:
                case DestroyMode.Refund:
                    return count;
                default:
                    return 0;
            }
        }

        private void DirtyVacuum()
        {
            if (vacBarrierInstalled)
            {
                parent.MapHeld?.GetComponent<VacuumComponent>()?.Dirty();
            }
        }

        private void ClearReachability()
        {
            parent.MapHeld?.reachability.ClearCache();
        }
        
        private static readonly float BarrierAltitude = AltitudeLayer.DoorMoveable.AltitudeFor(-1f);

        public override void PostDraw()
        {
            base.PostDraw();
            var rot = parent.Rotation;
            var barrier = BarrierGraphic;

            if (!VacBarrierActive || rot.IsHorizontal || barrier == null) return;

            var building = parent.def.building;
            var drawLoc = parent.DrawPos + building.doorTopHorizontalOffset + building.doorSupportGraphicOffset;
            drawLoc.y = BarrierAltitude;
            barrier.Draw(drawLoc, rot, parent);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (parent.Faction != Faction.OfPlayer || !CanInstallVacBarrier || InstallDesignated)
            {
                yield break;
            }

            yield return new Command_Action
            {
                defaultLabel = "SDE_InstallVacBarrier".Translate(),
                defaultDesc = "SDE_InstallVacBarrierDesc".Translate(Props.panelCost),
                icon = ContentFinder<Texture2D>.Get("UI/Commands/SDE_InstallVacBarrier"),
                action = delegate
                {
                    if (DebugSettings.godMode)
                    {
                        CompleteInstall();
                    }
                    else
                    {
                        parent.MapHeld.designationManager.AddDesignation(
                            new Designation(parent, SDE_DefOf.SDE_InstallVacBarrier));
                    }
                }
            };
        }

        public override string CompInspectStringExtra()
        {
            var sb = new StringBuilder();
            if (vacBarrierInstalled && PowerOn)
            {
                sb.AppendInNewLine("SDE_VacBarrierActive".Translate().Colorize(ColorLibrary.Green));
            }

            if (!vacBarrierInstalled && (InstallDesignated || InstallInProgress))
            {
                sb.AppendInNewLine(panelsDelivered
                    ? "SDE_Installing".Translate(InstallProgress.ToStringPercent())
                    : "SDE_InstallNeeds".Translate(
                        Props.panelCost, DefRefs.GravlitePanel.label));
            }

            return sb.Length > 0 ? sb.ToString() : null;
        }
    }
}