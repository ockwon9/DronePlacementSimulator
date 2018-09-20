using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Shapes;
using Excel = Microsoft.Office.Interop.Excel;

namespace DronePlacementSimulator
{
    public partial class MainForm : Form
    {
        
        private static float MIN_LONGITUDE = 126.7645806f;
        private static float MAX_LONGITUDE = 127.1831312f;
        private static float MIN_LATITUDE = 37.42834757f;
        private static float MAX_LATITUDE = 37.70130154f;

        private static double UNIT = 0.04f;

        private static int COVER_RANGE = 180;

        List<Station> stationList;
        List<OHCAEvent> eventList;
        List<Polygon> polygonList;

        private Bitmap _canvas;
        private Point _anchor; //The start point for click-drag operations
        private bool flag = false;
        private Brush _ghostBrush;

        public MainForm()
        {
            InitializeComponent();

            stationList = new List<Station>();
            eventList = new List<OHCAEvent>();
            polygonList = new List<Polygon>();

            // Set ths simulator's window size
            this.Height = Screen.PrimaryScreen.Bounds.Height;
            this.Width = (int)(this.Height * (MAX_LONGITUDE - MIN_LONGITUDE) / (MAX_LATITUDE - MIN_LATITUDE));
            
            // Read OHCA events data
            ReadEventData();
            ReadMapData();

            // Create grid with distribution
            Grid grid = new Grid(MIN_LATITUDE, MIN_LONGITUDE, MAX_LATITUDE, MAX_LONGITUDE, UNIT, ref eventList, ref polygonList);

            foreach (double[] coord in grid.cells)
            {
                stationList.Add(new Station(coord[0], coord[1]));
            }

            Pulver pulver = new Pulver(0.2, 30, 2, 5, ref stationList, ref grid);
            // Rubis rubis = new Rubis(MIN_LATITUDE, MIN_LONGITUDE, MAX_LATITUDE, MAX_LONGITUDE, 100, 100, ref eventList, ref stationList);

            // KMeansResults<OHCAEvent> stations = KMeans.Cluster<OHCAEvent>(eventList.ToArray(), 50, 100);

            /*
            foreach(double[] d in stations.Means)
            {
                Station s = new Station();
                s.latitude = d[0];
                s.longitude = d[1];
                s.x = transformLongitudeToInt(s.longitude);
                s.y = transformLatitudeToInt(s.latitude);
                for (int i = 0; i < 10; i++)
                {
                    Drone drone = new Drone(s.stationID);
                    s.droneList.Add(drone);
                }
                if (!stationList.Contains(s))
                {
                    stationList.Add(s);
                }
            }
            */

            Del defaultPolicy = NearestStation;
            Test pulverTest = new Test(ref stationList, ref eventList, defaultPolicy);
            Console.WriteLine(pulverTest.getExpectedSurvivalRate());
            
            /*
            Del defaultPolicy = NearestStation;
            Test kMeansTest = new Test(ref stationList, ref eventList, defaultPolicy);
            Console.WriteLine(kMeansTest.expectedSurvivalRate);
            */
        }

