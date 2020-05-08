using System;
using CellexalVR.AnalysisObjects;
using CellexalVR.Extensions;
using CellexalVR.General;
using CellexalVR.Multiuser;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace CellexalVR.Interaction
{
    /// <summary>
    /// Represents the list of the 10 previous searches of genes.
    /// </summary>
    public class PreviousSearchesList : MonoBehaviour
    {
        public ReferenceManager referenceManager;
        public GameObject listNodePrefab;
        public Mesh quadPrefab;
        //public int numberOfPanels = 10;

        public List<GameObject> listNodes = new List<GameObject>();
        public List<PreviousSearchesLock> searchLocks = new List<PreviousSearchesLock>();
        public List<CorrelatedGenesPanel> correlatedGenesButtons = new List<CorrelatedGenesPanel>();
        public List<ClickableTextPanel> previousSearchesListNodes = new List<ClickableTextPanel>();

        private MultiuserMessageSender multiuserMessageSender;

        private void Start()
        {
            multiuserMessageSender = referenceManager.multiuserMessageSender;
            CellexalEvents.GraphsUnloaded.AddListener(ClearList);
        }


        /// <summary>
        /// Checks if the list already contains an entry.
        /// </summary>
        /// <param name="name">THe name of the thing in the entry.</param>
        /// <param name="type">The type of the entry.</param>
        /// <param name="coloringMethod">The coloring method that was used.</param>
        /// <returns>True if an entry of this kind was already in the list, false otherwise.</returns>
        public bool Contains(string name, Definitions.Measurement type, GraphManager.GeneExpressionColoringMethods coloringMethod)
        {
            foreach (var node in previousSearchesListNodes)
            {
                if (string.Equals(node.NameOfThing, name, StringComparison.CurrentCultureIgnoreCase) && node.Type == type && node.ColoringMethod == coloringMethod)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Adds an entry to the list of saved searches. New entries are added to the top of the list and all entries below that are not locked are pushed down one step. 
        /// If the list is full, the bottom most entry will be removed from the list.
        /// </summary>
        /// <param name="name">The name of the thing we are adding to the list.</param>
        /// <param name="type">The type of the thing.</param>
        /// <param name="coloringMethod">The method of coloring that used.</param>
        /// <returns></returns>
        public string AddEntry(string name, Definitions.Measurement type, GraphManager.GeneExpressionColoringMethods coloringMethod)
        {
            bool pushingDown = false;
            string pushDownName = "";
            Definitions.Measurement pushDownType = Definitions.Measurement.INVALID;
            GraphManager.GeneExpressionColoringMethods pushDownColoringMethod = GraphManager.GeneExpressionColoringMethods.EqualExpressionRanges;
            for (int i = 0; i < previousSearchesListNodes.Count; ++i)
            {
                var listNode = previousSearchesListNodes[i];
                // if this node is locked, just move on
                if (searchLocks[i].Locked)
                    continue;
                // if this node is empty, insert what we are inserting and return
                if (listNode.NameOfThing == "")
                {
                    if (!pushingDown)
                    {
                        listNode.ColoringMethod = coloringMethod;
                        listNode.SetText(name, type);
                        return "";
                    }
                    else
                    {
                        listNode.ColoringMethod = pushDownColoringMethod;
                        listNode.SetText(pushDownName, pushDownType);
                        return "";
                    }
                }
                // if we have not started the pushing down, then insert the new entry and save the old entry so we can push it down
                if (!pushingDown)
                {
                    pushingDown = true;
                    pushDownName = listNode.NameOfThing;
                    pushDownType = listNode.Type;
                    pushDownColoringMethod = listNode.ColoringMethod;
                    listNode.ColoringMethod = coloringMethod;
                    listNode.SetText(name, type);
                }
                else
                {
                    // swap the saved entry that should be pushed down to this node
                    var tempPushDownName = listNode.NameOfThing;
                    var tempPushDownType = listNode.Type;
                    var tempPushDownColoringMethod = listNode.ColoringMethod;

                    listNode.ColoringMethod = pushDownColoringMethod;
                    listNode.SetText(pushDownName, pushDownType);

                    pushDownName = tempPushDownName;
                    pushDownType = tempPushDownType;
                    pushDownColoringMethod = tempPushDownColoringMethod;
                }

            }
            return pushDownName;
        }

        /// <summary>
        /// Clears the list.
        /// </summary>
        public void ClearList()
        {
            foreach (var node in previousSearchesListNodes)
            {
                node.SetText("", Definitions.Measurement.INVALID);
            }
            foreach (var lockButton in searchLocks)
            {
                lockButton.Locked = false;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (referenceManager == null && gameObject.scene.IsValid())
            {
                referenceManager = GameObject.Find("InputReader").GetComponent<ReferenceManager>();
            }
        }

        /// <summary>
        /// Build the previous serches list.
        /// </summary>
        /// <param name="numberOfPanels">The number of panels to create.</param>
        /// <param name="prefabInstance">The prefab instance we are changing.</param>
        public void BuildPreviousSearchesList(int numberOfPanels, PreviousSearchesList prefabInstance)
        {
            if (numberOfPanels < 1)
            {
                Debug.LogError("Can not create less than 1 previous searches list nodes. Change numberOfPanels in the inspector on the " + name + " gameobject.");
                return;
            }

            // remove old nodes
            foreach (var oldNode in prefabInstance.listNodes)
            {
                DestroyImmediate(oldNode.gameObject);
            }

            prefabInstance.listNodes.Clear();
            prefabInstance.previousSearchesListNodes.Clear();
            prefabInstance.searchLocks.Clear();
            prefabInstance.correlatedGenesButtons.Clear();

            PanelRaycaster panelRaycaster = prefabInstance.gameObject.GetComponentInParent<PanelRaycaster>();

            // generate new nodes
            for (int i = 0; i < numberOfPanels; ++i)
            {
                GameObject newPanel = Instantiate(prefabInstance.listNodePrefab, prefabInstance.transform);
                newPanel.SetActive(true);
                // get the child components
                ClickableTextPanel previousSearchesListNode = newPanel.GetComponentInChildren<ClickableTextPanel>();
                PreviousSearchesLock searchLock = newPanel.GetComponentInChildren<PreviousSearchesLock>();
                CorrelatedGenesPanel correlatedGenesButton = newPanel.GetComponentInChildren<CorrelatedGenesPanel>();
                // add the components to the lists
                prefabInstance.listNodes.Add(newPanel);
                prefabInstance.previousSearchesListNodes.Add(previousSearchesListNode);
                prefabInstance.searchLocks.Add(searchLock);
                prefabInstance.correlatedGenesButtons.Add(correlatedGenesButton);

                // assign meshes
                //Mesh previousSearchesListNodeMesh = new Mesh();
                //previousSearchesListNode.GetComponent<MeshFilter>().sharedMesh = quadPrefab;
                //searchLock.GetComponent<MeshFilter>().sharedMesh = quadPrefab;
                //correlatedGenesButton.GetComponent<MeshFilter>().sharedMesh = quadPrefab;
                // assign materials
                previousSearchesListNode.GetComponent<MeshRenderer>().sharedMaterial = panelRaycaster.keyNormalMaterial;
                searchLock.GetComponent<MeshRenderer>().sharedMaterial = panelRaycaster.unlockedNormalMaterial;
                correlatedGenesButton.GetComponent<MeshRenderer>().sharedMaterial = panelRaycaster.correlatedGenesNormalMaterial;

                newPanel.gameObject.name = "List Node " + (i + 1);
                newPanel.transform.localPosition = Vector3.zero;
                // assign positions
                KeyboardItem previousSearchesListNodeItem = previousSearchesListNode.GetComponentInParent<KeyboardItem>();
                KeyboardItem searchLockItem = searchLock.GetComponentInParent<KeyboardItem>();
                KeyboardItem correlatedGenesButtonItem = correlatedGenesButton.GetComponentInParent<KeyboardItem>();
                int keyboardPosY = numberOfPanels - i - 1;
                previousSearchesListNodeItem.position = new Vector2Int(-6, keyboardPosY);
                previousSearchesListNodeItem.size = new Vector2(3, 1);
                searchLockItem.position = new Vector2Int(-8, keyboardPosY);
                correlatedGenesButtonItem.position = new Vector2Int(-9, keyboardPosY);
            }


            //gameObject.GetComponentsInChildren<PreviousSearchesLock>(searchLocks);
            //gameObject.GetComponentsInChildren<ClickableTextPanel>(previousSearchesListNodes);
            //gameObject.GetComponentsInChildren<CorrelatedGenesPanel>(correlatedGenesButtons);
        }
#endif
    }

}