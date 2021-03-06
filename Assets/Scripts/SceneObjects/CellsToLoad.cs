using CellexalVR.General;
using UnityEngine;


namespace CellexalVR.SceneObjects
{

    /// <summary>
    /// Holds the directory name that the cells in the boxes should represent.
    /// </summary>
    public class CellsToLoad : MonoBehaviour
    {

        private string directory;
        private bool graphsLoaded = false;
        private Vector3 defaultPosition;
        private Quaternion defaultRotation;
        public ReferenceManager referenceManager;

        public string Directory
        {
            get
            {
                graphsLoaded = true;
                return directory;
            }
            set
            {
                directory = value;
            }
        }

        private void OnValidate()
        {
            if (gameObject.scene.IsValid())
            {
                referenceManager = GameObject.Find("InputReader").GetComponent<ReferenceManager>();
            }
        }

        private void Start()
        {
            //referenceManager = GameObject.Find("InputReader").GetComponent<ReferenceManager>();
            this.gameObject.name = directory;
        }

        private void Update()
        {
            //if (GetComponent<VRTK_InteractableObject>().enabled)
            //{
            //    referenceManager.MultiuserMessageSender.SendMessageMoveCells(transform.parent.gameObject.name, transform.position, transform.rotation);
            //    referenceManager.inputFolderGenerator.FindCells(this.gameObject.name);

            //}
        }

        public bool GraphsLoaded()
        {
            return graphsLoaded;
        }

        internal void ResetPosition()
        {
            transform.localPosition = defaultPosition;
            transform.localRotation = defaultRotation;
        }

        internal void SavePosition()
        {
            defaultPosition = transform.localPosition;
            defaultRotation = transform.localRotation;
        }
    }
}
