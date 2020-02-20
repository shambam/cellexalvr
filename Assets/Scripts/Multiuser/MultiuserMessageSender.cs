using CellexalVR.General;
using CellexalVR.Menu;
using Photon;
using System;
using System.Collections;


using UnityEngine;
using UnityEngine.SceneManagement;

namespace CellexalVR.Multiuser
{
    /// <summary>
    /// This class is responsible for passing commands that are about to be sent to a connected client.
    /// It also spawns the players and objects handling the client-server coordination.
    /// </summary>
    public class MultiuserMessageSender : Photon.PunBehaviour
    {
        #region Public Properties

        public ReferenceManager referenceManager;
        static public MultiuserMessageSender Instance;
        [Tooltip("The prefab to use for representing the player")]
        public GameObject playerPrefab;
        public GameObject spectatorPrefab;
        public GameObject ghostPrefab;
        public GameObject serverCoordinatorPrefab;
        public GameObject waitingCanvas;
        public GameObject spectatorRig;
        public GameObject VRRig;
        public bool avatarMenuActive;

        private MultiuserMessageReciever coordinator;
        public bool multiplayer = false;


        #endregion


        private void OnValidate()
        {
            if (gameObject.scene.IsValid())
            {
                referenceManager = GameObject.Find("InputReader").GetComponent<ReferenceManager>();
            }
        }

        private void Start()
        {
            Instance = this;
            waitingCanvas = referenceManager.screenCanvas.gameObject;
            spectatorRig = referenceManager.spectatorRig;
            VRRig = referenceManager.VRRig;

            if (!PhotonNetwork.connected) return;
            if (playerPrefab == null)
            {
                Debug.LogError("<Color=Red><a>Missing</a></Color> playerPrefab Reference. Please set it up in GameObject 'Game Manager'", this);
            }
            else
            {
                Debug.Log("We are Instantiating LocalPlayer from " + SceneManager.GetActiveScene().name);
                // we're in a room. spawn a character for the local player. it gets synced by using PhotonNetwork.Instantiate
                if (PlayerManager.LocalPlayerInstance == null)
                {
                    Debug.Log("We are Instantiating LocalPlayer from " + SceneManager.GetActiveScene().name);
                    // we're in a room. spawn a character for the local player. it gets synced by using PhotonNetwork.Instantiate

                    // If the user checked the spectator option the name prefix will be Spectator. If spectator spawn an invisible avatar instead.
                    GameObject player = new GameObject();
                    if (CrossSceneInformation.Spectator)
                    {
                        player = PhotonNetwork.Instantiate(this.spectatorPrefab.name, new Vector3(0f, 5f, 0f), Quaternion.identity, 0);
                        Destroy(VRRig);
                        spectatorRig.SetActive(true);
                    }

                    else if (CrossSceneInformation.Ghost)
                    {
                        Destroy(spectatorRig);
                        player = PhotonNetwork.Instantiate(this.ghostPrefab.name, new Vector3(0f, 5f, 0f), Quaternion.identity, 0);
                        Destroy(referenceManager.leftControllerScriptAlias);
                        Destroy(referenceManager.rightControllerScriptAlias);
                        referenceManager.leftController.GetComponent<MenuToggler>().menuCube.SetActive(false);
                        Destroy(referenceManager.leftController.GetComponent<MenuToggler>());
                    }

                    else if (!CrossSceneInformation.Spectator)
                    {
                        Destroy(spectatorRig);
                        player = PhotonNetwork.Instantiate(this.playerPrefab.name, new Vector3(0f, 5f, 0f), Quaternion.identity, 0);
                    }

                    player.gameObject.name = PhotonNetwork.playerName;



                    if (PhotonNetwork.isMasterClient)
                    {
                        coordinator = PhotonNetwork.Instantiate(this.serverCoordinatorPrefab.name, Vector3.zero, Quaternion.identity, 0).GetComponent<MultiuserMessageReciever>();

                    }
                    if (!PhotonNetwork.isMasterClient)
                    {
                        coordinator = PhotonNetwork.Instantiate("MultiuserMessageReciever", Vector3.zero, Quaternion.identity, 0).GetComponent<MultiuserMessageReciever>();
                    }
                }
                else
                {
                    Debug.Log("Ignoring scene load for " + SceneManager.GetActiveScene().name);
                }
            }
        }

