﻿using SQLiter;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System;
using VRTK;
using TMPro;
using System.IO;
using CellexalVR.General;
using CellexalVR.Interaction;
using CellexalVR.AnalysisObjects;
using CellexalVR.DesktopUI;
using CellexalVR.Extensions;
using CellexalVR.SceneObjects;

namespace CellexalVR.AnalysisLogic
{

    /// <summary>
    /// This class represent a manager that holds all the cells.
    /// </summary>
    public class CellManager : MonoBehaviour
    {
        #region Properties
        public string[] Attributes { get; set; }
        public string[] Facs { get; set; }
        public string[] Facs_values { get; set; }

        #endregion

        public ReferenceManager referenceManager;
        public VRTK_ControllerActions controllerActions;
        public GameObject lineBetweenTwoGraphPointsPrefab;


        private SQLite database;
        private SteamVR_TrackedObject rightController;
        private PreviousSearchesList previousSearchesList;
        private Dictionary<string, Cell> cells;
        private List<GameObject> lines = new List<GameObject>();
        private GameManager gameManager;
        private SelectionToolCollider selectionToolCollider;
        private SelectionManager selectionManager;
        private GraphManager graphManager;
        //private StatusDisplay statusDisplay;
        //private StatusDisplay statusDisplayHUD;
        //private StatusDisplay statusDisplayFar;
        private int coroutinesWaiting;
        private TextMesh currentFlashedGeneText;
        private GameObject HUD;
        private GameObject FarDisp;
        private TextMeshProUGUI HUDflashInfo;
        private TextMeshProUGUI HUDgroupInfo;
        private TextMeshProUGUI HUDstatus;
        private TextMeshProUGUI FarFlashInfo;
        private TextMeshProUGUI FarGroupInfo;
        private TextMeshProUGUI FarStatus;
        private List<string[]> prunedGenes = new List<string[]>();
        private bool flashingGenes = false;
        private bool loadingFlashingGenes;
        private int[] savedFlashGenesLengths;
        private int coloringInfoStatusId;
        private Dictionary<Cell, int> recolored;
        private Dictionary<Graph.GraphPoint, int> selectionList;

        private void OnValidate()
        {
            if (gameObject.scene.IsValid())
            {
                referenceManager = GameObject.Find("InputReader").GetComponent<ReferenceManager>();
            }
        }


        void Awake()
        {
            cells = new Dictionary<string, Cell>();
        }

        private void Start()
        {
            CellexalEvents.GraphsReset.AddListener(GraphsChanged);
            CellexalEvents.GraphsUnloaded.AddListener(GraphsChanged);

            database = referenceManager.database;
            rightController = referenceManager.rightController;
            previousSearchesList = referenceManager.previousSearchesList;
            gameManager = referenceManager.gameManager;
            selectionManager = referenceManager.selectionManager;
            //statusDisplay = referenceManager.statusDisplay;
            //statusDisplayHUD = referenceManager.statusDisplayHUD;
            //statusDisplayFar = referenceManager.statusDisplayFar;
            selectionToolCollider = referenceManager.selectionToolCollider;
            graphManager = referenceManager.graphManager;
            currentFlashedGeneText = referenceManager.currentFlashedGeneText;
            //HUD = referenceManager.HUD;
            //FarDisp = referenceManager.FarDisplay;
            //HUDflashInfo = referenceManager.HUDFlashInfo;
            //HUDgroupInfo = referenceManager.HUDGroupInfo;
            //FarFlashInfo = referenceManager.FarFlashInfo;
            //FarGroupInfo = referenceManager.FarGroupInfo;
            recolored = new Dictionary<Cell, int>();
            selectionList = new Dictionary<Graph.GraphPoint, int>();
        }

        /// <summary>
        /// Attempts to add a cell to the dictionary
        /// </summary>
        /// <param name="label"> The cell's name </param>
        /// <returns> Returns a reference to the added cell </returns>
        public Cell AddCell(string label)
        {
            if (!cells.ContainsKey(label))
            {
                cells[label] = new Cell(label, graphManager);
            }

            return cells[label];
        }

        /// <summary>
        /// Finds cell and returns it.
        /// </summary>
        /// <param name="label">The label(id) of the cell.</param>
        /// <returns></returns>
        public Cell GetCell(string label)
        {
            return cells[label];
        }

