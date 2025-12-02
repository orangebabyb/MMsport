using UnityEngine;
using System.Collections.Generic;
using System; // 引用 System 以使用 Func

public class AvatarAnimator : MonoBehaviour
{
    [Header("連接設定")]
    public AvatarRig rig;

    [Header("數據來源 (擇一拖入即可)")]
    public PoseReceiver poseReceiver;
    public PoseReceiver_NoUI poseReceiverNoUI;

    // ★ 核心修改 1: 定義一個「函式指針」，用來存我們到底要用哪一個 GetPoseData
    private Func<Vector3[]> _getPoseDataFunc;

    [Header("參數調整")]
    [Range(0f, 1f)]
    public float smoothSpeed = 0.5f;
    public Vector3 coordinateScale = new Vector3(-1, 1, 1);
    [Range(0.1f, 2f)]
    public float bodyTurnMultiplier = 1.0f;

    private List<BoneMap> boneMaps;
    private bool isInitialized = false;
    private Quaternion headInitialRotation;
    private Quaternion hipsInitialRotation;

    private class BoneMap
    {
        public Transform transform;
        public int startIdx;
        public int endIdx;
        public Quaternion initialRotation;
        public Vector3 initialDirection;

        public BoneMap(Transform t, int s, int e)
        {
            transform = t;
            startIdx = s;
            endIdx = e;
            initialRotation = t.rotation;
            if (t.childCount > 0)
                initialDirection = (t.GetChild(0).position - t.position).normalized;
            else
                initialDirection = t.right;
        }
    }

    void Start()
    {
        // ★ 核心修改 2: 在 Start 裡只做一次判斷，決定 _getPoseDataFunc 是誰
        if (poseReceiver != null)
        {
            // 如果有標準版，就用標準版的 GetPoseData
            _getPoseDataFunc = poseReceiver.GetPoseData;
        }
        else if (poseReceiverNoUI != null)
        {
            // 如果有 NoUI 版，就用 NoUI 版的 GetPoseData
            _getPoseDataFunc = poseReceiverNoUI.GetPoseData;
        }
        else
        {
            Debug.LogError("錯誤：請在 AvatarAnimator 中至少拖入一種 PoseReceiver！");
            return;
        }

        if (rig == null) return;

        boneMaps = new List<BoneMap>();

        // 1. 四肢設定
        AddBone(rig.leftShoulder, 11, 13);
        AddBone(rig.leftElbow,    13, 15);
        AddBone(rig.rightShoulder, 12, 14);
        AddBone(rig.rightElbow,    14, 16);
        AddBone(rig.leftHip,  23, 25);
        AddBone(rig.leftKnee, 25, 27);
        AddBone(rig.rightHip,  24, 26);
        AddBone(rig.rightKnee, 26, 28);

        // 2. 頭部初始化
        if (rig.head != null) headInitialRotation = rig.head.rotation;

        // 3. 身體(Hips)初始化
        Transform hips = rig.leftHip.parent; 
        if (hips != null) hipsInitialRotation = hips.rotation;

        isInitialized = true;
    }

    void Update()
    {
        if (!isInitialized) return;

        // ★ 核心修改 3: Update 裡不需要再 if-else 了，直接呼叫我們存好的函式
        Vector3[] fullLandmarks = _getPoseDataFunc?.Invoke();

        if (fullLandmarks != null && fullLandmarks.Length >= 29)
        {
            UpdateJoints(fullLandmarks);
            
            if (rig.head != null) UpdateHead(fullLandmarks);

            UpdateBodyTurn(fullLandmarks);
        }
    }

    // (以下運算邏輯完全不用動)
    private void UpdateBodyTurn(Vector3[] landmarks)
    {
        Transform hips = rig.leftHip.parent;
        if (hips == null) return;

        Vector3 leftShoulder = ScalePoint(landmarks[11]);
        Vector3 rightShoulder = ScalePoint(landmarks[12]);
        Vector3 shoulderDir = (rightShoulder - leftShoulder).normalized;
        shoulderDir.y = 0;
        shoulderDir.Normalize();

        if (shoulderDir == Vector3.zero) return;

        Vector3 bodyForward = Vector3.Cross(shoulderDir, Vector3.up).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(bodyForward, Vector3.up);
        hips.rotation = Quaternion.Slerp(hips.rotation, targetRotation, (1f - smoothSpeed) * bodyTurnMultiplier);
    }

    private void UpdateHead(Vector3[] landmarks)
    {
        Vector3 nose = ScalePoint(landmarks[0]);
        Vector3 l1 = ScalePoint(landmarks[1]);
        Vector3 l2 = ScalePoint(landmarks[2]);
        Vector3 l3 = ScalePoint(landmarks[3]);
        Vector3 l7 = ScalePoint(landmarks[7]);
        Vector3 leftCentroid = (l1 + l2 + l3 + l7) / 4f;

        Vector3 r4 = ScalePoint(landmarks[4]);
        Vector3 r5 = ScalePoint(landmarks[5]);
        Vector3 r6 = ScalePoint(landmarks[6]);
        Vector3 r8 = ScalePoint(landmarks[8]);
        Vector3 rightCentroid = (r4 + r5 + r6 + r8) / 4f;

        Vector3 faceCenter = (leftCentroid + rightCentroid) * 0.5f;
        Vector3 faceForward = (nose - faceCenter).normalized;

        faceForward.y = 0; 
        faceForward = faceForward.normalized;

        if (faceForward == Vector3.zero) return;

        Quaternion targetRotation = Quaternion.LookRotation(faceForward, Vector3.up);
        rig.head.rotation = Quaternion.Slerp(rig.head.rotation, targetRotation, 1f - smoothSpeed);
    }

    private void UpdateJoints(Vector3[] landmarks)
    {
        foreach (var map in boneMaps)
        {
            Vector3 startPos = ScalePoint(landmarks[map.startIdx]);
            Vector3 endPos   = ScalePoint(landmarks[map.endIdx]);

            Vector3 targetDir = (endPos - startPos).normalized;
            if (targetDir.sqrMagnitude < 1e-5f) continue;

            Quaternion rotDelta = Quaternion.FromToRotation(map.initialDirection, targetDir);
            Quaternion targetRotation = rotDelta * map.initialRotation;

            float t = 1f - smoothSpeed;
            map.transform.rotation = Quaternion.Slerp(map.transform.rotation, targetRotation, t);
        }
    }

    private void AddBone(Transform t, int s, int e)
    {
        if (t != null) boneMaps.Add(new BoneMap(t, s, e));
    }

    private Vector3 ScalePoint(Vector3 p)
    {
        return new Vector3(p.x * coordinateScale.x, p.y * coordinateScale.y, p.z * coordinateScale.z);
    }
}