        private void Update()
        {
            if (coordinator == null && multiplayer)
            {
                coordinator = GameObject.Find("MultiuserMessageReciever(Clone)").GetComponent<MultiuserMessageReciever>();
            }

        }

        #region Photon Messages

        #region Inform methods
        // these methods are called when a client wants to inform all other clients that something has happened

        #region Loading
        public void SendMessageReadFolder(string path)
        {
            if (!multiplayer) return;
            CellexalLog.Log("Informing clients to read folder " + path);
            coordinator.photonView.RPC("RecieveMessageReadFolder", PhotonTargets.Others, path);
        }

        public void SendMessageSynchConfig(byte[] data)
        {
            if (!multiplayer) return;
            CellexalLog.Log("Informing clients to synch relevant parts of config");
            coordinator.photonView.RPC("RecieveMessageSynchConfig", PhotonTargets.Others, data);
        }

        public void SendMessageLoadingMenu(bool delete)
        {
            if (!multiplayer) return;
            print("Sending to client to reset folders");
            coordinator.photonView.RPC("RecieveMessageLoadingMenu", PhotonTargets.Others, delete);
        }
        #endregion

        #region Interaction
        public void SendMessageEnableColliders(string name)
        {
            if (!multiplayer) return;
            coordinator.photonView.RPC("RecieveMessageEnableColliders", PhotonTargets.Others, name);
        }

        public void SendMessageDisableColliders(string name)
        {
            if (!multiplayer) return;
            coordinator.photonView.RPC("RecieveMessageDisableColliders", PhotonTargets.Others, name);
        }

        #endregion

        #region Coloring
        public void SendMessageColorGraphsByGene(string geneName)
        {
            if (!multiplayer) return;
            CellexalLog.Log("Informing clients to color graphs by " + geneName);
            Debug.Log("Informing clients to color graphs by " + geneName);
            coordinator.photonView.RPC("RecieveMessageColorGraphsByGene", PhotonTargets.Others, geneName);

        }

        public void SendMessageColoringMethodChanged(int newMode)
        {
            if (!multiplayer) return;
            CellexalLog.Log("Informing clients to change coloring mode to " + newMode);
            coordinator.photonView.RPC("RecieveMessageColoringMethodChanged", PhotonTargets.Others, newMode);
        }

        //public void SendMessageColorGraphByPreviousExpression(string geneName)
        //{
        //    if (!multiplayer) return;
        //    CellexalLog.Log("Informing clients to color graphs by previous gene " + geneName);
        //    coordinator.photonView.RPC("RecieveMessageColorGraphsByPreviousExpression", PhotonTargets.Others, geneName);
        //}

        public void SendMessageColorByAttribute(string attributeType, bool colored)
        {
            if (!multiplayer) return;
            CellexalLog.Log("Informing clients to color graphs by attribute " + attributeType);
            coordinator.photonView.RPC("RecieveMessageColorByAttribute", PhotonTargets.Others, attributeType, colored);
        }

        public void SendMessageColorByIndex(string indexName)
        {
            if (!multiplayer) return;
            CellexalLog.Log("Informing clients to color graphs by index " + indexName);
            coordinator.photonView.RPC("RecieveMessageColorByIndex", PhotonTargets.Others, indexName);
        }

        public void SendMessageRecolorSelectionPoints()
        {
            if (!multiplayer) return;
            CellexalLog.Log("Informing clients to recolor color graphs by current selection");
            coordinator.photonView.RPC("RecieveMessageRecolorSelectionPoints", PhotonTargets.Others);
        }
        public void SendMessageToggleTransparency(bool toggle)
        {
            if (!multiplayer) return;
            CellexalLog.Log("Informing clients to clear expression colours");
            coordinator.photonView.RPC("RecieveMessageToggleTransparency", PhotonTargets.Others, toggle);
        }

        #endregion

        #region Keyboard

        public void SendMessageActivateKeyboard(bool activate)
        {
            if (!multiplayer) return;
            coordinator.photonView.RPC("RecieveMessageActivateKeyboard", PhotonTargets.Others, activate);
        }

        public void SendMessageKeyClicked(string value)
        {
            if (!multiplayer) return;
            CellexalLog.Log("Informing clients that " + value + " was clicked");
            coordinator.photonView.RPC("RecieveMessageKeyClicked", PhotonTargets.Others, value);
        }

