﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Shapes;
using Excel = Microsoft.Office.Interop.Excel;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;


namespace DronePlacementSimulator
{
    public partial class MainForm : Form
    {
        private bool writeSimulation = false;
        private int coreCount = 6;
        
        private List<Station> stationList;
        private List<OHCAEvent> eventList;
        private List<Polygon> polygonList;
        private List<List<double[]>> polyCoordList;

        private Simulator simulator = null;
        private Grid eventGrid = null;
        private Bitmap _canvas = null;
        private int targetStationCount;
        private int workersRemaining;
        private SpinLock workersLock;
        private int numEvents;
        private SpinLock numEventLock;

        public int coverRange = 0;
        
        public MainForm()
        {
            InitializeComponent();

            stationList = new List<Station>();
            eventList = new List<OHCAEvent>();
            polygonList = new List<Polygon>();
            polyCoordList = new List<List<double[]>>();

            // Set the size of simulator's window
            this.Height = Screen.PrimaryScreen.Bounds.Height;
            this.Width = (int)(this.Height * Utils.SEOUL_WIDTH / Utils.SEOUL_HEIGHT);
            coverRange = (int)(this.Height * Utils.GOLDEN_TIME / Utils.SEOUL_HEIGHT);
            toolStripComboBoxStations.SelectedIndex = 12;
            targetStationCount = 20;
            this.numEventLock = new SpinLock();
            this.workersLock = new SpinLock();

            // Read OHCA events data
            ReadEventData();
            ReadMapData();

            eventGrid = new Grid(ref polyCoordList);
            if (File.Exists("pdf.csv"))
            {
                ReadPDF(ref eventGrid);
            }
            else
            {
                eventGrid.Interpolate(ref eventList);
                WritePDF(ref eventGrid);
            }
        }

        private void PerformKMeans()
        {
            stationList.Clear();
            KMeansResults<OHCAEvent> stations = KMeans.Cluster<OHCAEvent>(eventList.ToArray(), targetStationCount, Utils.KMEANS_ITERATION_COUNT);
            foreach (double[] d in stations.Means)
            {
                stationList.Add(new Station(d[0], d[1], 2));
            }
        }

        private void PerformPulver()
        {
            stationList.Clear();
            for (int i = 0; i < eventGrid.cells.Count; i++)
            {
                stationList.Add(new Station(eventGrid.cells[i].kiloX, eventGrid.cells[i].kiloY, 0));
            }
            Pulver pulver = new Pulver(0.2, targetStationCount, 2, ref stationList, ref eventGrid);
        }

        private void PerformBoutilier()
        {
            stationList.Clear();
            bool stationsSet = !File.Exists("Boutilier_stations.csv");

            if (stationsSet)
            {
                StreamReader file = new StreamReader("Boutilier_stations.csv");
                string line;
                while ((line = file.ReadLine()) != null)
                {
                    string[] parts = line.Split(',');
                    stationList.Add(new Station(double.Parse(parts[0]), double.Parse(parts[1]), int.Parse(parts[2])));
                }
                file.Close();
            }
            else
            {
                for (int i = 0; i < eventGrid.cells.Count; i++)
                {
                    stationList.Add(new Station(eventGrid.cells[i].kiloX, eventGrid.cells[i].kiloY, 0));
                }
            }
            Boutilier boutilier = new Boutilier(stationsSet, ref stationList, ref eventList, 100, 0.9999999);

            if (!stationsSet)
            {
                StreamWriter file = new StreamWriter("Boutilier_stations.csv");
                for (int i = 0; i < stationList.Count; i++)
                {
                    file.Write(stationList[i].kiloX);
                    file.Write(",");
                    file.Write(stationList[i].kiloY);
                    file.Write(",");
                    file.Write(stationList[i].droneList.Count);
                    file.Write("\n");
                }
                file.Close();
            }
        }

