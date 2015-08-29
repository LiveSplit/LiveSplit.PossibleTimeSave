using LiveSplit.Model;
using System;

namespace LiveSplit.UI.Components
{
    public class PossibleTimeSaveFactory : IComponentFactory
    {
        public string ComponentName
        {
            get { return "Possible Time Save"; }
        }

        public string Description
        {
            get { return "Displays the difference between a comparison segment and the best segment, effectively showing how much time can be saved."; }
        }

        public ComponentCategory Category
        {
            get { return ComponentCategory.Information; }
        }

        public IComponent Create(LiveSplitState state)
        {
            return new PossibleTimeSave(state);
        }

        public string UpdateName
        {
            get { return ComponentName; }
        }

        public string XMLURL
        {
#if RELEASE_CANDIDATE
            get { return "http://livesplit.org/update_rc_sdhjdop/Components/update.LiveSplit.PossibleTimeSave.xml"; }
#else
            get { return "http://livesplit.org/update/Components/update.LiveSplit.PossibleTimeSave.xml"; }
#endif
        }

        public string UpdateURL
        {
#if RELEASE_CANDIDATE
            get { return "http://livesplit.org/update_rc_sdhjdop/"; }
#else
            get { return "http://livesplit.org/update/"; }
#endif
        }

        public Version Version
        {
            get { return Version.Parse("1.6"); }
        }
    }
}
