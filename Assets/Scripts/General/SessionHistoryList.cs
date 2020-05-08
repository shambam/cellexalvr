using System;
using System.Collections.Generic;
using CellexalVR.AnalysisObjects;
using CellexalVR.Extensions;
using CellexalVR.Interaction;
using CellexalVR.Multiuser;
using TMPro;
using UnityEngine;

namespace CellexalVR.General
{
    /// <summary>
    /// Keeps track of things done in the current session. Such as selections, colouring by gene or index.
    /// Creates clickable panels to reselect/recolour.
    /// Similar to previous searches list on keyboard but contains selections and other more general stuff as well.
    /// Can be accessed without keyboard.
    /// </summary>
    public class SessionHistoryList : MonoBehaviour
    {
        /// <summary>
        /// Represents the list of the 10 previous things done in session.
        /// </summary>
        public ReferenceManager referenceManager;

        public GameObject listNodePrefab;

        public List<GameObject> listNodes = new List<GameObject>();

        public List<ClickableHistoryPanel> sessionHistoryListNodes = new List<ClickableHistoryPanel>();
        // public List<TextMeshPro> categoryTexts = new List<TextMeshPro>();

        private MultiuserMessageSender multiuserMessageSender;

        private void Start()
        {
            multiuserMessageSender = referenceManager.multiuserMessageSender;
        }

        /// <summary>
        /// Checks if the list already contains an entry.
        /// </summary>
        /// <param name="name">THe name of the thing in the entry.</param>
        /// <param name="type">The type of the entry.</param>
        /// <param name="coloringMethod">The coloring method that was used.</param>
        /// <returns>True if an entry of this kind was already in the list, false otherwise.</returns>
        public bool Contains(string name, Definitions.HistoryEvent type)
        {
            foreach (var node in sessionHistoryListNodes)
            {
                if (string.Equals(node.NameOfThing, name, StringComparison.CurrentCultureIgnoreCase) &&
                    node.Type == type)
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
        /// <param name="id">Optional argument for unique id of entry to be able to recreate a copy.
        /// Can be for example a layoutseed for networks so that layout will be the same when recreated.</param>
        /// <returns></returns>
        public string AddEntry(string entryName, Definitions.HistoryEvent type, int id = -1)
        {
            bool pushingDown = false;
            string pushDownName = "";
            int pushDownID = -1;
            Definitions.HistoryEvent pushDownType = Definitions.HistoryEvent.INVALID;
            for (int i = 0; i < sessionHistoryListNodes.Count; ++i)
            {
                ClickableHistoryPanel listNode = sessionHistoryListNodes[i];
                // if this node is empty, insert what we are inserting and return
                if (listNode.NameOfThing == "")
                {
                    if (!pushingDown)
                    {
                        listNode.SetText(entryName, type);
                        if (id != -1)
                        {
                            listNode.ID = id;
                        }

                        return "";
                    }
                    else
                    {
                        listNode.SetText(pushDownName, pushDownType);
                        listNode.ID = pushDownID;
                        return "";
                    }
                }

                // if we have not started the pushing down, then insert the new entry and save the old entry so we can push it down
                if (!pushingDown)
                {
                    pushingDown = true;
                    pushDownName = listNode.NameOfThing;
                    pushDownType = listNode.Type;
                    pushDownID = listNode.ID;
                    listNode.SetText(entryName, type);
                    listNode.ID = id;
                }
                else
                {
                    // swap the saved entry that should be pushed down to this node
                    var tempPushDownName = listNode.NameOfThing;
                    var tempPushDownType = listNode.Type;
                    var tempPushDownID = listNode.ID;

                    listNode.SetText(pushDownName, pushDownType);
                    listNode.ID = pushDownID;

                    pushDownName = tempPushDownName;
                    pushDownType = tempPushDownType;
                    pushDownID = tempPushDownID;
                }
            }

            return pushDownName;
        }

        /// <summary>
        /// Clears the list.
        /// </summary>
        public void ClearList()
        {
            foreach (var node in sessionHistoryListNodes)
            {
                node.SetText("", Definitions.HistoryEvent.INVALID);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (referenceManager == null && gameObject.scene.IsValid())
            {
                referenceManager = GameObject.Find("InputReader").GetComponent<ReferenceManager>();
            }

            // remove null entries
            for (int i = 0; i < listNodes.Count; ++i)
            {
                if (listNodes[i] == null)
                {
                    listNodes.RemoveAt(i);
                    i--;
                }
            }
        }

        /// <summary>
        /// Builds the session history list in a prefab instance.
        /// </summary>
        /// <param name="numberOfPanels">The number of panels to spawn.</param>
        /// <param name="prefabInstance">The prefab isntance that we are modifying.</param>
        public void BuildList(int numberOfPanels, SessionHistoryList prefabInstance)
        {
            if (numberOfPanels < 1)
            {
                return;
            }

            foreach (var oldNode in prefabInstance.listNodes)
            {
                DestroyImmediate(oldNode);
            }

            prefabInstance.listNodes.Clear();
            prefabInstance.sessionHistoryListNodes.Clear();

            PanelRaycaster panelRaycaster = prefabInstance.GetComponentInParent<PanelRaycaster>();

            // generate new nodes
            for (int i = 0; i < numberOfPanels; ++i)
            {
                GameObject newPanel =
                    Instantiate(prefabInstance.listNodePrefab, prefabInstance.transform);
                newPanel.SetActive(true);
                // newPanel.transform.parent = prefabInstance.listNodesParent.transform;
                // get the child components
                ClickableHistoryPanel historyPanel = newPanel.GetComponentInChildren<ClickableHistoryPanel>();
                // add the components to the lists
                prefabInstance.listNodes.Add(newPanel);
                prefabInstance.sessionHistoryListNodes.Add(historyPanel);

                // assign meshes
                //Mesh previousSearchesListNodeMesh = new Mesh();
                //correlatedPanel.GetComponent<MeshFilter>().sharedMesh = quadPrefab;
                //anticorrelatedPanel.GetComponent<MeshFilter>().sharedMesh = quadPrefab;
                // assign materials
                historyPanel.GetComponent<MeshRenderer>().sharedMaterial = panelRaycaster.keyNormalMaterial;

                newPanel.gameObject.name = "History List Node " + (i + 1);
                newPanel.transform.localPosition = Vector3.zero;
                // assign positions
                KeyboardItem historyItem = historyPanel.GetComponentInParent<KeyboardItem>();
                int keyboardPosY = numberOfPanels - i - 1;
                historyItem.position = new Vector2Int(-22, keyboardPosY);
                historyItem.size = new Vector2(8, 1);
            }
        }

#endif
    }
}