        private void PerformRUBIS()
        {
            if (simulator == null)
            {
                simulator = new Simulator();
            }

            RUBIS rubis = new RUBIS(eventGrid, simulator, targetStationCount, targetStationCount * 2); 
            List<RubisStation> resultList = rubis.Calculate();

            stationList.Clear();
            foreach (RubisStation s in resultList)
            {
                stationList.Add(s);
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            ResizeCanvas();
        }

        private void ResizeCanvas()
        {
            Bitmap tmp = new Bitmap(this.Width, this.Height, PixelFormat.Format32bppRgb);
            using (Graphics g = Graphics.FromImage(tmp))
            {
                g.Clear(Color.White);
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                if (_canvas != null)
                {
                    g.DrawImage(_canvas, 0, 0);
                    _canvas.Dispose();
                }
            }
            _canvas = tmp;
        }

        private void MainForm_Paint(object sender, PaintEventArgs e)
        {
            using (Graphics g = Graphics.FromImage(_canvas))
            {
                g.Clear(Color.White);
                DrawGrid(g);
                DrawMap(g);
                DrawOHCAEvents(g);
                DrawStations(g);
                e.Graphics.DrawImage(_canvas, 0, 0);
            }
        }

        private void DrawGrid(Graphics g)
        {
            int numXCells = (int)Math.Ceiling(Utils.SEOUL_WIDTH / Utils.UNIT) * 10;
            int numYCells = (int)Math.Ceiling(Utils.SEOUL_HEIGHT / Utils.UNIT) * 10;

            Pen pLight = new Pen(Color.LightGray, 1);
            Pen pDark = new Pen(Color.DimGray, 1);
            for (int x = 0; x <= numXCells; ++x)
            {
                int xInt = (int)(x * Utils.UNIT / 10 / Utils.SEOUL_WIDTH * this.Width);
                if ((x + 5) % 50 == 0)
                {
                    g.DrawLine(pDark, xInt, 0, xInt, this.Height);
                }
                else
                {
                    g.DrawLine(pLight, xInt, 0, xInt, this.Height);
                }
            }

            for (int y = 0; y <= numYCells; ++y)
            {
                int yInt = this.Height - (int)(y * Utils.UNIT / 10 / Utils.SEOUL_HEIGHT * this.Height);
                if ((y + 5) % 50 == 0)
                {
                    g.DrawLine(pDark, 0, yInt, this.Width, yInt);
                }
                else
                {
                    g.DrawLine(pLight, 0, yInt, this.Width, yInt);
                }
            }
        }

        private void DrawMap(Graphics g)
        {
            Pen p = new Pen(Color.Green, 1);
            foreach (Polygon polygon in polygonList)
            {
                for (int i = 0; i < polygon.Points.Count; i++)
                {
                    if (i < polygon.Points.Count - 1)
                    {
                        g.DrawLine(p, (float)polygon.Points[i].X, (float)polygon.Points[i].Y, (float)polygon.Points[i + 1].X, (float)polygon.Points[i + 1].Y);
                    }
                    else
                    {
                        g.DrawLine(p, (float)polygon.Points[i].X, (float)polygon.Points[i].Y, (float)polygon.Points[0].X, (float)polygon.Points[0].Y);
                    }
                }
            }
        }

        private void DrawStations(Graphics g)
        {
            if (stationList != null)
            {
                foreach (Station s in stationList)
                {
                    g.FillEllipse(new SolidBrush(Color.FromArgb(64, 255, 0, 0)), s.pixelX - coverRange, s.pixelY - coverRange, coverRange + coverRange, coverRange + coverRange);
                    g.DrawEllipse(new Pen(Color.Red, 1), s.pixelX - coverRange, s.pixelY - coverRange, coverRange + coverRange, coverRange + coverRange);
                    g.FillRectangle((Brush)Brushes.Red, s.pixelX, s.pixelY, 3, 3);
                }
            }
        }

        private void DrawOHCAEvents(Graphics g)
        {
            foreach (OHCAEvent e in eventList)
            {
                g.FillRectangle((Brush)Brushes.Blue, e.pixelX, e.pixelY, 3, 3);
            }
        }

        private void ReadEventData()
        {
            Excel.Application excelApp = null;
            Excel.Workbook wb = null;
            Excel.Worksheet ws = null;

            try
            {
                excelApp = new Excel.Application();
                wb = excelApp.Workbooks.Open(Environment.CurrentDirectory.ToString() + "\\data.xls");
                ws = wb.Worksheets.get_Item(1) as Excel.Worksheet;
                Excel.Range rng = ws.UsedRange;
                // Excel.Range rng = ws.Range[ws.Cells[2, 1], ws.Cells[5, 3]];

                object[,] data = rng.Value;
                for (int r = 2; r <= data.GetLength(0); r++)
                {
                    try
                    {
                        double kiloX = Utils.LonToKilos(float.Parse(data[r, 16].ToString()));
                        double kiloY = Utils.LatToKilos(float.Parse(data[r, 15].ToString()));
                        DateTime occurrenceTime = DateTime.Parse(data[r, 19].ToString());
                        OHCAEvent e = new OHCAEvent(kiloX, kiloY, occurrenceTime);
                        eventList.Add(e);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                }
                wb.Close(true);
                excelApp.Quit();
            }
            finally
            {
                ReleaseExcelObject(ws);
                ReleaseExcelObject(wb);
                ReleaseExcelObject(excelApp);
            }
        }

        private void ReadMapData()
        {
            Excel.Application excelApp = null;
            Excel.Workbook wb = null;
            Excel.Worksheet ws = null;

            try
            {
                excelApp = new Excel.Application();
                wb = excelApp.Workbooks.Open(Environment.CurrentDirectory.ToString() + "\\seoul.xls");
                ws = wb.Worksheets.get_Item(1) as Excel.Worksheet;
                Excel.Range rng = ws.UsedRange;

                object[,] data = rng.Value;

                int r = 1;
                for (int i = 0; i < 25; i++)
                {
                    Polygon p = new Polygon();
                    p.Name = data[r, 2].ToString().Replace("-", "");
                    System.Windows.Media.PointCollection pc = new System.Windows.Media.PointCollection();
                    List<double[]> pList = new List<double[]>();
                    r++;

                    for (int j = r; j <= data.GetLength(0); j++)
                    {
                        try
                        {
                            if ((data[j, 1].ToString().Equals("name") && pc.Count != 0) || j == data.GetLength(0))
                            {
                                p.Points = pc;
                                polygonList.Add(p);
                                polyCoordList.Add(pList);
                                r = j;
                                break;
                            }
                            else
                            {
                                float lon = float.Parse(data[j, 1].ToString());
                                float lat = float.Parse(data[j, 2].ToString());
                                double[] coord = new double[2];
                                coord[0] = Utils.LonToKilos(lon);
                                coord[1] = Utils.LatToKilos(lat);
                                int pixelX = Utils.TransformKiloXToPixel(coord[0]);
                                int pixelY = Utils.TransformKiloYToPixel(coord[1]);
                                pc.Add(new System.Windows.Point(pixelX, pixelY));
                                pList.Add(coord);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                        }
                    }
                }

                wb.Close(true);
                excelApp.Quit();
            }
            finally
            {
                ReleaseExcelObject(ws);
                ReleaseExcelObject(wb);
                ReleaseExcelObject(excelApp);
            }
        }

        private void ReadPDF(ref Grid grid)
        {
            StreamReader sr = new StreamReader("pdf.csv");
            String line = sr.ReadLine();
            while (line != null)
            {
                string[] cells = line.Split(',');
                int n = grid.lambda[0].Length;
                for (int i = 0; i < cells.Length - 1; i++)
                {
                    grid.lambda[i / n][i % n] = (double) Double.Parse(cells[i]);
                }
                line = sr.ReadLine();
            }
            sr.Close();
        }

        private void WritePDF(ref Grid grid)
        {
            StreamWriter file = new StreamWriter("pdf.csv");
            for (int i = 0; i < grid.lambda.Length; i++)
            {
                for (int j = 0; j < grid.lambda[i].Length; j++)
                {
                    file.Write(grid.lambda[i][j]);
                    file.Write(",");
                }
            }
            file.Write("\n");
            file.Close();
        }

        private static void ReleaseExcelObject(object obj)
        {
            try
            {
                if (obj != null)
                {
                    Marshal.ReleaseComObject(obj);
                    obj = null;
                }
            }
            catch (Exception ex)
            {
                obj = null;
                throw ex;
            }
            finally
            {
                GC.Collect();
            }
        }

        private void ClickExit(object sender, EventArgs e)
        {
            if (Application.MessageLoop)
            {
                Application.Exit();
            }
            else
            {
                Environment.Exit(1);
            }
        }

        private void ClickPlacementItems(object sender, EventArgs e)
        {
            kMeansToolStripMenuItem.CheckState = CheckState.Unchecked;
            pulverToolStripMenuItem.CheckState = CheckState.Unchecked;
            boutilierToolStripMenuItem.CheckState = CheckState.Unchecked;
            rubisToolStripMenuItem.CheckState = CheckState.Unchecked;

            ToolStripMenuItem item = (ToolStripMenuItem)sender;
            item.CheckState = CheckState.Checked;
            item.Checked = true;

            switch (item.Text)
            {
                case "K-Means":
                    PerformKMeans();
                    break;
                case "Pulver":
                    PerformPulver();
                    break;
                case "Boutilier":
                    PerformBoutilier();
                    break;
                case "RUBIS":
                    PerformRUBIS();
                    break;
                default:
                    break;
            }
            this.Invalidate();
        }

        private void ClickRunSimulation(object sender, EventArgs e)
        {
            Policy policy = (toolStripComboBoxPolicy.SelectedIndex == 0)
                ? Policy.NearestStationFirst : Policy.HighestSurvivalRateStationFirst;

            if (writeSimulation)
            {
                WriteSimulationEventList(eventGrid);
            }
            else
            {
                if (simulator == null)
                {
                    simulator = new Simulator();
                }
                simulator.SetPolicy(policy);
                simulator.Simulate(stationList, eventGrid);

                Console.WriteLine(simulator.GetExpectedSurvivalRate());
                Console.WriteLine("Total Miss Count = " + simulator.GetMissCount());

                labelOverallSurvivalRateValue.Text = simulator.GetExpectedSurvivalRate() * 100 + "%";
                double rate = (double)simulator.GetMissCount() / (double)simulator.GetSimulatedEventsCount() * 100.0;
                labelDeliveryMissValue.Text = simulator.GetMissCount().ToString() + " / " + simulator.GetSimulatedEventsCount() + " (" + rate + "%)";

                this.Invalidate();
            }
        }

        private void toolStripComboBoxStations_SelectedIndexChanged(object sender, EventArgs e)
        {
            targetStationCount = Int32.Parse(((ToolStripComboBox)sender).Text);
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Load a placement file";
            ofd.FileName = "";
            ofd.Filter = "Text File (*.txt) | *.txt; | All Files (*.*) | *.*";

            DialogResult dr = ofd.ShowDialog();
            string fileFullName = "";
            if (dr == DialogResult.OK)
            {
                string fileName = ofd.SafeFileName;
                fileFullName = ofd.FileName;
                string filePath = fileFullName.Replace(fileName, "");
            }
            else if (dr == DialogResult.Cancel)
            {
                return;
            }

            List<Station> tempList = new List<Station>();
            StreamReader objReader = new StreamReader(fileFullName);
            string line = "";
            try
            {
                while (line != null)
                {
                    line = objReader.ReadLine();
                    if (line != null)
                    {
                        string[] data = line.Split('\t');
                        double kiloX = Double.Parse(data[0]);
                        double kiloY = Double.Parse(data[1]);
                        int drones = int.Parse(data[2]);
                        tempList.Add(new Station(kiloX, kiloY, drones));
                    }
                }
                objReader.Close();
            }
            catch (Exception)
            {
                return;
            }

            stationList.Clear();
            stationList.AddRange(tempList);

            toolStripComboBoxStations.SelectedIndex = stationList.Count - 8;
            targetStationCount = stationList.Count;

            this.Invalidate();
        }

        private void savePlacementToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (stationList.Count == 0)
            {
                MessageBox.Show("There are no stations.", "Save File", MessageBoxButtons.OK);
                return;
            }

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Text File (*.txt) | *.txt; | All Files (*.*) | *.*";
            sfd.Title = "Save a placement";
            sfd.ShowDialog();

            // If the file name is not an empty string open it for saving.  
            if (sfd.FileName != "")
            {
                using (StreamWriter file = new StreamWriter(sfd.FileName))
                {
                    foreach (Station s in stationList)
                    {
                        file.WriteLine(String.Format("{0}\t{1}\t{2}", s.kiloX, s.kiloY, s.droneList.Count));
                    }
                }
            }
        }

        private void WriteSimulationEventList(Grid eventGrid)
        {
            BackgroundWorker[] workers = new BackgroundWorker[coreCount];
            int dividedLoad = eventGrid.lambda.Length / coreCount;
            int rem = eventGrid.lambda.Length % coreCount;
            this.workersRemaining = coreCount;
            int len = eventGrid.lambda[0].Length;

            this.numEvents = 0;
            int row = 0;
            for (int i = 0; i < workers.Length; i++)
            {
                int actualLoad = dividedLoad + ((i < rem) ? 1 : 0);
                workers[i] = new BackgroundWorker();
                double[][] workLoad = new double[actualLoad][];

                for (int j = 0; j < actualLoad; j++)
                {
                    workLoad[j] = new double[len];
                    Array.Copy(eventGrid.lambda[row + j], workLoad[j], len);
                }

                WorkObject work = new WorkObject(workLoad, i, row);

                workers[i].DoWork += eventList_DoWork;
                workers[i].RunWorkerCompleted += eventList_RunWorkerCompleted;
                row += actualLoad;
                workers[i].RunWorkerAsync(work);
            }

            while (workersRemaining > 0)
            {
            }
        }

        private void eventList_DoWork(object sender, DoWorkEventArgs e)
        {
            
            WorkObject workObject = e.Argument as WorkObject;
            System.Console.WriteLine(workObject.index);

            DateTime currentTime = new DateTime(2018, 1, 1);
            Random rand = new Random((int) DateTime.Now.ToBinary() + workObject.index);

            StreamWriter file = new StreamWriter("simulationEvents_" + workObject.index + ".csv");

            bool keepWorking = true;
            while (keepWorking)
            {
                currentTime = currentTime.AddMinutes(1.0);
                for (int i = 0; i < workObject.lambda.Length; i++)
                {
                    for (int j = 0; j < workObject.lambda[i].Length; j++)
                    {
                        double randVal = rand.NextDouble();
                        if (randVal < workObject.lambda[i][j])
                        {
                            bool lockTaken = false;
                            while (!lockTaken)
                            {
                                this.numEventLock.TryEnter(ref lockTaken);
                            }

                            if (this.numEvents >= Utils.SIMULATION_EVENTS)
                            {
                                keepWorking = false;
                                this.numEventLock.Exit();
                                break;
                            }

                            this.numEvents++;
                            if (this.numEvents % 10000 == 0)
                            {
                                Console.WriteLine(this.numEvents);
                            }
                            this.numEventLock.Exit();

                            file.Write((j + 0.5) * Utils.LAMBDA_PRECISION);
                            file.Write(",");
                            file.Write((workObject.row + i + 0.5) * Utils.LAMBDA_PRECISION);
                            file.Write(",");
                            String s = string.Format("{0 : yyyy MM dd HH mm ss}", currentTime);
                            file.Write(currentTime);
                            file.Write("\n");
                        }
                    }

                    if (!keepWorking)
                    {
                        break;
                    }
                }
            }
            Console.WriteLine("thread " + workObject.index + " done.");
            file.Close();
        }

        private void eventList_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
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
            public double[][] lambda;
            public int index;
            public int row;
            public WorkObject(double[][] lambda, int index, int row)
            {
                this.lambda = lambda.Clone() as double[][];
                this.index = index;
                this.row = row;
            }
        }
    }   
}
