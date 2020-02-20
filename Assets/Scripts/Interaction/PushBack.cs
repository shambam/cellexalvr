using UnityEngine;
using VRTK;
using CellexalVR.General;
using CellexalVR.AnalysisObjects;

namespace CellexalVR.Interaction
{
    /// <summary>
    /// Use to push/pull object away/towards user. 
    /// </summary>
    public class PushBack : MonoBehaviour
    {
        //public SteamVR_TrackedObject rightController;
        public float distanceMultiplier = 0.1f;
        public float scaleMultiplier = 0.4f;
        public float maxScale;
        public float minScale;
        public ReferenceManager referenceManager;

        //private SteamVR_Controller.Device device;
        private Ray ray;
        private RaycastHit hit;
        private Transform raycastingSource;
        private bool push;
        private bool pull;
        private int maxDist = 10;

        private void OnValidate()
        {
            if (gameObject.scene.IsValid())
            {
                referenceManager = GameObject.Find("InputReader").GetComponent<ReferenceManager>();
            }
        }

        void Start()
        {
            if (CrossSceneInformation.Ghost || CrossSceneInformation.Spectator)
            {
                Destroy(this);
            }
            referenceManager = GameObject.Find("InputReader").GetComponent<ReferenceManager>();
        }

        /// <summary>
        /// Pulls an object closer to the user.
        /// </summary>
        public void Pull()
        {
            raycastingSource = this.transform;
            int layerMask = 1 << LayerMask.NameToLayer("GraphLayer") | 1 << LayerMask.NameToLayer("NetworkLayer");
            Physics.Raycast(raycastingSource.position, raycastingSource.TransformDirection(Vector3.forward), out hit, maxDist + 5, layerMask);
            if (hit.collider)
            {
                if (hit.transform.gameObject.name.Contains("Slice"))
                {
                    return;
                }
                // don't let the thing become smaller than what it was originally
                // this could cause some problems if the user rescales the objects while they are far away
                if (Vector3.Distance(hit.transform.position, raycastingSource.position) < 0.5f)
                {
                    pull = false;
                    return;
                }
                Vector3 dir = hit.transform.position - raycastingSource.position;
                dir = -dir.normalized;
                hit.transform.position += dir * distanceMultiplier;
                if (hit.transform.GetComponent<Graph>())
                {
                    referenceManager.multiuserMessageSender.SendMessageMoveGraph(hit.transform.gameObject.name, hit.transform.localPosition, hit.transform.localRotation, hit.transform.localScale);
                }
                else if (hit.transform.GetComponent<NetworkCenter>())
                {
                    hit.transform.LookAt(raycastingSource.transform);
                    hit.transform.Rotate(0, 0, 180);
                    referenceManager.multiuserMessageSender.SendMessageMoveNetwork(hit.transform.gameObject.name, hit.transform.localPosition, hit.transform.localRotation, hit.transform.localScale);
                }
                else if (hit.transform.GetComponent<Heatmap>())
                {
                    hit.transform.LookAt(raycastingSource.transform);
                    hit.transform.Rotate(0, 180, 0);
                    referenceManager.multiuserMessageSender.SendMessageMoveHeatmap(hit.transform.gameObject.name, hit.transform.localPosition, hit.transform.localRotation, hit.transform.localScale);
                }
            }
        }

        /// <summary>
        /// Pushes an object further from the user.
        /// </summary>
        public void Push()
        {
            int layerMask = 1 << LayerMask.NameToLayer("GraphLayer") | 1 << LayerMask.NameToLayer("NetworkLayer");
            raycastingSource = this.transform;
            Physics.Raycast(raycastingSource.position, raycastingSource.TransformDirection(Vector3.forward), out hit, maxDist, layerMask);
            if (hit.collider)
            {
                if (hit.transform.gameObject.name.Contains("Slice"))
                {
                    return;
                }
                Vector3 dir = hit.transform.position - raycastingSource.position;
                dir = dir.normalized;
                hit.transform.position += dir * distanceMultiplier;
                if (hit.transform.GetComponent<Graph>())
                {
                    referenceManager.multiuserMessageSender.SendMessageMoveGraph(hit.transform.gameObject.name, hit.transform.localPosition, hit.transform.localRotation, hit.transform.localScale);
                }
                else if (hit.transform.GetComponent<NetworkCenter>())
                {
                    hit.transform.LookAt(referenceManager.headset.transform);
                    hit.transform.Rotate(0, 0, 180);
                    referenceManager.multiuserMessageSender.SendMessageMoveNetwork(hit.transform.gameObject.name, hit.transform.localPosition, hit.transform.localRotation, hit.transform.localScale);
                }
                else if (hit.transform.GetComponent<Heatmap>())
                {
                    hit.transform.LookAt(referenceManager.headset.transform);
                    hit.transform.Rotate(0, 180, 0);
                    referenceManager.multiuserMessageSender.SendMessageMoveHeatmap(hit.transform.gameObject.name, hit.transform.localPosition, hit.transform.localRotation, hit.transform.localScale);
                }
            }
        }

    }
}