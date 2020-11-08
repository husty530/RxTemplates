using System;
using OpenCvSharp;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using System.Threading;

namespace RxTemplates
{
    static class RxVideo
    {

        private static Size _size;
        private static VideoCapture _cap;
        private static int _span;
        private static int _location;

        public static void OpenFile(string filePath, int width, int height)
        {
            _size = new Size(width, height);
            _cap = new VideoCapture(filePath);
            _span = (int)(1000 / _cap.Fps);
            _location = 0;
        }

        public static void Seek(int location)
        {
            _location = location;
        }

        public static IObservable<(Mat Frame, int FrameCount, int CurrentFrameNumber)> CaptureStream()
        {
            _cap.Set(0, _span * _location);
            var observable = Observable.Range(0, _cap.FrameCount - _location, ThreadPoolScheduler.Instance)
                .Select(i =>
                {
                    var frame = new Mat();
                    _cap.Read(frame);
                    Cv2.Resize(frame, frame, _size);
                    var frameCount = _cap.FrameCount;
                    Thread.Sleep(_span);
                    return (frame, frameCount, _location++);
                })
                .Publish()
                .RefCount();
            return observable;
        }

        public static void Close()
        {
            _cap?.Dispose();
        }
    }
}
