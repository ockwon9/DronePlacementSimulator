using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Gurobi;
using System.ComponentModel;
using System.Threading;
using System.IO;


namespace DronePlacementSimulator
{
    class Pulver
    {
        private static bool DEBUG = false;
        private static double square = (25 * Utils.LAMBDA_PRECISION * Utils.LAMBDA_PRECISION);
        private List<Station> stationList;
        private int n;
        private int m;

        private double w;
        private double h;
        private double[][] b;
        private double optimalCoverage;
        private List<double> demandList;
        private List<int>[] N;
        private int workersRemaining;
        private SpinLock workersLock;

        public Pulver (double w, int p, double h, ref List<Station> stationList, ref Grid grid)
        {
            this.stationList = stationList.ConvertAll(s => new Station(s));
            this.n = grid.inSeoul.Count;
            this.m = stationList.Count;
            this.w = w;
            this.h = h;

            this.b = new double[n][];
            for (int i = 0; i < n; i++)
            {
                b[i] = new double[m];
            }

            this.demandList = new List<double>();
            this.Demandify(grid);
            if (DEBUG)
            {
                QuantifyService(n, m, ref stationList, ref grid);
            }
            else
            {
                ReadDemand();
            }
            this.N = new List<int>[n];
            for (int i = 0; i < n; i++)
            {
                this.N[i] = new List<int>();
            }
            BoundByT(ref grid, ref stationList);
            this.optimalCoverage = OptimalCoverage(n, m, w, p, h, ref b, ref this.N, ref this.demandList, ref stationList);
        }

        public void ReadDemand()
        {
            int coreCount = 6;
            int row = 0;
            int[] actualLoad = new int[coreCount];
            for (int i = 0; i < coreCount; i++)
            {
                actualLoad[i] = n / coreCount + ((i < (n % coreCount)) ? 1 : 0);
                StreamReader file = new StreamReader("Pulver_Area_" + i + ".csv");
                string line;
                while ((line = file.ReadLine()) != null)
                {
                    string[] split = line.Split(',');
                    for (int j = 0; j < m; j++)
                    {
                        this.b[row][j] = double.Parse(split[j]);
                    }
                    row++;
                }
                file.Close();
            }
        }

        public void QuantifyService(int n, int m, ref List<Station> stationList, ref Grid grid)
        {
            int coreCount = 6;
            BackgroundWorker[] workers = new BackgroundWorker[coreCount];
            int dividedLoad = n / coreCount;
            int rem = n % coreCount;
            this.workersRemaining = coreCount;
            this.workersLock = new SpinLock();
            
            int row = 0;
            for (int i = 0; i < coreCount; i++)
            {
                int actualLoad = dividedLoad + ((i < rem) ? 1 : 0);
                workers[i] = new BackgroundWorker();
                Pair[] workLoad = new Pair[actualLoad];
                Array.Copy(grid.inSeoul.ToArray(), row, workLoad, 0, actualLoad);

                WorkObject work = new WorkObject(workLoad, i, row);

                workers[i].DoWork += Quantify_DoWork;
                workers[i].RunWorkerCompleted += Quantify_RunWorkerCompleted;
                row += actualLoad;
                workers[i].RunWorkerAsync(work);
            }

            while (workersRemaining > 0)
            {
            }

            return;
        }

        private void Quantify_DoWork(object sender, DoWorkEventArgs e)
        {
            WorkObject workObject = e.Argument as WorkObject;
            Overlap overlap = new Overlap();
            System.Console.WriteLine(workObject.index);
            StreamWriter file = new StreamWriter("Pulver_Area_" + workObject.index + ".csv");

            for (int i = 0; i < workObject.load.Count(); i++)
            {
                int k = i + workObject.row;
                for (int j = 0; j < this.m; j++)
                {
                    this.b[k][j] = overlap.Area(workObject.load[i].kiloX, workObject.load[i].kiloY, 5 * Utils.LAMBDA_PRECISION, 5 * Utils.LAMBDA_PRECISION, stationList[j].kiloX, stationList[j].kiloY, Utils.GOLDEN_TIME);
                    this.b[k][j] /= square;
                    file.Write(this.b[k][j]);
                    file.Write(",");
                }
                file.Write("\n");
                Console.WriteLine("Thread " + workObject.index + " done with line " + i);
            }
            file.Close();
            
            Console.WriteLine("thread " + workObject.index + " done.");
        }

        private void Quantify_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            System.Console.WriteLine((string)e.Result);
            bool lockTaken = false;
            while (!lockTaken)
            {
                workersLock.TryEnter(ref lockTaken);
            }
            --workersRemaining;
            workersLock.Exit();
        }

        public class WorkObject
        {
            public Pair[] load;
            public int index;
            public int row;
            public WorkObject(Pair[] load, int index, int row)
            {
                this.load = load.Clone() as Pair[];
                this.index = index;
                this.row = row;
            }
        }

        public int Demandify(Grid grid)
        {

            this.demandList.Clear();

            double maxDemand = grid.GetMaxDemand();
            for (int i = 0; i < n; i++)
            {
                this.demandList.Add(grid.pooledLambda[grid.inSeoul[i].yIndex][grid.inSeoul[i].xIndex] / (25 * maxDemand));
            }

            return this.demandList.Count;
        }

        public void BoundByT(ref Grid grid, ref List<Station> stationList)
        {
            for (int i = 0; i < this.n; i++)
            {
                for (int j = 0; j < this.m; j++)
                {
                    if (this.b[i][j] > 0)
                    {
                        this.N[i].Add(j);
                    }
                }
            }
        }

