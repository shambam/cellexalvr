using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CellexalVR.General;
using CellexalVR.Interaction;
using CellexalVR.MarchingCubes;
using CellexalVR.Multiuser;
using CellexalVR.Tools;
using SQLiter;
using TMPro;
using UnityEngine;
using VRTK;

namespace CellexalVR.AnalysisObjects
{

    /// <summary>
    /// Represents a graph consisting of multiple GraphPoints.
    /// </summary>
    public class Graph : MonoBehaviour
    {
        public GameObject skeletonPrefab;
        public GameObject contourParent;
        public Material opaqueMat;
        public GameObject emptySkeletonPrefab;
        public Material skeletonMaterial;
        public Material lineMaterial;
        public GameObject movingOutlineCircle;
        [HideInInspector]
        public GameObject convexHull;
        public GameObject chunkManagerPrefab;
        //public string DirectoryName { get; set; }
        public List<GameObject> CTCGraphs { get; set; }
        [HideInInspector]
        public GraphManager graphManager;
        public GameObject infoParent;
        public TextMeshPro graphNameText;
        public TextMeshPro graphInfoText;
        public TextMeshPro graphNrText;
        public LegendManager legendManager;
        [HideInInspector]
        public GameObject axes;
        public GameObject annotationsParent;
        public string[] axisNames = new string[3];
        public bool GraphActive = true;
        public Dictionary<string, GraphPoint> points = new Dictionary<string, GraphPoint>();
        public Dictionary<string, GraphPoint> subSelectionPoints = new Dictionary<string, GraphPoint>();
        public ReferenceManager referenceManager;
        public List<GameObject> graphPointClusters = new List<GameObject>();
        public int textureWidth;
        public int textureHeight;
        public Texture2D texture;
        private bool textureChanged;
        public Vector3 minCoordValues = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        public Vector3 maxCoordValues = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        public List<GameObject> topExprCircles = new List<GameObject>();
        public Vector3 diffCoordValues;
        public float longestAxis;
        public Vector3 scaledOffset;
        public int nbrOfClusters;
        public VelocityParticleEmitter velocityParticleEmitter;
        public GameObject lineParent;
        public bool hasVelocityInfo;
        [HideInInspector]
        public bool graphPointsInactive = false;
        public string GraphName
        {
            get { return graphName; }
            set
            {
                graphName = value;
                graphNameText.text = value;
                this.name = graphName;
                this.gameObject.name = graphName;
            }
        }
        public string FolderName
        {
            get { return folderName; }
            set
            {
                folderName = value;
                graphNameText.text = folderName + "_" + graphName;
                //graphNameText.text = value;
            }
        }
        public int GraphNumber
        {
            get { return graphNr; }
            set
            {
                graphNr = value;
                graphNrText.text = value.ToString();
            }
        }


        private ControllerModelSwitcher controllerModelSwitcher;
        private MultiuserMessageSender multiuserMessageSender;
        private Vector3 startPosition;
        private List<Vector3> nodePosition;
        private List<Vector3> nodeSizes;
        // For minimization animation
        private bool minimize;
        private bool maximize;
        private bool delete;
        private float speed;

        private bool minimized;
        //private Vector3 targetPos;
        private float targetMinScale;
        private float targetMaxScale;
        private float shrinkSpeed;
        private Vector3 oldPos;
        private Quaternion oldRot;
        private Vector3 oldScale;
        private float currentTime = 0f;
        private float animationTime = 0.7f;


#if UNITY_EDITOR
        // Debug stuff
        private Vector3 debugGizmosPos;
        private Vector3 debugGizmosMin;
        private Vector3 debugGizmosMax;
#endif
        private string graphName;
        private string folderName;
        private int graphNr;
        private int nbrOfExpressionColors;



        private static LayerMask selectionToolLayerMask;

        public OctreeNode octreeRoot;
        private GraphGenerator graphGenerator;
        private bool isTransparent;
        private GameObject contour;

        //private ChunkManager chunkManager;

        private void OnValidate()
        {
            if (gameObject.scene.IsValid())
            {
                referenceManager = GameObject.Find("InputReader").GetComponent<ReferenceManager>();
            }
        }

        void Start()
        {
            speed = 1.5f;
            //targetPos = Vector3.zero;
            shrinkSpeed = 2f;
            targetMinScale = 0.05f;
            targetMaxScale = 1f;
            oldPos = new Vector3();
            graphManager = referenceManager.graphManager;
            multiuserMessageSender = referenceManager.multiuserMessageSender;
            graphManager = referenceManager.graphManager;
            CTCGraphs = new List<GameObject>();
            controllerModelSwitcher = referenceManager.controllerModelSwitcher;
            graphGenerator = referenceManager.graphGenerator;
            selectionToolLayerMask = 1 << LayerMask.NameToLayer("SelectionToolLayer");
            startPosition = transform.position;
            nbrOfExpressionColors = CellexalConfig.Config.GraphNumberOfExpressionColors;
            //chunkManager = GameObject.Find("ChunkManager").GetComponent<ChunkManager>();
        }

        private void Update()
        {
            if (textureChanged && texture != null)
            {
                texture.Apply();
                textureChanged = false;
            }

            if (GetComponent<VRTK_InteractableObject>().IsGrabbed())
            {
                multiuserMessageSender.SendMessageMoveGraph(GraphName, transform.position, transform.rotation, transform.localScale);
            }
            if (minimize)
            {
                Minimize();
            }
            if (maximize)
            {
                Maximize();
            }
        }

        /// <summary>
        /// Starts the maximize animation.
        /// </summary>
        internal void ShowGraph()
        {
            transform.position = referenceManager.leftController.transform.position;
            GraphActive = true;
            //foreach (Renderer r in GetComponentsInChildren<Renderer>())
            //    r.enabled = true;
            gameObject.SetActive(true);
            shrinkSpeed = (oldScale.x - transform.localScale.x) / animationTime;
            currentTime = 0;
            maximize = true;
        }

        public void MakeAllPointsTransparent(bool toggle)
        {
            foreach (KeyValuePair<string, GraphPoint> gpPair in points)
            {
                MakePointTransparent(gpPair.Value, toggle);
            }


            // for transparancy 0.5 < g < 0.9 
            //for (int i = 0; i < textureWidth; ++i)
            //{
            //    for (int j = 0; j < textureHeight; ++j)
            //    {
            //        //texture.SetPixel(i, j, Color.black);
            //        if (toggle)
            //        {
            //            texture.SetPixels32(i, j, 1, 1, new Color32[] { new Color32(254, 0, 0, 255) });
            //        }
            //        else
            //        {
            //            texture.SetPixels32(i, j, 1, 1, new Color32[] { new Color32(255, 0, 0, 255) });
            //        }
            //    }
            //}
            //texture.Apply();
            isTransparent = toggle;

            //texture.SetPixels32(i, j, 1, 1, new Color32[] { new Color32(254, 0, 0, 255) });
            //foreach (GraphPoint gp in points)
            //{

            //}
        }

        /// <summary>
        /// Animation for showing graph.
        /// </summary>
        void Maximize()
        {
            float step = speed * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, oldPos, step);
            transform.localScale += Vector3.one * Time.deltaTime * shrinkSpeed;
            transform.Rotate(Vector3.one * Time.deltaTime * -100);
            //multiuserMessageSender.SendMessageMoveGraph(GraphName, transform.position, transform.rotation, transform.localScale);
            if (Mathf.Abs(currentTime - animationTime) <= 0.05f)
            {
                transform.localScale = oldScale;
                transform.localPosition = oldPos;
                transform.localRotation = oldRot;
                //CellexalLog.Log("Maximized object" + name);
                maximize = false;
                GraphActive = true;
                minimized = false;
                foreach (GameObject obj in CTCGraphs)
                {
                    if (obj != null)
                    {
                        obj.SetActive(true);
                    }
                }
                foreach (Collider c in GetComponentsInChildren<Collider>())
                    c.enabled = true;
            }
            currentTime += Time.deltaTime;
        }

