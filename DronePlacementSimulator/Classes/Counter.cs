using System;
using System.Collections.Generic;

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

        public Counter(Counter c)
        {
            this.limits = new int[c.limits.Length];
            Array.Copy(c.limits, this.limits, c.limits.Length);
            this.whenReady = new Queue<DateTime>[c.limits.Length];
            for (int i = 0; i < c.limits.Length; i++)
            {
                this.whenReady[i] = new Queue<DateTime>();
                foreach (DateTime dt in c.whenReady[i])
                {
                    this.whenReady[i].Enqueue(dt);
                }
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
            this.whenReady[i].Enqueue(now.AddHours(Utils.DRONE_REST_TIME));
        }
    }
}
