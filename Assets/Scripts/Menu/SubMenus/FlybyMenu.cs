using CellexalVR.General;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;

namespace CellexalVR.Menu.SubMenus
{
    public class FlybyMenu : MenuWithoutTabs
    {
        public GameObject cameraGameObject;
        private List<Vector3> positions = new List<Vector3>();
        private List<Quaternion> rotations = new List<Quaternion>();

        //private int captureWidth = 1280;
        //private int captureHeight = 720;
        private int framesPerPos = 100;

        public void RecordPosition(Vector3 position, Quaternion rotation)
        {
            positions.Add(position);
            rotations.Add(rotation);
        }

        //public void RenderFlyby()
        //{
        //    StartCoroutine(RenderFlybyCoroutine());
        //}

        //private IEnumerator void RenderFlybyCoroutine()
        public void RenderFlyby()
        {
            CellexalLog.Log("Started creating flyby");
            string outputPath = CellexalUser.UserSpecificFolder + @"\Flyby\";
            if (Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
                CellexalLog.Log("Created directory " + outputPath);
            }

            int dirEnd = 0;

            while (Directory.Exists(outputPath + @"\Flyby" + dirEnd))
            {
                dirEnd++;
            }
            outputPath = outputPath + @"\Flyby" + dirEnd;
            Directory.CreateDirectory(outputPath);
            CellexalLog.Log("Created directory " + outputPath);

            cameraGameObject.SetActive(true);
            cameraGameObject.transform.parent = null;
            Camera camera = cameraGameObject.GetComponent<Camera>();
            RenderTexture renderTexture = camera.targetTexture;
            RenderTexture.active = renderTexture;
            Texture2D frame = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
            Rect rect = new Rect(0, 0, renderTexture.width, renderTexture.height);
            List<string> savedImages = new List<string>();
            float tInc = 1f / framesPerPos;
            int fileId = 0;

            for (int i = 0; i < positions.Count - 1; ++i)
            {
                Vector3 pos1 = positions[i];
                Vector3 pos2 = positions[i + 1];

                Quaternion rot1 = rotations[i];
                Quaternion rot2 = rotations[i + 1];
                for (float t = 0; t < 1f; t += tInc)
                {
                    cameraGameObject.transform.position = Vector3.Lerp(pos1, pos2, t);
                    cameraGameObject.transform.rotation = Quaternion.Lerp(rot1, rot2, t);
                    camera.Render();
                    frame.ReadPixels(rect, 0, 0);
                    byte[] frameData = frame.EncodeToJPG();

                    Thread thread = new Thread(() =>
                    {
                        string fileName = "frame_" + fileId.ToString("D6") + ".jpg";

                        FileStream fileStream = File.Create(outputPath + Path.DirectorySeparatorChar + fileName);
                        fileStream.Write(frameData, 0, frameData.Length);
                        fileStream.Flush();
                        fileStream.Close();

                        savedImages.Add(fileName);
                    });
                    thread.Start();
                    fileId++;
                }
                //yield return null;
            }

            cameraGameObject.transform.parent = transform;
            cameraGameObject.SetActive(false);
            CellexalLog.Log("Finished creating flyby");
        }

        public void ResetPositions()
        {
            positions.Clear();
            rotations.Clear();
        }
    }
}