        /// <summary>
        /// Used to delete a graph. Starts the minimizing animation and then deletes the object.
        /// </summary>
        /// <param name="tag"></param>
        public void DeleteGraph(string tag)
        {
            if (tag == "SubGraph")
            {
                if (hasVelocityInfo)
                {
                    var veloButton = referenceManager.velocitySubMenu.FindButton("", GraphName);
                    referenceManager.velocitySubMenu.buttons.Remove(veloButton);
                    Destroy(veloButton.gameObject);
                }
                referenceManager.graphManager.Graphs.Remove(this);
                referenceManager.graphManager.attributeSubGraphs.Remove(this);
                for (int i = 0; i < CTCGraphs.Count; i++)
                {
                    CTCGraphs[i].GetComponent<GraphBetweenGraphs>().RemoveGraph();
                }
                CTCGraphs.Clear();
            }
            else if (tag == "FacsGraph")
            {
                referenceManager.graphManager.Graphs.Remove(this);
                referenceManager.graphManager.facsGraphs.Remove(this);
                for (int i = 0; i < CTCGraphs.Count; i++)
                {
                    CTCGraphs[i].GetComponent<GraphBetweenGraphs>().RemoveGraph();
                }
                CTCGraphs.Clear();
            }
            shrinkSpeed = (transform.localScale.x - targetMinScale) / animationTime;
            currentTime = 0;
            minimize = true;
            delete = true;
        }

        /// <summary>
        /// Turns off all renderers and colliders for this graph. And starts the minimazation process by sett minimize to true.
        /// </summary>
        internal void HideGraph()
        {
            GraphActive = false;
            //targetPos = referenceManager.minimizedObjectHandler.transform.position;
            foreach (Collider c in GetComponentsInChildren<Collider>())
            {
                c.enabled = false;
            }
            foreach (GameObject obj in CTCGraphs)
            {
                if (obj != null)
                {
                    obj.SetActive(false);
                }
            }
            oldPos = transform.position;
            oldScale = transform.localScale;
            shrinkSpeed = (transform.localScale.x - targetMinScale) / animationTime;
            currentTime = 0;
            minimize = true;
        }

