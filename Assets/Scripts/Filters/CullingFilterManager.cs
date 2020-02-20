using UnityEngine;
using System.Collections;
using CellexalVR.General;
using System.Collections.Generic;
using CellexalVR.AnalysisLogic;
using CellexalVR.AnalysisObjects;
using System;
using Assets.Scripts.SceneObjects;

namespace CellexalVR.Filters
{
    /// <summary>
    /// Class represents the filter that is attached to the culling cube. 
    /// It works similar to the regular filter but does not add the filter to the selection tool.
    /// Instead it activates it on the culling cube. This is mainly useful for more simple filters.
    /// For more complex filters use the filter creator.
    /// </summary>
    public class CullingFilterManager : MonoBehaviour
    {

        public ReferenceManager referenceManager;
        public Filter currentFilter;
        public GameObject cullingCubePrefab;
        public int cubeCounter;

        // Key.Item1 is a gene name, Key.Item2 is a cell name, Value is the expression
        public Dictionary<Tuple<string, string>, float> GeneExprs { get; set; }

        private string geneName = string.Empty;
        private LegendManager legendManager;

        private void OnValidate()
        {
            if (gameObject.scene.IsValid())
            {
                referenceManager = GameObject.Find("InputReader").GetComponent<ReferenceManager>();
            }
        }

        private void Start()
        {
            GeneExprs = new Dictionary<Tuple<string, string>, float>(new TupleComparer());
            currentFilter = new Filter();
            legendManager = referenceManager.legendManager;
            CellexalEvents.LegendDetached.AddListener(DeActivateFilter);
        }

        /// <summary>
        /// Removes the latest added culling cube.
        /// </summary>
        public void RemoveCube()
        {
            GameObject cubeToDestroy = GameObject.Find("CullingCube" + cubeCounter);
            Destroy(cubeToDestroy);
            cubeCounter--;
            CellexalEvents.CullingCubeRemoved.Invoke();
        }

        /// <summary>
        /// Adds one more culling cube. A maximum of two can be added.
        /// </summary>
        public void AddCube()
        {
            GameObject cube = Instantiate(cullingCubePrefab);
            cubeCounter++;
            cube.GetComponent<CullingCube>().boxNr = cubeCounter;
            cube.gameObject.name = "CullingCube" + cubeCounter;
            CellexalEvents.CullingCubeSpawned.Invoke();
        }

        /// <summary>
        /// If something has changed on the filter update it. Either new attribute has been added/removed or gene filter has changed etc.
        /// </summary>
        private void UpdateCullingFilter()
        {
            foreach (Cell c in referenceManager.cellManager.GetCells())
            {
                if (!geneName.Equals(string.Empty))
                    GeneExprs[new Tuple<string, string>(geneName, c.Label)] = c.ExpressionValue;
                foreach (Graph g in referenceManager.graphManager.Graphs)
                {
                    Graph.GraphPoint otherGp = g.FindGraphPoint(c.Label);
                    if (otherGp != null)
                    {
                        bool pass = (currentFilter.Expression != null) ? currentFilter.Pass(c) : false;
                        g.MakePointUnCullable(otherGp, pass);

                    }
                }
            }
        }

        private void DeActivateFilter()
        {
            currentFilter.Expression = null;
            legendManager.geneExpressionHistogram.filterTextLabel.text = "";
            UpdateCullingFilter();
        }

        /// <summary>
        /// Add attribute to the filter. All added attributes are linked together with or statements meaning it will pass points that have any of the attributes.
        /// Adding attribute to the filter means the points belonging to this attribute will NOT be hid.
        /// </summary>
        /// <param name="attribute"></param>
        public void AddAttributeToFilter(string attribute)
        {
            if (currentFilter.Expression == null)
            {
                currentFilter.Expression = new BooleanExpression.AttributeExpr(attribute, true);
            }
            else
            {
                BooleanExpression.OrExpr newExpr = new BooleanExpression.OrExpr(currentFilter.Expression, new BooleanExpression.AttributeExpr(attribute, true));
                currentFilter.Expression = newExpr;
            }
            UpdateCullingFilter();
        }