        public void SendMessageBackspaceKeyClicked()
        {
            if (!multiplayer) return;
            CellexalLog.Log("Informing clients that backspace was clicked");
            coordinator.photonView.RPC("RecieveMessageKBackspaceKeyClicked", PhotonTargets.Others);
        }

        public void SendMessageClearKeyClicked()
        {
            if (!multiplayer) return;
            CellexalLog.Log("Informing clients that clear was clicked");
            coordinator.photonView.RPC("RecieveMessageClearKeyClicked", PhotonTargets.Others);
        }

        public void SendMessageSearchLockToggled(int index)
        {
            if (!multiplayer) return;
            CellexalLog.Log("Informing clients to toggle lock number " + index);
            coordinator.photonView.RPC("RecieveMessageSearchLockToggled", PhotonTargets.Others, index);
        }

        public void SendMessageAddAnnotation(string annotation, int index)
        {
            if (!multiplayer) return;
            CellexalLog.Log("Informing clients to add annotation: " + annotation);
            coordinator.photonView.RPC("RecieveMessageAddAnnotation", PhotonTargets.Others, annotation, index);
        }

        public void SendMessageExportAnnotations()
        {
            if (!multiplayer) return;
            CellexalLog.Log("Informing clients to export annotations");
            coordinator.photonView.RPC("RecieveMessageExportAnnotations", PhotonTargets.Others);
        }

        public void SendMessageClearExpressionColours()
        {
            if (!multiplayer) return;
            CellexalLog.Log("Informing clients to clear expression colours");
            coordinator.photonView.RPC("RecieveMessageClearExpressionColours", PhotonTargets.Others);
        }


        public void SendMessageCalculateCorrelatedGenes(string geneName)
        {
            if (!multiplayer) return;
            CellexalLog.Log("Informing clients to calculate genes correlated to " + geneName);
            coordinator.photonView.RPC("RecieveMessageCalculateCorrelatedGenes", PhotonTargets.Others, geneName);
        }

        #endregion

        #region Selection
        public void SendMessageConfirmSelection()
        {
            if (!multiplayer) return;
            CellexalLog.Log("Informing clients to confirm selection");
            coordinator.photonView.RPC("RecieveMessageConfirmSelection", PhotonTargets.Others);
        }

        public void SendMessageSelectedAdd(string graphName, string label, int newGroup, Color color)
        {
            if (!multiplayer) return;
            coordinator.photonView.RPC("RecieveMessageAddSelect", PhotonTargets.Others, graphName, label, newGroup, color.r, color.g, color.b);
        }

        public void SendMessageCubeColoured(string graphName, string label, int newGroup, Color color)
        {
            if (!multiplayer) return;
            //Debug.Log("Informing clients to colour selected cube");
            coordinator.photonView.RPC("RecieveMessageCubeColoured", PhotonTargets.Others, graphName, label, newGroup, color.r, color.g, color.b);
        }

        public void SendMessageGoBackOneColor()
        {
            if (!multiplayer) return;
            //Debug.Log("Informing clients to undo last color");
            coordinator.photonView.RPC("RecieveMessageGoBackOneColor", PhotonTargets.Others);
        }

        public void SendMessageGoBackSteps(int k)
        {
            if (!multiplayer) return;
            //Debug.Log("Informing clients to undo last color");
            coordinator.photonView.RPC("RecieveMessageGoBackSteps", PhotonTargets.Others, k);
        }

        public void SendMessageCancelSelection()
        {
            if (!multiplayer) return;
            //Debug.Log("Informing clients to undo last color");
            coordinator.photonView.RPC("RecieveMessageCancelSelection", PhotonTargets.Others);
        }

        public void SendMessageRedoOneColor()
        {
            if (!multiplayer) return;
            //Debug.Log("Informing clients to undo last color");
            coordinator.photonView.RPC("RecieveMessageRedoOneColor", PhotonTargets.Others);
        }

        public void SendMessageRedoSteps(int k)
        {
            if (!multiplayer) return;
            //Debug.Log("Informing clients to undo last color");
            coordinator.photonView.RPC("RecieveMessageRedoSteps", PhotonTargets.Others, k);
        }

        public void SendMessageRemoveCells()
        {
            if (!multiplayer) return;
            //Debug.Log("Informing clients to remove selected cells");
            coordinator.photonView.RPC("RecieveMessageRemoveCells", PhotonTargets.Others);
        }


        #endregion

