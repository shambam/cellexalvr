﻿using Photon;
using System;
using System.Collections;


using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// This class is responsible for passing commands that are about to be sent to a connected client.
/// </summary>
public class GameManager : Photon.PunBehaviour
{
    #region Public Properties

    public ReferenceManager referenceManager;
    static public GameManager Instance;
    [Tooltip("The prefab to use for representing the player")]
    public GameObject playerPrefab;
    public GameObject serverCoordinatorPrefab;

    private GraphManager graphManager;
    public CellManager cellManager;
    public SelectionToolHandler selectionToolHandler;
    public HeatmapGenerator heatmapGenerator;
    public NetworkGenerator networkGenerator;
    private ServerCoordinator serverCoordinator;
    private ServerCoordinator clientCoordinator;
    private bool multiplayer = false;

    #endregion
    private void Start()
    {
        graphManager = referenceManager.graphManager;
        cellManager = referenceManager.cellManager;
        selectionToolHandler = referenceManager.selectionToolHandler;
        heatmapGenerator = referenceManager.heatmapGenerator;
        networkGenerator = referenceManager.networkGenerator;

        Instance = this;
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
                PhotonNetwork.Instantiate(this.playerPrefab.name, new Vector3(0f, 5f, 0f), Quaternion.identity, 0);
                if (PhotonNetwork.isMasterClient)
                {
                    serverCoordinator = PhotonNetwork.Instantiate(this.serverCoordinatorPrefab.name, Vector3.zero, Quaternion.identity, 0).GetComponent<ServerCoordinator>();
                    // serverCoordinator.RegisterClient(this);
                }
                else
                {
                    PhotonNetwork.Instantiate("ClientCoordinator", Vector3.zero, Quaternion.identity, 0);

                    //GameObject.Find("ServerCoordinator").GetComponent<ServerCoordinator>().RegisterClient(this);
                }
            }
            else
            {
                Debug.Log("Ignoring scene load for " + SceneManager.GetActiveScene().name);
            }
        }
    }


    #region Photon Messages

    #region Inform methods
    // these methods are called when a client wants to inform all other clients that something has happened

    public void InformReadFolder(string path)
    {
        if (!multiplayer) return;
        CellExAlLog.Log("Informing clients to read folder " + path);
        if (PhotonNetwork.isMasterClient)
        {
            clientCoordinator.photonView.RPC("SendReadFolder", PhotonTargets.Others, path);
        }
        else
        {
            serverCoordinator.photonView.RPC("SendReadFolder", PhotonTargets.Others, path);
        }
    }

    public void InformGraphPointChangedColor(string graphname, string label, Color color)
    {
        if (!multiplayer) return;
        if (PhotonNetwork.isMasterClient)
        {
            clientCoordinator.photonView.RPC("SendGraphpointChangedColor", PhotonTargets.Others, graphname, label, color.r, color.g, color.b);
        }
        else
        {
            serverCoordinator.photonView.RPC("SendGraphpointChangedColor", PhotonTargets.Others, graphname, label, color.r, color.g, color.b);
        }
    }

    public void InformColorGraphsByGene(string geneName)
    {
        if (!multiplayer) return;
        CellExAlLog.Log("Informing clients to color graphs by " + geneName);
        if (PhotonNetwork.isMasterClient)
        {
            clientCoordinator.photonView.RPC("SendColorGraphsByGene", PhotonTargets.Others, geneName);
        }
        else
        {
            serverCoordinator.photonView.RPC("SendColorGraphsByGene", PhotonTargets.Others, geneName);
        }
    }

    public void InformConfirmSelection()
    {
        if (!multiplayer) return;
        CellExAlLog.Log("Informing clients to confirm selection");
        if (PhotonNetwork.isMasterClient)
        {
            clientCoordinator.photonView.RPC("SendConfirmSelection", PhotonTargets.Others);
        }
        else
        {
            serverCoordinator.photonView.RPC("SendConfirmSelection", PhotonTargets.Others);
        }
    }

    public void InformDrawLine(float r, float g, float b, float[] xcoords, float[] ycoords, float[] zcoords)
    {
        if (!multiplayer) return;
        CellExAlLog.Log("Informing clients to draw line with " + xcoords.Length);
        if (PhotonNetwork.isMasterClient)
        {
            clientCoordinator.photonView.RPC("SendDrawLine", PhotonTargets.Others, r, g, b, xcoords, ycoords, zcoords);
        }
        else
        {
            serverCoordinator.photonView.RPC("SendDrawLine", PhotonTargets.Others, r, g, b, xcoords, ycoords, zcoords);
        }
    }

    public void InformMoveGraph(string moveGraphName, Vector3 pos, Quaternion rot)
    {
        if (!multiplayer) return;
        if (PhotonNetwork.isMasterClient)
        {
            clientCoordinator.photonView.RPC("SendMoveGraph", PhotonTargets.Others, moveGraphName, pos.x, pos.y, pos.z, rot.x, rot.y, rot.z, rot.w);
        }
        else
        {
            serverCoordinator.photonView.RPC("SendMoveGraph", PhotonTargets.Others, moveGraphName, pos.x, pos.y, pos.z, rot.x, rot.y, rot.z, rot.w);
        }
    }

    public void InformMoveHeatmap(string moveHeatmapName, Vector3 pos, Quaternion rot)
    {
        if (!multiplayer) return;
        if (PhotonNetwork.isMasterClient)
        {
            clientCoordinator.photonView.RPC("SendMoveHeatmap", PhotonTargets.Others, moveHeatmapName, pos.x, pos.y, pos.z, rot.x, rot.y, rot.z, rot.w);
        }
        else
        {
            serverCoordinator.photonView.RPC("SendMoveHeatmap", PhotonTargets.Others, moveHeatmapName, pos.x, pos.y, pos.z, rot.x, rot.y, rot.z, rot.w);
        }
    }

    public void InformSelectedAdd(string graphName, string label)
    {
        if (!multiplayer) return;
        if (PhotonNetwork.isMasterClient)
        {
            clientCoordinator.photonView.RPC("SendAddSelect", PhotonTargets.Others, graphName, label);
        }
        else
        {
            serverCoordinator.photonView.RPC("SendAddSelect", PhotonTargets.Others, graphName, label);
        }
    }

    public void InformCreateHeatmap()
    {
        if (!multiplayer) return;
        CellExAlLog.Log("Informing clients to create heatmap");
        if (PhotonNetwork.isMasterClient)
        {
            clientCoordinator.photonView.RPC("SendCreateHeatmap", PhotonTargets.Others);
        }
        else
        {
            serverCoordinator.photonView.RPC("SendCreateHeatmap", PhotonTargets.Others);
        }
    }

    public void InformGenerateNetworks()
    {
        if (!multiplayer) return;
        CellExAlLog.Log("Informing clients to generate networks");
        if (PhotonNetwork.isMasterClient)
        {
            clientCoordinator.photonView.RPC("SendGenerateNetworks", PhotonTargets.Others);
        }
        else
        {
            serverCoordinator.photonView.RPC("SendGenerateNetworks", PhotonTargets.Others);
        }
    }

    public void InformMoveNetwork(string moveNetworkName, Vector3 pos, Quaternion rot)
    {
        if (!multiplayer) return;
        if (PhotonNetwork.isMasterClient)
        {
            clientCoordinator.photonView.RPC("SendMoveNetwork", PhotonTargets.Others, moveNetworkName, pos.x, pos.y, pos.z, rot.x, rot.y, rot.z, rot.w);
        }
        else
        {
            serverCoordinator.photonView.RPC("SendMoveNetwork", PhotonTargets.Others, moveNetworkName, pos.x, pos.y, pos.z, rot.x, rot.y, rot.z, rot.w);
        }
    }

    public void InformEnlargeNetwork(string networkHandlerName, string networkName)
    {
        if (!multiplayer) return;
        CellExAlLog.Log("Informing clients to enalarge network " + networkName + " in handler + " + networkHandlerName);
        if (PhotonNetwork.isMasterClient)
        {
            clientCoordinator.photonView.RPC("SendEnlargeNetwork", PhotonTargets.Others, networkHandlerName, networkName);
        }
        else
        {
            serverCoordinator.photonView.RPC("SendEnlargeNetwork", PhotonTargets.Others, networkHandlerName, networkName);
        }
    }

    public void InformBringBackNetwork(string networkHandlerName, string networkName)
    {
        if (!multiplayer) return;
        CellExAlLog.Log("Informing clients to bring back network " + networkName + " in handler " + networkHandlerName);
        if (PhotonNetwork.isMasterClient)
        {
            clientCoordinator.photonView.RPC("SendBringBackNetwork", PhotonTargets.Others, networkHandlerName, networkName);
        }
        else
        {
            serverCoordinator.photonView.RPC("SendBringBackNetwork", PhotonTargets.Others, networkHandlerName, networkName);
        }
    }

    public void InformMoveNetworkCenter(string networkHandlerName, string networkCenterName, Vector3 pos, Quaternion rot, Vector3 scale)
    {
        if (!multiplayer) return;
        if (PhotonNetwork.isMasterClient)
        {
            clientCoordinator.photonView.RPC("SendMoveNetworkCenter", PhotonTargets.Others, networkHandlerName, networkCenterName, pos.x, pos.y, pos.z, rot.x, rot.y, rot.z, rot.w, scale.x, scale.y, scale.z);
        }
        else
        {
            serverCoordinator.photonView.RPC("SendMoveNetworkCenter", PhotonTargets.Others, networkHandlerName, networkCenterName, pos.x, pos.y, pos.z, rot.x, rot.y, rot.z, rot.w, scale.x, scale.y, scale.z);
        }
    }

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
        CellExAlLog.Log("A client connected to our server");

        Debug.Log("MASTER JOINED ROOM");
        //LoadArena();
        StartCoroutine(FindClientCoordinator());

    }
    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        multiplayer = true;
        Debug.Log("CLIENT JOINED ROOM");
        CellExAlLog.Log("We joined a server");
        StartCoroutine(FindServerCoordinator());
    }

    private IEnumerator FindClientCoordinator()
    {
        yield return new WaitForSeconds(1f);
        clientCoordinator = GameObject.Find("ClientCoordinator(Clone)").GetComponent<ServerCoordinator>();
        Debug.Log("Client Coordinator Found");
     }

    private IEnumerator FindServerCoordinator()
    {
        yield return new WaitForSeconds(1f);
        serverCoordinator = GameObject.Find("ServerCoordinator(Clone)").GetComponent<ServerCoordinator>();
        Debug.Log("Server Coordinator Found");
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
        PhotonNetwork.LoadLevel("PUN_vrjeans_scene");
    }


    #endregion
}
