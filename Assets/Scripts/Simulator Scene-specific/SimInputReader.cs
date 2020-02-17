// using UnityEngine;
// using System.IO;
//
// // A classs for reading a data file and creating GraphPoints at the correct locations
// public class SimInputReader  : MonoBehaviour{
//
//  public string CellCoordinatesFileName;
//  public string GeneExpressionFileName;
//  //public string GeneExpressionFileName;
//  public GraphManager graphManager;
//  public CellManager cellManager;
//
//  void Start() {
//
//      // put each line into an array
//      string[] lines = System.IO.File.ReadAllLines(CellCoordinatesFileName);
//      string[] geneLines = System.IO.File.ReadAllLines(GeneExpressionFileName);
//
//      UpdateMinMax (lines);
//      foreach (string line in lines) {
//          // the coordinates are split with tab characters
//          string[] words = line.Split('\t');
//          // float[] coords = new float[3];
//          graphManager.AddCell(words[0], float.Parse(words[1]), float.Parse(words[2]), float.Parse(words[3]));
//      }
//
//
//      string[] cellNames = geneLines [0].Split ('\t');
//      for (int i = 1; i < geneLines.Length; i++) {
//          string[] words = geneLines[i].Split ('\t');
//          string geneName = words [0];
//          float minExpr = 10000f;
//          float maxExpr = -1f;
//          for (int j = 1; j < words.Length; j++) {
//              float expr = float.Parse(words [j]);
//              if (expr > maxExpr) {
//                  maxExpr = expr;
//              }
//              if (expr < minExpr) {
//                  minExpr = expr;
//              }
//          }
//
//          float binSize = (maxExpr - minExpr) / 30;
//
//          for (int k = 1; k < words.Length; k++) {
//              int binIndex = 0;
//              float expr = float.Parse(words [k]);
//              binIndex = (int)((expr - minExpr) / binSize);
//              if (binIndex == 30) {
//                  binIndex--;
//              }
//              cellManager.SetGeneExpression (cellNames [k - 1], geneName, binIndex);
//          }
//      }
//
//
//
//         string[] txtList = Directory.GetFiles(Directory.GetCurrentDirectory() + "/" + "Assets\\Data\\runtimeGroups", "*.txt");
//         foreach (string f in txtList)
//         {
//             //File.Delete(f);
//         }
//     }
//
//  /**
//   * Determines the maximum and the minimum values of the dataset.
//   * Will be used for the scaling part onto the graphArea.
//   **/
//  void UpdateMinMax(string[] lines){
//      Vector3 maxCoordValues = new Vector3 ();
//      maxCoordValues.x = maxCoordValues.y = maxCoordValues.z = -1000000.0f;
//      Vector3 minCoordValues = new Vector3 ();
//      minCoordValues.x = minCoordValues.y = minCoordValues.z = 1000000.0f;
//      foreach (string line in lines) {
//          // the coordinates are split with tab characters
//          string[] words = line.Split('\t');
//          float[] coords = new float[3];
//          coords[0] = float.Parse(words[1]);
//          coords[1] = float.Parse(words[2]);
//          coords[2] = float.Parse(words[3]);
//          if (coords [0] < minCoordValues.x)
//              minCoordValues.x = coords [0];
//          if (coords [0] > maxCoordValues.x)
//              maxCoordValues.x = coords [0];
//          if (coords [1] < minCoordValues.y)
//              minCoordValues.y = coords [1];
//          if (coords [1] > maxCoordValues.y)
//              maxCoordValues.y = coords [1];
//          if (coords [2] < minCoordValues.z)
//              minCoordValues.z = coords [2];
//          if (coords [2] > maxCoordValues.z)
//              maxCoordValues.z = coords [2];
//
//      }
//      graphManager.SetMinMaxCoords (minCoordValues, maxCoordValues);
//  }
// }
