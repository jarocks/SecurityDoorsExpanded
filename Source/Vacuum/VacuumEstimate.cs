using RimWorld;
using UnityEngine;
using Verse;

namespace SecurityDoorsExpanded
{
    public static class VacuumEstimate
    {
        private const float SafeSeverity = 0.15f;
        private const float DeathSeverity = 1f;

        private static string TimeTo(float severity, float resistance)
        {
            var remaining = Mathf.Max(1f - resistance, 0f);
            var ticks = Mathf.RoundToInt(severity / (0.02f * remaining) * 60);
            return ticks.ToStringTicksToPeriod(allowSeconds: false);
        }

        public static string Describe(float resistance)
        {
            if (resistance >= 1f)
            {
                return "SDE_SettingVacResistant".Translate();
            }
            return "SDE_SettingVacEstimate".Translate(
                TimeTo(SafeSeverity, resistance),
                TimeTo(DeathSeverity, resistance));
        }
    }
}
