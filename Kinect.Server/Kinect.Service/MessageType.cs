using System.Runtime.InteropServices;

namespace Kinect.Service
{
    [ComVisible(true)]
    public enum MessageType
    {
        Information = 1,
        Error = 2,
        GestureDetected = 3,
        HandPosition = 4,
        JointPosition = 5,
        DepthJpeg = 6,
        DepthArray = 7,
        None = 8
        //StabilityInfo
    }
}