        /// <summary>
        /// Animation for hiding graph. Same animation is used when deleting the graph.
        /// </summary>
        void Minimize()
        {
            float step = speed * Time.deltaTime;
            Vector3 targetPosition;
            if (CrossSceneInformation.Spectator)
            {
                targetPosition = Vector3.zero;
            }
            else if (delete)
            {
                targetPosition = referenceManager.deleteTool.transform.position;
                referenceManager.deleteTool.GetComponent<RemovalController>().ResetHighlight();
            }
            else
            {
                targetPosition = referenceManager.minimizedObjectHandler.transform.position;
            }
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, step);
            transform.localScale -= Vector3.one * Time.deltaTime * shrinkSpeed;
            transform.Rotate(Vector3.one * Time.deltaTime * 100);
            //multiuserMessageSender.SendMessageMoveGraph(GraphName, transform.position, transform.rotation, transform.localScale);
            if (Mathf.Abs(currentTime - animationTime) <= 0.05f || transform.localScale.x < 0)
            {
                if (delete)
                {
                    Destroy(this.gameObject);
                }
                else
                {
                    minimize = false;
                    GraphActive = false;
                    gameObject.SetActive(false);
                    referenceManager.minimizeTool.GetComponent<Light>().range = 0.04f;
                    referenceManager.minimizeTool.GetComponent<Light>().intensity = 0.8f;
                    minimized = true;
                }
            }
            currentTime += Time.deltaTime;
        }

        /// <summary>
        /// Scales one <see cref="Vector3"/> of coordinates from the original scale to the graph's desired scale.
        /// </summary>
        /// <param name="coords">The coordinates to scale.</param>
        /// <returns>A scaled <see cref="Vector3"/>.</returns>
        public Vector3 ScaleCoordinates(Vector3 coords)
        {
            // move one of the graph's corners to origo
            coords -= minCoordValues;

            // uniformly scale all axes down based on the longest axis
            // this makes the longest axis have length 1 and keeps the proportions of the graph
            coords /= longestAxis;

            // move the graph a bit so (0, 0, 0) is the center point
            coords -= scaledOffset;
            return coords;
        }

        /// <summary>
        /// A graph point represents a point in the graphs as well as one cell in the dimension reduced data.
        /// </summary>
        public class GraphPoint
        {
            private static int indexCounter = 0;

            public string Label;
            public Vector3 Position;
            public GameObject cluster;
            public int index;
            public Vector2Int textureCoord;
            public Graph parent;
            private int group;
            public int Group
            {
                get { return group; }
                set { SetGroup(value); }
            }
            public bool unconfirmedInSelection;
            public List<Selectable> lineBetweenCellsCubes;
            public Vector3 WorldPosition { get { return parent.transform.TransformPoint(Position); } }
            public OctreeNode node;

            public GraphPoint(string label, float x, float y, float z, Graph parent)
            {
                Label = label;
                Position = new Vector3(x, y, z);
                this.parent = parent;
                index = indexCounter;
                group = -1;
                unconfirmedInSelection = false;
                indexCounter++;
                lineBetweenCellsCubes = new List<Selectable>();
            }

            public override string ToString()
            {
                return Label;
            }

            /// <summary>
            /// Scales this graph point's coordinates from the graph's original scale to it's desired scale.
            /// </summary>
            public void ScaleCoordinates()
            {
                // move one of the graph's corners to origo
                Position -= parent.minCoordValues;

                // uniformly scale all axes down based on the longest axis
                // this makes the longest axis have length 1 and keeps the proportions of the graph
                Position /= parent.longestAxis;

                // move the graph a bit so (0, 0, 0) is the center point
                Position -= parent.scaledOffset;
            }

            public void SetTextureCoord(Vector2Int newPos)
            {
                textureCoord = newPos;
            }

            public void ColorGeneExpression(int i, bool outline)
            {
                parent.ColorGraphPointGeneExpression(this, i, outline);
            }

            public void ColorSelectionColor(int i, bool outline)
            {
                parent.ColorGraphPointSelectionColor(this, i, outline);
                Group = i;
            }

            public void HighlightGraphPoint(bool active)
            {
                parent.HighlightGraphPoint(this, active);

            }

            public void ResetColor()
            {
                parent.ResetGraphPointColor(this);
                Group = -1;
            }

            public Color GetColor()
            {
                return parent.GetGraphPointColor(this);
            }

            private void SetGroup(int group)
            {
                this.group = group;
                if (node.Group != group)
                    node.Group = group;
            }
        }

        /// <summary>
        /// Private class to represent one node in the octree used for collision detection
        /// </summary>
        public class OctreeNode
        {
            public OctreeNode parent;
            /// <summary>
            /// Not always of length 8. Empty children are removed when the octree is constructed to save some memory.
            /// </summary>
            public OctreeNode[] children;

            /// <summary>
            /// Null if this is not a leaf. Only leaves represents points in the graph.
            /// </summary>
            public GraphPoint point;
            public Vector3 pos;
            /// <summary>
            /// Not the geometrical center, just a point inside the node that defines corners for its children, unless this is a leaf node.
            /// </summary>
            public Vector3 center;
            public Vector3 size;
            private int group = -1;
            private bool raycasted;
            /// <summary>
            /// The group that this node belongs to. -1 means no group, 0 or a positive number means some group.
            /// If this is a leaf node, this should be the same as the group that the selection tool has given the <see cref="GraphPoint"/>.
            /// If this is not a leaf node, this is not -1 if all its children are of that group.
            /// </summary>
            public int Group
            {
                get { return group; }
                set { SetGroup(value); }
            }
            public bool rejected;
            public bool completelyInside;

            public OctreeNode() { }

            private bool nodeIterated;
            public bool NodeIterated
            {
                get { return nodeIterated; }
                set
                {
                    nodeIterated = value;
                    UpdateIterated();
                }
            }
            /// <summary>
            /// Returns a string representation of this node and all its children. May produce very long strings for very large trees.
            /// </summary>
            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                ToStringRec(ref sb);
                return sb.ToString();
            }

            private void ToStringRec(ref StringBuilder sb)
            {
                if (point != null)
                {
                    sb.Append(point.Label);
                }
                else
                {
                    if (children.Length > 0)
                    {
                        sb.Append("(");
                        foreach (var child in children)
                        {
                            child.ToStringRec(ref sb);
                            sb.Append(", ");
                        }
                        sb.Remove(sb.Length - 2, 2);
                        sb.Append(")");
                    }
                }
            }

            private void SetGroup(int group)
            {
                this.group = group;
                foreach (var child in children)
                {
                    child.SetGroup(group);
                }
                if (parent != null && parent.group != group)
                {
                    parent.NotifyGroupChange(group);
                }
            }

            private void NotifyGroupChange(int group)
            {
                int setGroupTo = group;
                // only set this node's group if all children are also of that group
                foreach (var child in children)
                {
                    if (child.group != group)
                    {
                        // not all children were of that group, set this node's group to -1
                        setGroupTo = -1;
                        break;
                    }
                }
                this.group = setGroupTo;
                if (parent != null)
                {
                    parent.NotifyGroupChange(group);
                }
            }

            /// <summary>
            /// Changes the group of this node and all of its children (and grand-children and so on) recursively.
            /// </summary>
            /// <param name="group">The new group.</param>
            public void ChangeGroupRecursively(int group)
            {
                this.group = group;
                foreach (var child in children)
                {
                    child.ChangeGroupRecursively(group);
                }
            }

            public List<OctreeNode> GetSkeletonNodesRecursive(OctreeNode node, List<OctreeNode> nodes = null, int currentNodeLevel = 0, int desiredNodeLevel = 0)
            {
                if (nodes == null)
                {
                    nodes = new List<OctreeNode>();
                }
                foreach (OctreeNode child in node.children)
                {
                    if (desiredNodeLevel == currentNodeLevel)
                    {
                        nodes.Add(child);
                        return nodes;
                    }
                    else
                    {
                        child.GetSkeletonNodesRecursive(child, nodes, currentNodeLevel + 1, desiredNodeLevel);
                    }
                }
                return nodes;
            }

            /// <summary>
            /// Returns the smallest node that contains a point. The retured node might not be a leaf.
            /// </summary>
            /// <param name="point">The point to look for a node around.</param>
            /// <returns>The that contains the point.</returns>
            public OctreeNode NodeContainingPoint(Vector3 point)
            {
                if (children.Length == 0)
                {
                    return this;
                }
                foreach (OctreeNode child in children)
                {
                    if (child.PointInside(point))
                    {
                        return child.NodeContainingPoint(point);
                    }
                }
                return this;
            }

            /// <summary>
            /// Checks if a point is inside this node.
            /// </summary>
            public bool PointInside(Vector3 point)
            {
                return point.x >= pos.x && point.x <= pos.x + size.x
                    && point.y >= pos.y && point.y <= pos.y + size.y
                    && point.z >= pos.z && point.z <= pos.z + size.z;
            }

            /// <summary>
            /// Checks if a point is inside the selection tool by raycasting.
            /// </summary>
            /// <param name="selectionToolCenter">The selection tool's position in world space.</param>
            /// <param name="pointPosWorldSpace">This node's position in world space. (use Transform.TransformPoint(node.splitCenter) )</param>
            /// <returns>True if <paramref name="pointPosWorldSpace"/> is inside the selection tool.</returns>
            public bool PointInsideSelectionTool(Vector3 selectionToolCenter, Vector3 pointPosWorldSpace)
            {
                raycasted = true;
                Vector3 difference = selectionToolCenter - pointPosWorldSpace;
                return !Physics.Raycast(pointPosWorldSpace, difference, difference.magnitude, Graph.selectionToolLayerMask);
            }

            /// <summary>
            /// Sets <see cref="NodeIterated"/> to false for this node and all of its children recursively.
            /// </summary>
            public void ResetIteration()
            {
                NodeIterated = false;
                foreach (OctreeNode child in children)
                {
                    child.ResetIteration();
                }
            }

            /// <summary>
            /// Finds the first leaf node (using depth first) that has <see cref="NodeIterated"/> == false. Returns null if no non-iterated node was found.
            /// </summary>
            public OctreeNode FirstLeafNotIterated()
            {
                if (children.Length == 0)
                {
                    if (NodeIterated)
                    {
                        return null;
                    }
                    else
                    {
                        NodeIterated = true;
                        return this;
                    }

                }
                else
                {
                    foreach (OctreeNode child in children)
                    {
                        if (!child.NodeIterated)
                        {
                            OctreeNode found = child.FirstLeafNotIterated();
                            if (found != null)
                            {
                                return found;
                            }
                        }
                    }
                    // all children iterated
                    NodeIterated = true;
                    return null;
                }
            }

            /// <summary>
            /// Updates each parent's <see cref="nodeIterated"/> if all its children are iterated.
            /// </summary>
            private void UpdateIterated()
            {
                foreach (OctreeNode child in children)
                {
                    if (!child.nodeIterated)
                    {
                        return;
                    }
                }
                nodeIterated = true;
                if (parent != null)
                {
                    parent.UpdateIterated();
                }
            }

            /// <summary>
            /// Returns a <see cref="List{OctreeNode}"/> all leaves that are under this node in the octree.
            /// </summary>
            /// <param name="includeIterated">True if nodes marked as iterated (<see cref="OctreeNode.NodeIterated"/>) should be included, false if they should not.</param>
            public List<OctreeNode> AllLeavesUnder(bool includeIterated = true)
            {
                List<OctreeNode> list = new List<OctreeNode>();
                AllLeavesUnder(ref list, includeIterated);
                return list;
            }

            private void AllLeavesUnder(ref List<OctreeNode> list, bool includeIterated)
            {
                if (children.Length == 0)
                {
                    NodeIterated = true;
                    list.Add(this);
                }
                else
                {
                    foreach (OctreeNode child in children)
                    {
                        if (includeIterated || !child.NodeIterated)
                        {
                            child.AllLeavesUnder(ref list, includeIterated);
                        }
                    }
                }
            }

#if UNITY_EDITOR
            #region DEBUG_FUNCTIONS
            public void DebugColorLeaves(Color color)
            {
                if (point != null)
                {
                    point.ColorSelectionColor(0, false);
                }
                foreach (var child in children)
                {
                    child.DebugColorLeaves(color);
                }
            }

            public void DrawDebugCubesRecursive(Vector3 gameobjectPos, bool onlyLeaves, int i)
            {
                Gizmos.color = GizmoColors(i++);
                if (!onlyLeaves || children.Length == 0 && onlyLeaves)
                {
                    Gizmos.DrawWireCube(gameobjectPos + pos + size / 2, size / 0.95f);
                }
                foreach (var child in children)
                {
                    child.DrawDebugCubesRecursive(gameobjectPos, onlyLeaves, i);
                }

            }

            public void DrawDebugCubesRecursive(Vector3 gameobjectPos, int i, int level)
            {

                if (i == level)
                {
                    Gizmos.DrawWireCube(gameobjectPos + pos + size / 2, size / 0.95f);
                    return;
                }
                foreach (var child in children)
                {
                    child.DrawDebugCubesRecursive(gameobjectPos, i + 1, level);
                }

            }

            public IEnumerator DrawSkeletonCubes(List<Vector3> nodePositions, List<Vector3> nodeSizes, int nodeLevel)
            {
                if (nodePositions == null)
                {
                    nodePositions = new List<Vector3>();
                }
                foreach (OctreeNode child in children)
                {
                    foreach (OctreeNode c in child.children)
                    {
                        foreach (OctreeNode on in c.children)
                        {
                            nodeSizes.Add(c.size);
                            nodePositions.Add(c.center);
                            //print("adding nodes");
                            //foreach (OctreeNode onc in on.children)
                            //{

                            //}
                        }
                    }
                    yield return null;
                }
                NodeIterated = true;

            }


            public void DrawDebugLines(Vector3 gameobjectPos)
            {
                //if (children.Length != 0)
                //{
                //    Gizmos.color = Color.red;
                //    Gizmos.DrawWireSphere(gameobjectPos + center, 0.03f);
                //    Gizmos.color = Color.white;
                //}
                foreach (var child in children)
                {
                    Gizmos.DrawLine(gameobjectPos + center, gameobjectPos + child.center);
                    child.DrawDebugLines(gameobjectPos);
                }
            }

            public void DrawDebugRaycasts(Vector3 gameobjectPos, Vector3 selectionToolPos)
            {
                if (raycasted)
                {
                    Gizmos.DrawLine(center + gameobjectPos, selectionToolPos);
                    raycasted = false;
                }
                else
                {
                    foreach (var child in children)
                    {
                        child.DrawDebugRaycasts(gameobjectPos, selectionToolPos);
                    }
                }
            }

            public void DrawDebugGroups(Vector3 gameobjectPos)
            {
                Gizmos.color = GizmoColors(group);
                Gizmos.DrawWireCube(gameobjectPos + pos + size / 2, size / 0.95f);
                foreach (var child in children)
                {
                    child.DrawDebugGroups(gameobjectPos);
                }
            }

            private Color GizmoColors(int i)
            {
                if (i == -1)
                {
                    return Color.white;
                }
                i = i % CellexalConfig.Config.SelectionToolColors.Length;
                return CellexalConfig.Config.SelectionToolColors[i];
            }
            #endregion
