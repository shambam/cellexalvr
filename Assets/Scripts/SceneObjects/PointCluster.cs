﻿using UnityEngine;
using System.Collections;
using CellexalVR.AnalysisObjects;
using System.Collections.Generic;
using CellexalVR.General;
using Valve.VR;
using Valve.VR.InteractionSystem;

namespace CellexalVR.SceneObjects
{
    public class PointCluster : MonoBehaviour
    {
        public Transform t1, t2, t3;
        public Vector3 fromGraphCentroid, midGraphCentroid, toGraphCentroid;
        public IEnumerable<Graph.GraphPoint> fromPointCluster, midPointCluster, toPointCluster;
        public Color LineColor { get; set; }
        public ReferenceManager referenceManager;
        public LineRenderer lineRenderer;
        public GameObject velocityFromGraph;
        public GameObject velocityMidGraph;
        public GameObject velocityToGraph;
        public int ClusterId { get; set; }


        private Vector3 fromPos, toPos, midPos, firstAnchor, secondAnchor;
        private BoxCollider bcFrom, bcMid, bcTo;
        private bool controllerInside;
        private Vector3[] linePosistions;
        private List<LineBetweenTwoPoints> lines = new List<LineBetweenTwoPoints>();
        private bool linesBundled;
        private GraphBetweenGraphs gbg;

        // Use this for initialization
        private void Start()
        {
            fromPos = t1.TransformPoint(fromGraphCentroid);
            toPos = t2.TransformPoint(toGraphCentroid);
            midPos = t3.TransformPoint(midGraphCentroid);
            bcFrom = gameObject.AddComponent<BoxCollider>();
            bcFrom.size = Vector3.one * 0.025f;
            bcFrom.center = fromPos;
            bcFrom.isTrigger = true;

            bcMid = gameObject.AddComponent<BoxCollider>();
            bcMid.size = Vector3.one * 0.025f;
            bcMid.center = midPos;
            bcMid.isTrigger = true;

            bcTo = gameObject.AddComponent<BoxCollider>();
            bcTo.size = Vector3.one * 0.025f;
            bcTo.center = toPos;
            bcTo.isTrigger = true;

            linesBundled = true;
            gbg = t3.GetComponent<GraphBetweenGraphs>();
        }

        private void OnTriggerEnter(Collider other)
        {
            bool touched = other.CompareTag("Player");
            if (!touched) return;
            Highlight(true);
            referenceManager.multiuserMessageSender.SendMessageHighlightCluster(true, gbg.gameObject.name, ClusterId);
            controllerInside = true;
            gbg.TogglePointClusterColliders(false, gameObject.name);
        }

        private void OnTriggerExit(Collider other)
        {
            bool touched = other.CompareTag("Player");
                           // other.gameObject.name.Equals(controllerCollider);
            if (!touched) return;
            Highlight(false);
            referenceManager.multiuserMessageSender.SendMessageHighlightCluster(false, gbg.gameObject.name, ClusterId);
            controllerInside = false;
            gbg.TogglePointClusterColliders(true, gameObject.name);
        }

        public void Highlight(bool highlight)
        {
            if (highlight)
            {
                lineRenderer.startColor = lineRenderer.endColor = Color.white;
                if (linesBundled)
                {
                    foreach (LineBetweenTwoPoints line in lines)
                    {
                        line.GetComponent<LineRenderer>().startColor =
                            line.GetComponent<LineRenderer>().endColor = Color.white;
                    }
                }

                foreach (Graph.GraphPoint gp in fromPointCluster)
                {
                    gp.HighlightGraphPoint(true);
                }

                foreach (Graph.GraphPoint gp in midPointCluster)
                {
                    gp.HighlightGraphPoint(true);
                }

                foreach (Graph.GraphPoint gp in toPointCluster)
                {
                    gp.HighlightGraphPoint(true);
                }
            }
            else
            {
                foreach (Graph.GraphPoint gp in fromPointCluster)
                {
                    gp.HighlightGraphPoint(false);
                }

                foreach (Graph.GraphPoint gp in midPointCluster)
                {
                    gp.HighlightGraphPoint(false);
                }

                foreach (Graph.GraphPoint gp in toPointCluster)
                {
                    gp.HighlightGraphPoint(false);
                }

                if (linesBundled)
                {
                    foreach (LineBetweenTwoPoints line in lines)
                    {
                        line.GetComponent<LineRenderer>().startColor =
                            line.GetComponent<LineRenderer>().endColor = LineColor;
                    }
                }

                lineRenderer.startColor = lineRenderer.endColor = LineColor;
            }
        }

        private void Update()
        {
            if (controllerInside && Player.instance.rightHand.grabPinchAction.GetStateDown(Player.instance.rightHand.handType))
            {
                Highlight(false);
                referenceManager.multiuserMessageSender.SendMessageHighlightCluster(false, gbg.gameObject.name,
                    ClusterId);
                RemakeLines(fromPointCluster);
                referenceManager.multiuserMessageSender.SendMessageToggleBundle(gbg.gameObject.name, ClusterId);
                controllerInside = false;
            }

            if (t1.hasChanged || t2.hasChanged)
            {
                fromPos = t1.TransformPoint(fromGraphCentroid);
                toPos = t2.TransformPoint(toGraphCentroid);
                midPos = t3.TransformPoint(midGraphCentroid);

                bcFrom.center = fromPos;
                bcMid.center = midPos;
                bcTo.center = toPos;
            }
        }

        public void RemakeLines(IEnumerable<Graph.GraphPoint> cluster)
        {
            Graph from = t1.GetComponent<Graph>();
            Graph to = t2.GetComponent<Graph>();
            if (linesBundled)
            {
                lineRenderer.enabled = false;
                if (lines.Count == 0)
                {
                    foreach (Graph.GraphPoint graphPoint in cluster)
                    {
                        lines.Add(gbg.AddLine(from, to, graphPoint));
                    }
                }
                else
                {
                    foreach (LineBetweenTwoPoints line in lines)
                    {
                        line.GetComponent<LineRenderer>().enabled = true;
                    }
                }

                linesBundled = false;
            }

            else
            {
                foreach (LineBetweenTwoPoints l in lines)
                {
                    l.GetComponent<LineRenderer>().enabled = false;
                }

                lineRenderer.enabled = true;
                //LineBetweenTwoPoints line = mid.AddBundledLine(from, to, cluster);
                //lineRenderer = line.GetComponent<LineRenderer>();
                //line.transform.parent = transform;
                //line.fromGraphCentroid = fromGraphCentroid;
                //line.midGraphCentroid = midGraphCentroid;
                //line.toGraphCentroid = toGraphCentroid;
                //line.fromPointCluster = fromPointCluster;
                //line.midPointCluster = midPointCluster;
                //line.toPointCluster = toPointCluster;

                linesBundled = true;
            }
        }
    }
}