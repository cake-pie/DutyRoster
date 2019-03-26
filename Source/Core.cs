using System;
using System.Collections.Generic;
using UnityEngine;

namespace DutyRoster
{
    /// <summary>
    /// Provides general static methods and fields for DutyRoster
    /// </summary>
    public class Core
    {
        public static bool Loaded = false;

        /// <summary> 
        /// List of all tracked kerbals
        /// </summary>
        public static DutyRosterList DutyRosterList { get; set; } = new DutyRosterList();


        public static void LoadConfig()
        {
            Log("Loading config...", LogLevel.Important);
            Loaded = true;
        }

        #region SETTINGS

        /// <summary>
        /// Is Duty Roster enabled via Settings menu?
        /// </summary>
        public static bool ModEnabled
        {
            get => HighLogic.CurrentGame.Parameters.CustomParams<DutyRosterGeneralSettings>().modEnabled;
            set => HighLogic.CurrentGame.Parameters.CustomParams<DutyRosterGeneralSettings>().modEnabled = value;
        }

        /// <summary>
        /// Use Blizzy's Toolbar mod instead of stock app launcher
        /// </summary>
        public static bool UseBlizzysToolbar
        {
            get => HighLogic.CurrentGame.Parameters.CustomParams<DutyRosterGeneralSettings>().UseBlizzysToolbar;
            set => HighLogic.CurrentGame.Parameters.CustomParams<DutyRosterGeneralSettings>().UseBlizzysToolbar = value;
        }

        /// <summary>
        /// Number of game seconds between updates
        /// </summary>
        public static float UpdateInterval
        {
            get => HighLogic.CurrentGame.Parameters.CustomParams<DutyRosterGeneralSettings>().UpdateInterval;
            set => HighLogic.CurrentGame.Parameters.CustomParams<DutyRosterGeneralSettings>().UpdateInterval = value;
        }

        /// <summary>
        /// Minimum number of real-world seconds between updates (used in high timewarp)
        /// </summary>
        public static float MinUpdateInterval
        {
            get => HighLogic.CurrentGame.Parameters.CustomParams<DutyRosterGeneralSettings>().MinUpdateInterval;
            set => HighLogic.CurrentGame.Parameters.CustomParams<DutyRosterGeneralSettings>().MinUpdateInterval = value;
        }

      
        #endregion
        /// <summary>
        /// True if the current scene is Editor (VAB or SPH)
        /// </summary>
        public static bool IsInEditor => HighLogic.LoadedSceneIsEditor;

        /// <summary>
        /// Returns number of current crew in a vessel the kerbal is in or in the currently constructed vessel
        /// </summary>
        /// <param name="pcm"></param>
        /// <returns></returns>
        public static int GetCrewCount(ProtoCrewMember pcm) => IsInEditor ? ShipConstruction.ShipManifest.CrewCount : (IsKerbalLoaded(pcm) ? KerbalVessel(pcm).GetCrewCount() : 1);

        /// <summary>
        /// Returns number of maximum crew in a vessel the kerbal is in or in the currently constructed vessel
        /// </summary>
        /// <param name="pcm"></param>
        /// <returns></returns>
        public static int GetCrewCapacity(ProtoCrewMember pcm) => IsInEditor ? ShipConstruction.ShipManifest.GetAllCrew(true).Count : (IsKerbalLoaded(pcm) ? Math.Max(KerbalVessel(pcm).GetCrewCapacity(), 1) : 1);

        /// <summary>
        /// Returns Part where ProtoCrewMember is currently located or null if none
        /// </summary>
        /// <param name="pcm"></param>
        /// <returns></returns>
        public static Part GetCrewPart(ProtoCrewMember pcm) => IsInEditor ? KSPUtil.GetPartByCraftID(EditorLogic.SortedShipList, ShipConstruction.ShipManifest.GetPartForCrew(pcm).PartID) : pcm?.seat?.part;

        /// <summary>
        /// Returns true if the kerbal is in a loaded vessel
        /// </summary>
        /// <param name="pcm"></param>
        /// <returns></returns>
        public static bool IsKerbalLoaded(ProtoCrewMember pcm) => KerbalVessel(pcm)?.loaded ?? false;

        /// <summary>
        /// Returns true if kerbal exists and is either or available
        /// </summary>
        /// <param name="pcm"></param>
        /// <returns></returns>
        public static bool IsKerbalTrackable(ProtoCrewMember pcm) => (pcm != null) && ((pcm.rosterStatus == ProtoCrewMember.RosterStatus.Assigned) || (pcm.rosterStatus == ProtoCrewMember.RosterStatus.Available));

        static Dictionary<string, Vessel> kerbalVesselsCache = new Dictionary<string, Vessel>();

        /// <summary>
        /// Clears kerbal vessels cache, to be called on every list update or when necessary
        /// </summary>
        public static void ClearCache()
        {
            kerbalVesselsCache.Clear();
        }

