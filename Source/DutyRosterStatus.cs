using System;
using System.Collections.Generic;

namespace DutyRoster
{
    /// <summary>
    /// Contains data about a kerbal's duty times
    /// </summary>
    public class DutyRosterStatus
    {
        #region BASIC PROPERTIES
        string name;
        /// <summary>
        /// Kerbal's name
        /// </summary>
        public string Name
        {
            get => name;
            set
            {
                name = value;
                pcmCached = null;
            }
        }

        string trait = null;
        /// <summary>
        /// Returns saved kerbal's trait or current trait if nothing is saved
        /// </summary>
        string Trait
        {
            get => trait ?? PCM.trait;
            set => trait = value;
        }

        string permatrait = null;
        /// <summary>
        /// Returns saved kerbal's trait or current trait if nothing is saved onlyy written when KDR created.
        /// </summary>
        string PermaTrait
        {
            get => permatrait ?? PCM.trait;
            set => permatrait = value;
        }

        /// <summary>
        /// Returns true if the kerbal is marked as being on Duty
        /// </summary>
        public bool IsOnDuty { get; set; } = false;

        /// <summary>
        /// Defaults to [4]hr [13]min (Sunrise at KSC in Year 0
        /// </summary>
        public int DutyStart { get; set; } = 15180; // Time of day in seconds

        /// <summary>
        /// Calculated End of Duty Time. Takes into account Kerbal XP Level
        /// </summary>
        public int DutyEnd { get; set; } = 999999; // Time of day in seconds

        /// <summary>
        /// Duty Time a Kerbal is moving to (30min change per Duty cycle)
        /// </summary>
        public int DutyChangeTo { get; set; } = 0; // Time of day in seconds

        /// <summary>
        /// Returns true if Kerbal in changing their DutystartTime
        /// </summary>
        public bool DutyChanging { get; set; } = false; // Time of day in seconds

        /// <summary>
        /// Returns true if Kerbal already shifted their starting time today
        /// </summary>
        public bool DutyChangedthisShift { get; set; } = false; // Time of day in seconds


        /// <summary>
        /// Returns true for changing Sart Time by ading minutes, false for subtracting
        /// </summary>
        public bool DutyChangeDirection { get; set; } = false; // true = addition


        /// <summary>
        /// Returns true if the kerbal is marked as being on EVA
        /// </summary>
        public bool IsOnEVA { get; set; } = false;

        ProtoCrewMember pcmCached;
        /// <summary>
        /// Returns ProtoCrewMember for the kerbal
        /// </summary>
        public ProtoCrewMember PCM
        {
            get
            {
                if (pcmCached != null) return pcmCached;
                try { return pcmCached = HighLogic.fetch.currentGame.CrewRoster[Name]; }
                catch (Exception)
                {
                    Core.Log("Could not find ProtoCrewMember for " + Name + ". DutyRoster Kerbal List contains " + Core.DutyRosterList.Count + " records:\r\n" + Core.DutyRosterList);
                    return null;
                }
            }
            set
            {
                Name = value.name;
                pcmCached = value;
            }
        }

        /// <summary>
        /// Returns true if the kerbal is member of an array of ProtoCrewMembers
        /// </summary>
        /// <param name="crew"></param>
        /// <returns></returns>
        bool IsInCrew(ProtoCrewMember[] crew)
        {
            foreach (ProtoCrewMember pcm in crew) if (pcm?.name == Name) return true;
            return false;
        }

        public string LocationString
        {
            get
            {
                switch (PCM.rosterStatus)
                {
                    case ProtoCrewMember.RosterStatus.Available: return "KSC";
                    case ProtoCrewMember.RosterStatus.Dead: return "Dead";
                    case ProtoCrewMember.RosterStatus.Missing: return "Unknown";
                }
                Vessel v = Core.KerbalVessel(PCM);
                if (v == null) return "???";
                if (v.isEVA) return "EVA (" + v.mainBody.bodyName + ")";
                return v.vesselName;
            }
        }


        #endregion
        #region DUTYSTATUS

        /// <summary>
        /// Returns false if kerbal is off duty (i.e. turns into a Tourist), true otherwise
        /// </summary>
        public bool IsCapable
        {
            get
            {
                if (!IsOnDuty) return false;
                return true;
            }
        }



