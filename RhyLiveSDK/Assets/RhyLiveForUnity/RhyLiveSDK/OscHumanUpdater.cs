using extOSC;
using UnityEngine;

namespace RhythMo.RhyLiveSDK
{

public class OscHumanUpdater : MonoBehaviour
{
    public FaceUpdater faceUpdater {get; set;}
    public PoseUpdater poseUpdater {get; set;}

    public void UpdateFace(OSCMessage message)  {
        faceUpdater.UpdateFromFloats(message.Values);
    }

    public void UpdatePose(OSCMessage message) {
        poseUpdater.UpdateFromFloats(message.Values);
    }
}

}