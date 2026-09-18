using UnityEngine;
using Verse;

namespace SecurityDoorsExpanded
{
    public class Graphic_LinkedSupport : Graphic_Linked
    {
        public class RotationAtlas : Graphic_Multi
        {
            public override Material MatSingleFor(Thing thing) => MatAt(thing.Rotation, thing);
        }

        public override void Init(GraphicRequest req)
        {
            data = req.graphicData;
            path = req.path;
            maskPath = req.maskPath;
            color = req.color;
            colorTwo = req.colorTwo;
            drawSize = req.drawSize;
            subGraphic = GraphicDatabase.Get(typeof(RotationAtlas), req.path, req.shader, req.drawSize, req.color,
                req.colorTwo, req.graphicData, req.shaderParameters, req.maskPath);
        }

        public override Graphic GetColoredVersion(Shader newShader, Color newColor, Color newColorTwo)
        {
            return GraphicDatabase.Get<Graphic_LinkedSupport>(path, newShader, drawSize, newColor, newColorTwo, data);
        }

        public override bool ShouldLinkWith(IntVec3 c, Thing parent)
        {
            var rect = parent.OccupiedRect();
            if (rect.Contains(c)) return true;
            
            if (parent.Rotation.IsHorizontal ? c.x == rect.minX : c.z == rect.minZ) return false;

            var map = parent.Map;
            if (!c.InBounds(map)) return (data.linkFlags & LinkFlags.MapEdge) != 0;

            if ((map.terrainGrid.FoundationAt(c)?.IsSubstructure ?? false) !=
                (map.terrainGrid.FoundationAt(parent.Position)?.IsSubstructure ?? false))
            {
                return false;
            }

            return (map.linkGrid.LinkFlagsAt(c) & data.linkFlags) != 0 ||
                   (data.asymmetricLink?.linkToDoors == true && c.GetDoor(map) != null);
        }

        public override void DrawWorker(Vector3 loc, Rot4 rot, ThingDef thingDef, Thing thing, float extraRotation)
        {
            if (thing?.Spawned != true) return;

            var rect = thing.OccupiedRect();
            var offset = (loc - thing.DrawPos).WithY(loc.y);

            if (rot.IsHorizontal)
            {
                DrawSupport(new IntVec3(rect.minX, 0, rect.maxZ + 1));
                DrawSupport(new IntVec3(rect.minX, 0, rect.minZ - 1));
            }
            else
            {
                DrawSupport(new IntVec3(rect.minX - 1, 0, rect.minZ));
                DrawSupport(new IntVec3(rect.maxX + 1, 0, rect.minZ));
            }

            return;

            void DrawSupport(IntVec3 cell)
            {
                Graphics.DrawMesh(MeshPool.GridPlane(drawSize), cell.ToVector3Shifted() + offset, Quaternion.identity,
                    LinkedDrawMatFrom(thing, cell), 0);
            }
        }

        public override string ToString() => $"LinkedSupport(path={path}, color={color}, colorTwo={colorTwo})";
    }
}
