using CellexalVR.AnalysisObjects;
using CellexalVR.General;

namespace CellexalVR.Menu.Buttons.General
{
    ///<summary>
    /// Represents a button used for resetting the color and position of the graphs.
    ///</summary>
    public class ResetGraphColorButton : CellexalButton
    {

        private GraphManager graphManager;

        protected override string Description
        {
            get
            {
                return "Reset Colour Of All Graphs";
            }
        }

        private void Start()
        {
            graphManager = referenceManager.graphManager;
            SetButtonActivated(false);
            CellexalEvents.GraphsLoaded.AddListener(OnGraphsLoaded);
            CellexalEvents.GraphsUnloaded.AddListener(OnGraphsUnloaded);
        }

        public override void Click()
        {
            // CellexalEvents.GraphsResetKeepSelection.Invoke();
            graphManager.ResetGraphsColor();
            referenceManager.multiuserMessageSender.SendMessageResetGraphColor();
        }

        private void OnGraphsLoaded()
        {
            SetButtonActivated(true);
        }

        private void OnGraphsUnloaded()
        {
            SetButtonActivated(false);
        }
    }
}