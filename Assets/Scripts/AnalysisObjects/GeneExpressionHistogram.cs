using CellexalVR.General;
using System;
using System.Linq;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using CellexalVR.Filters;

namespace CellexalVR.AnalysisObjects
{

    /// <summary>
    /// Represents a gene expression histogram that is showed beside a graph.
    /// </summary>
    public class GeneExpressionHistogram : MonoBehaviour
    {
        public ReferenceManager referenceManager;
        public GameObject histogramParent;
        public TextMeshPro geneNameLabel;
        public TextMeshPro yAxisMaxLabel;
        public TextMeshPro xAxisMaxLabel;
        public TextMeshPro yAxisLabel;
        public GameObject barPrefab;
        public GameObject highlightArea;
        public TextMeshPro highlightAreaInfoText;
        public GameObject selectedArea;
        public TextMeshPro selectedAreaInfoText;
        public GameObject cutOffBarTopShape;
        public TextMeshPro filterTextLabel;

        /// <summary>
        /// Local position of the lower left corner of the histogram in the <see cref="LegendManager"/> local pos.
        /// </summary>
        public Vector3 HistogramMinPos { get; private set; }
        /// <summary>
        /// Local position of the upper right corner of the histogram.
        /// </summary>
        public Vector3 HistogramMaxPos { get; private set; }

        public int NumberOfBars { get => heightsInt.Length; }


        private int tallestBarsToSkip;
        private List<GameObject> cutOffTops = new List<GameObject>();

        /// <summary>
        /// The number of tallest bars to skip when scaling the Y axis. Call <see cref="RecreateHistogram"/> after changing this.
        /// </summary>
        public int TallestBarsToSkip
        {
            get => tallestBarsToSkip;
            set
            {
                if (value >= NumberOfBars)
                    tallestBarsToSkip = NumberOfBars - 1;
                else if (value < 0)
                    tallestBarsToSkip = 0;
                else
                    tallestBarsToSkip = value;
            }
        }

        /// <summary>
        /// The desired Y axis scaling mode. Call <see cref="RecreateHistogram"/> after changing this.
        /// </summary>
        public YAxisMode DesiredYAxisMode { get; set; }
        public enum YAxisMode { Linear, Logarithmic }

        private List<GameObject> bars = new List<GameObject>();
        private int[] heightsInt;
        private int[] sortedHeightsInt;
        private float[] heightsFloat;
        private Vector3 startPos;
        private LegendManager manager;
        private bool attached;

        // local position of the center of the histogram
        private Vector3 center = new Vector3(0.0064f, -0.0129f, 0f);
        private float histogramHeight = 0.4f;
        private float histogramWidth = 0.4f;

        private void OnValidate()
        {
            if (gameObject.scene.IsValid())
            {
                referenceManager = GameObject.Find("InputReader").GetComponent<ReferenceManager>();
            }
        }

        private void Awake()
        {
            if (!referenceManager)
            {
                referenceManager = GameObject.Find("InputReader").GetComponent<ReferenceManager>();
            }
            manager = gameObject.GetComponentInParent<LegendManager>();
            HistogramMinPos = new Vector3(center.x - histogramWidth / 2f, center.y - histogramHeight / 2f, 0f);
            HistogramMaxPos = new Vector3(center.x + histogramWidth / 2f, center.y + histogramHeight / 2f, 0f);
            CellexalEvents.LegendAttached.AddListener(ActivateExtraColumn);
        }

        private void ActivateExtraColumn()
        {
            attached = true;
        }

        private void DeActivateExtraColumn()
        {
            attached = false;
        }

        /// <summary>
        /// Creates a new histogram with the specifed barheights.
        /// This function will remove the previous histogram.
        /// </summary>
        /// <param name="geneName">The name of the gene</param>
        /// <param name="bins">An array of ints that represents the heights of the bars.</param>
        /// <param name="xAxisMaxLabel">The label at the max value of the x-axis</param>
        /// <param name="barHeightMode">The Y axis mode to use.</param>
        public void CreateHistogram(string geneName, int[] bins, string xAxisMaxLabel, YAxisMode barHeightMode, int skip = 0)
        {
            this.geneNameLabel.text = geneName;
            heightsInt = bins;
            sortedHeightsInt = new int[bins.Length];
            Array.Copy(bins, sortedHeightsInt, bins.Length);
            Array.Sort(sortedHeightsInt);

            DesiredYAxisMode = barHeightMode;
            TallestBarsToSkip = skip;

            string yAxisMaxLabel;
            List<float> barHeights;
            CalculateBarHeights(out yAxisMaxLabel, out barHeights);
            CreateHistogram(xAxisMaxLabel, yAxisMaxLabel, barHeights);
        }

