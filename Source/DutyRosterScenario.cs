using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using KSP.UI.Screens;

namespace DutyRoster
{
    /// <summary>
    /// Main class for processing kerbals' duty roster
    /// </summary>
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.SPACECENTER, GameScenes.TRACKSTATION, GameScenes.FLIGHT, GameScenes.EDITOR)]
    public class DutyRosterScenario : ScenarioModule
    {
        static double lastUpdated;  // UT at last Duty Roster update
        Version version;  // Current Duty Roster version

        ApplicationLauncherButton appLauncherButton;
        IButton toolbarButton;
        SortedList<ProtoCrewMember, DutyRosterStatus> kerbals;// = new SortedList<ProtoCrewMember, DutyRosterStatus>(KerbalComparer.Default);
        bool dirty = false, crewChanged = false;
        const int colNumMain = 8, colNumDetails = 6;  // # of columns in Duty Roster
        const int colWidth = 100;  // Width of a cell
        const int colSpacing = 10;
        const int gridWidthMain = colNumMain * (colWidth + colSpacing) - colSpacing,
            gridWidthDetails = colNumDetails * (colWidth + colSpacing) - colSpacing;  // Grid width
        Rect monitorPosition = new Rect(0.5f, 0.5f, gridWidthMain, 200);
        PopupDialog monitorWindow;  // Duty Roster window
        System.Collections.Generic.List<DialogGUIBase> gridContents;  // Duty Roster grid's labels
        int page = 1;  // Current page in the list of kerbals

        public void Start()
        {
            if (!Core.ModEnabled) return;
            Core.Log("DutyRosterScenario.Start", Core.LogLevel.Important);
            Core.DutyRosterList.RegisterKerbals();

            GameEvents.onCrewOnEva.Add(OnKerbalEva);
            GameEvents.onCrewKilled.Add(OnCrewKilled);
            GameEvents.OnCrewmemberHired.Add(OnCrewmemberHired);
            GameEvents.OnCrewmemberSacked.Add(OnCrewmemberSacked);
            GameEvents.onKerbalAdded.Add(OnKerbalAdded);
            GameEvents.onKerbalRemoved.Add(OnKerbalRemoved);
            GameEvents.onKerbalNameChanged.Add(OnKerbalNameChanged);
            DRGameEvents.onKerbalOffDuty.Add(OnKerbalDutyChange);
            DRGameEvents.onKerbalOnDuty.Add(OnKerbalDutyChange);


            if (ToolbarManager.ToolbarAvailable && Core.UseBlizzysToolbar)
            {
                Core.Log("Registering Blizzy's Toolbar button...", Core.LogLevel.Important);
                toolbarButton = ToolbarManager.Instance.add("DutyRoster", "DutyRosterMonitor");
                toolbarButton.Text = "Kerbal Duty Roster";
                toolbarButton.TexturePath = "DutyRoster/toolbar";
                toolbarButton.ToolTip = "Duty Roster";
                toolbarButton.OnClick += (e) => { if (monitorWindow == null) DisplayData(); else UndisplayData(); };
            }
            else
            {
                Core.Log("Registering AppLauncher button...", Core.LogLevel.Important);
                Texture2D icon = new Texture2D(38, 38);
                icon.LoadImage(System.IO.File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "icon.png")));
                appLauncherButton = ApplicationLauncher.Instance.AddModApplication(DisplayData, UndisplayData, null, null, null, null, ApplicationLauncher.AppScenes.ALWAYS, icon);
            }
            lastUpdated = Planetarium.GetUniversalTime();

            // Automatically updating settings from older versions
            Version v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            if (version != v)
            {
                Core.Log("Current mod version " + v + " is different from v" + version + " used to save the game. Most likely, Duty Roster has been recently updated.", Core.LogLevel.Important);
                if ((version < new Version("1.1.0")) && (Planetarium.GetUniversalTime() > 0))
                {
                    Core.ShowMessage("Duty Roster has been updated to v" + v.ToString(3) + ". Some change happen in previous version . It is recommended that you load each crewed vessel briefly to update Duty Roster cache.", true);
                }
                version = v;
            }
            else Core.Log("Duty Roster v" + version);
            Core.Log("DutyRosterScenario.Start finished.", Core.LogLevel.Important);
        }

        public void OnDisable()
        {
            Core.Log("DutyRosterScenario.OnDisable", Core.LogLevel.Important);
            UndisplayData();

            GameEvents.onCrewOnEva.Remove(OnKerbalEva);
            GameEvents.onCrewKilled.Remove(OnCrewKilled);
            GameEvents.OnCrewmemberHired.Remove(OnCrewmemberHired);
            GameEvents.OnCrewmemberSacked.Remove(OnCrewmemberSacked);
            GameEvents.onKerbalAdded.Remove(OnKerbalAdded);
            GameEvents.onKerbalRemoved.Remove(OnKerbalRemoved);
            GameEvents.onKerbalNameChange.Remove(OnKerbalNameChanged);
            DRGameEvents.onKerbalOffDuty.Remove(OnKerbalDutyChange);

            if (toolbarButton != null) toolbarButton.Destroy();
            if ((appLauncherButton != null) && (ApplicationLauncher.Instance != null))
                ApplicationLauncher.Instance.RemoveModApplication(appLauncherButton);
            Core.Log("DutyRosterScenario.OnDisable finished.", Core.LogLevel.Important);
        }

        /// <summary>
        /// Marks the kerbal as being on EVA to apply EVA-only effects
        /// </summary>
        /// <param name="action"></param>
        public void OnKerbalEva(GameEvents.FromToAction<Part, Part> action)
        {
            if (!Core.ModEnabled) return;
            Core.Log(action.to.protoModuleCrew[0].name + " went on EVA from " + action.from.name + ".", Core.LogLevel.Important);
            Core.DutyRosterList.Find(action.to.protoModuleCrew[0]).IsOnEVA = true;
            UpdateKerbals(true);
        }

        public void OnCrewKilled(EventReport er)
        {
            Core.Log("OnCrewKilled(<'" + er.msg + "', " + er.sender + ", " + er.other + ">)", Core.LogLevel.Important);
            Core.DutyRosterList.Remove(er.sender);
            dirty = crewChanged = true;
        }

        public void OnCrewmemberHired(ProtoCrewMember pcm, int i)
        {
            Core.Log("OnCrewmemberHired('" + pcm.name + "', " + i + ")", Core.LogLevel.Important);
            dirty = crewChanged = true;
        }

        public void OnCrewmemberSacked(ProtoCrewMember pcm, int i)
        {
            Core.Log("OnCrewmemberSacked('" + pcm.name + "', " + i + ")", Core.LogLevel.Important);
            Core.DutyRosterList.Remove(pcm.name);
            dirty = crewChanged = true;
        }

        public void OnKerbalAdded(ProtoCrewMember pcm)
        {
            if ((pcm.type == ProtoCrewMember.KerbalType.Applicant) || (pcm.type == ProtoCrewMember.KerbalType.Unowned))
            {
                Core.Log("The kerbal is " + pcm.type + ". Skipping.");
                return;
            }
            Core.Log("OnKerbalAdded('" + pcm.name + "')", Core.LogLevel.Important);
            Core.DutyRosterList.Add(pcm.name);
            dirty = crewChanged = true;
        }

        public void OnKerbalRemoved(ProtoCrewMember pcm)
        {
            Core.Log("OnKerbalRemoved('" + pcm.name + "')", Core.LogLevel.Important);
            Core.DutyRosterList.Remove(pcm.name);
            dirty = crewChanged = true;
        }

        public void OnKerbalNameChanged(ProtoCrewMember pcm, string name1, string name2)
        {
            Core.Log("OnKerbalNameChanged('" + pcm.name + "', '" + name1 + "', '" + name2 + "')", Core.LogLevel.Important);
            Core.DutyRosterList.Rename(name1, name2);
            dirty = true;
        }

        public void OnKerbalDutyChange(ProtoCrewMember pcm)
        {
            Core.Log("OnKerbalDutyChange'" + pcm.name + "')", Core.LogLevel.Important);
            dirty = crewChanged = true;
        }

        /// <summary>
        /// The main method for updating all kerbals' duty roster
        /// </summary>
        /// <param name="forced">Whether to process kerbals regardless of the amount of time passed</param>
        void UpdateKerbals(bool forced)
        {
            double time = Planetarium.GetUniversalTime();
            double timePassed = time - lastUpdated;
            if (timePassed <= 0) return;
            if (forced || ((timePassed >= Core.UpdateInterval) && (timePassed >= Core.MinUpdateInterval * TimeWarp.CurrentRate)))
            {
                Core.ClearCache();
                Core.DutyRosterList.Update(timePassed);
                lastUpdated = time;
                dirty = true;
            }
        }

        public void FixedUpdate()
        { if (Core.ModEnabled) UpdateKerbals(false); }

        int LinesPerPage => HighLogic.CurrentGame.Parameters.CustomParams<DutyRosterGeneralSettings>().LinesPerPage;

        bool ShowPages => Core.DutyRosterList.Count > LinesPerPage;

        int PageCount => (int)System.Math.Ceiling((double)(Core.DutyRosterList.Count) / LinesPerPage);

        int FirstLine => (page - 1) * LinesPerPage;

        int LineCount => System.Math.Min(Core.DutyRosterList.Count - FirstLine, LinesPerPage);

        void FirstPage()
        {
            dirty = page != PageCount;
            page = 1;
            if (!dirty) Invalidate();
        }

        void PageUp()
        {
            dirty = page != PageCount;
            if (page > 1) page--;
            if (!dirty) Invalidate();
        }

        void PageDown()
        {
            if (page < PageCount) page++;
            if (page == PageCount) Invalidate();
            else dirty = true;
        }

        void LastPage()
        {
            page = PageCount;
            Invalidate();
        }

        /// <summary>
        /// Shows Duty Roster when the AppLauncher/Blizzy's Toolbar button is clicked
        /// </summary>
        public void DisplayData()
        {
            Core.Log("DutyRosterScenario.DisplayData", Core.LogLevel.Important);
            UpdateKerbals(true);

            // Preparing a sorted list of kerbals
            kerbals = new SortedList<ProtoCrewMember, DutyRosterStatus>(new KerbalComparer(HighLogic.CurrentGame.Parameters.CustomParams<DutyRosterGeneralSettings>().SortByStartTimes));
            foreach (DutyRosterStatus drs in Core.DutyRosterList.Values)
                kerbals.Add(drs.PCM, drs);

            DialogGUILayoutBase layout = new DialogGUIVerticalLayout(true, true);
            if (page > PageCount) page = PageCount;
            if (ShowPages) layout.AddChild(new DialogGUIHorizontalLayout(true, false,
                new DialogGUIButton("<<", FirstPage, () => (page > 1), true),
                new DialogGUIButton("<", PageUp, () => (page > 1), false),
                new DialogGUIHorizontalLayout(TextAnchor.LowerCenter, new DialogGUILabel("Page " + page + "/" + PageCount)),
                new DialogGUIButton(">", PageDown, () => (page < PageCount), false),
                new DialogGUIButton(">>", LastPage, () => (page < PageCount), true)));
            gridContents = new List<DialogGUIBase>((Core.DutyRosterList.Count + 1) * colNumMain);

            // Creating column titles
            gridContents.Add(new DialogGUILabel("<b><color=\"white\">Name</color></b>", true));
            gridContents.Add(new DialogGUILabel("<b><color=\"white\">Location</color></b>", true));
            gridContents.Add(new DialogGUILabel("<b><color=\"white\">Duty Start</color></b>", true));
            gridContents.Add(new DialogGUILabel("<b><color=\"white\">Duty End</color></b>", true));
            gridContents.Add(new DialogGUILabel("<b><color=\"white\">Changing to</color></b>", true));
            gridContents.Add(new DialogGUILabel("<b><color=\"white\">Currently</color></b>", true));
            gridContents.Add(new DialogGUILabel("<b><color=\"white\">Change Start Time</color></b>", true));
            gridContents.Add(new DialogGUILabel("", false));

            
            DialogGUIBase addbtnsmall = null;
            DialogGUIBase minbtnsmall = null;
            DialogGUIBase addbtnmedium = null;
            DialogGUIBase minbtnmedium = null;


            // Initializing Duty Roster's grid with empty labels, to be filled in Update()
            for (int i = FirstLine; i < FirstLine + LineCount; i++)
            {
                for (int j = 0; j < colNumMain - 2; j++)
                    gridContents.Add(new DialogGUILabel("", false));

                addbtnsmall = new DialogGUIButton<int>("+1", (n) => { kerbals.Values[n].AddMinutesStartTime(1); }, i, false);
                minbtnsmall = new DialogGUIButton<int>("-1", (n) => { kerbals.Values[n].SubtractMinutesStartTime(1); }, i, false);
                addbtnmedium = new DialogGUIButton<int>("+10", (n) => { kerbals.Values[n].AddMinutesStartTime(10); }, i, false);
                minbtnmedium = new DialogGUIButton<int>("-10", (n) => { kerbals.Values[n].SubtractMinutesStartTime(10); }, i, false);

                addbtnsmall.size = new Vector2(20, 20);
                minbtnsmall.size = new Vector2(20, 20);
                addbtnmedium.size = new Vector2(30, 20);
                minbtnmedium.size = new Vector2(30, 20);

                gridContents.Add(new DialogGUIHorizontalLayout(new DialogGUIBase[] {addbtnsmall, minbtnsmall, addbtnmedium, minbtnmedium }));
                gridContents.Add(new DialogGUILabel("", true));
            }
            layout.AddChild(new DialogGUIGridLayout(new RectOffset(0, 0, 0, 0), new Vector2(colWidth, 20), new Vector2(colSpacing, 10), UnityEngine.UI.GridLayoutGroup.Corner.UpperLeft, UnityEngine.UI.GridLayoutGroup.Axis.Horizontal, TextAnchor.MiddleCenter, UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount, colNumMain, gridContents.ToArray()));
            monitorPosition.width = gridWidthMain + 10;
            monitorWindow = PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new MultiOptionDialog("Duty Roster", "", "Duty Roster", HighLogic.UISkin, monitorPosition, layout), false, HighLogic.UISkin, false);

            dirty = true;
        }

        /// <summary>
        /// Hides the Duty Roster window
        /// </summary>
        public void UndisplayData()
        {
            if (monitorWindow != null)
            {
                Vector3 v = monitorWindow.RTrf.position;
                monitorPosition = new Rect(v.x / Screen.width + 0.5f, v.y / Screen.height + 0.5f, gridWidthMain + 20, 50);
                monitorWindow.Dismiss();
            }
        }

        void Invalidate()
        {
            UndisplayData();
            DisplayData();
        }

        /// <summary>
        /// Displays actual values in Duty Roster
        /// </summary>
        public void Update()
        {
            if (!Core.ModEnabled)
            {
                if (monitorWindow != null) monitorWindow.Dismiss();
                return;
            }

            if ((monitorWindow == null) || !dirty) return;

            if (gridContents == null)
            {
                Core.Log("DutyRosterScenario.gridContents is null.", Core.LogLevel.Error);
                monitorWindow.Dismiss();
                return;
            }


            if (crewChanged)
            {
                Core.DutyRosterList.RegisterKerbals();
                Invalidate();
                crewChanged = false;
            }
            Core.Log(kerbals.Count + " kerbals in Duty Roster list.");
            // Fill the Duty Roster's grid with kerbals start/end time data
            for (int i = 0; i < LineCount; i++)
            {
                DutyRosterStatus drs = kerbals.Values[FirstLine + i];
                string formatTag = "", formatUntag = "";
                string dutystatus = "";
                if (drs.IsOnDuty)
                {
                    dutystatus = "On Duty (" + drs.PCM.trait + ")";
                    formatTag = "<color=\"yellow\">";
                    formatUntag = "</color>";
                }
                else
                {
                    dutystatus = "Off Duty";
                    formatTag = "<color=\"red\">";
                    formatUntag = "</color>";
                }
                string chgstr = "";
                if (drs.DutyChanging)
                { 
                    TimeSpan chgstart = TimeSpan.FromSeconds(drs.DutyChangeTo);
                    chgstr = string.Format("{0:D2}h:{1:D2}m", chgstart.Hours, chgstart.Minutes) + " UT";
                }
                else
                    chgstr = "-";

                TimeSpan tstart = TimeSpan.FromSeconds(drs.DutyStart);
                TimeSpan tend = TimeSpan.FromSeconds(drs.DutyEnd);

                gridContents[(i + 1) * colNumMain].SetOptionText(drs.Name);
                gridContents[(i + 1) * colNumMain + 1].SetOptionText(drs.LocationString);
                gridContents[(i + 1) * colNumMain + 2].SetOptionText(string.Format("{0:D2}h:{1:D2}m", tstart.Hours, tstart.Minutes) + " UT");
                if (HighLogic.LoadedScene == GameScenes.EDITOR)
                    gridContents[(i + 1) * colNumMain + 3].SetOptionText("IN VAB / SPH");
                else
                    gridContents[(i + 1) * colNumMain + 3].SetOptionText(string.Format("{0:D2}h:{1:D2}m", tend.Hours, tend.Minutes) + " UT");
                gridContents[(i + 1) * colNumMain + 4].SetOptionText(chgstr);
                gridContents[(i + 1) * colNumMain + 5].SetOptionText(formatTag + dutystatus + formatUntag);
            }

            dirty = false;
        }

        public override void OnSave(ConfigNode node)
        {
            if (!Core.ModEnabled) return;
            Core.Log("DutyRosterScenario.OnSave", Core.LogLevel.Important);
            UpdateKerbals(true);
            node.AddValue("version", version.ToString());
            int i = 0;
            foreach (DutyRosterStatus drs in Core.DutyRosterList.Values)
            {
                Core.Log("Saving " + drs.Name + "'s roster.");
                node.AddNode(drs.ConfigNode);
                i++;
            }
            Core.Log("DutyRosterScenario.OnSave complete. " + i + " kerbal(s) saved.", Core.LogLevel.Important);
        }

        public override void OnLoad(ConfigNode node)
        {
            if (!Core.Loaded) Core.LoadConfig();
            if (!Core.ModEnabled) return;
            Core.Log("DutyRosterScenario.OnLoad", Core.LogLevel.Important);
            version = new Version(Core.GetString(node, "version", "0.0"));
            Core.DutyRosterList.Clear();
            int i = 0;
            foreach (ConfigNode n in node.GetNodes("DutyRosterStatus"))
            {
                Core.DutyRosterList.Add(new DutyRosterStatus(n));
                i++;
            }
            lastUpdated = Planetarium.GetUniversalTime();
            Core.Log("" + i + " kerbal(s) loaded.", Core.LogLevel.Important);
        }
    }

    /// <summary>
    /// Class used for ordering vessels in Duty Roster
    /// </summary>
    public class KerbalComparer : Comparer<ProtoCrewMember>
    {
        bool sortByStartTimes;
        
        public int CompareStartTimes(ProtoCrewMember x, ProtoCrewMember y)
        {
            double srt1 = Core.DutyRosterList.Find(x).DutyStart;
            double srt2 = Core.DutyRosterList.Find(y).DutyStart;
            return (srt1 < srt2) ? -1 : ((srt1 > srt2) ? 1 : 0);
        }

        public int CompareLocation(ProtoCrewMember x, ProtoCrewMember y)
        {
            if (x.rosterStatus != ProtoCrewMember.RosterStatus.Assigned) return y.rosterStatus == ProtoCrewMember.RosterStatus.Assigned ? 1 : 0;
            if (y.rosterStatus != ProtoCrewMember.RosterStatus.Assigned) return -1;
            Vessel xv = Core.KerbalVessel(x), yv = Core.KerbalVessel(y);
            if (HighLogic.LoadedSceneIsFlight)
            {
                if (xv.isActiveVessel) return yv.isActiveVessel ? 0 : -1;
                if (yv.isActiveVessel) return 1;
            }
            if (xv.isEVA) return yv.isEVA ? 0 : -1;
            if (yv.isEVA) return 1;
            return string.Compare(xv.vesselName, yv.vesselName, true);
        }

        public override int Compare(ProtoCrewMember x, ProtoCrewMember y)
        {
            if (sortByStartTimes)
            {
                int l = CompareStartTimes(x, y);
                Core.Log("Time comparison result: " + x.name + " " + (l < 0 ? "<" : (l > 0 ? ">" : "=")) + " " + y.name);
                if (l == 0) Core.Log("Name comparison: " + string.Compare(x.name, y.name, true));
                return (l != 0) ? l : string.Compare(x.name, y.name, true);
            }
            return string.Compare(x.name, y.name, true);
        }

        public KerbalComparer(bool sortByStartTimes) => this.sortByStartTimes = sortByStartTimes;
    }
}
