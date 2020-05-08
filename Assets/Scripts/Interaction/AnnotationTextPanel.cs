using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CellexalVR.AnalysisLogic;
using CellexalVR.AnalysisObjects;
using CellexalVR.General;

namespace CellexalVR.Interaction
{
    public class AnnotationTextPanel : MonoBehaviour
    {
        public ReferenceManager referenceManager;

        private List<Cell> cells;


        private int myColor = 1;
        private int[] otherColor;
        

        public void SetCells(List<Cell> cells)
        {
            this.cells = new List<Cell>(cells);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.name.Equals("ControllerCollider(Clone)"))
            {
                referenceManager.cellManager.HighlightCells(cells.ToArray(), true);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            referenceManager.cellManager.HighlightCells(cells.ToArray(), false);
        }
    }
}