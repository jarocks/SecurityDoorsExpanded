using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace SecurityDoorsExpanded
{
    public static class DockingPointOverlay
    {
        private static readonly List<IntVec3> Cells = new List<IntVec3>();

        private static readonly Color DockColor = ColorLibrary.Cyan;
        
        public static void DrawFor(Gravship gravship, IntVec3 root)
        {
            if (gravship == null) return;

            Cells.Clear();
            foreach (var pair in gravship.ExteriorDoorPlacements)
            {
                if (pair.Key.TryGetComp<CompDockingPoint>()?.isDockingPoint != true)
                {
                    continue;
                }

                var data = pair.Value;
                foreach (var cell in GenAdj.OccupiedRect(root + data.local, data.rotation, pair.Key.def.Size))
                {
                    Cells.Add(cell);
                }
            }

            if (Cells.Count > 0)
            {
                GenDraw.DrawFieldEdges(Cells, DockColor);
            }
        }
    }
}
