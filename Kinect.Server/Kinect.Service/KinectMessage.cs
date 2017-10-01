using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.ServiceModel.Channels;
using System.Text;

namespace Kinect.Service
{
    [ComVisible(true)]
    public class KinectMessage : IDisposable
    {
        public string Stability { get; set; }

        public DateTime Timestamp { get; set; }
        public MessageType MessageType { get; set; }




        public Tuple<float, float> LeftHand;
        public Tuple<float, float> RightHand;



        public byte[] DepthDataArray;
        public byte[] DepthJpegDataBytes;

        /// <summary>
        /// First 2 byte for message type and 4 x 4 bytes for hand coordinates.
        /// </summary>
        private byte[] _handsDataBytes = new byte[18];

        /// <summary>
        /// Kinect V2 has 25 joints.
        /// Storing joint positions (x,y float, 4 bytes each) requires 8 bytes per joint
        /// in a total of 25 * 8 = 200 bytes, plus 1 bytes for MessageType.
        /// </summary>
        public byte[] JointsDataBytes = new byte[201];


        public List<byte> DataBytes => DepthDataArray?.ToList();

        public string Data { get; set; }
        public byte[] JpegBytes { get; set; }

        /// <summary>
        /// Creates a new message of type information.
        /// </summary>
        public KinectMessage(string data) : this(MessageType.Information, data)
        {

        }

        public KinectMessage(MessageType type, string data)
        {
            MessageType = type;
            Data = data;

            //Depth data is sent normalized in range from 0-255 (one byte each), without message type           
            //For full resolution, our message would have: 1 for  [MessageType] + pixels [512 * 424] * 1 bytes (each: 1 byte)
            //For 1/8 resolution:
            DepthJpegDataBytes = new byte[1 + 3 * (512 * 424)];

            DepthDataArray = new byte[512 * 424];


            //Upper bits are ignored            
            DepthJpegDataBytes[0] = (byte)(ushort)MessageType.DepthJpeg;
            JointsDataBytes[0] = (byte)(ushort)MessageType.JointPosition;
            _handsDataBytes[0] = (byte)(ushort)MessageType.HandPosition;
            //DepthDataBytes[1] = (byte)((ushort)MessageType.Depth >> 8);

            Timestamp = DateTime.Now;
        }


        public static KinectMessage FromByteArray(byte[] messageData)
        {
            throw new NotImplementedException();
            /*
            //MessageType is always in the first byte
            var type = (MessageType)messageData[0];
            var message = new KinectMessage(type, null);

            switch (type)
            {
                case MessageType.Information:
                    message.Data = Encoding.UTF8.GetString(messageData, 1, messageData.Length - 1);
                    break;
                case MessageType.HandPosition:
                    message.LeftHand = new Tuple<float, float>(BitConverter.ToSingle(messageData, 2), BitConverter.ToSingle(messageData, 4));
                    message.RightHand = new Tuple<float, float>(BitConverter.ToSingle(messageData, 6), BitConverter.ToSingle(messageData, 8));

                    //message.Data = Encoding.UTF8.GetString(messageData, 1, messageData.Length - 1);
                    break;
                case MessageType.JointPosition:
                    //message.Data = Encoding.UTF8.GetString(messageData, 1, messageData.Length - 1);
                    break;
            }
            return message;
            */

        }

        public Message CreateBinaryMessage()
        {
            byte[] messageDataBytes = null;
            var webSockectMsgType = WebSocketMessageType.Binary;


            switch (MessageType)
            {
                case MessageType.JointPosition:
                    messageDataBytes = JointsDataBytes;
                    break;
                case MessageType.HandPosition:

                    _handsDataBytes[0] = (byte)MessageType;

                    Buffer.BlockCopy(BitConverter.GetBytes(LeftHand.Item1), 0, _handsDataBytes, 1, 4);
                    Buffer.BlockCopy(BitConverter.GetBytes(LeftHand.Item2), 0, _handsDataBytes, 5, 4);
                    Buffer.BlockCopy(BitConverter.GetBytes(RightHand.Item1), 0, _handsDataBytes, 9, 4);
                    Buffer.BlockCopy(BitConverter.GetBytes(RightHand.Item2), 0, _handsDataBytes, 13, 4);
                    messageDataBytes = _handsDataBytes;
                    break;
                case MessageType.DepthJpeg:
                    messageDataBytes = JpegBytes;
                    break;
                case MessageType.DepthArray:
                    //var sizeInBytes = 2 * DepthDataArray.Length;
                    //messageDataBytes = new byte[sizeInBytes];
                    //Buffer.BlockCopy(DepthDataArray, 0, messageDataBytes, 0, sizeInBytes);
                    messageDataBytes = DepthDataArray;
                    break;
                case MessageType.Information:
                    webSockectMsgType = WebSocketMessageType.Text;
                    var bCount = Encoding.UTF8.GetByteCount(Data);
                    messageDataBytes = new byte[1 + bCount];
                    messageDataBytes[0] = (byte)MessageType;
                    Buffer.BlockCopy(Encoding.UTF8.GetBytes(Data), 0, messageDataBytes, 1,
                        Encoding.UTF8.GetByteCount(Data));
                    break;

            }

            if (messageDataBytes == null)
                return null;

            Message msg = ByteStreamMessage.CreateMessage(
                new ArraySegment<byte>(messageDataBytes));

            msg.Properties["WebSocketMessageProperty"] =
                new WebSocketMessageProperty
                {
                    MessageType = webSockectMsgType,


                };
            return msg;
        }


        public void Dispose()
        {
            _handsDataBytes = null;
            DepthDataArray = null;
            JpegBytes = null;
        }
    }




}
