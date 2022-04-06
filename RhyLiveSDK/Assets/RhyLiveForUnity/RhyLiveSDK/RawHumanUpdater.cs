using UnityEngine;

namespace RhythMo.RhyLiveSDK
{

public class RawHumanUpdater : MonoBehaviour
{
    public FaceUpdater faceUpdater {get; set;}
    public PoseUpdater poseUpdater {get; set;}
    
    public int RefDataByteCount {get; private set;}
    public void Start() {
        RefDataByteCount = (FaceUpdater.FloatDataCount + PoseUpdater.FloatDataCount) * sizeof(float);
    }

    public void UpdateData(byte[] data, uint valid_len) {
        if (valid_len >= RefDataByteCount) {
            if (valid_len != RefDataByteCount) {
                Debug.LogWarning("[RhyLive SDK] Data: " + RefDataByteCount.ToString() + " < " + valid_len.ToString());
            }
            faceUpdater.UpdateFromFloats(data, 0, FaceUpdater.FloatDataCount * sizeof(float));
            poseUpdater.UpdateFromFloats(data, FaceUpdater.FloatDataCount * sizeof(float), PoseUpdater.FloatDataCount * sizeof(float));
        } else {
            Debug.LogError("[RhyLive SDK] Data: " + RefDataByteCount.ToString() + " > " + valid_len.ToString());
        }
    }
}

}