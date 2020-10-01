using CellexalVR.Spatial;

namespace CellexalVR.Menu.Buttons.Slicing
{
    public class GatherSlicesButton : CellexalButton
    {
        public bool toggle;
        public SliceManager sliceManager;
        protected override string Description => toggle ? "Disperse" : "Gather" + " split slices together";

        private void Start()
        {
            if (!sliceManager.slicesActive)
            {
                SetButtonActivated(false);
            }
        }

        public override void Click()
        {
            sliceManager.ActivateSlices(toggle);
            // TODO: Add multi-user functionality.
        }

    }
}