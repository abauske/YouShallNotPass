using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Logging
{
    public class StopWatch
    {
        public List<(string, float)> times = new();
        private Stopwatch w = Stopwatch.StartNew();
        
        public void Restart()
        {
            times.Clear();
            w.Restart();
        }

        public void Take(string name)
        {
            Stop(name);
            w.Restart();
        }

        public void Stop(string name)
        {
            w.Stop();
            times.Add((name, w.ElapsedTicks));
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var t in times)
            {
                sb.Append(t.Item1).Append(" took ").Append(t.Item2).Append(" ticks\n");
            }
            return sb.ToString();
        }
    }
}