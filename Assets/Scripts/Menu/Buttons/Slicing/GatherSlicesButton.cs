using CellexalVR.Spatial;

namespace CellexalVR.Menu.Buttons.Slicing
{
    public class GatherSlicesButton : CellexalButton
    {
        protected override string Description => "Gather Slices to Parent";

        public SliceManager sliceManager;

        private void Start()
        {
            if (!sliceManager.slicesActive)
            {
                SetButtonActivated(false);
            }
        }

        public override void Click()
        {
            sliceManager.ActivateSlices(false);

            // TODO: Add multi-user functionality.
        }

    }
}