        #region Draw tool
        public void SendMessageDrawLine(float r, float g, float b, float[] xcoords, float[] ycoords, float[] zcoords)
        {
            if (!multiplayer) return;
            CellexalLog.Log("Informing clients to draw line with " + xcoords.Length);
            coordinator.photonView.RPC("RecieveMessageDrawLine", PhotonTargets.Others, r, g, b, xcoords, ycoords, zcoords);
        }

        public void SendMessageClearAllLines()
        {
            if (!multiplayer) return;
            CellexalLog.Log("Informing clients to clear all lines");
            coordinator.photonView.RPC("RecieveMessageClearAllLines", PhotonTargets.Others);
        }

        public void SendMessageClearLastLine()
        {
            if (!multiplayer) return;
            CellexalLog.Log("Informing clients to clear last line");
            coordinator.photonView.RPC("RecieveMessageClearLastLine", PhotonTargets.Others);
        }

        public void SendMessageClearAllLinesWithColor(Color color)
        {
            if (!multiplayer) return;
            CellexalLog.Log("Informing clients to clear all lines with color: " + color);
            coordinator.photonView.RPC("RecieveMessageClearLinesWithColor", PhotonTargets.Others, color.r, color.g, color.b);
        }
        #endregion

        #region Graphs
        public void SendMessageMoveGraph(string moveGraphName, Vector3 pos, Quaternion rot, Vector3 scale)
        {
            if (!multiplayer) return;
            coordinator.photonView.RPC("RecieveMessageMoveGraph", PhotonTargets.Others, moveGraphName, pos.x, pos.y, pos.z, rot.x, rot.y, rot.z, rot.w, scale.x, scale.y, scale.z);
        }

        public void SendMessageGraphUngrabbed(string moveGraphName, Vector3 vel, Vector3 angVel)
        {
            if (!multiplayer) return;
            coordinator.photonView.RPC("RecieveMessageGraphUngrabbed", PhotonTargets.Others, moveGraphName, vel.x, vel.y, vel.z, angVel.x, angVel.y, angVel.z);
        }

        public void SendMessageToggleGrabbable(string name, bool b)
        {
            if (!multiplayer) return;
            coordinator.photonView.RPC("RecieveMessageToggleGrabbable", PhotonTargets.Others, name, b);
        }

        public void SendMessageResetGraphColor()
        {
            if (!multiplayer) return;
            coordinator.photonView.RPC("RecieveMessageResetGraph", PhotonTargets.Others);
        }

        public void SendMessageResetGraphPosition()
        {
            if (!multiplayer) return;
            coordinator.photonView.RPC("RecieveMessageResetGraphPosition", PhotonTargets.Others);
        }

        public void SendMessageDrawLinesBetweenGps(bool toggle = false)
        {
            if (!multiplayer) return;
            coordinator.photonView.RPC("RecieveMessageDrawLinesBetweenGps", PhotonTargets.Others, toggle);
        }

        public void SendMessageBundleAllLines()
        {
            if (!multiplayer) return;
            coordinator.photonView.RPC("RecieveMessageBundleAllLines", PhotonTargets.Others);
        }
        public void SendMessageClearLinesBetweenGps()
        {
            if (!multiplayer) return;
            coordinator.photonView.RPC("RecieveMessageClearLinesBetweenGps", PhotonTargets.Others);
        }

        public void SendMessageAddMarker(string indexName)
        {
            if (!multiplayer) return;
            coordinator.photonView.RPC("RecieveMessageAddMarker", PhotonTargets.Others, indexName);
        }

        public void SendMessageCreateMarkerGraph()
        {
            if (!multiplayer) return;
            coordinator.photonView.RPC("RecieveMessageCreateMarkerGraph", PhotonTargets.Others);
        }

        public void SendMessageCreateAttributeGraph()
        {
            if (!multiplayer) return;
            coordinator.photonView.RPC("RecieveMessageCreateAttributeGraph", PhotonTargets.Others);
        }

        public void SendMessageActivateSlices()
        {
            if (!multiplayer) return;
            coordinator.photonView.RPC("RecieveMessageActivateSlices", PhotonTargets.Others);
        }
        public void SendMessageSpatialGraphGrabbed(string sliceName, string graphName)
        {
            if (!multiplayer) return;
            coordinator.photonView.RPC("RecieveMessageSpatialGraphGrabbed", PhotonTargets.Others, sliceName, graphName);
        }
        public void SendMessageSpatialGraphUnGrabbed(string sliceName, string graphName)
        {
            if (!multiplayer) return;
            coordinator.photonView.RPC("RecieveMessageSpatialGraphUnGrabbed", PhotonTargets.Others, sliceName, graphName);
        }
        #endregion

