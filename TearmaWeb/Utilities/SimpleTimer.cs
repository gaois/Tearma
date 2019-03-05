using System;
using System.Diagnostics;

namespace TearmaWeb.Utilities
{
    public class SimpleTimer : IDisposable
    {
        private readonly Stopwatch _stopwatch;

        public SimpleTimer()
        {
            _stopwatch = new Stopwatch();
            _stopwatch.Start();
        }

        public void Dispose()
        {
            _stopwatch.Stop();
        }

        public long ElapsedMilliseconds => _stopwatch.ElapsedMilliseconds;
    }
}