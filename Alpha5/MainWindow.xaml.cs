using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ConvNetSharp.Core;
using ConvNetSharp.Core.Serialization;
using ConvNetSharp.Core.Layers.Double;
using ConvNetSharp.Core.Training;
using ConvNetSharp.Volume;
using Emgu.CV;
using Emgu.Util;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using System.Runtime.InteropServices;
using System.IO;
using System.Runtime.Serialization;
using LiveCharts;
using LiveCharts.Wpf;
using LiveCharts.Configurations;

namespace Alpha5
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Deklarasi Variabel
        // Deklarasi variabel kamera
        private Alpha5.CameraProcess camera1_process = null; // process recognition

        private Alpha5.CameraProcess camera2_process = null;

        private Alpha5.CameraProcess camera3_process = null;

        private Alpha5.CameraProcess camera4_process = null;

        Image<Gray, byte> result;
        Image<Bgr, byte> display;

        // Deklarasi variabel classifier dan network
        new CascadeClassifier cascade;
        private Net<double> fernet;
        private Net<double> frnet;

        // Deklarasi variabel label recognition
        string emotionstring = null;
        string[] emotion_labels = { "neutral", "anger", "disgust", "fear", "happy", "sadness", "surprise" };
        string[] fr_labels = { "Faisal", "Jonatan", "Pradipta" };
        public Func<double, string> Formatter { get; set; }

        // Deklarasi variabel data pengolahan ekspresi dan pengenalan wajah
        double[,] val_net = new double[101, 5000]; // untuk 100 orang dan 5000 detik emosi netral
        double[,] val_ang = new double[101, 5000]; // untuk 100 orang dan 5000 detik emosi marah
        double[,] val_dis = new double[101, 5000]; // untuk 100 orang dan 5000 detik emosi jijik
        double[,] val_fea = new double[101, 5000]; // untuk 100 orang dan 5000 detik emosi takut
        double[,] val_hap = new double[101, 5000]; // untuk 100 orang dan 5000 detik emosi senang
        double[,] val_sad = new double[101, 5000]; // untuk 100 orang dan 5000 detik emosi sedih
        double[,] val_sur = new double[101, 5000]; // untuk 100 orang dan 5000 detik emosi terkejut
        double[,] val_exc = new double[5000, 9]; // array untuk menampung data yang akan masukkan ke excel
        double[] net_output = new double[8]; // array untuk menampung hasil dari network emosi


        public MainWindow()
        {
            InitializeComponent();

            //Load Haar Cascade
            cascade = new CascadeClassifier(@"D:\TA171801038\Expression Recognition\Alpha5\Alpha5\haarcascade_frontalface_default.xml");

            //Load network
            var json = File.ReadAllText(@"D:\TA171801038\Expression Recognition\Alpha5\Alpha5\mynetwork.json");
            fernet = SerializationExtensions.FromJson<double>(json);

            var frjson = File.ReadAllText(@"D:\TA171801038\Expression Recognition\Alpha5\Alpha5\frnetwork.json");
            frnet = SerializationExtensions.FromJson<double>(frjson);

            // inisialisasi array
            for (int j = 0; j < 101; j++)
            {
                for (int k = 0; k < 5000; k++)
                {
                    val_net[j, k] = -1;
                    val_ang[j, k] = -1;
                    val_dis[j, k] = -1;
                    val_fea[j, k] = -1;
                    val_hap[j, k] = -1;
                    val_sad[j, k] = -1;
                    val_sur[j, k] = -1;
                }
            }

            cv_net1 = new ChartValues<double> { };
            cv_ang1 = new ChartValues<double> { };
            cv_dis1 = new ChartValues<double> { };
            cv_fea1 = new ChartValues<double> { };
            cv_hap1 = new ChartValues<double> { };
            cv_sad1 = new ChartValues<double> { };
            cv_sur1 = new ChartValues<double> { };

            DataContext = this;

            //Every 15 minutes
            var dayConfig = Mappers.Xy<DateModel>()
              .X(dateModel => dateModel.DateTime.Ticks / TimeSpan.FromMinutes(15).Ticks);
            //and the formatter
            Formatter = value => new DateTime((long)(value * TimeSpan.FromMinutes(15).Ticks)).ToString("t");
        }

        // untuk mengatur camera 1
        private void Camera1_Click(object sender, RoutedEventArgs e)
        {
            if (camera1_process == null)
            {
                Camera1.Visibility = Visibility.Visible;

                /* initialize the cameramode object and pass it the event handler */
                camera1_process = new Alpha5.CameraProcess(timer_Tick1, 0, 0, 30);

                camera1_process.startTimer();
            }
            else
            {
                camera1_process.stopTimer();

                Camera1.Visibility = Visibility.Hidden;
            }
        }

        void timer_Tick1(object sender, EventArgs e)
        {
            /* Grab a frame from the camera */
            Image<Bgr, Byte> processFrame = camera1_process.queryFrame();

            /* Check to see that there was a frame collected */
            if (processFrame != null)
            {
                ProcessFrame(processFrame, 1);
            }
        }
        // akhir pengaturan camera 1

        // untuk mengatur camera 2
        private void Camera2_Click(object sender, RoutedEventArgs e)
        {
            if (camera2_process == null)
            {
                Camera2.Visibility = Visibility.Visible;

                /* initialize the cameramode object and pass it the event handler */
                camera2_process = new Alpha5.CameraProcess(timer_Tick2, 1, 0, 30);

                camera2_process.startTimer();
            }
            else
            {
                camera2_process.stopTimer();

                Camera2.Visibility = Visibility.Hidden;
            }
        }

        void timer_Tick2(object sender, EventArgs e)
        {
            /* Grab a frame from the camera */
            Image<Bgr, Byte> processFrame = camera2_process.queryFrame();

            /* Check to see that there was a frame collected */
            if (processFrame != null)
            {
                ProcessFrame(processFrame, 2);
            }
        }
        // akhir pengaturan camera 2

        // untuk mengatur camera 3
        private void Camera3_Click(object sender, RoutedEventArgs e)
        {
            if (camera3_process == null)
            {
                Camera3.Visibility = Visibility.Visible;

                /* initialize the cameramode object and pass it the event handler */
                camera3_process = new Alpha5.CameraProcess(timer_Tick3, 2, 0, 30);

                camera3_process.startTimer();
            }
            else
            {
                camera3_process.stopTimer();

                Camera3.Visibility = Visibility.Hidden;
            }
        }

        void timer_Tick3(object sender, EventArgs e)
        {
            /* Grab a frame from the camera */
            Image<Bgr, Byte> processFrame = camera3_process.queryFrame();

            /* Check to see that there was a frame collected */
            if (processFrame != null)
            {
                ProcessFrame(processFrame, 3);
            }
        }
        // akhir pengaturan camera 3

        // untuk mengatur camera 4
        private void Camera4_Click(object sender, RoutedEventArgs e)
        {
            if (camera4_process == null)
            {
                Camera4.Visibility = Visibility.Visible;

                /* initialize the cameramode object and pass it the event handler */
                camera4_process = new Alpha5.CameraProcess(timer_Tick4, 3, 0, 1);

                camera4_process.startTimer();
            }
            else
            {
                camera4_process.stopTimer();

                Camera4.Visibility = Visibility.Hidden;
            }
        }

        void timer_Tick4(object sender, EventArgs e)
        {
            /* Grab a frame from the camera */
            Image<Bgr, Byte> processFrame = camera4_process.queryFrame();

            /* Check to see that there was a frame collected */
            if (processFrame != null)
            {
                ProcessFrame(processFrame, 4);
            }
        }
        // akhir pengaturan camera 4

        private void ProcessFrame(Image<Bgr, Byte> imageproc, int cameranumber)
        {
            var counter = 0;
            var numface = 0;
            var status = 0;
            var dataShape = new ConvNetSharp.Volume.Shape(48, 48, 1, 1);
            var data = new double[dataShape.TotalLength];

            /* Check to see that there was a frame collected */
            if (imageproc != null)
            {
                var grayframe = imageproc.Convert<Gray, byte>();
                var faces = cascade.DetectMultiScale(grayframe, 1.1, 10, System.Drawing.Size.Empty); //the actual face detection happens here

                foreach (var face in faces)
                {
                    numface = numface + 1;
                    imageproc.Draw(face, new Bgr(System.Drawing.Color.Blue), 3); //the detected face(s) is highlighted here using a box that is drawn around it/them

                    result = imageproc.Copy(face).Convert<Gray, byte>().Resize(48, 48, Inter.Linear); //wajah yang akan di kenali ekspresinya

                    // convert image to mat
                    Mat matImage = new Mat();
                    matImage = result.Mat;

                    // create volume and fill volume with pixels
                    var emotion = BuilderInstance<double>.Volume.From(data, dataShape);
                    for (var i = 0; i < 48; i++)
                    {
                        for (var j = 0; j < 48; j++)
                        {
                            emotion.Set(i, j, 0, MatExtension.GetValue(matImage, i, j));
                        }
                    }

                    // feed the network with volume
                    var results = fernet.Forward(emotion);
                    var c = fernet.GetPrediction();

                    var frresults = frnet.Forward(emotion);
                    var d = frnet.GetPrediction();

                    // mengakses softmax layer
                    var softmaxLayer = fernet.Layers[fernet.Layers.Count - 1] as SoftmaxLayer;
                    var activation = softmaxLayer.OutputActivation;
                    var N = activation.Shape.GetDimension(3);
                    var C = activation.Shape.GetDimension(2);

                    // mengambil setiap confidence level dari setiap label
                    for (var k = 0; k < 7; k++)
                    {
                        net_output[k] = Math.Round(activation.Get(1, 1, (k + 1), 0) * 100);
                    }

                    // display prediction result
                    // emotion prediction
                    if (c[0] == 0)
                    {
                        emotionstring = emotion_labels[0];
                        imageproc.Draw(face, new Bgr(System.Drawing.Color.Blue), 3);
                    }
                    else
                    if (c[0] == 1)
                    {
                        emotionstring = emotion_labels[1];
                        imageproc.Draw(face, new Bgr(System.Drawing.Color.Red), 3);
                    }
                    else
                    if (c[0] == 2)
                    {
                        emotionstring = emotion_labels[2];
                        imageproc.Draw(face, new Bgr(System.Drawing.Color.Yellow), 3);
                    }
                    else
                    if (c[0] == 3)
                    {
                        emotionstring = emotion_labels[3];
                        imageproc.Draw(face, new Bgr(System.Drawing.Color.Green), 3);
                    }
                    else
                    if (c[0] == 4)
                    {
                        emotionstring = emotion_labels[4];
                        imageproc.Draw(face, new Bgr(System.Drawing.Color.HotPink), 3);
                    }
                    else
                    if (c[0] == 5)
                    {
                        emotionstring = emotion_labels[5];
                        imageproc.Draw(face, new Bgr(System.Drawing.Color.Orange), 3);
                    }
                    else
                    if (c[0] == 6)
                    {
                        emotionstring = emotion_labels[6];
                        imageproc.Draw(face, new Bgr(System.Drawing.Color.Purple), 3);
                    }

                    // face recognition prediction
                    if (d[0] == 0)
                    {
                        CvInvoke.PutText(imageproc, fr_labels[0] + "-" + emotionstring, face.Location, FontFace.HersheyComplex, 0.75, new MCvScalar(0, 255, 0));
                    }
                    else
                    if (d[0] == 1)
                    {
                        CvInvoke.PutText(imageproc, fr_labels[1] + "-" + emotionstring, face.Location, FontFace.HersheyComplex, 0.75, new MCvScalar(0, 255, 0));
                    }
                    else
                    if (d[0] == 2)
                    {
                        CvInvoke.PutText(imageproc, fr_labels[2] + "-" + emotionstring, face.Location, FontFace.HersheyComplex, 0.75, new MCvScalar(0, 255, 0));
                        //updatearray(status, numface, counter);
                        // update array
                        while (status == 0)
                        {
                            if (val_net[numface - 1, counter] < 0)
                            {
                                val_net[numface - 1, counter] = net_output[0];
                                val_ang[numface - 1, counter] = net_output[1];
                                val_dis[numface - 1, counter] = net_output[2];
                                val_fea[numface - 1, counter] = net_output[3];
                                val_hap[numface - 1, counter] = net_output[4];
                                val_sad[numface - 1, counter] = net_output[5];
                                val_sur[numface - 1, counter] = net_output[6];
                                status = 1;
                            }
                            else
                            {
                                counter = counter + 1;
                            }
                        }

                        chart1(counter);
                    }
                }
            }

            

            // display ke gui
            if (cameranumber == 1)
            {
                Camera1.Source = BitmapSourceConvert.ToBitmapSource(imageproc);
            }
            else if (cameranumber == 2)
            {
                Camera2.Source = BitmapSourceConvert.ToBitmapSource(imageproc);
            }
            else if (cameranumber == 3)
            {
                Camera3.Source = BitmapSourceConvert.ToBitmapSource(imageproc);
            }
            else if (cameranumber == 4)
            {
                Camera4.Source = BitmapSourceConvert.ToBitmapSource(imageproc);
            }
        }

        public void updatearray(int status, int numface, int counter)
        {
            // update array
            while (status == 0)
            {
                if (val_net[numface - 1, counter] < 0)
                {
                    val_net[numface - 1, counter] = net_output[0];
                    val_ang[numface - 1, counter] = net_output[1];
                    val_dis[numface - 1, counter] = net_output[2];
                    val_fea[numface - 1, counter] = net_output[3];
                    val_hap[numface - 1, counter] = net_output[4];
                    val_sad[numface - 1, counter] = net_output[5];
                    val_sur[numface - 1, counter] = net_output[6];
                    status = 1;
                }
                else
                {
                    counter = counter + 1;
                }
            }
        }

        public void chart1(int counter)
        {
            cv_net1.Add(val_net[0, counter]);
            cv_ang1.Add(val_ang[0, counter]);
            cv_dis1.Add(val_dis[0, counter]);
            cv_fea1.Add(val_fea[0, counter]);
            cv_hap1.Add(val_hap[0, counter]);
            cv_sad1.Add(val_sad[0, counter]);
            cv_sur1.Add(val_sur[0, counter]);
        }

        public void chart2(int counter)
        {
            cv_net2.Add(val_net[0, counter]);
            cv_ang2.Add(val_ang[0, counter]);
            cv_dis2.Add(val_dis[0, counter]);
            cv_fea2.Add(val_fea[0, counter]);
            cv_hap2.Add(val_hap[0, counter]);
            cv_sad2.Add(val_sad[0, counter]);
            cv_sur2.Add(val_sur[0, counter]);
        }

        public ChartValues<double> cv_net1 { get; set; }
        public ChartValues<double> cv_ang1 { get; set; }
        public ChartValues<double> cv_dis1 { get; set; }
        public ChartValues<double> cv_fea1 { get; set; }
        public ChartValues<double> cv_hap1 { get; set; }
        public ChartValues<double> cv_sad1 { get; set; }
        public ChartValues<double> cv_sur1 { get; set; }

        public ChartValues<double> cv_net2 { get; set; }
        public ChartValues<double> cv_ang2 { get; set; }
        public ChartValues<double> cv_dis2 { get; set; }
        public ChartValues<double> cv_fea2 { get; set; }
        public ChartValues<double> cv_hap2 { get; set; }
        public ChartValues<double> cv_sad2 { get; set; }
        public ChartValues<double> cv_sur2 { get; set; }
    }

    public static class BitmapSourceConvert
    {
        [DllImport("gdi32")]
        private static extern int DeleteObject(IntPtr o);

        public static BitmapSource ToBitmapSource(IImage image)
        {
            using (System.Drawing.Bitmap source = image.Bitmap)
            {
                IntPtr ptr = source.GetHbitmap();

                BitmapSource bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    ptr,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());

                DeleteObject(ptr);
                return bs;
            }
        }
    }

    public class DateModel
    {
        public System.DateTime DateTime { get; set; }
    }
}