#endif
        }

#if UNITY_EDITOR
        public void OnDrawGizmos()
        {
            if (!Application.isPlaying)
                return;

            if (octreeRoot != null)
            {
                if (graphManager.drawDebugCubes)
                {
                    octreeRoot.DrawDebugCubesRecursive(transform.position, false, 0);
                }
                if (graphManager.drawDebugLines)
                {
                    Gizmos.color = Color.white;
                    octreeRoot.DrawDebugLines(transform.position);
                }
                if (graphManager.drawDebugCubesOnLevel > -1)
                {
                    octreeRoot.DrawDebugCubesRecursive(transform.position, 0, graphManager.drawDebugCubesOnLevel);
                }
            }
            if (graphManager.drawSelectionToolDebugLines)
            {
                Gizmos.color = Color.green;
                DrawDebugCube(Color.green, transform.TransformPoint(debugGizmosMin), transform.TransformPoint(debugGizmosMax), true);
            }
            if (graphManager.drawDebugRaycast)
            {
                octreeRoot.DrawDebugRaycasts(transform.position, debugGizmosPos);
            }
            if (graphManager.drawDebugRejectionApprovedCubes)
            {
                DrawRejectionApproveCubes(octreeRoot);
            }
            if (graphManager.drawDebugGroups)
            {
                octreeRoot.DrawDebugGroups(transform.position);
            }

        }