        public double Distance(double x1, double y1, double x2, double y2)
        {
            return Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));
        }

        private int NearestStation(ref List<Station> stationList, OHCAEvent ohca)
        {
            int n = stationList.Count;

            double min = Double.PositiveInfinity;
            int nearest = -1;
            for (int i = 0; i < n; i++)
            {
                Station s = stationList[i];
                double distance = Distance(s.latitude, s.longitude, ohca.latitude, ohca.longitude);
                if (distance < min)
                {
                    min = distance;
                    nearest = i;
                }
            }

            /*
            Console.WriteLine("sx : " + stationList[nearest].latitude);
            Console.WriteLine("sy : " + stationList[nearest].longitude);
            Console.WriteLine("ox : " + ohca.latitude);
            Console.WriteLine("oy : " + ohca.longitude);
            Console.WriteLine(ohca.occurrenceTime);
            Console.WriteLine(nearest);*/

            return nearest;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            resizeCanvas();
        }

        private void resizeCanvas()
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
                drawGrid(g);
                drawMap(g);
                drawOHCAEvents(g);
                drawStations(g);

                flag = true;
                e.Graphics.DrawImage(_canvas, 0, 0);
            }
        }
       
        private void drawGrid(Graphics g)
        {
            int numXCells = (int) Math.Ceiling((MAX_LONGITUDE - MIN_LONGITUDE) / UNIT);
            int numYCells = (int) Math.Ceiling((MAX_LATITUDE - MIN_LATITUDE) / UNIT);
            int cellWidth = this.Width / numXCells;
            int cellHeight = this.Height / numYCells;

            Pen p = new Pen(Color.LightGray, 1);
            for (int x = 0; x <= numXCells; ++x)
            {
                g.DrawLine(p, x * cellWidth, 0, x * cellWidth, this.Height);
            }

            for (int y = 0; y <= numYCells; ++y)
            {
                g.DrawLine(p, 0, y * cellHeight, this.Width, y * cellHeight);
            }
        }

        private void drawMap(Graphics g)
        {
            Pen p = new Pen(Color.Green, 1);
            foreach(Polygon polygon in polygonList)
            {
                for (int i = 0; i < polygon.Points.Count; i++)
                {
                    if(i < polygon.Points.Count-1)
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

        private void drawStations(Graphics g)
        {
            foreach (Station s in stationList)
            {
                g.FillEllipse(new SolidBrush(Color.FromArgb(64, 255, 0, 0)), s.x - COVER_RANGE, s.y - COVER_RANGE, COVER_RANGE + COVER_RANGE, COVER_RANGE + COVER_RANGE);
                g.DrawEllipse(new Pen(Color.Red, 1), s.x - COVER_RANGE, s.y - COVER_RANGE, COVER_RANGE + COVER_RANGE, COVER_RANGE + COVER_RANGE);
                g.FillRectangle((Brush)Brushes.Red, s.x, s.y, 3, 3);
            }
        }

        private void drawOHCAEvents(Graphics g)
        {
            foreach (OHCAEvent e in eventList)
            {
                g.FillRectangle((Brush)Brushes.Blue, e.x, e.y, 3, 3);
            }
        }

        private int transformLatitudeToInt(double latitude)
        {
            double latitudeRatio = (latitude - MIN_LATITUDE) / (MAX_LATITUDE - MIN_LATITUDE);
            return this.Height - (int)(this.Height * latitudeRatio);
        }

        private int transformLongitudeToInt(double longitude)
        {
            double longitudeRatio = (longitude - MIN_LONGITUDE) / (MAX_LONGITUDE - MIN_LONGITUDE);
            return this.Width - (int)(this.Width * longitudeRatio);
        }

        public void ReadEventData()
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
                        OHCAEvent e = new OHCAEvent();
                        e.latitude = float.Parse(data[r, 15].ToString());
                        e.longitude = float.Parse(data[r, 16].ToString());
                        e.occurrenceTime = DateTime.Parse(data[r, 19].ToString());
                        e.x = transformLongitudeToInt(e.longitude);
                        e.y = transformLatitudeToInt(e.latitude);
                        eventList.Add(e);

                        // Location of stations that dispatched the ambulance to OHCA patients
                        /*Station s = new Station();
                        s.latitude = float.Parse(data[r, 17].ToString());
                        s.longitude = float.Parse(data[r, 18].ToString());
                        s.x = transformLongitudeToInt(s.longitude);
                        s.y = transformLatitudeToInt(s.latitude);
                        if (!stationList.Contains(s))
                        {
                            stationList.Add(s);
                        }
                        */
                    }
                    catch (Exception ex)
                    {

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


        public void ReadMapData()
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
                    r++;

                    for (int j = r; j <= data.GetLength(0); j++)
                    {
                        try
                        {
                            if ((data[j, 1].ToString().Equals("name") && pc.Count != 0) || j == data.GetLength(0))
                            {
                                p.Points = pc;
                                polygonList.Add(p);
                                r = j;
                                break;
                            }
                            else
                            {
                                float lon = float.Parse(data[j, 1].ToString());
                                float lat = float.Parse(data[j, 2].ToString());
                                int pLon = transformLongitudeToInt(lon);
                                int pLat = transformLatitudeToInt(lat);
                                pc.Add(new System.Windows.Point(pLon, pLat));
                            }
                        }
                        catch (Exception ex)
                        {

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
    }
}
