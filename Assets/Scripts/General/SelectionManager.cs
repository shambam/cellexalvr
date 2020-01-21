using CellexalVR.AnalysisLogic;
using CellexalVR.AnalysisObjects;
using CellexalVR.DesktopUI;
using CellexalVR.Extensions;
using CellexalVR.Filters;
using CellexalVR.Interaction;
using CellexalVR.Menu.SubMenus;
using CellexalVR.Multiuser;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using TMPro;
using UnityEngine;

namespace CellexalVR.General
{

    /// <summary>
    /// Represents the selection tool that can be used to select multiple GraphPoints.
    /// </summary>
    public class SelectionManager : MonoBehaviour
    {
        public ReferenceManager referenceManager;
        public GameObject annotationTextPrefab;
        //public GroupInfoDisplay groupInfoDisplay;
        //public GroupInfoDisplay HUDGroupInfoDisplay;
        //public GroupInfoDisplay FarGroupInfoDisplay;
        [HideInInspector]
        public bool selectionConfirmed = false;
        [HideInInspector]
        public bool heatmapGrabbed = false;
        [HideInInspector]
        public int fileCreationCtr = 0;
        public ushort hapticIntensity = 2000;
        public int groupCount;

        private SelectionFromPreviousMenu previousSelectionMenu;
        private ControllerModelSwitcher controllerModelSwitcher;
        private GraphManager graphManager;
        private SteamVR_TrackedObject rightController;
        private SteamVR_Controller.Device device;
        private List<Graph.GraphPoint> selectedCells = new List<Graph.GraphPoint>();
        private List<Graph.GraphPoint> lastSelectedCells = new List<Graph.GraphPoint>();
        private List<Tuple<string, string>> annotatedPoints = new List<Tuple<string, string>>();
        private Dictionary<string, List<string>> cellTags = new Dictionary<string, List<string>>();
        private bool selectionMade = false;
        private GameObject grabbedObject;
        private bool heatmapCreated = true;
        private SelectionToolCollider selectionToolCollider;
        private int annotationCtr = 0;

        [HideInInspector]
        public string DataDir { get; set; }
        private List<HistoryListInfo> selectionHistory = new List<HistoryListInfo>();
        // the number of steps we have taken back in the history.
        private int historyIndexOffset;
        private MultiuserMessageSender multiuserMessageSender;
        private FilterManager filterManager;

        public bool RObjectUpdating { get; private set; }

        /// <summary>
        /// Helper struct for remembering history when selecting graphpoints.
        /// </summary>
        private struct HistoryListInfo
        {
            // the graphpoint this affects
            public Graph.GraphPoint graphPoint;
            // the group it was given, -1 means no group
            public int toGroup;
            // the group it had before, -1 means no group
            public int fromGroup;
            // true if this graphpoint was previously not in the list of selected graphpoints
            public bool newNode;

            public HistoryListInfo(Graph.GraphPoint graphPoint, int toGroup, int fromGroup, bool newNode)
            {
                this.graphPoint = graphPoint;
                this.toGroup = toGroup;
                this.fromGroup = fromGroup;
                this.newNode = newNode;
            }
        }

        private void OnValidate()
        {
            if (gameObject.scene.IsValid())
            {
                referenceManager = GameObject.Find("InputReader").GetComponent<ReferenceManager>();
            }
        }

        void Awake()
        {
            previousSelectionMenu = referenceManager.selectionFromPreviousMenu;
            graphManager = referenceManager.graphManager;
        }

        private void Start()
        {
            controllerModelSwitcher = referenceManager.controllerModelSwitcher;
            rightController = referenceManager.rightController;
            multiuserMessageSender = referenceManager.multiuserMessageSender;
            selectionToolCollider = referenceManager.selectionToolCollider;
            filterManager = referenceManager.filterManager;
            //CellexalEvents.GraphsColoredByGene.AddListener(Clear);
            //CellexalEvents.GraphsColoredByIndex.AddListener(Clear);
            CellexalEvents.GraphsReset.AddListener(Clear);
        }

