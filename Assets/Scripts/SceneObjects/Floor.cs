using CellexalVR.General;
using System.Collections;
using System.IO;
using UnityEngine;

namespace CellexalVR.SceneObjects
{

    public class Floor : MonoBehaviour
    {

        public Material gridMaterial;

        private bool pulseToggle;

        // Use this for initialization
        void Start()
        {
            CellexalEvents.GraphsLoaded.AddListener(StartWave);
            CellexalEvents.GraphsColoredByGene.AddListener(StartWave);
            CellexalEvents.GraphsColoredByIndex.AddListener(StartWave);
            //CellexalEvents.HeatmapCreated.AddListener(StartWave);
            //CellexalEvents.NetworkCreated.AddListener(StartWave);
            CellexalEvents.ScriptFinished.AddListener(() => StartCoroutine(ScriptFinished()));
            //CellexalEvents.ScriptFinished.AddListener(StartWave);
        }

        // Update is called once per frame
        void Update()
        {
            //if (Input.GetKeyDown(KeyCode.T))
            //{
            //    StartWave();
            //}
            //if (Input.GetKeyDown(KeyCode.R))
            //{
            //    StartPulse();
            //}

        }
        /// <summary>
        /// Starts the pulse that goes outwards from the play area.
        /// </summary>
        public void StartPulse()
        {
            gridMaterial.SetFloat("_PulseToggle", 1.0f);
        }

        /// <summary>
        /// Stops the pulse that goes outwards from the play area.
        /// </summary>
        public void StopPulse()
        {
            gridMaterial.SetFloat("_PulseToggle", 0.0f);
        }

        /// <summary>
        /// Starts the wave that circles around the play area.
        /// </summary>
        public void StartWave()
        {
            //gridMaterial.SetFloat("_PulseToggle", 0.0f);
            StartCoroutine(WaveCoroutine());
        }

        private IEnumerator WaveCoroutine()
        {
            float t = 0f;
            float pulseDuration = gridMaterial.GetFloat("_WaveDuration");
            float pulseSpeed = gridMaterial.GetFloat("_WaveSpeed");

            while (t < 1f)
            {
                gridMaterial.SetFloat("_WaveStartTime", t);
                t += Time.deltaTime * pulseSpeed / pulseDuration;
                yield return null;
            }

            gridMaterial.SetFloat("_WaveStartTime", -1f);
        }

        private IEnumerator ScriptFinished()
        {
            float waitTime = 0f;
            while (waitTime < 1.0f)
            {
                waitTime += Time.deltaTime;
                yield return null;
            }
            if (!File.Exists(CellexalUser.UserSpecificFolder + Path.PathSeparator + "mainServer.input.lock"))
            {
                StopPulse();
                StartWave();
            }
        }
    }
}
