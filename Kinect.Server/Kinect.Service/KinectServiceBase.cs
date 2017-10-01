using System;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Kinect;

namespace Kinect.Service
{
    public class KinectServiceBase : IDisposable
    {

        protected KinectMessage NextKinectMessage = new KinectMessage(string.Empty);
        public IKinectCallback Callback;

        /// <summary>
        /// Constant for clamping Z values of camera space points from being negative
        /// </summary>
        protected const float InferredZPositionClamp = 0.1f;


        private KinectSensor _kinectSensorDefault;

        public KinectSensor KinectSensorDefault
        {
            get
            {
                if (_kinectSensorDefault == null)
                {
                    _kinectSensorDefault = KinectSensor.GetDefault();
                }

                if (!_kinectSensorDefault.IsOpen)
                {
                    _kinectSensorDefault.Open();

                }

                return _kinectSensorDefault;
            }
        }

        public bool IsInitialized { get; set; }

        private bool _keepAliveFaulted = false;

        internal readonly Timer KeepAliveTimer = new Timer();


        protected MultiSourceFrameReader MultiSourceFrameReader;

        public KinectServiceBase()
        {

            if (!IsInitialized)
            {
                Initialize();
            }

            // Tell the timer what to do when it elapses
            KeepAliveTimer.Elapsed += new ElapsedEventHandler(SendKinectMessageKeepAlive);
            KeepAliveTimer.Interval = 60000;
            KeepAliveTimer.Enabled = false;
        }

        protected void SendKinectMessageKeepAlive(object source, ElapsedEventArgs e)
        {
            if (Callback == null)
            {
                return;
            }

            KinectMessage kinectMessage = null;

            var channelState = ((IChannel)Callback).State;

            kinectMessage = new KinectMessage(MessageType.Information, "KeepAlive");
            if (channelState == CommunicationState.Opened)
            {
                try
                {
                    Callback.SendKinectMessage(kinectMessage.CreateBinaryMessage());
                }
                catch (Exception)
                {
                    _keepAliveFaulted = true;
                    KeepAliveTimer.Enabled = false;
                    Console.WriteLine("Error sending keep-alive message.");
                    return;
                    //throw new FaultException<Exception>(new Exception("Client disconnected or unknown error."));
                }
            }
            else
            {
                _keepAliveFaulted = true;
                KeepAliveTimer.Enabled = false;

            }

        }

        public async Task ClientConnectRequest(Message msg)
        {
            if (OperationContext.Current != null)
            {
                OperationContext context = OperationContext.Current;
                MessageProperties prop = context.IncomingMessageProperties;

                //Logging origin, just for fun
                var endpoint = prop[WebSocketMessageProperty.Name] as WebSocketMessageProperty;
                string origin = endpoint?.WebSocketContext.Origin ?? "<null>";
                string requestUri = endpoint?.WebSocketContext.RequestUri.ToString() ?? "<null>";
                Console.WriteLine($"Client connected. Origin: {origin} RequestUri {requestUri} ");
            }

            Initialize();
            _keepAliveFaulted = false;
            Callback = OperationContext.Current.GetCallbackChannel<IKinectCallback>();

            //Reply to client
            //KinectMessage kinectMessage = new KinectMessage(MessageType.Information, "Connected");
            //await Callback.SendKinectMessage(kinectMessage.CreateBinaryMessage());
            //kinectMessage.Dispose();

            //Start sending joint position each tick of the timer
            KeepAliveTimer.Enabled = true;
            try
            {
                await SendKinectMessage();
            }
            catch (Exception)
            {
                Console.WriteLine("ClientConnectRequest:Error sending Kinect message. Likely client has disconnected.");
                Dispose();
                throw;
            }

        }

        protected void EnqueueKinectMessage(KinectMessage message)
        {
            NextKinectMessage = message;
        }

