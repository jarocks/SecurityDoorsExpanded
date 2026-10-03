using Verse;

namespace SecurityDoorsExpanded
{
    public class CompProperties_DoorBanners : CompProperties
    {
        public GraphicData lockdown;
        public GraphicData checkpoint;
        public GraphicData conditional;

        public CompProperties_DoorBanners()
        {
            compClass = typeof(CompDoorBanners);
        }
    }

    public sealed class CompDoorBanners : ThingComp
    {
        public CompProperties_DoorBanners Props => (CompProperties_DoorBanners)props;

        private static readonly float BannerAltitude = AltitudeLayer.DoorMoveable.AltitudeFor(1f);

        public override void PostDraw()
        {
            base.PostDraw();

            if (!SecurityDoorsExpandedMod.Settings.showStatus || !(parent is Building_VacDoor door) ||
                door.IsOpening) return;

            GraphicData banner;
            var rotation = door.Rotation;

            if (door.lockedDown)
            {
                banner = Props.lockdown;
            }
            else if (door.Checkpoint?.Active == true && door.Checkpoint.Rotation != Rot4.North)
            {
                banner = Props.checkpoint;
                rotation = door.Checkpoint.Rotation;
            } else if (door.ClearanceRestricted)
            {
                banner = Props.conditional;
            }
            else
            {
                return;
            }

            var graphic = banner?.Graphic;
            if (graphic == null) return;

            var drawLoc = door.DrawPos;
            drawLoc.y = BannerAltitude;
            graphic.Draw(drawLoc, rotation, door);
        }
    }
}