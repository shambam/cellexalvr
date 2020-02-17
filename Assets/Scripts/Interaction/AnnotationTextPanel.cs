using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CellexalVR.AnalysisObjects;
using CellexalVR.General;

namespace CellexalVR.Interaction
{
    public class AnnotationTextPanel : MonoBehaviour
    {
        public ReferenceManager referenceManager;

        List<Graph.GraphPoint> graphPoints = new List<Graph.GraphPoint>();
        int myColor = 1;
        int[] otherColor;

        private void Start()
        {

        }

        public void FilList(List<Graph.GraphPoint> graphPts)
        {
            graphPoints = new List<Graph.GraphPoint>(graphPts);
        }


        private void OnTriggerEnter(Collider other)
        {
            //print("trigger entered " + other.gameObject.name);
            if (other.gameObject.name.Equals("ControllerCollider(Clone)"))
            {
                //print("nr of cells: " + graphPoints.Count);
                //this.otherColor = new int[graphPoints.Count];
                //int i = 0;
                foreach (Graph.GraphPoint gp in graphPoints)
                {
                    //print("SET id " + i + " color " + gp.Group);
                    //this.otherColor[i++] = gp.Group;
                    gp.HighlightGraphPoint(true);
                    //gp.ColorSelectionColor( i: this.myColor, outline: false);
                }

            }
        }

        private void OnTriggerExit(Collider other)
        {
            //print("trigger exited " + other.gameObject.name);
            //int i = 0;
            foreach (Graph.GraphPoint gp in graphPoints)
            {
                //print("RESET id " + i + " color " + this.otherColor[i]);
                gp.HighlightGraphPoint(false);
                //gp.ReColour( i: this.otherColor[i++], outline: false);
            }
            
        }

    }
}