        /// <summary>
        /// Remove attribute from the filter. 
        /// </summary>
        /// <param name="attribute"></param>
        public void RemoveAttributeFromFilter(string attribute)
        {
            List<string> currentAttributes = new List<string>();
            currentFilter.Expression.GetAttributes(ref currentAttributes);
            if (currentAttributes.Count == 1)
            {
                currentFilter.Expression = null;
            }
            else if (currentAttributes.Count >= 2)
            {
                currentAttributes.Remove(attribute.ToLower());
                currentFilter.Expression = new BooleanExpression.AttributeExpr(currentAttributes[0], true);
                for (int i = 1; i < currentAttributes.Count; i++)
                {
                    BooleanExpression.OrExpr newExpr = new BooleanExpression.OrExpr(currentFilter.Expression, new BooleanExpression.AttributeExpr(currentAttributes[i], true));
                    currentFilter.Expression = newExpr;
                }

            }
            UpdateCullingFilter();
        }

        public void AddSelectionGroupToFilter(int group)
        {
            if (currentFilter.Expression == null)
            {
                currentFilter.Expression = new BooleanExpression.SelectionGroupExpr(group, true);
            }
            else
            {
                BooleanExpression.OrExpr newExpr = new BooleanExpression.OrExpr(currentFilter.Expression, new BooleanExpression.SelectionGroupExpr(group, true));
                currentFilter.Expression = newExpr;
            }
            UpdateCullingFilter();
        }

        /// <summary>
        /// Remove attribute from the filter. 
        /// </summary>
        /// <param name="group"></param>
        public void RemoveGroupFromFilter(int group)
        {
            List<int> currentGroups = new List<int>();
            currentFilter.Expression.GetGroups(ref currentGroups);
            if (currentGroups.Count == 1)
            {
                currentFilter.Expression = null;
            }
            else if (currentGroups.Count >= 2)
            {
                currentGroups.Remove(group);
                currentFilter.Expression = new BooleanExpression.SelectionGroupExpr(currentGroups[0], true);
                for (int i = 1; i < currentGroups.Count; i++)
                {
                    BooleanExpression.OrExpr newExpr = new BooleanExpression.OrExpr(currentFilter.Expression, new BooleanExpression.SelectionGroupExpr(currentGroups[i], true));
                    currentFilter.Expression = newExpr;
                }

            }
            UpdateCullingFilter();
        }


        public void AddGeneFilter(string gene, int startX, int endX, float highestGeneValue)
        {
            this.geneName = gene.ToLower();
            BooleanExpression.Token greaterToken = new BooleanExpression.Token(BooleanExpression.Token.Type.OP_GTEQ, ">=", startX);
            BooleanExpression.Token lessToken = new BooleanExpression.Token(BooleanExpression.Token.Type.OP_LTEQ, "<=", endX);
            float lowValue = (highestGeneValue / 30) * startX;
            float highValue = (highestGeneValue / 30) * endX;
            BooleanExpression.GeneExpr lowExpr = new BooleanExpression.GeneExpr(gene, greaterToken, lowValue, false);
            lowExpr.SetCullingFilterManager(this);
            BooleanExpression.GeneExpr highExpr = new BooleanExpression.GeneExpr(gene, lessToken, highValue, false);
            highExpr.SetCullingFilterManager(this);
            BooleanExpression.AndExpr andExpr = new BooleanExpression.AndExpr(lowExpr, highExpr);
            currentFilter.Expression = andExpr;
            legendManager.geneExpressionHistogram.filterTextLabel.text = currentFilter.Expression.ToString().Trim(new char[] { '(', ')' });
            UpdateCullingFilter();
        }


        /// <summary>
        /// Helper class to compare tuples of strings.
        /// </summary>
        private class TupleComparer : IEqualityComparer<Tuple<string, string>>
        {
            public bool Equals(Tuple<string, string> x, Tuple<string, string> y)
            {
                return x.Item1.Equals(y.Item1) && x.Item2.Equals(y.Item2);
            }

            public int GetHashCode(Tuple<string, string> obj)
            {
                return obj.Item1.GetHashCode() ^ obj.Item2.GetHashCode();
            }
        }
    }
}