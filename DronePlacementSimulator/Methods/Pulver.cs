using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gurobi;


namespace DronePlacementSimulator
{
    class Demand
    {
        public double x;
        public double y;
        public double d;

        public Demand(double x, double y, double d)
        {
            this.x = x;
            this.y = y;
            this.d = d;
        }
    }

    class Pulver
    {
        private static double DRONE_VELOCITY = 1.0f;
        private int n;
        private int m;
        private double w;
        private double h;
        private double[][] b;
        private int t;
        private double optimalCoverage;
        private List<Demand> demandList;
        private List<int>[] N;

        public Pulver (double w, int p, double h, int t, ref List<Station> stationList, ref Grid grid)
        {
            this.n = grid.numCells;
            this.m = stationList.Count();
            this.w = w;
            this.h = h;
            this.t = t;

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
            BoundByT(ref stationList, ref this.demandList, t);
            this.optimalCoverage = OptimalCoverage(n, m, w, p, h, ref b, ref this.N, ref this.demandList, ref stationList);
        }

        public void QuantifyService(int n, int m, ref List<Station> stationList, ref Grid grid, int t)
        {
            Overlap overlap = new Overlap();

            int i = 0;
            foreach (double[] temp in grid.cells)
            {
                double x = temp[0] + grid.unit;
                double y = temp[1] + grid.unit;

                int j = 0;
                foreach (Station s in stationList)
                {
                    this.b[i][j] = overlap.Area(x, y, grid.unit, grid.unit, s.latitude, s.longitude, t);
                    j++;
                }

                i++;
            }
        }

        public void Demandify(Grid grid)
        {
            this.demandList.Clear();

            double maxDemand = grid.getMaxDemand();
            int i = 0;
            foreach (double[] temp in grid.cells)
            {
                this.demandList.Add(new Demand(temp[0], temp[1], grid.pdf[i] / maxDemand));
                i++;
            }
        }

        public void BoundByT(ref List<Station> stationList, ref List<Demand> demandList, int t)
        {
            int i = 0;
            foreach (Demand d in demandList)
            {
                int j = 0;
                foreach (Station s in stationList)
                {
                    if (Distance(s.x, s.y, d.x, d.y) <= DRONE_VELOCITY * (double)t)
                    {
                        this.N[i].Add(j);
                    }
                    j++;
                }

                i++;
            }
        }
        
        public double Distance(double x1, double y1, double x2, double y2)
        {
            return Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));
        }

        public double OptimalCoverage(int n, int m, double w, int p, double h, ref double[][] b, ref List<int>[] N, ref List<Demand> demandList, ref List<Station> stationList)
        {
            double res = -1;

            try
            {
                GRBEnv env = new GRBEnv("Pulver.log");
                GRBModel model = new GRBModel(env);

                GRBVar[] X = new GRBVar[m];         // number of drones launched from site j
                for (int j = 0; j < m; j++)
                {
                    X[j] = model.AddVar(0.0, 10.0, 0.0, GRB.INTEGER, "X_" + j);
                }

                GRBVar[] Z = new GRBVar[n];         // amount of total overall coverage received by demand unit i
                for (int i = 0; i < n; i++)
                {
                    Z[i] = model.AddVar(0.0, 1.0, 0.0, GRB.CONTINUOUS, "Z_" + i);
                }

                GRBVar[] Y = new GRBVar[n];         // amount of backup coverage received by demand unit i
                for (int i = 0; i < n; i++)
                {
                    Y[i] = model.AddVar(0.0, 1.0, w, GRB.CONTINUOUS, "Y_" + i);
                }

                GRBVar[] W = new GRBVar[n];         // amount of primary coverage received by demand unit i
                for (int i = 0; i < n; i++)
                {
                    W[i] = model.AddVar(0.0, 1.0, 1 - w, GRB.CONTINUOUS, "W_" + i);
                }

                GRBLinExpr expr;
                for (int i = 0; i < n; i++)         // sum_{j} {b_i,j * X_j} >= Z_i for i = 0, ..., n - 1
                {
                    expr = 0;
                    foreach (int j in N[i])
                    {
                        expr = expr + b[i][j] * X[j];
                    }
                    expr = expr - Z[i];
                    model.AddConstr(expr, GRB.GREATER_EQUAL, 0.0, "c0_" + i);
                }

                for (int i = 0; i < n; i++)         // Y_i <= Z_i - d_i
                {
                    model.AddConstr(Y[i] - Z[i] + demandList[i].d <= 0, "c1_" + i);
                }

                for (int i = 0; i < n; i++)         // W_i <= Z_i
                {
                    model.AddConstr(W[i] - Z[i] <= 0, "c2_" + i);
                }

                for (int i = 0; i < n; i++)         // W_i <= d_i
                {
                    model.AddConstr(W[i] - demandList[i].d <= 0, "c3_" + i);
                }

                for (int i = 0; i < n; i++)         // Z_i <= h * d_i
                {
                    model.AddConstr(Z[i] - h * demandList[i].d <= 0, "c4_" + i);
                }

                expr = 0;
                for (int j = 0; j < m; j++)         // sum_{j} {X_j} <= p
                {
                    expr = expr + X[j];
                }
                model.AddConstr(expr, GRB.LESS_EQUAL, p, "c_p");

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
                
                int l = 0;
                foreach (Station s in stationList)
                {
                    int numDrone = (int) X[l].Get(GRB.DoubleAttr.X);
                    for (int k = 0; k < numDrone; k++)
                    {
                        s.droneList.Add(new Drone(s.stationID));
                    }

                    l++;
                }

                model.Dispose();
                env.Dispose();
            }
            catch (Exception e)
            {
                Console.WriteLine("GRB::" + e.ToString());
            }

            return res;
        }
    }
}