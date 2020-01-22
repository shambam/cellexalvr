using UnityEngine;
using UnityEngine.UI;
using System.IO;

namespace CellexalVR.General
{
    /// <summary>
    /// Handles what happens when a username is entered into the username field in the <see cref="DesktopMenu"/>.
    /// </summary>
    public class UsernameManager : MonoBehaviour
    {

        public InputField usernameField;
        public Text usernameText;
        private void Start()
        {
            usernameField.onEndEdit.AddListener(OnUsernameSubmitted);
            usernameText.text = "Current user: " + CellexalUser.Username;
        }

        private void OnUsernameSubmitted(string username)
        {
            CellexalUser.Username = username;
            usernameText.text = "Current user: " + username;
        }
    }


    /// <summary>
    /// This static class represents a user that works with some data.
    /// </summary>
    public static class CellexalUser
    {
        private static string workingDirectory = Directory.GetCurrentDirectory();
        private static string username = "default_user";
        private static string dataFolder;

        /// <summary>
        /// Path to a folder unique to the current user and the currently loaded dataset.
        /// </summary>
        public static string UserSpecificFolder = workingDirectory + @"\Output\" + username;

        /// <summary>
        /// The user's name. This is edited through the escape menu.
        /// </summary>
        public static string Username
        {
            get
            {
                return username;
            }
            set
            {
                if (value.Equals(""))
                    return;
                username = value.ToLower();
                UpdateUserSpecificFolder(username, DataSourceFolder);
                CellexalEvents.UsernameChanged.Invoke();
            }
        }

        /// <summary>
        /// The folder containing the source data that we are currently working on.
        /// This should not be a full path, but rather a relative path from the Data folder.
        /// </summary>
        /// <example>
        /// Loading "Cellexal/Data/data_set_1" means this should be set to "data_set_1"
        /// </example>
        public static string DataSourceFolder
        {
            get
            {
                return dataFolder;
            }
            set
            {
                if (value == "")
                    return;
                dataFolder = value;
                UpdateUserSpecificFolder(Username, value);
            }
        }

        /// <summary>
        /// Helper method to set the user specific folder
        /// </summary>
        private static void UpdateUserSpecificFolder(string username, string dataFolder)
        {
            if (Username == "" || DataSourceFolder == "")
            {
                return;
            }

            // make sure all the folders exist
            string userFolder = workingDirectory + @"\Output\" + username;
            if (!Directory.Exists(userFolder))
            {
                CellexalLog.Log("Created directory " + userFolder);
                Directory.CreateDirectory(userFolder);
            }

            UserSpecificFolder = userFolder + Path.PathSeparator + dataFolder;
            if (!Directory.Exists(UserSpecificFolder))
            {
                CellexalLog.Log("Created directory " + UserSpecificFolder);
                Directory.CreateDirectory(UserSpecificFolder);
            }
        }
    }
}