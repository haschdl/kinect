using System;
using System.ComponentModel;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Drawing;

namespace Kinect.WPF.DepthViz
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        /// <summary>
        /// INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        

        private const int receiveChunkSize = 512 * 424; //1 byte per pixel

        public MainWindow()
        {


            // allocate space to put the pixels being received and converted
            this.depthPixels = new byte[512 * 424];
            // create the bitmap to display
            this.depthBitmap = new WriteableBitmap(512, 424, 96.0, 96.0, PixelFormats.Gray8, null);
            LabelStatus = "Starting...";
            Task.Run(() => Connect("ws://127.0.0.1:8000/kinectservice/depth"));

            this.DataContext = this;
            InitializeComponent();

        }

        /// <summary>
        /// Gets the bitmap to display
        /// </summary>
        public ImageSource ImageSource
        {
            get
            {
                return this.depthBitmap;
            }
        }

        private string _labelStatus;
        public string LabelStatus
        {
            get
            {
                return this._labelStatus;
            }
            set
            {
                if (this._labelStatus != value)
                {
                    this._labelStatus = value;

                    // notify any bound elements that the text has changed
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("LabelStatus"));
                    }
                }
            }
        }



        /// <summary>
        /// Bitmap to display
        /// </summary>
        private WriteableBitmap depthBitmap = null;


        /// <summary>
        /// Intermediate storage for frame data converted to color
        /// </summary>
        private byte[] depthPixels = null;


      
        public async Task Connect(string uri)
        {
            LabelStatus = "Connecting...";
            ClientWebSocket webSocket = null;

            try
            {
                webSocket = new ClientWebSocket();
                
                
                await webSocket.ConnectAsync(new Uri(uri), CancellationToken.None);

                
                UpdateLabel("Connected!");
                //await Task.WhenAll(Receive(webSocket), Send(webSocket));
                await Receive(webSocket);
            }
            catch (Exception ex)
            {
                UpdateLabel($"Exception: {ex}");
            }
            finally
            {
                if (webSocket != null)
                    webSocket.Dispose();

                UpdateLabel("WebSocket closed.");

            }
        }
        static UTF8Encoding encoder = new UTF8Encoding();

        public void UpdateLabel(String text)
        {
            LabelStatus = text;

        }

        ArraySegment<Byte> temporaryBuffer = new ArraySegment<Byte>(new byte[receiveChunkSize]);
        private async Task Receive(ClientWebSocket webSocket)
        {

            WebSocketReceiveResult result = null;

            while (webSocket.State == WebSocketState.Open)
            {
                var i = 0;
                using (var ms = new MemoryStream())
                {

                    do
                    {
                        result = await webSocket.ReceiveAsync(temporaryBuffer, CancellationToken.None);
                        i += result.Count;
                        ms.Write(temporaryBuffer.Array, temporaryBuffer.Offset, result.Count);
                    }
                    //while (i < receiveChunkSize); // i < receiveChunkSize works well if we know that message size is constant
                    while (!result.EndOfMessage);

                    ms.Seek(0, SeekOrigin.Begin);
                    
                    try
                    {
                        Dispatcher.Invoke(() =>
                            CreateBitmapFromDepthData(ms.ToArray()));
                    }
                    catch (TaskCanceledException)
                    {

                    }
                }
                

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                }
               
            }
        }

        void CreateBitmapFromDepthData(byte[] depthData)
        {
            this.depthBitmap = new WriteableBitmap( byteToImage(depthData));
            /*
            this.depthBitmap.WritePixels(
                new Int32Rect(0, 0, this.depthBitmap.PixelWidth, this.depthBitmap.PixelHeight),                
                depthData,
                this.depthBitmap.PixelWidth,
                0);
                */
        }

        public BitmapSource byteToImage(byte[] buffer)
        {
            System.Drawing.Image image;
            using (var ms = new MemoryStream(buffer))
            {

                image = System.Drawing.Image.FromStream(ms);
            }
         
            return GetImageStream(image);
        }
        public static BitmapSource GetImageStream(System.Drawing.Image myImage)
        {
            var bitmap = new Bitmap(myImage);
            IntPtr bmpPt = bitmap.GetHbitmap();
            BitmapSource bitmapSource =
             System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                   bmpPt,
                   IntPtr.Zero,
                   Int32Rect.Empty,
                   BitmapSizeOptions.FromEmptyOptions());

            //freeze bitmapSource and clear memory to avoid memory leaks
            bitmapSource.Freeze();
            //DeleteObject(bmpPt);

            return bitmapSource;
        }
    }
}
