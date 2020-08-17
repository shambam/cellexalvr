using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
namespace CellexalVR.AnalysisLogic.H5reader
{
    public class ProjectionObjectScript : MonoBehaviour
    {
        public enum projectionType
        {
            p3D,
            p2D_sep,
            p3D_sep,
            p2D
        }

        public projectionType type;
        public string name = "unnamed-projection";
        private Dictionary<string, string> paths;
        private Dictionary<string, char> dataTypes;

        public GameObject AnchorPrefab;
        public H5ReaderAnnotater h5readerAnnotater;
        private List<GameObject> instantiatedGameObjects;

        private Dictionary<projectionType, string[]> menu_setup;
        private TextMeshProUGUI nameTextMesh;

        void Awake()
        {
            paths = new Dictionary<string, string>();
            dataTypes = new Dictionary<string, char>();
            nameTextMesh = transform.GetChild(0).GetComponentInChildren<TextMeshProUGUI>();


            menu_setup = new Dictionary<projectionType, string[]>
            {
                 { projectionType.p3D, new string[] { "X", "vel" } },
                 { projectionType.p2D_sep, new string[] { "X", "Y", "velX", "velY" } },
                 { projectionType.p3D_sep, new string[] { "X", "Y", "Z", "velX", "velY", "velZ" } },
                 { projectionType.p2D, new string[] { "X", "velX"} }
            };
        }

        // Update is called once per frame
        void Update()
        {

        }

        public void AddToPaths(string key, string value, char dtype)
        {
            if (!paths.ContainsKey(key))
            {
                paths.Add(key, value);
                dataTypes.Add(key, dtype);
            }
            else
            {
                paths[key] = value;
                dataTypes[key] = dtype;
            }
            h5readerAnnotater.AddToConfig(key + "_" + name, value, dtype);
        }

        public void RemoveFromPaths(string key)
        {
            if (paths.ContainsKey(key))
            {
                paths.Remove(key);
            }
            if (dataTypes.ContainsKey(key))
                dataTypes.Remove(key);

            h5readerAnnotater.RemoveFromConfig(key + "_" + name);

        }

        public void SwitchToSeparate()
        {
         
            
            switch (type)
            {
                case projectionType.p3D:
                    type = projectionType.p3D_sep;
                    break;
                case projectionType.p2D_sep:
                    type = projectionType.p2D;
                    break;
                case projectionType.p3D_sep:
                    type = projectionType.p3D;
                    break;
                case projectionType.p2D:
                    type = projectionType.p2D_sep;
                    break;
            }


            foreach (string key in paths.Keys)
                h5readerAnnotater.RemoveFromConfig(key + "_" + name);

            Init(type);
        }


        public void ChangeName(string name)
        {
            print(name);
            this.name = name;
            nameTextMesh.text = name;
        }

        public void OnDestroy()
        {
            foreach (string key in paths.Keys)
                h5readerAnnotater.RemoveFromConfig(key + "_" + name);

            h5readerAnnotater.projectionObjectScripts.Remove(this);
            if (instantiatedGameObjects != null)
            {
                foreach (GameObject g in instantiatedGameObjects)
                {
                    Destroy(g);
                }
            }
            Destroy(this.gameObject);
            RectTransform rect;
            int counter = 0;
            foreach (ProjectionObjectScript p in h5readerAnnotater.projectionObjectScripts)
            {
                rect = p.GetComponent<RectTransform>();
                rect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, rect.rect.width * (1.1f) * counter++, rect.rect.width);
            }
        }

        public void Init(projectionType type)
        {
            this.type = type;
            paths = new Dictionary<string, string>();
            if(instantiatedGameObjects != null)
            {
                foreach (GameObject g in instantiatedGameObjects)
                    Destroy(g);
            }
            instantiatedGameObjects = new List<GameObject>();
                

            int offset = 0;
            GameObject go;

            foreach (string anchor in menu_setup[type])
            {
                go = Instantiate(AnchorPrefab, gameObject.transform, false);
                go.transform.localPosition += Vector3.up * -10 * offset;
                go.transform.localPosition += Vector3.back * 0.1f; 
                go.GetComponentInChildren<LineScript>().type = anchor;
                offset++;
                go.GetComponent<TextMeshProUGUI>().text = anchor;


                instantiatedGameObjects.Add(go);
            }
        }
    }
}
