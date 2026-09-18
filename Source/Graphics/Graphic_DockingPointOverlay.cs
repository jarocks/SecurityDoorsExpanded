using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace SecurityDoorsExpanded
{
    public static class Graphic_DockingPointOverlay
    {
        private static readonly List<IntVec3> Cells = new List<IntVec3>();

        private static readonly Color DockColor = ColorLibrary.Cyan;

        public static void DrawFor(Gravship gravship, IntVec3 root)
        {
            if (gravship == null) return;

            Cells.Clear();
            Cells.AddRange(gravship.ExteriorDoorPlacements
                .Where(p => p.Key.TryGetComp<CompDockingPoint>()?.isDockingPoint == true)
                .SelectMany(p => GenAdj.OccupiedRect(root + p.Value.local, p.Value.rotation, p.Key.def.Size)));

            if (Cells.Count > 0)
            {
                GenDraw.DrawFieldEdges(Cells, DockColor);
            }
        }
    }
}