        /// <summary>
        /// Adds a graphpoint to the current selection, and changes its color to the current color of the selection tool.
        /// This method is called by a child object that holds the collider.
        /// </summary>
        public void AddGraphpointToSelection(Graph.GraphPoint graphPoint)
        {
            AddGraphpointToSelection(graphPoint, selectionToolCollider.currentColorIndex, true, selectionToolCollider.Colors[selectionToolCollider.currentColorIndex]);
            multiuserMessageSender.SendMessageSelectedAdd(graphPoint.parent.GraphName, graphPoint.Label, selectionToolCollider.currentColorIndex, selectionToolCollider.Colors[selectionToolCollider.currentColorIndex]);
        }

        /// <summary>
        /// Adds a graphpoint to the current selection, and changes its color.
        /// </summary>
        public void AddGraphpointToSelection(Graph.GraphPoint graphPoint, int newGroup, bool hapticFeedback)
        {
            if (filterManager.currentFilter != null)
            {
                referenceManager.filterManager.AddCellToEval(graphPoint, newGroup);
            }
            else
            {
                AddGraphpointToSelection(graphPoint, newGroup, true, selectionToolCollider.Colors[newGroup]);
                multiuserMessageSender.SendMessageSelectedAdd(graphPoint.parent.GraphName, graphPoint.Label, newGroup, selectionToolCollider.Colors[newGroup]);
            }

            //Debug.Log("Adding gp to sel. Inform clients.");
        }

        /// <summary>
        /// Adds a graphpoint to the current selection, and changes its color.
        /// </summary>
        public void AddGraphpointToSelection(Graph.GraphPoint graphPoint, int newGroup, bool hapticFeedback, Color color)
        {
            if (graphPoint == null)
            {
                return;
            }
            var parentGraph = graphPoint.parent;
            if (parentGraph.tag.Equals("Untagged"))
            {
                var ctcGraph = graphPoint.parent.GetComponent<GraphBetweenGraphs>();
                graphPoint = ctcGraph.graph1.FindGraphPoint(graphPoint.Label);
            }
            // more_cells if (CurrentFilter != null && !CurrentFilter.Pass(graphPoint)) return;

            int oldGroup = graphPoint.Group;

            foreach (Graph graph in graphManager.Graphs)
            {
                Graph.GraphPoint gp = graphManager.FindGraphPoint(graph.GraphName, graphPoint.Label);
                if (gp != null)
                {
                    gp.ColorSelectionColor( newGroup, true);
                }
            }
            //graphPoint.Recolor(Colors[newGroup], newGroup);
            graphPoint.Group = newGroup;
            // renderer.material.color = Colors[newGroup];
            //multiuserMessageSender.SendMessageGraphPointChangedColor(graphPoint.GraphName, graphPoint.Label, color);

            bool newNode = !graphPoint.unconfirmedInSelection;
            graphPoint.unconfirmedInSelection = true;
            if (historyIndexOffset != 0)
            {
                // if we have undone some selected graphpoints, then they should be removed from the history
                selectionHistory.RemoveRange(selectionHistory.Count - historyIndexOffset, historyIndexOffset);
                historyIndexOffset = 0;
                // turn off the redo buttons
                CellexalEvents.EndOfHistoryReached.Invoke();
            }
            if (!selectionMade)
            {
                // if this is a new selection we should reset some stuff
                selectionMade = true;
                //selectionToolMenu.SelectionStarted();
                //try
                //{
                //    groupInfoDisplay.ResetGroupsInfo();
                //    HUDGroupInfoDisplay.ResetGroupsInfo();
                //    FarGroupInfoDisplay.ResetGroupsInfo();
                //}
                //catch (NullReferenceException e)
                //{
                //Debug.Log("Could not update group info display");
                //}
                // turn on the undo buttons
                CellexalEvents.BeginningOfHistoryLeft.Invoke();
            }
            // The user might select cells that already have that color
            //if (newGroup != oldGroup || newNode)
            //{
            selectionHistory.Add(new HistoryListInfo(graphPoint, newGroup, oldGroup, newNode));

            referenceManager.legendManager.desiredLegend = LegendManager.Legend.SelectionLegend;

            referenceManager.legendManager.selectionLegend.AddOrUpdateGroup(newGroup.ToString(), 1, color);
            if (!newNode)
            {
                referenceManager.legendManager.selectionLegend.AddOrUpdateGroup(oldGroup.ToString(), -1, Color.white);
            }

            //try
            //{
            //    groupInfoDisplay.ChangeGroupsInfo(newGroup, 1);
            //    HUDGroupInfoDisplay.ChangeGroupsInfo(newGroup, 1);
            //    FarGroupInfoDisplay.ChangeGroupsInfo(newGroup, 1);
            //}
            //catch (NullReferenceException e)
            //{
            //Debug.Log("Tried to change infodisplays but could not. Perhaps none available..");
            //}

            if (newNode)
            {
                //multiuserMessageSender.SendMessageSelectedAdd(graphPoint.GraphName, graphPoint.Label);
                if (selectedCells.Count == 0)
                {
                    CellexalEvents.SelectionStarted.Invoke();
                }
                selectedCells.Add(graphPoint);
            }
            else
            {
                // If graphPoint was reselected. Remove it and add again so it is moved to the end of the list.
                selectedCells.Remove(graphPoint);
                selectedCells.Add(graphPoint);
                //try
                //{
                //    groupInfoDisplay.ChangeGroupsInfo(oldGroup, -1);
                //    HUDGroupInfoDisplay.ChangeGroupsInfo(oldGroup, -1);
                //    FarGroupInfoDisplay.ChangeGroupsInfo(oldGroup, -1);
                //}
                //catch (NullReferenceException e)
                //{
                //Debug.Log("Tried to change infodisplays but could not. Perhaps none available..");
                //}
            }
            if (hapticFeedback && selectionToolCollider.hapticFeedbackThisFrame)
            {
                selectionToolCollider.hapticFeedbackThisFrame = false;
                SteamVR_Controller.Input((int)rightController.index).TriggerHapticPulse(hapticIntensity);
            }
            //}
        }
        /// <summary>
        /// If selecting from client then graphpoint to be added needs to be found by searching since it has not collided with selection tool.
        /// </summary>
        /// <param name="graphName">The name of the graph that the point is in.</param>
        /// <param name="label">The label of the cell (graphpoint)</param>
        /// <param name="newGroup">The group which the cell is to be added to. </param>
        /// <param name="color">Colour that the graphpoint should be coloured by.</param>
        public void DoClientSelectAdd(string graphName, string label, int newGroup, Color color)
        {
            Graph.GraphPoint gp = referenceManager.graphManager.FindGraphPoint(graphName, label);
            AddGraphpointToSelection(gp, newGroup, true, color);
        }

