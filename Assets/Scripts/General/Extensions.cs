﻿using CellexalVR.General;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CellexalVR.Extensions
{
    /// <summary>
    /// Definitions that might be used through out the code.
    /// </summary>
    public static class Definitions
    {
        public enum Measurement { INVALID, GENE, ATTRIBUTE, FACS }
        public static string ToString(this Measurement m)
        {
            switch (m)
            {
                case Measurement.INVALID:
                    return "invalid";
                case Measurement.GENE:
                    return "gene";
                case Measurement.ATTRIBUTE:
                    return "attribute";
                case Measurement.FACS:
                    return "facs";
                default:
                    return "";
            }

        }

    }

    public enum AttributeLogic { INVALID, NOT_INCLUDED, YES, NO }

    /// <summary>
    /// Extension methods and such.
    /// </summary>
    public static class Extensions
    {
        public static UnityEngine.Color[] InterpolateColors(UnityEngine.Color color1, UnityEngine.Color color2, int numColors)
        {
            if (numColors == 1)
            {
                return new UnityEngine.Color[] { color1 };
            }
            var colors = new UnityEngine.Color[numColors];
            if (numColors < 1)
            {
                CellexalError.SpawnError("Error when interpolating colors", "Can not interpolate less than 1 color.");
                return null;
            }

            int divider = numColors - 1;

            float lowMidDeltaR = (color2.r * color2.r - color1.r * color1.r) / divider;
            float lowMidDeltaG = (color2.g * color2.g - color1.g * color1.g) / divider;
            float lowMidDeltaB = (color2.b * color2.b - color1.b * color1.b) / divider;

            for (int i = 0; i < numColors; ++i)
            {
                float r = color1.r * color1.r + lowMidDeltaR * i;
                float g = color1.g * color1.g + lowMidDeltaG * i;
                float b = color1.b * color1.b + lowMidDeltaB * i;
                if (r < 0) r = 0;
                if (g < 0) g = 0;
                if (b < 0) b = 0;
                colors[i] = new UnityEngine.Color(UnityEngine.Mathf.Sqrt(r), UnityEngine.Mathf.Sqrt(g), UnityEngine.Mathf.Sqrt(b));
            }

            return colors;
        }

        /// <summary>
        /// Replaces forward and backward slashes with whatever is appropriate for this system.
        /// </summary>
        /// <param name="s">The string to fix.</param>
        /// <returns>The fixed string.</returns>
        public static string FixFilePath(this string s)
        {
            char directorySeparatorChar = Path.DirectorySeparatorChar;
            s = s.Replace('/', directorySeparatorChar);
            s = s.Replace('\\', directorySeparatorChar);
            return s;
        }

        /// <summary>
        /// Finds a child or grand-child or grand-grand-child and so on by its name.
        /// </summary>
        /// <param name="aParent">The parent transform.</param>
        /// <param name="aName">The name of the child.</param>
        /// <returns>The child, or null if no child was found.</returns>
        public static Transform FindDeepChild(this Transform aParent, string aName)
        {
            Queue<Transform> queue = new Queue<Transform>();
            queue.Enqueue(aParent);
            while (queue.Count > 0)
            {
                var c = queue.Dequeue();
                if (c.name == aName)
                    return c;
                foreach (Transform t in c)
                    queue.Enqueue(t);
            }
            return null;
        }

        /// <summary>
        /// Replaces forward and backward slashes with double backward slashes. Used for passing filepaths as arguments to things that parse escape characters.
        /// </summary>
        /// <param name="s">The string to unfix.</param>
        /// <returns>The unfixed string.</returns>
        public static string UnFixFilePath(this string s)
        {
            string directorySeparatorChar = "\\\\";
            s = s.Replace("/", directorySeparatorChar);
            s = s.Replace("\\", directorySeparatorChar);
            return s;
        }
    }
}
