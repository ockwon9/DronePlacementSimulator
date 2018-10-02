using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DronePlacementSimulator
{
    public class Counter
    {
        public int numStations;
        public int[] limits;
        public Queue<DateTime>[] whenReady;

        public Counter(int numStations, ref int[] limits)
        {
            this.numStations = numStations;
            this.limits = new int[numStations];
            for (int i = 0; i < numStations; i++)
            {
                this.limits[i] = limits[i];
            }

            this.whenReady = new Queue<DateTime>[numStations];
            for (int i = 0; i < numStations; i++)
            {
                this.whenReady[i] = new Queue<DateTime>();
            }
        }

        public void flush(DateTime now)
        {
            for (int i = 0; i < this.numStations; i++)
            {
                while (this.whenReady[i].Count > 0 && this.whenReady[i].Peek() <= now)
                {
                    this.whenReady[i].Dequeue();
                }
            }
        }

        public void Dispatch(int i, DateTime now)
        {
            this.flush(now);
            this.whenReady[i].Enqueue(now + new TimeSpan(1, 0, 0));
        }
    }
}
