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
        int otherColor;

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
                foreach (Graph.GraphPoint gp in graphPoints)
                {
                    //otherColor = gp.Group;
                    
                    gp.HighlightGraphPoint(true);
                    //gp.ColorSelectionColor( i: myColor, outline: false);
                }

            }
        }

        private void OnTriggerExit(Collider other)
        {
            //print("trigger exited " + other.gameObject.name);
            if (other.gameObject.name.Equals("ControllerCollider(Clone)"))
            {
                foreach (Graph.GraphPoint gp in graphPoints)
                {
                    gp.HighlightGraphPoint(false);
                    //gp.ColorSelectionColor( i: otherColor, outline: false);
                }
            }
        }

    }
}
