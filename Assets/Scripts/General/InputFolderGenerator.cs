using UnityEngine;
using System.IO;
using System;
using CellexalVR.SceneObjects;
using TMPro;

namespace CellexalVR.General
{
    /// <summary>
    /// Generates the boxes that represents folders with input data.
    /// </summary>
    public class InputFolderGenerator : MonoBehaviour
    {
        public GameObject folderPrefab;
        public GameObject sessionFolderPrefab;
        public ReferenceManager referenceManager;

        private int nfolder = 0;

        // 6 is the number of boxes on each "floor"
        private Vector3[] folderBaseCoords = new Vector3[6];

        private void OnValidate()
        {
            if (gameObject.scene.IsValid())
            {
                referenceManager = GameObject.Find("InputReader").GetComponent<ReferenceManager>();
            }
        }

        private void Start()
        {
            var folderAngle = -(Math.PI * 1.1d) / 2d;
            folderBaseCoords = new Vector3[6];
            for (int i = 0; i < 3; ++i)
            {
                folderBaseCoords[i] = new Vector3((float) Math.Cos(folderAngle), 0, (float) Math.Sin(folderAngle));
                folderAngle -= (Math.PI * .9d) / 6d;
            }

            int j = 1;
            for (int i = 3; i < 6; ++i, j += 2)
            {
                folderBaseCoords[i] = new Vector3(folderBaseCoords[i - j].x, 0, -folderBaseCoords[i - j].z);
            }

            GenerateFolders();

            CellexalEvents.GraphsLoaded.AddListener(referenceManager.loaderController.DestroyFolderColliders);
            CellexalEvents.GraphsLoaded.AddListener(referenceManager.loaderController.DestroyCells);
        }

        /// <summary>
        /// Generates the boxes that represent folders.
        /// </summary>
        /// <param name="filter">String filter to search for specific data folder directory.</param>
        public void GenerateFolders(string filter = "")
        {
            DestroyFolders();

            string dataDirectory;
            string[] directories;
            if (CrossSceneInformation.Tutorial)
            {
                referenceManager.tutorialManager.gameObject.SetActive(true);
                dataDirectory = Directory.GetCurrentDirectory() + "\\Data";
                directories = Directory.GetDirectories(dataDirectory, "Mouse_HSPC");
            }
            else
            {
                referenceManager.tutorialManager.gameObject.SetActive(false);
                dataDirectory = Directory.GetCurrentDirectory() + "\\Data";
                directories = Directory.GetDirectories(dataDirectory);
            }

            if (directories.Length == 0)
            {
                if (directories.Length == 0)
                {
                    CellexalError.SpawnError(new Vector3(0, 1, 0), "Error in data folder",
                        "Error in data folder\nNo datasets found.\nMake sure you have placed your dataset(s) in the correct folder. They should be in serperate folders inside the \'Data\' folder, located where you installed CellexalVR.");
                }

                return;
            }

            CellexalLog.Log("Started generating folders from " + CellexalLog.FixFilePath(dataDirectory));
            nfolder = 0;
            foreach (string directory in directories)
            {
                int forwardSlashIndex = directory.LastIndexOf('/');
                int backwardSlashIndex = directory.LastIndexOf('\\');
                string croppedDirectoryName;

                // Handle both forwardslash and backwardslash
                if (backwardSlashIndex > forwardSlashIndex)
                {
                    croppedDirectoryName = directory.Substring(backwardSlashIndex + 1);
                }

                else
                {
                    croppedDirectoryName = directory.Substring(forwardSlashIndex + 1);
                }

                if (croppedDirectoryName.ToLower().Contains(filter))
                {
                    Vector3 heightVector = new Vector3(0f, 1 + nfolder / 6, 0f);
                    GameObject newFolder = Instantiate(folderPrefab, folderBaseCoords[nfolder % 6] + heightVector,
                        Quaternion.identity);
                    newFolder.transform.parent = transform;
                    newFolder.transform.LookAt(transform.position + heightVector - new Vector3(0f, 1f, 0f));
                    newFolder.transform.Rotate(0, -90f, 0);
                    newFolder.GetComponentInChildren<CellsToLoad>().SavePosition();
                    nfolder++;

                    // Set text on folder box
                    newFolder.GetComponentInChildren<TextMeshPro>().text = croppedDirectoryName;
                    newFolder.GetComponentInChildren<CellsToLoad>().Directory = croppedDirectoryName;
                    newFolder.gameObject.name = croppedDirectoryName + "_box";
                }
            }

            GeneratePreviousSessionFolders(filter);
        }


        private void GeneratePreviousSessionFolders(string filter = "")
        {
            string dataDirectory;
            string[] directories;
            if (CrossSceneInformation.Tutorial)
            {
                return;
            }

            // Generate previous session folders
            dataDirectory = CellexalUser.UserSpecificFolder;
            string[] reportFiles = Directory.GetFiles(dataDirectory, "*.html", SearchOption.AllDirectories);
            foreach (string report in reportFiles)
            {
                string[] words = report.Split(Path.DirectorySeparatorChar);
                string pathLastPart = words[words.Length - 1];
                string dataFolderPart = words[words.Length - 2];
                int fromIndex = pathLastPart.IndexOfAny("0123456789".ToCharArray());
                int toIndex = pathLastPart.LastIndexOfAny("0123456789".ToCharArray()) + 1;
                string sessionTimestamp = pathLastPart.Substring(fromIndex, toIndex - fromIndex);
                
                string fullName = dataFolderPart + sessionTimestamp;

                if (fullName.ToLower().Contains(filter))
                {
                    Vector3 heightVector = new Vector3(0f, 1 + nfolder / 6, 0f);
                    GameObject newFolder = Instantiate(sessionFolderPrefab, folderBaseCoords[nfolder % 6] + heightVector,
                        Quaternion.identity);
                    newFolder.transform.parent = transform;
                    newFolder.transform.LookAt(transform.position + heightVector - new Vector3(0f, 1f, 0f));
                    newFolder.transform.Rotate(0, -90f, 0);
                    newFolder.GetComponentInChildren<CellsToLoad>().SavePosition();
                    nfolder++;

                    // Set text on folder box
                    newFolder.GetComponentInChildren<TextMeshPro>().text =
                        "Session \n" + dataFolderPart + "\n" + sessionTimestamp;
                    newFolder.GetComponentInChildren<CellsToLoad>().Directory = report;
                    newFolder.gameObject.name = dataFolderPart + "_" + sessionTimestamp + "_box";
                }

            }
        }


        /// <summary>
        /// Find the cells object of a given name.
        /// </summary>
        public GameObject FindCells(string name)
        {
            foreach (Transform child in transform)
            {
                if (child.gameObject.name == (name + "_box"))
                {
                    return gameObject;
                }
            }

            return null;
        }

        /// <summary>
        /// Destroys all folders.
        /// </summary>
        public void DestroyFolders()
        {
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
        }
    }
}