        /// <summary>
        /// Calculates the bar heights.
        /// </summary>
        /// <param name="yAxisMaxLabel">The resulting text on the max y label</param>
        /// <param name="barHeights">The resulting heights, range [0, 1]</param>
        private void CalculateBarHeights(out string yAxisMaxLabel, out List<float> barHeights)
        {
            string yAxisText = "Number of cells";

            if (DesiredYAxisMode != YAxisMode.Linear)
            {
                yAxisText = "Log(" + yAxisText + ")";
            }

            float largestBin = (float)sortedHeightsInt[sortedHeightsInt.Length - TallestBarsToSkip - 1];
            yAxisLabel.text = yAxisText;
            barHeights = new List<float>(heightsInt.Length);
            if (DesiredYAxisMode == YAxisMode.Linear)
            {
                for (int i = 0; i < heightsInt.Length; ++i)
                {
                    barHeights.Add((float)heightsInt[i] / largestBin);
                }
                heightsFloat = null;
                yAxisMaxLabel = largestBin.ToString();
            }
            else
            {
                heightsFloat = new float[heightsInt.Length];
                for (int i = 0; i < heightsInt.Length; ++i)
                {
                    if (heightsInt[i] == 0)
                    {
                        heightsFloat[i] = 0f;
                    }
                    else
                    {
                        heightsFloat[i] = Mathf.Log(heightsInt[i]);
                    }
                }

                largestBin = Mathf.Log(largestBin);

                for (int i = 0; i < heightsFloat.Length; ++i)
                {
                    barHeights.Add(heightsFloat[i] / largestBin);
                }
                yAxisMaxLabel = largestBin.ToString();

            }
        }

        /// <summary>
        /// Creates a histogram.
        /// </summary>
        /// <param name="xAxisMaxLabel">The label of the x axis</param>
        /// <param name="yAxisMaxLabel">The label of the y axis</param>
        /// <param name="barHeights">The height of each bar, range [0, 1]</param>
        private void CreateHistogram(string xAxisMaxLabel, string yAxisMaxLabel, List<float> barHeights)
        {
            if (bars.Count != barHeights.Count)
            {
                InstantiateBars(barHeights.Count);
            }

            foreach (GameObject go in cutOffTops)
            {
                Destroy(go);
            }
            cutOffTops.Clear();

            for (int i = 0; i < NumberOfBars; ++i)
            {
                Color color = referenceManager.graphGenerator.geneExpressionColors[i];

                GameObject bar = bars[i];
                bar.GetComponent<Renderer>().material.color = color;
                bar.transform.localRotation = Quaternion.identity;
                float height = Mathf.Min(barHeights[i], 1f);

                var pos = BarPos(i, i, height, NumberOfBars);
                Vector3 currentScale = bar.transform.localScale;
                var scale = BarScale(i, i, height, NumberOfBars, currentScale.z);

                if (barHeights[i] > 1f)
                {
                    GameObject cutOffGameObject = Instantiate(cutOffBarTopShape);
                    cutOffGameObject.transform.parent = histogramParent.transform;
                    cutOffGameObject.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                    cutOffGameObject.transform.localPosition = new Vector3(pos.x, pos.y + scale.y / 2f, pos.z + 8e-05f);

                    cutOffGameObject.transform.localScale = new Vector3(scale.x * 100f, scale.z * 100f, 1f);
                    cutOffGameObject.GetComponent<Renderer>().material.color = color;
                    cutOffTops.Add(cutOffGameObject);
                }

                bar.transform.localPosition = pos;
                bar.transform.localScale = scale;
            }

            this.yAxisMaxLabel.text = yAxisMaxLabel;
            this.xAxisMaxLabel.text = xAxisMaxLabel;
        }

        /// <summary>
        /// Recreates the current histogram.
        /// </summary>
        public void RecreateHistogram()
        {
            string yAxisMaxLabel;
            List<float> barHeights;
            CalculateBarHeights(out yAxisMaxLabel, out barHeights);
            CreateHistogram(xAxisMaxLabel.text, yAxisMaxLabel, barHeights);
        }

        /// <summary>
        /// Calculates a bars position. A bar can span many indices.
        /// </summary>
        /// <param name="startIndex">The left index of the bar</param>
        /// <param name="endIndex">The right index of the bar</param>
        /// <param name="height">The bars height, range [0, 1]</param>
        /// <param name="nBars">The total number of bars in the histogram</param>
        /// <returns>The bar's local position</returns>
        private Vector3 BarPos(int startIndex, int endIndex, float height, int nBars)
        {
            float meanIndex = (endIndex + startIndex) / 2f;
            float xCoord = center.x + histogramWidth * ((meanIndex + 0.5f) / nBars) - histogramWidth / 2f;
            float yCoord = center.y + histogramHeight * (height - 1f) / 2f;
            return new Vector3(xCoord, yCoord, 0f);
        }

        /// <summary>
        /// Calculates a bars scale. A bar can span many indices.
        /// </summary>
        /// <param name="startIndex">The left index of the bar</param>
        /// <param name="endIndex">The right index of the bar</param>
        /// <param name="height">The bars height, range [0, 1]</param>
        /// <param name="nBars">The total number of bars in the histogram</param>
        /// <param name="zScale">The desired z-component of the scale</param>
        /// <returns>The bar's local scale</returns>
        private Vector3 BarScale(int startIndex, int endIndex, float height, int nBars, float zScale)
        {
            float xScale = histogramWidth / nBars * (endIndex - startIndex + 1);
            float yScale = histogramHeight * height;
            return new Vector3(xScale, yScale, zScale);
        }

