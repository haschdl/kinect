using System;
using System.Threading.Tasks;
using System.ServiceModel.Channels;
using Kinect.Service;

namespace Kinect.Server.Console
{
    public class KinectCallbackConsole : IKinectCallback
    {
        private static string kinectMessageLog;
        public Task SendKinectMessage(Message msg)
        {
            var msgBytes = msg.GetBody<byte[]>();
            var kinectMsg = KinectMessage.FromByteArray(msgBytes);
            switch (kinectMsg.MessageType)
            {
                case MessageType.HandPosition:
                    kinectMessageLog = $"KINECT Msg type: {kinectMsg.MessageType.ToString()} LeftHand: {kinectMsg.LeftHand.Item1},{kinectMsg.LeftHand.Item2}";
                    break;
                default:
                    kinectMessageLog = $"KINECT Msg type: {kinectMsg.MessageType.ToString()} Data: {kinectMsg.Data}";
                    break;
            }
            

            Action action = ConsoleWriteLine;
            return Task.Factory.StartNew(action);
        }

        public static void ConsoleWriteLine()
        {
           System.Console.WriteLine(kinectMessageLog);
        }
    }
}
