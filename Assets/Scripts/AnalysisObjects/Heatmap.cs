using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using VRTK;
using System.Collections;
using System.Drawing.Imaging;
using System.Threading;
using TMPro;
using System.Drawing;
using CellexalVR.Menu.Buttons.Heatmap;
using CellexalVR.Menu.Buttons.Report;
using CellexalVR.General;
using CellexalVR.AnalysisLogic;
using CellexalVR.Interaction;
using CellexalVR.Menu.Buttons;
using CellexalVR.Extensions;
using System.Linq;
using CellexalVR.Multiuser;
using CellexalVR.Tools;

namespace CellexalVR.AnalysisObjects
{
    /// <summary>
    /// This class represents a heatmap. Contains methods for calling r-script, building texture and interaction methods etc.
    /// </summary>
    public class Heatmap : MonoBehaviour
    {
        #region Public variables
        public ReferenceManager referenceManager;
        public Texture texture;
        public TextMeshPro infoText;
        public TextMeshPro statusText;
        public TextMeshPro barInfoText;
        public SaveHeatmapButton saveImageButton;
        public GOanalysisButton goAnalysisButton;
        public GameObject highlightQuad;
        public GameObject highlightGeneQuad;
        public GameObject confirmQuad;
        public GameObject movingQuadX;
        public GameObject movingQuadY;
        public int selectionNr;
        public string selectionFile;
        public TextMeshPro enlargedGeneText;
        public TextMeshPro highlightGeneText;
        public TextMeshPro highlightInfoText;
        public bool removable;
        public string directory;
        public Bitmap bitmap;
        public List<Graph.GraphPoint> selection;

        /// <summary>
        /// Item1: group number, Item2: group width in coordinates, Item3: number of cells in the group
        /// </summary>
        [HideInInspector]
        public List<Tuple<int, float, int>> groupWidths; // these are numbers ranging [0, groupWidths.Length)
        public List<Tuple<int, float, int>> attributeWidths;
        public List<Tuple<Cell, int>> cellAttributes;
        public Dictionary<int, UnityEngine.Color> groupingColors; //// these are numbers ranging [0, genes.Length)
        public Dictionary<int, UnityEngine.Color> attributeColors;
        public Cell[] cells;
        [HideInInspector]
        public bool createAnim = false;
        [HideInInspector]
        public bool orderedByAttribute = false;
        [HideInInspector]
        public bool buildingTexture = false;
        [HideInInspector]
        public string[] genes;
        [HideInInspector]
        public int bitmapWidth = 4096;
        [HideInInspector]
        public int bitmapHeight = 4096;
        [HideInInspector]
        public int heatmapX = 250;
        //[HideInInspector]
        public int heatmapY = 330;
        //[HideInInspector]
        public int heatmapWidth = 3596;
        //[HideInInspector]
        public int heatmapHeight = 3596;
        //[HideInInspector]
        public int geneListX = 3846;
        //[HideInInspector]
        public int geneListWidth = 250;
        //[HideInInspector]
        public int attributeBarY = 10;
        //[HideInInspector]
        public int groupBarY = 170;
        //[HideInInspector]
        public int groupBarHeight = 140;
        //[HideInInspector]
        public int attributeBarHeight = 140;
        [HideInInspector]
        public int noAttribute = -2; // different from -1 and other colour indices.
        [HideInInspector]
        public int lastAttribute = -1;
        [HideInInspector]
        public int attributeWidth = 0;
        #endregion region
        

        #region Private variables
        private MultiuserMessageSender multiuserMessageSender;
        private HeatmapGenerator heatmapGenerator;


        // For creation animation
        private float targetScale;
        private float speed;
        private float shrinkSpeed;
        private Vector3 target;
        // Minimizing
        private Vector3 originalPos;
        private Quaternion originalRot;
        private Vector3 originalScale;
        private float targetMinScale;
        private bool minimize;
        private bool maximize;
        private bool highlight;
        private bool delete;
        private float highlightTime = 0;
        private float currentTime = 0;
        private float animationTime = 0.7f;
        #endregion