        /// <summary>
        /// Creates a new selection.
        /// </summary>
        /// <param name="graphName"> The graph that the selection originated from. </param>
        /// <param name="cellnames"> An array of all the cell names (the graphpoint labels). </param>
        /// <param name="groups"> An array of all colors that the cells should have. </param>
        /// <param name="groupingColors">Optional parameter, used if a custom color scheme should be used. Maps groups to colors.</param>
        public void CreateNewSelection(string graphName, string[] cellnames, int[] groups, Dictionary<int, Color> groupingColors = null)
        {
            selectionManager.CancelSelection();
            Graph graph = graphManager.FindGraph(graphName);
            if (!graph)
            {
                graph = graphManager.FindGraph("");
            }
            if (groupingColors == null)
            {
                for (int i = 0; i < cellnames.Length; ++i)
                {
                    Cell cell = cells[cellnames[i]];
                    //cell.SetGroup(groups[i], true);
                    selectionManager.AddGraphpointToSelection(graph.points[cellnames[i]], groups[i], false);
                    //graphManager.FindGraphPoint(graphName, cell.Label).SetOutLined(true, groups[i]);
                }
            }
            else
            {
                for (int i = 0; i < cellnames.Length; ++i)
                {
                    Cell cell = cells[cellnames[i]];
                    //cell.SetGroup(groups[i], false);
                    selectionManager.AddGraphpointToSelection(graph.points[cellnames[i]], groups[i], false, groupingColors[groups[i]]);
                    //graphManager.FindGraphPoint(graphName, cell.Label).SetOutLined(true, groupingColors[groups[i]]);
                }
            }
        }



        [ConsoleCommand("cellManager", "colorbygene", "cbg")]
        public void ColorGraphsByGene(string geneName)
        {
            ColorGraphsByGene(geneName, graphManager.GeneExpressionColoringMethod, true);
        }

        /// <summary>
        /// Colors all GraphPoints in all current Graphs based on their expression of a gene.
        /// </summary>
        /// <param name="geneName"> The name of the gene. </param>
        public void ColorGraphsByGene(string geneName, bool triggerEvent = true)
        {
            ColorGraphsByGene(geneName, graphManager.GeneExpressionColoringMethod, triggerEvent);
        }

        /// <summary>
        /// Colors all GraphPoints in all current Graphs based on their expression of a gene.
        /// </summary>
        /// <param name="geneName"> The name of the gene. </param>
        public void ColorGraphsByGene(string geneName, GraphManager.GeneExpressionColoringMethods coloringMethod, bool triggerEvent = true)
        {
            try
            {
                StartCoroutine(QueryDatabase(geneName, coloringMethod, triggerEvent));

            }
            catch (Exception e)
            {
                CellexalLog.Log("Failed to colour by expression - " + e.StackTrace);
                CellexalError.SpawnError("Could not colour by gene expression", "Find stack trace in cellexal log");
            }
            if (rightController.isActiveAndEnabled)
            {
                controllerActions.TriggerHapticPulse(2000, (ushort)600, 0);
            }
            referenceManager.heatmapGenerator.HighLightGene(geneName);
            referenceManager.networkGenerator.HighLightGene(geneName);
        }

        private IEnumerator QueryDatabase(string geneName, GraphManager.GeneExpressionColoringMethods coloringMethod, bool triggerEvent)
        {
            if (coroutinesWaiting >= 1)
            {
                // If there is already another query  waiting for the current to finish we should probably abort.
                // This is just to make sure that a bug can't create many many coroutines that will form a long queue.
                CellexalLog.Log("WARNING: Not querying database for " + geneName + " because there is already a query waiting.");
                yield break;
            }
            coroutinesWaiting++;
            // if there is already a query running, wait for it to finish
            while (database.QueryRunning)
                yield return null;

            coroutinesWaiting--;
            database.QueryGene(geneName, coloringMethod);
            // now we have to wait for our query to return the results.
            while (database.QueryRunning)
                yield return null;

            GetComponent<AudioSource>().Play();
            SteamVR_Controller.Input((int)rightController.index).TriggerHapticPulse(2000);
            ArrayList expressions = database._result;
            // stop the coroutine if the gene was not in the database
            if (expressions.Count == 0)
            {
                CellexalLog.Log("WARNING: The gene " + geneName + " was not found in the database");
                yield break;
            }

            //        Dictionary<string, int> sortedCells = new Dictionary<string, int>();
            //        for (int i = 0; i < expressions.Count; ++i)
            //        {
            //            Cell cell = cells[((CellExpressionPair)expressions[i]).Cell];
            //            //cell.Hide();
            //            cell.ColorByExpression((int)((CellExpressionPair)expressions[i]).Expression);
            //            sortedCells.Add(((CellExpressionPair)expressions[i]).Cell, (int)((CellExpressionPair)expressions[i]).Expression);
            //        }
            //
            //        int n = (int)Math.Round(0.01 * cells.Count);
            //        HighlightTopExpressedCells(sortedCells, n);
            //
            //        yield return new WaitForSeconds(2);
            //
            //        foreach (Cell c in cells.Values)
            //        {
            //            c.Show();
            //        }
            graphManager.ColorAllGraphsByGeneExpression(expressions);

            //float percentInResults = (float)database._result.Count / cells.Values.Count;
            //statusDisplay.RemoveStatus(coloringInfoStatusId);
            //coloringInfoStatusId = statusDisplay.AddStatus(String.Format("Stats for {0}:\nlow: {1:0.####}, high: {2:0.####}, above 0: {3:0.##%}", geneName, database.LowestExpression, database.HighestExpression, percentInResults));

            if (!previousSearchesList.Contains(geneName, Definitions.Measurement.GENE, coloringMethod))
            {
                var removedGene = previousSearchesList.AddEntry(geneName, Definitions.Measurement.GENE, coloringMethod);
                foreach (Cell c in cells.Values)
                {
                    c.SaveExpression(geneName + " " + coloringMethod, removedGene);
                }
            }
            if (triggerEvent)
            {
                CellexalEvents.GraphsColoredByGene.Invoke();
            }
            CellexalLog.Log("Colored " + expressions.Count + " points according to the expression of " + geneName);
        }

