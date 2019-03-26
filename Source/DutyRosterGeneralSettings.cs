namespace DutyRoster
{
    class DutyRosterGeneralSettings : GameParameters.CustomParameterNode
    {
        public override string Title => "General Settings";
        public override GameParameters.GameMode GameMode => GameParameters.GameMode.ANY;
        public override bool HasPresets => true;
        public override string Section => "Duty Roster";
        public override string DisplaySection => Section;
        public override int SectionOrder => 1;

        public override void SetDifficultyPreset(GameParameters.Preset preset)
        {
            switch (preset)
            {
                case GameParameters.Preset.Easy:
                    OnDutyHours = 4;
                    OnDutyMinutes = 0;
                    DutyMinutesPerLevel = 20;
                    //Min 4hs        onDuty (4hr + 0x 20mins) for 0 Star Kerbal
                    //Max 5hr 40mins onDuty (4hr + 5x 20mins) for 5 Star Kerbal

                    DutyChangeMinutesPerShift = 60;


                    break;
                case GameParameters.Preset.Normal:
                    OnDutyHours = 3;
                    OnDutyMinutes = 20;
                    DutyMinutesPerLevel = 20;
                    //Min 3hs 20min onDuty (3hr 20min + 0x 20mins) for 0 Star Kerbal
                    //Max 5hr       onDuty (3hr 20min + 5x 20mins) for 5 Star Kerbal
                    DutyChangeMinutesPerShift = 30;

                    break;
                case GameParameters.Preset.Moderate:
                    OnDutyHours = 3;
                    OnDutyMinutes = 30;
                    DutyMinutesPerLevel = 12;
                    //Min 3hs 30min onDuty (3hr 30min + 0x 12mins) for 0 Star Kerbal
                    //Max 4hr 30min onDuty (3hr 30min + 5x 12mins) for 5 Star Kerbal
                    DutyChangeMinutesPerShift = 20;
                    break;
                case GameParameters.Preset.Hard:
                    //Max 3hr 30mins onDuty
                    OnDutyHours = 3;
                    OnDutyMinutes = 0;
                    DutyMinutesPerLevel = 12;
                    //Min 3hs onDuty (3hr + 0x 12mins) for 0 Star Kerbal
                    //Max 4hr onDuty (3hr + 5x 12mins) for 5 Star Kerbal
                    DutyChangeMinutesPerShift = 10;

                    break;
            }
        }

        [GameParameters.CustomParameterUI("Mod Enabled", toolTip = "Turn Duty Roster mechanics on/off")]
        public bool modEnabled = true;

        [GameParameters.CustomParameterUI("Use Blizzy's Toolbar", toolTip = "Use Blizzy's Toolbar mod (is installed) instead of stock app launcher. May need a scene change")]
        public bool UseBlizzysToolbar = true;

        [GameParameters.CustomIntParameterUI("Sort Kerbals by Duty Start Time", toolTip = "Kerbals in Duty Roster will be displayed depending on their Start Time, otherwise sort by name")]
        public bool SortByStartTimes = true;

        [GameParameters.CustomIntParameterUI("Lines per Page in Duty Roster", toolTip = "How many kerbals to show on one page of Duty Roster", minValue = 5, maxValue = 20, stepSize = 5)]
        public int LinesPerPage = 10;

        [GameParameters.CustomFloatParameterUI("Update Interval", toolTip = "Number of GAME seconds between Duty shift updates\n Increase if performance too slow", minValue = 0.04f, maxValue = 60)]
        public float UpdateInterval = 1f;

        [GameParameters.CustomFloatParameterUI("Minimum Update Interval", toolTip = "Minimum number of REAL seconds between updated on high time warp\nMust be <= Update Interval", minValue = 0.04f, maxValue = 60)]
        public float MinUpdateInterval = 1;


        //        [GameParameters.CustomParameterUI("Duty Change Alert", toolTip = "Display onscreen alert when Kerbal goes on/off Duty")]
        //        public bool ShiftChangeAlert = false;

        [GameParameters.CustomIntParameterUI("On Duty Hours", toolTip = "Hours On Duty for 0-star kerbals", minValue = 0, maxValue = 5, stepSize = 1)]
        public int OnDutyHours = 3;

        [GameParameters.CustomIntParameterUI("On Duty Minutes", toolTip = "Minutes On Duty (in addition of hours) for 0-star kerbals", minValue = 0, maxValue = 60, stepSize = 1)]
        public int OnDutyMinutes = 20;

        [GameParameters.CustomIntParameterUI("Duty Minutes per Level", toolTip = "Additional Minutes On Duty for EACH level gained.", minValue = 0, maxValue = 60, stepSize = 10)]
        public int DutyMinutesPerLevel = 20;

        [GameParameters.CustomIntParameterUI("Duty Change Minutes", toolTip = "How much a Kerbal can change their Duty Start time per Shift", minValue = 1, maxValue = 60, stepSize = 5)]
        public int DutyChangeMinutesPerShift = 30;

        [GameParameters.CustomParameterUI("Debug Mode", toolTip = "Controls amount of logging")]
        public bool DebugMode = false;

    }
}
