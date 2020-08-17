using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Linq;
using CellexalVR.AnalysisObjects;
using SQLiter;
using CellexalVR.General;
using Newtonsoft.Json;


namespace CellexalVR.AnalysisLogic.H5reader
{
    /// <summary>
    /// Class that handles the reading of hdf5 files.
    /// </summary>
    public class H5Reader : MonoBehaviour
    {
        
        private Process p;
        private StreamWriter writer;
        private StreamReader reader;
        private Dictionary<string, int> chromeLengths;
        private Dictionary<string, int> cellname2index;
        Dictionary<string, int> genename2index;
        
        //Genenames are all saved in uppercase
        public string[] index2genename;
        public string[] index2cellname;
        public bool busy;
        public ArrayList _expressionResult;
        public float[] _coordResult;
        public float[][] _matrixCoordResult;
        public float[] _velResult;
        public string[] _attrResult;
        public List<string> attributes;

        private string filePath;
        public string identifier;
        private Dictionary<string, string> conf;
        private string conditions;

        private List<string> projections;
        private List<string> velocities;
        //We save all projections and velocities in uppercase, Attributes can be whatever

        private bool ascii = false;
        private bool sparse = false;
        private bool geneXcell = true;


        private float LowestExpression { get; set; }
        private float HighestExpression { get; set; }

        private enum FileTypes
        {
            anndata = 0,
            loom = 1
        }

        private FileTypes fileType;
        private ReferenceManager referenceManager;
        /// <summary>
        /// H5reader
        /// </summary>
        /// <param name="path">filename in the Data folder</param>
        public void SetConf(string path, Dictionary<string, string> recievedConfig)
        {
            projections = new List<string>();
            velocities = new List<string>();
            attributes = new List<string>();
            string fullPath = Directory.GetCurrentDirectory() + "\\Data\\" + path;

            string[] files = Directory.GetFiles(fullPath);
            string configFile = "";

            foreach (string s in files)
            {
                if (s.EndsWith(".conf"))
                    configFile = s;
                else if (s.EndsWith(".loom")){
                    filePath = s;
                    fileType = FileTypes.loom;
                }else if( s.EndsWith(".h5ad")){
                    filePath = s;
                    fileType = FileTypes.anndata;
                    conf = new Dictionary<string, string>();
                    return;
                }

            }


            if(recievedConfig != null){
                conf = recievedConfig;
            } else {
                conf = JsonConvert.DeserializeObject<Dictionary<string,string>>(File.ReadAllText(configFile));
            }

            foreach (KeyValuePair<string, string> kvp in conf)
            {
                if (kvp.Key == "sparse")
                    sparse = bool.Parse(kvp.Value);
                else if (kvp.Key == "gene_x_cell")
                    geneXcell = bool.Parse(kvp.Value);
                else if (kvp.Key == "ascii")
                    ascii = bool.Parse(kvp.Value);

                if (kvp.Key.StartsWith("X") || kvp.Key.StartsWith("Y") || kvp.Key.StartsWith("Z"))
                {
                    string proj = kvp.Key.Split(new[] { '_' }, 2)[1];

                    if (!projections.Contains(proj.ToUpper()))
                        projections.Add(proj.ToUpper());
                }
                if (kvp.Key.StartsWith("vel"))
                {
                    string vel = kvp.Key.Split(new[] { '_' }, 2)[1];
                    if (!velocities.Contains(vel.ToUpper()))
                        velocities.Add(vel.ToUpper());
                }
                if (kvp.Key.StartsWith("attr")) { 
                    string attr = kvp.Key.Split(new[] { '_' }, 2)[1];
                    if (!attributes.Contains(attr))
                            attributes.Add(attr);
                }
            }

            if(recievedConfig == null)
                referenceManager.multiuserMessageSender.SendMessageReadH5Config(path, conf);
            
        }

        private void OnValidate()
        {
            if (gameObject.scene.IsValid())
            {
                referenceManager = GameObject.Find("InputReader").GetComponent<ReferenceManager>();
            }
        }