        /// <summary>
        /// Same as above but this function is called when coluring the cubes between same cells in different graphs.
        /// </summary>
        /// <param name="graphName"></param>
        /// <param name="label"></param>
        /// <param name="newGroup"></param>
        /// <param name="color"></param>
        /// <param name="cube">If a cube should be coloured as well as the graphpoint.</param>
        public void DoClientSelectAdd(string graphName, string label, int newGroup, Color color, bool cube)
        {
            Graph.GraphPoint gp = referenceManager.graphManager.FindGraphPoint(graphName, label);
            AddGraphpointToSelection(gp, newGroup, true, color);
            foreach (Selectable sel in gp.lineBetweenCellsCubes)
            {
                sel.GetComponent<Renderer>().material.color = color;
            }
        }


        /// <summary>
        /// Goes back one step in the history of selecting cells.
        /// </summary>
        public void GoBackOneStepInHistory()
        {
            if (historyIndexOffset == 0)
            {
                CellexalEvents.EndOfHistoryLeft.Invoke();
            }

            int indexToMoveTo = selectionHistory.Count - historyIndexOffset - 1;
            if (indexToMoveTo == 0)
            {
                // beginning of history reached
                CellexalEvents.BeginningOfHistoryReached.Invoke();
                //selectionToolMenu.UndoSelection();
            }
            else if (indexToMoveTo < 0)
            {
                // no more history
                return;
            }
            HistoryListInfo info = selectionHistory[indexToMoveTo];
            //info.graphPoint.Group = info.fromGroup;

            // if info.fromGroup != 1 then the outline should be drawn
            foreach (Graph graph in graphManager.Graphs)
            {
                Graph.GraphPoint gp = graphManager.FindGraphPoint(graph.GraphName, info.graphPoint.Label);
                gp.ColorSelectionColor(info.fromGroup, !info.newNode);
            }

            //groupInfoDisplay.ChangeGroupsInfo(info.toGroup, -1);
            //HUDGroupInfoDisplay.ChangeGroupsInfo(info.toGroup, -1);
            //FarGroupInfoDisplay.ChangeGroupsInfo(info.toGroup, -1);
            if (info.newNode)
            {
                selectedCells.Remove(info.graphPoint);
                info.graphPoint.unconfirmedInSelection = false;
                //info.graphPoint.ResetColor();
            }
            else
            {
                //groupInfoDisplay.ChangeGroupsInfo(info.fromGroup, 1);
                //HUDGroupInfoDisplay.ChangeGroupsInfo(info.fromGroup, 1);
                //FarGroupInfoDisplay.ChangeGroupsInfo(info.fromGroup, 1);
                //info.graphPoint.ResetColor();
            }
            historyIndexOffset++;
            selectionMade = false;
        }

