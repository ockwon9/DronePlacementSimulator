using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DronePlacementSimulator
{
    public class Counter
    {
        public int[] limits;
        public Queue<DateTime>[] whenReady;

        public Counter(ref int[] limits)
        {
            this.limits = new int[limits.Length];
            Array.Copy(limits, this.limits, limits.Length);
            this.whenReady = new Queue<DateTime>[limits.Length];
            for (int i = 0; i < limits.Length; i++)
            {
                this.whenReady[i] = new Queue<DateTime>();
            }
        }

        public void Flush(DateTime now)
        {
            for (int i = 0; i < this.limits.Length; i++)
            {
                while (this.whenReady[i].Count > 0 && this.whenReady[i].Peek() <= now)
                {
                    this.whenReady[i].Dequeue();
                }
            }
        }

        public void Dispatch(int i, DateTime now)
        {
            this.whenReady[i].Enqueue(now.AddHours(1));
        }
    }
}
