namespace CellexalVR.Menu.Buttons.Slicing
{
    public class ChangeSliceModeButton : CellexalButton
    {
        public SlicingMenu.SliceMode modeMenuToActivate;
        protected override string Description => "Switch to " + modeMenuToActivate;

        private SlicingMenu slicingMenu;

        private void Awake()
        {
            slicingMenu = GetComponentInParent<SlicingMenu>();
        }
        
        public override void Click()
        {
            slicingMenu.ActivateMode(modeMenuToActivate);
            
            // TODO: Add multi user synch
            
        }
    }
}