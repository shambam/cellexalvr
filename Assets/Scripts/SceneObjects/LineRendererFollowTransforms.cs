using UnityEngine;
using System.Collections;

namespace CellexalVR.SceneObjects
{
    public class LineRendererFollowTransforms : MonoBehaviour
    {
        public LineRenderer lineRenderer;
        public Transform transform1;
        public Transform transform2;
        public bool bendLine;


        private void Update()
        {
            if (bendLine)
            {
                Vector3 dir = -transform1.transform.forward + transform1.transform.up;
                Vector3 start = transform1.position;
                Vector3 end = transform2.transform.position;
                if (!transform1.hasChanged && !transform2.hasChanged) return;
                for (int i = 0; i < 20; i++)
                {
                    Vector3 pos = Vector3.Lerp(start, end, i / 19f) + dir * 0.02f * Mathf.Sin(Mathf.PI * (i / 19f));
                    lineRenderer.SetPosition(i, pos);
                }
            }
            else
            {
                lineRenderer.SetPositions(new Vector3[]
                {
                    transform1.position,
                    transform2.position
                });
            }
        }
    }
}