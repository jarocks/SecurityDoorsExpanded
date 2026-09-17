using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace SecurityDoorsExpanded
{
    public class CompProperties_DockingPoint : CompProperties
    {
        public CompProperties_DockingPoint()
        {
            compClass = typeof(CompDockingPoint);
        }
    }
    
    [StaticConstructorOnStartup]
    public sealed class CompDockingPoint : ThingComp
    {
        public bool isDockingPoint;

        // Todo: needs its own icon
        private static readonly Texture2D DockIcon = TexCommand.Install;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref isDockingPoint, "isDockingPoint");
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (parent.Faction != Faction.OfPlayer)
            {
                yield break;
            }

            yield return new Command_Toggle
            {
                defaultLabel = "SDE_DockingPoint".Translate(),
                defaultDesc = "SDE_DockingPointDesc".Translate(),
                icon = DockIcon,
                isActive = () => isDockingPoint,
                toggleAction = delegate { isDockingPoint = !isDockingPoint; }
            };
        }
    }
}