        #region Heatmaps
        public void SendMessageMoveHeatmap(string moveHeatmapName, Vector3 pos, Quaternion rot, Vector3 scale)
        {
            if (!multiplayer) return;
            coordinator.photonView.RPC("RecieveMessageMoveHeatmap", PhotonTargets.Others, moveHeatmapName, pos.x, pos.y, pos.z, rot.x, rot.y, rot.z, rot.w, scale.x, scale.y, scale.z);
        }


        public void SendMessageCreateHeatmap(string hmName)
        {
            if (!multiplayer) return;
            CellexalLog.Log("Informing clients to create heatmap");
            coordinator.photonView.RPC("RecieveMessageCreateHeatmap", PhotonTargets.Others, hmName);
        }

        public void SendMessageHandleBoxSelection(string heatmapName, int hitx, int hity, int selectionStartX, int selectionStartY)
        {
            if (!multiplayer) return;
            coordinator.photonView.RPC("RecieveMessageHandleBoxSelection", PhotonTargets.Others, heatmapName, hitx, hity, selectionStartX, selectionStartY);
        }

        public void SendMessageConfirmSelection(string heatmapName, int hitx, int hity, int selectionStartX, int selectionStartY)
        {
            if (!multiplayer) return;
            coordinator.photonView.RPC("RecieveMessageConfirmSelection", PhotonTargets.Others, heatmapName, hitx, hity, selectionStartX, selectionStartY);
        }

        public void SendMessageHandleMovingSelection(string heatmapName, int hitx, int hity)
        {
            if (!multiplayer) return;
            coordinator.photonView.RPC("RecieveMessageHandleMovingSelection", PhotonTargets.Others, heatmapName, hitx, hity);
        }

        public void SendMessageMoveSelection(string heatmapName, int hitx, int hity, int selectedGroupLeft, int selectedGroupRight, int selectedGeneTop, int selectedGeneBottom)
        {
            if (!multiplayer) return;
            coordinator.photonView.RPC("RecieveMessageMoveSelection", PhotonTargets.Others, heatmapName, hitx, hity, selectedGroupLeft, selectedGroupRight, selectedGeneTop, selectedGeneBottom);
        }

        public void SendMessageHandleHitHeatmap(string heatmapName, int hitx, int hity)
        {
            if (!multiplayer) return;
            coordinator.photonView.RPC("RecieveMessageHandleHitHeatmap", PhotonTargets.Others, heatmapName, hitx, hity);
        }

        public void SendMessageResetHeatmapHighlight(string heatmapName)
        {
            if (!multiplayer) return;
            coordinator.photonView.RPC("RecieveMessageResetHeatmapHighlight", PhotonTargets.Others, heatmapName);
        }

        public void SendMessageResetSelecting(string heatmapName)
        {
            if (!multiplayer) return;
            coordinator.photonView.RPC("RecieveMessageResetSelecting", PhotonTargets.Others, heatmapName);
        }

        public void SendMessageHandlePressDown(string heatmapName, int hitx, int hity)
        {
            if (!multiplayer) return;
            coordinator.photonView.RPC("RecieveMessageHandlePressDown", PhotonTargets.Others, heatmapName, hitx, hity);
        }

        public void SendMessageCreateNewHeatmapFromSelection(string heatmapName, int selectedGroupLeft, int selectedGroupRight, int selectedGeneTop,
            int selectedGeneBottom, float selectedBoxWidth, float selectedBoxHeight)
        {
            if (!multiplayer) return;
            coordinator.photonView.RPC("RecieveMessageCreateNewHeatmapFromSelection", PhotonTargets.Others, heatmapName, selectedGroupLeft, selectedGroupRight,
                selectedGeneTop, selectedGeneBottom, selectedBoxWidth, selectedBoxHeight);
        }

        public void SendMessageReorderByAttribute(string heatmapName, bool order)
        {
            if (!multiplayer) return;
            coordinator.photonView.RPC("RecieveMessageReorderByAttribute", PhotonTargets.Others, heatmapName, order);
        }

        public void SendMessageHandleHitGenesList(string heatmapName, int hity)
        {
            if (!multiplayer) return;
            coordinator.photonView.RPC("RecieveMessageHandleHitGenesList", PhotonTargets.Others, heatmapName, hity);
        }

