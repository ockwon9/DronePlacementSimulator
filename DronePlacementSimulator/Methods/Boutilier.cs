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
        private int I;
        private int J;
        private double f;
        private List<List<int>> coverList;

        public Boutilier(ref List<Station> stationList, ref List<OHCAEvent> eventList, double f, double r)
        {
            this.I = stationList.Count;
            this.J = eventList.Count;
            this.f = f;
            this.coverList = new List<List<int>>();

            Console.WriteLine(I);
            Console.WriteLine(J);
            
            OptimalPlacement(ref stationList, ref eventList);
            PlaceDrones(r, ref stationList, ref eventList);
        }

        public void OptimalPlacement(ref List<Station> stationList, ref List<OHCAEvent> eventList)
        {
            int[,] a = new int[J, I];
            for (int j = 0; j < J; j++)
            {
                for (int i = 0; i < I; i++)
                {
                    a[j, i] = (Utils.GetDistance(stationList[i].lat, stationList[i].lon, eventList[j].lat, eventList[j].lon) < Utils.GOLDEN_TIME - 1.0 / 6.0) ? 1 : 0;
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

                GRBVar[, ] z = new GRBVar[J, I];
                for (int j = 0; j < J; j++)
                {
                    for (int i = 0; i < I; i++)
                    {
                        z[j, i] = model.AddVar(0.0, 1.0, 0.0, GRB.BINARY, "z_" + i + "," + j);
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
                        expr.AddTerm(1, z[j, i]);
                    }
                    bigExpr.Add(expr);
                    model.AddConstr(expr, GRB.LESS_EQUAL, 1.0, "c0_" + j);
                }
                model.AddConstr(bigExpr, GRB.GREATER_EQUAL, f / 100.0 * J, "c1");

                for (int j = 0; j < J; j++)
                {
                    for (int i = 0; i < I; i++)
                    {
                        model.AddConstr(z[j, i], GRB.LESS_EQUAL, a[j, i] * y[i], "c2_" + i + "," + j);
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
                }

                else if (optimstatus == GRB.Status.INFEASIBLE)
                {
                    Console.WriteLine("Model is unbounded");
                }
                else
                {
                    Console.WriteLine("Optimization was stopped with status = " + optimstatus);
                }

                List<Station> tempList = new List<Station>();
                CloneList(stationList, tempList);
                stationList.Clear();
                for (int i = 0; i < I; i++)
                {
                    if (y[i].Get(GRB.DoubleAttr.X) > 0)
                    {
                        stationList.Add(tempList[i]);
                    }
                }

                I = stationList.Count;

                coverList = new List<List<int>>();
                for (int i = 0; i < I; i++)
                {
                    coverList.Add(new List<int>());
                }

                for (int j = 0; j < J; j++)
                {
                    for (int i = 0; i < I; i++)
                    {
                        if (z[j, i].Get(GRB.DoubleAttr.X) > 0)
                        {
                            coverList[i].Add(j);
                        }
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

        public void PlaceDrones(double r, ref List<Station> stationList, ref List<OHCAEvent> eventList)
        {
            for (int i = 0;  i < I; i++)
            {
                int m = 0, eventCount = 0;
                List<int> list = coverList[i];
                for (int j = 0; j < list.Count; j++)
                {
                    if (eventList[j].occurrenceTime.Hour >= 8 && eventList[j].occurrenceTime.Hour < 20)
                        eventCount++;
                }

                double lambda = 2.0 * (double)eventCount / Utils.MINUTES_IN_4_YEARS;
                double mu = 1.0 / (60 * Utils.DRONE_REST_TIME);
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

        private void CloneList(List<Station> srcList, List<Station> dstList)
        {
            dstList.Clear();
            srcList.ForEach((item) =>
            {
                dstList.Add(new Station(item));
            });
        }
    }
}
