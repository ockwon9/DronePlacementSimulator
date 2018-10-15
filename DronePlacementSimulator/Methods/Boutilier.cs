using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gurobi;

namespace DronePlacementSimulator
{
    class Boutilier
    {
        private static bool DEBUG = false;
        private int I;
        private int J;
        private double f;
        private int numStations = 0;
        private List<List<int>> realCoverList;

        public Boutilier(bool stationsSet, ref List<Station> stationList, ref List<OHCAEvent> eventList, double f, double r)
        {
            this.I = stationList.Count;
            this.J = eventList.Count;
            this.f = f;
            this.realCoverList = new List<List<int>>();

            Console.WriteLine(I);
            Console.WriteLine(J);

            if (!stationsSet)
            {
                OptimalPlacement(ref stationList, ref eventList);
                PlaceDrones(r, ref stationList);
            }
        }

        public void OptimalPlacement(ref List<Station> stationList, ref List<OHCAEvent> eventList)
        {
            int[][] a = new int[J][];
            for (int j = 0; j < J; j++)
            {
                a[j] = new int[I];
                for (int i = 0; i < I; i++)
                {
                    a[j][i] = (Utils.GetDistance(stationList[i].kiloX, stationList[i].kiloY, eventList[j].kiloX, eventList[j].kiloY) < Utils.GOLDEN_TIME) ? 1 : 0;
                }
            }

            try
            {
                GRBEnv env = new GRBEnv("Boutilier.log");
                GRBModel model = new GRBModel(env);

                GRBVar[] y = new GRBVar[I];
                for (int i = 0; i < I; i++)
                {
                    y[i] = model.AddVar(0.0, 1.0, 0.0, GRB.BINARY, "y_" + i);
                }

                GRBVar[][] z = new GRBVar[J][];
                for (int j = 0; j < J; j++)
                {
                    z[j] = new GRBVar[I];
                    for (int i = 0; i < I; i++)
                    {
                        z[j][i] = model.AddVar(0.0, 1.0, 0.0, GRB.BINARY, "z_" + i + "," + j);
                    }
                }

                GRBLinExpr obj_expr = 0.0;
                for (int i = 0; i < I; i++)
                {
                    obj_expr.AddTerm(1.0, y[i]);
                }

                model.SetObjective(obj_expr, GRB.MINIMIZE);
                GRBLinExpr bigExpr = 0.0;
                for (int j = 0; j < J; j++)
                {
                    GRBLinExpr expr = 0.0;
                    for (int i = 0; i < I; i++)
                    {
                        expr.AddTerm(1, z[j][i]);
                    }
                    bigExpr.Add(expr);
                    model.AddConstr(expr, GRB.LESS_EQUAL, 1.0, "c0_" + j);
                }
                model.AddConstr(bigExpr, GRB.GREATER_EQUAL, f / 100.0 * J, "c1");

                for (int j = 0; j < J; j++)
                {
                    for (int i = 0; i < I; i++)
                    {
                        model.AddConstr(z[j][i], GRB.LESS_EQUAL, a[j][i] * y[i], "c2_" + i + "," + j);
                    }
                }

                model.Write("model_b.lp");

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

                    numStations = (int) objval;
                }
                else if (optimstatus == GRB.Status.INFEASIBLE)
                {
                    Console.WriteLine("Model is unbounded");
                }
                else
                {
                    Console.WriteLine("Optimization was stopped with status = " + optimstatus);
                }

                bool[] boolY = new bool[I];
                for (int i = 0; i < I; i++)
                {
                    boolY[i] = (y[i].Get(GRB.DoubleAttr.X) == 1);
                }

                List<int>[] coverList = new List<int>[I];
                for (int i = 0; i < I; i++)
                {
                    coverList[i] = new List<int>();
                }
                for (int j = 0; j < J; j++)
                {
                    for (int i = 0; i < I; i++)
                    {
                        if (boolY[i] && z[j][i].Get(GRB.DoubleAttr.X) == 1)
                        {
                            coverList[i].Add(j);
                        }
                    }
                }

                for (int i = I - 1; i >= 0; i--)
                {
                    if (boolY[i])
                    {
                        realCoverList.Insert(0, coverList[i]);
                    }
                    else
                    {
                        stationList.RemoveAt(i);
                    }
                }

                model.Dispose();
                env.Dispose();
            }
            catch (GRBException e)
            {
                Console.WriteLine("Error code : " + e.ErrorCode + ", " + e.Message);
            }
        }

        public void PlaceDrones(double r, ref List<Station> stationList)
        {
            for (int i = 0;  i < stationList.Count; i++)
            {
                int m = 0;
                List<int> list = realCoverList[i];

                double lambda = list.Count / Utils.MINUTES_IN_4_YEARS;
                double mu = 1.0 / 60.0;
                double rho, mrho;

                double sum = 0.0;
                while (sum <= r)
                {
                    m++;
                    mrho = lambda / mu;
                    rho = mrho / m;

                    sum = 0.0;
                    double temp = 1.0;
                    double pi = 1.0;
                    for (int k = 1; k < m; k++)
                    {
                        temp *= (mrho / k);
                        pi += temp;
                    }
                    temp *= (rho / (1 - rho));
                    pi += temp;
                    pi = 1 / pi;
                    sum += pi;

                    for (int k = 1; k < m; k++)
                    {
                        pi *= (mrho / k);
                        sum += pi;
                    }
                }

                for (int j = 0; j < m; j++)
                {
                    stationList[i].droneList.Add(new Drone(stationList[i].stationID));
                }
            }
        }
    }
}