        /// <summary>
        /// Queries the database for all genes and sorts them based on the chosen mode.
        /// </summary>
        /// <param name="mode">The chosen mode. <see cref="SQLite.QueryTopGenesRankingMode"/></param>
        public void QueryTopGenes(SQLite.QueryTopGenesRankingMode mode)
        {
            StartCoroutine(QueryTopGenesCoroutine(mode));
        }

        private IEnumerator QueryTopGenesCoroutine(SQLite.QueryTopGenesRankingMode mode)
        {
            CellexalEvents.QueryTopGenesStarted.Invoke();
            while (database.QueryRunning)
            {
                yield return null;
            }

            database.QueryTopGenes(mode);

            while (database.QueryRunning)
            {
                yield return null;
            }
            Pair<string, float>[] results = (Pair<string, float>[])database._result.ToArray(typeof(Pair<string, float>));
            Array.Sort(results, (Pair<string, float> x, Pair<string, float> y) => y.Second.CompareTo(x.Second));
            string[] genes = new string[20];
            float[] values = new float[20];
            if (mode == SQLite.QueryTopGenesRankingMode.Mean)
            {
                for (int i = 0; i < 10; ++i)
                {
                    genes[i] = results[i].First;
                    values[i] = results[i].Second;
                }
                for (int i = 0; i < 10; ++i)
                {
                    genes[i + 10] = results[results.Length - (i + 1)].First;
                    values[i + 10] = results[results.Length - (i + 1)].Second;
                }
            }
            else if (mode == SQLite.QueryTopGenesRankingMode.TTest)
            {
                for (int i = 0; i < 10; ++i)
                {
                    genes[i] = results[i].First;
                    values[i] = results[i].Second;
                }
                for (int i = 0; i < 10; ++i)
                {
                    genes[i + 10] = results[results.Length - (i + 1)].First;
                    values[i + 10] = results[results.Length - (i + 1)].Second;
                }
            }
            CellexalLog.Log("Overwriting file: " + CellexalUser.UserSpecificFolder + "\\gene_expr_diff.txt with new results");
            StreamWriter stream = new StreamWriter(CellexalUser.UserSpecificFolder + "\\gene_expr_diff.txt", false);
            foreach (Pair<string, float> p in results)
            {
                stream.Write(p.First + "\t\t " + p.Second + "\n");
            }
            stream.Flush();
            stream.Close();

            CellexalEvents.QueryTopGenesFinished.Invoke();
            referenceManager.colorByGeneMenu.CreateGeneButtons(genes, values);
        }

        /// <summary>
        /// Used by the database to tell the cellmanager which genes were actually in the database.
        /// </summary>
        /// <param name="genesToAdd"> An array of genes that was in the database. </param>
        public void AddToPrunedGenes(string[] genesToAdd)
        {
            prunedGenes.Add(genesToAdd);
        }

        /// <summary>
        /// Removes all cells.
        /// </summary>
        public void DeleteCells()
        {
            cells.Clear();
            Attributes = null;
            Facs = null;
        }

