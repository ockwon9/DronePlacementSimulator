using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Gurobi;


namespace DronePlacementSimulator
{
    class Demand
    {
        public double lon;
        public double lat;
        public double d;

        public Demand(double lon, double lat, double d)
        {
            this.lon = lon;
            this.lat = lat;
            this.d = d;
        }
    }

    class Pulver
    {
        private static bool DEBUG = false;
        private int n;
        private int m;
        private double w;
        private double h;
        private double[][] b;
        private double t;
        private double optimalCoverage;
        private List<Demand> demandList;
        private List<int>[] N;
        private static double DRONE_VELOCITY = 1.0;

        public Pulver (double w, int p, double h, ref List<Station> stationList, ref Grid grid)
        {
            this.n = grid.inSeoul.Count;
            this.m = stationList.Count;
            this.w = w;
            this.h = h;

            this.b = new double[n][];
            for (int i = 0; i < n; i++)
            {
                b[i] = new double[m];
            }
            QuantifyService(n, m, ref stationList, ref grid, t);

            this.demandList = new List<Demand>();
            Demandify(grid);
            this.N = new List<int>[n];
            for (int i = 0; i < n; i++)
            {
                this.N[i] = new List<int>();
            }
            BoundByT(ref grid, ref stationList, t);
            this.optimalCoverage = OptimalCoverage(n, m, w, p, h, ref b, ref this.N, ref this.demandList, ref stationList);
        }

        public void QuantifyService(int n, int m, ref List<Station> stationList, ref Grid grid, double t)
        {
            Overlap overlap = new Overlap();
            int c = grid.lambda[0].Length;
            
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < m; j++)
                {
                    this.b[i][j] = overlap.Area(grid.inSeoul[i].kiloX, grid.inSeoul[i].kiloY, Utils.LAMBDA_PRECISION, Utils.LAMBDA_PRECISION, stationList[j].kiloX, stationList[j].kiloY, Utils.GOLDEN_TIME);
                }
            }
        }

        public void Demandify(Grid grid)
        {

            this.demandList.Clear();

            double maxDemand = grid.GetMaxDemand();
            for (int i = 0; i < grid.lambda.Length; i++)
            {
                for (int j = 0; j < grid.lambda[i].Length; j++)
                {
                    this.demandList.Add(new Demand((j + 0.5) * Utils.LAMBDA_PRECISION, (i + 0.5) * Utils.LAMBDA_PRECISION, grid.lambda[i][j] / maxDemand));
                }
            }
        }

        public void BoundByT(ref Grid grid, ref List<Station> stationList, double t)
        {
            Overlap overlap = new Overlap();
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

        public double OptimalCoverage(int n, int m, double w, int p, double h, ref double[][] b, ref List<int>[] N, ref List<Demand> demandList, ref List<Station> stationList)
        {
            double res = -1;

            try
            {
                GRBEnv env = new GRBEnv();
                GRBModel model = new GRBModel(env);

                GRBVar[] X = new GRBVar[m];         // number of drones launched from site j
                for (int j = 0; j < m; j++)
                {
                    X[j] = model.AddVar(0.0, 10.0, 0.0, GRB.INTEGER, "X_" + j);
                }

                GRBVar[] Z = new GRBVar[n];         // amount of total overall coverage received by demand unit i
                for (int i = 0; i < n; i++)
                {
                    Z[i] = model.AddVar(0.0, 10.0, 0.0, GRB.CONTINUOUS, "Z_" + i);
                }

                GRBVar[] Y = new GRBVar[n];         // amount of backup coverage received by demand unit i
                for (int i = 0; i < n; i++)
                {
                    Y[i] = model.AddVar(0.0, 10.0, 0.0, GRB.CONTINUOUS, "Y_" + i);
                }

                GRBVar[] W = new GRBVar[n];         // amount of primary coverage received by demand unit i
                for (int i = 0; i < n; i++)
                {
                    W[i] = model.AddVar(0.0, 10.0, 0.0, GRB.CONTINUOUS, "W_" + i);
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
                    expr.AddTerm(1.0, Y[i]);
                    expr.AddTerm(-1.0, Z[i]);
                    expr.AddConstant(demandList[i].d);
                    model.AddConstr(expr, GRB.LESS_EQUAL, 0, "c1_" + i);
                }

                for (int i = 0; i < n; i++)         // W_i <= Z_i
                {
                    GRBLinExpr expr = 0.0;
                    expr.AddTerm(1.0, W[i]);
                    expr.AddTerm(-1.0, Z[i]);
                    model.AddConstr(expr, GRB.LESS_EQUAL, 0, "c2_" + i);
                }

                for (int i = 0; i < n; i++)         // W_i <= d_i
                {
                    GRBLinExpr expr = 0.0;
                    expr.AddTerm(1.0, W[i]);
                    expr.AddConstant(-demandList[i].d);
                    model.AddConstr(expr, GRB.LESS_EQUAL, 0, "c3_" + i);
                }

                for (int i = 0; i < n; i++)         // Z_i <= h * d_i
                {
                    GRBLinExpr expr = 0.0;
                    expr.AddTerm(1.0, Z[i]);
                    expr.AddConstant(-h * demandList[i].d);
                    model.AddConstr(expr, GRB.LESS_EQUAL, 0, "c4_" + i);
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
                
                for (int l = 0; l < stationList.Count; l++)
                {
                    int numDrone = (int) X[l].Get(GRB.DoubleAttr.X);
                    for (int k = 0; k < numDrone; k++)
                    {
                        stationList[l].droneList.Add(new Drone(stationList[l].stationID));
                    }
                    
                    if (DEBUG && numDrone > 0)
                        Console.WriteLine(numDrone + " drones at station " + l + ", which is at (" + stationList[l].kiloX + ", " + stationList[l].kiloY + ")");
                }
                for (int l = stationList.Count - 1; l >= 0; l--)
                {
                    if (stationList[l].droneList.Count == 0)
                        stationList.RemoveAt(l);
                }

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