        /// <summary>
        /// Returns <see cref="Vessel"/> the kerbal is in or null if the kerbal is not assigned
        /// </summary>
        /// <param name="pcm"></param>
        /// <returns></returns>
        public static Vessel KerbalVessel(ProtoCrewMember pcm)
        {
            if ((pcm == null) || (pcm.rosterStatus == ProtoCrewMember.RosterStatus.Available)) return null;
            if (kerbalVesselsCache.ContainsKey(pcm.name)) return kerbalVesselsCache[pcm.name];
            foreach (Vessel v in FlightGlobals.Vessels)
                foreach (ProtoCrewMember k in v.GetVesselCrew())
                    if (k == pcm)
                    {
                        kerbalVesselsCache.Add(pcm.name, v);
                        return v;
                    }
            Log(pcm.name + " is " + pcm.rosterStatus + " and was not found in any of the " + FlightGlobals.Vessels.Count + " vessels!", LogLevel.Important);
            return null;
        }

        public static bool IsPlanet(CelestialBody body) => body?.orbit?.referenceBody == Sun.Instance.sun;

        public static CelestialBody GetPlanet(CelestialBody body) => ((body == null) || IsPlanet(body)) ? body : GetPlanet(body?.orbit?.referenceBody);

        public static string GetString(ConfigNode n, string key, string defaultValue = null) => n.HasValue(key) ? n.GetValue(key) : defaultValue;

        public static double GetDouble(ConfigNode n, string key, double defaultValue = 0)
        {
            double res;
            try { res = Double.Parse(n.GetValue(key)); }
            catch (Exception) { res = defaultValue; }
            return res;
        }

        public static int GetInt(ConfigNode n, string key, int defaultValue = 0)
        {
            int res;
            try { res = Int32.Parse(n.GetValue(key)); }
            catch (Exception) { res = defaultValue; }
            return res;
        }

        public static bool GetBool(ConfigNode n, string key, bool defaultValue = false)
        {
            bool res;
            try { res = Boolean.Parse(n.GetValue(key)); }
            catch (Exception) { res = defaultValue; }
            return res;
        }

        /// <summary>
        /// Returns x*x
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static double Sqr(double x) => x * x;

        /// <summary>
        /// Returns a string representing value v with a mandatory sign (+ or -, unless v = 0)
        /// </summary>
        /// <param name="v">Value to present as a string</param>
        /// <param name="format">String format according to Double.ToString</param>
        /// <returns></returns>
        public static string SignValue(double v, string format) => ((v > 0) ? "+" : "") + v.ToString(format);

        static string[] prefixes = { "", "K", "M", "G", "T" };

        /// <summary>
        /// Converts a number into a string with a multiplicative character (K, M, G or T), if applicable
        /// </summary>
        /// <param name="value">The value to convert</param>
        /// <param name="allowedDigits">Max number of digits to allow before the prefix (must be 3 or more)</param>
        /// <returns></returns>
        public static string PrefixFormat(double value, int allowedDigits = 3, bool mandatorySign = false)
        {
            double v = Math.Abs(value);
            if (v < 0.5) return "0";
            int n, m = (int)Math.Pow(10, allowedDigits);
            for (n = 0; (v >= m) && (n < prefixes.Length - 1); n++)
                v /= 1000;
            return (value < 0 ? "-" : (mandatorySign ? "+" : "")) + v.ToString("N0") + prefixes[n];
        }

        /// <summary>
        /// Returns the number of occurences of a character in a string
        /// </summary>
        /// <param name="s"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        public static int CountChars(string s, char c)
        {
            int res = 0;
            foreach (char ch in s) if (ch == c) res++;
            return res;
        }

        /// <summary>
        /// Parses UT into a string (e.g. "2 d 3 h 15 m 59 s"), hides zero elements
        /// </summary>
        /// <param name="time">Time in seconds</param>
        /// <param name="showSeconds">If false, seconds will be displayed only if time is less than 1 minute; otherwise always</param>
        /// <param name="daysTimeLimit">If time is longer than this number of days, time value will be skipped; -1 to alwys show time</param>
        /// <returns></returns>
        public static string ParseUT(double time, bool showSeconds = true, int daysTimeLimit = -1)
        {
            if (Double.IsNaN(time) || (time == 0)) return "—";
            if (time > KSPUtil.dateTimeFormatter.Year * 10) return "10y+";
            double t = time;
            int y, d, m, h;
            string res = "";
            bool show0 = false;
            if (t >= KSPUtil.dateTimeFormatter.Year)
            {
                y = (int)Math.Floor(t / KSPUtil.dateTimeFormatter.Year);
                t -= y * KSPUtil.dateTimeFormatter.Year;
                res += y + " y ";
                show0 = true;
            }
            if ((t >= KSPUtil.dateTimeFormatter.Day) || (show0 && (t >= 1)))
            {
                d = (int)Math.Floor(t / KSPUtil.dateTimeFormatter.Day);
                t -= d * KSPUtil.dateTimeFormatter.Day;
                res += d + " d ";
                show0 = true;
            }
            if ((daysTimeLimit == -1) || (time < KSPUtil.dateTimeFormatter.Day * daysTimeLimit))
            {
                if ((t >= 3600) || show0)
                {
                    h = (int)Math.Floor(t / 3600);
                    t -= h * 3600;
                    res += h + " h ";
                    show0 = true;
                }
                if ((t >= 60) || show0)
                {
                    m = (int)Math.Floor(t / 60);
                    t -= m * 60;
                    res += m + " m ";
                }
                if ((time < 60) || (showSeconds && (Math.Floor(t) > 0))) res += t.ToString("F0") + " s";
            }
            else if (time < KSPUtil.dateTimeFormatter.Day) res = "0 d";
            return res.TrimEnd();
        }

