using CellexalVR.Filters;
using CellexalVR.General;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CellexalVR.AnalysisObjects
{
    /// <summary>
    /// Represents a legend that contains <see cref="GroupingLegendEntry"/>.
    /// </summary>
    public class GroupingLegend : MonoBehaviour
    {
        public ReferenceManager referenceManager;
        public GameObject entryPrefab;
        public GameObject extraColumn;

        private List<GroupingLegendEntry> entries = new List<GroupingLegendEntry>();
        private int activeCells = 0;
        private bool attached;

        private Vector3 startPos = new Vector3(0f, 0.1968f, 0f);
        private Vector3 posInc = new Vector3(0f, -0.0416f, 0f);

        private void OnValidate()
        {
            if (gameObject.scene.IsValid())
            {
                referenceManager = GameObject.Find("InputReader").GetComponent<ReferenceManager>();
            }
        }

        private void Start()
        {
            CellexalEvents.LegendAttached.AddListener(ActivateExtraColumn);
            CellexalEvents.LegendDetached.AddListener(DeActivateExtraColumn);
            CellexalEvents.GraphsReset.AddListener(ClearLegend);
            CellexalEvents.GraphsColoredByGene.AddListener(ClearLegend);
            CellexalEvents.GraphsColoredByIndex.AddListener(ClearLegend);
        }

        private void ActivateExtraColumn()
        {
            extraColumn.SetActive(true);
            GroupingLegendEntry groupingLegendEntry = entryPrefab.GetComponent<GroupingLegendEntry>();
            AdjustEntry(groupingLegendEntry, true);

            foreach (GroupingLegendEntry entry in entries)
            {
                AdjustEntry(entry, true);
            }
            attached = true;
        }
        private void DeActivateExtraColumn()
        {
            extraColumn.SetActive(false);
            GroupingLegendEntry groupingLegendEntry = entryPrefab.GetComponent<GroupingLegendEntry>();
            AdjustEntry(groupingLegendEntry, false);

            foreach (GroupingLegendEntry entry in entries)
            {
                AdjustEntry(entry, false);
            }
            attached = false;
        }

        private void AdjustEntry(GroupingLegendEntry entry, bool toggle)
        {
            Vector3 scale = entry.topDivider.transform.localScale;
            scale.x = toggle ? 0.064f : 0.057f;
            entry.topDivider.transform.localScale = scale;
            Vector3 pos = entry.topDivider.transform.localPosition;
            pos.x = toggle ? 0.03f : 0f;
            entry.topDivider.transform.localPosition = pos;
            entry.filterButton.GetComponent<AttributeFilterButton>().toggle = false;
            entry.filterButton.GetComponent<AttributeFilterButton>().ToggleOutline(false);
            entry.filterButton.SetActive(toggle);
        }

        /// <summary>
        /// Adds a new group to the legend.
        /// </summary>
        /// <param name="groupName">The name of the group.</param>
        /// <param name="numberOfCells">The number of cells the group has.</param>
        /// <param name="color">The color that represents the group.</param>
        public void AddGroup(string groupName, int numberOfCells, Color color)
        {
            GameObject newEntryGameObject = Instantiate(entryPrefab);
            newEntryGameObject.SetActive(true);
            newEntryGameObject.transform.parent = transform;
            newEntryGameObject.transform.localPosition = startPos + posInc * entries.Count;
            newEntryGameObject.transform.localRotation = Quaternion.identity;
            GroupingLegendEntry newEntry = newEntryGameObject.GetComponent<GroupingLegendEntry>();
            newEntry.filterButton.SetActive(attached);
            newEntry.filterButton.GetComponent<AttributeFilterButton>().group = groupName;
            activeCells += numberOfCells;

            string percentOfSelectedString = ((float)numberOfCells / activeCells).ToString("P");
            string percentOfAllString = ((float)numberOfCells / referenceManager.cellManager.GetNumberOfCells()).ToString("P");
            newEntry.SetPanelText(groupName, numberOfCells, percentOfSelectedString, percentOfAllString, color);
            UpdatePercentages();
            newEntry.transform.localScale = Vector3.one;
            entries.Add(newEntry);
        }

        /// <summary>
        /// Removes a group from the legend.
        /// </summary>
        /// <param name="groupName">The name of the group to remove.</param>
        public void RemoveGroup(string groupName)
        {
            int index = entries.FindIndex((item) => item.groupName.text == groupName);
            if (index != -1)
            {
                GroupingLegendEntry entry = entries[index];
                entries.RemoveAt(index);
                activeCells -= entry.numberOfCells;
                Destroy(entry.gameObject);
                UpdatePercentages();
                UpdatePositions();
            }
        }

        private void ClearLegend()
        {
            foreach (GroupingLegendEntry entry in entries)
            {
                Destroy(entry.gameObject);
            }
            entries.Clear();
        }

        /// <summary>
        /// Updates the positions of all groups. Should be called when a group is removed.
        /// </summary>
        private void UpdatePositions()
        {
            for (int i = 0; i < entries.Count; ++i)
            {
                entries[i].transform.localPosition = startPos + i * posInc;
            }
        }

        /// <summary>
        /// Updates the percentages of all groups. Should be called when group is added or updated.
        /// </summary>
        private void UpdatePercentages()
        {
            foreach (GroupingLegendEntry remainingEntry in entries)
            {
                int numberOfCells = remainingEntry.numberOfCells;
                remainingEntry.numberOfCellsText.text = numberOfCells.ToString();
                string percentOfSelectedString = ((float)numberOfCells / activeCells).ToString("P");
                string percentOfAllString = ((float)numberOfCells / referenceManager.cellManager.GetNumberOfCells()).ToString("P");
                remainingEntry.UpdatePercentages(percentOfSelectedString, percentOfAllString);
            }
        }

        /// <summary>
        /// Updates an existing group or, if no group with the specified name exists, creates a new group.
        /// </summary>
        /// <param name="groupName">The name of the group.</param>
        /// <param name="numberOfCellsToAdd">The number of cells to add or remove from the group.</param>
        /// <param name="color">The color the group should have if it is created. If <paramref name="numberOfCellsToAdd"/> is negative (cells are removed from the group), this parameter does not matter.</param>
        public void AddOrUpdateGroup(string groupName, int numberOfCellsToAdd, Color color)
        {
            GroupingLegendEntry foundEntry = entries.FirstOrDefault((entry) => entry.groupName.text == groupName);
            if (foundEntry)
            {
                foundEntry.numberOfCells += numberOfCellsToAdd;
                if (foundEntry.numberOfCells == 0)
                {
                    RemoveGroup(groupName);
                }
                UpdatePercentages();
            }
            else
            {
                AddGroup(groupName, numberOfCellsToAdd, color);
                //if (attached)
                //{
                //    referenceManager.cullingFilterManager.AddSelectionGroupToFilter(groupName);
                //}
            }

            activeCells += numberOfCellsToAdd;
        }
    }
}
