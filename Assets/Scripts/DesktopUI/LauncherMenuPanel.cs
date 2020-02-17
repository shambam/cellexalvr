using CellexalVR.General;
using SFB;
using System.IO;
using System.Xml.Serialization;
using TMPro;
using UnityEngine;

namespace CellexalVR.DesktopUI
{

    public class LauncherMenuPanel : MonoBehaviour
    {

        public TMP_InputField rScriptInputField;
        public TMP_InputField usernameInputField;

        private string configDir;
        private string configPath;
        private string sampleConfigPath;
        // Use this for initialization
        void Start()
        {
            ReadConfig();
        }

        /// <summary>
        /// If a config file exists reads it in and sets the R path to the one in the config.
        /// </summary>
        private void ReadConfig()
        {
            configDir = Directory.GetCurrentDirectory() +  @"/Config";
            configPath = configDir +  @"/config.xml";
            sampleConfigPath = Application.streamingAssetsPath +  @"/sample_config.xml";
            if (!Directory.Exists("Config"))
            {
                Directory.CreateDirectory("Config");
                CellexalLog.Log("Created directory " + CellexalLog.FixFilePath(configDir));
            }

            if (!File.Exists(configPath))
            {
                File.Copy(sampleConfigPath, configPath);
                CellexalLog.Log("WARNING: No config file found at " + configPath + ". A sample config file has been created.");
            }
            CellexalLog.Log("Started reading the config file");

            FileStream fileStream = new FileStream(configPath, FileMode.Open);
            StreamReader streamReader = new StreamReader(fileStream);
            XmlSerializer serializer = new XmlSerializer(typeof(Config));
            CellexalConfig.Config = (Config)serializer.Deserialize(streamReader);
            streamReader.Close();
            fileStream.Close();

            if (CellexalConfig.Config.RscriptexePath.Contains(".exe"))
            {
                CrossSceneInformation.RScriptPath = CellexalConfig.Config.RscriptexePath;
            }
            rScriptInputField.text = CellexalConfig.Config.RscriptexePath;

        }

        public void ShowFileDialog()
        {
            SetRScriptPath(StandaloneFileBrowser.OpenFilePanel("Select File", "", "exe", false));

        }


        public void SetRScriptPath()
        {
            SetRScriptPath(new string[1]);
        }

        public void SetRScriptPath(string[] paths)
        {
            if (paths[0] == null)
            {
                paths[0] = rScriptInputField.text;
            }
            if (paths.Length == 1)
            {
                string path = paths[0];
                if (File.Exists(path) && path.Contains(".exe"))
                {
                    bool hasChanged = path != CellexalConfig.Config.RscriptexePath;
                    CrossSceneInformation.RScriptPath = path;
                    rScriptInputField.text = path;
                    if (hasChanged)
                    {
                        CellexalConfig.Config.RscriptexePath = path;
                        string configDir = Directory.GetCurrentDirectory() +  @"/Config";
                        string configPath = configDir +  @"/config.xml";
                        if (!Directory.Exists("Config"))
                        {
                            Directory.CreateDirectory("Config");
                            CellexalLog.Log("Created directory " + CellexalLog.FixFilePath(configDir));
                        }

                        CellexalLog.Log("Started saving the config file");

                        FileStream fileStream = new FileStream(configPath, FileMode.Create, FileAccess.Write, FileShare.None);
                        StreamWriter streamWriter = new StreamWriter(fileStream);

                        XmlSerializer ser = new XmlSerializer(typeof(Config));
                        ser.Serialize(streamWriter, CellexalConfig.Config);
                        streamWriter.Close();
                        fileStream.Close();
                    }
                }
                else
                {
                    rScriptInputField.text = "Failed to find R path";
                }
            }
        }

        public void SetUsername()
        {
            //CrossSceneInformation.Username = usernameInputField.text;
            CellexalUser.Username = usernameInputField.text;
            PhotonNetwork.playerName = CellexalUser.Username + Random.Range(0, 10000);
            PhotonNetwork.AuthValues = new AuthenticationValues();
            PhotonNetwork.AuthValues.UserId = PhotonNetwork.playerName;
        }


    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }
}
}
