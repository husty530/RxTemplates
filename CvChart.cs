using OpenCvSharp;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RxTemplates
{
    static class CvChart
    {
        private static int _width;
        private static int _height;
        private static Scalar _brush;
        private static Scalar[] _brushes;
        private static double[] _scaleX;
        private static double[] _scaleY;
        private static double _minY;
        private static double _maxY;
        private static double _zeroHeight;
        private static string _xLabel;
        private static string[] _lineLabel;

        private static Point linePy1;
        private static Point linePy2;
        private static Point linePx1;
        private static Point linePx2;
        private static Point textPy1;
        private static Point textPy2;
        private static Point textPy3;
        private static Point textPy4;
        private static Point textPy5;
        private static Point textPx1;
        private static Point textPx2;
        private static Point textPx3;
        private static Point textPx4;
        private static Point xLabelP;
        private static Point lineLabelP;

        public static List<List<Point2d>> Values { set; get; }
        public static Mat Chart { set; get; }
        public static int MaxWidth { set; get; }

        public static Mat Initialize(string xLabel, string[] lineLabel, int maxWidth, int lineCount)
        {
            MaxWidth = maxWidth;
            _xLabel = xLabel;
            _lineLabel = lineLabel;
            _width = 1400;
            _height = 900;
            _minY = -0.2;
            _maxY = 0.2;
            _brush = new Scalar(255, 255, 255);
            _brushes = new Scalar[] { new Scalar(0, 255, 100), new Scalar(255, 100, 0), new Scalar(0, 0, 150), new Scalar(50, 255, 0), new Scalar(100, 0, 0), new Scalar(0, 255, 0), new Scalar(255, 100, 100) };
            _scaleX = new double[] { 0, MaxWidth / 3, MaxWidth * 2 / 3, MaxWidth };
            _scaleY = new double[] { -0.2, -0.1, 0.0, 0.1, 0.2 };
            MakeDefaultScale();
            Values = new List<List<Point2d>>();
            for (int i = 0; i < lineCount; i++) Values.Add(new List<Point2d>());
            return Chart;
        }

        public static Mat Update(double x, double[] y)
        {
            for (int i = 0; i < y.Length; i++)
            {
                if (y[i] < _minY) _minY = y[i];
                if (x < 1 && y[i] > 1) _maxY = y[i] / x;
                if (y[i] > _maxY) _maxY = y[i];
                Values[i].Add(new Point2d(x, y[i]));
            }
            MakeCustomScale(x);
            Draw(Values);
            return Chart;
        }

        public static void SaveAsCsv(string savePath)
        {
            using (var sw = new StreamWriter(savePath, false))
            {
                sw.Write($"{ _xLabel}");
                foreach (var l in _lineLabel) sw.Write($", {l}");
                sw.WriteLine();
                for (int i = 0; i < Values[0].Count; i++)
                {
                    sw.Write($"{Values[0][i].X:f1}");
                    foreach (var val in Values) sw.Write($", {val[i].Y:f3}");
                    sw.WriteLine();
                }
            }
        }

        private static void MakeDefaultScale()
        {
            linePy1 = new Point(_width / 8 - 5, _height / 10);
            linePy2 = new Point(_width / 8 - 5, _height * 9 / 10);
            linePx1 = new Point(_width / 8 - 5, _height * 9 / 10);
            linePx2 = new Point(_width * 7 / 8, _height * 9 / 10);
            textPy1 = new Point(_width / 10 - 90, _height * 9 / 10 + 5);
            textPy2 = new Point(_width / 10 - 90, _height * 7 / 10 + 5);
            textPy3 = new Point(_width / 10 - 90, _height * 5 / 10 + 5);
            textPy4 = new Point(_width / 10 - 90, _height * 3 / 10 + 5);
            textPy5 = new Point(_width / 10 - 90, _height * 1 / 10 + 5);
            textPx1 = new Point(_width * 1 / 8 - 15, _height * 9 / 10 + 50);
            textPx2 = new Point(_width * 3 / 8 - 15, _height * 9 / 10 + 50);
            textPx3 = new Point(_width * 5 / 8 - 15, _height * 9 / 10 + 50);
            textPx4 = new Point(_width * 7 / 8 - 15, _height * 9 / 10 + 50);
            xLabelP = new Point(_width * 7 / 8 + 10, _height * 9 / 10 + 10);
            _zeroHeight = (_height * 9 / 10) + (_minY * (_height * 8 / 10) / (_maxY - _minY));
            ArrangeChart();
        }

        private static void MakeCustomScale(double xMax)
        {
            if (xMax < MaxWidth) _scaleX = new double[] { 0, MaxWidth / 3, MaxWidth * 2 / 3, MaxWidth };
            else _scaleX = new double[] { xMax - MaxWidth, xMax - MaxWidth * 2 / 3, xMax - MaxWidth / 3, xMax };
            var height = _maxY - _minY;
            _scaleY = new double[] { _minY, _minY + height / 4, _minY + height * 2 / 4, _minY + height * 3 / 4, _maxY };
            _zeroHeight = (_height * 9 / 10) + (_minY * (_height * 8 / 10) / (_maxY - _minY));
            ArrangeChart();
        }

        private static void Draw(List<List<Point2d>> values)
        {
            int count = 0;
            foreach (var val in values)
            {
                if (val.Count < 2) return;
                if (val.Last().X < MaxWidth)
                {
                    var points = new List<Point>();
                    for (int i = 0; i < val.Count; i++)
                    {
                        points.Add(new Point((_width / 8) + (val[i].X * (_width * 6 / 8) / MaxWidth), _zeroHeight - (val[i].Y * (_height * 8 / 10) / (_maxY - _minY))));
                        if (i != 0) Cv2.Line(Chart, points[i - 1], points[i], _brushes[count], 2);
                    }
                }
                else
                {
                    var points = new List<Point>();
                    for (int i = 0; i < val.Count; i++)
                    {
                        points.Add(new Point((_width / 8) + ((MaxWidth - val.Last().X + val[i].X) * (_width * 6 / 8) / MaxWidth), _zeroHeight - (val[i].Y * (_height * 8 / 10) / (_maxY - _minY))));
                        int j = 0;
                        if (val.Last().X - val[i].X > MaxWidth)
                        {
                            j = i + 1;
                            continue;
                        }
                        if (i != j) Cv2.Line(Chart, points[i - 1], points[i], _brushes[count], 2);
                    }
                }
                count++;
            }
        }

        private static void ArrangeChart()
        {
            Chart = new Mat(_height, _width, MatType.CV_8UC3, new Scalar(0, 0, 0));
            linePx1 = new Point(_width / 8 - 5, _zeroHeight);
            linePx2 = new Point(_width * 9 / 10 - 5, _zeroHeight);
            Cv2.Line(Chart, linePy1, linePy2, _brush);
            Cv2.Line(Chart, linePx1, linePx2, _brush);
            Cv2.PutText(Chart, $"{_scaleX[0]:0}", textPx1, HersheyFonts.HersheyTriplex, 1, _brush);
            Cv2.PutText(Chart, $"{_scaleX[1]:0}", textPx2, HersheyFonts.HersheyTriplex, 1, _brush);
            Cv2.PutText(Chart, $"{_scaleX[2]:0}", textPx3, HersheyFonts.HersheyTriplex, 1, _brush);
            Cv2.PutText(Chart, $"{_scaleX[3]:0}", textPx4, HersheyFonts.HersheyTriplex, 1, _brush);
            Cv2.PutText(Chart, $"{_scaleY[0]:f1}", textPy1, HersheyFonts.HersheyTriplex, 1, _brush);
            Cv2.PutText(Chart, $"{_scaleY[1]:f1}", textPy2, HersheyFonts.HersheyTriplex, 1, _brush);
            Cv2.PutText(Chart, $"{_scaleY[2]:f1}", textPy3, HersheyFonts.HersheyTriplex, 1, _brush);
            Cv2.PutText(Chart, $"{_scaleY[3]:f1}", textPy4, HersheyFonts.HersheyTriplex, 1, _brush);
            Cv2.PutText(Chart, $"{_scaleY[4]:f1}", textPy5, HersheyFonts.HersheyTriplex, 1, _brush);
            Cv2.PutText(Chart, $"{_xLabel}", xLabelP, HersheyFonts.HersheyTriplex, 1, _brush);
            for (int i = 0; i < _lineLabel.Length; i++)
            {
                lineLabelP = new Point(_width * 7 / 8 + 5, _height / 2 + i * 75);
                Cv2.PutText(Chart, $"{_lineLabel[i]}", lineLabelP, HersheyFonts.HersheyTriplex, 1.2, _brushes[i]);
            }
        }
    }
}