        /// <summary>
        /// Coroutine for connecting to the file
        /// </summary>
        /// <returns>All genenames and cellnames from the file are saved in the class</returns>
        private IEnumerator ConnectToFile()
        {
            busy = true;
            p = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardInput = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.WindowStyle = ProcessWindowStyle.Minimized;
            startInfo.CreateNoWindow = false;

            startInfo.FileName = "py.exe";

            string file_name = filePath;
            startInfo.Arguments = "python/ann.py " + file_name;
            p.StartInfo = startInfo;
            Thread t = new Thread(
                () =>
                {
                    bool start = p.Start();
                });
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            t.Start();
            while (t.ThreadState != System.Threading.ThreadState.Stopped)
                yield return null;

            writer = p.StandardInput;

            yield return null;

            reader = p.StandardOutput;

            yield return null;

            var watch = Stopwatch.StartNew();
            string line = "";
            if (conf.ContainsKey("custom_cellnames"))
            {
                line = conf["custom_cellnames"];
            }
            else if(fileType == FileTypes.loom)
            {
                if (ascii)
                    line = "[s.decode('UTF-8') for s in f['" + conf["cellnames"] + "'][:].tolist()]";
                else
                    line = "f['" + conf["cellnames"] + "'][:].tolist()";
            }
            else if(fileType == FileTypes.anndata)
            {
                line = "f.obs.index.tolist()";
            }
            writer.WriteLine(line);


            while (reader.Peek() == 0)
                yield return null;

            string output = reader.ReadLine();
            index2cellname = JsonConvert.DeserializeObject<string[]>(output);
            cellname2index = new Dictionary<string, int>();
            for (int i = 0; i < index2cellname.Length; i++)
            {
                index2cellname[i] = index2cellname[i].Replace(" ", "").Replace("'", "");

                if (!cellname2index.ContainsKey(index2cellname[i]))
                    cellname2index.Add(index2cellname[i], i);

                if (i == 0 || i == 1 || i == index2cellname.Length - 1)
                    UnityEngine.Debug.Log(index2cellname[i]);

                if (i % (index2cellname.Length / 3) == 0)
                    yield return null;
            }

            if (conf.ContainsKey("custom_genenames"))
            {
                line = conf["custom_genenames"];
            }
            else if(fileType == FileTypes.loom)
            {
                if (ascii)
                    line = "[s.decode('UTF-8') for s in f['" + conf["genenames"] + "'][:].tolist()]";
                else
                    line = "f['" + conf["genenames"] + "'][:].tolist()";
            }
            else if(fileType == FileTypes.anndata)
            {
                line = "f.var.index.tolist()";
            }
            writer.WriteLine(line);


            while (reader.Peek() == 0)
                yield return null;
            output = reader.ReadLine();
            index2genename = JsonConvert.DeserializeObject<string[]>(output);
            genename2index = new Dictionary<string, int>();
            for (int i = 0; i < index2genename.Length; i++)
            {
                index2genename[i] = index2genename[i].Replace(" ", "").Replace("'", "").ToUpper();

                if (i == 0 || i == 1 || i == index2genename.Length - 1)
                    UnityEngine.Debug.Log(index2genename[i]);

                if (!genename2index.ContainsKey(index2genename[i]))
                    genename2index.Add(index2genename[i], i);

                if (i % (index2genename.Length / 3) == 0)
                    yield return null;
            }


            //Special anndata stuff
            if(fileType == FileTypes.anndata){
                line = "list(f.obsm.keys())";
                writer.WriteLine(line);

                while (reader.Peek() == 0)
                    yield return null;

                output = reader.ReadLine();
                projections = JsonConvert.DeserializeObject<List<string>>(output);

                line = "list(f.obs.keys())";
                writer.WriteLine(line);
                while (reader.Peek() == 0)
                    yield return null;

                output = reader.ReadLine();

                //THERE ARE TOO MANY ATTRIBUTES AT THIS TIME
                List<string> attr = JsonConvert.DeserializeObject<List<string>>(output);
                attr.RemoveRange(1, attr.Count - 1);
                attributes = attr;

            }


            watch.Stop();

            UnityEngine.Debug.Log("H5reader booted and read all names in " + watch.ElapsedMilliseconds + " ms");
            busy = false;


            UnityEngine.Debug.Log("nbr of cells: " + index2cellname.Length + " with distinct names: " +
                                index2cellname.Distinct().Count());


        
        }


