using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Shapes;
using Nito.AsyncEx;
using Excel = Microsoft.Office.Interop.Excel;

namespace DronePlacementSimulator
{
    public partial class MainForm : Form
    {
        private bool writeSimulation = false;
        
        private List<Station> stationList;
        private List<OHCAEvent> eventList;
        private List<Polygon> polygonList;
        private List<List<GeoCoordinate>> polyCoordList;
        List<DispatchFailure> failedEventList;

        private Simulator simulator = null;
        private Grid eventGrid = null;
        private Bitmap _canvas = null;
        private int targetStationCount;
        private bool placedStations = false;

        public int coverRange = 0;
        
        public MainForm()
        {
            InitializeComponent();

            stationList = new List<Station>();
            eventList = new List<OHCAEvent>();
            polygonList = new List<Polygon>();
            polyCoordList = new List<List<GeoCoordinate>>();
            failedEventList = new List<DispatchFailure>();

            // Set the size of simulator's window
            this.Height = Screen.PrimaryScreen.Bounds.Height;
            this.Width = (int)(this.Height * Utils.SEOUL_WIDTH / Utils.SEOUL_HEIGHT);
            coverRange = (int)(this.Height * (Utils.GOLDEN_TIME * Utils.DRONE_VELOCITY) / Utils.SEOUL_HEIGHT);
            toolStripComboBoxStations.SelectedIndex = 12;
            toolStripComboBoxBudget.SelectedIndex = 0;
            targetStationCount = 20;

            // Read OHCA events data
            ReadEventData();
            ReadMapData();

            //simulator = new Simulator();

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
            int numStations = int.Parse(toolStripComboBoxStations.SelectedItem.ToString());
            KMeansResults<OHCAEvent> stations = KMeans.Cluster<OHCAEvent>(eventList.ToArray(), numStations, 100);
            double[][] results = stations.Means.Clone() as double[][];
            for (int j = 0; j < numStations; j++)
            {
                stationList.Add(new Station(results[j][0], results[j][1], 1));
            }
            /*
             * Find the best number of stations for the given budget
             * 
            int budget = int.Parse(toolStripComboBoxBudget.SelectedItem.ToString());
            double maxSurvivalRate = 0.0;

            for (int i = 1; i <= budget / (Utils.DRONE_PRICE + Utils.STATION_PRICE); i++)
            {
                KMeansResults<OHCAEvent> stations = KMeans.Cluster<OHCAEvent>(eventList.ToArray(), i, 100);
                List<Station> tempStationList = new List<Station>();
                double[][] results = stations.Means.Clone() as double[][];
                int numStations = results.Length;
                int numDrones = (budget - Utils.STATION_PRICE * numStations) / Utils.DRONE_PRICE;
                int fewer = numDrones / numStations;
                int remainder = numDrones % numStations;
                for (int j = 0; j < numStations; j++)
                {
                    tempStationList.Add(new Station(results[j][0], results[j][1], fewer + (j < remainder ? 1 : 0)));
                }

                Simulator tempSimulator = new Simulator();
                tempSimulator.Simulate(tempStationList, eventGrid);
                double survivalRate = tempSimulator.GetExpectedSurvivalRate();

                if (survivalRate > maxSurvivalRate)
                {
                    maxSurvivalRate = survivalRate;
                    stationList.Clear();

                    foreach(Station s in tempStationList)
                    {
                        stationList.Add(s);
                    }
                }
            }
            */

            placedStations = true;
        }