        /// <summary>
        /// Go forward one step in the history of selecting cells.
        /// </summary>
        public void GoForwardOneStepInHistory()
        {
            if (historyIndexOffset == selectionHistory.Count)
            {
                CellexalEvents.BeginningOfHistoryLeft.Invoke();
                //selectionToolMenu.SelectionStarted();
            }

            int indexToMoveTo = selectionHistory.Count - historyIndexOffset;
            if (indexToMoveTo == selectionHistory.Count - 1)
            {
                // end of history reached
                CellexalEvents.EndOfHistoryReached.Invoke();
            }
            else if (indexToMoveTo >= selectionHistory.Count)
            {
                // no more history
                return;
            }

            HistoryListInfo info = selectionHistory[indexToMoveTo];
            //info.graphPoint.Group = info.toGroup;
            foreach (Graph graph in graphManager.Graphs)
            {
                Graph.GraphPoint gp = graphManager.FindGraphPoint(graph.GraphName, info.graphPoint.Label);
                gp.ColorSelectionColor(info.toGroup, info.toGroup != -1);
            }
            //groupInfoDisplay.ChangeGroupsInfo(info.toGroup, 1);
            //HUDGroupInfoDisplay.ChangeGroupsInfo(info.toGroup, 1);
            //FarGroupInfoDisplay.ChangeGroupsInfo(info.toGroup, 1);
            if (info.newNode)
            {
                selectedCells.Add(info.graphPoint);
                info.graphPoint.unconfirmedInSelection = true;
            }
            else
            {
                //groupInfoDisplay.ChangeGroupsInfo(info.fromGroup, -1);
                //HUDGroupInfoDisplay.ChangeGroupsInfo(info.fromGroup, -1);
                //FarGroupInfoDisplay.ChangeGroupsInfo(info.fromGroup, -1);
            }
            historyIndexOffset--;
            selectionMade = false;
        }

        /// <summary>
        /// Go back in history until the color changes. This unselects all the last cells that have the same color.
        /// </summary>
        /// <example>
        /// If the user selects 2 cells as red then 3 cells as blue and then 4 cells as red, in that order, the 4 last red cells would be unselected when calling this method. 
        /// </example>
        public void GoBackOneColorInHistory()
        {
            int indexToMoveTo = selectionHistory.Count - historyIndexOffset - 1;
            int group = selectionHistory[indexToMoveTo].toGroup;
            Color color = group != -1 ? selectionToolCollider.Colors[group] : Color.white;
            Color nextColor;
            do
            {
                GoBackOneStepInHistory();
                indexToMoveTo--;
                if (indexToMoveTo >= 0)
                {
                    nextColor = selectionToolCollider.Colors[selectionHistory[indexToMoveTo].toGroup];
                }
                else
                {
                    break;
                }
            } while (color.Equals(nextColor));
        }

        /// <summary>
        /// Go forward in history until the color changes. This re-selects all the last cells that have the same color.
        /// </summary>
        public void GoForwardOneColorInHistory()
        {
            int indexToMoveTo = selectionHistory.Count - historyIndexOffset;
            int group = selectionHistory[indexToMoveTo].toGroup;
            Color color = group != -1 ? selectionToolCollider.Colors[group] : Color.white;
            Color nextColor;
            do
            {
                GoForwardOneStepInHistory();
                indexToMoveTo++;
                if (indexToMoveTo < selectionHistory.Count)
                {
                    nextColor = selectionToolCollider.Colors[selectionHistory[indexToMoveTo].toGroup];
                }
                else
                {
                    break;
                }
            } while (color.Equals(nextColor));
        }

