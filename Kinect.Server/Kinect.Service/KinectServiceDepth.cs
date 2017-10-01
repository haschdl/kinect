using System;
using System.IO;
using System.ServiceModel;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Kinect;
using System.Drawing;


namespace Kinect.Service
{
    [ServiceBehavior(AutomaticSessionShutdown = true, InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class KinectServiceDepth : KinectServiceBase, IKinectService
    {
        /// <summary>
        /// Map depth range to byte range
        /// </summary>
        private const int MapDepthToByte = 8000 / 256;
        private const int DepthFrameLength = 217088; //512*424

        private ushort[] _depthFrameData;
        private byte[] _bodyIndexFrameData;
        private byte[] whiteDepthImage; 

        /// <summary>
        /// Description of the data contained in the depth frame
        /// </summary>
        private FrameDescription depthFrameDescription = null;

        public KinectServiceDepth()
        {
            this.NextKinectMessage.MessageType = MessageType.DepthJpeg;
            _depthFrameData = new ushort[DepthFrameLength];
            _bodyIndexFrameData = new byte[DepthFrameLength];
            this.depthFrameDescription = this.KinectSensorDefault.DepthFrameSource.FrameDescription;
            this.whiteDepthImage = CreateBitmapFromDepthData(this.depthFrameDescription.Width, this.depthFrameDescription.Height, System.Drawing.Brushes.Black  );
        }

        protected override void MultiSourceFrameReaderOnMultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            var reference = e.FrameReference.AcquireFrame();
            using (var bodyIndexFrame = reference.BodyIndexFrameReference.AcquireFrame())            
            using (var depthFrame = reference.DepthFrameReference.AcquireFrame())
            {
                if (depthFrame != null && bodyIndexFrame != null )
                {

                   depthFrame.CopyFrameDataToArray(_depthFrameData);
                    bodyIndexFrame.CopyFrameDataToArray(_bodyIndexFrameData);

                    // the fastest way to process the body index data is to directly access 
                    // the underlying buffer
                    using (Microsoft.Kinect.KinectBuffer depthBuffer = depthFrame.LockImageBuffer())
                    {
                        // verify data and write the color data to the display bitmap
                        if (((this.depthFrameDescription.Width * this.depthFrameDescription.Height) == (depthBuffer.Size / this.depthFrameDescription.BytesPerPixel)))
                        {                            
                            this.ProcessDepthFrameData(depthBuffer.UnderlyingBuffer, depthBuffer.Size, 500, 3000);
                        }
                    }
                    EnqueueKinectMessage(NextKinectMessage);
                }
            }
        }

        /// <summary>
        /// Directly accesses the underlying image buffer of the DepthFrame to 
        /// create a displayable bitmap.
        /// This function requires the /unsafe compiler option as we make use of direct
        /// access to the native memory pointed to by the depthFrameData pointer.
        /// </summary>
        /// <param name="depthFrameData">Pointer to the DepthFrame image data</param>
        /// <param name="depthFrameDataSize">Size of the DepthFrame image data</param>
        /// <param name="minDepth">The minimum reliable depth value for the frame</param>
        /// <param name="maxDepth">The maximum reliable depth value for the frame</param>
        private unsafe void ProcessDepthFrameData(IntPtr depthFrameData, uint depthFrameDataSize, ushort minDepth, ushort maxDepth)
        {
            // depth frame data is a 16 bit value
            ushort* frameData = (ushort*)depthFrameData;

            // convert depth to a visual representation
            for (int i = 0, l = (int)(depthFrameDataSize / this.depthFrameDescription.BytesPerPixel); i < l; ++i)
            {
                // Get the depth for this pixel
                ushort depth = frameData[i];

                //ignore if the depth pixel doesnt belong to a body                
                if (_bodyIndexFrameData[i] == 255)
                {
                    depth = 0;
                }

                //NextKinectMessage.MessageType = MessageType.DepthArray;
                NextKinectMessage.DepthDataArray[i] = (byte)(depth >= minDepth && depth <= maxDepth ? (depth / MapDepthToByte) : 0);
            }

            NextKinectMessage.MessageType = MessageType.DepthJpeg;
            NextKinectMessage.JpegBytes = CreateBitmapFromDepthData(NextKinectMessage.DepthDataArray, this.depthFrameDescription.Width, this.depthFrameDescription.Height);
        }


        byte[] CreateBitmapFromDepthData( int width, int height, System.Drawing.Brush brush)
        {
            // Creates a new empty image with the pre-defined palette
            Bitmap bmp = new Bitmap(width, width);
            using (Graphics graph = Graphics.FromImage(bmp))
            {
                Rectangle ImageSize = new Rectangle(0, 0, width, width);
                graph.FillRectangle(brush, ImageSize);
            }

            byte[] result = null;

            using (MemoryStream bmpStream = new MemoryStream())
            {
                bmp.Save(bmpStream, System.Drawing.Imaging.ImageFormat.Jpeg);
                bmpStream.Seek(0, 0);
                using (MemoryStream stream = new MemoryStream())
                {
                    JpegBitmapEncoder encoder = new JpegBitmapEncoder
                    {
                        FlipHorizontal = false,
                        FlipVertical = false,
                        QualityLevel = 90,
                        Rotation = Rotation.Rotate0
                    };
                    encoder.Frames.Add(BitmapFrame.Create(bmpStream));
                    encoder.Save(stream);
                    stream.Seek(0, 0);


                    /*
                    using (MemoryStream outStream = new MemoryStream())
                    {
                        // Initialize the ImageFactory using the overload to preserve EXIF metadata.
                        using (ImageFactory imageFactory = new ImageFactory(preserveExifData: false))
                        {
                            // Load, resize, set the format and quality and save an image.
                            imageFactory.Load(stream).Format(jpegFormat)                           
                                        .GaussianBlur(new ImageProcessor.Imaging.GaussianLayer(5,1.4,0))                                    
                                        .Save(outStream);
                        }
                        result = outStream.ToArray();                    
                    }
                   */
                    result = stream.ToArray();
                    //using (var fileStream = new System.IO.FileStream(@"c:\temp\kinect\" + "file_" + DateTime.Now.ToShortTimeString() + ".jpeg", System.IO.FileMode.Create))
                    //{
                    //    encoder.Save(fileStream);
                    //}

                }
            }
            return result;
        }

        byte[] CreateBitmapFromDepthData(byte[] depthDataColorPixels, int width, int height)
        {
            // Creates a new empty image with the pre-defined palette
            BitmapSource image = BitmapSource.Create(
                width,
                height,
                96,
                96,
                PixelFormats.Gray8,
                null,
                depthDataColorPixels,
                width);

            //Scaling to 1:1 aspect ratio
            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);
            image = new TransformedBitmap(image, new ScaleTransform(1, (double)width / (double)height));

            byte[] result = null;

            using (MemoryStream stream = new MemoryStream())
            {
                JpegBitmapEncoder encoder = new JpegBitmapEncoder
                {
                    FlipHorizontal = false,
                    FlipVertical = false,
                    QualityLevel = 90,
                    Rotation = Rotation.Rotate0
                };
                encoder.Frames.Add(BitmapFrame.Create(image));
                encoder.Save(stream);
                stream.Seek(0, 0);
                result = stream.ToArray();
                //using (var fileStream = new System.IO.FileStream(@"c:\temp\kinect\" + "file_" + DateTime.Now.ToShortTimeString() + ".jpeg", System.IO.FileMode.Create))
                //{
                //    encoder.Save(fileStream);
                //}


            }
            return result;
        }
        
    }
}