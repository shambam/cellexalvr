using UnityEngine;
using System.Collections;
using TMPro;
namespace CellexalVR.AnalysisObjects
{
    /// <summary>
    /// Represents an entry in <see cref="GroupingLegend"/>.
    /// </summary>
    public class GroupingLegendEntry : MonoBehaviour
    {
        public GameObject colorSquare;
        public TextMeshPro groupName;
        public TextMeshPro numberOfCellsText;
        public TextMeshPro percentOfSelectedText;
        public TextMeshPro percentOfAllText;
        public GameObject topDivider;
        public GameObject filterButton;
        [HideInInspector]
        public int numberOfCells;

        /// <summary>
        /// Sets all text fields in this entry.
        /// </summary>
        /// <param name="groupName">The name of the group.</param>
        /// <param name="numberOfCells">The number of cells the group contains.</param>
        /// <param name="percentOfSelected">The percent of all selected groups.</param>
        /// <param name="percentOfAll">The percent of all cells in the dataset.</param>
        /// <param name="color">The color of the group.</param>
        public void SetPanelText(string groupName, int numberOfCells, string percentOfSelected, string percentOfAll, Color color)
        {
            this.groupName.text = groupName;
            numberOfCellsText.text = numberOfCells.ToString();
            percentOfSelectedText.text = percentOfSelected;
            percentOfAllText.text = percentOfAll;
            colorSquare.GetComponent<MeshRenderer>().material.color = color;
            this.numberOfCells = numberOfCells;
        }

        /// <summary>
        /// Updates the percentages of this group.
        /// </summary>
        /// <param name="percentOfSelected">The percent of all selected groups.</param>
        /// <param name="percentOfAll">The percent of all cells in the dataset.</param>
        public void UpdatePercentages(string percentOfSelected, string percentOfAll)
        {
            percentOfSelectedText.text = percentOfSelected;
            percentOfAllText.text = percentOfAll;
        }

    }
}
