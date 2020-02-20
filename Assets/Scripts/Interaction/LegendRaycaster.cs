using CellexalVR.AnalysisObjects;
using CellexalVR.General;
using UnityEngine;

namespace CellexalVR.Interaction
{

    public class LegendRaycaster : MonoBehaviour
    {
        private ReferenceManager referenceManager;
        private LegendManager legendManager;
        private SteamVR_Controller.Device device;
        private SteamVR_TrackedObject rightController;
        private Transform raycastingSource;
        private ControllerModelSwitcher controllerModelSwitcher;
        private int layerMask;
        private int savedGeneExpressionHistogramHitX = -1;
        private float clickStartTime;

        private void Start()
        {

            referenceManager = GameObject.Find("InputReader").GetComponent<ReferenceManager>();
            if (CrossSceneInformation.Normal)
            {
                rightController = referenceManager.rightController;
                raycastingSource = rightController.transform;
                controllerModelSwitcher = referenceManager.controllerModelSwitcher;
                legendManager = gameObject.GetComponent<LegendManager>();
            }
            layerMask = 1 << LayerMask.NameToLayer("GraphLayer");

        }

        void Update()
        {
            if (CrossSceneInformation.Normal || CrossSceneInformation.Tutorial)
            {
                device = SteamVR_Controller.Input((int)rightController.index);
                bool correctModel = controllerModelSwitcher.ActualModel == ControllerModelSwitcher.Model.TwoLasers
                                    || controllerModelSwitcher.ActualModel == ControllerModelSwitcher.Model.Keyboard
                                    || controllerModelSwitcher.ActualModel == ControllerModelSwitcher.Model.WebBrowser;
                if (correctModel)
                {
                    Raycast();
                }
            }
        }

        private void Raycast()
        {
            raycastingSource = referenceManager.rightLaser.transform;
            RaycastHit hit;
            Physics.Raycast(raycastingSource.position, raycastingSource.TransformDirection(Vector3.forward), out hit, 100f, layerMask);
            if (hit.collider && hit.collider.gameObject == legendManager.gameObject)
            {
                if (legendManager.desiredLegend == LegendManager.Legend.GeneExpressionLegend)
                {
                    var localPos = legendManager.WorldToRelativeHistogramPos(hit.point);
                    if (localPos.x >= 0f && localPos.x <= 1f && localPos.y >= 0f && localPos.y <= 1f)
                    {
                        HandleHitGeneExpressionHistogram(localPos);
                        if (device.GetPressDown(SteamVR_Controller.ButtonMask.Trigger))
                        {
                            // if the trigger was pressed
                            HandleClickDownGeneExpressionHistogram(localPos);
                        }
                        // we hit the gene expression histogram, in the histogram area
                        else if (device.GetPressUp(SteamVR_Controller.ButtonMask.Trigger))
                        {
                            // if the trigger was released
                            HandleClickUpGeneExpressionHistogram(localPos);
                        }
                    }
                    else
                    {
                        // we hit the legend but not the right area
                        legendManager.geneExpressionHistogram.DeactivateHighlightArea();
                    }
                    if (device.GetPressUp(Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger))
                    {
                        // if the trigger was released
                        savedGeneExpressionHistogramHitX = -1;
                    }
                }
            }
            else
            {
                // we hit nothing of interest
                legendManager.geneExpressionHistogram.DeactivateHighlightArea();
            }
            if (device.GetPressUp(Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger))
            {
                // if the trigger was released
                savedGeneExpressionHistogramHitX = -1;
            }
        }

        private void HandleHitGeneExpressionHistogram(Vector3 hit)
        {
            int hitIndex = (int)(hit.x * legendManager.geneExpressionHistogram.NumberOfBars);

            if (savedGeneExpressionHistogramHitX != -1)
            {
                legendManager.geneExpressionHistogram.MoveHighlightArea(hitIndex, savedGeneExpressionHistogramHitX);
            }
            else
            {
                legendManager.geneExpressionHistogram.MoveHighlightArea(hitIndex, hitIndex);
            }
        }

        private void HandleClickDownGeneExpressionHistogram(Vector3 hit)
        {
            clickStartTime = Time.time;
            savedGeneExpressionHistogramHitX = (int)(hit.x * legendManager.geneExpressionHistogram.NumberOfBars);
        }

        private void HandleClickUpGeneExpressionHistogram(Vector3 hit)
        {
            if (Time.time - clickStartTime < 0.1f)
            {
                legendManager.geneExpressionHistogram.DeactivateSelectedArea();
            }
            else
            {
                int hitIndex = (int)(hit.x * legendManager.geneExpressionHistogram.NumberOfBars);
                legendManager.geneExpressionHistogram.MoveSelectedArea(hitIndex, savedGeneExpressionHistogramHitX);
                savedGeneExpressionHistogramHitX = -1;
            }
        }
    }
}
