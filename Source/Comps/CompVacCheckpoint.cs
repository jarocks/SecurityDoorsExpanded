using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace SecurityDoorsExpanded
{
    public class CompProperties_VacCheckpoint : CompProperties
    {
        public CompProperties_VacCheckpoint()
        {
            compClass = typeof(CompVacCheckpoint);
        }
    }

    [StaticConstructorOnStartup]
    public sealed class CompVacCheckpoint : ThingComp
    {
        public CompProperties_VacCheckpoint Props => (CompProperties_VacCheckpoint)props;

        private bool checkpointEnabled;

        [Unsaved(false)] private bool inAtmosphere;

        [Unsaved(false)] public bool frontActive;

        [Unsaved(false)] public bool Active;

        private static readonly Texture2D VacRestrictIcon = ContentFinder<Texture2D>.Get("UI/Commands/SDE_Checkpoint");

        private void RefreshVacuum()
        {
            var map = parent.Map;
            if (!checkpointEnabled || inAtmosphere || map == null)
            {
                Active = false;
                return;
            }

            // Maybe the front direction should be stored instead?
            var axis = parent.Rotation.FacingCell;
            frontActive = IsOxygenated(parent.Position + axis, map);
            Active = frontActive != IsOxygenated(parent.Position - axis, map);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref checkpointEnabled, "checkpointEnabled");
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            inAtmosphere = parent.Map?.Biome.inVacuum != true;
        }

        public override void CompTick()
        {
            base.CompTick();

            // Ensure room lookups don't all happen at once
            if (parent.IsHashIntervalTick(30))
            {
                RefreshVacuum();
            }
        }

        // Not really an override, but I like the semantics
        public bool BlocksPawn(Pawn p)
        {
            if (!Active || (SecurityDoorsExpandedMod.Settings.draftedBypassCheckpoint && p.Drafted))
            {
                return false;
            }

            var hasEntry = VacResistanceCache.Cache.TryGetValue(p.thingIDNumber, out var entry);

            if (!UnityData.IsInMainThread)
            {
                return hasEntry && entry.value;
            }

            var now = Find.TickManager.TicksGame;
            if (hasEntry && now - entry.tick < 60)
            {
                return entry.value;
            }

            var restricted = p.HarmedByVacuum && !p.HostileTo(parent) &&
                             p.GetStatValue(DefRefs.VacuumResistance, applyPostProcess: true, cacheStaleAfterTicks: 60) <
                             SecurityDoorsExpandedMod.Settings.vacThreshold && IsOxygenated(p.Position, p.Map);

            VacResistanceCache.Cache[p.thingIDNumber] = new VacResistanceCache.Entry { tick = now, value = restricted };
            return restricted;
        }
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (parent.Faction != Faction.OfPlayer)
            {
                yield break;
            }

            yield return new Command_Toggle
            {
                defaultLabel = "SDE_VacCheckpoint".Translate(),
                defaultDesc = "SDE_VacCheckpointDesc".Translate(
                    SecurityDoorsExpandedMod.Settings.vacThreshold.ToStringPercent(),
                    SecurityDoorsExpandedMod.Settings.draftedBypassCheckpoint
                        ? "SDE_BypassDrafted".Translate()
                        : (TaggedString)""),
                icon = VacRestrictIcon,
                isActive = () => checkpointEnabled,
                toggleAction = delegate
                {
                    checkpointEnabled = !checkpointEnabled;
                    RefreshVacuum();
                    parent.MapHeld?.reachability.ClearCache();
                }
            };
        }

        public override string CompInspectStringExtra()
        {
            if (!checkpointEnabled)
            {
                return null;
            }

            return Active
                ? "SDE_CheckpointActive".Translate(SecurityDoorsExpandedMod.Settings.vacThreshold.ToStringPercent())
                : "SDE_CheckpointInactive".Translate();
        }

        
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private bool IsOxygenated(IntVec3 cell, Map map)
        {
            var room = map.regionGrid.GetValidRegionAt_NoRebuild(cell)?.District?.Room;
            return room?.Vacuum < VacuumUtility.MinVacuumForDamage;
        }
    }
}