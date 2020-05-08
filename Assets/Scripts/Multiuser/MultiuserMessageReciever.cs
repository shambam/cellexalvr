using CellexalVR.AnalysisObjects;
using CellexalVR.Extensions;
using CellexalVR.General;
using CellexalVR.Interaction;
using CellexalVR.Menu.Buttons;
using CellexalVR.Menu.Buttons.Attributes;
using CellexalVR.Menu.Buttons.Facs;
using CellexalVR.Spatial;
using CellexalVR.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using CellexalVR.AnalysisLogic;
using CellexalVR.DesktopUI;
using CellexalVR.SceneObjects;
using UnityEditor;
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
        public void RecieveMessageH5Config(string path, Dictionary<string, string> h5config)
        {
            CellexalLog.Log(
                "Recieved message to read folder at " + path + " with h5 config with size " + h5config.Count);

            referenceManager.inputReader.ReadFolder(path, h5config);
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

        [PunRPC]
        public void RecieveMessageToggleLaser(bool active, int ownerId, string ownerName)
        {
            if (ownerId != photonView.ownerId) return;
            MultiuserLaserManager mlm =
                referenceManager.multiuserMessageSender.GetComponentInChildren<MultiuserLaserManager>();
            bool exists = mlm.lasersLineRends.TryGetValue(ownerId, out LineRenderer lr);
            if (!exists)
            {
                lr = mlm.AddLaser(ownerId, ownerName);
                if (lr == null) return;
            }
            lr.startColor = lr.endColor = referenceManager.rightLaser.validCollisionColor;
            lr.material.color = lr.startColor = lr.endColor = referenceManager.rightLaser.validCollisionColor;
            lr.gameObject.SetActive(active);
        }

        [PunRPC]
        public void RecieveMessageMoveLaser(float originX, float originY, float originZ,
            float hitX, float hitY, float hitZ, int ownerId, string ownerName)
        {
            if (ownerId != photonView.ownerId) return;
            MultiuserLaserManager mlm =
                referenceManager.multiuserMessageSender.GetComponentInChildren<MultiuserLaserManager>();
            // LineRenderer lr = mlm.GetLaser(ownerId);
            bool exists = mlm.lasersLineRends.TryGetValue(ownerId, out LineRenderer lr);
            if (!exists)
            {
                lr = mlm.AddLaser(ownerId, ownerName);
                if (lr == null) return;
                lr.material.color = lr.startColor = lr.endColor = referenceManager.rightLaser.validCollisionColor;
            }
            lr.SetPosition(0, mlm.laserTransforms[ownerId].position);
            lr.SetPosition(1, new Vector3(hitX, hitY, hitZ));
            if (!lr.gameObject.activeSelf) lr.gameObject.SetActive(true);

        }

        #endregion

        #region Legend

        [PunRPC]
        public void RecieveMessageToggleLegend()
        {
            if (referenceManager.legendManager.legendActive)
            {
                referenceManager.legendManager.DeactivateLegend();
            }
            else
            {
                referenceManager.legendManager.ActivateLegend();
            }
        }

        [PunRPC]
        public void RecieveMessageMoveLegend(float posX, float posY, float posZ,
            float rotX, float rotY, float rotZ, float rotW, float scaleX, float scaleY, float scaleZ)
        {
            GameObject legend = referenceManager.legendManager.gameObject;
            if (legend == null || !legend.activeSelf) return;
            legend.transform.position = new Vector3(posX, posY, posZ);
            legend.transform.rotation = new Quaternion(rotX, rotY, rotZ, rotW);
            legend.transform.localScale = new Vector3(scaleX, scaleY, scaleZ);
        }

        [PunRPC]
        public void RecieveMessageLegendUngrabbed(
            float posX, float posY, float posZ,
            float rotX, float rotY, float rotZ, float rotW,
            float velX, float velY, float velZ,
            float angVelX, float angVelY, float angVelZ)
        {
            LegendManager legend = referenceManager.legendManager;
            if (legend)
            {
                Rigidbody r = legend.GetComponent<Rigidbody>();
                legend.transform.position = new Vector3(posX, posY, posZ);
                legend.transform.rotation = new Quaternion(rotX, rotY, rotZ, rotW);
                r.velocity = new Vector3(velX, velY, velZ);
                r.angularVelocity = new Vector3(angVelX, angVelY, angVelZ);
            }
        }

        [PunRPC]
        public void RecieveMessageChangeLegend(string legendName)
        {
            switch (legendName)
            {
                case "GeneExpressionLegend":
                    referenceManager.legendManager.ActivateLegend(LegendManager.Legend.GeneExpressionLegend);
                    break;
                case "SelectionLegend":
                    referenceManager.legendManager.ActivateLegend(LegendManager.Legend.SelectionLegend);
                    break;
                case "AttributeLegend":
                    referenceManager.legendManager.ActivateLegend(LegendManager.Legend.AttributeLegend);
                    break;
            }
        }

        [PunRPC]
        public void RecieveMessageAttributeLegendChangePage(bool dir)
        {
            referenceManager.legendManager.attributeLegend.ChangePage(dir);
        }

        [PunRPC]
        public void RecieveMessageSelectionLegendChangePage(bool dir)
        {
            referenceManager.legendManager.selectionLegend.ChangePage(dir);
        }

        [PunRPC]
        public void RecieveMessageChangeTab(int index)
        {
            referenceManager.legendManager.geneExpressionHistogram.SwitchToTab(index);
        }

        [PunRPC]
        public void RecieveMessageDeactivateSelectedArea()
        {
            referenceManager.legendManager.geneExpressionHistogram.DeactivateSelectedArea();
        }

        [PunRPC]
        public void RecieveMessageMoveSelectedArea(int hitIndex, int savedGeneExpressionHistogramHit)
        {
            referenceManager.legendManager.geneExpressionHistogram.MoveSelectedArea(hitIndex,
                savedGeneExpressionHistogramHit);
            referenceManager.legendManager.GetComponent<LegendRaycaster>().savedGeneExpressionHistogramHitX = -1;
        }

        [PunRPC]
        public void RecieveMessageMoveHighlightArea(int minX, int maxX)
        {
            referenceManager.legendManager.geneExpressionHistogram.MoveHighlightArea(minX, maxX);
        }

        [PunRPC]
        public void RecieveMessageSwitchMode(string mode)
        {
            GeneExpressionHistogram histogram = referenceManager.legendManager.geneExpressionHistogram;
            switch (mode)
            {
                case "Linear":
                    histogram.DesiredYAxisMode = GeneExpressionHistogram.YAxisMode.Linear;
                    break;
                case "Logarithmic":
                    histogram.DesiredYAxisMode = GeneExpressionHistogram.YAxisMode.Logarithmic;
                    break;
            }

            histogram.RecreateHistogram();
        }

        [PunRPC]
        public void RecieveMessageChangeThreshold(int increment)
        {
            GeneExpressionHistogram histogram = referenceManager.legendManager.geneExpressionHistogram;
            histogram.TallestBarsToSkip += increment;
            histogram.RecreateHistogram();
        }

        #endregion

        #region Coloring

        [PunRPC]
        public void RecieveMessageColorGraphsByGene(string geneName)
        {
            CellexalLog.Log("Recieved message to color all graphs by " + geneName);
            referenceManager.cellManager
                .ColorGraphsByGene(geneName); //, referenceManager.graphManager.GeneExpressionColoringMethod);
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
            CellexalLog.Log("Recieved message to " + (toggle ? "toggle" : "untoggle") + " all graphs by attribute " +
                            attributeType);
            referenceManager.cellManager.ColorByAttribute(attributeType, toggle);
            var attributeButton = referenceManager.attributeSubMenu.FindButton(attributeType);
            attributeButton.ToggleOutline(toggle);
            attributeButton.GetComponent<ColorByAttributeButton>().colored = toggle;
        }

        [PunRPC]
        public void RecieveMessageColorByIndex(string indexName)
        {
            CellexalLog.Log("Recieved message to color all graphs by index " + indexName);
            referenceManager.cellManager.ColorByIndex(indexName);
            referenceManager.indexMenu.FindButton(indexName).GetComponent<ColorByIndexButton>().TurnOff();
        }

        [PunRPC]
        public void RecieveMessageToggleTransparency(bool toggle)
        {
            CellexalLog.Log("Recieved message to toggle transparency on gps" + toggle);
            referenceManager.graphManager.ToggleGraphPointTransparency(toggle);
            referenceManager.mainMenu.GetComponentInChildren<ToggleTransparencyButton>().Toggle = !toggle;
        }

        [PunRPC]
        public void RecieveMessageAddCullingCube()
        {
            CellexalLog.Log("Recieved message to add culling cube");
            referenceManager.cullingFilterManager.AddCube();
        }

        [PunRPC]
        public void RecieveMessageRemoveCullingCube()
        {
            CellexalLog.Log("Recieved message to remove culling cube");
            referenceManager.cullingFilterManager.RemoveCube();
        }

        [PunRPC]
        public void RecieveMessageGenerateRandomColors(int n)
        {
            CellexalLog.Log(message: "Recieved message to generate " + n + " random colors");
            referenceManager.settingsMenu.GetComponent<ColormapManager>().DoGenerateRandomColors(n, true);
        }

        [PunRPC]
        public void RecieveMessageGenerateRainbowColors(int n)
        {
            CellexalLog.Log(message: "Recieved message to generate " + n + " rainbow colors");
            referenceManager.settingsMenu.GetComponent<ColormapManager>().DoGenerateRainbowColors(n, true);
        }

        [PunRPC]
        public void RecieveMessageHighlightCells(int group, bool highlight)
        {
            Cell[] cellsToHighlight;
            if (group != -1)
            {
                cellsToHighlight = referenceManager.cellManager.GetCells(group);
            }
            else
            {
                cellsToHighlight = referenceManager.cellManager.GetCells();
            }

            referenceManager.cellManager.HighlightCells(cellsToHighlight, highlight);
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
            referenceManager.geneKeyboard.AddText(key, false);
        }

        [PunRPC]
        public void RecieveMessageKBackspaceKeyClicked()
        {
            referenceManager.geneKeyboard.BackSpace(false);
        }

        [PunRPC]
        public void RecieveMessageClearKeyClicked()
        {
            referenceManager.geneKeyboard.Clear(false);
        }

        [PunRPC]
        public void RecieveMessageSearchLockToggled(int index)
        {
            referenceManager.previousSearchesList.searchLocks[index].Click();
        }

        [PunRPC]
        public void RecieveMessageAddAnnotation(string annotation, int index)
        {
            CellexalLog.Log("Recieved message to add annotation: " + annotation);
            referenceManager.annotationManager.AddAnnotation(annotation, index);
        }

        [PunRPC]
        public void RecieveMessageExportAnnotations()
        {
            CellexalLog.Log("Recieved message to export annotations");
            referenceManager.annotationManager.DumpAnnotatedSelectionToTextFile();
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
        public void RecieveMessageMoveGraph(string moveGraphName, float posX, float posY, float posZ, float rotX,
            float rotY, float rotZ, float rotW, float scaleX, float scaleY, float scaleZ)
        {
            Graph g = referenceManager.graphManager.FindGraph(moveGraphName);
            SpatialGraph sg = referenceManager.graphManager.FindSpatialGraph(moveGraphName);
            if (g != null)
            {
                try
                {
                    g.transform.position = new Vector3(posX, posY, posZ);
                    g.transform.rotation = new Quaternion(rotX, rotY, rotZ, rotW);
                    g.transform.localScale = new Vector3(scaleX, scaleY, scaleZ);
                }
                catch (Exception e)
                {
                    CellexalLog.Log("Could not move graph - Error: " + e);
                }
            }
            else if (sg != null)
            {
                try
                {
                    sg.transform.position = new Vector3(posX, posY, posZ);
                    sg.transform.rotation = new Quaternion(rotX, rotY, rotZ, rotW);
                    sg.transform.localScale = new Vector3(scaleX, scaleY, scaleZ);
                }
                catch (Exception e)
                {
                    CellexalLog.Log("Could not move graph - Error: " + e);
                }
            }
        }

        [PunRPC]
        public void RecieveMessageGraphUngrabbed(string graphName,
            float posX, float posY, float posZ,
            float rotX, float rotY, float rotZ, float rotW,
            float velX, float velY, float velZ,
            float angVelX, float angVelY, float angVelZ)
        {
            Graph g = referenceManager.graphManager.FindGraph(graphName);
            if (g)
            {
                Rigidbody r = g.GetComponent<Rigidbody>();
                g.transform.position = new Vector3(posX, posY, posZ);
                g.transform.rotation = new Quaternion(rotX, rotY, rotZ, rotW);
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
            StartCoroutine(
                referenceManager.lineBundler.DrawLinesBetweenGraphPoints(referenceManager.selectionManager
                    .GetLastSelection()));
            //CellexalEvents.LinesBetweenGraphsDrawn.Invoke();
        }

        [PunRPC]
        public void RecieveMessageClearLinesBetweenGps()
        {
            CellexalLog.Log("Recieved message to clear lines between graph points");
            referenceManager.lineBundler.ClearLinesBetweenGraphPoints();
            //CellexalEvents.LinesBetweenGraphsCleared.Invoke();
        }

        [PunRPC]
        public void RecieveMessageBundleAllLines()
        {
            CellexalLog.Log("Recieved message to clear lines between graph points");
            referenceManager.lineBundler.BundleAllLines();
        }


        [PunRPC]
        public void RecieveMessageAddMarker(string indexName)
        {
            //var markerButton = GameObject.Find("/Main Menu/Attribute Menu/TabPrefab(Clone)/" + indexName);
            var markerButton = referenceManager.createFromMarkerMenu.FindButton(indexName);
            if (referenceManager.newGraphFromMarkers.markers.Count < 3 &&
                !referenceManager.newGraphFromMarkers.markers.Contains(indexName))
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
        //[PunRPC]
        //public void RecieveMessageSpatialGraphGrabbed(string sliceName, string graphName)
        //{
        //    foreach (SpatialGraph spatialGraph in referenceManager.graphManager.spatialGraphs)
        //    {
        //        if (spatialGraph.gameObject.name.Equals(graphName))
        //            spatialGraph.GetSlice(sliceName).ToggleGrabbing(true);
        //    }
        //}

        //[PunRPC]
        //public void RecieveMessageSpatialGraphUnGrabbed(string sliceName, string graphName)
        //{
        //    CellexalLog.Log("Recieved message to activate slices in spatial graph");
        //    foreach (SpatialGraph spatialGraph in referenceManager.graphManager.spatialGraphs)
        //    {
        //        if (spatialGraph.gameObject.name.Equals(graphName))
        //            spatialGraph.GetSlice(sliceName).ToggleGrabbing(false);
        //    }
        //}

        [PunRPC]
        public void RecieveMessageHighlightCluster(bool highlight, string graphName, int id)
        {
            Graph g = referenceManager.graphManager.FindGraph(graphName);
            if (g == null) return;
            PointCluster cluster = g.GetComponent<GraphBetweenGraphs>().GetCluster(id);
            if (cluster == null) return;
            cluster.Highlight(highlight);
        }

        [PunRPC]
        public void RecieveMessageToggleBundle(string graphName, int id)
        {
            Graph g = referenceManager.graphManager.FindGraph(graphName);
            if (g == null) return;
            PointCluster cluster = g.GetComponent<GraphBetweenGraphs>().GetCluster(id);
            if (cluster == null) return;
            cluster.RemakeLines(cluster.fromPointCluster);
        }

        #endregion

        #region Heatmaps

        [PunRPC]
        public void RecieveMessageMoveHeatmap(string heatmapName, float posX, float posY, float posZ, float rotX,
            float rotY, float rotZ, float rotW, float scaleX, float scaleY, float scaleZ)
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
        public void RecieveMessageHandleBoxSelection(string heatmapName, int hitx, int hity, int selectionStartX,
            int selectionStartY)
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
        public void RecieveMessageConfirmSelection(string heatmapName, int hitx, int hity, int selectionStartX,
            int selectionStartY)
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
                    CellexalLog.Log("Could not confirm selection - Error: " + e);
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
                    CellexalLog.Log("Could not move heatmap selection - Error: " + e);
                }
            }
        }

        [PunRPC]
        public void RecieveMessageMoveSelection(string heatmapName, int hitx, int hity, int selectedGroupLeft,
            int selectedGroupRight, int selectedGeneTop, int selectedGeneBottom)
        {
            Heatmap hm = referenceManager.heatmapGenerator.FindHeatmap(heatmapName);
            bool heatmapExists = hm != null;
            if (!heatmapExists) return;
            try
            {
                hm.GetComponent<HeatmapRaycast>().MoveSelection(hitx, hity, selectedGroupLeft, selectedGroupRight,
                    selectedGeneTop, selectedGeneBottom);
            }
            catch (Exception)
            {
                // CellexalLog.Log("Could not move heatmap - Error: " + e);
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
                catch (Exception)
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
        public void RecieveMessageCreateNewHeatmapFromSelection(string heatmapName, int selectedGroupLeft,
            int selectedGroupRight, int selectedGeneTop,
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
        public void RecieveMessageMoveNetwork(string networkName, float posX, float posY, float posZ, float rotX,
            float rotY, float rotZ, float rotW, float scaleX, float scaleY, float scaleZ)
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
        public void RecieveMessageNetworkUngrabbed(string networkName,
            float posX, float posY, float posZ,
            float rotX, float rotY, float rotZ, float rotW,
            float velX, float velY, float velZ,
            float angVelX, float angVelY, float angVelZ)
        {
            NetworkHandler nh = referenceManager.networkGenerator.FindNetworkHandler(networkName);
            if (nh)
            {
                nh.transform.position = new Vector3(posX, posY, posZ);
                nh.transform.rotation = new Quaternion(rotX, rotY, rotZ, rotW);
                Rigidbody r = nh.GetComponent<Rigidbody>();
                r.velocity = new Vector3(velX, velY, velZ);
                r.angularVelocity = new Vector3(angVelX, angVelY, angVelZ);
            }
        }

        [PunRPC]
        public void RecieveMessageEnlargeNetwork(string networkHandlerName, string networkCenterName)
        {
            CellexalLog.Log("Recieved message to enlarge network " + networkCenterName + " in handler " +
                            networkHandlerName);
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
            CellexalLog.Log("Recieved message to bring back network " + networkCenterName + " in handler " +
                            networkHandlerName);
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
        public void RecieveMessageMoveNetworkCenter(string networkHandlerName, string networkCenterName, float posX,
            float posY, float posZ, float rotX, float rotY, float rotZ, float rotW, float scaleX, float scaleY,
            float scaleZ)
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
        public void RecieveMessageNetworkCenterUngrabbed(string networkHandlerName, string networkCenterName,
            float posX, float posY, float posZ,
            float rotX, float rotY, float rotZ, float rotW,
            float velX, float velY, float velZ,
            float angVelX, float angVelY, float angVelZ)
        {
            NetworkHandler nh = referenceManager.networkGenerator.FindNetworkHandler(networkHandlerName);
            if (nh)
            {
                NetworkCenter nc = nh.FindNetworkCenter(networkCenterName);
                if (nc)
                {
                    nc.transform.position = new Vector3(posX, posY, posZ);
                    nc.transform.rotation = new Quaternion(rotX, rotY, rotZ, rotW);
                    Rigidbody r = nc.GetComponent<Rigidbody>();
                    r.velocity = new Vector3(velX, velY, velZ);
                    r.angularVelocity = new Vector3(angVelX, angVelY, angVelZ);
                }
            }
        }

        [PunRPC]
        public void RecieveMessageHighlightNetworkNode(string handlerName, string centerName, string geneName)
        {
            referenceManager.networkGenerator?.FindNetworkHandler(handlerName)?.FindNetworkCenter(centerName)?.HighlightNode(geneName, true);
        }

        [PunRPC]
        public void RecieveMessageUnhighlightNetworkNode(string handlerName, string centerName, string geneName)
        {
            referenceManager.networkGenerator?.FindNetworkHandler(handlerName)?.FindNetworkCenter(centerName)?.HighlightNode(geneName, false);
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
            if (g == null) return;
            g.HideGraph();
            referenceManager.minimizedObjectHandler.MinimizeObject(g.gameObject, graphName);
        }

        [PunRPC]
        public void RecieveMessageShowGraph(string graphName, string jailName)
        {
            Graph g = referenceManager.graphManager.FindGraph(graphName);
            if (g == null) return;
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
            if (nh == null) return;
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
                GameObject.Find(name)?.GetComponent<NetworkHandler>().DeleteNetwork();
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
                referenceManager.velocityGenerator.ReadVelocityFile(shorterFilePath, subGraphName);
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
            referenceManager.filterManager.ResetFilter( /*false*/);
        }

        #endregion

        #region Browser

        [PunRPC]
        public void RecieveMessageMoveBrowser(float posX, float posY, float posZ, float rotX, float rotY, float rotZ,
            float rotW, float scaleX, float scaleY, float scaleZ)
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