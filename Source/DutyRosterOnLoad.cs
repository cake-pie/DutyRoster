using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

//Early Register of Events

namespace DutyRoster
{
    [KSPAddon(KSPAddon.Startup.MainMenu, false)]
    public class LoadGlobals : MonoBehaviour
    {
        public static LoadGlobals Instance;
        //Awake Event - when the DLL is loaded
        public void Awake()
        {
            if (Instance != null)
                return;
            Instance = this;
            DontDestroyOnLoad(this);
        }

        public void Start()
        {
            DRGameEvents.onKerbalOffDuty = new EventData<ProtoCrewMember>("onKerbalOffDuty");
            DRGameEvents.onKerbalOnDuty = new EventData<ProtoCrewMember>("onKerbalOnDuty");
        }

        public void OnDestroy()
        {
            //GameEvents.onGameSceneSwitchRequested.Remove(onGameSceneSwitchRequested);
        }
    }
}