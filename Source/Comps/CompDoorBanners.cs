using Verse;

namespace SecurityDoorsExpanded
{
    public class CompProperties_DoorBanners : CompProperties
    {
        public GraphicData lockdown;
        public GraphicData checkpoint;

        public CompProperties_DoorBanners()
        {
            compClass = typeof(CompDoorBanners);
        }
    }

    public sealed class CompDoorBanners : ThingComp
    {
        public CompProperties_DoorBanners Props => (CompProperties_DoorBanners) props;

        private static readonly float BannerAltitude = AltitudeLayer.DoorMoveable.AltitudeFor(1f);

        public override void PostDraw()
        {
            base.PostDraw();

            if (!SecurityDoorsExpandedMod.Settings.showStatus || !(parent is Building_VacDoor door) ||
                door.IsOpening) return;

            var lockdown = door.lockedDown;
            Rot4 side;

            if (lockdown)
            {
                side = door.Rotation;
            }
            else
            {
                var checkpoint = door.Checkpoint;
                if (checkpoint?.Active != true) return;

                side = checkpoint.frontActive ? door.Rotation : door.Rotation.Opposite;
                if (side == Rot4.North) return;
            }

            var graphic = (lockdown ? Props.lockdown : Props.checkpoint)?.Graphic;
            if (graphic == null) return;

            var drawLoc = door.DrawPos;
            drawLoc.y = BannerAltitude;
            graphic.Draw(drawLoc, side, door);
        }
    }
}
