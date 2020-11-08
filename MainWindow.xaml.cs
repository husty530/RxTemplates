using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;

namespace RxTemplates
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {

        private enum Mode { Init, Video, Chart, Dialogue }
        private bool active;
        private string _videoName;
        private IDisposable vidDisposer;
        private Subject<string> _subject;

        public MainWindow()
        {
            InitializeComponent();
            active = false;
        }

        //Rxの1つ目、映像配信のObservableを呼ぶところ
        private void ReceiveVideoStream()
        {
            vidDisposer = RxVideo.CaptureStream()
                .Where(obj => !obj.Frame.Empty())   //もし空の画像が飛んで来たらシャットアウト
                .Select(obj =>                      //画像にちょっとおめかし
                                {
                    Cv2.PutText(obj.Frame, Path.GetFileNameWithoutExtension(_videoName), new OpenCvSharp.Point(30, 30), HersheyFonts.HersheyPlain, 2, new Scalar(0, 0, 70), 3);
                    return obj;
                })
                .ObserveOnDispatcher()              //別スレッドからUIにアクセスできないため、部分的にスレッドを戻す
                .Subscribe(obj =>                   //以下、メインスレッド
                                {
                    Slider.Maximum = obj.FrameCount;
                    Slider.Value = obj.CurrentFrameNumber;
                    Image.Source = obj.Frame.ToBitmapSource();
                    if (obj.CurrentFrameNumber == obj.FrameCount- 1)
                    {
                        RxVideo.Close();
                        Slider.Value = 0;
                        active = false;
                        ChangeStatus(Mode.Video);
                    }
                });
        }


        private void OpenCloseButton_Click(object sender, RoutedEventArgs e)
        {
            var status = (Mode)ModeCombo.SelectedIndex + 1;

            switch (status)
            {

                case Mode.Video:

                    if (!active)
                    {
                        var op = new OpenFileDialog();
                        op.Title = "動画ファイルを開く";
                        op.Filter = "(*.mp4)|*.mp4";
                        if (op.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        {
                            active = true;
                            _videoName = op.FileName;
                            ChangeStatus(status);
                            RxVideo.OpenFile(_videoName, 640, 480);
                            ReceiveVideoStream();
                            PauseButton.IsEnabled = true;
                            Slider.IsEnabled = true;
                        }
                    }
                    else
                    {
                        active = false;
                        ChangeStatus(status);
                        PauseButton.IsEnabled = false;
                        Slider.IsEnabled = false;
                        vidDisposer?.Dispose();
                        RxVideo.Close();
                    }
                    break;

                case Mode.Chart:
                    if (!active)
                    {
                        active = true;
                        ChangeStatus(status);
                        var timeLoc = new OpenCvSharp.Point(1100, 30);
                        var sinLoc = new OpenCvSharp.Point(1100, 60);
                        var noisySinLoc = new OpenCvSharp.Point(1100, 90);
                        var fontscale = 1.0;
                        var color = new Scalar(100, 100, 255);

                        //Rxの2つ目、数値のストリームを受け取る部分

                        RxLogger.MakeStream(10)
                            .Select(obj =>
                            {
                                Cv2.PutText(obj.Frame, $"Time     : {obj.Time:f1}", timeLoc, HersheyFonts.HersheySimplex, fontscale, color, 2);
                                Cv2.PutText(obj.Frame, $"Sin      : {obj.Sin:f2}", sinLoc, HersheyFonts.HersheySimplex, fontscale, color, 2);
                                Cv2.PutText(obj.Frame, $"NoisySin : {obj.NoisySin:f2}", noisySinLoc, HersheyFonts.HersheySimplex, fontscale, color, 2);
                                return obj.Frame;
                            })
                            .ObserveOnDispatcher()
                            .Subscribe(frame =>
                            {
                                Image.Source = frame.ToBitmapSource();
                            });
                    }
                    else
                    {
                        RxLogger.Close();
                        active = false;
                        ChangeStatus(status);
                    }
                    break;

                default:

                    break;

            }
        }

        private void ModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            OpenCloseButton.IsEnabled = true;
            var status = (Mode)ModeCombo.SelectedIndex + 1;
            ChangeStatus(Mode.Init);
            ChangeStatus(status);
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (active)
            {
                active = false;
                ChangeStatus(Mode.Video);
                vidDisposer?.Dispose();
            }
            else
            {
                active = true;
                ChangeStatus(Mode.Video);
                ReceiveVideoStream();
            }
        }

        private void Slider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            vidDisposer?.Dispose();
        }

        private void Slider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            RxVideo.Seek((int)Slider.Value);
            if (active)
            {
                ReceiveVideoStream();
            }
        }

        private void TextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                var message = MessageTx.Text;
                _subject.OnNext(MessageTx.Text);        //Subjectを発動させている
            }
        }


        private void ChangeStatus(Mode status)
        {

            switch (status)
            {

                case Mode.Init:
                    OpenCloseButton.IsEnabled = false;
                    OpenCloseButton.Visibility = Visibility.Hidden;
                    Slider.Visibility = Visibility.Hidden;
                    PauseButton.Visibility = Visibility.Hidden;
                    Image.Source = new Mat(480, 640, MatType.CV_8UC3, new Scalar(0, 0, 0)).ToBitmapSource();
                    Image.IsEnabled = false;
                    Image.Visibility = Visibility.Hidden;
                    Instraction.Visibility = Visibility.Hidden;
                    MessageTx.IsEnabled = false;
                    MessageTx.Visibility = Visibility.Hidden;
                    LeftLabel.Visibility = Visibility.Hidden;
                    RightLabel.Visibility = Visibility.Hidden;
                    break;

                case Mode.Video:

                    OpenCloseButton.IsEnabled = true;
                    OpenCloseButton.Visibility = Visibility.Visible;
                    Slider.Visibility = Visibility.Visible;
                    PauseButton.Visibility = Visibility.Visible;
                    Image.IsEnabled = true;
                    Image.Visibility = Visibility.Visible;
                    Instraction.Visibility = Visibility.Hidden;
                    MessageTx.IsEnabled = false;
                    MessageTx.Visibility = Visibility.Hidden;
                    LeftLabel.Visibility = Visibility.Hidden;
                    RightLabel.Visibility = Visibility.Hidden;
                    if (active)
                    {
                        OpenCloseButton.Content = "Close";
                        ModeCombo.IsEnabled = false;
                        PauseButton.Content = "| |";
                    }
                    else
                    {
                        OpenCloseButton.Content = "Open";
                        ModeCombo.IsEnabled = true;
                        PauseButton.Content = "▶";
                    }
                    break;

                case Mode.Chart:

                    OpenCloseButton.IsEnabled = true;
                    OpenCloseButton.Visibility = Visibility.Visible;
                    Slider.Visibility = Visibility.Hidden;
                    PauseButton.Visibility = Visibility.Hidden;
                    Image.IsEnabled = true;
                    Image.Visibility = Visibility.Visible;
                    Instraction.Visibility = Visibility.Hidden;
                    MessageTx.IsEnabled = false;
                    MessageTx.Visibility = Visibility.Hidden;
                    LeftLabel.Visibility = Visibility.Hidden;
                    RightLabel.Visibility = Visibility.Hidden;
                    Image.Source = RxLogger.Init().ToBitmapSource();
                    if (active)
                    {
                        OpenCloseButton.Content = "Stop";
                        ModeCombo.IsEnabled = false;
                    }
                    else
                    {
                        OpenCloseButton.Content = "Start";
                        ModeCombo.IsEnabled = true;
                    }
                    break;

                case Mode.Dialogue:

                    OpenCloseButton.IsEnabled = false;
                    OpenCloseButton.Visibility = Visibility.Hidden;
                    Slider.Visibility = Visibility.Hidden;
                    PauseButton.Visibility = Visibility.Hidden;
                    Image.IsEnabled = false;
                    Image.Visibility = Visibility.Hidden;
                    Instraction.Visibility = Visibility.Visible;
                    MessageTx.IsEnabled = true;
                    MessageTx.Visibility = Visibility.Visible;
                    MessageTx.Text = "";
                    LeftLabel.Visibility = Visibility.Visible;
                    LeftLabel.Content = "";
                    RightLabel.Visibility = Visibility.Visible;
                    RightLabel.Content = "";

                    //Rxの3つ目、対話型通信のSubject本体

                    _subject = new Subject<string>();
                    _subject
                        .Where(message => message != "")
                        .Select(message =>
                        {
                            MessageTx.Clear();
                            LeftLabel.Content += $"{message} -->\n";
                            RightLabel.Content += $"--> {message}\n";
                            var reverse = string.Concat(message.Reverse());
                            LeftLabel.Content += $"{reverse} <--\n\n";
                            RightLabel.Content += $"<-- {reverse}\n\n";
                            return message;
                        })
                        .Where(message => message == "clear" || message == "CLEAR" || message == "Clear")
                        .Subscribe(message =>
                        {
                            LeftLabel.Content = "";
                            RightLabel.Content = "";
                        });
                    break;

            }
            
        }
    }
}
