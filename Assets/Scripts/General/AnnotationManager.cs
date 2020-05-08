using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CellexalVR.AnalysisLogic;
using CellexalVR.AnalysisObjects;
using CellexalVR.DesktopUI;
using CellexalVR.Extensions;
using CellexalVR.Interaction;
using TMPro;
using UnityEngine;

namespace CellexalVR.General
{
    /// <summary>
    /// Handles annotations on graphs. Annotations are notes added to certain groups of cells.
    /// They can be read from file or added via the gene keyboard.
    /// </summary>
    public class AnnotationManager : MonoBehaviour
    {
        public GameObject annotationTextPrefab;
        public ReferenceManager referenceManager;
        private int annotationCtr = 0;
        private GraphManager graphManager;
        private SelectionManager selectionManager;
        private List<Tuple<string, string>> annotatedPoints = new List<Tuple<string, string>>();
        private Dictionary<string, List<GameObject>> annotationDictionary = new Dictionary<string, List<GameObject>>();

        private void OnValidate()
        {
            if (gameObject.scene.IsValid())
            {
                referenceManager = GameObject.Find("InputReader").GetComponent<ReferenceManager>();
            }
        }

        private void Start()
        {
            graphManager = referenceManager.graphManager;
            selectionManager = referenceManager.selectionManager;
        }

        [ConsoleCommand("annotationManager", aliases: new string[] {"toggleannotations", "ta"})]
        public void ToggleAnnotationFile(string path, bool toggle)
        {
            if (toggle)
            {
                referenceManager.inputReader.ReadAnnotationFile(path);
            }
            else
            {
                RemoveAnnotations(path);
            }
        }

        [ConsoleCommand("annotationManager", aliases: new string[] {"clearallannotations", "caa"})]
        public void ClearAllAnnotations()
        {
            foreach (string key in annotationDictionary.Keys)
            {
                List<GameObject> annotationObjects = annotationDictionary[key];
                foreach (GameObject obj in annotationObjects)
                {
                    Destroy(obj);
                }

            }
            annotationDictionary.Clear();
            CellexalEvents.AnnotationsCleared.Invoke();
        }

        /// <summary>
        /// Adds the annotation to the currently selected group of cells.
        /// Also adds a line and text object above the cells with the annotation.
        /// </summary>
        /// <param name="annotation">The text that will be shown above the cells and later written to file.</param>
        /// <param name="index">The index of the selection group.</param>
        public void AddAnnotation(string annotation, int index)
        {
            Cell[] cellsToAnnotate = referenceManager.cellManager.GetCells(index);
            selectionManager.RecolorSelectionPoints();
            AddAnnotation(annotation, cellsToAnnotate.ToList());
        }

        /// <summary>
        /// Adds the annotation to the specified graphpoints. For example when reading from an annotation file.
        /// </summary>
        /// <param name="annotation">The annotation string.</param>
        /// <param name="pointsToAnnotate">The points to annotate.</param>
        public void AddAnnotation(string annotation, List<Cell> cellsToAnnotate, string path = "")
        {
            foreach (Cell cells in cellsToAnnotate)
            {
                annotatedPoints.Add(new Tuple<string, string>(cells.Label, annotation));
            }

            List<GameObject> annotationTexts = new List<GameObject>();

            foreach (Graph graph in graphManager.Graphs)
            {
                GameObject annotationText = Instantiate(annotationTextPrefab, graph.annotationsParent.transform);
                AnnotationTextPanel textPanel = annotationText.GetComponent<AnnotationTextPanel>();
                textPanel.referenceManager = referenceManager;
                textPanel.SetCells(cellsToAnnotate);
                annotationText.gameObject.name = annotation;
                Vector3 position = graph.FindGraphPoint(cellsToAnnotate[0].Label).Position;
                annotationText.transform.localPosition = position;
                annotationText.GetComponentInChildren<TextMeshPro>().text = annotation;
                annotationTexts.Add(annotationText);
            }

            if (path == "") return;
            if (annotationDictionary.ContainsKey(path))
            {
                foreach (GameObject obj in annotationTexts)
                {
                    annotationDictionary[path].Add(obj);
                }
            }
            else
            {
                annotationDictionary[path] = annotationTexts;
            }
        }

        private void RemoveAnnotations(string annotationFile)
        {
            List<GameObject> annotationObjects = annotationDictionary[annotationFile];
            foreach (GameObject obj in annotationObjects)
            {
                Destroy(obj);
            }

            annotationDictionary.Remove(annotationFile);
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
                while (File.Exists(filePath))
                {
                    annotationCtr++;
                    filePath = annotationDirectory + "\\annotated_selection" + annotationCtr + ".txt";
                }

                using (StreamWriter file = new StreamWriter(filePath, true))
                {
                    CellexalLog.Log("Dumping selection data to " + CellexalLog.FixFilePath(filePath));
                    CellexalLog.Log("\tSelection consists of  " + selectionManager.GetCurrentSelection().Count +
                                    " points");
                    if (selectionManager.selectionHistory != null)
                        CellexalLog.Log("\tThere are " + selectionManager.selectionHistory.Count +
                                        " entries in the history");
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
            referenceManager.selectionFromPreviousMenu.ReadAnnotationFiles();
        }
    }
}