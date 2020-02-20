using CellexalVR.AnalysisLogic;
using CellexalVR.AnalysisObjects;
using CellexalVR.Filters;
using CellexalVR.General;
using UnityEngine;

namespace Assets.Scripts.SceneObjects
{
    public class CullingCube : MonoBehaviour
    {
        public ReferenceManager referenceManager;
        public int boxNr;
        public GameObject attachOnSideArea;

        private Material material;
        private FilterManager filterManager;

        private void OnValidate()
        {
            if (gameObject.scene.IsValid())
            {
                referenceManager = GameObject.Find("InputReader").GetComponent<ReferenceManager>();
            }
        }

        private void Start()
        {
            referenceManager = GameObject.Find("InputReader").GetComponent<ReferenceManager>();
            filterManager = referenceManager.filterManager;
        }

        private void Update()
        {
            if (!referenceManager.graphGenerator.isCreating)
            {
                foreach (Graph graph in referenceManager.graphManager.Graphs)
                {
                    material = graph.graphPointClusters[0].GetComponent<Renderer>().sharedMaterial;
                    if (boxNr == 1)
                    {
                        material.SetMatrix("_BoxMatrix", transform.worldToLocalMatrix);
                    }
                    else
                    {
                        material.SetMatrix("_BoxMatrix2", transform.worldToLocalMatrix);
                    }
                }
            }
        }

        public void InverseCulling(bool invert)
        {
            float value = invert ? -1 : 1;
            foreach (Graph graph in referenceManager.graphManager.Graphs)
            {
                material = graph.graphPointClusters[0].GetComponent<Renderer>().sharedMaterial;
                material.SetFloat("_Culling", value);
            }
        }

        public void ActivateFilter()
        {
            if (filterManager.currentFilter == null)
                return;
            Graph g = referenceManager.graphManager.originalGraphs.Find(x => !x.GraphName.Contains("Slice"));
            foreach (Cell c in referenceManager.cellManager.GetCells())
            {
                Graph.GraphPoint gp = g.FindGraphPoint(c.Label);
                filterManager.ActivateCullingFilter();
                referenceManager.filterManager.AddCellToEval(gp, referenceManager.selectionToolCollider.currentColorIndex);

            }
        }
    }
}

