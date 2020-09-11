//----------------------------------------------
// SQLiter
// Copyright � 2014 OuijaPaw Games LLC
//----------------------------------------------

using UnityEngine;
using Mono.Data.SqliteClient;
using System.Collections;
using System.Threading;
using System.Text;
using System.Collections.Generic;
using System;
using CellexalVR.General;
using CellexalVR.AnalysisObjects;
using SQLiter;
using CellexalVR.AnalysisLogic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;

namespace SQLiter
{
    /// <summary>
    /// The idea is that here is a bunch of the basics on using SQLite
    /// Nothing is some advanced course on doing joins and unions and trying to make your infinitely normalized schema work
    /// SQLite is simple.  Very simple.  
    /// Pros:
    /// - Very simple to use
    /// - Very small memory footprint
    /// 
    /// Cons:
    /// - It is a flat file database.  You can change the settings to make it run completely in memory, which will make it even
    /// faster; however, you cannot have separate threads interact with it -ever-, so if you plan on using SQLite for any sort
    /// of multiplayer game and want different Unity instances to interact/read data... they absolutely cannot.
    /// - Doesn't offer as many bells and whistles as other DB systems
    /// - It is awfully slow.  I mean dreadfully slow.  I know "slow" is a relative term, but unless the DB is all in memory, every
    /// time you do a write/delete/update/replace, it has to write to a physical file - since SQLite is just a file based DB.
    /// If you ever do a write and then need to read it shortly after, like .5 to 1 second after... there's a chance it hasn't been
    /// updated yet... and this is local.  So, just make sure you use a coroutine or whatever to make sure data is written before
    /// using it.
    /// 
    /// SQLite is nice for small games, high scores, simple saved, etc.  It is not very secure and not very fast, but it's cheap,
    /// simple, and useful at times.
    /// 
    /// Here are some starting tools and information.  Go explore.
    /// </summary>
    public class SQLite : MonoBehaviour
    {
        public static SQLite Instance = null;
        public bool DebugMode = false;
        public bool QueryRunning { get; private set; }
        public bool _databaseOK = false;

        public ReferenceManager referenceManager;

        private CellManager cellManager;
        private InputReader inputReader;
        //private StatusDisplay status;
        // Location of database - this will be set during Awake as to stop Unity 5.4 error regarding initialization before scene is set
        // file should show up in the Unity inspector after a few seconds of running it the first time
        private static string _sqlDBLocation = "";

        /// <summary>
        /// DB objects
        /// </summary>
        private IDbConnection _connection = null;
        private IDbCommand _command = null;
        private IDataReader _reader = null;

        private string _sqlString;
        [HideInInspector]
        public ArrayList _result = new ArrayList();
        public float LowestExpression { get; private set; }
        public float HighestExpression { get; private set; }


        private void OnValidate()
        {
            if (gameObject.scene.IsValid())
            {
                referenceManager = GameObject.Find("InputReader").GetComponent<ReferenceManager>();
            }
        }

        /// <summary>
        /// Awake will initialize the connection.  
        /// RunAsyncInit is just for show.  You can do the normal SQLiteInit to ensure that it is
        /// initialized during the Awake() phase and everything is ready during the Start() phase
        /// </summary>
        void Awake()
        {
            if (DebugMode)
                Debug.Log("--- Awake ---");

            // here is where we set the file location
            // ------------ IMPORTANT ---------
            // - during builds, this is located in the project root - same level as Assets/Library/obj/ProjectSettings
            // - during runtime (Windows at least), this is located in the SAME directory as the executable
            // you can play around with the path if you like, but build-vs-run locations need to be taken into account
        }

        void Start()
        {
            if (DebugMode)
                Debug.Log("--- Start ---");
            cellManager = referenceManager.cellManager;
            inputReader = referenceManager.inputReader;
            //status = referenceManager.statusDisplay;
            // just for testing, comment/uncomment to play with it
            // note that it MUST be invoked after SQLite has initialized, 2-3 seconds later usually.  1 second is cutting it too close
            // Invoke("Test", 3);
        }

        /// <summary>
        /// Uncomment if you want to see the time it takes to do things
        /// </summary>
        //void Update()
        //{
        //    Debug.Log(Time.time);
        //}

        /// <summary>
        /// Clean up SQLite Connections, anything else
        /// </summary>
        void OnDestroy()
        {
            SQLiteClose();
        }

        /// <summary>
        /// Example using the Loom to run an asynchronous method on another thread so SQLite lookups
        /// do not block the main Unity thread
        /// </summary>
        public void RunAsyncInit()
        {
            LoomManager.Loom.QueueOnMainThread(() =>
            {
                SQLiteInit();
            });
        }

