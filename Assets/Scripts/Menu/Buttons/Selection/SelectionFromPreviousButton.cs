using System.Collections.Generic;
using CellexalVR.General;
using TMPro;
using UnityEngine;

namespace CellexalVR.Menu.Buttons.Selection
{
    /// <summary>
    /// Represents the buttons that are used to create new selections from a previously made selection.
    /// </summary>
    public class SelectionFromPreviousButton : CellexalButton
    {
        public TextMeshPro buttonDescription;
        public string Path { get; set; }
        private string graphName;
        private string[] selectionCellNames;
        private int[] selectionGroups;
        private Dictionary<int, Color> groupingColors;
        private bool toggle;

        protected override string Description => "Create a selection from " + Path;


        private void Start()
        {
            rightController = referenceManager.rightController;
            CellexalEvents.SelectionCanceled.AddListener(ResetButton);
            CellexalEvents.SelectionStarted.AddListener(ResetButton);
            CellexalEvents.SelectedFromFile.AddListener(ResetButton);
        }

        public override void Click()
        {
            bool tempToggle = toggle;
            if (toggle)
            {
                referenceManager.selectionManager.CancelSelection();
            }
            else
            {
                referenceManager.inputReader.ReadSelectionFile(Path);
            }

            ToggleOutline(!tempToggle);
            toggle = !tempToggle;
        }

        private void ResetButton()
        {
            toggle = false;
            ToggleOutline(false);
        }

        /// <summary>
        /// Set which selection this button represents.
        /// </summary>
        /// <param name="graphName"> Which graph the selection originated from. </param>
        /// <param name="selectionName"> The name of this selection. </param>
        /// <param name="selectionCellNames"> An array containing the cell names. </param>
        /// <param name="selectionGroups"> An array containing which groups the cells belonged to. </param>
        public void SetSelection(string graphName, string selectionName, string[] selectionCellNames,
            int[] selectionGroups, Dictionary<int, Color> groupingColors)
        {
            buttonDescription.text = selectionName;
            this.graphName = graphName;
            this.selectionCellNames = selectionCellNames;
            this.selectionGroups = selectionGroups;
            this.groupingColors = groupingColors;
        }
    }
}