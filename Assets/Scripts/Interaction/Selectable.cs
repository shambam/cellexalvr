// using UnityEngine;
// using CellexalVR.AnalysisObjects;
// using CellexalVR.General;
//
// namespace CellexalVR.Interaction
// {
//     /// <summary>
//     /// Selectable gameobject. Selectable means adding to selection of cells.
//     /// </summary>
//     public class Selectable : MonoBehaviour
//     {
//
//         public ReferenceManager referenceManager;
//         public SelectionManager selectionManager;
//         public Graph.GraphPoint graphPoint;
//         public bool selected = false;
//
//         private void OnValidate()
//         {
//             if (gameObject.scene.IsValid())
//             {
//                 referenceManager = GameObject.Find("InputReader").GetComponent<ReferenceManager>();
//             }
//         }
//
//         private void Start()
//         {
//             referenceManager = selectionManager.referenceManager;
//             CellexalEvents.GraphsReset.AddListener(Reset);
//             CellexalEvents.SelectionCanceled.AddListener(Reset);
//         }
//
//         void OnTriggerEnter(Collider other)
//         {
//             print(other.name);
//             int colorIndex = referenceManager.selectionToolCollider.currentColorIndex;
//             if (gameObject.GetComponent<Renderer>().material.color != referenceManager.selectionToolCollider.Colors[colorIndex])
//             {
//                 selectionManager.AddGraphpointToSelection(graphPoint);
//                 foreach (Selectable sel in graphPoint.lineBetweenCellsCubes)
//                 {
//                     sel.GetComponent<Renderer>().material.color = referenceManager.selectionToolCollider.Colors[colorIndex];
//                 }
//                 referenceManager.multiuserMessageSender.SendMessageCubeColoured(graphPoint.parent.name, graphPoint.Label,
//                                                                 colorIndex, referenceManager.selectionToolCollider.Colors[colorIndex]);
//             }
//             //if (!selected)
//             //{
//             //    //selected = true;
//             //}
//             //var cubeOnLine = other.gameObject.GetComponent<Selectable>();
//             //if (cubeOnLine != null && !cubeOnLine.selected)
//             //{
//         }
//
//         void Reset()
//         {
//             selected = false;
//         }
//     }
//
//
//
//     //private void OnTriggerEnter(Collider other)
//     //{
//     //    selectionManager.AddGraphpointToSelection(graphPoint);
//     //}
//
//
// }