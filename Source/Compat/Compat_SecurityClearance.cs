using System.Runtime.CompilerServices;
using RimWorld;
using SecurityClearance;
using Verse;

namespace SecurityDoorsExpanded
{
    public static class Compat_SecurityClearance
    {
        private static readonly bool Active = ModLister.AnyModActiveNoSuffix(new[] { "7f.SecurityClearance" });

        public static bool LockedOut(Building_Door door) => Active && LockedOutInt(door);

        public static bool Restricted(Building_Door door) => Active && RestrictedInt(door);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool LockedOutInt(Building_Door door) => 
            !GameComponent_SecurityClearance.TryGetClearance(door, out _);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool RestrictedInt(Building_Door door) =>
            !GameComponent_SecurityClearance.TryGetClearance(door, out var level) || level != ClearanceLevel.None;
    }
}
