using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using CellexalVR.General;
using UnityEngine.UI;
using System.IO;
namespace CellexalVR.AnalysisLogic.H5reader
{
    public class H5ReaderAnnotatorTextBoxScript : MonoBehaviour
    {
        public ReferenceManager referenceManager;
        private SteamVR_TrackedObject rightController;
        private SteamVR_Controller.Device device;
        private bool controllerInside;
        public Dictionary<string, H5ReaderAnnotatorTextBoxScript> subkeys = new Dictionary<string, H5ReaderAnnotatorTextBoxScript>();
        public H5ReaderAnnotatorTextBoxScript parentScript;
        public GameObject textBoxPrefab;
        public RectTransform rect;
        public TextMeshProUGUI tmp;
        public BoxCollider boxCollider;

        public RectTransform expandButtonRect;
        public BoxCollider expandButtonBoxCollider;
        public GameObject KeyObject;

        public H5readerAnnotater annotater;


        public string name;
        public bool isTop;
        public bool isBottom = false;
        private Color hoverColor = Color.white;
        public Color color = Color.black;
        private Color highlightColor = Color.yellow;
        private bool highLightOn = false;
        public string type = "none";
        public bool isSelected = false;
        private float timer = 0f;

        private void Start()
        {
            if (!referenceManager)
            {
                referenceManager = GameObject.Find("InputReader").GetComponent<ReferenceManager>();
            }
            rightController = referenceManager.rightController;
        }

        public void Insert(string name, H5readerAnnotater annotaterScript)
        {
            annotater = annotaterScript;
            if (name.Contains("/"))
            {
                string parentKey = name.Substring(0, name.IndexOf("/"));
                string newName = name.Substring(name.IndexOf("/") + 1);
                if (!subkeys.ContainsKey(parentKey))
                {
                    GameObject go = Instantiate(textBoxPrefab);
                    H5ReaderAnnotatorTextBoxScript script = go.GetComponent<H5ReaderAnnotatorTextBoxScript>();
                    script.isTop = false;
                    script.name = parentKey;
                    subkeys.Add(parentKey, script);
                    script.parentScript = this;
                }
                subkeys[parentKey].Insert(newName, annotaterScript);
            }
            else
            {
                GameObject go = Instantiate(textBoxPrefab);
                H5ReaderAnnotatorTextBoxScript script = go.GetComponent<H5ReaderAnnotatorTextBoxScript>();
                script.name = name;
                subkeys.Add(name, script);
                script.isTop = false;
                script.isBottom = true;
                script.parentScript = this;
                if(!isTop)
                    go.SetActive(false);
            }
        }

        public char GetDataType()
        { 
            char dtype = name[name.Length - 1];
            return dtype;
        }

        public string GetName()
        {
            return name.Substring(0, name.LastIndexOf(":"));
        }

        public string GetPath()
        {
            string path = GetName();
            H5ReaderAnnotatorTextBoxScript p = parentScript;
            while (p.isTop == false)
            {
                path = p.name + "/" + path;
                p = p.parentScript;
            }
            return path;
        }

        public ArrayList GetTypeInChildren(string type)
        {
            ArrayList list = new ArrayList();
            foreach (H5ReaderAnnotatorTextBoxScript k in subkeys.Values)
            {
                list.AddRange(k.GetTypeInChildren(type));
                if (k.type == type)
                {
                    list.Add(k);
                }
            }
            return list;
        }

        public void FillContent(RectTransform content, int depth = 0)
        {
            gameObject.name = name;
            transform.SetParent(content);

            rect.localPosition = Vector3.zero;
            rect.localEulerAngles = Vector3.zero;
            rect.localScale = new Vector3(1, 1, 1);

            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);

            tmp.fontSize = 8;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            string text = "";
            for (int i = 0; i < depth; i++)
            {
                text += "--";
            }
            text += "> ";
            tmp.text = text + name;

            foreach (H5ReaderAnnotatorTextBoxScript k in subkeys.Values)
            {
                k.FillContent(rect, depth + 1);
            }
        }

        public float UpdatePosition(float offset = 0f)
        {
            rect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, offset, 0);
            rect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 5f, 0);
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
            boxCollider.center = new Vector3(rect.rect.width / 2, -rect.rect.height / 2, 0);
            boxCollider.size = new Vector3(rect.rect.width, rect.rect.height, 1);

            expandButtonRect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 0, 8f);
            expandButtonRect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Right, -15f, 8f);
            expandButtonBoxCollider.size = new Vector3(expandButtonRect.rect.size.x, expandButtonRect.rect.size.y,5);
            
            

            float temp = 0f;
            foreach (H5ReaderAnnotatorTextBoxScript k in subkeys.Values)
            {
                if (k.gameObject.activeSelf)
                {
                    temp += 10f;
                    temp += k.UpdatePosition(temp);
                }
            }
            return temp;
        }

        private void OnTriggerEnter(Collider other)
        {
            //if (other.gameObject.name.Equals("ControllerCollider(Clone)"))
            
            if (other.gameObject.name.Equals("AnchorB"))
            {
                controllerInside = true;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.name.Equals("AnchorB"))
            {
                controllerInside = false;
            }
        }

        private void Update()
        {
            device = SteamVR_Controller.Input((int)rightController.index);
            if (controllerInside && device.GetPressDown(SteamVR_Controller.ButtonMask.Trigger))
            {

            }
            if (isSelected)
            {
                timer += UnityEngine.Time.deltaTime;
                if (timer > 0.5f)
                {
                    timer = 0f;
                    if (highLightOn)
                    {
                        tmp.color = color;
                        highLightOn = false;
                    }
                    else
                    {
                        tmp.color = highlightColor;
                        highLightOn = true;
                    }
                }
            }

        }
    }
}


