using CellexalVR.AnalysisObjects;
using CellexalVR.General;

namespace CellexalVR.Menu.Buttons
{
    public class ToggleTransparencyButton : CellexalButton
    {
        private GraphManager graphManager;
        public bool Toggle { get; set; }

        protected override string Description
        {
            get
            {
                return "Toggle transparency of all graph points";
            }
        }

        public override void Click()
        {
            referenceManager.multiuserMessageSender.SendMessageToggleTransparency(!Toggle);
            graphManager.ToggleGraphPointTransparency(!Toggle);
            Toggle = !Toggle;
        }

        // Use this for initialization
        void Start()
        {
            graphManager = referenceManager.graphManager;
        }

    }

}