        public void CloseConnection()
        {
            print("Closing connection");
            UnityEngine.Debug.Log("Closing connection loom");
            p.CloseMainWindow();

            p.Close();
        }

        /// <summary>
        /// Get 3D coordinates from file
        /// </summary>
        /// <param name="projection">The graph type, (umap or phate)</param>
        /// <returns>Coroutine, use _coordResult</returns>
        public IEnumerator GetCoords(string projection)
        {
            string output;
            busy = true;
            var watch = Stopwatch.StartNew();
            if(fileType == FileTypes.loom){
                projection = projection.ToUpper();
                

                if (conf.ContainsKey("Y_" + projection))
                {
                    conditions = "2D_sep";
                    writer.WriteLine("f['" + conf["X_" + projection] + "'][:].tolist()");
                    while (reader.Peek() == 0)
                        yield return null;


                    output = reader.ReadLine();
                    float[] Xcoords = JsonConvert.DeserializeObject<float[]>(output);


                    writer.WriteLine("f['" + conf["Y_" + projection] + "'][:].tolist()");
                    while (reader.Peek() == 0)
                        yield return null;

                    output = reader.ReadLine();
                    float[] Ycoords = JsonConvert.DeserializeObject<float[]>(output);

                    _coordResult = Xcoords.Concat(Ycoords).ToArray();

                    if (conf.ContainsKey("Z_" + projection))
                    {
                        conditions = "3D_sep";

                        writer.WriteLine("f['" + conf["Z_" + projection] + "'][:].tolist()");
                        while (reader.Peek() == 0)
                            yield return null;

                        output = reader.ReadLine();

                        _coordResult = _coordResult.Concat(JsonConvert.DeserializeObject<float[]>(output)).ToArray();
                    }
                }
                else
                {
                    writer.WriteLine("f['" + conf["X_" + projection] + "'][:,:].tolist()");

                    while (reader.Peek() == 0)
                        yield return null;

                    output = reader.ReadLine();
                    float[] coords = JsonConvert.DeserializeObject<float[]>(output);

                    _coordResult = coords;
                }


                watch.Stop();
                UnityEngine.Debug.Log("Reading all coords: " + watch.ElapsedMilliseconds);
                busy = false;
            } else if (fileType == FileTypes.anndata) {
                string line = "f.obsm['" + projection + "'].tolist()";
                writer.WriteLine(line);
                while (reader.Peek() == 0)
                    yield return null;

                output = reader.ReadLine();

                float[][] coords = JsonConvert.DeserializeObject<float[][]>(output);
                conditions = "2D";
                _matrixCoordResult = coords;

                watch.Stop();
                UnityEngine.Debug.Log("Reading all coords: " + watch.ElapsedMilliseconds);
                busy = false;
            }
            
        }

        /// <summary>
        /// Get the cellattributes from the file
        /// </summary>
        /// <returns>_attrResult</returns>
        public IEnumerator GetAttributes(string attribute)
        {
            busy = true;
            string line = "";
            if(fileType == FileTypes.loom)
            {
                if (ascii)
                    line = "[s.decode('UTF-8') for s in " + "f['" + conf["attr_" + attribute] + "[:].tolist()]";
                else
                    line = "f['" + conf["attr_" + attribute] + "'][:].tolist()";
            } 
            else if(fileType == FileTypes.anndata) 
            {
                line = "f.obs['" + attribute + "'].tolist()";
            }

            writer.WriteLine(line);
            while (reader.Peek() == 0)
                yield return null;
            string output = reader.ReadLine();
            if (output != null) _attrResult = JsonConvert.DeserializeObject<string[]>(output);
            busy = false;
        }