        public void SendMessageHandleHitGroupingBar(string heatmapName, int hitx)
        {
            if (!multiplayer) return;
            coordinator.photonView.RPC("RecieveMessageHandleHitGroupingBar", PhotonTargets.Others, heatmapName, hitx);
        }

        public void SendMessageHandleHitAttributeBar(string heatmapName, int hitx)
        {
            if (!multiplayer) return;
            coordinator.photonView.RPC("RecieveMessageHandleHitAttributeBar", PhotonTargets.Others, heatmapName, hitx);
        }

        public void SendMessageResetInfoTexts(string heatmapName)
        {
            if (!multiplayer) return;
            coordinator.photonView.RPC("RecieveMessageResetInfoTexts", PhotonTargets.Others, heatmapName);
        }

        #endregion

        #region Networks
        public void SendMessageGenerateNetworks(int layoutSeed)
        {
            if (!multiplayer) return;
            CellexalLog.Log("Informing clients to generate networks");
            coordinator.photonView.RPC("RecieveMessageGenerateNetworks", PhotonTargets.Others, layoutSeed);
        }

        public void SendMessageMoveNetwork(string moveNetworkName, Vector3 pos, Quaternion rot, Vector3 scale)
        {
            if (!multiplayer) return;
            coordinator.photonView.RPC("RecieveMessageMoveNetwork", PhotonTargets.Others, moveNetworkName, pos.x, pos.y, pos.z, rot.x, rot.y, rot.z, rot.w, scale.x, scale.y, scale.z);
        }

        public void SendMessageNetworkUngrabbed(string networkName, Vector3 vel, Vector3 angVel)
        {
            if (!multiplayer) return;
            coordinator.photonView.RPC("RecieveMessageNetworkUngrabbed", PhotonTargets.Others, networkName, vel.x, vel.y, vel.z, angVel.x, angVel.y, angVel.z);
        }

        public void SendMessageEnlargeNetwork(string networkHandlerName, string networkName)
        {
            if (!multiplayer) return;
            CellexalLog.Log("Informing clients to enalarge network " + networkName + " in handler + " + networkHandlerName);
            coordinator.photonView.RPC("RecieveMessageEnlargeNetwork", PhotonTargets.Others, networkHandlerName, networkName);
        }


        public void SendMessageBringBackNetwork(string networkHandlerName, string networkName)
        {
            if (!multiplayer) return;
            CellexalLog.Log("Informing clients to bring back network " + networkName + " in handler " + networkHandlerName);
            coordinator.photonView.RPC("RecieveMessageBringBackNetwork", PhotonTargets.Others, networkHandlerName, networkName);
        }

        public void SendMessageSwitchNetworkLayout(int layout, string networkName, string networkHandlerName)
        {
            if (!multiplayer) return;
            CellexalLog.Log("Informing clients to switch network layout: " + layout);
            coordinator.photonView.RPC("RecieveMessageSwitchNetworkLayout", PhotonTargets.Others, layout, networkHandlerName, networkName);
        }

        public void SendMessageMoveNetworkCenter(string networkHandlerName, string networkCenterName, Vector3 pos, Quaternion rot, Vector3 scale)
        {
            if (!multiplayer) return;
            coordinator.photonView.RPC("RecieveMessageMoveNetworkCenter", PhotonTargets.Others, networkHandlerName, networkCenterName, pos.x, pos.y, pos.z, rot.x, rot.y, rot.z, rot.w, scale.x, scale.y, scale.z);
        }

        public void SendMessageNetworkCenterUngrabbed(string networkHandlerName, string networkCenterName, Vector3 vel, Vector3 angVel)
        {
            if (!multiplayer) return;
            coordinator.photonView.RPC("RecieveMessageNetworkCenterUngrabbed", PhotonTargets.Others, networkHandlerName, networkCenterName, vel.x, vel.y, vel.z, angVel.x, angVel.y, angVel.z);
        }

        public void SendMessageSetArcsVisible(bool toggleToState, string networkName)
        {
            if (!multiplayer) return;
            coordinator.photonView.RPC("RecieveMessageSetArcsVisible", PhotonTargets.Others, toggleToState, networkName);
        }

        public void SendMessageSetCombinedArcsVisible(bool toggleToState, string networkName)
        {
            if (!multiplayer) return;
            coordinator.photonView.RPC("RecieveMessageSetCombinedArcsVisible", PhotonTargets.Others, toggleToState, networkName);
        }
        #endregion

