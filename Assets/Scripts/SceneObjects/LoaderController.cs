using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CellexalVR.General;
using CellexalVR.DesktopUI;
using CellexalVR.AnalysisLogic;
using CellexalVR.AnalysisObjects;
using CellexalVR.Multiuser;
using System.IO;
using System.Linq;
using CellexalVR.AnalysisLogic.H5reader;

namespace CellexalVR.SceneObjects
{
    /// <summary>
    /// This class represent the loader. The loader reacts to cells representing dtasets that fall into it and starts loading the dataset.
    /// </summary>
    public class LoaderController : MonoBehaviour
    {
        public ReferenceManager referenceManager;
        public Transform cylinder;
        public GameObject helpVideoObj;

        [HideInInspector] public bool loaderMovedDown = false;
        public GameObject keyboard;
        public bool loadingComplete = false;
        public List<string> pathsToLoad;

        private InputReader inputReader;
        private InputFolderGenerator inputFolderGenerator;
        private GraphManager graphManager;
        private GameObject helperCylinder;
        private float timeEntered = 0;
        private ArrayList cellsToDestroy;
        private bool cellsEntered = false;
        private bool collidersDestroyed = false;
        private Vector3 startPosition;
        private Vector3 finalPosition;
        private Vector3 startScale;
        private Vector3 finalScale;
        private bool moveLoader = false;
        private bool moveFloor = false;
        private float currentTime;
        private float arrivalTime;
        private MultiuserMessageSender multiuserMessageSender;

        private void OnValidate()
        {
            if (gameObject.scene.IsValid())
            {
                referenceManager = GameObject.Find("InputReader").GetComponent<ReferenceManager>();
            }
        }

        private void Start()
        {
            multiuserMessageSender = referenceManager.multiuserMessageSender;
            cylinder = GameObject.Find("Floor/ExpandableFloor").transform;
            cellsToDestroy = new ArrayList();
            pathsToLoad = new List<string>();
            inputReader = referenceManager.inputReader;
            inputFolderGenerator = referenceManager.inputFolderGenerator;
            graphManager = referenceManager.graphManager;
            //helperCylinder = referenceManager.helperCylinder;
            // multiple_exp datasetList = GetComponentInChildren<DatasetList>();
        }

        private void Update()
        {
            if (moveLoader)
            {
                gameObject.transform.position = Vector3.Lerp(startPosition, finalPosition, currentTime / arrivalTime);
                currentTime += Time.deltaTime;
                if (Mathf.Abs(transform.position.y - finalPosition.y) <= 0.005)
                {
                    moveLoader = false;
                    loadingComplete = true;
                    moveFloor = true;
                    currentTime = 0f;
                    //Debug.Log("Loading Complete");
                    //sound.Stop();
                }
            }

            if (!moveFloor) return;
            currentTime += Time.deltaTime;
            cylinder.transform.localScale = Vector3.Lerp(startScale, finalScale, currentTime / arrivalTime);
            if (Mathf.Abs(cylinder.transform.localScale.x - finalScale.x) <= 0.005)
            {
                moveFloor = false;
            }


            //if (timeEntered + 2 < Time.time && cellsEntered && !collidersDestroyed)
            //{
            //    //helperCylinder.SetActive(false);
            //    //DestroyFolderColliders();
            //}

            //if (timeEntered + 5 < Time.time && collidersDestroyed)
            //{
            //    //inputFolderGenerator.DestroyFolders();
            //    //DestroyCells();
            //}
        }

        /// <summary>
        /// Resets some important variables used by the loader.
        /// </summary>
        private void ResetLoaderBooleans()
        {
            cellsEntered = false;
            timeEntered = 0;
            collidersDestroyed = false;
        }

        /// <summary>
        /// Moves the loader.
        /// </summary>
        /// <param name="distance"> The distance in world space to move the loader. </param>
        /// <param name="time"> The total time in seconds to move the loader. </param>
        public void MoveLoader(Vector3 distance, float time)
        {
            //sound.Play();
            currentTime = 0;
            arrivalTime = time;
            startPosition = transform.position;
            startScale = cylinder.localScale;
            if (distance.y > 0)
            {
                finalScale = new Vector3(0.144f, startScale.y, 0.144f);
                //helperCylinder.SetActive(true);
            }
            else
            {
                finalScale = new Vector3(1f, startScale.y, 1f);
            }

            if (moveLoader)
            {
                finalPosition = distance;
            }
            else
            {
                finalPosition = transform.position + distance;
            }

            keyboard.SetActive(false);
            helpVideoObj.SetActive(false);
            // multiple_exp datasetList.gameObject.SetActive(false);
            DestroyFolderColliders();
            DestroyCells();
            moveLoader = true;
        }