#endif

        ///// <summary>
        ///// Coroutine to create the graph skeleton using the nodes from the octree. Each octreenode on a certain level will be represented by a transparant cube.
        ///// </summary>
        ///// <returns></returns>
        //public IEnumerator CreateGraphSkeletonCoroutine()
        //{
        //    //MeshFilter convexHull = null;
        //    convexHull = Instantiate(skeletonPrefab);
        //    convexHull.gameObject.SetActive(false);
        //    List<Vector3> nodePositions = new List<Vector3>();
        //    List<Vector3> sizes = new List<Vector3>();
        //    List<MeshFilter> meshes = new List<MeshFilter>();
        //    StartCoroutine(octreeRoot.DrawSkeletonCubes(nodePositions, sizes, 0));
        //    //octreeRoot.DrawSkeletonCubes(nodePositions, sizes, 0);
        //    //octreeRoot.DrawSkeletonCubes(ref nodePositions, ref sizes, 0);
        //    int i = 0;
        //    while (!octreeRoot.nodeIterated)
        //    {
        //        yield return null;
        //    }
        //    foreach (Vector3 pos in nodePositions)
        //    {
        //        var cube = Instantiate(skeletonCubePrefab, convexHull.transform);
        //        meshes.Add(cube.GetComponent<MeshFilter>());
        //        cube.transform.localPosition = pos;
        //        cube.transform.localScale = Vector3.one * 0.025f;
        //        //cube.transform.localScale = sizes[i];
        //        i++;
        //    }
        //    CombineInstance[] combine = new CombineInstance[meshes.Count];
        //    i = 0;
        //    while (i < meshes.Count)
        //    {
        //        combine[i].mesh = meshes[i].sharedMesh;
        //        combine[i].transform = meshes[i].transform.localToWorldMatrix;
        //        meshes[i].gameObject.SetActive(false);
        //        //yield return null;
        //        i++;
        //    }
        //    convexHull.GetComponent<MeshFilter>().mesh = new Mesh();
        //    convexHull.GetComponent<MeshFilter>().mesh.CombineMeshes(combine);
        //    convexHull.GetComponent<MeshRenderer>().material = skeletonMaterial;

        //    convexHull.gameObject.SetActive(true);

        //    //CreateGraphSkeleton(convexHull);
        //    //return convexHull.gameObject;
        //}

        /// <summary>
        /// Method to greate lines between nodes in the octree. This gives a sort of skeleton representation of the graph.
        /// </summary>
        public IEnumerator CreateGraphSkeleton(bool empty = false)
        {
            if (empty)
            {
                convexHull = Instantiate(emptySkeletonPrefab);
            }
            else
            {
                convexHull = Instantiate(skeletonPrefab);
            }
            var nodes = octreeRoot.GetSkeletonNodesRecursive(octreeRoot, null, 0, 4);
            int posCount = nodes.Count;
            nodes.OrderBy(v => v.center.x).ToList();
            var sortedNodes = new List<Vector3>();
            var subNodes = nodes;
            int frameCount = 0;
            while (nodes.Count > 0)
            {
                var firstNode = subNodes[0];
                sortedNodes.Add(firstNode.center);
                nodes.Remove(firstNode);
                subNodes = nodes.OrderBy(v => Vector3.Distance(firstNode.center, v.center)).ToList();
                frameCount++;
                if (nodes.Count % 60 == 0)
                    yield return null;
            }
            LineRenderer line = convexHull.gameObject.AddComponent<LineRenderer>();
            line.material = lineMaterial;
            line.startWidth = line.endWidth = 0.02f;
            line.useWorldSpace = false;
            line.alignment = LineAlignment.TransformZ;
            line.positionCount = posCount;
            line.SetPositions(sortedNodes.ToArray());

            //convexHull.GetComponent<BoxCollider>().size = GetComponent<BoxCollider>().size;
            convexHull.SetActive(true);

            //return convexHull;
        }



        public IEnumerator CreateGeneMesh(string geneName)
        {
            string path = Directory.GetCurrentDirectory() + @"\Data\" + CellexalUser.DataSourceFolder + Path.PathSeparator + "gene1vertex" + ".mesh";
            List<List<Vector3>> meshes = new List<List<Vector3>>();
            List<Vector3> vertices = new List<Vector3>();
            CellexalLog.Log("Started reading " + path);
            //for (int i = 0; i < points.Count; ++i)
            //{
            //    vertices[i] = pointsPositions[i];
            //}

            ChunkManager chunkManager = GameObject.Instantiate(chunkManagerPrefab).GetComponent<ChunkManager>();
            yield return null;
            FileStream fileStream = new FileStream(path, FileMode.Open);
            StreamReader streamReader = new StreamReader(fileStream);


            string header = streamReader.ReadLine();
            while (!streamReader.EndOfStream)
            {
                string[] coords = streamReader.ReadLine().Split(new string[] { " ", "\t" }, StringSplitOptions.RemoveEmptyEntries);
                print(coords[0]);
                GraphPoint gp = FindGraphPoint(coords[0]);
                int onConvHull = int.Parse(coords[2]);
                //if (coords.Length != 3)
                //    continue;
                //int density = (int)(float.Parse(coords[4]) + float.Parse(coords[5]) + float.Parse(coords[6]) / 3.0);
                //chunkManager.addDensity((int)float.Parse(coords[1]), (int)float.Parse(coords[2]), (int)float.Parse(coords[3]), onConvHull);
                chunkManager.addDensity((int)gp.Position.x, (int)gp.Position.y, (int)gp.Position.z, onConvHull);
                //vertices.Add(new Vector3(float.Parse(coords[1]), float.Parse(coords[2]), float.Parse(coords[3])));
                //if (vertices.Count >= 65535 && !(streamReader.Peek() == -1))
                //{
                //    meshes.Add(new List<Vector3>(vertices));
                //    vertices.Clear();
                //}
            }
            streamReader.Close();
            fileStream.Close();
            //meshes.Add(vertices);
            //triangles = Enumerable.Range(0, vertices.Count).ToList();
            chunkManager.toggleSurfaceLevelandUpdateCubes(0);

            foreach (MeshFilter mf in chunkManager.GetComponentsInChildren<MeshFilter>())
            {
                mf.mesh.RecalculateBounds();
                mf.mesh.RecalculateNormals();
            }



            //meshes.Add(vertices);
            //triangles = Enumerable.Range(0, vertices.Count).ToList();

            //contour = Instantiate(contourParent);
            chunkManager.transform.parent = contour.transform;
            contour.transform.localPosition = Vector3.zero;
            contour.transform.localScale = Vector3.one * 0.25f;
            //BoxCollider bc = contour.AddComponent<BoxCollider>();
            //bc.center = Vector3.one * 4;
            //bc.size = Vector3.one * 7;


            // if vertices are reaching the max nr for a mesh we need to subdivide into several meshes.
            //if (meshes.Count > 0)
            //{
            //    foreach (List<Vector3> vertsList in meshes)
            //    {
            //        var convexHull = Instantiate(contourMeshPrefab, contour.transform).GetComponent<MeshFilter>();
            //        convexHull.gameObject.name = "ConvexHull_" + this.name;
            //        convexHull.mesh = new Mesh()
            //        {
            //            vertices = vertsList.ToArray(),
            //            triangles = Enumerable.Range(0, vertsList.Count).ToArray() // triangles.ToArray()
            //        };
            //        //convexHull.transform.parent = contour.transform;
            //        //convexHull.transform.localRotation = contour.transform.localRotation;
            //        //convexHull.GetComponent<MeshCollider>().sharedMesh = convexHull.mesh;
            //        convexHull.mesh.RecalculateBounds();
            //        convexHull.mesh.RecalculateNormals();
            //        convexHull.transform.localPosition = contour.transform.localPosition;
            //        if (geneName == "gene1")
            //        {
            //            convexHull.GetComponent<MeshRenderer>().material.color = new Color(255, 0, 0, 0.15f);
            //        }
            //        else if (geneName == "gene2")
            //        {
            //            convexHull.GetComponent<MeshRenderer>().material.color = new Color(0, 255, 0, 0.15f);
            //        }
            //        else
            //        {
            //            convexHull.GetComponent<MeshRenderer>().material.color = new Color(0, 0, 255, 0.15f);
            //        }
            //        //convexHull.gameObject.AddComponent<BoxCollider>();
            //    }
            //}

        }


        public IEnumerator CreateMeshFromAShape(string geneName)
        {

            string path = Directory.GetCurrentDirectory() + @"\Data\" + CellexalUser.DataSourceFolder + Path.PathSeparator + "gene1triang" + ".hull";
            string vertPath = Directory.GetCurrentDirectory() + @"\Data\" + CellexalUser.DataSourceFolder + Path.PathSeparator + geneName + ".mesh";

            FileStream fileStream = new FileStream(path, FileMode.Open);
            StreamReader streamReader = new StreamReader(fileStream);

            ChunkManager chunkManager = GameObject.Instantiate(chunkManagerPrefab).GetComponent<ChunkManager>();
            yield return null;
            List<List<Vector3>> meshes = new List<List<Vector3>>();
            List<Vector3> vertices = new List<Vector3>();
            using (StreamReader sr = new StreamReader(vertPath))
            {
                sr.ReadLine();
                while (!sr.EndOfStream)
                {
                    string[] line = sr.ReadLine().Split(null);
                    var gp = FindGraphPoint(line[1]);
                    //Color32 tex = texture.GetPixel(gp.textureCoord.x, gp.textureCoord.y);
                    //print(tex.r);
                    //float density = tex.r / 30;
                    //vertices.Add(gp.Position);
                    //chunkManager.addDensity((int)gp.Position.x, (int)gp.Position.y, (int)gp.Position.z, 1);
                    chunkManager.addDensity((int)float.Parse(line[1]), (int)float.Parse(line[2]), (int)float.Parse(line[3]), 1);
                }
            }
            List<int> triangles = new List<int>();
            CellexalLog.Log("Started reading " + path);
            //int i = 0;
            //foreach (KeyValuePair<string, GraphPoint> tuple in points)
            //{
            //    vertices[i] = tuple.Value.Position;
            //    i++;
            //}

            meshes.Add(new List<Vector3>(vertices));
            //contour = Instantiate(contourParent);
            chunkManager.toggleSurfaceLevelandUpdateCubes(0);



            foreach (MeshFilter mf in chunkManager.GetComponentsInChildren<MeshFilter>())
            {
                mf.gameObject.GetComponent<Renderer>().material = opaqueMat;
                mf.mesh.RecalculateBounds();
                mf.mesh.RecalculateNormals();
            }



            //meshes.Add(vertices);
            //triangles = Enumerable.Range(0, vertices.Count).ToList();

            //contour = Instantiate(contourParent);
            chunkManager.transform.parent = contour.transform;
            yield return null;
            chunkManager.transform.localScale = Vector3.one;
            chunkManager.transform.localPosition = Vector3.zero;
            chunkManager.transform.localRotation = Quaternion.identity;
            //string header = streamReader.ReadLine();
            //while (!streamReader.EndOfStream)
            //{
            //    string[] coords = streamReader.ReadLine().Split(new string[] { " ", "\t" }, StringSplitOptions.RemoveEmptyEntries);
            //if (coords.Length != 3)
            //    continue;
            // subtract 1 because R is 1-indexed
            //vertices.Add(FindGraphPoint(coords[0]).Position);
            //if (vertices.Count >= 65535 && !(streamReader.Peek() == -1))
            //{
            //    meshes.Add(new List<Vector3>(vertices));
            //    vertices.Clear();
            //}
            //print(vertices[int.Parse(coords[1])]);
            //triangles.Add(int.Parse(coords[1]) - 1);
            //triangles.Add(int.Parse(coords[2]) - 1);
            //triangles.Add(int.Parse(coords[3]) - 1);


        }
        //if (meshes.Count > 0)
        //{
        //    foreach (List<Vector3> vertsList in meshes)
        //    {
        //        var convexHull = Instantiate(contourMeshPrefab, contour.transform).GetComponent<MeshFilter>();
        //        convexHull.gameObject.AddComponent<MeshCollider>();
        //        convexHull.gameObject.name = "ConvexHull_" + this.name;
        //        convexHull.mesh = new Mesh()
        //        {
        //            vertices = vertsList.ToArray(),
        //            triangles = triangles.ToArray()
        //            //triangles = Enumerable.Range(0, vertsList.Count).ToArray() // triangles.ToArray()
        //        };
        //        convexHull.GetComponent<MeshCollider>().sharedMesh = convexHull.mesh;
        //        convexHull.mesh.RecalculateBounds();
        //        convexHull.mesh.RecalculateNormals();
        //        //convexHull.gameObject.AddComponent<BoxCollider>();
        //    }
        //}
        //meshes.Add(vertices);

        //CellexalLog.Log("Created convex hull with " + vertices.Count() + " vertices");
        //return contour.gameObject;

        public IEnumerator CreateMesh()
        {
            // Read the .hull file
            // The file format should be
            //  VERTEX_1    VERTEX_2    VERTEX_3
            //  VERTEX_1    VERTEX_2    VERTEX_3
            // ...
            // Each line is 3 integers that corresponds to graphpoints
            // 1 means the graphpoint that was created from the first line in the .mds file
            // 2 means the graphpoint that was created from the second line
            // and so on
            // Each line in the file connects three graphpoints into a triangle
            // One problem is that the lines are always ordered numerically so when unity is figuring out 
            // which way of the triangle is in and which is out, it's pretty much random what the result is.
            // The "solution" was to place a shader which does not cull the backside of the triangles, so 
            // both sides are always rendered.
            //string path = Directory.GetCurrentDirectory() + @"\Data\" + CellexalUser.DataSourceFolder + Path.PathSeparator + GraphName + ".mds";
            string path = Directory.GetCurrentDirectory() + @"\Data\" + CellexalUser.DataSourceFolder + Path.PathSeparator + GraphName + ".mds";
            FileStream fileStream = new FileStream(path, FileMode.Open);
            StreamReader streamReader = new StreamReader(fileStream);

            ChunkManager chunkManager = GameObject.Instantiate(chunkManagerPrefab).GetComponent<ChunkManager>();
            yield return null;
            //Vector3[] vertices = new Vector3[points.Count];
            List<List<Vector3>> meshes = new List<List<Vector3>>();
            List<Vector3> vertices = new List<Vector3>();
            CellexalLog.Log("Started reading " + path);
            //for (int i = 0; i < points.Count; ++i)
            //{
            //    vertices[i] = pointsPositions[i];
            //}

            string header = streamReader.ReadLine();
            while (!streamReader.EndOfStream)
            {
                string[] coords = streamReader.ReadLine().Split(new string[] { " ", "\t" }, StringSplitOptions.RemoveEmptyEntries);
                //if (coords.Length != 3)
                //    continue;
                chunkManager.addDensity((int)float.Parse(coords[1]), (int)float.Parse(coords[2]), (int)float.Parse(coords[3]), 1);

                //vertices.Add(new Vector3(float.Parse(coords[1]), float.Parse(coords[2]), float.Parse(coords[3])));
                //if (vertices.Count >= 65535 && !(streamReader.Peek() == -1))
                //{
                //    meshes.Add(new List<Vector3>(vertices));
                //    vertices.Clear();
                //}
            }
            chunkManager.toggleSurfaceLevelandUpdateCubes(0);



            foreach (MeshFilter mf in chunkManager.GetComponentsInChildren<MeshFilter>())
            {
                mf.mesh.RecalculateBounds();
                mf.mesh.RecalculateNormals();
            }



            meshes.Add(vertices);
            //triangles = Enumerable.Range(0, vertices.Count).ToList();

            streamReader.Close();
            fileStream.Close();

            contour = Instantiate(contourParent);
            chunkManager.transform.parent = contour.transform;
            contour.transform.localScale = Vector3.one * 0.25f;
            BoxCollider bc = contour.AddComponent<BoxCollider>();
            bc.center = Vector3.one * 4;
            bc.size = Vector3.one * 6;


            // if vertices are reaching the max nr for a mesh we need to subdivide into several meshes.
            //if (meshes.Count > 0)
            //{
            //    foreach (List<Vector3> vertsList in meshes)
            //    {
            //        var convexHull = Instantiate(contourMeshPrefab, contour.transform).GetComponent<MeshFilter>();
            //        convexHull.gameObject.name = "ConvexHull_" + this.name;
            //        convexHull.mesh = new Mesh()
            //        {
            //            vertices = vertsList.ToArray(),
            //            triangles = Enumerable.Range(0, vertsList.Count).ToArray() // triangles.ToArray()
            //        };
            //        convexHull.GetComponent<MeshCollider>().sharedMesh = convexHull.mesh;
            //        convexHull.mesh.RecalculateBounds();
            //        convexHull.mesh.RecalculateNormals();
            //        convexHull.gameObject.AddComponent<BoxCollider>();
            //    }
            //}
            //contour.AddComponent<BoxCollider>();
            //contour.AddComponent<MeshCollider>();
            //else
            //{
            //    var convexHull = Instantiate(skeletonPrefab).GetComponent<MeshFilter>();
            //    convexHull.gameObject.name = "ConvexHull_" + this.name;
            //    convexHull.mesh = new Mesh()
            //    {
            //        vertices = vertices.ToArray(),
            //        triangles = triangles.ToArray()
            //    };
            //    convexHull.GetComponent<MeshCollider>().sharedMesh = convexHull.mesh;
            //    convexHull.mesh.RecalculateBounds();
            //    convexHull.mesh.RecalculateNormals();
            //}


            //if (gameManager.multiplayer)
            //{
            //    convexHull.transform.position = new Vector3(0, 1f, 0);
            //}
            //if (!gameManager.multiplayer)
            //{
            //    convexHull.transform.position = referenceManager.rightController.transform.position;
            //}
            // move the convexhull slightly out of the way of the graph
            // in a direction sort of pointing towards the middle.
            // otherwise it lags really bad when the skeleton is first 
            // moved out of the original graph
            //Vector3 moveDist = new Vector3(0f, 0.3f, 0f);
            //if (transform.position.x > 0) moveDist.x = -.2f;
            //if (transform.position.z > 0) moveDist.z = -.2f;
            //convexHull.transform.Translate(moveDist);
            //convexHull.transform.position += referenceManager.rightController.transform.forward * 1f;
            //convexHull.transform.rotation = transform.rotation;
            //convexHull.transform.localScale = transform.localScale;
            //convexHull.transform.position = Vector3.zero;
            CellexalLog.Log("Created convex hull with " + vertices.Count() + " vertices");
            //return contour.gameObject;
        }
        private void DrawDebugCube(Color color, Vector3 min, Vector3 max, bool inWorldSpace = false)
        {
            if (!inWorldSpace)
            {
                min += transform.position;
                max += transform.position;
            }
            Gizmos.color = color;
            Vector3[] corners = new Vector3[]
            {
           min,
           new Vector3(max.x, min.y, min.z),
           new Vector3(min.x, max.y, min.z),
           new Vector3(min.x, min.y, max.z),
           new Vector3(max.x, max.y, min.z),
           new Vector3(max.x, min.y, max.z),
           new Vector3(min.x, max.y, max.z),
           max
            };

            Gizmos.DrawLine(corners[0], corners[1]);
            Gizmos.DrawLine(corners[0], corners[2]);
            Gizmos.DrawLine(corners[0], corners[3]);
            Gizmos.DrawLine(corners[1], corners[4]);
            Gizmos.DrawLine(corners[1], corners[5]);
            Gizmos.DrawLine(corners[2], corners[4]);
            Gizmos.DrawLine(corners[2], corners[6]);
            Gizmos.DrawLine(corners[3], corners[5]);
            Gizmos.DrawLine(corners[3], corners[6]);
            Gizmos.DrawLine(corners[4], corners[7]);
            Gizmos.DrawLine(corners[5], corners[7]);
            Gizmos.DrawLine(corners[6], corners[7]);

        }

        private void DrawRejectionApproveCubes(OctreeNode node)
        {
            if (node.rejected)
            {
                node.rejected = false;
                DrawDebugCube(Color.red, node.pos, node.pos + node.size);
            }
            else if (node.completelyInside)
            {
                node.completelyInside = false;
                DrawDebugCube(Color.green, node.pos, node.pos + node.size);
            }

            foreach (var child in node.children)
            {
                DrawRejectionApproveCubes(child);
            }
        }

        /// <summary>
        /// Tells this graph that all graphpoints are added to this graph and we can update the info text.
        /// </summary>
        public void SetInfoText()
        {
            //graphInfoText.transform.parent.localPosition = ScaleCoordinates(maxCoordValues.x - (maxCoordValues.x - minCoordValues.x) / 2, maxCoordValues.y, maxCoordValues.z);
            //graphInfoText.text = "Points: " + points.Count;
            graphNameText.text += "\n Points: " + points.Count;
            //SetInfoTextVisible(true);
        }

        /// <summary>
        /// Set this graph's info panel visible or not visible.
        /// </summary>
        /// <param name="visible"> True for visible, false for invisible </param>
        public void SetInfoTextVisible(bool visible)
        {
            infoParent.SetActive(visible);
        }

        /// <summary>
        /// Set this graph's axes visible or not visible.
        /// </summary>
        /// <param name="visible"> True for visible, false for invisible </param>
        public void SetAxesVisible(bool visible)
        {
            if (axes != null)
            {
                axes.SetActive(visible);
            }
        }

        public void ResetPosition()
        {
            transform.position = startPosition;
        }

        public void ResetSizeAndRotation()
        {
            transform.localScale = Vector3.one;
            transform.localRotation = Quaternion.identity;
        }

        /// <summary>
        /// Resets the color of all graphpoints in this graph to white.
        /// </summary>
        public void ResetColors(bool resetGroup = true)
        {
            for (int i = 0; i < textureWidth; ++i)
            {
                for (int j = 0; j < textureHeight; ++j)
                {
                    texture.SetPixel(i, j, Color.red);
                }
            }
            texture.Apply();

            //foreach (CombinedGraphPoint p in points.Values)
            //{
            //    p.Group = -1;
            //}
            if (resetGroup)
            {
                octreeRoot.Group = -1;
            }
        }

        /// <summary>
        /// Toggles all graphpoint cluster gameobjects active/inactive.
        /// </summary>
        public void ToggleGraphPoints()
        {
            graphPointsInactive = !graphPointsInactive;
            foreach (GameObject cluster in graphPointClusters)
            {
                cluster.SetActive(!cluster.activeSelf);
            }
        }


        public Color GetGraphPointColor(GraphPoint gp)
        {
            int group = (int)(255 * texture.GetPixel(gp.textureCoord.x, gp.textureCoord.y).r);
            return graphGenerator.graphPointColors.GetPixel(group, 0);
        }

        public void Party()
        {
            for (int i = 0; i < textureWidth; ++i)
            {
                for (int j = 0; j < textureHeight; ++j)
                {
                    // green channel above 0.9
                    texture.SetPixel(i, j, Color.green);
                }
            }
            texture.Apply();
        }



        /// <summary>
        /// Clears the circles from previous colouring so it doesn't stack.
        /// </summary>
        public void ClearTopExprCircles()
        {
            foreach (GameObject circle in topExprCircles)
            {
                Destroy(circle);
            }
            topExprCircles.Clear();
        }

        /// <summary>
        /// Recolors a single graphpoint.
        /// </summary>
        /// <param name="graphPoint">The graphpoint to recolor.</param>
        /// <param name="color">The graphpoint's new color.</param>
        public void ColorGraphPointGeneExpression(GraphPoint graphPoint, int i, bool outline)
        {
            //byte greenChannel = (byte)(outline || i > 27 ? 27 : 0);
            // TODO CELLEXAL: make the most expressed a percent of the total or something that isn't a hard coded 27
            if (i > 27 && CellexalConfig.Config.GraphMostExpressedMarker)
            {
                var circle = Instantiate(movingOutlineCircle);
                circle.GetComponent<MovingOutlineCircle>().camera = referenceManager.headset.transform;
                circle.transform.position = graphPoint.WorldPosition;
                circle.transform.parent = transform;
                topExprCircles.Add(circle);
            }
            else if (i == -1)
            {
                i = 255;
            }
            Color32 finalColor = new Color32((byte)i, 0, 0, 255);
            texture.SetPixels32(graphPoint.textureCoord.x, graphPoint.textureCoord.y, 1, 1, new Color32[] { finalColor });
            textureChanged = true;
        }

        /// <summary>
        /// Colors one graph point based on one of the selection colors.
        /// </summary>
        /// <param name="graphPoint">The graph point to color.</param>
        /// <param name="i">An integer betweeen 0 and the number of selection colors.</param>
        /// <param name="outline">True if the graph point should get an outline as well, false otherwise.</param>
        public void ColorGraphPointSelectionColor(GraphPoint graphPoint, int i, bool outline)
        {
            byte greenChannel = (byte)(outline ? 4 : 0);
            byte redChannel;
            if (i == -1)
            {
                redChannel = 255;
            }
            else
            {
                redChannel = (byte)(nbrOfExpressionColors + i);
            }
            Color32 finalColor = new Color32(redChannel, greenChannel, 0, 255);
            texture.SetPixels32(graphPoint.textureCoord.x, graphPoint.textureCoord.y, 1, 1, new Color32[] { finalColor });
            textureChanged = true;
        }


        public void MakePointTransparent(GraphPoint graphPoint, bool active)
        {
            Color32 tex = texture.GetPixel(graphPoint.textureCoord.x, graphPoint.textureCoord.y);
            // for thicker outline 0.1 < g < 0.2 ( 0.1 < (38 / 255) < 0.2 )
            byte greenChannel = (byte)(active ? 180 : 0);
            Color32 finalColor = new Color32(tex.r, greenChannel, 0, 255);
            texture.SetPixels32(graphPoint.textureCoord.x, graphPoint.textureCoord.y, 1, 1, new Color32[] { finalColor });
            textureChanged = true;

        }


        public void HighlightGraphPoint(GraphPoint graphPoint, bool active)
        {
            Color32 tex = texture.GetPixel(graphPoint.textureCoord.x, graphPoint.textureCoord.y);
            // for thicker outline 0.1 < g < 0.2 ( 0.1 < (38 / 255) < 0.2 )
            byte greenChannel = (byte)(active ? 38 : 0);
            Color32 finalColor = new Color32(tex.r, greenChannel, 0, 255);
            texture.SetPixels32(graphPoint.textureCoord.x, graphPoint.textureCoord.y, 1, 1, new Color32[] { finalColor });
            textureChanged = true;

        }
        /// <summary>
        /// Resets the color of the graph point back to its default.
        /// </summary>
        /// <param name="graphPoint">The graphpoint to recolor.</param>
        public void ResetGraphPointColor(GraphPoint graphPoint)
        {
            if (isTransparent)
            {
                texture.SetPixels32(graphPoint.textureCoord.x, graphPoint.textureCoord.y, 1, 1, new Color32[] { new Color32(254, 0, 0, 255) });
            }
            else
            {
                texture.SetPixels32(graphPoint.textureCoord.x, graphPoint.textureCoord.y, 1, 1, new Color32[] { new Color32(255, 0, 0, 255) });
            }
            textureChanged = true;
        }
        /// <summary>
        /// Finds a graph point by its name.
        /// </summary>
        /// <param name="label">The name of the graph point</param>
        /// <returns>The graph point, or null if was not found.</returns>
        public GraphPoint FindGraphPoint(string label)
        {
            var keyExists = points.TryGetValue(label, out GraphPoint gp);
            return keyExists ? gp : null;
        }


        /// <summary>
        /// Color all graphpoints in this graph with the expression of some gene.
        /// </summary>
        /// <param name="expressions">An arraylist with <see cref="CellExpressionPair"/>.</param>
        public void ColorByGeneExpression(ArrayList expressions)
        {
            // expression values are saved in the textures red channel
            // cells that have 0 (or whatever the lowest is) expression are not in the results
            // fill entire background with the lowest expression color
            for (int i = 0; i < textureWidth; ++i)
            {
                for (int j = 0; j < textureHeight; ++j)
                {
                    //texture.SetPixel(i, j, Color.black);

                    texture.SetPixels32(i, j, 1, 1, new Color32[] { new Color32(254, 0, 0, 255) });
                }
            }
            MakeAllPointsTransparent(true);

            int nbrOfExpressionColors = CellexalConfig.Config.GraphNumberOfExpressionColors;
            Color32[][] colorValues = new Color32[nbrOfExpressionColors][];
            for (byte i = 0; i < nbrOfExpressionColors - 3; ++i)
            {
                colorValues[i] = new Color32[] { new Color32(i, 0, 0, 1) };
            }
            for (byte i = (byte)(nbrOfExpressionColors - 3); i < nbrOfExpressionColors; ++i)
            {
                colorValues[i] = new Color32[] { new Color32(i, 0, 0, 1) };
            }
            int topExpressedThreshold = (int)(nbrOfExpressionColors - nbrOfExpressionColors / 10f);
            if (CellexalConfig.Config.GraphMostExpressedMarker)
            {
                ClearTopExprCircles();
            }
            foreach (CellExpressionPair pair in expressions)
            {
                // If this is a subgraph it does not contain all cells...
                if (points.ContainsKey(pair.Cell))
                {
                    Vector2Int pos = points[pair.Cell].textureCoord;
                    int expressionColorIndex = pair.Color;
                    if (pair.Color >= nbrOfExpressionColors)
                    {
                        expressionColorIndex = nbrOfExpressionColors - 1;
                    }

                    if (CellexalConfig.Config.GraphMostExpressedMarker && pair.Color >= topExpressedThreshold && !minimized)
                    {
                        var circle = Instantiate(movingOutlineCircle);
                        circle.GetComponent<MovingOutlineCircle>().camera = referenceManager.headset.transform;
                        circle.transform.position = points[pair.Cell].WorldPosition;
                        circle.transform.parent = transform;
                        topExprCircles.Add(circle);
                    }
                    texture.SetPixels32(pos.x, pos.y, 1, 1, colorValues[expressionColorIndex]);
                }
            }

            texture.Apply();
            //var pixels = texture.GetPixels32(0);
            //int[] hist = new int[nbrOfExpressionColors];
            //for (int i = 0; i < pixels.Length; ++i)
            //{
            //    hist[pixels[i].r]++;
            //}

            graphPointClusters[0].GetComponent<Renderer>().sharedMaterial.mainTexture = texture;
        }

        /// <summary>
        /// Finds all <see cref="GraphPoint"/> that are inside the selection tool. This is done by traversing the generated Octree and dismissing subtrees using Minkowski differences.
        /// Ultimately, raycasting is used to find collisions because the selection tool is not a box.
        /// </summary>
        /// <param name="selectionToolPos">The selection tool's position in world space.</param>
        /// <param name="selectionToolBoundsCenter">The selection tool's bounding box's center in world space.</param>
        /// <param name="selectionToolBoundsExtents">The selection tool's bounding box's extents in world space.</param>
        /// <param name="group">The group that the selection tool is set to color the graphpoints by.</param>
        /// <returns>A <see cref="List{CombinedGraphPoint}"/> with all <see cref="GraphPoint"/> that are inside the selecion tool.</returns>
        public List<GraphPoint> MinkowskiDetection(Vector3 selectionToolPos, Vector3 selectionToolBoundsCenter, Vector3 selectionToolBoundsExtents, int group)
        {
            List<GraphPoint> result = new List<GraphPoint>(64);

            // calculate a new (non-minimal) bounding box for the selection tool, in the graph's local space
            Vector3 center = transform.InverseTransformPoint(selectionToolBoundsCenter);

            Vector3 axisX = transform.InverseTransformVector(selectionToolBoundsExtents.x, 0, 0);
            Vector3 axisY = transform.InverseTransformVector(0, selectionToolBoundsExtents.y, 0);
            Vector3 axisZ = transform.InverseTransformVector(0, 0, selectionToolBoundsExtents.z);

            float extentsx = Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x);
            float extentsy = Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y);
            float extentsz = Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z);

            extentsx = Mathf.Max(extentsx, 0.05f);
            extentsy = Mathf.Max(extentsy, 0.05f);
            extentsz = Mathf.Max(extentsz, 0.05f);

            Vector3 extents = new Vector3(extentsx, extentsy, extentsz);
            Vector3 min = center - extents;
            Vector3 max = center + extents;

