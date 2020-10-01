using System;
using System.Linq;
using CellexalVR.General;
using CellexalVR.Spatial;
using Spatial;
using UnityEngine;

namespace CellexalVR.Menu.Buttons.Slicing
{
    public class SlicingMenu : MonoBehaviour
    {
        public GameObject automaticModeMenu;
        public GameObject manualModeMenu;
        public GameObject freeHandModeMenu;
        public ToggleAutoSliceAxisButton[] axisButtons = new ToggleAutoSliceAxisButton[3];


        public enum SliceMode
        {
            None,
            Automatic,
            Manual,
            Freehand,
        }

        private SliceMode currentMode;
        private GraphSlicer graphSlicer;

        private void Start()
        {
            ActivateMode(SliceMode.Automatic);
            graphSlicer = GetComponentInParent<GraphSlicer>();
        }

        public void ActivateMode(SliceMode modeToActivate)
        {
            currentMode = modeToActivate;
            switch (modeToActivate)
            {
                case SliceMode.None:
                    automaticModeMenu.SetActive(false);
                    manualModeMenu.SetActive(false);
                    break;
                case SliceMode.Automatic:
                    automaticModeMenu.SetActive(true);
                    manualModeMenu.SetActive(false);
                    break;
                case SliceMode.Freehand:
                case SliceMode.Manual:
                    manualModeMenu.SetActive(true);
                    automaticModeMenu.SetActive(false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(modeToActivate), modeToActivate, null);
            }
        }

        public void SetSliceAxis(int axis)
        {
            if (currentMode != SliceMode.Automatic) return;
            graphSlicer.slicer.Axis = axis;

            if (axisButtons.All(x => !x.CurrentState))
            {
                graphSlicer.slicer.Axis = -1;
                GetComponentInChildren<SliceGraphButton>(true).SetButtonActivated(false);
                graphSlicer.slicer.plane.SetActive(false);
            }
            
            else
            {
                GetComponentInChildren<SliceGraphButton>(true).SetButtonActivated(true);
                graphSlicer.slicer.ActivatePlane(axis);
            }
        }


        public void SliceGraph()
        {
            switch (currentMode)
            {
                case SliceMode.Automatic:
                    StartCoroutine(graphSlicer.SliceGraph(currentMode, axis: graphSlicer.slicer.Axis, true));
                    break;
                case SliceMode.Manual:
                    StartCoroutine(graphSlicer.SliceGraph(currentMode, 2, true));
                    break;
                case SliceMode.Freehand:
                    StartCoroutine(graphSlicer.SliceGraph(currentMode, 2, true));
                    break;
                case SliceMode.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}