        protected async Task SendKinectMessage()
        {
            if (Callback == null)
            {
                return;
            }

            while (!_keepAliveFaulted)
            {
                if (NextKinectMessage != null && NextKinectMessage.MessageType != MessageType.None)
                {
                    //if (!KinectServiceBase.KinectMessages.TryDequeue(out kinectMessage)) continue;
                    switch (NextKinectMessage.MessageType)
                    {
                        case MessageType.HandPosition:
                        case MessageType.JointPosition:
                            //await
                            await Callback.SendKinectMessage(NextKinectMessage.CreateBinaryMessage());
                            break;
                        //kinectMessage = null;
                        case MessageType.DepthJpeg:
                        case MessageType.DepthArray:
                            var binaryMessage = NextKinectMessage.CreateBinaryMessage();
                            if (binaryMessage != null)
                                await
                                    Callback.SendKinectMessage(
                                        binaryMessage);
                            break;

                            //await callback.SendKinectMessage(kinectMessage.CreateBinaryMessage(kinectMessage.Depth));
                            //This option converts the message to a string and sends as Text
                            //var message = JsonConvert.SerializeObject(kinectMessage);
                            //await callback.SendKinectMessage(KinectMessage.CreateMessage(message));
                    }
                    //Resetting the next message
                    NextKinectMessage.MessageType = MessageType.None;
                }
                await Task.Delay(100);
            }
            Dispose();
        }



        private void Initialize()
        {
            if (IsInitialized)
                return;

            //loop through all the Kinects attached to this PC, and start the first that is connected without an error.
            if (KinectSensorDefault == null)
            {
                var msg = new KinectMessage(MessageType.Error,
                    "Uexpected error. API call to KinectSensorDefault.GetDefault() return null");
                EnqueueKinectMessage(msg);

                throw new Exception(msg.Data);
            }

            // open the reader for the body frames
            //if (MultiSourceFrameReader == null)
            //{
            MultiSourceFrameReader =
                KinectSensorDefault.OpenMultiSourceFrameReader(FrameSourceTypes.Body | FrameSourceTypes.Depth |
                                                               FrameSourceTypes.BodyIndex);
            MultiSourceFrameReader.MultiSourceFrameArrived -= MultiSourceFrameReaderOnMultiSourceFrameArrived;
            MultiSourceFrameReader.MultiSourceFrameArrived += MultiSourceFrameReaderOnMultiSourceFrameArrived;
            //}

            IsInitialized = true;

        }

        protected virtual void MultiSourceFrameReaderOnMultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            return;
        }


        /// <summary>
        /// Positions normalized according to the resolution of the Depth sensor
        /// </summary>
        /// <param name="joint"></param>
        /// <returns></returns>
        protected Tuple<float, float> GetCoordinatesFromJoint(Joint joint)
        {
            //Note: Inferred also include points outside 512*424
            if (joint.TrackingState != TrackingState.Tracked)
                return new Tuple<float, float>(0, 0);

            // sometimes the depth(Z) of an inferred joint may show as negative
            // clamp down to 0.1f to prevent coordinatemapper from returning (-Infinity, -Infinity)

            if (joint.Position.Z < 0)
            {
                joint.Position.Z = InferredZPositionClamp;
            }
            DepthSpacePoint depthSpacePoint = KinectSensorDefault.CoordinateMapper.MapCameraPointToDepthSpace(joint.Position);
            return new Tuple<float, float>
                ((float)depthSpacePoint.X / 512,
                    (float)depthSpacePoint.Y / 424);
        }
        public void Dispose()
        {
            Console.WriteLine("     KinectServiceBase.Dispose() called");
            try
            {
                //MultiSourceFrameReader.Dispose();
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error disposing MultiSourceFrameReader: " + exception.Message);
            }

            ;
            try
            {
                //if (KinectSensorDefault.IsOpen)
                //    KinectSensorDefault.Close();
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error closing Kinect sensor: " + exception.Message);
            };

        }
    }
}