        /// <summary>
        /// Process any changes to Start Duty Time
        /// </summary>
        public void AddMinutesStartTime(int minutecount)
        {
            if (!DutyChanging)
            {
                DutyChanging = true;
                DutyChangeTo = DutyStart + (minutecount * 60);
            }
            else
                DutyChangeTo += (minutecount * 60);

            if (DutyChangeTo > KSPUtil.dateTimeFormatter.Day)
                DutyChangeTo -= KSPUtil.dateTimeFormatter.Day;

            if (DutyStart == DutyChangeTo)
                DutyChanging = false;
            else if (!IsOnDuty)
            {
                DutyChangedthisShift = true;
                TimeSpan tstart = TimeSpan.FromSeconds(DutyChangeTo);
                Core.Log(Name + " change start time to " + string.Format("{0:D2}h:{1:D2}m", tstart.Hours, tstart.Minutes) + " UT.");
            }
            
        }

        public void SubtractMinutesStartTime(int minutecount)
        {
            if (!DutyChanging)
            {
                DutyChanging = true;
                DutyChangeTo = DutyStart - (minutecount * 60);
            }
            else
                DutyChangeTo -= (minutecount * 60);

            if (DutyChangeTo < 0)
                DutyChangeTo += KSPUtil.dateTimeFormatter.Day;

            if (DutyStart == DutyChangeTo)
                DutyChanging = false;
            else if (!IsOnDuty)
            {
                DutyChangedthisShift = true;
                TimeSpan tstart = TimeSpan.FromSeconds(DutyChangeTo);
                Core.Log(Name + " change start time to " + string.Format("{0:D2}h:{1:D2}m", tstart.Hours, tstart.Minutes) + " UT.");
            }
        }



        /// <summary>
        /// Process any changes to Start Duty Time
        /// </summary>
        void CheckMovingStartTime()
        {
            //Only run this while OffDuty
            if ((!IsOnDuty) && (Trait != null) && (PCM.type == ProtoCrewMember.KerbalType.Tourist) && (DutyChanging) && (!DutyChangedthisShift))
            {

                int CurrentUTseconds = Core.UTSeconds(Planetarium.GetUniversalTime());


                //somehow wait 1hour into OffDuty to change this
                //int runafter = (DutyEnd + (HighLogic.CurrentGame.Parameters.CustomParams<DutyRosterGeneralSettings>().DutyChangeMinutesPerShift * 60));
                //if (runafter > KSPUtil.dateTimeFormatter.Day)
                //{ 
                //    runafter -= KSPUtil.dateTimeFormatter.Day;
                //    if (CurrentUTseconds < runafter) return;
               // }
               // else
               //     if (CurrentUTseconds < runafter) return;

                int changeamtfwd = 0;
                int changeamtbwd = 0;
                if (DutyChangeTo < DutyStart)
                {
                    changeamtfwd = (DutyChangeTo + KSPUtil.dateTimeFormatter.Day) - DutyStart;
                    changeamtbwd = DutyStart - DutyChangeTo;
                }
                else if (DutyStart < DutyChangeTo)
                {
                    changeamtfwd = DutyChangeTo - DutyStart;
                    changeamtbwd = (DutyStart + KSPUtil.dateTimeFormatter.Day) - DutyChangeTo;
                }



                if((changeamtfwd < (HighLogic.CurrentGame.Parameters.CustomParams<DutyRosterGeneralSettings>().DutyChangeMinutesPerShift * 60)) ||
                   (changeamtbwd < (HighLogic.CurrentGame.Parameters.CustomParams<DutyRosterGeneralSettings>().DutyChangeMinutesPerShift * 60)))
                {
                    // Change too small, just set new time and finish
                    DutyStart = DutyChangeTo;
                    DutyEnd = GetEndDutyTime();
                    DutyChanging = false;
                }
                else
                {
                    if (changeamtfwd < changeamtbwd)
                    {
                        DutyChangeDirection = true;
                        DutyStart += (HighLogic.CurrentGame.Parameters.CustomParams<DutyRosterGeneralSettings>().DutyChangeMinutesPerShift * 60);
                        if (DutyStart > KSPUtil.dateTimeFormatter.Day)
                            DutyStart -= KSPUtil.dateTimeFormatter.Day;
                    }
                    else
                    {
                        DutyChangeDirection = false;
                        DutyStart -= (HighLogic.CurrentGame.Parameters.CustomParams<DutyRosterGeneralSettings>().DutyChangeMinutesPerShift * 60);
                        if (DutyStart < 0)
                            DutyStart += KSPUtil.dateTimeFormatter.Day;
                    }
                    DutyChangedthisShift = true;

                    if(DutyStart==DutyChangeTo)
                        DutyChanging = false;
                    else
                        DutyEnd = GetEndDutyTime();

                }

                TimeSpan tstart = TimeSpan.FromSeconds(DutyStart);
                Core.Log(Name + " moved Start time to " + string.Format("{0:D2}h:{1:D2}m", tstart.Hours, tstart.Minutes) + " UT.", Core.LogLevel.Important);
                return;
            }
            
        }

