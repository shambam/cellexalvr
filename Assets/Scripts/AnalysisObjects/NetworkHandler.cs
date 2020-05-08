using CellexalVR.AnalysisLogic;
using CellexalVR.General;
using CellexalVR.Interaction;
using CellexalVR.Multiuser;
using CellexalVR.Tools;
using System.Collections.Generic;
using CellexalVR.Extensions;
using CellexalVR.Menu.Buttons.Networks;
using CellexalVR.SceneObjects;
using UnityEngine;
using VRTK;

namespace CellexalVR.AnalysisObjects
{
    /// <summary>
    /// Represents a collection of networks placed on a skeleton like model of a graph.
    /// </summary>
    public class NetworkHandler : MonoBehaviour
    {
        public Material highlightMaterial;
        public Material normalMaterial;
        public List<NetworkCenter> Replacements { get; private set; }
        public bool removable;

        public bool removing = false;
        // public GameObject wirePrefab;

        public ReferenceManager referenceManager;
        //public string NetworkHandlerName { get; internal set; }

        public List<NetworkCenter> networks = new List<NetworkCenter>();

        private MultiuserMessageSender multiuserMessageSender;
        private MeshRenderer meshRenderer;
        private Material[] highlightedMaterials;
        private Material[] unhighlightedMaterials;

        // For minimization animation
        private bool minimize;
        private bool maximize;
        private bool delete;
        private float speed;
        private float targetMinScale;
        private float targetMaxScale;
        private float shrinkSpeed;
        private Vector3 originalPos;
        private Quaternion originalRot;
        private Vector3 originalScale;

        private bool createAnim;
        private Vector3 targetPos;
        private float targetScale;
        private float currentTime = 0;
        private float animationTime = 0.7f;

        private GameObject previewWire;
        private bool buttonClickedThisFrame;
        private ToggleArcsButton previouslyClickedButton;
        private readonly Color highlightColor = Definitions.TronColor;
        private Color standardColor;

        public int layoutApplied = 0;

        private void OnValidate()
        {
            if (gameObject.scene.IsValid())
            {
                referenceManager = GameObject.Find("InputReader").GetComponent<ReferenceManager>();
            }
        }

        private void Start()
        {
            speed = 1.5f;
            shrinkSpeed = 2f;
            targetMinScale = 0.05f;
            targetMaxScale = targetScale = 1f;
            targetPos = originalPos = originalScale = new Vector3();
            originalRot = new Quaternion();
            Replacements = new List<NetworkCenter>();
            referenceManager = GameObject.Find("InputReader").GetComponent<ReferenceManager>();
            GetComponent<NetworkHandlerInteract>().referenceManager = referenceManager;
            multiuserMessageSender = referenceManager.multiuserMessageSender;
            meshRenderer = GetComponent<MeshRenderer>();
            standardColor = GetComponent<LineRenderer>().startColor;
            // highlightedMaterials = new Material[] {meshRenderer.materials[0], new Material(highlightMaterial)};
            // highlightedMaterials[1].SetFloat("_Thickness", 0.2f);
            // unhighlightedMaterials = new Material[] {meshRenderer.materials[0], new Material(normalMaterial)};
            this.transform.localScale = Vector3.zero;
            CellexalEvents.ScriptFinished.AddListener(() => removable = true);
            CellexalEvents.ScriptRunning.AddListener(() => removable = false);
            referenceManager.graphManager.AddNetwork(this);
            // wirePrefab = referenceManager.filterManager.wirePrefab;
            // previewWire = Instantiate(referenceManager.arcsSubMenu.wirePrefab, this.transform);
            // previewWire.SetActive(false);
        }


        private void Update()
        {
            if (GetComponent<VRTK_InteractableObject>().IsGrabbed())
            {
                multiuserMessageSender.SendMessageMoveNetwork(name, transform.position, transform.rotation,
                    transform.localScale);
            }

            if (minimize)
            {
                Minimize();
            }

            if (maximize)
            {
                Maximize();
            }

            if (createAnim)
            {
                NetworkAnimation();
            }
        }

