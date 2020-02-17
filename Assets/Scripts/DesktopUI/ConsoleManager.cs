using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using CellexalVR.General;
using System.IO;

namespace CellexalVR.DesktopUI
{

    /// <summary>
    /// The console for executing commands from the desktop.
    /// </summary>
    public class ConsoleManager : MonoBehaviour
    {
        public ReferenceManager referenceManager;
        public GameObject consoleGameObject;
        public TMPro.TMP_InputField inputField;
        public TMPro.TMP_InputField outputField;
        public TMPro.TMP_Text suggestionField;
        private bool consoleActive = false;
        private Dictionary<MethodInfo, string> accessors = new Dictionary<MethodInfo, string>();
        private Dictionary<string, MethodInfo> commands = new Dictionary<string, MethodInfo>();
        private Dictionary<string, string> folders = new Dictionary<string, string>();
        private Dictionary<MethodInfo, List<string>> aliases = new Dictionary<MethodInfo, List<string>>();

        private LinkedList<string> history = new LinkedList<string>();
        private LinkedListNode<string> currentHistoryNode;
        // default value in case things are written to the console before the log is read.
        private int maxBufferLines = 64;
        private int currentNumberOfLines = 0;
        private string outputBufferString = "";
        private bool awaitingConfirm = false;
        // flags if running multiple commands from a file
        private bool lastCommandFinished = false;
        private bool lastCommandOK = false;

        private void OnValidate()
        {
            if (gameObject.scene.IsValid())
            {
                referenceManager = GameObject.Find("InputReader").GetComponent<ReferenceManager>();
            }
        }

        private void Awake()
        {
            CellexalEvents.ConfigLoaded.AddListener(OnConfigLoaded);
            CellexalEvents.CommandFinished.AddListener(CommandFinished);
        }

        private void Start()
        {
            // scan everything for methods that are marked as console commands
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    foreach (var method in type.GetMethods())
                    {
                        var attribute = method.GetCustomAttribute<ConsoleCommandAttribute>();
                        if (attribute != null)
                        {
                            accessors[method] = attribute.Access;
                            foreach (string alias in attribute.Aliases)
                            {
                                commands[alias] = method;
                                if (!aliases.ContainsKey(method))
                                {
                                    aliases[method] = new List<string>();
                                }
                                aliases[method].Add(alias);
                                folders[alias] = attribute.Folder;
                            }
                        }
                    }
                }
            }

            history.AddFirst("");
            currentHistoryNode = history.First;

            //float fontMultiplier = Screen.dpi / 100f;
            //outputField.pointSize *= fontMultiplier;
            //inputField.pointSize *= fontMultiplier;
            //inputField.pointSize *= fontMultiplier;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F12))
            {
                consoleActive = !consoleActive;
                if (consoleActive)
                {
                    consoleGameObject.SetActive(true);
                    inputField.ActivateInputField();
                    //inputField.Select();
                    //StartCoroutine(DeselectInputField());
                }
                else
                {

                    inputField.DeactivateInputField();
                    consoleGameObject.SetActive(false);
                }
            }

            if (!consoleActive)
                return;