        private void PerformPulver()
        {
            int budget = int.Parse(toolStripComboBoxBudget.SelectedItem.ToString());

            stationList.Clear();
            bool stationsSet = File.Exists("Pulver_stations_" + budget + ".csv");

            if (stationsSet)
            {
                StreamReader file = new StreamReader("Pulver_stations_" + budget + ".csv");
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
                StreamReader file = new StreamReader("Boutilier.csv");
                string line;
                while ((line = file.ReadLine()) != null)
                {
                    string[] parts = line.Split(',');
                    stationList.Add(new Station(double.Parse(parts[0]), double.Parse(parts[1]), 0));
                }
                file.Close();

                StreamWriter file2 = new StreamWriter("Pulver_stations_" + budget + ".csv");
                for (int i = 0; i < stationList.Count; i++)
                {
                    file2.Write(stationList[i].lat);
                    file2.Write(",");
                    file2.Write(stationList[i].lon);
                    file2.Write(",");
                    file2.Write(stationList[i].droneList.Count);
                    file2.Write("\n");
                }
                file2.Close();

                Console.WriteLine("targetStationCount = " + targetStationCount);
                Pulver pulver = new Pulver(0.2, targetStationCount, 2, stationList, eventGrid);
            }

            placedStations = true;
        }

        private void PerformBoutilier()
        {
            Console.WriteLine("space coverage constraint : ");
            double space = double.Parse(Console.ReadLine());
            Console.WriteLine("time coverage constraint : ");
            double time = double.Parse(Console.ReadLine());
            stationList.Clear();
            bool stationsSet = File.Exists("Boutilier_stations_" + space + "_" + time + ".csv");

            if (stationsSet)
            {
                StreamReader file = new StreamReader("Boutilier_stations_" + space + "_" + time + ".csv");
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
                StreamReader file = new StreamReader("Boutilier.csv");
                string line;
                while ((line = file.ReadLine()) != null)
                {
                    string[] parts = line.Split(',');
                    stationList.Add(new Station(double.Parse(parts[0]), double.Parse(parts[1]), 0));
                }
                file.Close();

                Boutilier boutilier = new Boutilier(ref stationList, ref eventList, space, time);
                
                StreamWriter file2 = new StreamWriter("Boutilier_stations_" + space + "_" + time + ".csv");
                for (int i = 0; i < stationList.Count; i++)
                {
                    file2.Write(stationList[i].lat);
                    file2.Write(",");
                    file2.Write(stationList[i].lon);
                    file2.Write(",");
                    file2.Write(stationList[i].droneList.Count);
                    file2.Write("\n");
                }
                file2.Close();
            }

            placedStations = true;
        }

        private void PerformRUBIS()
        {
            if (simulator == null)
            {
                simulator = new Simulator();
            }

            RUBIS rubis = new RUBIS(eventGrid, simulator, ref polyCoordList);
            int budget = int.Parse(toolStripComboBoxBudget.SelectedItem.ToString());
            List <RubisStation> resultList = rubis.Calculate(eventList, budget);

            stationList.Clear();
            foreach (RubisStation s in resultList)
            {
                stationList.Add(s);
            }

            placedStations = true;
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
                DrawFailedEvents(g);
                e.Graphics.DrawImage(_canvas, 0, 0);
            }
        }