        /// <summary>
        /// Get the phate velocities from the file
        /// </summary>
        /// <returns>_velResult</returns>
        public IEnumerator GetVelocites(string graph)
        {
            graph = graph.ToUpper();
            busy = true;
            var watch = Stopwatch.StartNew();
            string output;
            if (conf.ContainsKey("velX_" + graph))
            {
                conditions = "2D_sep";

                writer.WriteLine("f['" + conf["velX_" + graph] + "'][:,:].tolist()");
                while (reader.Peek() == 0)
                    yield return null;

                output = reader.ReadLine();
                float[] Xvel = JsonConvert.DeserializeObject<float[]>(output);

                writer.WriteLine("f['" + conf["velY_" + graph] + "'][:].tolist()");
                while (reader.Peek() == 0)
                    yield return null;

                output = reader.ReadLine();
                float[] Yvel =  JsonConvert.DeserializeObject<float[]>(output);

                _velResult = Xvel.Concat(Yvel).ToArray();
            }
            else
            {
                writer.WriteLine("f['" + conf["vel_" + graph] + "'][:,:].tolist()");
                while (reader.Peek() == 0)
                    yield return null;


                output = reader.ReadLine();
                _velResult = JsonConvert.DeserializeObject<float[]>(output);
            }


            watch.Stop();
            UnityEngine.Debug.Log("Read all velocities for " + graph + " in " + watch.ElapsedMilliseconds);
            busy = false;
        }

        /// <summary>
        /// Reads expressions of gene on all cells, returns list of CellExpressionPair
        /// </summary>
        /// <param name="geneName">gene name</param>
        /// <param name="coloringMethod">Either same number of cells in each color bin or each color bin are of same range.</param>
        /// <returns>_result</returns>
        public IEnumerator ColorByGene(string geneName, GraphManager.GeneExpressionColoringMethods coloringMethod)
        {
            busy = true;
            _expressionResult = new ArrayList();
            int geneindex = genename2index[geneName.ToUpper()];
            string line = "";
            if(fileType == FileTypes.loom)
            {
                if (geneXcell)
                {
                    if (sparse)
                        line = "f['" + conf["cellexpr"] + "'][" + geneindex + ",:].data.tolist()";
                    else
                        line = "f['" + conf["cellexpr"] + "'][" + geneindex + ",:][" + "f['" + conf["cellexpr"] + "'][" + geneindex +
                                            ",:].nonzero()].tolist()";
                }
                else
                {
                    if (sparse)
                        line = "f['" + conf["cellexpr"] + "'][:," + geneindex + "].data.tolist()";
                    else
                        line = "f['" + conf["cellexpr"] + "'][:," + geneindex + "][" + "f['" + conf["cellexpr"] + "'][:," +
                                            geneindex + "].nonzero()].tolist()";
                }
            } 
            else if (fileType == FileTypes.anndata) 
            {
                line = "f.X[" + geneindex + ",:].data.tolist()";
            }

            writer.WriteLine(line);

            while (reader.Peek() == 0)
                yield return null;

            string output = reader.ReadLine();

            float[] expressions = JsonConvert.DeserializeObject<float[]>(output);
            if(fileType == FileTypes.loom)
            {
                if (geneXcell)
                    line = "f['" + conf["cellexpr"] + "'][" + geneindex + ",:].nonzero()[0].tolist()";
                else
                    line = "f['" + conf["cellexpr"] + "'][:," + geneindex + "].nonzero()[0].tolist()";
            }
            else if(fileType == FileTypes.anndata)
            {
                line = "f.X["+geneindex+",:].nonzero()[1].tolist()";
            }

            writer.WriteLine(line);

            while (reader.Peek() == 0)
                yield return null;

            output = reader.ReadLine();


            int[] indices = JsonConvert.DeserializeObject<int[]>(output);

            LowestExpression = float.MaxValue;
            HighestExpression = float.MinValue;
            if (coloringMethod == GraphManager.GeneExpressionColoringMethods.EqualExpressionRanges)
            {
                // put results in equally sized buckets
                for (int i = 0; i < expressions.Length; i++)
                {
                    float expr = expressions[i];
                    if (expr > HighestExpression)
                    {
                        HighestExpression = expr;
                    }

                    if (expr < LowestExpression)
                    {
                        LowestExpression = expr;
                    }

                    try
                    {
                        _expressionResult.Add(new CellExpressionPair(index2cellname[indices[i]], expr, -1));
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.Log(indices[i]);
                        break;
                    }
                }

                if (Math.Abs(HighestExpression - LowestExpression) < 0.001)
                {
                    HighestExpression += 1;
                }

                HighestExpression *= 1.0001f;
                float binSize = (HighestExpression - LowestExpression) /
                                CellexalConfig.Config.GraphNumberOfExpressionColors;

                foreach (CellExpressionPair pair in _expressionResult)
                {
                    pair.Color = (int) ((pair.Expression - LowestExpression) / binSize);
                }

                UnityEngine.Debug.Log(HighestExpression);
            }
            else
            {
                List<CellExpressionPair> result = new List<CellExpressionPair>();
                LowestExpression = float.MaxValue;
                HighestExpression = float.MinValue;
                // put the same number of results in each bucket, ordered
                for (int i = 0; i < expressions.Length; i++)
                {
                    CellExpressionPair newPair = new CellExpressionPair(index2cellname[indices[i]], expressions[i], -1);
                    result.Add(newPair);
                    float expr = newPair.Expression;
                    if (expr > HighestExpression)
                    {
                        HighestExpression = expr;
                    }

                    if (expr < LowestExpression)
                    {
                        LowestExpression = expr;
                    }
                }

                if (HighestExpression == LowestExpression)
                {
                    HighestExpression += 1;
                }

                // sort the list based on gene expressions
                result.Sort();

                HighestExpression *= 1.0001f;
                int binsize = result.Count / CellexalConfig.Config.GraphNumberOfExpressionColors;
                for (int j = 0; j < result.Count; ++j)
                {
                        result[j].Color = j;
                }

                _expressionResult.AddRange(result);
            }

            busy = false;
        }

