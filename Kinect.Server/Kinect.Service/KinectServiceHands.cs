using System.Linq;
using System.ServiceModel;
using Microsoft.Kinect;

namespace Kinect.Service
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession, ConcurrencyMode = ConcurrencyMode.Single)]
    public class KinectServiceHands : KinectServiceBase, IKinectService 
    {
        /// <summary>
        /// Array for the bodies
        /// </summary>
        private Body[] _bodies = null;
        protected override void MultiSourceFrameReaderOnMultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            var reference = e.FrameReference.AcquireFrame();

            using (var bodyFrame = reference.BodyFrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    if (_bodies == null)
                    {
                        _bodies = new Body[bodyFrame.BodyCount];
                    }

                    // The first time GetAndRefreshBodyData is called, Kinect will allocate each Body in the array.
                    // As long as those body objects are not disposed and not set to null in the array,
                    // those body objects will be re-used.

                    bodyFrame.GetAndRefreshBodyData(this._bodies);


                    foreach (var body in Enumerable.Where<Body>(_bodies, body => body.IsTracked))
                    {
                        var handPositionMsg = new KinectMessage(string.Empty)
                        {
                            MessageType = MessageType.HandPosition,
                            LeftHand = GetCoordinatesFromJoint(body.Joints[JointType.HandLeft]),
                            RightHand = GetCoordinatesFromJoint(body.Joints[JointType.HandRight])
                        };


                        if (handPositionMsg.LeftHand.Item1 != 0
                            || handPositionMsg.LeftHand.Item2 != 0
                            || handPositionMsg.RightHand.Item1 != 0
                            || handPositionMsg.RightHand.Item2 != 0
                            )
                        {
                            EnqueueKinectMessage(handPositionMsg);
                        }
                    }
                }
            }
        }

       
    }
}