        private void DrawGrid(Graphics g)
        {
            double unit = 1.0;
            int numXCells = (int)Math.Ceiling(Utils.SEOUL_WIDTH / unit) * 10;
            int numYCells = (int)Math.Ceiling(Utils.SEOUL_HEIGHT / unit) * 10;

            Pen pLight = new Pen(Color.LightGray, 1);
            Pen pDark = new Pen(Color.DimGray, 1);
            for (int x = 0; x <= numXCells; ++x)
            {
                int xInt = (int)(x * unit / 10 / Utils.SEOUL_WIDTH * this.Width);
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
                int yInt = this.Height - (int)(y * unit / 10 / Utils.SEOUL_HEIGHT * this.Height);
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
                    g.FillEllipse(new SolidBrush(Color.FromArgb(32, 255, 0, 0)), s.pixelCol - coverRange, s.pixelRow - coverRange, coverRange + coverRange, coverRange + coverRange);
                    g.DrawEllipse(new Pen(Color.Red, 1), s.pixelCol - coverRange, s.pixelRow - coverRange, coverRange + coverRange, coverRange + coverRange);
                    g.FillRectangle((Brush)Brushes.Red, s.pixelCol, s.pixelRow, 3, 3);
                    string stationInfo = stationList.IndexOf(s).ToString();
                    if (s.droneList.Count > 0)
                    {
                        stationInfo += " (" + s.droneList.Count + ")";
                    }
                    g.DrawString(stationInfo, new Font("Times New Roman", 24, FontStyle.Bold, GraphicsUnit.Pixel), Brushes.Black, new Point(s.pixelCol, s.pixelRow));
                }
            }
        }

        private void DrawOHCAEvents(Graphics g)
        {
            foreach (OHCAEvent e in eventList)
            {
                g.FillRectangle((Brush)Brushes.Blue, e.pixelCol, e.pixelRow, 3, 3);
            }
        }

        private void DrawFailedEvents(Graphics g)
        {
            foreach (DispatchFailure e in failedEventList)
            {
                if (e.failure == Utils.Failure.NO_DRONES)
                {
                    g.FillRectangle((Brush)Brushes.Lime, Utils.TransformLonToPixel(e.lon), Utils.TransformLatToPixel(e.lat), 3, 3);
                }
                else if (e.failure == Utils.Failure.UNREACHABLE)
                {
                    g.FillRectangle((Brush)Brushes.Cyan, Utils.TransformLonToPixel(e.lon), Utils.TransformLatToPixel(e.lat), 3, 3);
                }
                
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
                    { // TODO
                        double lat = double.Parse(data[r, 15].ToString());
                        double lon = double.Parse(data[r, 16].ToString());
                        DateTime occurrenceTime = DateTime.Parse(data[r, 19].ToString());
                        OHCAEvent e = new OHCAEvent(lat, lon, occurrenceTime);
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
                    List<GeoCoordinate> pList = new List<GeoCoordinate>();
                    r++;

                    for (int j = r; j <= data.GetLength(0); j++)
                    {
                        try
                        {
                            if ((data[j, 2].ToString().Equals("name") && pc.Count != 0) || j == data.GetLength(0))
                            {
                                p.Points = pc;
                                polygonList.Add(p);
                                polyCoordList.Add(pList);
                                r = j;
                                break;
                            }
                            else
                            {
                                float lat = float.Parse(data[j, 1].ToString());
                                float lon = float.Parse(data[j, 2].ToString());
                                int pixelRow = Utils.TransformLatToPixel(lat);
                                int pixelCol = Utils.TransformLonToPixel(lon);
                                pc.Add(new System.Windows.Point(pixelCol, pixelRow));
                                pList.Add(new GeoCoordinate(lat, lon));
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
            int row = 0;
            while (line != null)
            {
                string[] cells = line.Split(',');
                for (int col = 0; col < Utils.COL_NUM; col++)
                {
                    grid.lambda[row, col] = Double.Parse(cells[col]);
                }
                row++;
                line = sr.ReadLine();
            }
            sr.Close();
        }

        private void WritePDF(ref Grid grid)
        {
            StreamWriter file = new StreamWriter("pdf.csv");
            for (int i = 0; i < Utils.ROW_NUM; i++)
            {
                for (int j = 0; j < Utils.COL_NUM; j++)
                {
                    file.Write(grid.lambda[i, j]);
                    file.Write(",");
                }
                file.Write("\n");
            }
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

        private void SaveLastStations()
        {
            StreamWriter file = new StreamWriter("Last.csv");
            for (int i = 0; i < stationList.Count; i++)
            {
                file.Write(stationList[i].lat);
                file.Write(',');
                file.Write(stationList[i].lon);
                file.Write(',');
                file.Write(stationList[i].droneList.Count);
                file.Write("\n");
            }
            file.Close();
        }

        private void LoadLastStations()
        {
            StreamReader file = new StreamReader("Last.csv");
            String line = file.ReadLine();
            while (line != null)
            {
                string[] split = line.Split(',');
                stationList.Add(new Station(double.Parse(split[0]), double.Parse(split[1]), int.Parse(split[2])));
                line = file.ReadLine();
            }
            file.Close();
        }

        private void ClickRunSimulation(object sender, EventArgs e)
        {
            if (!placedStations)
            {
                LoadLastStations();
            }
            else
            {
                SaveLastStations();
            }

            if (writeSimulation)
            {
                WriteSimulationEventList();
            }
            else
            {
                if (simulator == null)
                {
                    simulator = new Simulator();
                }
                failedEventList.Clear();
                failedEventList = simulator.Simulate(stationList, eventGrid);

                Console.WriteLine(simulator.GetExpectedSurvivalRate());
                Console.WriteLine("Total Unreachable Events = " + simulator.GetUnreachableEvents());
                Console.WriteLine("Total No Drones Events = " + simulator.GetNoDrones());

                labelOverallSurvivalRateValue.Text = simulator.GetExpectedSurvivalRate() * 100 + "%";
                double rate = (double)simulator.GetUnreachableEvents() / (double)simulator.GetSimulatedEventsCount() * 100.0;
                labelDeliveryMissValue.Text = simulator.GetUnreachableEvents().ToString() + " / " + simulator.GetSimulatedEventsCount() + " (" + rate + "%)";

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
                        double lat = Double.Parse(data[0]);
                        double lon = Double.Parse(data[1]);
                        int drones = int.Parse(data[2]);
                        tempList.Add(new Station(lat, lon, drones));
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
                        file.WriteLine(String.Format("{0}\t{1}\t{2}", s.lat, s.lon, s.droneList.Count));
                    }
                }
            }
        }

        private class WorkObject
        {
            public int index;
            public DateTime begin;
            public Grid grid;

            public WorkObject(int index, DateTime begin, Grid grid)
            {
                this.index = index;
                this.begin = begin;
                this.grid = grid;
            }
        }

        private void WriteEventsDoWork(WorkObject workObject)
        {
            DateTime end = workObject.begin.AddYears(20);
            DateTime current = workObject.begin;
            Random rand = new Random((int)DateTime.Now.ToBinary() + Utils.SIMULATION_EVENTS * workObject.index);
            StreamWriter file = new StreamWriter("simulationEvents_" + workObject.index + ".csv");
            int eventCount = 0;

            while (current < end)
            {
                for (int i = 0; i < Utils.ROW_NUM; i++)
                {
                    for (int j = 0; j < Utils.COL_NUM; j++)
                    {
                        if (!workObject.grid.inSeoul[i, j])
                            continue;

                        double randVal = rand.NextDouble();
                        if (randVal < workObject.grid.lambda[i, j])
                        {
                            file.Write(Utils.MIN_LATITUDE + (i + 0.5) * Utils.LAT_UNIT);
                            file.Write(",");
                            file.Write(Utils.MIN_LONGITUDE + (j + 0.5) * Utils.LON_UNIT);
                            file.Write(",");
                            file.Write(current.ToString());
                            file.Write("\n");

                            eventCount++;
                        }
                    }
                }
                current = current.AddMinutes(1);
            }
            file.Close();
        }

        private async Task WriteEvents()
        {
            int[] quotientAndRemainder = new int[2];
            int coreCount = 12;
            Task[] tasks = new Task[coreCount];
            DateTime startDate = new DateTime(2019, 1, 1);
            
            for (int i = 0; i < coreCount; i++, startDate = startDate.AddYears(20))
            {
                WorkObject workObject = new WorkObject(i, startDate, eventGrid);
                tasks[i] = Task.Run(() => WriteEventsDoWork(workObject));
            }

            await Task.WhenAll(tasks);
        }

        private void WriteSimulationEventList()
        {
            AsyncContext.Run(() => WriteEvents());
        }
        
        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            Random rand = new Random();
            int pos = rand.Next(0, 990000);

            Console.WriteLine("pos = " + pos);

            List<OHCAEvent> temp = simulator.GetSimulatedEvents().GetRange(pos, 10000);
            CloneList(temp, eventList);
            this.Invalidate();
        }

        private void CloneList(List<OHCAEvent> srcList, List<OHCAEvent> dstList)
        {
            dstList.Clear();
            srcList.ForEach((item) =>
            {
                dstList.Add(new OHCAEvent(item));
            });
        }
    }   
}