        #region Hide tool
        public void SendMessageMinimizeGraph(string graphName)
        {
            if (!multiplayer) return;
            coordinator.photonView.RPC("RecieveMessageMinimizeGraph", PhotonTargets.Others, graphName);
        }

        public void SendMessageShowGraph(string graphName, string jailName)
        {
            if (!multiplayer) return;
            coordinator.photonView.RPC("RecieveMessageShowGraph", PhotonTargets.Others, graphName, jailName);
        }

        public void SendMessageMinimizeNetwork(string networkName)
        {
            if (!multiplayer) return;
            coordinator.photonView.RPC("RecieveMessageMinimizeNetwork", PhotonTargets.Others, networkName);
        }

        public void SendMessageMinimizeHeatmap(string heatmapName)
        {
            if (!multiplayer) return;
            coordinator.photonView.RPC("RecieveMessageMinimizeHeatmap", PhotonTargets.Others, heatmapName);
        }

        public void SendMessageShowNetwork(string networkName, string jailName)
        {
            if (!multiplayer) return;
            coordinator.photonView.RPC("RecieveMessageShowNetwork", PhotonTargets.Others, networkName, jailName);
        }

        public void SendMessageShowHeatmap(string heatmapName, string jailName)
        {
            if (!multiplayer) return;
            coordinator.photonView.RPC("RecieveMessageShowHeatmap", PhotonTargets.Others, heatmapName, jailName);
        }


        #endregion

        #region Delete tool
        public void SendMessageDeleteObject(string objName, string objTag)
        {
            if (!multiplayer) return;
            coordinator.photonView.RPC("RecieveMessageDeleteObject", PhotonTargets.Others, objName, objTag);
        }
        #endregion

        #region Velocity
        public void SendMessageStartVelocity()
        {
            if (!multiplayer) return;
            coordinator.photonView.RPC("RecieveMessageStartVelocity", PhotonTargets.Others);
        }

        public void SendMessageStopVelocity()
        {
            if (!multiplayer) return;
            coordinator.photonView.RPC("RecieveMessageStopVelocity", PhotonTargets.Others);
        }

        public void SendMessageToggleGraphPoints()
        {
            if (!multiplayer) return;
            coordinator.photonView.RPC("RecieveMessageToggleGraphPoints", PhotonTargets.Others);
        }

        public void SendMessageConstantSynchedMode()
        {
            if (!multiplayer) return;
            coordinator.photonView.RPC("RecieveMessageConstantSynchedMode", PhotonTargets.Others);
        }

        public void SendMessageGraphPointColorsMode()
        {
            if (!multiplayer) return;
            coordinator.photonView.RPC("RecieveMessageGraphPointColorsMode", PhotonTargets.Others);
        }

        public void SendMessageChangeParticleMode()
        {
            if (!multiplayer) return;
            coordinator.photonView.RPC("RecieveMessageChangeParticleMode", PhotonTargets.Others);
        }

        public void SendMessageChangeFrequency(float amount)
        {
            if (!multiplayer) return;
            coordinator.photonView.RPC("RecieveMessageChangeFrequency", PhotonTargets.Others, amount);
        }

        public void SendMessageChangeThreshold(float amount)
        {
            if (!multiplayer) return;
            coordinator.photonView.RPC("RecieveMessageChangeThreshold", PhotonTargets.Others, amount);
        }

        public void SendMessageChangeSpeed(float amount)
        {
            if (!multiplayer) return;
            coordinator.photonView.RPC("RecieveMessageChangeSpeed", PhotonTargets.Others, amount);
        }

        public void SendMessageReadVelocityFile(string filePath, string subGraphName, bool activate)
        {
            if (!multiplayer) return;
            coordinator.photonView.RPC("RecieveMessageReadVelocityFile", PhotonTargets.Others, filePath, subGraphName, activate);
        }
        #endregion

        #region Filters
        public void SendMessageSetFilter(string filter)
        {
            if (!multiplayer) return;
            CellexalLog.Log("Informing clients to set filter to " + filter);
            coordinator.photonView.RPC("RecieveMessageSetFilter", PhotonTargets.Others, filter);
        }

        public void SendMessageResetFilter()
        {
            if (!multiplayer) return;
            CellexalLog.Log("Informing clients to reset filter");
            coordinator.photonView.RPC("RecieveMessageResetFilter", PhotonTargets.Others);
        }