        private void OnValidate()
        {
            if (gameObject.scene.IsValid())
            {
                referenceManager = GameObject.Find("InputReader").GetComponent<ReferenceManager>();
            }
        }

        void Start()
        {
            targetScale = 2f;
            speed = 2f;
            shrinkSpeed = 3f;
            transform.localScale = new Vector3(0f, 0f, 0f);
            target = new Vector3(1.4f, 1.2f, 0.05f);
            originalPos = originalScale = new Vector3();
            originalRot = new Quaternion();
        }

        public void Init()
        {
            referenceManager = GameObject.Find("InputReader").GetComponent<ReferenceManager>();
            GetComponent<HeatmapGrab>().referenceManager = referenceManager;
            //if (CrossSceneInformation.Normal)
            //{
            //    rightController = referenceManager.rightController;
            //    controllerModelSwitcher = referenceManager.controllerModelSwitcher;
            //}
            multiuserMessageSender = referenceManager.multiuserMessageSender;
            highlightQuad.SetActive(false);
            highlightGeneQuad.SetActive(false);
            confirmQuad.SetActive(false);
            movingQuadX.SetActive(false);
            movingQuadY.SetActive(false);
            highlightInfoText = highlightQuad.GetComponentInChildren<TextMeshPro>();
            heatmapGenerator = referenceManager.heatmapGenerator;
            highlightQuad.GetComponent<Renderer>().material.color = heatmapGenerator.HighlightMarkerColor;
            confirmQuad.GetComponent<Renderer>().material.color = heatmapGenerator.ConfirmMarkerColor;
            foreach (CellexalButton b in GetComponentsInChildren<CellexalButton>())
            {
                b.referenceManager = referenceManager;
            }
        }

        void Update()
        {
            //if (device == null && CrossSceneInformation.Normal)
            //{
            //    device = SteamVR_Controller.Input((int)rightController.index);
            //}
            if (createAnim)
            {
                CreateHeatmapAnimation();
            }
            if (minimize)
            {
                Minimize();
            }
            if (maximize)
            {
                Maximize();
            }
            if (highlight)
            {
                highlightTime += Time.deltaTime;
                if (highlightTime > 6f)
                {
                    highlight = false;
                    highlightTime = 0;
                    highlightGeneQuad.SetActive(false);
                    highlightGeneText.text = "";
                }
            }

            if (GetComponent<VRTK_InteractableObject>().IsGrabbed())
            {
                multiuserMessageSender.SendMessageMoveHeatmap(name, transform.position, transform.rotation, transform.localScale);
            }

        }

        private void CreateHeatmapAnimation()
        {
            if (transform.localScale.x >= targetScale)
            {
                createAnim = false;
                return;
            }

            float step = speed * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, target, step);
            transform.localScale += Vector3.one * Time.deltaTime * shrinkSpeed;

        }

        /// <summary>
        /// Starts the deletion of heatmap. Same animation as when minimizing but destroying the object once minimized.
        /// </summary>
        public void DeleteHeatmap()
        {
            minimize = true;
            delete = true;
        }

        /// <summary>
        /// Starts the minimize animation and hides the heatmap.
        /// </summary>
        internal void HideHeatmap()
        {
            originalPos = transform.position;
            originalRot = transform.localRotation;
            originalScale = transform.localScale;
            foreach (Collider c in GetComponentsInChildren<Collider>())
            {
                c.enabled = false;
            }
            currentTime = 0;
            shrinkSpeed = (transform.localScale.x - targetScale) / animationTime;
            minimize = true;
        }