        /// <summary>
        /// Instantiates or destroys the bars until the desired number of bars are left.
        /// </summary>
        /// <param name="numberOfBars">The desired number of bars to have.</param>
        private void InstantiateBars(int numberOfBars)
        {
            int currentNumberOfBars = bars.Count;
            if (numberOfBars > currentNumberOfBars)
            {
                // if we are missing bars
                for (int i = currentNumberOfBars; i < numberOfBars; ++i)
                {
                    GameObject newBar = Instantiate(barPrefab);
                    newBar.transform.parent = histogramParent.transform;
                    newBar.SetActive(true);
                    bars.Add(newBar);
                }
            }
            else if (numberOfBars < currentNumberOfBars)
            {
                int numberOfBarsToRemove = currentNumberOfBars - numberOfBars;
                for (int i = 0; i < numberOfBarsToRemove; ++i)
                {
                    Destroy(bars[bars.Count - 1 - i]);
                }
                bars.RemoveRange(bars.Count - numberOfBarsToRemove, numberOfBarsToRemove);

            }
        }

        /// <summary>
        /// Deactivates the highlight area and its accompanying text.
        /// </summary>
        public void DeactivateHighlightArea()
        {
            highlightArea.SetActive(false);
            highlightAreaInfoText.gameObject.SetActive(false);
        }

        /// <summary>
        /// Deactivates the selected highlight area and its accompanying text.
        /// </summary>
        public void DeactivateSelectedArea()
        {
            selectedArea.SetActive(false);
            selectedAreaInfoText.gameObject.SetActive(false);
        }

        /// <summary>
        /// Moves a highlight area and its text.
        /// </summary>
        /// <param name="area">The highlight area gameObject</param>
        /// <param name="text">The highlight area's text</param>
        /// <param name="minX">The left index of the highlight area</param>
        /// <param name="maxX">The right index of the highlight area</param>
        private void MoveArea(GameObject area, TextMeshPro text, int minX, int maxX)
        {
            if (minX > maxX)
            {
                int temp = minX;
                minX = maxX;
                maxX = temp;
            }

            if (!area.activeSelf)
            {
                area.SetActive(true);
                text.gameObject.SetActive(true);
            }
            Vector3 pos = BarPos(minX, maxX, 1f, bars.Count);
            Vector3 scale = BarScale(minX, maxX, 1f, bars.Count, 0.01f);
            area.transform.localPosition = pos;
            area.transform.localScale = scale;
            Vector3 infoTextPos = new Vector3(pos.x + scale.x / 2f, scale.y / 2f, -scale.z);
            text.transform.localPosition = infoTextPos;

            int[] selectedSlice = new int[maxX - minX + 1];
            Array.Copy(heightsInt, minX, selectedSlice, 0, selectedSlice.Length);
            Array.Sort(selectedSlice);
            double mean = selectedSlice.Average();
            float median;
            int middleIndex = selectedSlice.Length / 2;
            if (selectedSlice.Length % 2 == 0)
            {
                median = (selectedSlice[middleIndex] + selectedSlice[middleIndex - 1]) / 2f;
            }
            else
            {
                median = selectedSlice[middleIndex];
            }


            if (minX != maxX)
            {
                int sum = 0;
                for (int i = minX; i <= maxX; ++i)
                {

                    sum += heightsInt[i];
                }
                highlightAreaInfoText.text = "x: [" + minX + ", " + maxX + "]\nsum: " + sum + "\nmean: " + mean + "\nmedian: " + median;
            }
            else
            {
                highlightAreaInfoText.text = "x: " + minX + "\ny: " + heightsInt[minX];
            }

        }

        /// <summary>
        /// Moves and resizes the highlight area to span between two bars.
        /// </summary>
        /// <param name="minX">The index of the left bar</param>
        /// <param name="maxX">The index of the right bar</param>
        public void MoveHighlightArea(int minX, int maxX)
        {
            MoveArea(highlightArea, highlightAreaInfoText, minX, maxX);
        }

        /// <summary>
        /// Moves and resizes the selected highlight area to span between two bars.
        /// </summary>
        /// <param name="minX">The index of the left bar</param>
        /// <param name="maxX">The index of the right bar</param>
        public void MoveSelectedArea(int minX, int maxX)
        {
            selectedAreaInfoText.text = highlightAreaInfoText.text;
            MoveArea(selectedArea, selectedAreaInfoText, minX, maxX);
            if (attached)
            {
                if (minX > maxX)
                {
                    int temp = minX;
                    minX = maxX;
                    maxX = temp;
                }
                referenceManager.cullingFilterManager.AddGeneFilter(geneNameLabel.text, minX, maxX, float.Parse(xAxisMaxLabel.text));
            }
        }

    }
}
