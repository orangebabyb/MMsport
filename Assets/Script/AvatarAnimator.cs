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

    // ★ 新增：身體轉動幅度 (1.0 = 1:1 跟隨, 0.5 = 轉一半)
    [Range(0.1f, 2f)]
    public float bodyTurnMultiplier = 1.0f;

    private List<BoneMap> boneMaps;
    private bool isInitialized = false;

    // 用來修正模型本身的初始旋轉偏差
    private Quaternion headInitialRotation;
    private Quaternion hipsInitialRotation; // ★ 新增 Hips 初始旋轉

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
        AddBone(rig.leftShoulder, 11, 13);
        AddBone(rig.leftElbow,    13, 15);
        AddBone(rig.rightShoulder, 12, 14);
        AddBone(rig.rightElbow,    14, 16);
        AddBone(rig.leftHip,  23, 25);
        AddBone(rig.leftKnee, 25, 27);
        AddBone(rig.rightHip,  24, 26);
        AddBone(rig.rightKnee, 26, 28);

        // ─────────────── 2. 頭部初始化 ───────────────
        if (rig.head != null)
        {
            headInitialRotation = rig.head.rotation;
        }

        // ─────────────── 3. 身體(Hips)初始化 ───────────────
        // 假設左腳的父物件通常是 Hips (如果你的 rig 沒有 hips 欄位，這是一個權宜之計)
        // 建議在 AvatarRig 腳本中增加 public Transform hips; 欄位會更嚴謹
        Transform hips = rig.leftHip.parent; 
        if (hips != null)
        {
            hipsInitialRotation = hips.rotation;
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
            
            // 更新頭部
            if (rig.head != null)
            {
                UpdateHead(fullLandmarks);
            }

            // ★ 更新身體轉身
            UpdateBodyTurn(fullLandmarks);
        }
    }

    // ★ 新增：偵測轉身 (Torso Rotation)
    private void UpdateBodyTurn(Vector3[] landmarks)
    {
        // 我們通常旋轉 Hips (骨盆) 來帶動全身
        Transform hips = rig.leftHip.parent; // 嘗試抓取 Hips
        if (hips == null) return;

        // 1. 取得肩膀座標 (鏡像後)
        Vector3 leftShoulder = ScalePoint(landmarks[11]);
        Vector3 rightShoulder = ScalePoint(landmarks[12]);

        // 2. 計算「肩膀連線向量」
        // 從 右肩 指向 左肩 (因為是鏡像，Unity 的左邊對應 MP 的右邊數據)
        // 這裡的邏輯是：MediaPipe ID 12 是右肩(Unity左), 11 是左肩(Unity右)
        // 算出目前的肩膀水平線
        Vector3 shoulderDir = (rightShoulder - leftShoulder).normalized;
        
        // 3. 鎖定 Y 軸 (只看水平旋轉)
        shoulderDir.y = 0;
        shoulderDir.Normalize();

        if (shoulderDir == Vector3.zero) return;

        // 4. 計算與「正前方」的夾角
        // 我們的目標是算出 Hips 該轉多少度。
        // 假設 T-Pose 時，肩膀連線是指向 Vector3.right (或 left)
        // 這裡我們直接用 LookRotation 建立一個旋轉：正前方是肩膀連線的垂直方向
        // 數學原理：肩膀連線 X Vector3.up = 身體正前方
        Vector3 bodyForward = Vector3.Cross(shoulderDir, Vector3.up).normalized;

        // ★ 如果發現身體轉反了 (背對鏡頭)，把 bodyForward 改成 -bodyForward
        Quaternion targetRotation = Quaternion.LookRotation(bodyForward, Vector3.up);

        // 5. 應用旋轉 (加上初始偏差)
        // 這裡用 Slerp 插值，可以透過 bodyTurnMultiplier 調整轉動的靈敏度
        // 注意：這裡我們直接設定 hips 的 rotation，這會影響到子物件(腿)，
        // 但因為腿部是用 IK 或 FK 絕對座標更新的，所以理論上會自動修正回來。
        hips.rotation = Quaternion.Slerp(hips.rotation, targetRotation, (1f - smoothSpeed) * bodyTurnMultiplier);
    }

    // 鎖定 Y 軸的頭部轉動 (保持不變)
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
        return new Vector3(
            p.x * coordinateScale.x,
            p.y * coordinateScale.y,
            p.z * coordinateScale.z
        );
    }
}