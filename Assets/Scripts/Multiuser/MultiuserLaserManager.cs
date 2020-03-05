﻿using System.Collections.Generic;
using UnityEngine;

namespace CellexalVR.Multiuser
{
    public class MultiuserLaserManager : MonoBehaviour
    {
        private List<GameObject> lasers = new List<GameObject>();

        public LineRenderer GetLaser(int id)
        {
            foreach (GameObject laser in lasers)
            {
                if (laser.gameObject.name == id.ToString())
                {
                    return laser.GetComponent<LineRenderer>();
                }
            }
            return null;
        }

        public LineRenderer AddLaser(int id)
        {
            GameObject newLaser = GameObject.Instantiate(new GameObject(), this.transform);
            newLaser.gameObject.name = id.ToString();
            lasers.Add(newLaser);
            return newLaser.AddComponent<LineRenderer>();
        }
    }
}