        // more_cells   public void SingleSelect(Collider other)
        // more_cells   {
        // more_cells       Color transparentColor = new Color(selectedColor.r, selectedColor.g, selectedColor.b);
        // more_cells       other.gameObject.GetComponent<Renderer>().material.color = transparentColor;
        // more_cells       GraphPoint gp = other.GetComponent<CombinedGraph.CombinedGraphPoint>();
        // more_cells       if (!selectedCells.Contains(gp))
        // more_cells       {
        // more_cells           selectedCells.Add(gp);
        // more_cells       }
        // more_cells       if (!selectionMade)
        // more_cells       {
        // more_cells           selectionMade = true;
        // more_cells           //UpdateButtonIcons();
        // more_cells       }
        // more_cells   }

        /// <summary>
        /// Adds rigidbody to all selected cells, making them fall to the ground.
        /// </summary>
        // more_cells   public void ConfirmRemove()
        // more_cells   {
        // more_cells       //GetComponent<AudioSource>().Play();
        // more_cells       foreach (GraphPoint other in selectedCells)
        // more_cells       {
        // more_cells           other.transform.parent = null;
        // more_cells           other.gameObject.AddComponent<Rigidbody>();
        // more_cells           other.GetComponent<Rigidbody>().useGravity = true;
        // more_cells           other.GetComponent<Rigidbody>().isKinematic = false;
        // more_cells           other.GetComponent<Collider>().isTrigger = false;
        // more_cells           other.SetOutLined(false, -1);
        // more_cells       }
        // more_cells       selectionHistory.Clear();
        // more_cells       CellexalEvents.SelectionCanceled.Invoke();
        // more_cells       selectedCells.Clear();
        // more_cells       selectionMade = false;
        // more_cells       //selectionToolMenu.RemoveSelection();
        // more_cells   }

        /// <summary>
        /// Confirms a selection and dumps the relevant data to a .txt file.
        /// </summary>
        [ConsoleCommand("selectionManager", aliases: new string[] { "confirmselection", "confirm" })]
        public void ConfirmSelection()
        {
            foreach (Graph graph in graphManager.Graphs)
            {
                graph.octreeRoot.Group = -1;
            }

            if (selectedCells.Count == 0)
            {
                print("empty selection confirmed");
            }
            // create .txt file with latest selection
            DumpSelectionToTextFile();
            lastSelectedCells.Clear();
            IEnumerable<Graph.GraphPoint> uniqueCells = selectedCells.Reverse<Graph.GraphPoint>().Distinct().Reverse();
            List<int> groups = new List<int>();
            foreach (Graph.GraphPoint gp in uniqueCells)
            {
                if (!groups.Contains(gp.Group))
                {
                    groups.Add(gp.Group);
                }

                //if (gp.CustomColor)
                //    gp.SetOutLined(false, gp.Material.color);
                //else
                //    gp.SetOutLined(false, gp.CurrentGroup);
                //gp.RecolorSelectionColor(gp.group, false);
                foreach (Graph graph in graphManager.Graphs)
                {
                    Graph.GraphPoint graphPoint = graphManager.FindGraphPoint(graph.GraphName, gp.Label);
                    if (graphPoint != null)
                    {
                        graphPoint.ColorSelectionColor(gp.Group, false);
                    }
                }
                lastSelectedCells.Add(gp);
                gp.unconfirmedInSelection = false;
            }
            groupCount = groups.Count;
            //previousSelectionMenu.CreateButton(selectedCells);
            // clear the list since we are done with it
            selectedCells.Clear();
            selectionHistory.Clear();
            historyIndexOffset = 0;
            CellexalEvents.SelectionConfirmed.Invoke();
            heatmapCreated = false;
            selectionMade = false;
            selectionConfirmed = true;
            //selectionToolMenu.ConfirmSelection();
            CellexalEvents.CommandFinished.Invoke(true);
        }

