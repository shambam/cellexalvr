using CellexalVR.AnalysisObjects;

namespace CellexalVR.Menu.Buttons.Networks
{
    /// <summary>
    /// Represents the button that chooses a network on the toggle arcs menu.
    /// </summary>
    public class ArcsTabButton : TabButton
    {
        [UnityEngine.HideInInspector]
        public NetworkHandler Handler;

        public override void SetHighlighted(bool highlight)
        {
            base.SetHighlighted(highlight);
            Handler.HighlightNetworkSkeleton(highlight);
        }
        
        
    }
}