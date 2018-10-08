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

        public Boutilier(ref List<Station> stationList, ref List<OHCAEvent> eventList, double f)
        {
            this.I = stationList.Count;
            this.J = eventList.Count;
            this.f = f;

            OptimalPlacement(ref stationList, ref eventList);
            PlaceDrones(ref stationList);
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

                for (int l = I - 1; l >= 0; l--)
                {
                    if (y[I].Get(GRB.DoubleAttr.X) == 0)
                    {
                        stationList.RemoveAt(I);
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

        public void PlaceDrones(ref List<Station> stationList)
        {
            return;
        }
    }
}