        public void InitDatabase(string path)
        {
            _sqlDBLocation = "URI=file:" + path;

            //Debug.Log(_sqlDBLocation);
            Instance = this;
            SQLiteInit();
        }

        /// <summary>
        /// Basic initialization of SQLite
        /// </summary>
        private void SQLiteInit()
        {
            Debug.Log("SQLiter - Opening SQLite Connection");
            _connection = new SqliteConnection(_sqlDBLocation);
            _command = _connection.CreateCommand();
            _connection.Open();

            // WAL = write ahead logging, very huge speed increase
            _command.CommandText = "PRAGMA journal_mode = WAL;";
            _command.ExecuteNonQuery();

            // journal mode = look it up on google, I don't remember
            _command.CommandText = "PRAGMA journal_mode";
            _reader = _command.ExecuteReader();
            if (DebugMode && _reader.Read())
                Debug.Log("SQLiter - WAL value is: " + _reader.GetString(0));
            _reader.Close();

            // more speed increases
            _command.CommandText = "PRAGMA synchronous = OFF";
            _command.ExecuteNonQuery();

            // and some more
            _command.CommandText = "PRAGMA synchronous";
            _reader = _command.ExecuteReader();
            if (DebugMode && _reader.Read())
                Debug.Log("SQLiter - synchronous value is: " + _reader.GetInt32(0));
            _reader.Close();


            //_reader.GetSchemaTable();
            //// here we check if the table you want to use exists or not.  If it doesn't exist we create it.
            //_command.CommandText = "SELECT * FROM data LIMIT 1";
            //_reader = _command.ExecuteReader();
            //if (!_reader.Read())
            //{
            //    Debug.Log("SQLiter - Could not find SQLite table " + SQL_TABLE_NAME);
            //    // _createNewTavle = true;
            //}
            //_reader.Close();

            // create new table if it wasn't found
            //if (_createNewTavle)
            //{
            //    Debug.Log("SQLiter - Creating new SQLite table " + SQL_TABLE_NAME);

            //    // insurance policy, drop table
            //    _command.CommandText = "DROP TABLE IF EXISTS " + SQL_TABLE_NAME;
            //    _command.ExecuteNonQuery();

            //    // create new - SQLite recommendation is to drop table, not clear it
            //    _sqlString = "CREATE TABLE IF NOT EXISTS " + SQL_TABLE_NAME + " (" +
            //        COL_NAME + " TEXT UNIQUE, " +
            //        COL_RACE + " INTEGER, " +
            //        COL_CLASS + " INTEGER, " +
            //        COL_GOLD + " INTEGER, " +
            //        COL_LOGIN_LAST + " INTEGER, " +
            //        COL_LEVEL + " INTEGER, " +
            //        COL_XP + " INTEGER)";
            //    _command.CommandText = _sqlString;
            //    _command.ExecuteNonQuery();
            //}
            //else
            //{
            //    if (DebugMode)
            //        Debug.Log("SQLiter - SQLite table " + SQL_TABLE_NAME + " was found");
            //}

            // close connection
            _connection.Close();
        }

        /// <summary>
        /// Validates the database by making sure that the correct tabels exists.
        /// </summary>
        public IEnumerator ValidateDatabaseCoroutine()
        {
            string query = "select count(*) from sqlite_master where type='table' and name in ('genes', 'cells', 'datavalues');";
            Thread t = new Thread(() => QueryThread(query));
            t.Start();
            while (t.IsAlive)
            {
                yield return null;
            }

            _reader.Read();
            int nTables = _reader.GetInt32(0);
            if (nTables == 3)
            {
                _databaseOK = true;
            }
            QueryRunning = false;
            CellexalEvents.CommandFinished.Invoke(true);
        }

        #region Query

        /// <summary>
        /// Different modes to sort genes.
        /// </summary>
        public enum QueryTopGenesRankingMode { Mean, TTest }

        /// <summary>
        /// Queries the database for all gene expressions and sorts them based on the difference in expression between two chosen groups.
        /// </summary>
        /// <param name="mode">The mode to sort the gene expressinos by.</param>
        public void QueryTopGenes(QueryTopGenesRankingMode mode)
        {
            var list = referenceManager.selectionManager.GetLastSelection();
            if (list.Count < 2)
            {
                CellexalLog.Log("WARNING: Not querying for genes because list of cells is too short.");
                return;
            }
            List<string> cellNames1 = new List<string>();
            List<string> cellNames2 = new List<string>();
            int group1 = list[0].Group;
            foreach (Graph.GraphPoint gp in list)
            {
                if (gp.Group == group1)
                {
                    cellNames1.Add(gp.Label);
                }
                else
                {
                    cellNames2.Add(gp.Label);
                }
            }
            CellexalLog.Log("Querying database for all gene expressions in " + (cellNames1.Count + cellNames2.Count) + " cells");
            QueryRunning = true;
            StartCoroutine(QueryTopGenesCoroutine(cellNames1.ToArray(), cellNames2.ToArray(), mode));
        }