        private void OnTriggerEnter(Collider collider)
        {
            if (collider.gameObject.CompareTag("Sphere"))
            {
                Transform cellParent = collider.transform.parent;

                if (cellParent != null)
                {
                    if (timeEntered == 0)
                    {
                        timeEntered = Time.time;
                        cellsEntered = true;
                    }

                    if (!cellParent.GetComponent<CellsToLoad>().GraphsLoaded())
                    {
                        string path = cellParent.GetComponent<CellsToLoad>().Directory;
                        graphManager.directories.Add(path);
                        try
                        {
                            if (path.Contains(".html"))
                            {
                                referenceManager.reportReader.ReadFolder(path);
                            }
                            else
                            {
                                multiuserMessageSender.SendMessageReadFolder(path);
                                inputReader.ReadFolder(path);
                            }
                        }
                        catch (System.InvalidOperationException e)
                        {
                            CellexalLog.Log("Could not read folder. Caught exception - " + e.StackTrace);
                            ResetFolders(true);
                        }
                    }

                    Destroy(cellParent.GetComponent<FixedJoint>());
                    Destroy(cellParent.GetComponent<Rigidbody>());
                    foreach (Transform child in cellParent)
                    {
                        child.gameObject.AddComponent<Rigidbody>();
                        cellsToDestroy.Add(child);
                    }

                    foreach (Transform child in cellsToDestroy)
                    {
                        child.parent = null;
                    }
                }
            }
        }

        //[ConsoleCommand("loaderController", aliases: new string[] { "loadallcells", "lac" })]
        // multiple_exp     public void LoadAllCells()
        // multiple_exp     {
        // multiple_exp         if (pathsToLoad.Count == 0)
        // multiple_exp         {
        // multiple_exp             return;
        // multiple_exp         }
        // multiple_exp         if (timeEntered == 0)
        // multiple_exp         {
        // multiple_exp             timeEntered = Time.time;
        // multiple_exp             cellsEntered = true;
        // multiple_exp         }
        // multiple_exp         foreach (string path in pathsToLoad)
        // multiple_exp         {
        // multiple_exp             graphManager.directories.Add(path);
        // multiple_exp             try
        // multiple_exp             {
        // multiple_exp                 inputReader.ReadFolder(path);
        // multiple_exp             }
        // multiple_exp             catch (System.InvalidOperationException e)
        // multiple_exp             {
        // multiple_exp                 CellexalLog.Log("Could not read folder. Caught exception - " + e.StackTrace);
        // multiple_exp                 ResetFolders(false);
        // multiple_exp             }
        // multiple_exp 
        // multiple_exp             referenceManager.keyboardStatusFolder.ClearKey();
        // multiple_exp             multiuserMessageSender.SendMessageReadFolder(path);
        // multiple_exp 
        // multiple_exp         }
        // multiple_exp         // must pass over list again to remove the parents. doing so in the
        // multiple_exp         // above loop messes with the iterator somehow and only removes every
        // multiple_exp         // second child's parent reference
        // multiple_exp         foreach (Transform child in cellsToDestroy)
        // multiple_exp         {
        // multiple_exp             child.parent = null;
        // multiple_exp         }
        // multiple_exp         pathsToLoad.Clear();
        // multiple_exp     }

        /// <summary>
        /// Removes all folders' colliders and gives the rigidbodies.
        /// </summary>
        public void DestroyFolderColliders()
        {
            // foreach (Collider c in GetComponentsInChildren<Collider>()) {
            //  Destroy(c);
            // }

            foreach (Transform child in cellsToDestroy)
            {
                if (child.GetComponent<Collider>() != null)
                    Destroy(child.gameObject.GetComponent<Collider>());
            }

            foreach (Collider c in inputFolderGenerator.GetComponentsInChildren<Collider>())
            {
                Destroy(c);
            }

            foreach (Transform child in inputFolderGenerator.transform)
            {
                if (child.CompareTag("Folder") && child.gameObject.GetComponent<Rigidbody>() == null)
                {
                    child.gameObject.AddComponent<Rigidbody>();
                }
            }

            collidersDestroyed = true;
        }

        public void DestroyCells()
        {
            // since we are responsible for removing the parent reference we should probably
            // destroy the objects as well
            foreach (Transform child in cellsToDestroy)
            {
                Destroy(child.gameObject);
            }

            cellsToDestroy.Clear();
            ResetLoaderBooleans();
        }

        internal void DestroyFolders()
        {
            inputFolderGenerator.DestroyFolders();
        }

        /// <summary>
        /// Reset everything back to the state it was before loading the data and returns the loading area.
        /// </summary>
        /// <param name="reset">True if everything should be deleted, false if only the loading area should be returned, and everything else kept.</param>
        public void ResetFolders(bool reset)
        {
            if (reset)
            {
                graphManager.DeleteGraphsAndNetworks();
                referenceManager.heatmapGenerator.DeleteHeatmaps();
                referenceManager.drawTool.ClearAllLines();
                referenceManager.selectionManager.Clear();
                referenceManager.graphGenerator.graphCount = 0;
                referenceManager.inputReader.QuitServer();
                referenceManager.keyboardSwitch.SetKeyboardVisible(false);
                referenceManager.lineBundler.ClearLinesBetweenGraphPoints();
                referenceManager.velocityGenerator.ActiveGraphs.Clear();

                CellexalEvents.GraphsUnloaded.Invoke();
            }

            // must reset loader before generating new folders
            if (loaderMovedDown)
            {
                loaderMovedDown = false;
                MoveLoader(new Vector3(0f, 2f, 0f), 2);
            }

            ResetLoaderBooleans();
            inputFolderGenerator.GenerateFolders();
            referenceManager.inputFolderGenerator.gameObject.SetActive(true);
            keyboard.SetActive(true);
            helpVideoObj.SetActive(true);
            // multiple_exp datasetList.gameObject.SetActive(true);
            // multiple_exp datasetList.ClearList();
        }
    }
}