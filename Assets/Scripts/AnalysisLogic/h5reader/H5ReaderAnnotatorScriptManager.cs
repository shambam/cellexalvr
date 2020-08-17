using CellexalVR.General;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace CellexalVR.AnalysisLogic.H5reader
{
    public class H5ReaderAnnotatorScriptManager : MonoBehaviour
    {
        public GameObject annotatorPrefab;
        public Dictionary<string, H5ReaderAnnotater> annotators = new Dictionary<string, H5ReaderAnnotater>();
        private ReferenceManager referenceManager;

        private Vector3 finalPoint;
        private Vector3 finalScale;
        // Start is called before the first frame update
        private void Start()
        {
            if (!referenceManager)
            {
                referenceManager = GameObject.Find("InputReader").GetComponent<ReferenceManager>();
            }
        }

        public void AddAnnotator(string path)
        {
            if (annotators.ContainsKey(path))
            {
                annotators[path].gameObject.SetActive(true);
            }
            else
            {
                GameObject go = Instantiate(annotatorPrefab, this.transform);
                go.transform.parent = transform;
                go.transform.localRotation = Quaternion.Euler(0, -90, 0);
                finalScale = go.transform.localScale;
                go.transform.localScale *= 0.01f;


                Transform funnel = referenceManager.loaderController.transform;
                print(funnel.transform.position);

                go.transform.position = funnel.transform.position;

                finalPoint = funnel.transform.position + Vector3.up*1.2f;
                print(finalPoint);

                StartCoroutine(SpawnAnimation(go));

                H5ReaderAnnotater script = go.GetComponent<H5ReaderAnnotater>();
                script.Init(path);
                annotators.Add(path, script);
            }
        }

        public void RemoveAnnotator(string path,bool destroy = false)
        {
            annotators[path].gameObject.SetActive(false);

            if (destroy)
            {
                Destroy(annotators[path].gameObject);
                annotators.Remove(path);
            }
        }

        IEnumerator SpawnAnimation(GameObject go)
        {
            // Getting the difference in position and rotation
            Vector3 deltaPos = finalPoint - go.transform.position;
            Vector3 deltaScale = finalScale - go.transform.localScale;
            //Vector3 deltaRotation = finalPoint.rotation.eulerAngles - transform.rotation.eulerAngles;
            // Repeat once per frame for a second:
            yield return new WaitForSeconds(1f);
            float timePassed = 0;
            while (timePassed < 1.0f)
            {
                // Increment the time to prevent an infinite loop
                timePassed += Time.deltaTime;
                // Slowly adjust the position and rotation so it approaches the spawn position and rotation
                go.transform.position += deltaPos * Time.deltaTime;
                go.transform.localScale += deltaScale * Time.deltaTime;
                //transform.Rotate(deltaRotation * Time.deltaTime);
                // Exit the function (it will begin where it left off the next frame)
                yield return null;
            }
            // Ensures the player is exactly where you want him when the time is up           
            go.transform.position = finalPoint;
            go.transform.localScale = finalScale;
            //transform.rotation = spawnPoint.rotation;
            // Waits for a second after the player has returned before running the game again
            //yield return new WaitForSeconds(1f);
        }
    }

}
