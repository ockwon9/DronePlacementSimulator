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
        List<Station> stationList;
        List<OHCAEvent> eventList;
        List<Polygon> polygonList;
        List<List<double[]>> polyCoordList;

        private int coverRange;
        //private Grid gridEvent;

        private Bitmap _canvas;
        //private Point _anchor; //The start point for click-drag operations
        //private Brush _ghostBrush;
        public enum TestMethod
        {
            KMeans,
            Pulver,
            Boutilier,
            RUBIS
        }

        public MainForm()
        {
            InitializeComponent();

            eventList = new List<OHCAEvent>();
            polygonList = new List<Polygon>();
            polyCoordList = new List<List<double[]>>();

            // Set ths simulator's window size
            this.Height = Screen.PrimaryScreen.Bounds.Height;
            this.Width = (int)(this.Height * Utils.SEOUL_WIDTH / Utils.SEOUL_HEIGHT);
            coverRange = (int)(this.Height * Utils.GOLDEN_TIME / Utils.SEOUL_HEIGHT);
            
            // Read OHCA events data
            ReadEventData();
            ReadMapData();
            
            // Choose the test method
            TestMethod testMethod = TestMethod.RUBIS;
            switch (testMethod)
            {
                case TestMethod.KMeans:
                    PerformKMeans();
                    break;
                case TestMethod.Pulver:
                    PerformPulver();
                    break;
                case TestMethod.Boutilier:
                    PerformBoutilier();
                    break;
                case TestMethod.RUBIS:
                    PerformRubis();
                    break;
                default:
                    break;
            }
        }

        private void PerformKMeans()
        {
            stationList = new List<Station>();
            KMeansResults<OHCAEvent> stations = KMeans.Cluster<OHCAEvent>(eventList.ToArray(), 15, 100);
            foreach (double[] d in stations.Means)
            {
                Station s = new Station(d[0], d[1]);
                s.pixelX = Utils.TransformKiloXToPixel(s.kiloX);
                s.pixelY = Utils.TransformKiloYToPixel(s.kiloY);
                for (int i = 0; i < 1; i++)
                {
                    Drone drone = new Drone(s.stationID);
                    s.droneList.Add(drone);
                }
                if (!stationList.Contains(s))
                {
                    stationList.Add(s);
                }
            }
            Del defaultPolicy = Policy.NearestStation;
            Test kMeansTest = new Test(ref stationList, ref eventList, defaultPolicy);
            Console.WriteLine(kMeansTest.GetExpectedSurvivalRate());
        }

        private void PerformPulver()
        {
            Grid gridEvent = new Grid(0.0, 0.0, Utils.SEOUL_WIDTH, Utils.SEOUL_HEIGHT, Utils.UNIT, ref polyCoordList);
            foreach (double[] coord in gridEvent.cells)
            {
                stationList.Add(new Station(coord[0] + 0.5 * Utils.UNIT, coord[1] + 0.5 * Utils.UNIT));
            }

            gridEvent.IdwInterpolate(ref eventList);
            Pulver pulver = new Pulver(0.2, 30, 2, 5, ref stationList, ref gridEvent);
            Del defaultPolicy = Policy.NearestStation;
            Test pulverTest = new Test(ref stationList, ref eventList, defaultPolicy);
            Console.WriteLine(pulverTest.GetExpectedSurvivalRate());
        }

        private void PerformBoutilier()
        {
            /*
            Boutilier boutilier = new Boutilier(ref eventList, ref stationList);
            Del defaultPolicy = NearestStation;
            Test boutilierTest = new Test(ref stationList, ref eventList, defaultPolicy);
            Console.WriteLine(boutilierTest.getExpectedSurvivalRate());
            */
        }

        private void PerformRubis()
        {
            stationList = Rubis.Calculate(eventList, polyCoordList);
            Del rubisPolicy = Policy.NearestStation;
            Test rubisTest = new Test(ref stationList, ref eventList, rubisPolicy);
            Console.WriteLine(rubisTest.GetExpectedSurvivalRate());
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
                DrawGrid(g);
                DrawMap(g);
                DrawOHCAEvents(g);
                DrawStations(g);
                e.Graphics.DrawImage(_canvas, 0, 0);
            }
        }
       
        private void DrawGrid(Graphics g)
        {
            int numXCells = (int) Math.Ceiling(Utils.SEOUL_WIDTH / Utils.UNIT);
            int numYCells = (int) Math.Ceiling(Utils.SEOUL_HEIGHT / Utils.UNIT);

            Pen p = new Pen(Color.LightGray, 1);
            for (int x = 0; x <= numXCells; ++x)
            {
                int xInt = (int)(x * Utils.UNIT / Utils.SEOUL_WIDTH * this.Width);
                g.DrawLine(p, xInt, 0, xInt, this.Height);
            }

            for (int y = 0; y <= numYCells; ++y)
            {
                int yInt = this.Height - (int)(y * Utils.UNIT / Utils.SEOUL_HEIGHT * this.Height);
                g.DrawLine(p, 0, yInt, this.Width, yInt);
            }
        }

        private void DrawMap(Graphics g)
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

        private void DrawStations(Graphics g)
        {
            foreach (Station s in stationList)
            {
                g.FillEllipse(new SolidBrush(Color.FromArgb(64, 255, 0, 0)), s.pixelX - coverRange, s.pixelY - coverRange, coverRange + coverRange, coverRange + coverRange);
                g.DrawEllipse(new Pen(Color.Red, 1), s.pixelX - coverRange, s.pixelY - coverRange, coverRange + coverRange, coverRange + coverRange);
                g.FillRectangle((Brush)Brushes.Red, s.pixelX, s.pixelY, 3, 3);
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
                        OHCAEvent e = new OHCAEvent();
                        e.kiloX = Utils.LonToKilos(float.Parse(data[r, 16].ToString()));
                        e.kiloY = Utils.LatToKilos(float.Parse(data[r, 15].ToString()));
                        e.occurrenceTime = DateTime.Parse(data[r, 19].ToString());
                        e.pixelX = Utils.TransformKiloXToPixel(e.kiloX);
                        e.pixelY = Utils.TransformKiloYToPixel(e.kiloY);
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
