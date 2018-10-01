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
        private static double MIN_LONGITUDE = 126.7645806f;
        private static double MAX_LONGITUDE = 127.1831312f;
        private static double MIN_LATITUDE = 37.42834757f;
        private static double MAX_LATITUDE = 37.70130154f;
        private static double SEOUL_WIDTH = 36.89;
        private static double SEOUL_HEIGHT = 30.35;

        private static double UNIT = 3.0;
        private static double GOLDEN_TIME = 6.36;
        private int coverRange;
        private Grid grid;

        List<Station> stationList;
        List<OHCAEvent> eventList;
        List<Polygon> polygonList;
        List<List<double[]>> polyCoordList;

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
            polyCoordList = new List<List<double[]>>();

            // Set ths simulator's window size
            this.Height = Screen.PrimaryScreen.Bounds.Height;
            this.Width = (int)(this.Height * SEOUL_WIDTH / SEOUL_HEIGHT);
            coverRange = (int)(this.Height * 5 / SEOUL_HEIGHT);
            
            // Read OHCA events data
            ReadEventData();
            ReadMapData();

            // Create grid with distribution
            grid = new Grid(0.0, 0.0, SEOUL_WIDTH, SEOUL_HEIGHT, UNIT, ref eventList, ref polyCoordList);

            // Draw 
            foreach (double[] coord in grid.cells)
            {
                Station s = new Station(coord[0] + 0.5 * UNIT, coord[1] + 0.5 * UNIT);
                s.x = transformLongitudeToInt(s.longitude);
                s.y = transformLatitudeToInt(s.latitude);
                stationList.Add(s);
            }         
        }

        private void doPulver()
        {
            Pulver pulver = new Pulver(0.2, 30, 2, 5, ref stationList, ref grid);
            Del defaultPolicy = NearestStation;
            Test pulverTest = new Test(ref stationList, ref eventList, defaultPolicy);
            Console.WriteLine(pulverTest.getExpectedSurvivalRate());
        }

        private void doRubis()
        {
            //Rubis rubis = new Rubis(MIN_LATITUDE, MIN_LONGITUDE, MAX_LATITUDE, MAX_LONGITUDE, 100, 100, ref eventList, ref stationList);
            Del defaultPolicy = NearestStation;
            Test rubisTest = new Test(ref stationList, ref eventList, defaultPolicy);
            Console.WriteLine(rubisTest.getExpectedSurvivalRate());
        }

        private void doKMeans()
        {
            KMeansResults<OHCAEvent> stations = KMeans.Cluster<OHCAEvent>(eventList.ToArray(), 50, 100);
            foreach (double[] d in stations.Means)
            {
                Station s = new Station(d[1], d[0]);
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
            Del defaultPolicy = NearestStation;
            Test kMeansTest = new Test(ref stationList, ref eventList, defaultPolicy);
            Console.WriteLine(kMeansTest.getExpectedSurvivalRate());
        }

        private double LonToKilos(double lon)
        {
            return (lon - MIN_LONGITUDE) / 0.4185506 * SEOUL_WIDTH;
        }

        private double LatToKilos(double lat)
        {
            return (lat - MIN_LATITUDE) / 0.27295397 * SEOUL_HEIGHT;
        }

        private double Distance(double x1, double y1, double x2, double y2)
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
                double distance = Distance(s.longitude, s.latitude, LonToKilos(ohca.longitude), LatToKilos(ohca.latitude));
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
            int numXCells = (int) Math.Ceiling(SEOUL_WIDTH / UNIT);
            int numYCells = (int) Math.Ceiling(SEOUL_HEIGHT / UNIT);

            Pen p = new Pen(Color.LightGray, 1);
            for (int x = 0; x <= numXCells; ++x)
            {
                int xInt = (int)(x * UNIT / SEOUL_WIDTH * this.Width);
                g.DrawLine(p, xInt, 0, xInt, this.Height);
            }

            for (int y = 0; y <= numYCells; ++y)
            {
                int yInt = this.Height - (int)(y * UNIT / SEOUL_HEIGHT * this.Height);
                g.DrawLine(p, 0, yInt, this.Width, yInt);
            }
        }

        private void drawMap(Graphics g)
        {
            Pen p = new Pen(Color.Green, 1);
            foreach(Polygon polygon in polygonList)
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

        private void drawStations(Graphics g)
        {
            foreach (Station s in stationList)
            {
                //g.FillEllipse(new SolidBrush(Color.FromArgb(64, 255, 0, 0)), s.x - coverRange, s.y - coverRange, coverRange + coverRange, coverRange + coverRange);
                //g.DrawEllipse(new Pen(Color.Red, 1), s.x - coverRange, s.y - coverRange, coverRange + coverRange, coverRange + coverRange);
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
            double latitudeRatio = latitude / SEOUL_HEIGHT;
            return this.Height - (int)(this.Height * latitudeRatio);
        }

        private int transformLongitudeToInt(double longitude)
        {
            double longitudeRatio = longitude / SEOUL_WIDTH;
            return (int)(this.Width * longitudeRatio);
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
                        OHCAEvent e = new OHCAEvent();
                        e.latitude = LatToKilos(float.Parse(data[r, 15].ToString()));
                        e.longitude = LonToKilos(float.Parse(data[r, 16].ToString()));
                        e.occurrenceTime = DateTime.Parse(data[r, 19].ToString());
                        e.x = transformLongitudeToInt(e.longitude);
                        e.y = transformLatitudeToInt(e.latitude);
                        eventList.Add(e);
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
                                coord[0] = LonToKilos(lon);
                                coord[1] = LatToKilos(lat);
                                int pLon = transformLongitudeToInt(coord[0]);
                                int pLat = transformLatitudeToInt(coord[1]);
                                pc.Add(new System.Windows.Point(pLon, pLat));
                                pList.Add(coord);
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