        public void SendMessageRemoveCullingCube()
        {
            if (!multiplayer) return;
            CellexalLog.Log("Informing clients to remove culling cube");
            coordinator.photonView.RPC("RecieveMessageRemoveCullingCube", PhotonTargets.Others);
        }
        public void SendMessageAddCullingCube()
        {
            if (!multiplayer) return;
            CellexalLog.Log("Informing clients to add culling cube");
            coordinator.photonView.RPC("RecieveMessageAddCullingCube", PhotonTargets.Others);
        }
        #endregion

        #region Browser
        public void SendMessageMoveBrowser(Vector3 pos, Quaternion rot, Vector3 scale)
        {
            if (!multiplayer) return;
            coordinator.photonView.RPC("RecieveMessageMoveBrowser", PhotonTargets.Others, pos.x, pos.y, pos.z, rot.x, rot.y, rot.z, rot.w, scale.x, scale.y, scale.z);
        }

        public void SendMessageActivateBrowser(bool activate)
        {
            if (!multiplayer) return;
            CellexalLog.Log("Informing clients to toggle web browser");
            coordinator.photonView.RPC("RecieveMessageActivateBrowser", PhotonTargets.Others, activate);
        }

        public void SendMessageBrowserKeyClicked(string value)
        {
            if (!multiplayer) return;
            CellexalLog.Log("Informing clients that " + value + " was clicked");
            coordinator.photonView.RPC("RecieveMessageBrowserKeyClicked", PhotonTargets.Others, value);
        }

        public void SendMessageBrowserEnter()
        {
            if (!multiplayer) return;
            coordinator.photonView.RPC("RecieveMessageBrowserEnter", PhotonTargets.Others);
        }
        #endregion

        #endregion

        /// <summary>
        /// Called when the local player left the room. We need to load the launcher scene.
        /// </summary>
        public override void OnLeftRoom()
        {
            SceneManager.LoadScene(0);
        }

        public override void OnPhotonPlayerConnected(PhotonPlayer other)
        {
            multiplayer = true;
            Debug.Log("OnPhotonPlayerConnected() " + other.NickName); // not seen if you're the player connecting
            CellexalLog.Log("A client connected to our server");

            //Debug.Log("MASTER JOINED ROOM");
            //LoadArena();
            StartCoroutine(FindClientCoordinator());
            if (coordinator != null)
            {
                waitingCanvas.SetActive(false);
            }

        }
        public override void OnJoinedRoom()
        {
            base.OnJoinedRoom();
            multiplayer = true;
            CellexalLog.Log("We joined a server");
            StartCoroutine(FindServerCoordinator());
        }

        private IEnumerator FindClientCoordinator()
        {
            yield return new WaitForSeconds(2f);
            if ((coordinator = GameObject.Find("MultiuserMessageReciever(Clone)").GetComponent<MultiuserMessageReciever>()) == null)
            {
                StartCoroutine(FindClientCoordinator());
            }
            else
            {
                waitingCanvas.SetActive(false);
            }
        }

        private IEnumerator FindServerCoordinator()
        {
            yield return new WaitForSeconds(2f);
            if ((coordinator = GameObject.Find("MultiuserMessageReciever(Clone)").GetComponent<MultiuserMessageReciever>()) == null)
            {
                StartCoroutine(FindServerCoordinator());
            }
        }


        public override void OnPhotonPlayerDisconnected(PhotonPlayer other)
        {
            Debug.Log("OnPhotonPlayerDisconnected() " + other.NickName); // seen when other disconnects


            if (PhotonNetwork.isMasterClient)
            {
                Debug.Log("OnPhotonPlayerDisonnected isMasterClient " + PhotonNetwork.isMasterClient); // called before OnPhotonPlayerDisconnected


                //LoadArena();
            }
        }


        #endregion

        #region Public Methods


        public void LeaveRoom()
        {
            PhotonNetwork.LeaveRoom();
        }

        #endregion

        #region Private Methods

        void LoadArena()
        {
            if (!PhotonNetwork.isMasterClient)
            {
                Debug.LogError("PhotonNetwork : Trying to Load a level but we are not the master Client");
            }
            Debug.Log("PhotonNetwork : Loading Level : PUN_vrjeans_scene");
            PhotonNetwork.LoadLevel("vrjeans_scene1");
        }


        #endregion
    }
}