        /// <summary>
        /// Process Duty Status
        /// </summary>
        void UpdateDutyStatus()
        {
            //Fix any rouge DateStrt Numbers and update DutyEnd
            if (DutyStart > KSPUtil.dateTimeFormatter.Day)
            {
                DutyStart -= KSPUtil.dateTimeFormatter.Day;
                DutyEnd = GetEndDutyTime();
            }
            if (DutyStart < 0)
            {
                DutyStart += KSPUtil.dateTimeFormatter.Day;
                DutyEnd = GetEndDutyTime();
            }
            if(DutyEnd == 999999) GetEndDutyTime();


            int CurrentUTseconds = Core.UTSeconds(Planetarium.GetUniversalTime());

            if (IsOnDuty)
            {
                if (DutyEnd < DutyStart)
                {
                    if ((CurrentUTseconds > DutyEnd) && (CurrentUTseconds < DutyStart)) MakeOffDuty();
                }
                else
                {
                    if ((CurrentUTseconds < DutyStart) || (CurrentUTseconds > DutyEnd)) MakeOffDuty();
                }
            }
            else
            {
                if (DutyEnd < DutyStart)
                {
                    if ((CurrentUTseconds <= DutyEnd) || (CurrentUTseconds >= DutyStart)) MakeOnDuty();
                }
                else
                {
                    if ((CurrentUTseconds >= DutyStart) && (CurrentUTseconds <= DutyEnd)) MakeOnDuty();
                }
            }


        }


        /// <summary>
        /// Returns EndDuty time in seconds. Takes into account Game Settings
        /// </summary>
        /// <returns></returns>
        public int GetEndDutyTime()
        {
            int CurrentUTseconds = Core.UTSeconds(Planetarium.GetUniversalTime());

            DutyEnd = DutyStart +
                (HighLogic.CurrentGame.Parameters.CustomParams<DutyRosterGeneralSettings>().OnDutyHours * 3600) +
                (HighLogic.CurrentGame.Parameters.CustomParams<DutyRosterGeneralSettings>().OnDutyMinutes * 60) +
                (PCM.experienceLevel * HighLogic.CurrentGame.Parameters.CustomParams<DutyRosterGeneralSettings>().DutyMinutesPerLevel * 60);
            if (DutyEnd > KSPUtil.dateTimeFormatter.Day)
                DutyEnd -= KSPUtil.dateTimeFormatter.Day;

            return (DutyEnd);
        }


        /// <summary>
        /// Turn a kerbal into a Tourist
        /// </summary>
        void MakeOffDuty()
        {
            if ((!IsOnDuty) && (Trait != null) && (PCM.type == ProtoCrewMember.KerbalType.Tourist))
            {
                Core.Log(Name + " is already Off Duty.", Core.LogLevel.Important);
                IsOnDuty = false;
                return;
            }
            IsOnDuty = false;
            if (PCM.type != ProtoCrewMember.KerbalType.Tourist) Trait = PCM.trait;
            PCM.type = ProtoCrewMember.KerbalType.Tourist;
            Core.Log(Name + " (" + Trait + ") is Off Duty.", Core.LogLevel.Important);
            KerbalRoster.SetExperienceTrait(PCM, KerbalRoster.touristTrait);
            DRGameEvents.onKerbalOffDuty.Fire(PCM);
        }

