using System.Collections.Generic;
using System;
using UnityEngine;
using CellexalVR.AnalysisObjects;
using CellexalVR.Extensions;

namespace CellexalVR.AnalysisLogic
{

    /// <summary>
    /// Represents one cell. A cell may be present in multiple graphs.
    /// </summary>
    public class Cell
    {
        public List<Graph.GraphPoint> GraphPoints;

        public Dictionary<string, int> Attributes { get; private set; }
        public Dictionary<string, float> Facs { get; private set; }
        public Dictionary<string, string> FacsValue { get; private set; }
        public int ExpressionLevel { get; internal set; }
        public float ExpressionValue { get; set; }
        public string Label { get; set; }

        private GraphManager graphManager;
        private Dictionary<string, int> lastExpressions = new Dictionary<string, int>(16);
        private Dictionary<string, int[]> flashingExpressions = new Dictionary<string, int[]>();
        private Material tempMat;


        /// <summary>
        /// Creates a new cell.
        /// </summary>
        /// <param name="label"> A string that differentiates this cell from other cells. </param>
        /// <param name="graphManager"> The graphmanager that this cell has graphpoints in. </param>
        public Cell(string label, GraphManager graphManager)
        {
            this.graphManager = graphManager;
            this.Label = label;
            GraphPoints = new List<Graph.GraphPoint>();
            Attributes = new Dictionary<string, int>();
            Facs = new Dictionary<string, float>();
            FacsValue = new Dictionary<string, string>();
            tempMat = null;
        }

        /// <summary>
        /// Tell this cell that it is now represented by a graphpoint.
        /// A cell may be represented by many graphpoints (typically one in each graph).
        /// </summary>
        /// <param name="g"> The graphpoint representing this cell. </param>
        public void AddGraphPoint(Graph.GraphPoint g)
        {
            GraphPoints.Add(g);
        }

        /// <summary>
        /// Adds an attribute to this cell.
        /// </summary>
        /// <param name="attributeType"> The type of the attribute. </param>
        /// <param name="color"> The color that should be used for this attribute. This corresponds to an index in <see cref="GraphManager.AttributeMaterials"/>. </param>
        public void AddAttribute(string attributeType, int color)
        {
            Attributes[attributeType.ToLower()] = color;
        }

        /// <summary>
        /// Colors all graphpoints that represents this cell if this cell is of an attribute.
        /// </summary>
        /// <param name="attributeType"> The attribute to color by. </param>
        /// <param name="color"> True if the graphpoints should be colored, false  if they should be white. (True means show this attribute, false means hide basically) </param>
        public void ColorByAttribute(string attributeType, bool color)
        {
            if (Attributes.ContainsKey(attributeType.ToLower()))
            {
                foreach (Graph.GraphPoint g in GraphPoints)
                {
                    if (color)
                    {
                        g.ColorSelectionColor(Attributes[attributeType.ToLower()], false);
                        //g.Material = graphManager.AttributeMaterials[Attributes[attributeType.ToLower()]];
                        //Debug.Log("ADD GROUP - " + Attributes[attributeType.ToLower()]);
                        //graphManager.referenceManager.selectionToolHandler.AddGraphpointToSelection(GraphPoints[0], Attributes[attributeType.ToLower()], false, g.Material.color);
                    }

                    else
                    {
                        //g.Material = graphManager.defaultGraphPointMaterial;
                        g.ResetColor();
                    }
                }
            }
        }

        [Obsolete]
        public bool EvaluateAttributeLogic(Tuple<string, AttributeLogic>[] attributes)
        {
            foreach (var attribute in attributes)
            {
                string attributeName = attribute.Item1.ToLower();
                if (attribute.Item2 == AttributeLogic.YES && !Attributes.ContainsKey(attributeName))
                    return false;
                if (attribute.Item2 == AttributeLogic.NO && Attributes.ContainsKey(attributeName))
                    return false;
            }
            return true;
        }



