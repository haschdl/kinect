using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceModel;
using Microsoft.Kinect;

namespace Kinect.Service
{
    //ConcurrentMode.Single with WCF hosted in Console didn't work very well...
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession, ConcurrencyMode = ConcurrencyMode.Single)]
    [ComVisible(true)]
    public class KinectServiceJoints : KinectServiceBase, IKinectService
    {
        /// <summary>
        /// Array for the bodies
        /// </summary>
        private Body[] _bodies = null;

        public KinectServiceJoints()
        {
            //Kinect V2 can track up to 6 bodies
            _bodies = new Body[6];
            this.NextKinectMessage.MessageType = MessageType.JointPosition;


        }
        protected override void MultiSourceFrameReaderOnMultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            var reference = e.FrameReference.AcquireFrame();

            using (var bodyFrame = reference.BodyFrameReference.AcquireFrame())
            {
                if (bodyFrame == null)
                    return;
                // The first time GetAndRefreshBodyData is called, Kinect will allocate each Body in the array.
                // As long as those body objects are not disposed and not set to null in the array,
                // those body objects will be re-used.

                bodyFrame.GetAndRefreshBodyData(this._bodies);
            }


            var body = _bodies.FirstOrDefault(b => b.IsTracked);

            if (body == null)
            {
                //Currently there are not bodies in tracked state
                return;
            }
            {
                var jointItems = body.Joints;
                var jointDepthPositions = new DepthSpacePoint[25];

                KinectSensorDefault.CoordinateMapper.MapCameraPointsToDepthSpace(
                    jointItems.Values.Select(i => i.Position).ToArray(),
                    jointDepthPositions);

                var jointDepthPositionsFloats = new float[25 * 2];
                for (int i = 0; i < 25; i += 1)
                {
                    jointDepthPositionsFloats[i * 2] = jointDepthPositions[i].X / 512;
                    //Flipping Y for webgl
                    jointDepthPositionsFloats[i * 2 + 1] = 1 - jointDepthPositions[i].Y / 424;
                }
                //Copying positions of all 25 joints
                Buffer.BlockCopy(jointDepthPositionsFloats, 0, NextKinectMessage.JointsDataBytes, 1, 25 * 2 * 4);

                EnqueueKinectMessage(NextKinectMessage);
            }
        }
    }
}
