using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using CellexalVR.DesktopUI;
using CellexalVR.Extensions;
using UnityEngine;
using HtmlAgilityPack;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;

namespace CellexalVR.General
{
    /// <summary>
    /// Recreate session based on a previously saved session report.
    /// Reads the different parts of the log and compiles a document with the corresponding commands used.
    /// This document can then be read by the ReadCommandsFile function <see cref="ReadCommandFile"/>.
    /// </summary>
    public class ReportReader : MonoBehaviour
    {
        public ReferenceManager referenceManager;

        private void OnValidate()
        {
            if (gameObject.scene.IsValid())
            {
                referenceManager = GameObject.Find("InputReader").GetComponent<ReferenceManager>();
            }
        }


        [ConsoleCommand("reportReader", folder: "Output", aliases: new string[] {"readreport", "rr"})]
        public void ReadFolder(string reportPath)
        {
            string[] words = reportPath.Split(Path.DirectorySeparatorChar);
            string pathLastPart = words[words.Length - 1];
            string dataFolderPart = words[words.Length - 2];
            int fromIndex = pathLastPart.IndexOfAny("0123456789".ToCharArray());
            int toIndex = pathLastPart.LastIndexOfAny("0123456789".ToCharArray()) + 1;
            string reportFolder = pathLastPart.Substring(fromIndex, toIndex - fromIndex)
                .Replace('-', '_');
            reportPath = (CellexalUser.UserSpecificFolder + "/" + dataFolderPart + "/" + pathLastPart).FixFilePath();
            string commandFile = (CellexalUser.UserSpecificFolder +
                                  "/" + dataFolderPart +
                                  "/" + reportFolder + 
                                  "/command_file.txt").FixFilePath();
            if (!File.Exists(commandFile))
            {
                CreateCommandFileFromHtmlReport(reportPath, dataFolderPart, reportFolder);
            }

            referenceManager.consoleManager.RunCommandFile(commandFile);
        }

        private void CreateCommandFileFromHtmlReport(string reportPath, string dataFolderPart, string reportFolder)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.Load(@reportPath);
            HtmlNodeCollection pnodes = doc.DocumentNode.SelectNodes("//p");
            HtmlNodeCollection h2nodes = doc.DocumentNode.SelectNodes("//h2");
            IEnumerable<HtmlNode> nodes = pnodes.Union(h2nodes);

            List<string> relevantLines = new List<string>();
            foreach (HtmlNode node in nodes)
            {
                string line = node.InnerHtml;
                if (line.Contains("Analysis of data:"))
                {
                    string commandLine = "rps" + line.Split(':')[1] + " "
                                         + (CellexalUser.UserSpecificFolder + "/" + dataFolderPart).FixFilePath();
                    relevantLines.Add(commandLine);
                }
                else if (line.Contains("Network from Saved Selection"))
                {
                    string[] lineWords = line.Split(null);
                    int selectionIndex = int.Parse(lineWords[lineWords.Length - 1]) - 1;
                    string commandLine = "rsf " + (CellexalUser.UserSpecificFolder +
                                                   "/" + dataFolderPart +
                                                   "/" + reportFolder +
                                                   "/selection" + selectionIndex).FixFilePath() + ".txt " +
                                         "true";
                    if (relevantLines.Contains(commandLine)) continue;
                    relevantLines.Add(commandLine);
                    relevantLines.Add("confirmselection");
                    relevantLines.Add("gn");
                }
                else if (line.Contains("Heatmap From Saved Selection"))
                {
                    string[] lineWords = line.Split(null);
                    int selectionIndex = int.Parse(lineWords[lineWords.Length - 1]) - 1;
                    string commandLine = "rsf " + (CellexalUser.UserSpecificFolder +
                                                   "/" + dataFolderPart +
                                                   "/" + reportFolder +
                                                   "/selection" + selectionIndex).FixFilePath() + ".txt " +
                                         "true";
                    ;
                    if (relevantLines.Contains(commandLine)) continue;
                    relevantLines.Add(commandLine);
                    relevantLines.Add("confirmselection");
                    relevantLines.Add("gh");
                }
            }

            string savePath =
                (CellexalUser.UserSpecificFolder + "/" + dataFolderPart + "/" + reportFolder + "/command_file.txt")
                .FixFilePath();
            CompileCommandFile(relevantLines, savePath);
        }

        private static void CompileCommandFile(List<string> linesToCompile, string path)
        {
            using (StreamWriter streamWriter = new StreamWriter(path))
            {
                foreach (string line in linesToCompile)
                {
                    streamWriter.WriteLine(line);
                }
            }
        }
    }
}