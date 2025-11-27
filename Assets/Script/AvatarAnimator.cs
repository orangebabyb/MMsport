using UnityEngine;
using System.Collections.Generic;

public class AvatarAnimator : MonoBehaviour
{
    [Header("連接設定")]
    public AvatarRig rig;
    public PoseReceiver poseReceiver;

    [Header("參數調整")]
    [Range(0f, 1f)]
    public float smoothSpeed = 0.5f;

    // ★ X = -1 做左右鏡像
    public Vector3 coordinateScale = new Vector3(-1, 1, 1);

    private List<BoneMap> boneMaps;
    private bool isInitialized = false;

    // 用來修正模型頭部本身的初始旋轉偏差
    private Quaternion headInitialRotation;

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
        if (rig == null || poseReceiver == null)
        {
            Debug.LogError("請在 AvatarAnimator Inspector 中設定 Rig 和 PoseReceiver！");
            return;
        }

        boneMaps = new List<BoneMap>();

        // ─────────────── 1. 四肢設定 ───────────────
        // 左手 <--- MP 左手
        AddBone(rig.leftShoulder, 11, 13);
        AddBone(rig.leftElbow,    13, 15);
        // 右手 <--- MP 右手
        AddBone(rig.rightShoulder, 12, 14);
        AddBone(rig.rightElbow,    14, 16);
        // 左腿 <--- MP 左腿
        AddBone(rig.leftHip,  23, 25);
        AddBone(rig.leftKnee, 25, 27);
        // 右腿 <--- MP 右腿
        AddBone(rig.rightHip,  24, 26);
        AddBone(rig.rightKnee, 26, 28);

        // ─────────────── 2. 頭部初始化 ───────────────
        if (rig.head != null)
        {
            headInitialRotation = rig.head.rotation;
        }

        isInitialized = true;
    }

    void Update()
    {
        if (!isInitialized) return;

        Vector3[] fullLandmarks = poseReceiver.GetPoseData();

        if (fullLandmarks != null && fullLandmarks.Length >= 29)
        {
            // 更新身體四肢
            UpdateJoints(fullLandmarks);
            
            // ★ 更新頭部 (如果有設定頭部骨頭)
            if (rig.head != null)
            {
                UpdateHead(fullLandmarks);
            }
        }
    }

    // ★ 版本：鎖定 Y 軸 (只允許左右轉，消除抬頭低頭)
    private void UpdateHead(Vector3[] landmarks)
    {
        // 1. 取得座標
        Vector3 nose = ScalePoint(landmarks[0]);
        
        // 取得兩邊群組的中心點 (用來算頭的中心)
        // 左群組
        Vector3 l1 = ScalePoint(landmarks[1]);
        Vector3 l2 = ScalePoint(landmarks[2]);
        Vector3 l3 = ScalePoint(landmarks[3]);
        Vector3 l7 = ScalePoint(landmarks[7]);
        Vector3 leftCentroid = (l1 + l2 + l3 + l7) / 4f;

        // 右群組
        Vector3 r4 = ScalePoint(landmarks[4]);
        Vector3 r5 = ScalePoint(landmarks[5]);
        Vector3 r6 = ScalePoint(landmarks[6]);
        Vector3 r8 = ScalePoint(landmarks[8]);
        Vector3 rightCentroid = (r4 + r5 + r6 + r8) / 4f;

        // 2. 計算臉的中心
        Vector3 faceCenter = (leftCentroid + rightCentroid) * 0.5f;

        // 3. 計算臉的正前方 (Raw Forward)
        Vector3 faceForward = (nose - faceCenter).normalized;

        // ──────────────────────────────────────────────
        // ★ 關鍵修改：鎖定 Y 軸
        // ──────────────────────────────────────────────
        
        // 步驟 A: 把 Y 軸數值歸零 (壓扁向量)
        // 這樣無論你真人的頭抬多高，電腦判定這個向量永遠是平視的
        faceForward.y = 0; 
        
        // 重新標準化 (因為歸零後長度變了)
        faceForward = faceForward.normalized;

        // 防呆
        if (faceForward == Vector3.zero) return;

        // 步驟 B: 強制頭頂朝向世界正上方 (Vector3.up)
        // 之前我們是用 Cross Product 算頭頂，那會導致歪頭(Roll)
        // 現在直接告訴 Unity：「頭頂永遠朝著天空 (0,1,0)」
        // 這樣頭就不會歪，也不會仰視/俯視，只會像雷達一樣左右轉
        Quaternion targetRotation = Quaternion.LookRotation(faceForward, Vector3.up);

        // 4. 應用旋轉
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
        return new Vector3(
            p.x * coordinateScale.x,
            p.y * coordinateScale.y,
            p.z * coordinateScale.z
        );
    }
}