        /// <summary>
        /// Revives a kerbal after being offduty
        /// </summary>
        void MakeOnDuty()
        {
            if (PCM.type != ProtoCrewMember.KerbalType.Tourist)
            {
                Trait = null;
                IsOnDuty = true;
                return;  // Apparently, the kerbal has been revived by another mod
            }
            Core.Log(Name + " is becoming " + (Trait ?? "something strange") + " again.", Core.LogLevel.Important);
            if ((Trait != null) && (Trait != "Tourist"))
            {
                PCM.type = ProtoCrewMember.KerbalType.Crew;
                KerbalRoster.SetExperienceTrait(PCM, Trait);
            }
            Trait = null;
            IsOnDuty = true;
            DutyChangedthisShift = false;
            DRGameEvents.onKerbalOnDuty.Fire(PCM);
        }
        #endregion
        
        /// <summary>
        /// Updates kerbal's Roster status
        /// </summary>
        /// <param name="interval">Number of seconds since the last update</param>
        public void Update(double interval)
        {
            Core.Log("Updating " + Name + "'s Duty Roster.");
            if (PCM == null)
            {
                Core.Log(Name + " ProtoCrewMember record not found. Aborting Roster update.", Core.LogLevel.Error);
                return;
            }
            CheckMovingStartTime();
            UpdateDutyStatus();
        }


        #region SAVING, LOADING, INITIALIZING ETC.
        public ConfigNode ConfigNode
        {
            get
            {
                ConfigNode n = new ConfigNode("DutyRosterStatus");
                n.AddValue("name", Name);
                if (!IsCapable) n.AddValue("trait", Trait);
                n.AddValue("PermaTrait", PermaTrait);
                n.AddValue("StartDuty", DutyStart);
                if (IsOnDuty) n.AddValue("IsOnDuty", true);
                if (DutyChanging)
                {
                    n.AddValue("DutyChanging", true);
                    n.AddValue("DutyChangeTo", DutyChangeTo);
                    n.AddValue("DutyChangedthisShift", DutyChangedthisShift);
                }
                if (IsOnEVA) n.AddValue("onEva", true);
                

                return n;
            }
            set
            {
                Name = value.GetValue("name");
                Trait = value.GetValue("trait");
                PermaTrait = value.GetValue("PermaTrait");
                DutyStart = Core.GetInt(value, "StartDuty");
                IsOnDuty = Core.GetBool(value, "IsOnDuty");
                DutyChanging = Core.GetBool(value, "DutyChanging");
                if (DutyChanging)
                {
                    DutyChangeTo = Core.GetInt(value, "DutyChangeTo");
                    DutyChangedthisShift = Core.GetBool(value, "DutyChangedthisShift");
                }
                IsOnEVA = Core.GetBool(value, "onEva");
                
            }
        }

        public override bool Equals(object obj) => ((DutyRosterStatus)obj).Name.Equals(Name);

        public override int GetHashCode() => ConfigNode.GetHashCode();

        public DutyRosterStatus Clone() => (DutyRosterStatus)this.MemberwiseClone();

        public DutyRosterStatus(string name, int dutystart)
        {
            Name = name;
            DutyStart = dutystart;
            if (PCM.type != ProtoCrewMember.KerbalType.Tourist) PermaTrait = PCM.trait;
            Core.Log("Created record for " + name + " with StartTime of " + dutystart);
        }

        public DutyRosterStatus(string name)
        {
            Name = name;
            if (PCM.type != ProtoCrewMember.KerbalType.Tourist) PermaTrait = PCM.trait;
            DutyStart = Core.rand.Next(13000, KSPUtil.dateTimeFormatter.Day);
            DutyEnd = GetEndDutyTime();
            TimeSpan tstart = TimeSpan.FromSeconds(DutyStart);
            Core.Log("Created record for " + name + " with StartTime of " + string.Format("{0:D2}h:{1:D2}m", tstart.Hours, tstart.Minutes) + " UT", Core.LogLevel.Important);
        }

        public DutyRosterStatus(ConfigNode node) => ConfigNode = node;
        #endregion
    }
}