        /// <summary>
        /// Color all cells that belong to a certain attribute.
        /// </summary>
        /// <param name="attributeType">The name of the attribute.</param>
        /// <param name="color">True if the graphpoints should be colored to the attribute's color, false if they should be white.</param>
        [ConsoleCommand("cellManager", "colorbyattribute", "cba")]
        public void ColorByAttribute(string attributeType, bool color, bool subGraph = false)
        {
            if (!subGraph)
            {
                if (color)
                {
                    referenceManager.attributeSubMenu.attributes.Add(attributeType);
                }
                else
                {
                    referenceManager.attributeSubMenu.attributes.Remove(attributeType);
                }
            }
            CellexalLog.Log("Colored graphs by " + attributeType);
            foreach (Cell cell in cells.Values)
            {
                cell.ColorByAttribute(attributeType, color);
                Graph.GraphPoint gp = cell.GraphPoints[0];
                if (cell.Attributes.ContainsKey(attributeType.ToLower()))
                {
                    if (color && !selectionList.ContainsKey(gp))
                    {
                        selectionList.Add(gp, cell.Attributes[attributeType.ToLower()]);
                    }
                    if (!color)
                    {
                        selectionList.Remove(gp);
                    }
                }
            }

        }

        public void SendToSelection()
        {
            foreach (KeyValuePair<Graph.GraphPoint, int> entry in selectionList)
            {
                selectionManager.AddGraphpointToSelection(entry.Key, entry.Value, false);
            }
        }

        /// <summary>
        /// Color all cells based on an expression of attributes
        /// </summary>
        /// <param name="expr">The root of the tree representing a boolean expression of attributes.</param>
        public void ColorByAttributeExpression(BooleanExpression.Expr expr)
        {
            if (expr == null)
            {
                graphManager.ResetGraphsColor();
            }
            foreach (var cell in cells.Values)
            {
                if (expr.Eval(cell))
                {
                    cell.SetGroup(selectionToolCollider.currentColorIndex, true);
                }
                else
                {
                    if (recolored.ContainsKey(cell))
                        cell.SetGroup(recolored[cell], true);
                    else
                        cell.SetGroup(-1, true);
                }
            }
        }

        public void AddCellsToSelection(BooleanExpression.Expr attributes, int group)
        {
            if (attributes == null)
                return;
            int numAdded = 0;
            foreach (var cell in cells.Values)
            {
                if (attributes.Eval(cell))
                {
                    numAdded++;
                    // more_cells selectionToolHandler.AddGraphpointToSelection(cell.GraphPoints[0], group, false);
                    recolored[cell] = selectionToolCollider.currentColorIndex;
                }
            }
            CellexalLog.Log("Added " + numAdded + " cells to selection");
        }

        /// <summary>
        /// Adds an attribute to a cell. 
        /// </summary>
        /// <param name="cellname"> The cells name. </param>
        /// <param name="attributeType"> The attribute type / name </param>
        /// <param name="group"> The attribute value </param>
        public void AddAttribute(string cellname, string attributeType, int group)
        {
            cells[cellname].AddAttribute(attributeType, group);
        }

        internal void AddFacs(string cellName, string facs, int index)
        {
            if (index < 0 || index >= CellexalConfig.Config.GraphNumberOfExpressionColors)
            {
                // value hasn't been normalized correctly
                print(facs + " " + index);
            }
            cells[cellName].AddFacs(facs, index);
        }

        internal void AddFacsValue(string cellName, string facs, string value)
        {
            cells[cellName].AddFacsValue(facs, value);
        }


        /// <summary>
        /// Color all graphpoints according to a column in the index.facs file.
        /// </summary>
        [ConsoleCommand("cellManager", "colorbyindex", "cbi")]
        public void ColorByIndex(string name)
        {
            if (!previousSearchesList.Contains(name, Definitions.Measurement.FACS, graphManager.GeneExpressionColoringMethod))
                previousSearchesList.AddEntry(name, Definitions.Measurement.FACS, graphManager.GeneExpressionColoringMethod);
            CellexalLog.Log("Colored graphs by " + name);
            foreach (Cell cell in cells.Values)
            {
                cell.ColorByIndex(name);
            }
            CellexalEvents.GraphsColoredByIndex.Invoke();
        }