        public double OptimalCoverage(int n, int m, double w, int p, double h, ref double[][] b, ref List<int>[] N, ref List<double> demandList, ref List<Station> stationList)
        {
            double res = -1;

            try
            {
                GRBEnv env = new GRBEnv();
                GRBModel model = new GRBModel(env);

                GRBVar[] X = new GRBVar[m];         // number of drones launched from site j
                for (int j = 0; j < m; j++)
                {
                    X[j] = model.AddVar(0.0, Double.PositiveInfinity, 0.0, GRB.INTEGER, "X_" + j);
                }

                GRBVar[] Z = new GRBVar[n];         // amount of total overall coverage received by demand unit i
                for (int i = 0; i < n; i++)
                {
                    Z[i] = model.AddVar(0.0, Double.PositiveInfinity, 0.0, GRB.CONTINUOUS, "Z_" + i);
                }

                GRBVar[] Y = new GRBVar[n];         // amount of backup coverage received by demand unit i
                for (int i = 0; i < n; i++)
                {
                    Y[i] = model.AddVar(0.0, Double.PositiveInfinity, 0.0, GRB.CONTINUOUS, "Y_" + i);
                }

                GRBVar[] W = new GRBVar[n];         // amount of primary coverage received by demand unit i
                for (int i = 0; i < n; i++)
                {
                    W[i] = model.AddVar(0.0, Double.PositiveInfinity, 0.0, GRB.CONTINUOUS, "W_" + i);
                }

                GRBLinExpr obj_expr = 0.0;
                for (int i = 0; i < n; i++)
                {
                    obj_expr.AddTerm(w, Y[i]);
                }
                for (int i = 0; i < n; i++)
                {
                    obj_expr.AddTerm(1 - w, W[i]);
                }

                model.SetObjective(obj_expr, GRB.MAXIMIZE);
                
                for (int i = 0; i < n; i++)         // sum_{j} {b_i,j * X_j} >= Z_i for i = 0, ..., n - 1
                {
                    GRBLinExpr expr = 0.0;
                    foreach (int j in N[i])
                    {
                        expr.AddTerm(b[i][j], X[j]);
                    }
                    expr.AddTerm(-1.0, Z[i]);
                    model.AddConstr(expr, GRB.GREATER_EQUAL, 0.0, "c0_" + i);
                }

                for (int i = 0; i < n; i++)         // Y_i <= Z_i - d_i
                {
                    GRBLinExpr expr = 0.0;
                    expr.AddTerm(-1.0, Z[i]);
                    expr.AddTerm(1.0, Y[i]);
                    model.AddConstr(expr, GRB.LESS_EQUAL, -demandList[i], "c1_" + i);
                }

                for (int i = 0; i < n; i++)         // W_i <= Z_i
                {
                    GRBLinExpr expr = 0.0;
                    expr.AddTerm(-1.0, Z[i]);
                    expr.AddTerm(1.0, W[i]);
                    model.AddConstr(expr, GRB.LESS_EQUAL, 0, "c2_" + i);
                }

                for (int i = 0; i < n; i++)         // W_i <= d_i
                {
                    GRBLinExpr expr = 0.0;
                    expr.AddTerm(1.0, W[i]);
                    model.AddConstr(expr, GRB.LESS_EQUAL, demandList[i], "c3_" + i);
                }

                for (int i = 0; i < n; i++)         // Z_i <= h * d_i
                {
                    GRBLinExpr expr = 0.0;
                    expr.AddTerm(1.0, Z[i]);
                    model.AddConstr(expr, GRB.LESS_EQUAL, h * demandList[i], "c4_" + i);
                }

                GRBLinExpr p_expr = 0.0;
                for (int j = 0; j < m; j++)         // sum_{j} {X_j} <= p
                {
                    p_expr.AddTerm(1.0, X[j]);
                }
                model.AddConstr(p_expr, GRB.LESS_EQUAL, p, "c_p");
                model.Write("model.lp");

                model.Optimize();

                int optimstatus = model.Status;

                if (optimstatus == GRB.Status.INF_OR_UNBD)
                {
                    model.Parameters.Presolve = 0;
                    model.Optimize();
                    optimstatus = model.Status;
                }

                if (optimstatus == GRB.Status.OPTIMAL)
                {
                    double objval = model.ObjVal;

                    res = objval;
                }
                else if (optimstatus == GRB.Status.INFEASIBLE)
                {
                    Console.WriteLine("Model is unbounded");
                }
                else
                {
                    Console.WriteLine("Optimization was stopped with status = " + optimstatus);
                }

                int sum = 0;
                for (int l = 0; l < m; l++)
                {
                    int numDrone = (int) X[l].Get(GRB.DoubleAttr.X);
                    sum += numDrone;
                    for (int k = 0; k < numDrone; k++)
                    {
                        stationList[l].droneList.Add(new Drone(stationList[l].stationID));
                    }
                    
                    if (/*DEBUG && */numDrone > 0)
                        Console.WriteLine(numDrone + " drones at station " + l + ", which is at (" + stationList[l].kiloX + ", " + stationList[l].kiloY + ")");
                }
                for (int l = stationList.Count - 1; l >= 0; l--)
                {
                    if (stationList[l].droneList.Count == 0)
                        stationList.RemoveAt(l);
                }
                Console.WriteLine("sum = " + sum);

                model.Dispose();
                env.Dispose();
            }
            catch (GRBException e)
            {
                Console.WriteLine("Error code : " + e.ErrorCode + ", " + e.Message);
            }

            return res;
        }
    }    
}