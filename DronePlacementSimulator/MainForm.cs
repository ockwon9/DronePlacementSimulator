using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Excel = Microsoft.Office.Interop.Excel;

namespace DronePlacementSimulator
{
    public partial class MainForm : Form
    {
        private static float MIN_LATITUDE = 37.433373f;
        private static float MAX_LATITUDE = 37.697052f;
        private static float MIN_LONGITUDE = 126.789388f;
        private static float MAX_LONGITUDE = 127.180396f;
        
        private static int CELL_SIZE = 100;
        private static int COVER_RANGE = 200;

        private Graphics g;

        List<Station> stationList;
        List<OHCAEvent> eventList;

        private Bitmap _canvas;
        private Point _anchor; //The start point for click-drag operations
        private bool flag = false;
        private Brush _ghostBrush;

        public MainForm()
        {
            InitializeComponent();

            stationList = new List<Station>();
            eventList = new List<OHCAEvent>();

            // Set ths simulator's window size
            this.Height = Screen.PrimaryScreen.Bounds.Height;
            this.Width = (int)(this.Height * (MAX_LONGITUDE - MIN_LONGITUDE) / (MAX_LATITUDE - MIN_LATITUDE));

            //g = this.CreateGraphics();
            //g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Read OHCA events data
            ReadRawData();

            KMeansResults<OHCAEvent> stations = KMeans.Cluster<OHCAEvent>(eventList.ToArray(), 20, 100);
            foreach(double[] d in stations.Means)
            {
                Station s = new Station();
                s.latitude = d[0];
                s.longitude = d[1];
                s.x = transformLongitudeToInt(s.longitude);
                s.y = transformLatitudeToInt(s.latitude);
                if (!stationList.Contains(s))
                {
                    stationList.Add(s);
                }
            }
            Console.WriteLine("End!");
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
                drawOHCHEvents(g);
                drawStations(g);
                flag = true;
                e.Graphics.DrawImage(_canvas, 0, 0);
            }
        }
       
        private void drawGrid(Graphics g)
        {
            int numOfxCells = this.Width / CELL_SIZE;
            int numOfyCells = this.Height / CELL_SIZE;

            Pen p = new Pen(Color.LightGray, 1);
            for (int x = 0; x <= numOfxCells; ++x)
            {
                g.DrawLine(p, x * CELL_SIZE, 0, x * CELL_SIZE, numOfxCells * CELL_SIZE);
            }

            for (int y = 0; y <= numOfyCells; ++y)
            {
                g.DrawLine(p, 0, y * CELL_SIZE, this.Width, y * CELL_SIZE);
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

        private void drawOHCHEvents(Graphics g)
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

        public void ReadRawData()
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
                        e.x = transformLongitudeToInt(e.longitude);
                        e.y = transformLatitudeToInt(e.latitude);
                        eventList.Add(e);
                        
                        /*Station s = new Station();
                        s.latitude = float.Parse(data[r, 17].ToString());
                        s.longitude = float.Parse(data[r, 18].ToString());
                        s.x = transformLongitudeToInt(s.longitude);
                        s.y = transformLatitudeToInt(s.latitude);
                        if (!stationList.Contains(s))
                        {
                            stationList.Add(s);
                        }*/
                    }
                    catch(Exception ex)
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
