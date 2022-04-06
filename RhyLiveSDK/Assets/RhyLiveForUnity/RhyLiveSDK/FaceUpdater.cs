using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using extOSC;

namespace RhythMo.RhyLiveSDK
{

public class FaceUpdater : MonoBehaviour
{
    [SerializeField]
    private int smoothness = 3;
    
    private float[] velocityBlendShape = new float[57];
    //ARKit belndshapes每帧更新
    //ARKit belndshapes,updated per frame
    private Dictionary<string, float> sourceBlendShapes = new Dictionary<string, float>
    {
        {"browDownLeft", 0},
        {"browDownRight", 0},
        {"browInnerUp", 0},
        {"browOuterUpLeft", 0},
        {"browOuterUpRight", 0},
        {"cheekPuff", 0},
        {"cheekSquintLeft", 0},
        {"cheekSquintRight", 0},
        {"eyeBlinkLeft", 0},
        {"eyeBlinkRight", 0},
        {"eyeLookDownLeft", 0},
        {"eyeLookDownRight", 0},
        {"eyeLookInLeft", 0},
        {"eyeLookInRight", 0},
        {"eyeLookOutLeft", 0},
        {"eyeLookOutRight", 0},
        {"eyeLookUpLeft", 0},
        {"eyeLookUpRight", 0},
        {"eyeSquintLeft", 0},
        {"eyeSquintRight", 0},
        {"eyeWideLeft", 0},
        {"eyeWideRight", 0},
        {"jawForward", 0},
        {"jawLeft", 0},
        {"jawOpen", 0},
        {"jawRight", 0},
        {"mouthClose", 0},
        {"mouthDimpleLeft", 0},
        {"mouthDimpleRight", 0},
        {"mouthFrownLeft", 0},
        {"mouthFrownRight", 0},
        {"mouthFunnel", 0},
        {"mouthLeft", 0},
        {"mouthLowerDownLeft", 0},
        {"mouthLowerDownRight", 0},
        {"mouthPressLeft", 0},
        {"mouthPressRight", 0},
        {"mouthPucker", 0},
        {"mouthRight", 0},
        {"mouthRollLower", 0},
        {"mouthRollUpper", 0},
        {"mouthShrugLower", 0},
        {"mouthShrugUpper", 0},
        {"mouthSmileLeft", 0},
        {"mouthSmileRight", 0},
        {"mouthStretchLeft", 0},
        {"mouthStretchRight", 0},
        {"mouthUpperUpLeft", 0},
        {"mouthUpperUpRight", 0},
        {"noseSneerLeft", 0},
        {"noseSneerRight", 0},
        {"tongueOut", 0}
    };

    private List<string> updatingBlendShapeKeys;
    public static int FloatDataCount {get; private set;}
    private float[] floatsbuffer;

    private void Awake() {
        this.updatingBlendShapeKeys = new List<string>(sourceBlendShapes.Keys).ToList();
        FaceUpdater.FloatDataCount = this.updatingBlendShapeKeys.Count;

        this.floatsbuffer = new float[this.updatingBlendShapeKeys.Count];
    }
    //通过float直接更新blendshape（字节格式）
    //Update blendshape through float (use byte[])
    public void UpdateFromFloats(byte[] data, int startIdx, int len)
    {
        int count = len / sizeof(float);
        if (count > updatingBlendShapeKeys.Count) {
            count = updatingBlendShapeKeys.Count;
        } // avoiding overflow
        
        Buffer.BlockCopy(data, startIdx, floatsbuffer, 0, count * sizeof(float));

        for (int i = 0; i < count; i++) {
            string key = this.updatingBlendShapeKeys[i];
            this.sourceBlendShapes[key] = floatsbuffer[i];
        }
    }
        //通过float直接更新blendshape（List格式）
        //Update blendshape through float (use List<float>)
        public void UpdateFromFloats(List<float> data)
    {
        int count = data.Count;
        if (count > updatingBlendShapeKeys.Count) {
            count = updatingBlendShapeKeys.Count;
        } // avoid overflow

        for (int i = 0; i < count; i++) {
            string key = this.updatingBlendShapeKeys[i];
            this.sourceBlendShapes[key] = data[i];
        }
    }
        //通过float直接更新blendshape（List osc 格式）
        //Update blendshape through float (use List<OSCValue>)
        public void UpdateFromFloats(List<OSCValue> data)
    {
        int count = data.Count;
        if (count > updatingBlendShapeKeys.Count) {
            count = updatingBlendShapeKeys.Count;
        } // avoid overflow

        for (int i = 0; i < count; i++) {
            string key = this.updatingBlendShapeKeys[i];
            this.sourceBlendShapes[key] = data[i].FloatValue;
        }
    }
    //获取blendshape字典
    //Get blendshape dictionary
    public Dictionary<string, float> GetARKitBlendShapeDictionary() {
        return this.sourceBlendShapes;
    }
        //将blendshape字典的值赋给SkinnedMeshRenderer
        //Apply blendshape dictionary value to SkinnedMeshRenderer
        public void SmoothApplyToFaceRenderer(SkinnedMeshRenderer faceRenderer) {
        for (int i = 0; i < faceRenderer.sharedMesh.blendShapeCount; i++) {
            string curBlendShapeName = faceRenderer.sharedMesh.GetBlendShapeName(i);
            if (this.sourceBlendShapes.ContainsKey(curBlendShapeName)) {
                Debug.LogFormat("{0} {1}", curBlendShapeName, this.sourceBlendShapes[curBlendShapeName] * 100);
                float sourceValue = Mathf.SmoothDamp(faceRenderer.GetBlendShapeWeight(i) / 100, 
                        this.sourceBlendShapes[curBlendShapeName], ref this.velocityBlendShape[i], this.smoothness / 100f);
                faceRenderer.SetBlendShapeWeight(i, sourceValue * 100);
            }
        }
    }
}

}