        private IEnumerator UpdateRObjectGrouping()
        {
            RObjectUpdating = true;
            // wait one frame to let ConfirmSelection finish.
            yield return null;
            //string function = "userGrouping";
            string latestSelection = (CellexalUser.UserSpecificFolder + "\\selection"
                                        + (fileCreationCtr - 1) + ".txt").UnFixFilePath();
            string args = CellexalUser.UserSpecificFolder.UnFixFilePath() + " " + latestSelection;
            string rScriptFilePath = (Application.streamingAssetsPath + @"\R\update_grouping.R").FixFilePath();
            // Wait for server to start up and not be busy
            bool rServerReady = File.Exists(CellexalUser.UserSpecificFolder + "\\mainServer.pid") &&
                    !File.Exists(CellexalUser.UserSpecificFolder + "\\mainServer.input.R") &&
                    !File.Exists(CellexalUser.UserSpecificFolder + "\\mainServer.input.lock");
            while (!rServerReady || !RScriptRunner.serverIdle)
            {
                rServerReady = File.Exists(CellexalUser.UserSpecificFolder + "\\mainServer.pid") &&
                    !File.Exists(CellexalUser.UserSpecificFolder + "\\mainServer.input.R") &&
                    !File.Exists(CellexalUser.UserSpecificFolder + "\\mainServer.input.lock");
                yield return null;
            }
            Thread t = new Thread(() => RScriptRunner.RunRScript(rScriptFilePath, args));
            CellexalLog.Log("Updating R Object grouping at " + CellexalUser.UserSpecificFolder);
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            t.Start();
            while (t.IsAlive || File.Exists(CellexalUser.UserSpecificFolder + "\\mainServer.input.R"))
            {
                yield return null;
            }

            stopwatch.Stop();
            CellexalLog.Log("Updating grouping R script finished in " + stopwatch.Elapsed.ToString());
            RObjectUpdating = false;
        }

        /// <summary>
        /// Selects all points in to current group. If filter is active it queues all points for evaluation. 
        /// </summary>
        public void SelectAll()
        {
            Graph g = graphManager.Graphs[0];
            foreach (Cell c in referenceManager.cellManager.GetCells())
            {
                Graph.GraphPoint gp = g.FindGraphPoint(c.Label);
                if (filterManager.currentFilter != null)
                {
                    referenceManager.filterManager.AddCellToEval(gp, selectionToolCollider.currentColorIndex);
                }
                else
                {
                    AddGraphpointToSelection(gp, selectionToolCollider.currentColorIndex, false);
                }

            }
        }

        /// <summary>
        /// Gets the last selection that was confirmed.
        /// </summary>
        /// <returns> A List of all graphpoints that were selected. </returns>
        public List<Graph.GraphPoint> GetLastSelection()
        {
            return lastSelectedCells;
        }

        /// <summary>
        /// Removes all points from the current selection, and clears the selection history.
        /// </summary>
        public void Clear()
        {
            lastSelectedCells.Clear();
            selectionHistory.Clear();
            selectedCells.Clear();
            historyIndexOffset = 0;
        }

        /// <summary>
        /// Get the current (not yet confirmed) selection.
        /// </summary>
        /// <returns> A List of all graphpoints currently selected. </returns>
        public List<Graph.GraphPoint> GetCurrentSelection()
        {
            return selectedCells;
        }

        private List<Graph.GraphPoint> GetCurrentSelectionGroup(int index)
        {
            return selectedCells.FindAll(gp => gp.Group == index);
        }

        /// <summary>
        /// Unselects anything selected.
        /// </summary>
        [ConsoleCommand("selectionManager", aliases: new string[] { "cancelselection", "cs" })]
        public void CancelSelection()
        {
            int stepsToGoBack = selectionHistory.Count - historyIndexOffset;
            for (int i = 0; i <= stepsToGoBack; i++)
            {
                GoBackOneStepInHistory();
            }
            //foreach (Graph.GraphPoint other in selectedCells)
            //{
            //    other.ResetColor();
            //}

            //CellexalEvents.SelectionCanceled.Invoke();
            historyIndexOffset = selectionHistory.Count;
            CellexalEvents.BeginningOfHistoryReached.Invoke();
            //selectedCells.Clear();
            selectionMade = false;
            CellexalEvents.CommandFinished.Invoke(true);
        }

        /// <summary>
        /// Dumps the current selection to a .txt file.
        /// </summary>
        public void DumpSelectionToTextFile()
        {
            DumpSelectionToTextFile(selectedCells);
        }


