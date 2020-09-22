using CellexalVR.General;
using CellexalVR.Menu.Buttons;
using CellexalVR.Menu.Buttons.Slicing;
using CellexalVR.SceneObjects;
using UnityEngine;

namespace CellexalVR.Spatial
{
    public class ToggleAutoSliceAxisButton : SliderButton
    {
        public int axis;

        private SlicingMenu slicingMenu;

        protected override string Description { get; }

        protected override void Start()
        {
            base.Start();
            slicingMenu = GetComponentInParent<SlicingMenu>();
        }

        protected override void ActionsAfterSliding()
        {
            if (currentState)
            {
                slicingMenu.SetSliceAxis(axis);
            }

            for (int i = 0; i < slicingMenu.axisButtons.Length; i++)
            {
                if (i == axis) continue;
                ToggleAutoSliceAxisButton button = slicingMenu.axisButtons[i];
                if (button.CurrentState) button.CurrentState = !currentState;
            }
        }

        public void TurnOn()
        {
            SetButtonActivated(true);
        }

        public void TurnOff()
        {
            SetButtonActivated(false);
        }
    }
}