using CellexalVR.AnalysisObjects;
using CellexalVR.Extensions;
using CellexalVR.General;
using CellexalVR.Interaction;
using CellexalVR.Menu.Buttons.Attributes;
using CellexalVR.Menu.Buttons.Facs;
using CellexalVR.Spatial;
using CellexalVR.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CellexalVR.Multiuser
{
    /// <summary>
    /// This class holds the remote-callable commands that are sent over network between to connected clients.
    /// To synchronize the scenes in multiplayer it means when a function is called on one client the same has to be done on the others. 
    /// Each function in this class represent one such function to synchronize the scenes.
    /// </summary>
    public class MultiuserMessageReciever : Photon.MonoBehaviour
    {
        private List<MultiuserMessageSender> gamemanagers = new List<MultiuserMessageSender>();
        private MultiuserMessageSender multiuserMessageSender;
        public ReferenceManager referenceManager;

        private Dictionary<Collider, bool> colliders = new Dictionary<Collider, bool>();

        private void OnValidate()
        {
            if (gameObject.scene.IsValid())
            {
                referenceManager = GameObject.Find("InputReader").GetComponent<ReferenceManager>();
            }
        }

        private void Start()
        {
            if (referenceManager == null)
            {
                referenceManager = GameObject.Find("InputReader").GetComponent<ReferenceManager>();
            }
            multiuserMessageSender = referenceManager.multiuserMessageSender;
            //referenceManager = GameObject.Find("InputReader").GetComponent<ReferenceManager>();
        }

        #region RPCs
        // these methods are basically messages that are sent over the network from on client to another.

        #region Loading
        [PunRPC]
        public void RecieveMessageReadFolder(string path)
        {
            CellexalLog.Log("Recieved message to read folder at " + path);
            referenceManager.inputReader.ReadFolder(path);
            referenceManager.inputFolderGenerator.DestroyFolders();
        }

        [PunRPC]
        public void RecieveMessageSynchConfig(byte[] data)
        {
            CellexalLog.Log("Recieved message to synch config");
            referenceManager.configManager.SynchroniseConfig(data);
        }

        [PunRPC]
        public void RecieveMessageLoadingMenu(bool delete)
        {
            CellexalLog.Log("Recieved message to reset to loading dataset scene");
            referenceManager.loaderController.ResetFolders(delete);
        }
        #endregion

        #region Interaction
        [PunRPC]
        public void RecieveMessageDisableColliders(string name)
        {
            GameObject obj = GameObject.Find(name);
            if (obj == null)
            {
                return;
            }
            Collider[] children = obj.GetComponentsInChildren<Collider>();
            int i = 0;
            foreach (Collider c in children)
            {
                if (c)
                {
                    i++;
                    colliders[c] = c.enabled;
                    c.enabled = false;
                }
            }

        }



        [PunRPC]
        public void RecieveMessageEnableColliders(string name)
        {
            GameObject obj = GameObject.Find(name);
            if (obj == null)
            {
                return;
            }
            Collider[] children = obj.GetComponentsInChildren<Collider>();
            int i = 0;

            foreach (KeyValuePair<Collider, bool> pair in colliders)
            {
                if (pair.Key)
                {
                    i++;
                    pair.Key.enabled = pair.Value;
                }
            }
        }
        #endregion

        #region Coloring
        [PunRPC]
        public void RecieveMessageColorGraphsByGene(string geneName)
        {
            CellexalLog.Log("Recieved message to color all graphs by " + geneName);
            referenceManager.cellManager.ColorGraphsByGene(geneName); //, referenceManager.graphManager.GeneExpressionColoringMethod);
            referenceManager.geneKeyboard.SubmitOutput(false);
            referenceManager.autoCompleteList.ClearList();
        }

        [PunRPC]
        public void RecieveMessageColoringMethodChanged(int newMode)
        {
            CellexalLog.Log("Recieved message to change coloring mode to " + newMode);
            referenceManager.coloringOptionsList.SwitchMode((GraphManager.GeneExpressionColoringMethods)newMode);
        }

        //[PunRPC]
        //public void RecieveMessageColorGraphsByPreviousExpression(string geneName)
        //{
        //    CellexalLog.Log("Recieved message to color all graphs by " + geneName);
        //    referenceManager.cellManager.ColorGraphsByPreviousExpression(geneName);
        //}

        [PunRPC]
        public void RecieveMessageColorByAttribute(string attributeType, bool toggle)
        {
            CellexalLog.Log("Recieved message to " + (toggle ? "toggle" : "untoggle") + " all graphs by attribute " + attributeType);
            //Color col = new Color(r, g, b);
            referenceManager.cellManager.ColorByAttribute(attributeType, toggle);
            //var attributeButton = GameObject.Find("/[CameraRig]/Controller (left)/Main Menu/Attribute Menu/AttributeTabPrefab(Clone)/" + attributeType);
            var attributeButton = referenceManager.attributeSubMenu.FindButton(attributeType);
            attributeButton.ToggleOutline(toggle);
            //if (attributeButton)
            //{
            //    var outline = attributeButton.GetComponent<ColorByAttributeButton>().activeOutline;
            //    attributeButton.storedState = toggle;
            //}
            attributeButton.GetComponent<ColorByAttributeButton>().colored = toggle;
        }

        [PunRPC]
        public void RecieveMessageColorByIndex(string indexName)
        {
            CellexalLog.Log("Recieved message to color all graphs by index " + indexName);
            //Color col = new Color(r, g, b);
            referenceManager.cellManager.ColorByIndex(indexName);
            referenceManager.indexMenu.FindButton(indexName).GetComponent<ColorByIndexButton>().TurnOff();
        }
        #endregion

        #region Keyboard

        [PunRPC]
        public void RecieveMessageActivateKeyboard(bool activate)
        {
            referenceManager.keyboardSwitch.SetKeyboardVisible(activate);
        }

        [PunRPC]
        public void RecieveMessageKeyClicked(string key)
        {
            CellexalLog.Log("Recieved message to add  " + key + " to search field");
            referenceManager.geneKeyboard.AddText(key, false);
        }

        [PunRPC]
        public void RecieveMessageKBackspaceKeyClicked()
        {
            CellexalLog.Log("Recieved message to click backspace");
            referenceManager.geneKeyboard.BackSpace(false);
        }

        [PunRPC]
        public void RecieveMessageClearKeyClicked()
        {
            CellexalLog.Log("Recieved message to clear search field");
            referenceManager.geneKeyboard.Clear(false);
        }

        [PunRPC]
        public void RecieveMessageSearchLockToggled(int index)
        {
            CellexalLog.Log("Recieved message to toggle lock number " + index);
            referenceManager.previousSearchesList.searchLocks[index].Click();

        }

        [PunRPC]
        public void RecieveMessageAddAnnotation(string annotation, int index)
        {
            CellexalLog.Log("Recieved message to add annotation: " + annotation);
            referenceManager.selectionManager.AddAnnotation(annotation, index);
        }

        [PunRPC]
        public void RecieveMessageExportAnnotations()
        {
            CellexalLog.Log("Recieved message to export annotations");
            referenceManager.selectionManager.DumpAnnotatedSelectionToTextFile();
        }

        [PunRPC]
        public void RecieveMessageClearExpressionColours()
        {
            CellexalLog.Log("Recieved message to clear expression colours on the graphs");
            referenceManager.graphManager.ClearExpressionColours();
        }

        [PunRPC]
        public void RecieveMessageCalculateCorrelatedGenes(string geneName)
        {
            CellexalLog.Log("Recieved message to calculate genes correlated to " + geneName);
            referenceManager.correlatedGenesList.CalculateCorrelatedGenes(geneName, Definitions.Measurement.GENE);
        }

        [PunRPC]
        public void RecieveMessageRecolorSelectionPoints()
        {
            CellexalLog.Log("Recieved message to recolor graph points by current selection");
            referenceManager.selectionManager.RecolorSelectionPoints();
        }

        #endregion

        #region Selection
        [PunRPC]
        public void RecieveMessageConfirmSelection()
        {
            CellexalLog.Log("Recieved message to confirm selection");
            referenceManager.selectionManager.ConfirmSelection();
        }

        [PunRPC]
        public void RecieveMessageAddSelect(string graphName, string label, int newGroup, float r, float g, float b)
        {
            referenceManager.selectionManager.DoClientSelectAdd(graphName, label, newGroup, new Color(r, g, b));
        }

        [PunRPC]
        public void RecieveMessageCubeColoured(string graphName, string label, int newGroup, float r, float g, float b)
        {
            referenceManager.selectionManager.DoClientSelectAdd(graphName, label, newGroup, new Color(r, g, b), true);
        }

        [PunRPC]
        public void RecieveMessageGoBackOneColor()
        {
            referenceManager.selectionManager.GoBackOneColorInHistory();
        }

        [PunRPC]
        public void RecieveMessageGoBackSteps(int k)
        {
            for (int i = 0; i < k; i++)
            {
                referenceManager.selectionManager.GoBackOneStepInHistory();
            }
        }

        [PunRPC]
        public void RecieveMessageCancelSelection()
        {
            referenceManager.selectionManager.CancelSelection();
        }

        [PunRPC]
        public void RecieveMessageRedoOneColor()
        {
            referenceManager.selectionManager.GoForwardOneColorInHistory();
        }

        [PunRPC]
        public void RecieveMessageRedoSteps(int k)
        {
            for (int i = 0; i < k; i++)
            {
                referenceManager.selectionManager.GoForwardOneStepInHistory();
            }
        }
        #endregion

        #region Draw tool
        [PunRPC]
        public void RecieveMessageDrawLine(float r, float g, float b, float[] xcoords, float[] ycoords, float[] zcoords)
        {
            CellexalLog.Log("Recieved message to draw line with " + xcoords.Length + " segments");
            Vector3[] coords = new Vector3[xcoords.Length];
            for (int i = 0; i < xcoords.Length; i++)
            {
                coords[i] = new Vector3(xcoords[i], ycoords[i], zcoords[i]);

            }
            Color col = new Color(r, g, b);
            referenceManager.drawTool.DrawNewLine(col, coords);
        }

        [PunRPC]
        public void RecieveMessageClearAllLines()
        {
            CellexalLog.Log("Recieved message to clear line segments");
            referenceManager.drawTool.SkipNextDraw();
            referenceManager.drawTool.ClearAllLines();
        }

        [PunRPC]
        public void RecieveMessageClearLastLine()
        {
            CellexalLog.Log("Recieved message to clear previous line");
            referenceManager.drawTool.SkipNextDraw();
            referenceManager.drawTool.ClearLastLine();
        }

        [PunRPC]
        public void RecieveMessageClearLinesWithColor(float r, float g, float b)
        {
            CellexalLog.Log("Recieved message to clear previous line");
            referenceManager.drawTool.SkipNextDraw();
            referenceManager.drawTool.ClearAllLinesWithColor(new Color(r, g, b));
        }
        #endregion

        #region Graphs

        [PunRPC]
        public void RecieveMessageMoveGraph(string moveGraphName, float posX, float posY, float posZ, float rotX, float rotY, float rotZ, float rotW, float scaleX, float scaleY, float scaleZ)
        {
            Graph g = referenceManager.graphManager.FindGraph(moveGraphName);
            bool graphExists = g != null;
            if (graphExists)
            {
                try
                {
                    //g.GetComponent<VRTK.VRTK_InteractableObject>().isGrabbable = false;
                    g.transform.position = new Vector3(posX, posY, posZ);
                    g.transform.rotation = new Quaternion(rotX, rotY, rotZ, rotW);
                    g.transform.localScale = new Vector3(scaleX, scaleY, scaleZ);
                    //g.GetComponent<GraphInteract>().StopPositionSync();
                }
                catch (Exception e)
                {
                    CellexalLog.Log("Could not move graph - Error: " + e);
                }
            }
            //else
            //{
            //    CellexalLog.Log("Could not find graph to move");
            //}

        }

        [PunRPC]
        public void RecieveMessageGraphUngrabbed(string graphName, float velX, float velY, float velZ, float angVelX, float angVelY, float angVelZ)
        {
            Graph g = referenceManager.graphManager.FindGraph(graphName);
            if (g)
            {
                Rigidbody r = g.GetComponent<Rigidbody>();
                r.velocity = new Vector3(velX, velY, velZ);
                r.angularVelocity = new Vector3(angVelX, angVelY, angVelZ);
            }
        }

        [PunRPC]
        public void RecieveMessageToggleGrabbable(string name, bool enable)
        {
            //var graph = referenceManager.graphManager.FindGraph(name);
            var gameObject = GameObject.Find(name);
            if (gameObject == null)
            {
                CellexalLog.Log("Tried to toggle object colliders but could not find object with name: " + name);
            }
            else
            {
                var colliders = gameObject.GetComponents<Collider>();
                foreach (Collider c in colliders)
                {
                    c.enabled = enable;
                }
            }
        }

        [PunRPC]
        public void RecieveMessageResetGraph()
        {
            CellexalLog.Log("Recieved message to reset graph colors");
            referenceManager.graphManager.ResetGraphsColor();
        }

        [PunRPC]
        public void RecieveMessageResetGraphPosition()
        {
            CellexalLog.Log("Recieved message to reset graph position, scale and rotation");
            referenceManager.graphManager.ResetGraphsPosition();
        }

        [PunRPC]
        public void RecieveMessageDrawLinesBetweenGps(bool toggle)
        {
            CellexalLog.Log("Recieved message to draw lines between graph points");
            StartCoroutine(referenceManager.cellManager.DrawLinesBetweenGraphPoints(referenceManager.selectionManager.GetLastSelection()));
            //CellexalEvents.LinesBetweenGraphsDrawn.Invoke();
        }

        [PunRPC]
        public void RecieveMessageClearLinesBetweenGps()
        {
            CellexalLog.Log("Recieved message to clear lines between graph points");
            referenceManager.cellManager.ClearLinesBetweenGraphPoints();
            //CellexalEvents.LinesBetweenGraphsCleared.Invoke();
        }

        [PunRPC]
        public void RecieveMessageBundleAllLines()
        {
            CellexalLog.Log("Recieved message to clear lines between graph points");
            referenceManager.cellManager.BundleAllLines();
        }


        [PunRPC]
        public void RecieveMessageAddMarker(string indexName)
        {
            //var markerButton = GameObject.Find("/Main Menu/Attribute Menu/TabPrefab(Clone)/" + indexName);
            var markerButton = referenceManager.createFromMarkerMenu.FindButton(indexName);
            if (referenceManager.newGraphFromMarkers.markers.Count < 3 && !referenceManager.newGraphFromMarkers.markers.Contains(indexName))
            {
                referenceManager.newGraphFromMarkers.markers.Add(indexName);
                if (markerButton)
                {
                    markerButton.ToggleOutline(true);
                    //markerButton.GetComponent<AddMarkerButton>().activeOutline.SetActive(true);
                    //markerButton.GetComponent<AddMarkerButton>().activeOutline.GetComponent<MeshRenderer>().enabled = true;
                }
            }
            else if (referenceManager.newGraphFromMarkers.markers.Contains(indexName))
            {
                referenceManager.newGraphFromMarkers.markers.Remove(indexName);
                if (markerButton)
                {
                    markerButton.ToggleOutline(false);
                    //markerButton.GetComponent<AddMarkerButton>().activeOutline.SetActive(false);
                }
            }
        }

        [PunRPC]
        public void RecieveMessageCreateMarkerGraph()
        {
            CellexalLog.Log("Recieved message to create marker graph");
            referenceManager.newGraphFromMarkers.CreateMarkerGraph();
        }

        [PunRPC]
        public void RecieveMessageCreateAttributeGraph()
        {
            CellexalLog.Log("Recieved message to create attribute graph");
            referenceManager.graphGenerator.CreateSubGraphs(referenceManager.attributeSubMenu.attributes);
        }

        [PunRPC]
        public void RecieveMessageActivateSlices()
        {
            CellexalLog.Log("Recieved message to activate slices in spatial graph");
            foreach (SpatialGraph spatialGraph in referenceManager.graphManager.spatialGraphs)
            {
                spatialGraph.ActivateSlices();
            }
        }

        #endregion

        #region Heatmaps
        [PunRPC]
        public void RecieveMessageMoveHeatmap(string heatmapName, float posX, float posY, float posZ, float rotX, float rotY, float rotZ, float rotW, float scaleX, float scaleY, float scaleZ)
        {
            Heatmap hm = referenceManager.heatmapGenerator.FindHeatmap(heatmapName);
            bool heatmapExists = hm != null;
            if (heatmapExists)
            {
                try
                {
                    hm.transform.position = new Vector3(posX, posY, posZ);
                    hm.transform.rotation = new Quaternion(rotX, rotY, rotZ, rotW);
                    hm.transform.localScale = new Vector3(scaleX, scaleY, scaleZ);
                }
                catch (Exception e)
                {
                    CellexalLog.Log("Could not move heatmap - Error: " + e);
                }
            }
            //else
            //{
            //    CellexalLog.Log("Could not find heatmap to move");
            //}
        }

        [PunRPC]
        public void RecieveMessageCreateHeatmap(string hmName)
        {
            CellexalLog.Log("Recieved message to create heatmap");
            referenceManager.heatmapGenerator.CreateHeatmap(hmName);
        }

        [PunRPC]
        public void RecieveMessageHandleBoxSelection(string heatmapName, int hitx, int hity, int selectionStartX, int selectionStartY)
        {
            Heatmap hm = referenceManager.heatmapGenerator.FindHeatmap(heatmapName);
            bool heatmapExists = hm != null;
            if (heatmapExists)
            {
                try
                {
                    hm.GetComponent<HeatmapRaycast>().HandleBoxSelection(hitx, hity, selectionStartX, selectionStartY);
                }
                catch (Exception e)
                {
                    CellexalLog.Log("Could not move heatmap - Error: " + e);
                }
            }
        }

        [PunRPC]
        public void RecieveMessageConfirmSelection(string heatmapName, int hitx, int hity, int selectionStartX, int selectionStartY)
        {
            Heatmap hm = referenceManager.heatmapGenerator.FindHeatmap(heatmapName);
            bool heatmapExists = hm != null;
            if (heatmapExists)
            {
                try
                {
                    hm.GetComponent<HeatmapRaycast>().ConfirmSelection(hitx, hity, selectionStartX, selectionStartY);
                }
                catch (Exception e)
                {
                    CellexalLog.Log("Could not move heatmap - Error: " + e);
                }
            }
        }

        [PunRPC]
        public void RecieveMessageHandleMovingSelection(string heatmapName, int hitx, int hity)
        {
            Heatmap hm = referenceManager.heatmapGenerator.FindHeatmap(heatmapName);
            bool heatmapExists = hm != null;
            if (heatmapExists)
            {
                try
                {
                    hm.GetComponent<HeatmapRaycast>().HandleMovingSelection(hitx, hity);
                }
                catch (Exception e)
                {
                    CellexalLog.Log("Could not move heatmap - Error: " + e);
                }
            }
        }

        [PunRPC]
        public void RecieveMessageMoveSelection(string heatmapName, int hitx, int hity, int selectedGroupLeft, int selectedGroupRight, int selectedGeneTop, int selectedGeneBottom)
        {
            Heatmap hm = referenceManager.heatmapGenerator.FindHeatmap(heatmapName);
            bool heatmapExists = hm != null;
            if (heatmapExists)
            {
                try
                {
                    hm.GetComponent<HeatmapRaycast>().MoveSelection(hitx, hity, selectedGroupLeft, selectedGroupRight, selectedGeneTop, selectedGeneBottom);
                }
                catch (Exception e)
                {
                    //CellexalLog.Log("Could not move heatmap - Error: " + e);
                }
            }
        }

        [PunRPC]
        public void RecieveMessageHandleHitHeatmap(string heatmapName, int hitx, int hity)
        {
            Heatmap hm = referenceManager.heatmapGenerator.FindHeatmap(heatmapName);
            bool heatmapExists = hm != null;
            if (heatmapExists)
            {
                try
                {
                    hm.GetComponent<HeatmapRaycast>().HandleHitHeatmap(hitx, hity);
                }
                catch (Exception e)
                {
                    //CellexalLog.Log("Failed to handle hit on heatmap. Stacktrace : " + e.StackTrace);
                }
            }
        }


        [PunRPC]
        public void RecieveMessageResetSelecting(string heatmapName)
        {
            Heatmap hm = referenceManager.heatmapGenerator.FindHeatmap(heatmapName);
            bool heatmapExists = hm != null;
            if (heatmapExists)
            {
                try
                {
                    hm.GetComponent<HeatmapRaycast>().ResetSelecting();
                }
                catch (Exception e)
                {
                    CellexalLog.Log("Failed to reset heatmap selecting. Stacktrace : " + e.StackTrace);
                }
            }
        }

        [PunRPC]
        public void RecieveMessageHandlePressDown(string heatmapName, int hitx, int hity)
        {
            Heatmap hm = referenceManager.heatmapGenerator.FindHeatmap(heatmapName);
            bool heatmapExists = hm != null;
            if (heatmapExists)
            {
                try
                {
                    hm.GetComponent<HeatmapRaycast>().HandlePressDown(hitx, hity);
                }
                catch (Exception e)
                {
                    CellexalLog.Log("Failed to handle heatmap press down. Stacktrace : " + e.StackTrace);
                }
            }
        }

        [PunRPC]
        public void RecieveMessageCreateNewHeatmapFromSelection(string heatmapName, int selectedGroupLeft, int selectedGroupRight, int selectedGeneTop,
            int selectedGeneBottom, float selectedBoxWidth, float selectedBoxHeight)
        {
            Heatmap hm = referenceManager.heatmapGenerator.FindHeatmap(heatmapName);
            bool heatmapExists = hm != null;
            if (heatmapExists)
            {
                try
                {
                    hm.CreateNewHeatmapFromSelection(selectedGroupLeft, selectedGroupRight,
                        selectedGeneTop, selectedGeneBottom, selectedBoxWidth, selectedBoxHeight);
                }
                catch (Exception e)
                {
                    CellexalLog.Log("Failed to create new heatmap from selection. Stacktrace : " + e.StackTrace);
                }
            }
        }

        [PunRPC]
        public void RecieveMessageReorderByAttribute(string heatmapName, bool shouldReorder)
        {
            CellexalLog.Log("Recieved message to " + (shouldReorder ? "reorder" : "restore") + " heatmap");
            Heatmap hm = referenceManager.heatmapGenerator.FindHeatmap(heatmapName);
            bool heatmapExists = hm != null;
            if (heatmapExists)
            {
                if (shouldReorder)
                {
                    hm.ReorderByAttribute();
                }
                else
                {
                    referenceManager.heatmapGenerator.BuildTexture(hm.selection, "", hm);
                }
            }
        }

        [PunRPC]
        public void RecieveMessageHandleHitGenesList(string heatmapName, int hity)
        {
            Heatmap hm = referenceManager.heatmapGenerator.FindHeatmap(heatmapName);
            bool heatmapExists = hm != null;
            if (heatmapExists)
            {
                hm.GetComponent<HeatmapRaycast>().HandleHitGeneList(hity);
            }
            else
            {
                return;
            }
        }

        [PunRPC]
        public void RecieveMessageHandleHitGroupingBar(string heatmapName, int hitx)
        {
            Heatmap hm = referenceManager.heatmapGenerator.FindHeatmap(heatmapName);
            bool heatmapExists = hm != null;
            if (heatmapExists)
            {
                hm.GetComponent<HeatmapRaycast>().HandleHitGroupingBar(hitx);
            }
            else
            {
                return;
            }
        }

        [PunRPC]
        public void RecieveMessageHandleHitAttributeBar(string heatmapName, int hitx)
        {
            Heatmap hm = referenceManager.heatmapGenerator.FindHeatmap(heatmapName);
            bool heatmapExists = hm != null;
            if (heatmapExists)
            {
                hm.GetComponent<HeatmapRaycast>().HandleHitAttributeBar(hitx);
            }
            else
            {
                return;
            }
        }

        [PunRPC]
        public void RecieveMessageResetInfoTexts(string heatmapName)
        {
            Heatmap hm = referenceManager.heatmapGenerator.FindHeatmap(heatmapName);
            bool heatmapExists = hm != null;
            if (heatmapExists)
            {
                hm.barInfoText.text = "";
                hm.enlargedGeneText.gameObject.SetActive(false);
            }
            else
            {
                return;
            }

        }

        [PunRPC]
        public void RecieveMessageResetHeatmapHighlight(string heatmapName)
        {
            Heatmap hm = referenceManager.heatmapGenerator.FindHeatmap(heatmapName);
            bool heatmapExists = hm != null;
            if (heatmapExists)
            {
                try
                {
                    referenceManager.heatmapGenerator.FindHeatmap(heatmapName).ResetHeatmapHighlight();
                }
                catch (Exception e)
                {
                    CellexalLog.Log("Failed to reset heatmap highlight. Stacktrace : " + e.StackTrace);
                }
            }
        }
        #endregion

        #region Networks
        [PunRPC]
        public void RecieveMessageGenerateNetworks(int layoutSeed)
        {
            CellexalLog.Log("Recieved message to generate networks");
            referenceManager.networkGenerator.GenerateNetworks(layoutSeed);
        }

        [PunRPC]
        public void RecieveMessageMoveNetwork(string networkName, float posX, float posY, float posZ, float rotX, float rotY, float rotZ, float rotW, float scaleX, float scaleY, float scaleZ)
        {
            NetworkHandler nh = referenceManager.networkGenerator.FindNetworkHandler(networkName);
            bool networkExists = nh != null;
            if (networkExists)
            {
                try
                {
                    nh.transform.position = new Vector3(posX, posY, posZ);
                    nh.transform.rotation = new Quaternion(rotX, rotY, rotZ, rotW);
                    nh.transform.localScale = new Vector3(scaleX, scaleY, scaleZ);
                }
                catch (Exception e)
                {
                    CellexalLog.Log("Could not move network - Error: " + e);
                }
            }
            //else
            //{
            //    CellexalLog.Log("Could not find network to move");
            //}
        }

        [PunRPC]
        public void RecieveMessageNetworkUngrabbed(string networkName, float velX, float velY, float velZ, float angVelX, float angVelY, float angVelZ)
        {
            NetworkHandler nh = referenceManager.networkGenerator.FindNetworkHandler(networkName);
            if (nh)
            {
                Rigidbody r = nh.GetComponent<Rigidbody>();
                r.velocity = new Vector3(velX, velY, velZ);
                r.angularVelocity = new Vector3(angVelX, angVelY, angVelZ);
            }
        }

        [PunRPC]
        public void RecieveMessageEnlargeNetwork(string networkHandlerName, string networkCenterName)
        {
            CellexalLog.Log("Recieved message to enlarge network " + networkCenterName + " in handler " + networkHandlerName);
            var handler = referenceManager.networkGenerator.FindNetworkHandler(networkHandlerName);
            bool handlerExists = handler != null;
            if (!handlerExists)
            {
                return;
            }
            var center = handler.FindNetworkCenter(networkCenterName);
            bool networkExists = (handlerExists && center != null);
            if (networkExists)
            {
                center.EnlargeNetwork();
            }
            else
            {
                CellexalLog.Log("Could not find networkcenter " + networkCenterName);
            }
        }

        [PunRPC]
        public void RecieveMessageBringBackNetwork(string networkHandlerName, string networkCenterName)
        {
            CellexalLog.Log("Recieved message to bring back network " + networkCenterName + " in handler " + networkHandlerName);
            var handler = referenceManager.networkGenerator.FindNetworkHandler(networkHandlerName);
            bool handlerExists = handler != null;
            if (!handlerExists)
            {
                return;
            }
            var center = handler.FindNetworkCenter(networkCenterName);
            bool networkExists = (handlerExists && center != null);
            if (networkExists)
            {
                center.BringBackOriginal();
            }
            else
            {
                CellexalLog.Log("Could not find networkcenter " + networkCenterName);
            }
        }

        [PunRPC]
        public void RecieveMessageSwitchNetworkLayout(int layout, string networkHandlerName, string networkCenterName)
        {
            CellexalLog.Log("Recieved message to generate networks");
            print("network names:" + networkCenterName + " " + networkHandlerName);
            var handler = referenceManager.networkGenerator.FindNetworkHandler(networkHandlerName);
            bool handlerExists = handler != null;
            if (!handlerExists)
            {
                return;
            }
            var center = handler.FindNetworkCenter(networkCenterName);
            bool networkExists = (handlerExists && center != null);
            if (networkExists)
            {
                center.SwitchLayout((NetworkCenter.Layout)layout);
            }
            else
            {
                CellexalLog.Log("Could not find networkcenter " + networkCenterName);
            }
        }

        [PunRPC]
        public void RecieveMessageMoveNetworkCenter(string networkHandlerName, string networkCenterName, float posX, float posY, float posZ, float rotX, float rotY, float rotZ, float rotW, float scaleX, float scaleY, float scaleZ)
        {
            var handler = referenceManager.networkGenerator.FindNetworkHandler(networkHandlerName);
            bool handlerExists = handler != null;
            if (!handlerExists)
            {
                return;
            }
            var center = handler.FindNetworkCenter(networkCenterName);
            bool networkExists = (handlerExists && center != null);
            if (networkExists)
            {
                center.transform.position = new Vector3(posX, posY, posZ);
                center.transform.rotation = new Quaternion(rotX, rotY, rotZ, rotW);
                center.transform.localScale = new Vector3(scaleX, scaleY, scaleZ);
            }
            else
            {
                CellexalLog.Log("Could not find networkcenter to move");
            }
        }

        [PunRPC]
        public void RecieveMessageNetworkCenterUngrabbed(string networkHandlerName, string networkCenterName, float velX, float velY, float velZ, float angVelX, float angVelY, float angVelZ)
        {
            NetworkHandler nh = referenceManager.networkGenerator.FindNetworkHandler(networkHandlerName);
            if (nh)
            {
                NetworkCenter nc = nh.FindNetworkCenter(networkCenterName);
                if (nc)
                {
                    Rigidbody r = nc.GetComponent<Rigidbody>();
                    r.velocity = new Vector3(velX, velY, velZ);
                    r.angularVelocity = new Vector3(angVelX, angVelY, angVelZ);
                }
            }
        }

        [PunRPC]
        public void RecieveMessageSetArcsVisible(bool toggleToState, string networkName)
        {
            CellexalLog.Log("Toggle arcs of " + networkName);
            NetworkCenter network = GameObject.Find(networkName).GetComponent<NetworkCenter>();
            network.SetCombinedArcsVisible(false);
            network.SetArcsVisible(toggleToState);
        }

        [PunRPC]
        public void RecieveMessageSetCombinedArcsVisible(bool toggleToState, string networkName)
        {
            CellexalLog.Log("Toggle combined arcs of " + networkName);
            NetworkCenter network = GameObject.Find(networkName).GetComponent<NetworkCenter>();
            network.SetArcsVisible(false);
            network.SetCombinedArcsVisible(toggleToState);
        }
        #endregion

        #region Hide tool
        [PunRPC]
        public void RecieveMessageMinimizeGraph(string graphName)
        {
            Graph g = referenceManager.graphManager.FindGraph(graphName);
            g.HideGraph();
            referenceManager.minimizedObjectHandler.MinimizeObject(g.gameObject, graphName);
        }

        [PunRPC]
        public void RecieveMessageShowGraph(string graphName, string jailName)
        {
            Graph g = referenceManager.graphManager.FindGraph(graphName);
            GameObject jail = GameObject.Find(jailName);
            MinimizedObjectHandler handler = referenceManager.minimizedObjectHandler;
            g.ShowGraph();
            handler.ContainerRemoved(jail.GetComponent<MinimizedObjectContainer>());
            Destroy(jail);
        }

        [PunRPC]
        public void RecieveMessageMinimizeNetwork(string networkName)
        {
            NetworkHandler nh = referenceManager.networkGenerator.FindNetworkHandler(networkName);
            nh.HideNetworks();
            referenceManager.minimizedObjectHandler.MinimizeObject(nh.gameObject, networkName);
        }

        [PunRPC]
        public void RecieveMessageShowNetwork(string networkName, string jailName)
        {
            NetworkHandler nh = referenceManager.networkGenerator.FindNetworkHandler(networkName);
            GameObject jail = GameObject.Find(jailName);
            MinimizedObjectHandler handler = referenceManager.minimizedObjectHandler;
            nh.ShowNetworks();
            handler.ContainerRemoved(jail.GetComponent<MinimizedObjectContainer>());
            Destroy(jail);
        }

        [PunRPC]
        public void RecieveMessageMinimizeHeatmap(string heatmapName)
        {
            Heatmap h = referenceManager.heatmapGenerator.FindHeatmap(heatmapName);
            h.HideHeatmap();
            referenceManager.minimizedObjectHandler.MinimizeObject(h.gameObject, heatmapName);
        }

        [PunRPC]
        public void RecieveMessageShowHeatmap(string heatmapName, string jailName)
        {
            Heatmap h = referenceManager.heatmapGenerator.FindHeatmap(heatmapName);
            GameObject jail = GameObject.Find(jailName);
            MinimizedObjectHandler handler = referenceManager.minimizedObjectHandler;
            h.ShowHeatmap();
            handler.ContainerRemoved(jail.GetComponent<MinimizedObjectContainer>());
            Destroy(jail);
        }
        #endregion

        #region Delete tool
        [PunRPC]
        public void RecieveMessageDeleteObject(string name, string tag)
        {
            CellexalLog.Log("Recieved message to delete object with name: " + name);
            //GameObject objectToDelete = GameObject.Find(name);
            if (tag == "SubGraph")
            {
                referenceManager.graphManager.DeleteGraph(name, tag);
            }
            else if (tag == "FacsGraph")
            {
                referenceManager.graphManager.DeleteGraph(name, tag);
            }
            else if (tag == "HeatBoard")
            {
                referenceManager.heatmapGenerator.DeleteHeatmap(name);
            }
            else if (tag == "Network")
            {
                GameObject.Find(name).GetComponent<NetworkHandler>().DeleteNetwork();
            }
        }

        #endregion

        #region Velocity
        [PunRPC]
        public void RecieveMessageStartVelocity()
        {
            List<Graph> graphs = referenceManager.velocityGenerator.ActiveGraphs;
            foreach (Graph g in graphs)
            {
                g.velocityParticleEmitter.Play();
            }
        }

        [PunRPC]
        public void RecieveMessageStopVelocity()
        {
            List<Graph> graphs = referenceManager.velocityGenerator.ActiveGraphs;
            foreach (Graph g in graphs)
            {
                g.velocityParticleEmitter.Stop();
            }
        }

        [PunRPC]
        public void RecieveMessageToggleGraphPoints()
        {
            referenceManager.velocityGenerator.ToggleGraphPoints();
        }

        [PunRPC]
        public void RecieveMessageConstantSynchedMode()
        {
            referenceManager.velocityGenerator.ChangeConstantSynchedMode();
        }

        [PunRPC]
        public void RecieveMessageGraphPointColorsMode()
        {
            referenceManager.velocityGenerator.ChangeGraphPointColorMode();
        }

        [PunRPC]
        public void RecieveMessageChangeParticleMode()
        {
            referenceManager.velocityGenerator.ChangeParticle();
        }

        [PunRPC]
        public void RecieveMessageChangeFrequency(float amount)
        {
            referenceManager.velocityGenerator.ChangeFrequency(amount);
        }

        [PunRPC]
        public void RecieveMessageChangeThreshold(float amount)
        {
            referenceManager.velocityGenerator.ChangeThreshold(amount);
        }

        [PunRPC]
        public void RecieveMessageChangeSpeed(float amount)
        {
            referenceManager.velocityGenerator.ChangeSpeed(amount);
        }

        [PunRPC]
        public void RecieveMessageReadVelocityFile(string shorterFilePath, string subGraphName, bool activate)
        {
            CellexalLog.Log("Recieved message to read velocity file - " + shorterFilePath);
            var veloButton = referenceManager.velocitySubMenu.FindButton(shorterFilePath, subGraphName);
            string filePath = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "Data" + Path.DirectorySeparatorChar  + CellexalUser.DataSourceFolder + Path.DirectorySeparatorChar + shorterFilePath + ".mds";
            Graph graph = referenceManager.graphManager.FindGraph(subGraphName);
            //if (subGraphName != string.Empty)
            //{
            //    referenceManager.velocityGenerator.ReadVelocityFile(filePath, subGraphName);
            //}
            //else
            //{
            //    referenceManager.velocityGenerator.ReadVelocityFile(filePath);
            //}
            if (activate)
            {
                referenceManager.velocityGenerator.ReadVelocityFile(filePath, subGraphName);
            }
            else
            {
                if (graph.graphPointsInactive)
                {
                    graph.ToggleGraphPoints();
                }
                referenceManager.velocityGenerator.ActiveGraphs.Remove(graph);
            }
            referenceManager.velocitySubMenu.DeactivateOutlines();
            veloButton.ToggleOutline(true);
        }
        #endregion

        #region Filters
        [PunRPC]
        public void RecieveMessageSetFilter(string filter)
        {
            CellexalLog.Log("Recieved message to read filter " + filter);
            referenceManager.filterManager.ParseFilter(filter);
        }

        [PunRPC]
        public void RecieveMessageResetFilter()
        {
            CellexalLog.Log("Recieved message to reset filter");
            referenceManager.filterManager.ResetFilter(/*false*/);
        }
        #endregion

        #region Browser
        [PunRPC]
        public void RecieveMessageMoveBrowser(float posX, float posY, float posZ, float rotX, float rotY, float rotZ, float rotW, float scaleX, float scaleY, float scaleZ)
        {
            GameObject wm = referenceManager.webBrowser;
            bool browserExists = wm != null;
            if (browserExists)
            {
                try
                {
                    wm.transform.position = new Vector3(posX, posY, posZ);
                    wm.transform.rotation = new Quaternion(rotX, rotY, rotZ, rotW);
                    wm.transform.localScale = new Vector3(scaleX, scaleY, scaleZ);
                }
                catch (Exception e)
                {
                    CellexalLog.Log("Could not move browser - Error: " + e);
                }
            }
            else
            {
                CellexalLog.Log("Could not find browser to move");
            }
        }

        [PunRPC]
        public void RecieveMessageActivateBrowser(bool activate)
        {
            CellexalLog.Log("Recieved message to toggle web browser");
            referenceManager.webBrowser.GetComponent<WebManager>().SetBrowserActive(activate);
            //referenceManager.webBrowser.GetComponent<WebManager>().SetVisible(activate);
        }

        [PunRPC]
        public void RecieveMessageBrowserKeyClicked(string key)
        {
            CellexalLog.Log("Recieved message to add " + key + " to url field");
            referenceManager.webBrowserKeyboard.AddText(key, false);
        }

        [PunRPC]
        public void RecieveMessageBrowserEnter()
        {
            string text = referenceManager.webBrowserKeyboard.output.text;
            referenceManager.webBrowser.GetComponentInChildren<SimpleWebBrowser.WebBrowser>().OnNavigate(text);
        }

        #endregion

        #endregion

    }
}