        /// <summary>
        /// Dumps the confirmed selection to a text file in the output folder.
        /// The file contains the cell id and the colour of the selection group and which graph it was selected from.
        /// </summary>
        /// <param name="selection"></param>
        public void DumpSelectionToTextFile(List<Graph.GraphPoint> selection, string filePath = "")
        {
            if (filePath != "")
            {
                string savedSelectionsPath = CellexalUser.UserSpecificFolder + @"\SavedSelections\";
                if (!Directory.Exists(savedSelectionsPath))
                {
                    Directory.CreateDirectory(savedSelectionsPath);
                }
                filePath = savedSelectionsPath + filePath + ".txt";
            }
            else
            {
                filePath = CellexalUser.UserSpecificFolder + "\\selection" + (fileCreationCtr++) + ".txt";
                using (StreamWriter file = new StreamWriter(filePath))
                {
                    CellexalLog.Log("Dumping selection data to " + CellexalLog.FixFilePath(filePath));
                    CellexalLog.Log("\tSelection consists of  " + selection.Count + " points");
                    if (selectionHistory != null)
                        CellexalLog.Log("\tThere are " + selectionHistory.Count + " entries in the history");
                    foreach (Graph.GraphPoint gp in selection)
                    {
                        file.Write(gp.Label);
                        file.Write("\t");
                        Color c = gp.GetColor();
                        int r = (int)(c.r * 255);
                        int g = (int)(c.g * 255);
                        int b = (int)(c.b * 255);
                        // writes the color as #RRGGBB where RR, GG and BB are hexadecimal values
                        file.Write(string.Format("#{0:X2}{1:X2}{2:X2}", r, g, b));
                        file.Write("\t");
                        file.Write(gp.parent.GraphName);
                        file.Write("\t");
                        file.Write(gp.Group);
                        file.WriteLine();
                    }
                }
                StartCoroutine(UpdateRObjectGrouping());
            }
        }

        /// <summary>
        /// Helper function to recolour only the points that are in the current selection.
        /// </summary>
        public void RecolorSelectionPoints()
        {
            foreach (Graph.GraphPoint gp in selectedCells)
            {
                foreach (Graph.GraphPoint g in referenceManager.cellManager.GetCell(gp.Label).GraphPoints)
                {
                    g.ColorSelectionColor(gp.Group, true);
                }
            }
        }

        /// <summary>
        /// Adds the annotation to the currently selected group of cells.
        /// Also adds a line and text object above the cells with the annotation.
        /// Touching these texts in VR will highlight the annotated cells.
        /// </summary>
        /// <param name="annotation">The text that will be shown above the cells and later written to file.</param>
        /// <param name="index">The index of the currect selected group to tag.</param>
        public void AddAnnotation(string annotation, int index)
        {
            List<Graph.GraphPoint> pointsToAnnotate = GetCurrentSelectionGroup(index);
            List<string> cellNames = new List<string>();

            foreach (Graph.GraphPoint gp in pointsToAnnotate)
            {
              //  annotatedPoints.Add( new Tuple<string, string>( gp.Label, annotation) );
                cellNames.Add(gp.Label);
            }
            RecolorSelectionPoints();
            AnnotateCells ( cellNames, annotation );
            SaveAnnotationTag( annotation );
        }

        /// <summary>
        /// Based on a list of cell names and an annotation string this function creates a name tag
        /// attached to the first selected cell. This name tag highlights the attached cells when touched by an controller.
        /// </summary>
        /// <param name="annotation">The text that will be shown above the cells and later written to file.</param>
        public void AnnotateCells( List<string> cellNames, string annotation ){

            int id = 0;
            List<Cell> cells = new List<Cell>();

            if (cellTags.ContainsKey(annotation))
            {
                cellTags.Remove(annotation);
            }
            cellTags.Add(annotation, cellNames);

            foreach( string cellName in cellNames ){
                cells.Add( referenceManager.cellManager.GetCell( cellName ) );
            }

            foreach (Graph graph in referenceManager.graphManager.originalGraphs)
            {
                GameObject annotationText = Instantiate(annotationTextPrefab, graph.annotationsParent.transform);
                Vector3 position = graph.FindGraphPoint(cells[0].Label).Position;
                annotationText.transform.localPosition = position;
                annotationText.GetComponentInChildren<TextMeshPro>().text = annotation;
                List<Graph.GraphPoint> pointsHere = new List<Graph.GraphPoint>();
                // get all graph points for this graph and add them to this AnnotationTextPanel
                foreach ( Cell c in cells) {
                    pointsHere.Add( c.GraphPoints[id] );
                }
                annotationText.GetComponent<AnnotationTextPanel>().FilList( pointsHere );
                id = id+1;
            }

        }

