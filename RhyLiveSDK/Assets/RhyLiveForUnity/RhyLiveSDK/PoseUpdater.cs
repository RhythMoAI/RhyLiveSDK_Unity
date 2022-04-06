using System.Collections.Generic;
using UnityEngine;
using System;
using extOSC;

namespace RhythMo.RhyLiveSDK
{

public class PoseUpdater : MonoBehaviour
{
    [SerializeField]
    private int smoothness = 3;

    // posedata要传输的float数：每个骨骼的四元数+hips的position
    // posedata float number：localQuaternion for each bone +hips localPosition
    public static readonly int FloatDataCount = (int) HumanBodyBones.LastBone * 4 + 3;
    //posedata 每帧更新
    //posedata, updated per frame
    private float[] poseData = new float[FloatDataCount];
    private readonly Quaternion[] velocityQuaternionsPose = new Quaternion[(int)HumanBodyBones.LastBone];

    private void Awake()
    {
        for (int i = 0; i < (int) HumanBodyBones.LastBone; i++) {
            this.poseData[i * 4 + 0] = 0.0f;
            this.poseData[i * 4 + 1] = 0.0f;
            this.poseData[i * 4 + 2] = 0.0f;
            this.poseData[i * 4 + 3] = 1.0f;
            this.velocityQuaternionsPose[i] = Quaternion.identity;
        }

        this.poseData[(int) HumanBodyBones.LastBone * 4 + 0] = 0.0f;
        this.poseData[(int) HumanBodyBones.LastBone * 4 + 1] = 0.0f;
        this.poseData[(int) HumanBodyBones.LastBone * 4 + 2] = 0.0f;
    }

    private static Quaternion SmoothDamp(Quaternion rot, Quaternion target, ref Quaternion deriv, float time)
    {
        if (Time.deltaTime < Mathf.Epsilon) return rot;
        float dot = Quaternion.Dot(rot, target);
        float multi = dot > 0f ? 1f : -1f;
        target.x *= multi;
        target.y *= multi;
        target.z *= multi;
        target.w *= multi;
        Vector4 result = new Vector4(
            Mathf.SmoothDamp(rot.x, target.x, ref deriv.x, time),
            Mathf.SmoothDamp(rot.y, target.y, ref deriv.y, time),
            Mathf.SmoothDamp(rot.z, target.z, ref deriv.z, time),
            Mathf.SmoothDamp(rot.w, target.w, ref deriv.w, time)
        ).normalized;

        Vector4 derivError = Vector4.Project(new Vector4(deriv.x, deriv.y, deriv.z, deriv.w), result);
        deriv.x -= derivError.x;
        deriv.y -= derivError.y;
        deriv.z -= derivError.z;
        deriv.w -= derivError.w;
        return new Quaternion(result.x, result.y, result.z, result.w);
    }
    //通过float直接更新posedata（字节格式）
    //Update posedata through float (use byte[])
        public void UpdateFromFloats(byte[] data, int startIdx, int len)
    {
        int count = len / sizeof(float);
        if (count > FloatDataCount) {
            count = FloatDataCount;
        } // avoiding overflow
        
        Buffer.BlockCopy(data, startIdx, this.poseData, 0, count * sizeof(float));
    }
        //通过float直接更新posedata（List<float>格式）
        //Update posedata through float (use List<float>)
        public void UpdateFromFloats(List<float> data)
    {
        int count = data.Count;
        if (count > FloatDataCount) {
            count = FloatDataCount;
        } // avoiding overflow

        for (int i = 0; i < count; i++) {
            this.poseData[i] = data[i];
        }
    }
        //通过float直接更新posedata（List<OSCValue>格式）
        //Update posedata through float (use List<OSCValue>)
        public void UpdateFromFloats(List<OSCValue> data) {
        int count = data.Count;
        if (count > FloatDataCount) {
            count = FloatDataCount;
        } // avoiding overflow

        for (int i = 0; i < count; i++) {
            this.poseData[i] = data[i].FloatValue;
        }
    }
    //获取posedata
    //get posedata
    public float[] GetPoseData() {
        return this.poseData;
    }
    //获取目标骨骼的旋转
    //get target bone transform
    public Quaternion GetBoneQuaternion(HumanBodyBones index) {
        if (index == HumanBodyBones.LastBone) {
            return Quaternion.identity;
        }

        int i = (int) index;
        Quaternion quaternionData = new Quaternion(
            this.poseData[i * 4],
            this.poseData[i * 4 + 1], 
            this.poseData[i * 4 + 2],
            this.poseData[i * 4 + 3]
        );

        return quaternionData;
    }
        //获取根节点的位移
        //get hips bone transform
        public Vector3 GetRootPosition() {
        return new Vector3(
            this.poseData[(int) HumanBodyBones.LastBone * 4 + 0],
            this.poseData[(int) HumanBodyBones.LastBone * 4 + 1],
            this.poseData[(int) HumanBodyBones.LastBone * 4 + 2]
        );
    }
        //将posedata的值赋给要驱动的模型的animator
        //Apply posedata value to target model's animator
        public void SmoothApplyToHumanoidAnimator(Animator targetAnimator)
    {
        for (int i = 0; i < (int) HumanBodyBones.LastBone; i++) {
            Quaternion velocityQuaternionPose = this.velocityQuaternionsPose[i];
            Quaternion quaternionData = new Quaternion(
                this.poseData[i * 4],
                this.poseData[i * 4 + 1], 
                this.poseData[i * 4 + 2],
                this.poseData[i * 4 + 3]
            );
            if (targetAnimator.GetBoneTransform((HumanBodyBones) i)) {
                targetAnimator.GetBoneTransform((HumanBodyBones)i).localRotation = SmoothDamp(
                    targetAnimator.GetBoneTransform((HumanBodyBones)i).localRotation, 
                    quaternionData, 
                    ref velocityQuaternionPose, 
                    this.smoothness / 100f
                );
            };

            this.velocityQuaternionsPose[i] = velocityQuaternionPose;
        }
    }
    
}

}