            // only if the console is active
            if (!awaitingConfirm && Input.GetKeyDown(KeyCode.Return))
            {
                EnterCommand(inputField.text);
            }
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                TraverseHistory(true);
            }
            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                TraverseHistory(false);
            }
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                AutocompleteInput(inputField.text);
            }
        }

        private void OnConfigLoaded()
        {
            maxBufferLines = CellexalConfig.Config.ConsoleMaxBufferLines;
        }

        /// <summary>
        /// We can't deselect the text in the console input window the same frame it is activated, so this coroutine waits one frame and then deselects it.
        /// We also can't move the cursor at all without waiting a frame.
        /// </summary>
        private IEnumerator MoveTextEnd()
        {
            yield return null;
            inputField.MoveTextEnd(false);
        }

        /// <summary>
        /// Goes one step forward or backward in the history of all written commands.
        /// </summary>
        /// <param name="goBack">True for going back in history, false for going forward.</param>
        public void TraverseHistory(bool goBack)
        {
            if (goBack && currentHistoryNode.Next != null)
            {
                if (currentHistoryNode == history.First)
                {
                    currentHistoryNode.Value = inputField.text;
                }
                currentHistoryNode = currentHistoryNode.Next;
            }
            else if (!goBack && currentHistoryNode.Previous != null)
            {
                currentHistoryNode = currentHistoryNode.Previous;
            }
            ClearAndHideSuggestions();
            inputField.text = currentHistoryNode.Value;
            inputField.MoveTextEnd(false);
        }

        /// <summary>
        /// Appends some output to the console's output window
        /// </summary>
        /// <param name="output">The string to append.</param>
        public void AppendOutput(string output)
        {
            outputBufferString = outputBufferString + "\n" + output;
            // find number of lines breaks
            int nbrOfNewLines = output.Count((c) => c == '\n') + 1;
            currentNumberOfLines += nbrOfNewLines;

            if (currentNumberOfLines > maxBufferLines)
            {
                int nbrOfExcessLines = currentNumberOfLines - maxBufferLines;
                // remove the oldest line
                // find how many characters that is
                int lineBreakIndex = 0;
                for (int i = 0; i < nbrOfExcessLines; ++i)
                {
                    while (outputBufferString[lineBreakIndex] != '\n')
                    {
                        lineBreakIndex++;
                    }
                    lineBreakIndex++;
                }
                // remove that much
                outputBufferString = outputBufferString.Remove(0, lineBreakIndex);
                currentNumberOfLines -= nbrOfExcessLines;
            }

            outputField.text = outputBufferString;
            outputField.MoveTextEnd(false);
            outputField.textComponent.ForceMeshUpdate();
            //Canvas.ForceUpdateCanvases();

            ClearAndHideSuggestions();
        }

        /// <summary>
        /// Called when a command is entered.
        /// Checks that the command syntax is correct and executes the command.
        /// </summary>
        public void EnterCommand(string command)
        {
            AppendOutput("> " + command);
            inputField.text = "";

            inputField.ActivateInputField();
            inputField.Select();


            if (command == "")
            {
                return;
            }

            history.First.Value = command;
            history.AddFirst("");
            currentHistoryNode = history.First;

            string[] words = command.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            // check that the command exists
            if (!commands.ContainsKey(words[0]))
            {
                AppendOutput("Command not found. Type 'listall' for all defined commands.");
                return;
            }

            MethodInfo method = commands[words[0]];
            string accessFieldName = accessors[method];

            // get the object that the method should run in
            var access = referenceManager.GetType().GetField(accessFieldName).GetValue(referenceManager);

            ParameterInfo[] parameterInfo = method.GetParameters();
            // check the number of parameters
            if (words.Length - 1 != parameterInfo.Length)
            {
                AppendOutput(string.Format("Wrong number of parameters. The command {0} has the parameters {1}", words[0], ParameterInfosToString(parameterInfo)));
                return;
            }

            object[] parameters = new object[words.Length - 1];
            // parse the parameters
            for (int i = 1; i < words.Length; ++i)
            {
                try
                {
                    parameters[i - 1] = ParseArgument(words[i], parameterInfo[i - 1].ParameterType);
                }
                catch (ArgumentException e)
                {
                    AppendOutput(e.Message);
                    return;
                }
                catch (FormatException)
                {
                    AppendOutput(string.Format("The paramater {0} could not be parsed as type {1}", words[i], parameterInfo[i - 1].ParameterType));
                    return;
                }
            }
            //CellexalLog.Log("Running command: " + command);

            // finally invoke the method
            method.Invoke(access, parameters);
        }

        /// <summary>
        /// Helper method to parse arguments for commands.
        /// Commands must be executed with a <see cref="object[]"/> as arguments, but the actual type of the objects must be whatever the function expects.
        /// Throws <see cref="ArgumentException"/> if <paramref name="t"/> is not a parsable type.
        /// </summary>
        /// <param name="arg">The argument to parse.</param>
        /// <param name="t">The type that the argument should be.</param>
        /// <returns>A parsed argument of the type of <paramref name="t"/>.</returns>
        private object ParseArgument(string arg, Type t)
        {
            if (t == typeof(int))
            {
                return int.Parse(arg);
            }
            else if (t == typeof(float))
            {
                return float.Parse(arg);
            }
            else if (t == typeof(double))
            {
                return double.Parse(arg);
            }
            else if (t == typeof(bool))
            {
                return arg != "0";
            }
            else if (t == typeof(string))
            {
                return arg;
            }
            else
            {
                // unknown type
                throw new ArgumentException("ERROR: Argument was not a known type that could be parsed");
            }
        }

        /// <summary>
        /// Converts an array of <see cref="System.Reflection.ParameterInfo"/> to a readable string.
        /// </summary>
        /// <param name="info">The <see cref="System.Reflection.ParameterInfo"/> to convert to a string.</param>
        private string ParameterInfosToString(ParameterInfo[] info)
        {
            if (info.Length == 0)
            {
                return "Command has no arguments";
            }
            StringBuilder sb = new StringBuilder();
            sb.Append(info[0].ParameterType).Append(" ").Append(info[0].Name);
            for (int i = 1; i < info.Length; ++i)
            {
                sb.Append(", ").Append(info[i].ParameterType).Append(" ").Append(info[i].Name);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Auto-completes a command or file path.
        /// </summary>
        /// <param name="currentCommand">What the user has written so far in the termninal. (The full line).</param>
        private void AutocompleteInput(string currentCommand)
        {
            ClearAndHideSuggestions();
            if (currentCommand == "")
            {
                return;
            }

            string[] words = currentCommand.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string command = words[0];

            if (words.Length == 1)
            {
                // suggest command
                // get all commands that start with what is written in the console.
                string[] allCommands = commands.Keys.Where((string key) => key.Length >= command.Length && command == key.Substring(0, command.Length)).ToArray();
                if (allCommands.Length > 1)
                {
                    suggestionField.text = string.Join(" ", allCommands);
                    suggestionField.transform.parent.gameObject.SetActive(true);
                }
                string longestCommonBeginning = LongestCommonBeginning(words[0], allCommands);
                inputField.text = longestCommonBeginning;
            }
            else
            {
                // suggest file or folder
                string currentText = words[words.Length - 1];
                string folder = Directory.GetCurrentDirectory() + "/" + folders[command] + "/";
                string[] foundFolders = Directory.GetDirectories(folder, currentText + "*", SearchOption.TopDirectoryOnly);
                string[] foundFiles = Directory.GetFiles(folder, currentText + "*", SearchOption.TopDirectoryOnly);
                // put everything in one list
                var list = foundFolders.Concat(foundFiles);
                // remove the unnecessary full path, just keep the relative part
                list = list.Select((f) => f.Substring(folder.Length));

                if (currentText.Length > 0)
                {
                    // Directory.GetDirectories returns hidden folders even if the user did not start the searchpattern with '.'
                    // Remove those unless the user did start the searchpattern with '.'
                    // Otherwise the LongestCommonBeginning later can fail in some cases.
                    char firstchar = char.ToLower(currentText[0]);
                    list = list.Where((f) => char.ToLower(f[0]).Equals(firstchar));
                }

                string[] foldersAndFiles = list.ToArray();

                if (foldersAndFiles.Length > 1)
                {
                    suggestionField.text = string.Join(" ", foldersAndFiles);
                    suggestionField.transform.parent.gameObject.SetActive(true);
                }

                string longestCommonBeginning = LongestCommonBeginning(currentText, foldersAndFiles);
                words[words.Length - 1] = longestCommonBeginning;
                inputField.text = string.Join(" ", words);
            }

            StartCoroutine(MoveTextEnd());

        }

        /// <summary>
        /// Finds the longest common beginning of strings, used for autocompleting commands and file paths.
        /// </summary>
        /// <param name="start">What the user has written so far.</param>
        /// <param name="words">An array of words to check for the a common beginning.</param>
        /// <returns>The longest common beginning shared between <paramref name="start"/> and all strings in <paramref name="words"/>.</returns>
        private string LongestCommonBeginning(string start, string[] words)
        {
            if (words.Length == 0)
            {
                return start;
            }
            string shortestCommon = words[0];
            for (int i = 1; i < words.Length && shortestCommon.Length > start.Length; ++i)
            {

                for (int j = 0; j < words[i].Length && j < shortestCommon.Length; ++j)
                {
                    if (char.ToLower(words[i][j]) != char.ToLower(shortestCommon[j]))
                    {
                        shortestCommon = shortestCommon.Substring(0, j);
                        break;
                    }
                }
            }
            return shortestCommon;
        }

        /// <summary>
        /// Clears and hides the bar that show suggestions below the input field.
        /// </summary>
        public void ClearAndHideSuggestions()
        {
            suggestionField.text = "";
            suggestionField.transform.parent.gameObject.SetActive(false);
        }

        #region GENERAL COMMANDS
        /// <summary>
        /// Lists all available commands.
        /// </summary>
        [ConsoleCommand("consoleManager", aliases: "listall")]
        public void ListAllCommands()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var alias in aliases)
            {
                sb.Append(alias.Key.Name).Append(" is mapped to ");
                List<string> methodAliases = alias.Value;
                sb.Append(methodAliases[0]);
                for (int i = 1; i < methodAliases.Count; ++i)
                {
                    sb.Append(", ").Append(methodAliases[i]);
                }
                sb.Append("\n");

            }
            AppendOutput(sb.ToString());
            CellexalEvents.CommandFinished.Invoke(true);
        }

        /// <summary>
        /// Displays information about the arguments of a command. Semi-helpful.
        /// </summary>
        /// <param name="command">The command to show arguemnt information about.</param>
        [ConsoleCommand("consoleManager", aliases: new string[] { "arguments", "args" })]
        public void ParameterInfo(string command)
        {
            string parameterInfoString = ParameterInfosToString(commands[command].GetParameters());
            if (parameterInfoString == "")
            {
                AppendOutput("Command has no arguments");
            }
            else
            {
                AppendOutput(parameterInfoString);
            }
            CellexalEvents.CommandFinished.Invoke(true);
        }

        /// <summary>
        /// Clears the console.
        /// </summary>
        [ConsoleCommand("consoleManager", aliases: new string[] { "clear", "clr", "cls" })]
        public void ClearConsole()
        {
            currentNumberOfLines = 0;
            outputBufferString = string.Empty;
            outputField.text = string.Empty;
            CellexalEvents.CommandFinished.Invoke(true);
        }

        /// <summary>
        /// Asks for a confirmation and eventually quits CellexalVR.
        /// </summary>
        [ConsoleCommand("consoleManager", aliases: new string[] { "quit", "goodbye" })]
        public void Quit()
        {
            StartCoroutine(QuitCoroutine());
        }

        /// <summary>
        /// Helper coroutine for quitting.
        /// </summary>
        private IEnumerator QuitCoroutine()
        {
            AppendOutput("Really quit CellexalVR?\ny/n");
            awaitingConfirm = true;
            do
            {
                yield return null;
            }
            while (!Input.GetKeyDown(KeyCode.Return));

            awaitingConfirm = false;
            string input = inputField.text;
            if (input == "y")
            {
                CellexalLog.Log("Quit command issued");
                CellexalLog.LogBacklog();
                // Application.Quit() does not work in the unity editor, only in standalone builds.
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
            }
            CellexalEvents.CommandFinished.Invoke(true);
        }

        /// <summary>
        /// Shows or hides a little fps counter in the top left corner of the desktop display.
        /// </summary>
        /// <param name="b">True to show hte fps coutner, false to hide it.</param>
        [ConsoleCommand("consoleManager", aliases: "fps")]
        public void ShowFPS(bool b)
        {
            referenceManager.fpsCounter.SetActive(b);
            CellexalEvents.CommandFinished.Invoke(true);
        }

        /// <summary>
        /// Runs multiple commands written in a file.
        /// </summary>
        /// <param name="filename"></param>
        [ConsoleCommand("consoleManager", aliases: new string[] { "readcommandfile", "rcf" })]
        public void RunCommandFile(string filename)
        {
            StartCoroutine(RunCommandFileCoroutine(filename));
        }

        /// <summary>
        /// Helper coroutine to run multiple commands. See <see cref="ConsoleManager.RunCommandFile(string)"/>.
        /// </summary>
        private IEnumerator RunCommandFileCoroutine(string filename)
        {
            if (!File.Exists(filename))
            {
                CellexalLog.Log("ERROR: File not found: " + filename);
                CellexalEvents.CommandFinished.Invoke(false);
                yield break;
            }

            using (FileStream fileStream = new FileStream(filename, FileMode.Open))
            using (StreamReader streamReader = new StreamReader(fileStream))
            {
                while (!streamReader.EndOfStream)
                {
                    string line = streamReader.ReadLine();
                    lastCommandFinished = false;
                    EnterCommand(line);
                    do
                    {
                        yield return null;
                    } while (!lastCommandFinished);

                    if (!lastCommandOK)
                    {
                        break;
                    }
                }

                streamReader.Close();
                fileStream.Close();
            }
            CellexalEvents.CommandFinished.Invoke(true);
        }

        /// <summary>
        /// Called when the <see cref="CellexalEvents.CommandFinished"/> event is invoked.
        /// </summary>
        /// <param name="ok">True if the command that finished was successful, false otherwise.</param>
        private void CommandFinished(bool ok)
        {
            lastCommandFinished = true;
            lastCommandOK = ok;
        }

        #endregion

    }

    /// <summary>
    /// Attribute to mark methods as runnable using the in-app console interface.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    public class ConsoleCommandAttribute : Attribute
    {
        public string Access { get; private set; }
        public string Folder { get; private set; }
        public string[] Aliases { get; private set; }

        /// <summary>
        /// Marks a method as runnable in the console.
        /// </summary>
        /// <param name="access">The name of a field in the <see cref="ReferenceManager"/> to access this method from. Case sensitive.</param>
        /// <param name="aliases">One or more ways to refer to this method from the console.</param>
        public ConsoleCommandAttribute(string access, string folder = "", params string[] aliases)
        {
            Access = access;
            Folder = folder;
            Aliases = aliases;
        }
    }
}