        /// <summary>
        /// Calculates the 2D layout of all networks.
        /// </summary>
        public void CalculateLayoutOnAllNetworks()
        {
            foreach (var network in networks)
            {
                network.CalculateLayout(NetworkCenter.Layout.TWO_D);
            }
        }

        /// <summary>
        /// Saves the arcs that go between the networks.
        /// </summary>
        /// <param name="keyPairs">An array of <see cref="NetworkReader.NetworkKeyPair"/>.</param>
        /// <param name="nodes">All nodes in all networks.</param>
        public void CreateArcs(ref NetworkReader.NetworkKeyPair[] keyPairs, ref Dictionary<string, NetworkNode> nodes)
        {
            // since the list is sorted in a smart way, all keypairs that share a key will be next to eachother
            var lastKey = new NetworkReader.NetworkKeyPair("", "", "", "", "");
            List<NetworkReader.NetworkKeyPair> lastNodes = new List<NetworkReader.NetworkKeyPair>();
            for (int i = 0; i < keyPairs.Length; ++i)
            {
                NetworkReader.NetworkKeyPair keypair = keyPairs[i];
                // if this keypair shares a key with the last keypair
                if (lastKey.key1 == keypair.key1 || lastKey.key1 == keypair.key2)
                {
                    // add arcs to all previous pairs that also shared a key
                    foreach (NetworkReader.NetworkKeyPair node in lastNodes)
                    {
                        var center = nodes[node.node1].Center;
                        center.AddArc(nodes[node.node1], nodes[node.node2], nodes[keypair.node1], nodes[keypair.node2]);
                    }
                }
                else
                {
                    // clear the list if this key did not match the last one
                    lastNodes.Clear();
                }

                lastNodes.Add(keypair);
                lastKey = keypair;
            }


            // copy the networks to an array
            NetworkCenter[] networkCenterArray = new NetworkCenter[networks.Count];
            int j = 0;
            foreach (NetworkCenter n in networks)
            {
                networkCenterArray[j++] = n;
            }

            // create the toggle arcs menu and its buttons
            referenceManager.arcsSubMenu.CreateToggleArcsButtons(networkCenterArray);

            List<int> arcsCombinedList = new List<int>();
            foreach (NetworkCenter network in networks)
            {
                var arcscombined = network.CreateCombinedArcs();
                arcsCombinedList.Add(arcscombined);
                // toggle the arcs off
                network.SetArcsVisible(false);
                network.SetCombinedArcsVisible(false);
                // Also add toggle arcs buttons. 
                GameObject newButton = Instantiate(referenceManager.arcsSubMenu.buttonPrefab, transform);
                newButton.layer = LayerMask.NameToLayer("EnvironmentButtonLayer");
                ToggleArcsButton toggleArcButton = newButton.GetComponent<ToggleArcsButton>();
                referenceManager.arcsSubMenu.toggleArcButtonList.Add(toggleArcButton);
                newButton.transform.localScale = network.transform.localScale / 6;
                newButton.transform.localPosition = network.transform.localPosition + new Vector3(0, 0.13f, 0);
                newButton.name = network.gameObject.name + "_ArcButton";
                newButton.gameObject.SetActive(true);
                newButton.GetComponent<Renderer>().enabled = true;
                newButton.GetComponent<Collider>().enabled = true;
                Color color = network.GetComponent<Renderer>().material.color;
                toggleArcButton.ButtonColor = color;
                toggleArcButton.SetNetwork(network);
            }

            // figure out how many combined arcs there are
            var max = 0;
            foreach (int i in arcsCombinedList)
            {
                if (max < i)
                    max = i;
            }

            // color all combined arcs
            foreach (NetworkCenter network in networks)
            {
                network.ColorCombinedArcs(max);
            }
        }