        public static int UTSeconds(double time)
        {
            if (Double.IsNaN(time) || (time == 0)) return 0;
            double t = time;
            int y, d, m, h;
            string res = "";
            int timesec = 0;
            bool show0 = false;
            if (t >= KSPUtil.dateTimeFormatter.Year)
            {
                y = (int)Math.Floor(t / KSPUtil.dateTimeFormatter.Year);
                t -= y * KSPUtil.dateTimeFormatter.Year;
                //res += y + " y ";
                show0 = true;
            }
            if ((t >= KSPUtil.dateTimeFormatter.Day) || (show0 && (t >= 1)))
            {
                d = (int)Math.Floor(t / KSPUtil.dateTimeFormatter.Day);
                t -= d * KSPUtil.dateTimeFormatter.Day;
                //res += d + " d ";
                show0 = true;
            }
            if ((t >= 3600) || show0)
            {
                h = (int)Math.Floor(t / 3600);
                t -= h * 3600;
                res += h + " h ";
                show0 = true;
                timesec = h * 3600;
            }
            if ((t >= 60) || show0)
            {
                m = (int)Math.Floor(t / 60);
                t -= m * 60;
                res += m + " m ";
                timesec += m * 60;
            }
            if ((time < 60) || ((Math.Floor(t) > 0)))
            {
                res += t.ToString("F0") + " s";
                timesec += (int)t;
            }
            return timesec;
        }

        public static void ShowMessage(string msg, bool unwarpTime)
        {
            KSP.UI.Screens.MessageSystem.Instance.AddMessage(new KSP.UI.Screens.MessageSystem.Message("Duty Roster", KSPUtil.PrintDateCompact(Planetarium.GetUniversalTime(), true) + ": " + msg, KSP.UI.Screens.MessageSystemButton.MessageButtonColor.RED, KSP.UI.Screens.MessageSystemButton.ButtonIcons.ALERT));
            //else ScreenMessages.PostScreenMessage(msg);
            if (unwarpTime) TimeWarp.SetRate(0, false, true);
        }

        public static void ShowMessage(string msg, ProtoCrewMember pcm)
        {
            if ((pcm.rosterStatus == ProtoCrewMember.RosterStatus.Available)) return;
            ShowMessage(msg, pcm.rosterStatus == ProtoCrewMember.RosterStatus.Assigned);
        }

        /// <summary>
        /// Mod-wide random number generator
        /// </summary>
        public static System.Random rand = new System.Random();

        /// <summary>
        /// Log levels:
        /// <list type="bullet">
        /// <item><definition>None: do not log</definition></item>
        /// <item><definition>Error: log only errors</definition></item>
        /// <item><definition>Important: log only errors and important information</definition></item>
        /// <item><definition>Debug: log all information</definition></item>
        /// </list>
        /// </summary>
        public enum LogLevel { None, Error, Important, Debug };

        /// <summary>
        /// Current <see cref="LogLevel"/>: either Debug or Important
        /// </summary>
        public static LogLevel Level => HighLogic.CurrentGame.Parameters.CustomParams<DutyRosterGeneralSettings>().DebugMode ? LogLevel.Debug : LogLevel.Important;

        /// <summary>
        /// Returns true if current logging allows logging of messages at messageLevel
        /// </summary>
        /// <param name="messageLevel"></param>
        /// <returns></returns>
        public static bool IsLogging(LogLevel messageLevel = LogLevel.Debug) => messageLevel <= Level;

        /// <summary>
        /// Write into output_log.txt
        /// </summary>
        /// <param name="message">Text to log</param>
        /// <param name="messageLevel"><see cref="LogLevel"/> of the entry</param>
        public static void Log(string message, LogLevel messageLevel = LogLevel.Debug)
        { if (IsLogging(messageLevel) && (message != "")) Debug.Log("[DutyRoster] " + (messageLevel == LogLevel.Error ? "ERROR: " : "") + message); }

        private Core() { }
    }
}
