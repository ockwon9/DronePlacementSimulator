using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DronePlacementSimulator
{
    public partial class MainForm : Form
    {
        private static float MIN_LATITUDE = 37.433373f;
        private static float MAX_LATITUDE = 37.697052f;
        private static float MIN_LONGITUDE = 126.789388f;
        private static float MAX_LONGITUDE = 127.180396f;
        private static int CELL_SIZE = 100;
        private static int COVER_RANGE = 50;

        private Graphics g;

        List<Station> stationList;
        List<Event> eventList;

        public MainForm()
        {
            InitializeComponent();
            g = this.CreateGraphics();
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            stationList = new List<Station>();
            stationList.Add(new Station(37.460097f, 126.951877f));
            stationList.Add(new Station(37.485227f, 127.080794f));
            stationList.Add(new Station(37.64698f, 127.024444f));
            
            eventList = new List<Event>();
            eventList.Add(new Event(37.599612f, 127.056478f));
            eventList.Add(new Event(37.628185f, 127.035149f));
            eventList.Add(new Event(37.656151f, 127.045268f));
            eventList.Add(new Event(37.444751f, 127.065255f));
            eventList.Add(new Event(37.490355f, 126.986228f));
        }

        private void MainForm_Paint(object sender, PaintEventArgs e)
        {
            drawGrid();
            drawStations();
            drawOHCHEvents();
        }

        private void drawGrid()
        {
            int numOfxCells = this.Width / CELL_SIZE;
            int numOfyCells = this.Height / CELL_SIZE;

            Pen p = new Pen(Color.Gray, 1);
            for (int x = 0; x < numOfxCells; ++x)
            {
                g.DrawLine(p, x * CELL_SIZE, 0, x * CELL_SIZE, numOfxCells * CELL_SIZE);
            }

            for (int y = 0; y < numOfyCells; ++y)
            {
                g.DrawLine(p, 0, y * CELL_SIZE, numOfxCells * CELL_SIZE, y * CELL_SIZE);
            }
        }

        private void drawStations()
        {
            foreach (Station s in stationList)
            {
                int x = transformLatitudeToInt(s.latitude);
                int y = transformLongitudeToInt(s.longitude);
                g.FillEllipse(new SolidBrush(Color.FromArgb(64, 255, 0, 0)), x - COVER_RANGE, y - COVER_RANGE, COVER_RANGE + COVER_RANGE, COVER_RANGE + COVER_RANGE);
                g.DrawEllipse(new Pen(Color.Red, 1), x - COVER_RANGE, y - COVER_RANGE, COVER_RANGE + COVER_RANGE, COVER_RANGE + COVER_RANGE);
                g.FillRectangle((Brush)Brushes.Red, x, y, 3, 3);
            }
        }

        private void drawOHCHEvents()
        {
            foreach (Event e in eventList)
            {
                int x = transformLatitudeToInt(e.latitude);
                int y = transformLongitudeToInt(e.longitude);
                g.FillRectangle((Brush)Brushes.Blue, x, y, 3, 3);
            }
        }

        private int transformLatitudeToInt(float latitude)
        {
            float latitudeRatio = (latitude - MIN_LATITUDE) / (MAX_LATITUDE - MIN_LATITUDE);
            return (int)(this.Width * latitudeRatio);
        }

        private int transformLongitudeToInt(float longitude)
        {
            float longitudeRatio = (longitude - MIN_LONGITUDE) / (MAX_LONGITUDE - MIN_LONGITUDE);
            return (int)(this.Height * longitudeRatio);
        }
    }
}
