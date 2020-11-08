using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using OpenCvSharp;

namespace RxTemplates
{
    static class RxLogger
    {

        private static IDisposable _disposer;

        public static Mat Init()
        {
            var lines = new string[] { "Sin", "NoisySin" };
            return CvChart.Initialize("time", lines, 9, 2);
        }

        public static IObservable<(Mat Frame, double Time, double Sin, double NoisySin)> MakeStream(int frequency)
        {
            var span = 1000 / (double)frequency;
            var time = 0.0;
            var observable = Observable.Interval(TimeSpan.FromMilliseconds(span), ThreadPoolScheduler.Instance)
                .Select(_ =>
                {
                    time += span / 1000;
                    var sin = Math.Sin(time);
                    var noisySin = sin + (new Random((int)DateTime.Now.Ticks).NextDouble() - 0.5) / 4;
                    var frame = CvChart.Update(time, new double[] { sin, noisySin });
                    return (frame, time, sin, noisySin);
                })
                .Finally(() => CvChart.SaveAsCsv("..\\..\\..\\Log.csv"))
                .Publish();
            _disposer = observable.Connect();
            return observable;
        }

        public static void Close()
        {
            _disposer.Dispose();
        }
    }
}