        /// <summary>
        /// Starts the maximize animation and shows the heatmap.
        /// </summary>
        private void Minimize()
        {
            Vector3 targetPosition;
            float step = speed * Time.deltaTime;
            if (delete)
            {
                targetPosition = referenceManager.deleteTool.transform.position;
            }
            else
            {
                targetPosition = referenceManager.minimizedObjectHandler.transform.position;
            }
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, step);
            transform.localScale -= Vector3.one * Time.deltaTime * shrinkSpeed;
            transform.Rotate(Vector3.one * Time.deltaTime * 50);
            if (Mathf.Abs(currentTime - animationTime) <= 0.05f || transform.localScale.x < 0)
            {
                if (delete)
                {
                    referenceManager.deleteTool.GetComponent<RemovalController>().ResetHighlight();
                    Destroy(this.gameObject);
                }
                else
                {
                    minimize = false;
                    foreach (Renderer r in GetComponentsInChildren<Renderer>())
                        r.enabled = false;
                    GetComponent<Renderer>().enabled = false;
                    referenceManager.minimizeTool.GetComponent<Light>().range = 0.04f;
                    referenceManager.minimizeTool.GetComponent<Light>().intensity = 0.8f;
                }
            }
            currentTime += Time.deltaTime;
        }

        /// <summary>
        /// Starts the maximize animation and show the heatmap.
        /// </summary>
        internal void ShowHeatmap()
        {
            transform.position = referenceManager.minimizedObjectHandler.transform.position;
            foreach (Renderer r in GetComponentsInChildren<Renderer>())
                r.enabled = true;
            GetComponent<Renderer>().enabled = true;
            currentTime = 0;
            shrinkSpeed = (originalScale.x - transform.localScale.x) / animationTime;
            maximize = true;
        }

        /// <summary>
        /// Starts the minimize animation and hides the heatmap.
        /// </summary>
        private void Maximize()
        {
            float step = speed * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, originalPos, step);
            transform.localScale += Vector3.one * Time.deltaTime * shrinkSpeed;
            transform.Rotate(Vector3.one * Time.deltaTime * -50);
            if (Mathf.Abs(currentTime - animationTime) <= 0.05f)
            {
                transform.localScale = originalScale;
                transform.position = originalPos;
                transform.rotation = originalRot;
                maximize = false;
                foreach (Collider c in GetComponentsInChildren<Collider>())
                    c.enabled = true;
            }
            currentTime += Time.deltaTime;
        }

        /// <summary>
        /// Removes the highlight box and the highlight text.
        /// </summary>
        public void ResetHeatmapHighlight()
        {
            highlightInfoText.text = "";
            barInfoText.text = "";
            enlargedGeneText.gameObject.SetActive(false);
            highlightQuad.gameObject.SetActive(false);
        }

        /// <summary>
        /// Highlights a gene in the genelist if it is there.
        /// For example when colouring from keyboard it draws attention to the gene in the list.
        /// </summary>
        /// <param name="geneName">The name of the gene.</param>
        public void HighLightGene(string geneName)
        {
            int geneHit = Array.FindIndex(genes, s => s.Equals(geneName, StringComparison.InvariantCultureIgnoreCase));
            if (geneHit != -1)
            {
                float highlightMarkerWidth = (float)geneListWidth / bitmapWidth;
                float highlightMarkerHeight = ((float)heatmapHeight / bitmapHeight) / genes.Length;
                float highlightMarkerX = (float)geneListX / bitmapWidth + highlightMarkerWidth / 2 - 0.5f;
                float highlightMarkerY = -(float)heatmapY / bitmapHeight - geneHit * (highlightMarkerHeight) - highlightMarkerHeight / 2 + 0.5f;

                highlightGeneQuad.transform.localPosition = new Vector3(highlightMarkerX, highlightMarkerY, 0);
                highlightGeneQuad.transform.localScale = new Vector3(highlightMarkerWidth, highlightMarkerHeight, 1f);
                highlightGeneQuad.SetActive(true);
                highlightInfoText.text = "";
                highlightGeneText.text = genes[geneHit];
                highlightGeneText.transform.localPosition = new Vector3(highlightGeneText.transform.localPosition.x,
                                                    highlightGeneQuad.transform.localPosition.y + 0.09f, 0);
                highlightQuad.transform.localPosition = new Vector3(highlightMarkerX, highlightMarkerY, -0.001f);
                highlightQuad.transform.localScale = new Vector3(highlightMarkerWidth, highlightMarkerHeight, 1f);
                highlightQuad.SetActive(true);
                highlight = true;
            }
            else
            {
                highlightGeneText.text = geneName + " not in the heatmap list";
            }

        }

        /// <summary>
        /// Creates a new heatmap based on what was selected on this heatmap.
        /// </summary>
        public void CreateNewHeatmapFromSelection(int selectedGroupLeft, int selectedGroupRight, int selectedGeneTop,
            int selectedGeneBottom, float selectedBoxWidth, float selectedBoxHeight)
        {
            if (selectedBoxWidth == 0 || selectedBoxHeight == 0)
                return;

            // create a copy of this
            GameObject newHeatmap = Instantiate(gameObject);
            Heatmap hm = newHeatmap.GetComponent<Heatmap>();
            hm.transform.parent = referenceManager.heatmapGenerator.transform;
            heatmapGenerator.AddHeatmapToList(hm);
            hm.name = name + "_" + heatmapGenerator.heatmapsCreated;
            heatmapGenerator.heatmapsCreated++;
            hm.transform.Translate(0.1f, 0.1f, 0.1f, Space.Self);
            hm.groupingColors = groupingColors;
            hm.attributeColors = attributeColors;
            hm.orderedByAttribute = orderedByAttribute;
            // find out which indices the cells start and end at
            int cellsIndexStart = 0;
            for (int i = 0; i < selectedGroupLeft; ++i)
            {
                cellsIndexStart += groupWidths[i].Item3;
            }

            int numberOfCells = 0;
            for (int i = selectedGroupLeft; i <= selectedGroupRight; ++i)
            {
                numberOfCells += groupWidths[i].Item3;
            }

            Cell[] newCells = new Cell[numberOfCells];
            List<Graph.GraphPoint> newGps = new List<Graph.GraphPoint>();
            for (int i = 0, j = cellsIndexStart; i < numberOfCells; ++i, ++j)
            {
                newCells[i] = cells[j];
                //summertwerk
                if (cells[j].GraphPoints.Count == 0) continue;
                newGps.Add(cells[j].GraphPoints[0]);
            }
            hm.selection = newGps;
            string[] newGenes = new string[selectedGeneBottom - selectedGeneTop + 1];
            for (int i = selectedGeneTop, j = 0; i <= selectedGeneBottom; ++i, ++j)
            {
                newGenes[j] = genes[i];
            }

            // rebuild the groupwidth and list with the new widths.
            List<Tuple<int, float, int>> newGroupWidths = new List<Tuple<int, float, int>>();
            float newXCoordInc = (float)heatmapWidth / newCells.Length;
            for (int i = selectedGroupLeft; i <= selectedGroupRight; ++i)
            {
                Tuple<int, float, int> old = groupWidths[i];
                newGroupWidths.Add(new Tuple<int, float, int>(old.Item1, old.Item3 * newXCoordInc, old.Item3));
            }

            // need to dump selection to txt file for GO analysis script. But file creation counter should not increment
            // in case networks should be created on the selection that created the original heatmap.
            referenceManager.selectionManager.DumpSelectionToTextFile(newGps);
            referenceManager.selectionManager.fileCreationCtr--;
            hm.Init();
            try
            {
                heatmapGenerator.BuildTexture(newCells, newGenes, newGroupWidths, hm);
                hm.DumpGenesToTextFile(newGenes, hm.name);
            }
            catch (Exception e)
            {
                CellexalLog.Log("Could not create heatmap. " + e.StackTrace);
            }
            heatmapGenerator.selectionNr += 1;
            hm.selectionNr = heatmapGenerator.selectionNr;
        }


        /// <summary>
        /// Helper function to update the attribute bar widths. Used when creating a new hm from selection on old or moving groups around.
        /// </summary>
        public void UpdateAttributeWidhts()
        {
            cellAttributes = new List<Tuple<Cell, int>>();
            attributeColors = new Dictionary<int, UnityEngine.Color>();
            float cellWidth = (float)heatmapWidth / cells.Length;
            lastAttribute = -1;
            attributeWidth = 0;
            attributeWidths = new List<Tuple<int, float, int>>();
            for (int i = 0; i < cells.Length; ++i)
            {
                var cell = cells[i];
                var attributes = cell.Attributes;
                AddAttributeWidth(attributes, cellWidth, cell);
            }
            attributeWidths.Add(new Tuple<int, float, int>(lastAttribute, attributeWidth * cellWidth, attributeWidth));
        }

        /// <summary>
        /// Helper function to add attribute width and colour.
        /// </summary>
        public void AddAttributeWidth(Dictionary<string, int> attributes, float cellWidth, Cell cell)
        {
            int attribute;
            if (attributes.Count > 0)
            {
                attribute = attributes.First().Value;
                attributeColors[attribute] = referenceManager.selectionManager.GetColor(attribute);
            }
            else
            {
                attribute = noAttribute;
                attributeColors[noAttribute] = UnityEngine.Color.black;
            }
            cellAttributes.Add(new Tuple<Cell, int>(cell, attribute));
            if (lastAttribute == -1)
            {
                lastAttribute = attribute;
            }

            if (attribute != lastAttribute)
            {
                attributeWidths.Add(new Tuple<int, float, int>(lastAttribute, attributeWidth * cellWidth, (int)attributeWidth));
                attributeWidth = 0;
                lastAttribute = attribute;
            }
            attributeWidth++;
        }

        /// <summary>
        /// Reorders the heatmap so that each selection group is reordered based on the attributes. 
        /// Cells will be reordered so cells with the same attribute within the same group is next to each other.
        /// </summary>
        public void ReorderByAttribute()
        {
            int currentGroup = -1;
            int prevGroup = -1;
            int currentAttribute = 0;
            attributeWidth = 0;
            lastAttribute = -1;
            float cellWidth = (float)heatmapWidth / cells.Length;
            List<Tuple<Cell, int>> cellGroup = new List<Tuple<Cell, int>>();
            List<Tuple<Cell, int>> reorderedGroup = new List<Tuple<Cell, int>>();
            List<Tuple<Cell, int>> reorderedList = new List<Tuple<Cell, int>>();
            attributeWidths.Clear();
            foreach (Tuple<Cell, int> cellAttribute in cellAttributes)
            {
                currentAttribute = cellAttribute.Item2;
                if (cellAttribute.Item1.GraphPoints.Count == 0) continue;
                currentGroup = cellAttribute.Item1.GraphPoints[0].Group;
                if (prevGroup == -1)
                {
                    prevGroup = currentGroup;
                }
                if (currentGroup == prevGroup)
                {
                    cellGroup.Add(cellAttribute);
                    prevGroup = currentGroup;
                }
                else
                {
                    reorderedGroup = cellGroup.OrderBy(x => x.Item2).ToList();
                    reorderedList.AddRange(reorderedGroup);
                    cellGroup.Clear();
                    cellGroup.Add(cellAttribute);
                    prevGroup = currentGroup;
                    attributeWidth = 0;
                    lastAttribute = -1;
                    foreach (Tuple<Cell, int> ca in reorderedGroup)
                    {
                        if (lastAttribute == -1)
                        {
                            lastAttribute = ca.Item2;
                        }

                        if (ca.Item2 != lastAttribute)
                        {
                            attributeWidths.Add(new Tuple<int, float, int>(lastAttribute, attributeWidth * cellWidth, (int)attributeWidth));
                            attributeWidth = 0;
                            lastAttribute = ca.Item2;
                        }
                        attributeWidth++;
                    }
                    attributeWidths.Add(new Tuple<int, float, int>(lastAttribute, attributeWidth * cellWidth, attributeWidth));
                }
            }
            //last group
            reorderedGroup = cellGroup.OrderBy(x => x.Item2).ToList();
            reorderedList.ToList().AddRange(reorderedList);
            attributeWidth = 0;
            lastAttribute = -1;
            foreach (Tuple<Cell, int> ca in reorderedGroup)
            {
                if (lastAttribute == -1)
                {
                    lastAttribute = ca.Item2;
                }

                if (ca.Item2 != lastAttribute)
                {
                    attributeWidths.Add(new Tuple<int, float, int>(lastAttribute, attributeWidth * cellWidth, (int)attributeWidth));
                    attributeWidth = 0;
                    lastAttribute = ca.Item2;
                }
                attributeWidth++;
            }
            attributeWidths.Add(new Tuple<int, float, int>(lastAttribute, attributeWidth * cellWidth, attributeWidth));
            for (int i = 0; i < reorderedList.Count; i++)
            {
                cells[i] = reorderedList[i].Item1;
            }
            orderedByAttribute = true;

            StartCoroutine(heatmapGenerator.BuildTextureCoroutine(this));
        }

        /// <summary>
        /// Saves the image used to create this heatmap to another directory.
        /// The saved image will have the a name based on when the image is saved.
        /// </summary>
        public void SaveImage()
        {
            string heatmapImageDirectory = CellexalUser.UserSpecificFolder;
            if (!Directory.Exists(heatmapImageDirectory))
            {
                Directory.CreateDirectory(heatmapImageDirectory);
                CellexalLog.Log("Created directory " + heatmapImageDirectory);
            }

            heatmapImageDirectory += "\\Heatmap";
            if (!Directory.Exists(heatmapImageDirectory))
            {
                Directory.CreateDirectory(heatmapImageDirectory);
                CellexalLog.Log("Created directory " + heatmapImageDirectory);
            }

            string heatmapImageFilePath = heatmapImageDirectory + "\\" + name + "_" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".png";
            while (File.Exists(heatmapImageFilePath))
            {
                // append "_d" until the filenames no longer collide.
                // microsoft is removing the 260 character filename limit so this shouldn't run into too many problems
                // unless you press this button way too many times the same second
                heatmapImageFilePath += "_d";
            }
            //bitmap.Save(heatmapImageFilePath);
            heatmapGenerator.SavePNGtoDisk(this, heatmapImageFilePath);
            StartCoroutine(referenceManager.reportManager.LogHeatmap(heatmapImageFilePath, this));
        }

        /// <summary>
        /// Dumps the genes into a text file. 
        /// </summary>
        public void DumpGenesToTextFile(string[] genes, string name)
        {
            string filePath = (CellexalUser.UserSpecificFolder + "\\Heatmap\\" + name + ".txt").FixFilePath();
            using (StreamWriter file = new StreamWriter(filePath))
            {
                foreach (string gene in genes)
                {
                    file.Write(gene);
                    file.WriteLine();
                }
            }
        }



        /// <summary>
        /// Updates this heatmap's image.
        /// </summary>
        [Obsolete("Use BuildTexture")]
        public void UpdateImage(string filepath)
        {
            byte[] fileData = File.ReadAllBytes(filepath);
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(fileData);
            GetComponent<Renderer>().material.SetTexture("_MainTex", tex);
        }

        /// <summary>
        /// Recolours all graphs with the colors that the cells had when this heatmap was created.
        /// Graph points that are not part of this heatmap are not recoloured and will keep their colour.
        /// </summary>
        public void ColorCells()
        {
            var selectionManager = referenceManager.selectionManager;

            for (int i = 0, cellIndex = 0; i < groupWidths.Count; ++i)
            {

                int group = groupWidths[i].Item1;
                UnityEngine.Color groupColor = groupingColors[group];
                for (int j = 0; j < groupWidths[i].Item3; ++j, ++cellIndex)
                {
                    var graphPoint = cells[cellIndex].GraphPoints[0];

                    selectionManager.AddGraphpointToSelection(graphPoint, group, false);
                }
            }
        }

        /// <summary>
        /// Sets some variables. Should be called after a heatmap is instantiated.
        /// </summary>
        public void SetVars(Dictionary<Cell, int> colors)
        {
            // containedCells = new Dictionary<Cell, Color>();
            //containedCells = colors;
            infoText.text = "Total number of cells: " + colors.Count;
            // infoText.text += "\nNumber of colours: " + numberOfColours;
        }
    }
}