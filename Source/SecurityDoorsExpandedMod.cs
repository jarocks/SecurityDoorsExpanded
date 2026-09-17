using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace SecurityDoorsExpanded
{
    [StaticConstructorOnStartup]
    public static class SecurityDoorsExpandedStartup
    {
        static SecurityDoorsExpandedStartup()
        {
            new Harmony("jarocks.securityDoorsExpanded").PatchAll();
        }
    }

    public class SecurityDoorsExpandedSettings : ModSettings
    {
        // severity = 0.02 * vacuum * (1 - resistance) / 60 ticks
        public const float DefaultThreshold = 0.85f;
        
        public const float MinThreshold = 0.75f;

        public float vacThreshold = DefaultThreshold;
        
        public bool draftedBypassCheckpoint;
        
        public bool showStatus = true;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref vacThreshold, "vacThreshold", DefaultThreshold);
            Scribe_Values.Look(ref draftedBypassCheckpoint, "draftedBypassCheckpoint");
            Scribe_Values.Look(ref showStatus, "showStatus", true);
        }
    }

    public class SecurityDoorsExpandedMod : Mod
    {
        public static SecurityDoorsExpandedSettings Settings;

        public SecurityDoorsExpandedMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<SecurityDoorsExpandedSettings>();
        }

        public override string SettingsCategory() => "Security Doors Expanded";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var listing = new Listing_Standard();
            listing.Begin(inRect);
            
            listing.CheckboxLabeled(
                "SDE_SettingStatus".Translate(),
                ref Settings.showStatus,
                "SDE_SettingStatusDesc".Translate());

            if (ModsConfig.OdysseyActive)
            {
                listing.Gap();

                listing.CheckboxLabeled(
                    "SDE_SettingDraftedBypass".Translate(),
                    ref Settings.draftedBypassCheckpoint,
                    "SDE_SettingDraftedBypassDesc".Translate());
                listing.Gap();

                listing.Label("SDE_SettingVacThreshold".Translate(
                    Settings.vacThreshold.ToStringPercent()),
                    tooltip: VacuumEstimate.Describe(Settings.vacThreshold));
                Settings.vacThreshold =
                    Mathf.Round(listing.Slider(Settings.vacThreshold,
                        SecurityDoorsExpandedSettings.MinThreshold, 1f) * 20f) / 20f;
            }

            listing.End();
        }
    }
}
