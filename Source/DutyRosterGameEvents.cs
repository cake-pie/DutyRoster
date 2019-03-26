using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DutyRoster
{
    class DRGameEvents
    {
        /// <summary>
        /// Fires when Duty Roster changes Kerbal back to regular Trait.
        /// ProtoCrewMember is the Kerbal.
        /// </summary>
        public static EventData<ProtoCrewMember> onKerbalOnDuty; //= new EventData<ProtoCrewMember>("onKerbalOnDuty");
        /// <summary>
        /// Fires when Duty Roster changes Kerbal to Tourist.
        /// ProtoCrewMember is the Kerbal.
        /// </summary>
        public static EventData<ProtoCrewMember> onKerbalOffDuty; //= new EventData<ProtoCrewMember>("onKerbalOffDuty");
    }
}
