
namespace CellexalVR.Menu.Buttons.Slicing
{
    public class SliceGraphButton : CellexalButton
    {
        protected override string Description => "Slice Graph";

        private SlicingMenu slicingMenu;


        private void Start()
        {
            SetButtonActivated(false);
            slicingMenu = GetComponentInParent<SlicingMenu>();
        }

        public override void Click()
        {
            slicingMenu.SliceGraph();

            // TODO: Add multi-user functionality.
        }

        private void TurnOn()
        {
            SetButtonActivated(true);
        }

        private void TurnOff()
        {
            SetButtonActivated(false);
        }
    }
}