        private IEnumerator QueryTopGenesCoroutine(string[] cellNames1, string[] cellNames2, QueryTopGenesRankingMode mode)
        {
            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
            _result.Clear();
            // Create the two lists of cell names
            StringBuilder builder = new StringBuilder();

            int i = 0;
            for (; i < cellNames1.Length; ++i)
            {
                string cell = cellNames1[i];
                builder.Append("\"").Append(cell).Append("\"");
                if (i < cellNames1.Length - 1)
                {
                    builder.Append(", ");
                }
            }
            string cellNamesString1 = builder.ToString();

            builder = new StringBuilder();
            for (i = 0; i < cellNames2.Length; ++i)
            {
                string cell = cellNames2[i];
                builder.Append("\"").Append(cell).Append("\"");
                if (i < cellNames2.Length - 1)
                {
                    builder.Append(", ");
                }
            }
            string cellNamesString2 = builder.ToString();
            // query for list 1
            string query = "select gene_id, value from datavalues left join cells on datavalues.cell_id = cells.id where cname in (" + cellNamesString1 + ") order by gene_id";
            Thread t = new Thread(() => QueryThread(query));
            t.Start();
            while (t.IsAlive)
            {
                yield return null;
            }

            List<Pair<string, List<float>>> expressions1 = GetResultsTopGeneQuery();
            _result.Clear();
            // query for list 2
            query = "select gene_id, value from datavalues left join cells on datavalues.cell_id = cells.id where cname in (" + cellNamesString2 + ") order by gene_id";
            t = new Thread(() => QueryThread(query));
            t.Start();
            while (t.IsAlive)
            {
                yield return null;
            }

            List<Pair<string, List<float>>> expressions2 = GetResultsTopGeneQuery();

            // actual results will be in _result after these functions
            List<string> actualGeneIds = new List<string>(0);
            if (mode == QueryTopGenesRankingMode.Mean)
            {
                actualGeneIds = CalculateMeans(expressions1, expressions2, cellNames1.Length, cellNames2.Length);
            }
            else if (mode == QueryTopGenesRankingMode.TTest)
            {
                actualGeneIds = TTestLists(expressions1, expressions2, cellNames1.Length, cellNames2.Length);
            }

            // get the actual gene names
            builder = new StringBuilder();
            for (i = 0; i < actualGeneIds.Count; ++i)
            {
                string gene = actualGeneIds[i];
                builder.Append(gene);
                if (i < actualGeneIds.Count - 1)
                {
                    builder.Append(", ");
                }
            }
            string actualGeneIdsString = builder.ToString();

            query = "select id, gname from genes where id in (" + actualGeneIdsString + ") order by id";
            t = new Thread(() => QueryThread(query));
            t.Start();
            while (t.IsAlive)
            {
                yield return null;
            }

            //print(diffPairs.Count);
            i = 0;
            while (_reader.Read())
            {
                string geneName = _reader.GetString(1);
                ((Pair<string, float>)_result[i]).First = geneName;
                i++;
            }
            stopwatch.Stop();
            CellexalLog.Log("Finished querying for gene expressions in " + (cellNames1.Length + cellNames2.Length) + " cells in " + stopwatch.Elapsed.ToString());
            _reader.Close();
            _connection.Close();
            QueryRunning = false;
        }

        private List<Pair<string, List<float>>> GetResultsTopGeneQuery()
        {
            // int expressionsToAdd = 0;
            int prevGene = -1;
            int i = -1;
            List<Pair<string, List<float>>> expressions = new List<Pair<string, List<float>>>();
            while (_reader.Read())
            {
                int gene_id = _reader.GetInt32(0);
                float expr = _reader.GetFloat(1);

                if (prevGene != gene_id)
                {
                    expressions.Add(new Pair<string, List<float>>(gene_id.ToString(), new List<float>()));
                    prevGene = gene_id;
                    i++;
                }
                expressions[i].Second.Add(expr);
            }
            return expressions;
        }

