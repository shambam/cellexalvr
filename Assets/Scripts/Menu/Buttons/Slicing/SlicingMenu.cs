using System;
using CellexalVR.Spatial;
using UnityEngine;

namespace CellexalVR.Menu.Buttons.Slicing
{
    public class SlicingMenu : MonoBehaviour
    {
        public GameObject automaticModeMenu;
        public GameObject manualModeMenu;
        public GameObject freeHandModeMenu;

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
                case SliceMode.Manual:
                    manualModeMenu.SetActive(true);
                    automaticModeMenu.SetActive(false);
                    break;
                case SliceMode.Freehand:
                    freeHandModeMenu.SetActive(true);
                    automaticModeMenu.SetActive(false);
                    manualModeMenu.SetActive(false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(modeToActivate), modeToActivate, null);
            }
        }

        public void SliceGraph()
        {
            switch (currentMode)
            {
                case SliceMode.Automatic:
                    StartCoroutine(graphSlicer.SliceGraph(automatic: true, axis: graphSlicer.slicer.Axis, true));
                    break;
                case SliceMode.Manual:
                    StartCoroutine(graphSlicer.SliceGraph(false, 2, true));
                    break;
                case SliceMode.None:
                    break;
                case SliceMode.Freehand:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}