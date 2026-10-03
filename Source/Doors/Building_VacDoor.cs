using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace SecurityDoorsExpanded
{
    public class DefModExtension_DoorShutter : DefModExtension
    {
        public int ticksToClose = 60;
    }

    [StaticConstructorOnStartup]
    public class Building_VacDoor : Building_SupportedDoor
    {
        private bool tmpStuckOpen;

        [Unsaved] private Graphic upperMoverGraphicInt;
        [Unsaved] private CompVacCheckpoint cachedCheckpoint;
        [Unsaved] private CompVacDoor cachecVacBarrier;
        [Unsaved] private DefModExtension_DoorShutter cachedShutters;
        [Unsaved] private Graphic moverGraphicInt;
        [Unsaved] private Color moverGraphicColor;

        // 1 = shutter held open, 0 = vanilla door tracking
        [Unsaved] private float shutterOpenPct;

        public CompVacCheckpoint Checkpoint => cachedCheckpoint;

        public CompVacDoor VacBarrier => cachecVacBarrier;

        public DefModExtension_DoorShutter Shutter => cachedShutters;

        public bool ClearanceRestricted => Compat_SecurityClearance.Restricted(this);

        private Color ArrowColor
        {
            get
            {
                if (lockedDown) return ArrowLockdown;
                if (Checkpoint?.Active == true || ClearanceRestricted) return ArrowConditional;

                return DoorPowerOn ? ArrowNormal : ArrowOff;
            }
        }

        private Graphic MoverGraphic
        {
            get
            {
                var color = ArrowColor;
                if (moverGraphicInt == null || color != moverGraphicColor)
                {
                    moverGraphicInt = Graphic.GetColoredVersion(Graphic.Shader, DrawColor, moverGraphicColor = color);
                }

                return moverGraphicInt;
            }
        }

        private static readonly Color ArrowOff = Color.gray;
        private static readonly Color ArrowNormal = Color.green;
        private static readonly Color ArrowConditional = Color.yellow;
        private static readonly Color ArrowLockdown = Color.red;

        private const float MoverOffsetStart = 0.25f;
        private const float MoverOffsetSpan = 0.35000002f;

        private static readonly float UpperMoverAltitude = AltitudeLayer.DoorMoveable.AltitudeFor() + 0.018292684f;

        private static readonly Vector3 MoverDrawScale = new Vector3(0.5f, 1f, 1f);

        protected override bool CanDrawMovers => false;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            shutterOpenPct = Shutter != null && DoorPowerOn ? 1f : 0f;
            lockedDown = LockdownActive;

            cachedCheckpoint = GetComp<CompVacCheckpoint>();
            cachecVacBarrier = GetComp<CompVacDoor>();
            cachedShutters = def.GetModExtension<DefModExtension_DoorShutter>();
        }

        public override bool ExchangeVacuum => VacBarrier?.VacBarrierActive != true && base.ExchangeVacuum;

        protected override float TempEqualizeRate => VacBarrier?.VacBarrierActive == true ? 0f : base.TempEqualizeRate;

        public override bool FreePassage => Checkpoint?.Active != true && base.FreePassage;

        public override bool PawnCanOpen(Pawn p) => !lockedDown && !p.IsEntity && base.PawnCanOpen(p);

        public override bool BlocksPawn(Pawn p) => Checkpoint?.BlocksPawn(p) == true || base.BlocksPawn(p);

        public bool IsOpening => OpenPct > 0f;

        private Graphic UpperMoverGraphic
        {
            get
            {
                if (upperMoverGraphicInt == null)
                {
                    var graphic = def.building.upperMoverGraphic?.Graphic;
                    upperMoverGraphicInt = graphic?.GetColoredVersion(graphic.Shader, DrawColor, Graphic.ColorTwo);
                }

                return upperMoverGraphicInt;
            }
        }

        public override void Notify_ColorChanged()
        {
            upperMoverGraphicInt = null;
            moverGraphicInt = null;
            base.Notify_ColorChanged();
        }


        private bool failSecure;

        public bool lockedDown;

        public bool LockdownActive => !DoorPowerOn && (failSecure || Compat_SecurityClearance.LockedOut(this)) ||
                                      compForbiddable?.Forbidden == true;

        private static readonly Texture2D LockedIcon = ContentFinder<Texture2D>.Get("UI/Commands/SDE_LockModeSecure");

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref failSecure, "failSecure");
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }

            if (Faction != Faction.OfPlayer)
            {
                yield break;
            }

            yield return new Command_Toggle
            {
                defaultLabel = "SDE_LockMode".Translate(),
                defaultDesc = "SDE_LockModeDesc".Translate(),
                icon = LockedIcon,
                isActive = () => failSecure,
                toggleAction = delegate
                {
                    failSecure = !failSecure;
                    MapHeld?.reachability.ClearCache();
                }
            };
        }

        protected virtual void Notify_LockdownBegan()
        {
            SDE_DefOf.SDE_Lockdown.PlayOneShot(new TargetInfo(Position, Map));
            holdOpenInt = false;

            if (Open)
            {
                DoorTryClose();
            }

            MapHeld?.reachability.ClearCache();
        }

        private void TickLockdown()
        {
            // TODO: Add debounceish code to prevent spamming during brown-outs
            var active = LockdownActive;
            if (active && !lockedDown)
            {
                Notify_LockdownBegan();
            }

            lockedDown = active;
        }

        private void TickShutter()
        {
            var shutter = Shutter;
            if (shutter == null) return;

            shutterOpenPct = Mathf.MoveTowards(shutterOpenPct, DoorPowerOn && !LockdownActive ? 1f : 0f,
                1f / Mathf.Max(shutter.ticksToClose, 1));
        }

        protected override void Tick()
        {
            base.Tick();

            TickLockdown();
            TickShutter();
            tmpStuckOpen = StuckOpen;
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            DoorPreDraw();
            if (!tmpStuckOpen)
            {
                var offsetDist = MoverOffsetStart + MoverOffsetSpan * OpenPct;
                DrawMovers(drawLoc, offsetDist, MoverGraphic, AltitudeLayer.DoorMoveable.AltitudeFor(),
                    MoverDrawScale, Graphic.ShadowGraphic);

                if (def.building.upperMoverGraphic != null)
                {
                    // Works well enough
                    var upperOpenPct = Mathf.Max(Mathf.Clamp01(OpenPct * 2.5f), shutterOpenPct);
                    var upperOffsetDist = MoverOffsetStart + MoverOffsetSpan * upperOpenPct;
                    DrawMovers(drawLoc, upperOffsetDist, UpperMoverGraphic, UpperMoverAltitude,
                        MoverDrawScale, null);
                }
            }

            base.DrawAt(drawLoc, flip);
        }

        protected override void DoorOpen(int ticksToClose = 110)
        {
            if (!lockedDown)
            {
                base.DoorOpen(ticksToClose);
            }
        }

        public override string GetInspectString()
        {
            string line = null;
            if (lockedDown)
            {
                var reason = "SDE_Locked";

                if (Compat_SecurityClearance.LockedOut(this))
                {
                    reason = "SDE_Lockout";
                }
                else if (this.IsBrokenDown())
                {
                    reason = "SDE_BrokenDown";
                }
                else if (compForbiddable?.Forbidden == true)
                {
                    reason = "SDE_Forbidden";
                }
                else if (!DoorPowerOn)
                {
                    reason = "SDE_PoweredOff";
                }
                
                line = "SDE_Lockdown".Translate(reason.Translate()).Colorize(ColorLibrary.RedReadable);
            }

            return new StringBuilder(base.GetInspectString()).AppendInNewLine(line).ToString();
        }
    }
}