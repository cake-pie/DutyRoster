using System.Collections.Generic;

namespace DutyRoster
{
    /// <summary>
    /// List of all tracked kerbals
    /// </summary>
    public class DutyRosterList : Dictionary<string, DutyRosterStatus>
    {
        /// <summary>
        /// Adds a kerbal to the list, unless already exists
        /// </summary>
        /// <param name="name">Kerbal's name</param>
        public void Add(string name)
        {
            if (ContainsKey(name)) return;
            Core.Log("Registering " + name + ".", Core.LogLevel.Important);
            Add(name, new DutyRosterStatus(name));
        }

        /// <summary>
        /// Adds a kerbal to the list, unless already exists
        /// </summary>
        /// <param name="drs"></param>
        public void Add(DutyRosterStatus drs)
        {
            try { Add(drs.Name, drs); }
            catch (System.ArgumentException) { }
        }

        /// <summary>
        /// Changes name of a registered kerbal and renames the entry
        /// </summary>
        /// <param name="name1"></param>
        /// <param name="name2"></param>
        public void Rename(string name1, string name2)
        {
            Core.Log("DutyRosterList.Rename('" + name1 + "', '" + name2 + "')");
            if (ContainsKey(name1))
            {
                this[name1].Name = name2;
                Add(name2, this[name1]);
                Remove(name1);
            }
            else Core.Log("Could not find '" + name1 + "'.", Core.LogLevel.Error);
        }

        /// <summary>
        /// Scans all trackable kerbals and adds them to the list
        /// </summary>
        public void RegisterKerbals()
        {
            Core.Log("Registering kerbals...");
            KerbalRoster kerbalRoster = HighLogic.fetch.currentGame.CrewRoster;
            RemoveUntrackable();
            List<ProtoCrewMember> list = new List<ProtoCrewMember>(kerbalRoster.Crew);
            list.AddRange(kerbalRoster.Tourist);
            Core.Log(list.Count + " total trackable kerbals.", Core.LogLevel.Important);
            foreach (ProtoCrewMember pcm in list)
                if (Core.IsKerbalTrackable(pcm)) Add(pcm.name);
            Core.Log("DutyRosterList updated: " + Count + " kerbals found.", Core.LogLevel.Important);
        }

        void RemoveUntrackable()
        {
            List<string> toRemove = new List<string>();
            foreach (DutyRosterStatus drs in Values)
            {
                ProtoCrewMember pcm = drs.PCM;
                if (!Core.IsKerbalTrackable(pcm))
                {
                    Core.Log(drs.Name + " is not trackable anymore. Marking for removal.");
                    toRemove.Add(drs.Name);
                }
            }
            foreach (string name in toRemove) Remove(name);
            if (toRemove.Count > 0) Core.Log(toRemove.Count + " kerbal(s) removed from the list.");
        }

        public void Update(double interval)
        {
            RemoveUntrackable();
            foreach (DutyRosterStatus drs in Values) drs.Update(interval);
        }

        /// <summary>
        /// Returns DutyRosterStatus for a given kerbal
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public DutyRosterStatus Find(string name) => ContainsKey(name) ? this[name] : null;

        /// <summary>
        /// Returns DutyRosterStatus for a given kerbal
        /// </summary>
        /// <param name="pcm"></param>
        /// <returns></returns>
        public DutyRosterStatus Find(ProtoCrewMember pcm) => Find(pcm.name);

        /// <summary>
        /// Returns the list of names
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string s = "";
            foreach (string n in Keys) s += n + "\r\n";
            return s.Trim();
        }

        public DutyRosterList() : base(HighLogic.fetch.currentGame.CrewRoster.Count) { }
    }
}