#if UNITY_EDITOR
            debugGizmosMin = min;
            debugGizmosMax = max;
            debugGizmosPos = selectionToolPos;
#endif

            MinkowskiDetectionRecursive(selectionToolPos, min, max, octreeRoot, group, ref result);
            return result;
        }

        /// <summary>
        /// Recursive function to traverse the Octree and find collisions with the selection tool.
        /// </summary>
        /// <param name="selectionToolWorldPos">The selection tool's position in world space.</param>
        /// <param name="boundingBoxMin">A <see cref="Vector3"/> comprised of the smallest x, y and z coordinates of the selection tool's bounding box.</param>
        /// <param name="boundingBoxMax">A <see cref="Vector3"/> comprised of the largest x, y and z coordinates of the selection tool's bounding box.</param>
        /// <param name="node">The <see cref="OctreeNode"/> to evaluate.</param>
        /// <param name="group">The group to assign the node.</param>
        /// <param name="result">All leaf nodes found so far.</param>
        private void MinkowskiDetectionRecursive(Vector3 selectionToolWorldPos, Vector3 boundingBoxMin, Vector3 boundingBoxMax, OctreeNode node, int group, ref List<GraphPoint> result)
        {
            // minkowski difference selection tool bounding box and node
            // take advantage of both being AABB
            // check if result contains (0,0,0)
            if (boundingBoxMin.x - node.pos.x - node.size.x <= 0
                && boundingBoxMax.x - node.pos.x >= 0
                && boundingBoxMin.y - node.pos.y - node.size.y <= 0
                && boundingBoxMax.y - node.pos.y >= 0
                && boundingBoxMin.z - node.pos.z - node.size.z <= 0
                && boundingBoxMax.z - node.pos.z >= 0)
            {
                // check if this node is entirely inside the bounding box
                if (boundingBoxMin.x < node.pos.x && boundingBoxMax.x > node.pos.x + node.size.x &&
                    boundingBoxMin.y < node.pos.y && boundingBoxMax.y > node.pos.y + node.size.y &&
                    boundingBoxMin.z < node.pos.z && boundingBoxMax.z > node.pos.z + node.size.z)
                {
                    // just find the leaves and check if they are inside
                    if (node.Group != group)
                    {
                        node.completelyInside = true;
                        CheckIfLeavesInside(selectionToolWorldPos, node, group, ref result);
                    }
                    return;
                }

                // check if this is a leaf node that is inside the selection tool. Can't rely on bounding boxes here, have to raycast to find collisions
                if (node.point != null && node.Group != group && node.PointInsideSelectionTool(selectionToolWorldPos, transform.TransformPoint(node.center)))
                {
                    node.Group = group;
                    result.Add(node.point);
                }
                else
                {
                    // recursion
                    foreach (var child in node.children)
                    {
                        if (child.Group != group)
                        {
                            MinkowskiDetectionRecursive(selectionToolWorldPos, boundingBoxMin, boundingBoxMax, child, group, ref result);
                        }
                    }
                }
            }
            else
            {
                node.rejected = true;
            }
        }

        /// <summary>
        /// Recursive function to traverse the Octree and find collisions with the selection tool.
        /// </summary>
        /// <param name="selectionToolWorldPos">The selection tool's position in world space.</param>
        /// <param name="node">The to evaluate.</param>
        /// <param name="group">The group to assign the node.</param>
        /// <param name="result">All leaf nodes found so far.</param>
        private void CheckIfLeavesInside(Vector3 selectionToolWorldPos, OctreeNode node, int group, ref List<GraphPoint> result)
        {
            if (node.point != null && node.PointInsideSelectionTool(selectionToolWorldPos, transform.TransformPoint(node.center)))
            {
                node.Group = group;
                result.Add(node.point);
                return;
            }
            foreach (var child in node.children)
            {
                if (child.Group == group)
                {
                    continue;
                }
                if (child.point != null && child.PointInsideSelectionTool(selectionToolWorldPos, transform.TransformPoint(child.center)))
                {
                    child.Group = group;
                    result.Add(child.point);
                }
                else
                {
                    CheckIfLeavesInside(selectionToolWorldPos, child, group, ref result);
                }
            }
        }
    }
}