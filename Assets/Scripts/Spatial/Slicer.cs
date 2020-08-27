using System.Collections;
using System.Collections.Generic;
using CellexalVR.AnalysisObjects;
using UnityEngine;

namespace CellexalVR.Spatial
{
    public class Slicer : MonoBehaviour
    {
        public Transform t1, t2, t3, t4, t5, t6;

        private LineRenderer lr;

        private Graph graph;

        private void Start()
        {
            graph = GetComponentInParent<Graph>();
            lr = GetComponent<LineRenderer>();
            lr.useWorldSpace = false;
            Vector3 midPoint = graph.maxCoordValues + graph.minCoordValues;;
            t1.localPosition = new Vector3(midPoint.x, 0.5f, midPoint.z);
            t2.localPosition = new Vector3(midPoint.x, -0.5f, midPoint.z);
            lr.SetPositions(new Vector3[] {t1.localPosition, t2.localPosition});
        }

        // Update is called once per frame
        private void Update()
        {
            if (t1.hasChanged || t2.hasChanged)
            {
                lr.SetPositions(new Vector3[] {t1.localPosition, t2.localPosition});
            }

            if (t3.hasChanged || t4.hasChanged || t5.hasChanged || t6.hasChanged)
            {
            }
        }
    }
}