using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace SecurityDoorsExpanded
{
    public static class VacResistanceCache
    {

        public struct Entry
        {
            public int tick;
            public bool value;
        }

        public static readonly ConcurrentDictionary<int, Entry> Cache = new ConcurrentDictionary<int, Entry>();

        public static void Clear() => Cache.Clear();

        public static void Cleanup()
        {
            var now = Find.TickManager.TicksGame;
            Stale.Clear();
            foreach (var pair in Cache.Where(pair => now - pair.Value.tick >= 2500))
            {
                Stale.Add(pair.Key);
            }

            foreach (var id in Stale)
            {
                Cache.TryRemove(id, out _);
            }
        }

        private static readonly List<int> Stale = new List<int>();
    }

    // Ensure cache is destroyed when game is unloaded
    public class VacResistanceCacheDestroyer : GameComponent
    {
        private const int CleanupInterval = 2500;

        public VacResistanceCacheDestroyer(Game game) => VacResistanceCache.Clear();

        public override void GameComponentTick()
        {
            if (Find.TickManager.TicksGame % CleanupInterval == 0)
            {
                VacResistanceCache.Cleanup();
            }
        }
    }
}
