using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RhythMo.RhyLiveSDK;

[RequireComponent(typeof(PoseUpdater), typeof(FaceUpdater))]
public class HumanUpdater : MonoBehaviour
{
    PoseUpdater pose;
    FaceUpdater face;

    [SerializeField]
    Animator targetAnimator;

    [SerializeField]
    SkinnedMeshRenderer faceRenderer;

    // Start is called before the first frame update
    void Start()
    {
        pose = GetComponent<PoseUpdater>();
        face = GetComponent<FaceUpdater>();
    }

    // Update is called once per frame
    void Update()
    {
        pose.SmoothApplyToHumanoidAnimator(targetAnimator);
        face.SmoothApplyToFaceRenderer(faceRenderer);
    }
}
