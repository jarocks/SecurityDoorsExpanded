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
    
    public class DefModExtension_DoorArrows : DefModExtension
    {
        public Color off = Color.gray;
        public Color normal = Color.green;
        public Color checkpoint = Color.yellow;
        public Color lockdown = Color.red;
    }

    [StaticConstructorOnStartup]
    public class Building_VacDoor : Building_SupportedDoor
    {
        private bool tmpStuckOpen;
        
        [Unsaved] private Graphic upperMoverGraphicInt;
        [Unsaved] private CompVacCheckpoint cachedCheckpoint;
        [Unsaved] private CompVacDoor cachecVacBarrier;
        [Unsaved] private DefModExtension_DoorShutter cachedShutters;
        [Unsaved] private DefModExtension_DoorArrows cachedArrows;
        [Unsaved] private Graphic moverGraphicInt;
        [Unsaved] private Color moverGraphicColor;
        
        // 1 = shutter held open, 0 = vanilla door tracking
        [Unsaved] private float shutterOpenPct;
        
        public CompVacCheckpoint Checkpoint => cachedCheckpoint;

        public CompVacDoor VacBarrier => cachecVacBarrier;

        public DefModExtension_DoorShutter Shutter => cachedShutters;

        public DefModExtension_DoorArrows Arrows => cachedArrows;
        
        private Color ArrowColor
        {
            get
            {
                var arrows = Arrows;
                if (!lockedDown && Checkpoint?.Active != true)
                {
                    return DoorPowerOn ? arrows.normal : arrows.off;
                }

                return lockedDown ? arrows.lockdown : arrows.checkpoint;
            }
        }
        
        private Graphic MoverGraphic
        {
            get
            {
                if (Arrows == null) return Graphic;

                var color = ArrowColor;
                if (moverGraphicInt == null || color != moverGraphicColor)
                {
                    moverGraphicInt = Graphic.GetColoredVersion(Graphic.Shader, DrawColor,
                        moverGraphicColor = color);
                }
                return moverGraphicInt;
            }
        }

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
            cachedArrows = def.GetModExtension<DefModExtension_DoorArrows>();
        }

        public override bool ExchangeVacuum => VacBarrier?.VacBarrierActive != true && base.ExchangeVacuum;

        protected override float TempEqualizeRate => VacBarrier?.VacBarrierActive == true ? 0f : base.TempEqualizeRate;

        public override bool FreePassage => Checkpoint?.Active != true && base.FreePassage;
        
        public override bool PawnCanOpen(Pawn p) => !LockdownActive && !p.IsEntity &&  Checkpoint?.BlocksPawn(p) != true && base.PawnCanOpen(p);

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

        public bool LockdownActive => failSecure && !DoorPowerOn || compForbiddable?.Forbidden == true;

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

            if (!lockedDown || !Open) return;

            holdOpenInt = false;
            DoorTryClose();
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

        // TODO: Maybe add in status string for disallowed (although the 'X' icon kind of makes it self-explanatory)
        public override string GetInspectString()
        {
            string line = null;
            if (lockedDown && compForbiddable?.Forbidden != true)
            {
                var reason = (this.IsBrokenDown() ? "SDE_BrokenDown" : "SDE_NoPower").Translate();
                line = "SDE_Lockdown".Translate(reason).Colorize(ColorLibrary.RedReadable);
            }

            return new StringBuilder(base.GetInspectString()).AppendInNewLine(line).ToString();
        }
    }
}