using UnityEngine;
using System.Collections;
using System.IO;
using System.Threading;
using System.Collections.Generic;

/// <summary>
/// This class represent the loader. The loader reacts to cells representing dtasets that fall into it and starts loading the dataset.
/// </summary>
public class LoaderController : MonoBehaviour
{
    public ReferenceManager referenceManager;
    public Transform cylinder;
    public GameObject helpVideoObj;
    
    [HideInInspector]
    public bool loaderMovedDown = false;
    public GameObject keyboard;
    public bool loadingComplete = false;

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
    private bool moving = false;
    private float currentTime;
    private float arrivalTime;
    private GameManager gameManager;
    public List<string> pathsToLoad;


    void Start()
    {
        gameManager = referenceManager.gameManager;
        cellsToDestroy = new ArrayList();
        pathsToLoad = new List<string>();
        inputReader = referenceManager.inputReader;
        inputFolderGenerator = referenceManager.inputFolderGenerator;
        graphManager = referenceManager.graphManager;
        helperCylinder = referenceManager.helperCylinder;
    }

    void Update()
    {
        if (moving)
        {
            gameObject.transform.position = Vector3.Lerp(startPosition, finalPosition, currentTime / arrivalTime);
            cylinder.transform.localScale = Vector3.Lerp(startScale, finalScale, currentTime / arrivalTime);
            currentTime += Time.deltaTime;
            if (currentTime > arrivalTime || gameObject.transform.position.y > 0)
            {
                moving = false;
                loadingComplete = true;
                //Debug.Log("Loading Complete");
                //sound.Stop();
            }
        }

        if (timeEntered + 2 < Time.time && cellsEntered && !collidersDestroyed)
        {
            //helperCylinder.SetActive(false);
            DestroyFolderColliders();
        }

        if (timeEntered + 5 < Time.time && collidersDestroyed)
        {
            //inputFolderGenerator.DestroyFolders();
            DestroyCells();
        }
    }

    /// <summary>
    /// Resets some important variables used by the loader.
    /// </summary>
    public void ResetLoaderBooleans()
    {
        inputFolderGenerator.DestroyFolders();
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
            finalScale = new Vector3(1f, startScale.y, 1f);
            helperCylinder.SetActive(true);
        }
        else
        {
            finalScale = new Vector3(1f, startScale.y, 1f);
        }
        if (moving)
        {
            finalPosition = finalPosition + distance;
        }
        else
        {
            finalPosition = transform.position + distance;
        }
        keyboard.SetActive(false);
        helpVideoObj.SetActive(false);
        moving = true;
    }

    void OnTriggerEnter(Collider collider)
    {
        if (collider.gameObject.CompareTag("Sphere"))
        {
            Transform cellParent = collider.transform.parent;
            if (cellParent != null)
            {
                if (!cellParent.GetComponent<CellsToLoad>().GraphsLoaded())
                {
                    pathsToLoad.Add(cellParent.GetComponent<CellsToLoad>().Directory);

                }
                Destroy(cellParent.GetComponent<FixedJoint>());
                Destroy(cellParent.GetComponent<Rigidbody>());
                foreach (Transform child in cellParent)
                {
                    if (child.gameObject.GetComponent<Rigidbody>() == null)
                    {
                        child.gameObject.AddComponent<Rigidbody>();
                    }
                    cellsToDestroy.Add(child);
                }
            }
        }
    }

    [ConsoleCommand("loaderController", "loadallcells", "lac")]
    public void LoadAllCells()
    {
        if (pathsToLoad.Count == 0)
        {
            return;
        }
        if (timeEntered == 0)
        {
            timeEntered = Time.time;
            cellsEntered = true;
        }
        foreach (string path in pathsToLoad)
        {
            graphManager.directories.Add(path);
            try
            {
                inputReader.ReadFolder(path);
            }
            catch (System.InvalidOperationException e)
            {
                CellexalLog.Log("Could not read folder. Caught exception - " + e.StackTrace);
                ResetFolders(false);
            }

            referenceManager.keyboardStatusFolder.ClearKey();
            gameManager.InformReadFolder(path);

        }
        // must pass over list again to remove the parents. doing so in the
        // above loop messes with the iterator somehow and only removes every
        // second child's parent reference
        foreach (Transform child in cellsToDestroy)
        {
            child.parent = null;
        }
        pathsToLoad.Clear();
    }

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
            if (child.CompareTag("Folder"))
            {
                child.gameObject.AddComponent<Rigidbody>();
            }
        }
        collidersDestroyed = true;
    }

    void DestroyCells()
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

    public void ResetFolders(bool reset)
    {
        if (reset)
        {
            graphManager.DeleteGraphsAndNetworks();
            referenceManager.heatmapGenerator.DeleteHeatmaps();
            referenceManager.previousSearchesList.ClearList();
            CellexalEvents.GraphsUnloaded.Invoke();
        }
        // must reset loader before generating new folders
        ResetLoaderBooleans();
        inputFolderGenerator.GenerateFolders();
        referenceManager.inputFolderGenerator.gameObject.SetActive(true);
        if (loaderMovedDown)
        {
            loaderMovedDown = false;
            MoveLoader(new Vector3(0f, 2f, 0f), 2f);
        }
        keyboard.SetActive(true);
        helpVideoObj.SetActive(true);
    }
}