        private List<string> CalculateMeans(List<Pair<string, List<float>>> expressions1, List<Pair<string, List<float>>> expressions2, int length1, int length2)
        {
            // calculate the difference in expressions
            _result = new ArrayList();
            List<string> actualGeneIds = new List<string>();
            int index1 = 0, index2 = 0;
            for (int i = 0; index1 < expressions1.Count - 1 && index2 < expressions2.Count - 1; ++i)
            {
                string geneId = "";
                float expr1 = 0, expr2 = 0;
                if (int.Parse(expressions1[index1].First) == i && index1 < expressions1.Count - 1)
                {
                    geneId = expressions1[index1].First;
                    expr1 = Mean(expressions1[index1].Second, length1);
                    index1++;
                }
                if (int.Parse(expressions2[index2].First) == i && index2 < expressions2.Count - 1)
                {
                    geneId = expressions2[index2].First;
                    expr2 = Mean(expressions2[index2].Second, length2);
                    index2++;
                }

                // only add genes that have a difference in expression > 0
                if (expr1 != 0 || expr2 != 0)
                {
                    float diffExpr = expr1 - expr2;
                    _result.Add(new Pair<string, float>(geneId, diffExpr));
                    actualGeneIds.Add(geneId);
                }
            }
            return actualGeneIds;
        }

        private float Mean(List<float> list, int length)
        {
            double mean = 0;
            for (int i = 0; i < list.Count; ++i)
            {
                mean += list[i];
            }
            mean /= length;
            return (float)mean;
        }

        /// <summary>
        /// Queries the database for the expression of a gene in some cells.
        /// </summary>
        /// <param name="gene">The gene to query for.</param>
        /// <param name="cells">The cells to query for.</param>
        internal void QueryGenesInCells(string gene, Cell[] cells, Action<SQLite> action = null)
        {
            string[] cellnameArray = cells.Select((c) => c.Label).ToArray();
            QueryGenesInCells(gene, cells, action);

        }

        /// <summary>
        /// Queries the database for the expression of a gene in some cells.
        /// </summary>
        /// <param name="gene">The gene to query for.</param>
        /// <param name="cells">The cells to query for.</param>
        internal void QueryGenesInCells(string gene, string[] cells, Action<SQLite> action = null)
        {
            QueryRunning = true;
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < cells.Length; ++i)
            {
                string cell = cells[i];
                builder.Append("\"").Append(cell).Append("\"");
                if (i < cells.Length - 1)
                {
                    builder.Append(", ");
                }
            }
            string cellNames = builder.ToString();
            StartCoroutine(QueryGeneInCellsCoroutine(gene, cellNames, action));
        }

