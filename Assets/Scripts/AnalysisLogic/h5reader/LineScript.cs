using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace CellexalVR.AnalysisLogic.H5reader
{
    public class LineScript : MonoBehaviour
    {
        public AnchorScript AnchorA;
        public AnchorScript AnchorB;
        public LineRenderer line;
        public string type;
        public GameObject linePrefab;
        public bool isMulti;
        private H5ReaderAnnotater h5ReaderAnnotater;
        // Start is called before the first frame update
        void Start()
        {
            h5ReaderAnnotater = GetComponentInParent<H5ReaderAnnotater>();
            line.positionCount = 10;
        }

        // Update is called once per frame
        void Update()
        {
            Vector3 dir = -h5ReaderAnnotater.transform.forward;
            Vector3 start = AnchorA.rect.position;
            Vector3 end = AnchorB.transform.position;
            float dist = Vector3.Distance(start, end);
            if (dist < 0.02f || !AnchorB.isActiveAndEnabled)
            {
                for (int i = 0; i < 10; i++)
                {
                    line.SetPosition(i, start);
                }
                if (dist < 0.02f)
                    AnchorB.GetComponent<MeshRenderer>().enabled = false;
            }
            else
            {
                AnchorB.GetComponent<MeshRenderer>().enabled = true;
                for (int i = 0; i < 10; i++)
                {
                    Vector3 pos = Vector3.Lerp(start, end, i / 9f) + dir * 0.1f * Mathf.Sin(Mathf.PI * (i / 9f));
                    line.SetPosition(i, pos);
                }
            }
        }

        public void OnDestroy()
        {
            Destroy(AnchorB.gameObject);
        }

        public LineScript AddLine()
        {
            GameObject go = (GameObject)Resources.Load("h5Reader/Line");
            go = Instantiate(go, transform.parent);
            LineScript lineScript = go.GetComponent<LineScript>();
            lineScript.type = type;
            lineScript.transform.position = transform.position;
            lineScript.line.startColor = line.startColor;
            lineScript.line.endColor = line.endColor;
            return lineScript;
        }

        public bool IsExpanded()
        {
            return AnchorA.transform.position != AnchorB.transform.position;
        }
    }
}