        /// <summary>
        /// Draws lines between all points that share the same label.
        /// </summary>
        /// <param name="points"> The graphpoints to draw the lines from. </param>
        public void DrawLinesBetweenGraphPoints(List<Graph.GraphPoint> points)
        {
            foreach (Graph.GraphPoint g in points)
            {
                Color color = g.GetColor();
                foreach (Graph.GraphPoint sameCell in cells[g.Label].GraphPoints)
                {
                    if (sameCell != g)
                    {
                        LineBetweenTwoPoints line = Instantiate(lineBetweenTwoGraphPointsPrefab).GetComponent<LineBetweenTwoPoints>();
                        line.graphPoint1 = g;
                        line.graphPoint2 = sameCell;
                        line.selectionManager = selectionManager;
                        LineRenderer lineRenderer = line.GetComponent<LineRenderer>();
                        //lineRenderer.useWorldSpace = false;
                        line.t1 = g.parent.transform;
                        line.t2 = sameCell.parent.transform;
                        lineRenderer.startColor = color;
                        lineRenderer.endColor = color;
                        lines.Add(line.gameObject);
                        sameCell.parent.Lines.Add(line.gameObject);
                        g.parent.Lines.Add(line.gameObject);
                        if (!sameCell.parent.GraphActive)
                        {
                            line.gameObject.SetActive(false);
                        }
                        g.lineBetweenCellsCubes.Add(line.cube);
                    }
                }
            }
            CellexalEvents.LinesBetweenGraphsDrawn.Invoke();
        }

        public void DrawLinesBetweenGraphPoints(List<Graph.GraphPoint> points, Graph fromGraph, Graph toGraph)
        {
            foreach (Graph.GraphPoint g in points)
            {
                Color color = g.GetColor();
                var sourceCell = fromGraph.points[g.Label];
                var targetCell = toGraph.points[g.Label];
                LineBetweenTwoPoints line = Instantiate(lineBetweenTwoGraphPointsPrefab).GetComponent<LineBetweenTwoPoints>();
                line.t1 = targetCell.parent.transform;
                line.t2 = sourceCell.parent.transform;
                line.graphPoint1 = sourceCell;
                line.graphPoint2 = targetCell;
                line.selectionManager = selectionManager;
                LineRenderer lineRenderer = line.GetComponent<LineRenderer>();
                lineRenderer.startColor = color;
                lineRenderer.endColor = color;
                lines.Add(line.gameObject);
                fromGraph.Lines.Add(line.gameObject);
                toGraph.Lines.Add(line.gameObject);
                if (!targetCell.parent.GraphActive)
                {
                    line.gameObject.SetActive(false);
                }
                g.lineBetweenCellsCubes.Add(line.cube);
            }
        }

        /// <summary>
        /// Removes all lines between graphs.
        /// </summary>
        public void ClearLinesBetweenGraphPoints()
        {
            foreach (GameObject line in lines)
            {
                Destroy(line, 0.05f);
                line.GetComponent<LineBetweenTwoPoints>().graphPoint1.lineBetweenCellsCubes.Clear();
            }
            lines.Clear();
            graphManager.ClearLinesBetweenGraphs();
            CellexalEvents.LinesBetweenGraphsCleared.Invoke();
        }

        /// <summary>
        /// Saves a series of expressions that should be flashed.
        /// </summary>
        /// <param name="cell"> The cell that these expressions belong to. </param>
        /// <param name="category"> The expressions' category. </param>
        /// <param name="expr"> An array containing integers int he range [0,29] that denotes the cell's expression of the gene corresponding to that index. </param>
        internal void SaveFlashingExpression(string[] cell, string category, int[][] expr)
        {
            for (int i = 0; i < cell.Length; ++i)
            {
                cells[cell[i]].InitSaveSingleFlashingGenesExpression(category, expr.Length);
            }

            for (int i = 0; i < expr.Length; ++i)
            {
                for (int j = 0; j < expr[i].Length; ++j)
                {
                    cells[cell[j]].SaveSingleFlashingGenesExpression(category, i, expr[i][j]);
                }
            }
        }

        private void GraphsChanged()
        {
            //statusDisplay.RemoveStatus(coloringInfoStatusId);
            recolored.Clear();
            ClearLinesBetweenGraphPoints();


        }

        public List<Cell> SubSet(BooleanExpression.Expr expr)
        {
            List<Cell> result = new List<Cell>();
            foreach (Cell cell in cells.Values)
            {
                if (expr.Eval(cell))
                {
                    result.Add(cell);
                }
            }
            return result;
        }

        [ConsoleCommand("cellManager", "hardmode")]
        public void Hardmode()
        {
            foreach (var g in GameObject.FindObjectsOfType<GameObject>())
            {
                //if (g.name != "Camera (eye)")
                //{
                var added = g.AddComponent<VRTK.Examples.AutoRotation>();
                added.rotAxis = new Vector3(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
                added.degPerSec = UnityEngine.Random.value * 360;
                //}
            }
        }
    }
}