        /// <summary>
        /// H5 Coroutine to create graphs.
        /// </summary>
        /// <param name="path"> The path to the file. </param>
        /// <param name="type"></param>
        /// <param name="server"></param>
        public IEnumerator H5ReadGraphs(string path,
            GraphGenerator.GraphType type = GraphGenerator.GraphType.MDS,
            bool server = true)
        {
            if (!referenceManager.loaderController.loaderMovedDown)
            {
                referenceManager.loaderController.loaderMovedDown = true;
                referenceManager.loaderController.MoveLoader(new Vector3(0f, -2f, 0f), 2f);
            }

            StartCoroutine(ConnectToFile());
            while (busy)
                yield return null;

            //int statusId = status.AddStatus("Reading folder " + path);
            //int statusIdHUD = statusDisplayHUD.AddStatus("Reading folder " + path);
            //int statusIdFar = statusDisplayFar.AddStatus("Reading folder " + path);
            //  Read each .mds file
            //  The file format should be
            //  cell_id  axis_name1   axis_name2   axis_name3
            //  CELLNAME_1 X_COORD Y_COORD Z_COORD
            //  CELLNAME_2 X_COORD Y_COORD Z_COORD
            //  ...
            const float maximumDeltaTime = 0.05f; // 20 fps
            int maximumItemsPerFrame = CellexalConfig.Config.GraphLoadingCellsPerFrameStartCount;
            int itemsThisFrame = 0;

            int totalNbrOfCells = 0;
            foreach (string proj in projections)
            {
                while (referenceManager.graphGenerator.isCreating)
                {
                    yield return null;
                }

                Graph combGraph = referenceManager.graphGenerator.CreateGraph(type);
                if (velocities.Contains(proj))
                {
                    referenceManager.graphManager.velocityFiles.Add(proj);
                    combGraph.hasVelocityInfo = true;
                }

                // more_cells newGraph.GetComponent<GraphInteract>().isGrabbable = false;
                // file will be the full file name e.g C:\...\graph1.mds
                // good programming habits have left us with a nice mix of forward and backward slashes
                //combGraph.DirectoryName = regexResult[regexResult.Length - 2];
                if (type.Equals(GraphGenerator.GraphType.MDS))
                {
                    combGraph.GraphName = proj.ToUpper();
                    //combGraph.FolderName = regexResult[regexResult.Length - 2];
                }
                else
                {
                    string name = "";
                    foreach (string s in referenceManager.newGraphFromMarkers.markers)
                    {
                        name += s + " - ";
                    }

                    combGraph.GraphNumber = referenceManager.inputReader.facsGraphCounter;
                    combGraph.GraphName = name;
                }

                //combGraph.gameObject.name = combGraph.GraphName;
                //FileStream mdsFileStream = new FileStream(file, FileMode.Open);
                //image1 = new Bitmap(400, 400);
                //System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(image1);
                //int i, j;
                string[] axes = new string[3];
                while (busy)
                    yield return null;
                StartCoroutine(GetCoords(proj));
                while (busy)
                    yield return null;
                float[] coords = _coordResult;
                
                string[] cellNames = index2cellname;
                combGraph.axisNames = new string[] {"x", "y", "z"};
                int count = 0;
                for (int j = 0; j < cellNames.Length; j++)
                {
                    string cellName = cellNames[j];
                    float x, y, z;
                    switch (conditions)
                    {
                        case "2D_sep":
                            x = coords[j];
                            y = coords[j + cellNames.Length];
                            z = j * 0.00001f; //summertwerk, should scale after maxcoord
                            break;
                        case "3D_sep":
                            x = coords[j];
                            y = coords[j + cellNames.Length];
                            z = coords[j + 2*cellNames.Length];
                            break;
                        case "2D":
                            x = _matrixCoordResult[j][0];
                            y = _matrixCoordResult[j][1];
                            z = j * 0.00001f; //summertwerk, should scale after maxcoord
                            break;
                        default:
                            x = coords[j * 3];
                            y = coords[j * 3 + 1];
                            z = coords[j * 3 + 2];
                            break;
                    }

                    Cell cell = referenceManager.cellManager.AddCell(cellName);
                    referenceManager.graphGenerator.AddGraphPoint(cell, x, y, z);
                    totalNbrOfCells++;
                    count++;

                    if (count <= maximumItemsPerFrame) continue;
                    yield return null;
                    count = 0;
                    float lastFrame = Time.deltaTime;
                    if (lastFrame < maximumDeltaTime)
                    {
                        // we had some time over last frame
                        maximumItemsPerFrame += CellexalConfig.Config.GraphLoadingCellsPerFrameIncrement;
                    }
                    else if (lastFrame > maximumDeltaTime && maximumItemsPerFrame >
                        CellexalConfig.Config.GraphLoadingCellsPerFrameIncrement * 2)
                    {
                        // we took too much time last frame
                        maximumItemsPerFrame -= CellexalConfig.Config.GraphLoadingCellsPerFrameIncrement;
                    }
                }

                combGraph.SetInfoText();

                // Add axes in bottom corner of graph and scale points differently
                referenceManager.graphGenerator.SliceClustering();
                referenceManager.graphGenerator.AddAxes(combGraph, axes);
                referenceManager.graphManager.Graphs.Add(combGraph);
            }

            if (attributes.Count > 0)
            {
                referenceManager.inputReader.attributeReader =
                    referenceManager.inputReader.gameObject.AddComponent<AttributeReader>();
                referenceManager.inputReader.attributeReader.referenceManager = referenceManager;
                StartCoroutine(referenceManager.inputReader.attributeReader.H5ReadAttributeFilesCoroutine(this));
                while (!referenceManager.inputReader.attributeFileRead)
                    yield return null;
            }
            /*
            if (type.Equals(GraphGenerator.GraphType.MDS))
            {
                StartCoroutine(ReadAttributeFilesCoroutine(path));
                while (!attributeFileRead)
                    yield return null;
                ReadFacsFiles(path, totalNbrOfCells);
                ReadFilterFiles(CellexalUser.UserSpecificFolder);
            }
            */
            //loaderController.loaderMovedDown = true;

            //status.UpdateStatus(statusId, "Reading index.facs file");
            //statusDisplayHUD.UpdateStatus(statusIdHUD, "Reading index.facs file");
            //statusDisplayFar.UpdateStatus(statusIdFar, "Reading index.facs file");
            //flashGenesMenu.CreateTabs(path);
            //status.RemoveStatus(statusId);
            //statusDisplayHUD.RemoveStatus(statusIdHUD);
            //statusDisplayFar.RemoveStatus(statusIdFar);
            if (server)
            {
                StartCoroutine(referenceManager.inputReader.StartServer("main"));
                //StartCoroutine(StartServer("gene"));
            }

            while (referenceManager.graphGenerator.isCreating)
            {
                yield return null;
            }

            CellexalEvents.GraphsLoaded.Invoke();
        }
    }
}