        /// <summary>
        /// Adds a networkcenter to this handler.
        /// </summary>
        /// <param name="network"> The networkcenter to add. </param>
        internal void AddNetwork(NetworkCenter network)
        {
            networks.Add(network);
            networks.RemoveAll(item => item == null);
            // print(referenceManager == null);
            // print((referenceManager.arcsSubMenu == null));
            // print((referenceManager.arcsSubMenu.buttonPrefab == null));
        }

        /// <summary>
        /// Toggles all renderers and colliders on for all networks on this convex hull.
        /// </summary>
        internal void ShowNetworks()
        {
            transform.position = referenceManager.leftController.transform.position;
            foreach (Renderer r in GetComponentsInChildren<Renderer>())
                r.enabled = true;
            foreach (NetworkCenter network in networks)
            {
                network.HideSphereIfEnlarged();
                foreach (Renderer r in network.GetComponentsInChildren<Renderer>())
                    r.enabled = true;

                if (network.Enlarged)
                {
                    network.gameObject.GetComponent<Renderer>().enabled = false;
                }
            }

            currentTime = 0;
            shrinkSpeed = (originalScale.x - transform.localScale.x) / animationTime;
            GetComponent<Renderer>().enabled = true;
            //GetComponent<Collider>().enabled = true;
            maximize = true;
        }

        /// <summary>
        /// Animation for showing network.
        /// </summary>
        void Maximize()
        {
            float step = speed * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, originalPos, step);
            transform.localScale += Vector3.one * Time.deltaTime * shrinkSpeed;
            transform.Rotate(Vector3.one * Time.deltaTime * -100);
            if (Mathf.Abs(currentTime - animationTime) <= 0.05f)
            {
                transform.localScale = originalScale;
                transform.localPosition = originalPos;
                transform.localRotation = originalRot;
                foreach (NetworkCenter network in Replacements)
                {
                    network.GetComponent<Collider>().enabled = true;
                }

                foreach (NetworkCenter network in networks)
                {
                    network.GetComponent<Collider>().enabled = true;
                    if (network.Enlarged)
                    {
                        foreach (Collider c in network.GetComponentsInChildren<Collider>())
                            c.enabled = true;
                    }
                }

                GetComponent<Collider>().enabled = true;
                maximize = false;
            }

            currentTime += Time.deltaTime;
        }

        /// <summary>
        /// Toggles all renderers and colliders off for all networks on this convex hull.
        /// </summary>
        internal void HideNetworks()
        {
            foreach (NetworkCenter network in networks)
            {
                foreach (Collider c in network.GetComponentsInChildren<Collider>())
                    c.enabled = false;
            }

            foreach (Collider c in GetComponentsInChildren<Collider>())
                c.enabled = false;
            originalPos = transform.position;
            originalRot = transform.localRotation;
            originalScale = transform.localScale;
            currentTime = 0;
            shrinkSpeed = (transform.localScale.x - targetScale) / animationTime;
            minimize = true;
        }

        /// <summary>
        /// Animation for hiding network.
        /// </summary>
        void Minimize()
        {
            float step = speed * Time.deltaTime;
            if (delete)
            {
                transform.position = Vector3.MoveTowards(transform.position,
                    referenceManager.deleteTool.transform.position, step);
            }
            else
            {
                transform.position = Vector3.MoveTowards(transform.position,
                    referenceManager.minimizedObjectHandler.transform.position, step);
            }

            transform.localScale -= Vector3.one * Time.deltaTime * shrinkSpeed;
            transform.Rotate(Vector3.one * Time.deltaTime * 100);
            if (Mathf.Abs(currentTime - animationTime) <= 0.05f || transform.localScale.x < 0)
            {
                if (delete)
                {
                    referenceManager.deleteTool.GetComponent<RemovalController>().ResetHighlight();
                    Destroy(gameObject);
                    return;
                }

                foreach (NetworkCenter network in networks)
                {
                    foreach (Renderer r in network.GetComponentsInChildren<Renderer>())
                        r.enabled = false;
                }

                foreach (Renderer r in GetComponentsInChildren<Renderer>())
                    r.enabled = false;
                minimize = false;
                referenceManager.minimizeTool.GetComponent<Light>().range = 0.04f;
                referenceManager.minimizeTool.GetComponent<Light>().intensity = 0.8f;
            }

            currentTime += Time.deltaTime;
        }