        /// <summary>
        /// Queries the database for the expression of a gene in some cells.
        /// </summary>
        /// <param name="gene">The gene to query for.</param>
        /// <param name="cells">The graphpoints representing the cells to query for.</param>
        internal void QueryGenesInCells(string gene, List<string> cells)
        {
            QueryRunning = true;
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < cells.Count; ++i)
            {
                string cell = cells[i];
                builder.Append("\"").Append(cell).Append("\"");
                if (i < cells.Count - 1)
                {
                    builder.Append(", ");
                }
            }
            string cellNames = builder.ToString();
            StartCoroutine(QueryGeneInCellsCoroutine(gene, cellNames));
        }

        /// <summary>
        /// Queries the database for all gene names.
        /// </summary>
        internal void QueryGeneNames()
        {
            QueryRunning = true;
            StartCoroutine(QueryGenesCoroutine());
        }

        private IEnumerator QueryGenesCoroutine()
        {
            string query = "select gname from genes";
            Thread t = new Thread(() => QueryThread(query));
            t.Start();
            while (t.IsAlive)
            {
                yield return null;
            }

            _result.Clear();

            while (_reader.Read())
            {
                string geneName = _reader.GetString(0);
                _result.Add(geneName);
            }
            QueryRunning = false;

        }

        /// <summary>
        /// Queries the database for the lowest and highest expression. This fills <see cref="_reader"/> with <see cref="Tuple{string, float, float}"/> (gene_name, min, max)
        /// </summary>
        /// <param name="genes">An array of strings with the gene names.</param>
        internal void QueryGeneRanges(string[] genes)
        {
            QueryRunning = true;
            StartCoroutine(QueryGeneRangesCoroutine(genes));
        }

        private IEnumerator QueryGeneRangesCoroutine(string[] genes)
        {
            string genesString = string.Join("\", \"", genes);
            string query = "select gname, min(value), max(value) from datavalues inner join genes on datavalues.gene_id = genes.id where gname in (\"" + genesString + "\") group by gname";
            Thread t = new Thread(() => QueryThread(query));
            t.Start();
            while (t.IsAlive)
            {
                yield return null;
            }

            _result.Clear();

            while (_reader.Read())
            {
                string cellName = _reader.GetString(0);
                float min = _reader.GetFloat(1);
                float max = _reader.GetFloat(2);
                _result.Add(new Tuple<string, float, float>(cellName, min, max));
            }
            QueryRunning = false;
        }

        /// <summary>
        /// Queries the database for the expression of a gene in some cells. This function assumes that the string <paramref name="cells"/> is already formatted the way sqlite wants.
        /// </summary>
        /// <param name="gene">The gene to query for.</param>
        /// <param name="cells">The cells to query for. Cell names should be inside quotes (") and seperated with commas.</param>
        /// <example>
        /// <code>QueryGenesInCells("Gata1", "\"HSPC_001\", \"HSPC_002\", \"HSPC_003\"");</code>
        /// </example>
        internal void QueryGeneInCells(string gene, string cells)
        {
            QueryRunning = true;
            StartCoroutine(QueryGeneInCellsCoroutine(gene, cells));
        }

        private IEnumerator QueryGeneInCellsCoroutine(string gene, string cells, Action<SQLite> action = null)
        {
            string query = "select cname, value from datavalues left join cells on datavalues.cell_id = cells.id where cname in (" + cells + ") and gene_id = (select id from genes where gname = \"" + gene + "\")";
            Thread t = new Thread(() => QueryThread(query));
            t.Start();
            while (t.IsAlive)
            {
                yield return null;
            }

            _result.Clear();

            while (_reader.Read())
            {
                string cellName = _reader.GetString(0);
                float expression = _reader.GetFloat(1);
                _result.Add(new Tuple<string, float>(cellName, expression));
            }
            QueryRunning = false;
            if (action != null)
            {
                action.Invoke(this);
            }
        }

        /// <summary>
        /// Query the database for all expressions of multiple genes and multiple cells.
        /// This method puts many <see cref="Tuple{string, float}"/> with string and floats in the <see cref="ArrayList"/> <see cref="_result"/>.
        /// The first <see cref="Tuple{string, float}"/> is a gene name and the lowest expression for that gene, following that is the highest expression of that gene. 
        /// The next bunch of <see cref="Tuple{string, float}"/> are cell names and their respective expression of the gene in the previously mentioned Tuples.
        /// Then another two <see cref="Tuple{string, float}"/> follows with the next gene name, lowest and highest expression followed by cells and their expression and so on.
        /// Not all cells are put in the ArrayList, only the ones that were found in the database. This means that there is not the same amount of elements between each tuple which marks a new gene.
        /// </summary>
        /// <param name="genes">An array with the genes to query for.</param>
        /// <param name="cells">An array with the cells to query for.</param>
        internal void QueryGenesInCells(string[] genes, string[] cells)
        {
            QueryRunning = true;
            StartCoroutine(QueryGenesInCellsCoroutine(genes, cells));

        }

        private IEnumerator QueryGenesInCellsCoroutine(string[] genes, string[] cells)
        {
            if (cells.Length == 0 || genes.Length == 0)
            {
                yield break;
            }

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < cells.Length; ++i)
            {
                builder.Append("\"").Append(cells[i]).Append("\"");
                if (i < cells.Length - 1)
                {
                    builder.Append(", ");
                }
            }
            string cellNames = builder.ToString();

            builder.Clear();
            for (int i = 0; i < genes.Length; ++i)
            {
                string gene = genes[i];
                builder.Append("\"").Append(gene).Append("\"");
                if (i < genes.Length - 1)
                {
                    builder.Append(", ");
                }
            }
            string geneNames = builder.ToString();

            string query = "SELECT cname, gname, value " +
                           "FROM ((datavalues " +
                           "INNER JOIN cells ON datavalues.cell_id = cells.id) " +
                           "INNER JOIN genes ON datavalues.gene_id = genes.id) " +
                           "WHERE cname IN (" + cellNames + ") AND gname IN (" + geneNames + ") " +
                           "ORDER BY gname";
            Thread t = new Thread(() => QueryThread(query));
            t.Start();
            while (t.IsAlive)
            {
                yield return null;
            }

            _result.Clear();
            float lowestExpression = float.PositiveInfinity;
            float highestExpression = 0;
            string lastGeneId = "";
            int lowestGeneExpressionIndex = 0;
            int highestGeneExpressionIndex = 1;

            // reserve space for the first gene
            _result.Add(new Tuple<string, float>("", 0f));
            _result.Add(new Tuple<string, float>("", 0f));
            while (_reader.Read())
            {
                string cellName = _reader.GetString(0);
                string geneId = _reader.GetString(1);
                float expression = _reader.GetFloat(2);
                if (lastGeneId == "")
                {
                    lastGeneId = geneId;
                }
                if (geneId != lastGeneId)
                {
                    // replace the last reserved space with the highest expression we found
                    int numberOfCellsFound = _result.Count - highestGeneExpressionIndex - 1;
                    if (numberOfCellsFound < cells.Length)
                    {
                        // if one cell was not in the database, it has an expression of zero
                        lowestExpression = 0;
                    }
                    _result[lowestGeneExpressionIndex] = new Tuple<string, float>(lastGeneId, lowestExpression);
                    _result[highestGeneExpressionIndex] = new Tuple<string, float>(lastGeneId, highestExpression);
                    // reserve this space for later
                    _result.Add(new Tuple<string, float>("", 0f));
                    _result.Add(new Tuple<string, float>("", 0f));
                    highestGeneExpressionIndex = _result.Count - 1;
                    lowestGeneExpressionIndex = _result.Count - 2;
                    highestExpression = 0f;
                    lowestExpression = float.PositiveInfinity;
                    lastGeneId = geneId;
                }
                _result.Add(new Tuple<string, float>(cellName, expression));
                if (expression > highestExpression)
                {
                    highestExpression = expression;
                }
                if (expression < lowestExpression)
                {
                    lowestExpression = expression;
                }
            }

            // replace the last value as well
            _result[lowestGeneExpressionIndex] = new Tuple<string, float>(lastGeneId, lowestExpression);
            _result[highestGeneExpressionIndex] = new Tuple<string, float>(lastGeneId, highestExpression);

            QueryRunning = false;
        }

        /// <summary>
        /// Queries the databsae for the gene ids of multiple genes.
        /// This method will put <see cref="Tuple"/> with gene names as string and gene ids as string in <see cref="_result"/>.
        /// </summary>
        /// <param name="genes">An arra with the gene names.</param>
        internal void QueryGenesIds(string[] genes)
        {
            QueryRunning = true;
            StartCoroutine(QueryGeneIdsCoroutine(genes));

        }

        private IEnumerator QueryGeneIdsCoroutine(string[] genes)
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < genes.Length; ++i)
            {
                string gene = genes[i];
                builder.Append("\"").Append(gene).Append("\"");
                if (i < genes.Length - 1)
                {
                    builder.Append(", ");
                }
            }
            string geneNames = builder.ToString();

            string query = "select gname, id from genes where gname in (" + geneNames + ")";
            Thread t = new Thread(() => QueryThread(query));
            t.Start();
            while (t.IsAlive)
            {
                yield return null;
            }

            _result.Clear();
            while (_reader.Read())
            {
                string geneName = _reader.GetString(0);
                string id = _reader.GetInt32(1).ToString();
                _result.Add(new Tuple<string, string>(geneName, id));
            }
            QueryRunning = false;
        }

        /// <summary>
        /// Query the database for a gene and set up a function to be called that reads the results
        /// </summary>
        /// <param name="geneName">The gene to query for</param>
        /// <param name="action">The function to run when the results are ready</param>
        public void QueryGene(string geneName, Action<SQLite> action)
        {
            QueryRunning = true;
            StartCoroutine(QueryGeneCoroutine(geneName, action));
        }

        private IEnumerator QueryGeneCoroutine(string geneName, Action<SQLite> action)
        {
            _result.Clear();

            string query = "";
            Regex rx = new Regex(@",", RegexOptions.Compiled);
            MatchCollection matches = rx.Matches(geneName);
            if (matches.Count > 0) // multiple genes
            {
                Regex ry = new Regex(@"\b(?<word>\w+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                StringBuilder builder = new StringBuilder();
                matches = ry.Matches(geneName);
                foreach (Match match in matches)
                {
                    GroupCollection groups = match.Groups;
                    builder.Append("\"").Append(groups["word"].Value.ToUpper()).Append("\",");
                }
                geneName = builder.ToString();
                geneName = geneName.Remove(geneName.Length - 1);

                query = "select cname, sum(value) " +
                "from datavalues " +
                "INNER JOIN cells ON(datavalues.cell_id = cells.id) " +
                "where gene_id IN (" +
                    "select id from genes " +
                    "where upper(gname) " +
                    "IN(" + geneName + ") ) GROUP BY cname";
            }
            else
            {
                query = "SELECT cname, value from datavalues left join cells on datavalues.cell_id = cells.id " +
                   "where gene_id = (select id from genes where upper(gname) = upper( \"" + geneName + "\"))";
            }
       
            Thread t = new Thread(() => QueryThread(query));
            t.Start();
            while (t.IsAlive)
            {
                yield return null;
            }
            while (_reader.Read())
            {
                string cellName = _reader.GetString(0);
                float expression = _reader.GetFloat(1);
                _result.Add(new Tuple<string, float>(cellName, expression));
            }
            QueryRunning = false;
            action.Invoke(this);
        }

        /// <summary>
        /// Queries the database for the expressions of a gene.
        /// </summary>
        /// <param name="geneName"> The name of the gene </param>
        /// <returns> An array of all gene expression, ordered by cell </returns>
        public void QueryGene(string geneName, GraphManager.GeneExpressionColoringMethods coloringMethod)
        {
            QueryRunning = true;

            StartCoroutine(QueryGeneCoroutine(geneName, coloringMethod));
        }

        /// <summary>
        /// Queries the database for the expression values for a gene and puts the result in _result
        /// </summary>
        /// <param name="geneName"> The gene name </param>
        private IEnumerator QueryGeneCoroutine(string geneName, GraphManager.GeneExpressionColoringMethods coloringMethod)
        {
            //int statusId = status.AddStatus("Querying database for gene " + geneName);
            _result.Clear();

            string query = "";
            Regex rx = new Regex(@",", RegexOptions.Compiled);
            MatchCollection matches = rx.Matches(geneName);
            if ( matches.Count > 0) // multiple genes
            {
                Regex ry = new Regex(@"\b(?<word>\w+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                StringBuilder builder = new StringBuilder();
                matches = ry.Matches(geneName);
                foreach (Match match in matches)
                {
                    GroupCollection groups = match.Groups;
                    builder.Append("\"").Append(groups["word"].Value.ToUpper()).Append("\",");
                }
                geneName = builder.ToString();
                geneName = geneName.Remove(geneName.Length - 1);

                query = "select cname, sum(value) " +
                "from datavalues " +
                "INNER JOIN cells ON(datavalues.cell_id = cells.id) " +
                "where gene_id IN (" +
                    "select id from genes " +
                    "where upper(gname) " +
                    "IN(" + geneName + ") ) GROUP BY cname";
            }



            else
            {
                query = "SELECT cname, value from datavalues left join cells on datavalues.cell_id = cells.id " +
                   "where gene_id = (select id from genes where gname = \"" + geneName + "\")";
            }

            print("SQL queriy: " + query + ";");

            Thread t = new Thread(() => QueryThread(query));
            t.Start();
            while (t.IsAlive)
            {
                yield return null;
            }

            int i = 0;
            LowestExpression = float.MaxValue;
            HighestExpression = float.MinValue;
            if (coloringMethod == GraphManager.GeneExpressionColoringMethods.EqualExpressionRanges)
            {
                // put results in equally sized buckets
                while (_reader.Read())
                {
                    float expr = _reader.GetFloat(1);
                    if (expr > HighestExpression)
                    {
                        HighestExpression = expr;
                    }
                    if (expr < LowestExpression)
                    {
                        LowestExpression = expr;
                    }
                    i++;
                    _result.Add(new CellExpressionPair(_reader.GetString(0), expr, -1));
                }
                if (HighestExpression == LowestExpression)
                {
                    HighestExpression += 1;
                }
                // increase highest expresion slightly so the actually highest expressed cell get in the correct group
                HighestExpression *= 1.0001f;
                float binSize = (HighestExpression - LowestExpression) / CellexalConfig.Config.GraphNumberOfExpressionColors;
                if (DebugMode)
                {
                    print("binsize = " + binSize);
                }
                foreach (CellExpressionPair pair in _result)
                {
                    pair.Color = (int)((pair.Expression - LowestExpression) / binSize);
                }
            }
            else
            {
                List<CellExpressionPair> result = new List<CellExpressionPair>();
                LowestExpression = float.MaxValue;
                HighestExpression = float.MinValue;
                // put the same number of results in each bucket, ordered
                while (_reader.Read())
                {
                    CellExpressionPair newPair = new CellExpressionPair(_reader.GetString(0), _reader.GetFloat(1), -1);
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

                float binsize = (float)result.Count / CellexalConfig.Config.GraphNumberOfExpressionColors;
                for (int j = 0; j < result.Count; ++j)
                {
                    result[j].Color = (int)(j / binsize);
                }
                _result.AddRange(result);
            }
            if (DebugMode)
                print("Number of columns returned from database: " + i);
            _reader.Close();
            _connection.Close();
            //status.RemoveStatus(statusId);

            QueryRunning = false;
        }

        private List<string> TTestLists(List<Pair<string, List<float>>> expressions1, List<Pair<string, List<float>>> expressions2, int length1, int length2)
        {
            _result = new ArrayList();
            List<string> actualGeneIds = new List<string>();
            int index1 = 0, index2 = 0;
            for (int i = 0; index1 < expressions1.Count || index2 < expressions2.Count; ++i)
            {
                string geneId = "";
                List<float> expr1 = null, expr2 = null;
                if (index1 < expressions1.Count && int.Parse(expressions1[index1].First) == i)
                {
                    geneId = expressions1[index1].First;
                    expr1 = expressions1[index1].Second;
                    index1++;
                }
                if (index2 < expressions2.Count && int.Parse(expressions2[index2].First) == i)
                {
                    geneId = expressions2[index2].First;
                    expr2 = expressions2[index2].Second;
                    index2++;
                }

                if (expr1 != null || expr2 != null)
                {
                    if (expr1 == null)
                    {
                        expr1 = new List<float>();
                    }
                    if (expr2 == null)
                    {
                        expr2 = new List<float>();
                    }
                    float tValue = TTest(expr1, expr2, length1, length2);
                    _result.Add(new Pair<string, float>(geneId, tValue));
                    actualGeneIds.Add(geneId);
                }
            }
            return actualGeneIds;
        }

        /// <summary>
        /// Conduct a t-test
        /// </summary>
        private float TTest(List<float> x, List<float> y, int n1, int n2)
        {
            double sumX = 0.0;
            double sumY = 0.0;
            for (int i = 0; i < x.Count; ++i)
                sumX += x[i];
            for (int i = 0; i < y.Count; ++i)
                sumY += y[i];

            // means
            double meanX = sumX / n1;
            double meanY = sumY / n2;

            // variances
            double sumXminusMeanSquared = 0.0;
            double sumYminusMeanSquared = 0.0;
            for (int i = 0; i < x.Count; ++i)
                sumXminusMeanSquared += (x[i] - meanX) * (x[i] - meanX);
            sumXminusMeanSquared += meanX * meanX * (n1 - x.Count);

            for (int i = 0; i < y.Count; ++i)
                sumYminusMeanSquared += (y[i] - meanY) * (y[i] - meanY);
            sumYminusMeanSquared += meanY * meanY * (n2 - y.Count);

            double varX = sumXminusMeanSquared / (n1 - 1);
            double varY = sumYminusMeanSquared / (n2 - 1);

            // the t statistic
            if (varX == 0.0 && varY == 0.0) return 0f;
            double top = (meanX - meanY);
            double bot = Math.Sqrt((varX / n1) + (varY / n2));
            return (float)(top / bot);
        }


        /// <summary>
        /// Helper method that is run as a Thread.
        /// </summary>
        private void QueryThread(string query)
        {
            _connection.Open();
            _command.CommandText = query;
            _reader = _command.ExecuteReader();
        }
        #endregion

        /// <summary>
        /// Basic execute command - open, create command, execute, close
        /// </summary>
        /// <param name="commandText"></param>
        public void ExecuteNonQuery(string commandText)
        {
            _connection.Open();
            _command.CommandText = commandText;
            _command.ExecuteNonQuery();
            _connection.Close();
        }

        /// <summary>
        /// Clean up everything for SQLite
        /// </summary>
        private void SQLiteClose()
        {
            if (_reader != null && !_reader.IsClosed)
                _reader.Close();
            _reader = null;

            if (_command != null)
                _command.Dispose();
            _command = null;

            if (_connection != null && _connection.State != System.Data.ConnectionState.Closed)
                _connection.Close();
            _connection = null;
        }
    }

    /// <summary>
    /// Helper class to store two generic type.
    /// </summary>
    /// <typeparam name="T">The first type</typeparam>
    /// <typeparam name="U">The second type</typeparam>
    public class Pair<T, U>
    {
        public T First { get; set; }
        public U Second { get; set; }

        public Pair() { }

        public Pair(T first, U second)
        {
            this.First = first;
            this.Second = second;
        }
    }

    /// <summary>
    /// Helper struct for representing a pair of a cell (represented as a string) and a float
    /// </summary>
    public class CellExpressionPair : IComparable<CellExpressionPair>
    {
        public string Cell { get; set; }
        public float Expression { get; set; }
        public int Color { get; set; }

        public CellExpressionPair(string Cell, float Expression, int Color)
        {
            this.Cell = Cell;
            this.Expression = Expression;
            this.Color = Color;
        }

        public int CompareTo(CellExpressionPair other)
        {
            return (int)(Expression - other.Expression);
        }
    }
}