        /// <summary>
        /// saves this annotation tag in a file 
        /// CellexalUser.UserSpecificFolder + @"\TaggedCells\" + tag + '.txt'
        /// replacing old annotation tag saves.
        /// <param name="tag">The tag to save</param>
        public void SaveAnnotationTag( string tag ) {
            if (cellTags.ContainsKey(tag)){
                string savedSelectionsPath = CellexalUser.UserSpecificFolder + @"\TaggedCells\";
                if (!Directory.Exists(savedSelectionsPath))
                {
                    Directory.CreateDirectory(savedSelectionsPath);
                }
                string filePath = savedSelectionsPath + tag + ".txt";
                if ( File.Exists(filePath) )
                {
                    File.Delete(filePath);
                }
                using (StreamWriter file = new StreamWriter(filePath, true))
                {
                    file.Write(tag);
                    file.WriteLine();
                    foreach (string cName in cellTags[tag])
                    {
                        file.Write(cName);
                        file.WriteLine();
                    }
                }
            }
        }

        /// <summary>
        /// Writes the cells that have added annotation to a text file. 
        /// Only cell id and annotation is written to the file.
        /// </summary>
        /// <param name="selection">The selected graphpoints</param>
        /// <param name="filePath"></param>
        /// <param name="annotation">The string to write next to cell id as annotation.</param>
        public void DumpAnnotatedSelectionToTextFile(string filePath = "")
        {
            if (filePath != "")
            {
                string savedSelectionsPath = CellexalUser.UserSpecificFolder + @"\SavedSelections\";
                if (!Directory.Exists(savedSelectionsPath))
                {
                    Directory.CreateDirectory(savedSelectionsPath);
                }
                filePath = savedSelectionsPath + filePath + ".txt";
            }
            else
            {
                string annotationDirectory = CellexalUser.UserSpecificFolder + @"\AnnotatedSelections";
                if (!Directory.Exists(annotationDirectory))
                {
                    CellexalLog.Log("Creating directory " + annotationDirectory.FixFilePath());
                    Directory.CreateDirectory(annotationDirectory);
                }
                filePath = annotationDirectory + "\\annotated_selection" + annotationCtr + ".txt";
                if (File.Exists(filePath))
                {
                    annotationCtr++;
                    filePath = annotationDirectory + "\\annotated_selection" + annotationCtr + ".txt";
                }
                using (StreamWriter file = new StreamWriter(filePath, true))
                {
                    CellexalLog.Log("Dumping selection data to " + CellexalLog.FixFilePath(filePath));
                    CellexalLog.Log("\tSelection consists of  " + GetCurrentSelection().Count + " points");
                    if (selectionHistory != null)
                        CellexalLog.Log("\tThere are " + selectionHistory.Count + " entries in the history");
                    foreach (Tuple<string, string> gp in annotatedPoints)
                    {
                        file.Write(gp.Item1);
                        file.Write("\t");
                        file.Write(gp.Item2);
                        file.WriteLine();
                    }
                }
                annotationCtr++;
            }
            annotatedPoints.Clear();
        }

        /// <summary>
        /// Restore all saved AnnotationPanels
        /// </summary>
        /// <param name="filePath"></param>
        public void LoadAnnotationTags( )
        {   
            string savedSelectionsPath = CellexalUser.UserSpecificFolder + @"\TaggedCells\";
            if(Directory.Exists( savedSelectionsPath ))
            {
                string [] fileEntries = Directory.GetFiles(savedSelectionsPath);
                foreach(string fileName in fileEntries)
                {
                    LoadAnnotationTagFile( fileName );
                }
            }
        }

        /// <summary>
        /// Restore on annotation from a file
        /// </summary>
        /// <param name="file"></param>
        public void LoadAnnotationTagFile(string file)
        {   
            // read the cells, populate annotatedPoints and run AnnotateCells( List<Cell> Cells )
            List<string> cellsNames = new List<string>();
            string tag;
            using (StreamReader streamreader = new StreamReader( file ) )
            {
                tag = streamreader.ReadLine();
                while (streamreader.Peek() > 0)
                {
                    cellsNames.Add( streamreader.ReadLine() );
                }
            }
            //RecolorSelectionPoints();
            AnnotateCells ( cellsNames, tag );
        }
        /// <summary>
        /// Gets a selection colors.
        /// </summary>
        /// <param name="index">The index of the color. Indices outside the range of the array colors will be corrected with remainder division.</param>
        public Color GetColor(int index)
        {
            return CellexalConfig.Config.SelectionToolColors[index % CellexalConfig.Config.SelectionToolColors.Length];
        }
    }
}