        /// <summary>
        /// Spawn network beside graph it was created from.
        /// </summary>
        public void CreateNetworkAnimation(Transform graph)
        {
            if (multiuserMessageSender.multiplayer)
            {
                transform.position = new Vector3(0, 1, 0);
            }

            if (!multiuserMessageSender.multiplayer)
            {
                transform.position = graph.position;
                transform.rotation = graph.rotation;
                transform.position += transform.forward * 0.3f;
                //transform.position = referenceManager.headset.transform.position;
                //transform.rotation = referenceManager.headset.transform.rotation;
                //transform.position += transform.forward * 1f;
            }

            //transform.Rotate(-20, 0, 0);
            targetPos = transform.position;
            createAnim = true;
        }

        /// <summary
        /// <summary>
        /// Used to delete this network. Starts the same animation that is used to minimze but deletes the obj when its been minimized.
        /// </summary>
        public void DeleteNetwork()
        {
            if (!removable)
            {
                Debug.Log("Script is running");
                CellexalError.SpawnError("Delete failed",
                    "Can not delete network yet. Wait for script to finish before removing it.");
            }

            for (int i = 0; i < networks.Count; i++)
            {
                Destroy(networks[i].gameObject);
            }

            networks.Clear();
            referenceManager.arcsSubMenu.DestroyTab(name.Split('_')[1]); // Get last part of nw name   
            referenceManager.networkGenerator.networkList.RemoveAll(item => item == null);
            referenceManager.graphManager.RemoveNetwork(this);
            delete = true;
            minimize = true;
            removing = true;
            //Destroy(this.gameObject);
            //referenceManager.deleteTool.GetComponent<RemovalController>().DeleteObjectAnimation(this.gameObject);
        }


        void NetworkAnimation()
        {
            float step = speed * Time.deltaTime;
            //transform.position = Vector3.MoveTowards(transform.position, targetPos, step);
            transform.localScale += Vector3.one * Time.deltaTime * shrinkSpeed;
            if (transform.localScale.x >= targetScale)
            {
                createAnim = false;
                transform.localScale = new Vector3(targetScale, targetScale, targetScale);
                referenceManager.notificationManager.SpawnNotification("Transcription factor networks finished.");
            }
        }

        /// <summary>
        /// Toggles all colliders in all networks. Used the networks are being grabbed to reduce lag.
        /// </summary>
        /// <param name="newState">True to toggle the colliders on, false otherwise.</param>
        internal void ToggleNetworkColliders(bool newState)
        {
            foreach (NetworkCenter network in networks)
            {
                if (network.Enlarged)
                {
                    foreach (Collider c in network.GetComponentsInChildren<Collider>())
                    {
                        c.enabled = newState;
                    }
                }
            }
        }

        /// <summary>
        /// Finds a networkcenter in this networkhandler.
        /// </summary>
        /// <param name="networkCenterName"> The name of the networkcenter </param>
        /// <returns> A reference to the networkcenter, or null if it was not found. </returns>
        public NetworkCenter FindNetworkCenter(string networkCenterName)
        {
            foreach (NetworkCenter network in networks)
            {
                if (network.name == networkCenterName)
                {
                    return network;
                }
            }

            return null;
        }

        /// <summary>
        /// Highlights a gene in all networks with a red circle.
        /// </summary>
        /// <param name="geneName">The gene to highlight.</param>
        public void HighLightGene(string geneName)
        {
            foreach (NetworkCenter nc in networks)
            {
                nc.HighLightGene(geneName);
                referenceManager.multiuserMessageSender.SendMessageHighlightNetworkNode(name, nc.name, geneName);
            }
        }

        public void HighlightNetworkSkeleton(bool toggle)
        {
            LineRenderer lr = GetComponent<LineRenderer>();
            lr.startColor = lr.endColor = toggle ? highlightColor : standardColor;
        }
    }
}