        /// <summary>
        /// Saves the current gene expression of this cell is colored by
        /// </summary>
        /// <param name="saveGeneName"> The genename to save </param>
        /// <param name="removeGeneName"> The name of a gene to remove or an empty string to not remove anything. Gene expressions can use up quite some memory so only 10 are saved at a time. </param>
        public void SaveExpression(string saveGeneName, string removeGeneName)
        {
            if (removeGeneName != null && removeGeneName != "")
            {
                lastExpressions.Remove(removeGeneName);
            }
            lastExpressions[saveGeneName] = ExpressionLevel;
        }

        /// <summary>
        /// Color all graphpoints that represents this cell by an index.
        /// I don't know enough biology to know what this actually is.
        /// </summary>
        /// <param name="facsName"> The index. </param>
        public void ColorByIndex(string facsName)
        {
            //foreach (Graph.GraphPoint g in GraphPoints)
            //{
            //    g.RecolorGeneExpression(, false);
            //}
        }

        /// <summary>
        /// Colors this cell by a gene expression color.
        /// </summary>
        /// <param name="i">A number between 0 and <see cref="CellexalVR.General.CellexalConfig.Config.GraphNumberOfExpressionColors"/></param>
        public void ColorByGeneExpression(int i)
        {
            foreach (Graph.GraphPoint g in GraphPoints)
            {
                g.ColorGeneExpression(i, false);
            }
        }

        /// <summary>
        /// Adds a .facs bin index to this cell.
        /// </summary>
        /// <param name="facsName"> The thing's name. </param>
        /// <param name="value"> The value of the thing. </param>
        internal void AddFacs(string facsName, float value)
        {
            Facs[facsName.ToLower()] = value;
        }


        /// <summary>
        /// Adds a .facs original value to this cell.
        /// </summary>
        /// <param name="facsName"> The thing's name. </param>
        /// <param name="index"> The value of the thing. </param>
        internal void AddFacsValue(string facsName, string value)
        {
            FacsValue[facsName.ToLower()] = value;
        }

        /// <summary>
        /// Sets the group and color of all graphpoints that are representing this cell.
        /// </summary>
        /// <param name="group"> The new group. </param>
        public void SetGroup(int group, bool changeColor)
        {
            foreach (var g in GraphPoints)
            {
                g.ColorSelectionColor(group, false);
            }
        }


        /// <summary>
        /// Initializes the cell for saving genee expressions for flashing.
        /// Should be called before <see cref="SaveSingleFlashingGenesExpression(string, int, int)"/>
        /// </summary>
        /// <param name="category">The name of a category that should be initialized</param>
        /// <param name="length">The number of genes in that category</param>
        public void InitSaveSingleFlashingGenesExpression(string category, int length)
        {
            flashingExpressions[category] = new int[length];
        }

        /// <summary>
        /// Saves a gene expression that can be flashed later.
        /// </summary>
        /// <param name="category">The name of the category that this gene is in</param>
        /// <param name="index">Which index it should be put on</param>
        /// <param name="expression">A value between 0 and <see cref="CellexalConfig.Config.GraphNumberOfExpressionColors"/></param>
        public void SaveSingleFlashingGenesExpression(string category, int index, int expression)
        {
            flashingExpressions[category][index] = expression;
        }

        /// <summary>
        /// Saves gene expressions so they can be flashed quickly later.
        /// </summary>
        /// <param name="category"> The category the gene expressions are in </param>
        /// <param name="expression"> An array containing indices corresponding to <see cref="GraphManager.GeneExpressionMaterials"/>. </param>
        public void SaveFlashingExpression(string category, int[] expression)
        {
            flashingExpressions[category] = expression;
        }


        /// <summary>
        /// Gets the lengths of each category.
        /// </summary>
        /// <returns> A Dictionary with the categories as keys and their lengths as values. </returns>
        internal Dictionary<string, int> GetCategoryLengths()
        {
            Dictionary<string, int> lengths = new Dictionary<string, int>();
            foreach (KeyValuePair<string, int[]> pair in flashingExpressions)
            {
                lengths[pair.Key] = pair.Value.Length;
            }
            return lengths;
        }

        /// <summary>
        /// Clears the saved flashing expressions.
        /// </summary>
        public void ClearFlashingExpressions()
        {
            flashingExpressions.Clear();
        }
    }
}