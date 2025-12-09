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

    [Header("手臂 Z 軸修正 (越小修正越強)")]
    [Range(0f, 1f)] 
    public float leftArmZWeight = 0.6f;  // 左手專用 (試著調小這個!)
    [Range(0f, 1f)] 
    public float rightArmZWeight = 0.6f; // 右手專用 (保持現狀)
    [Range(0f, 1f)]
    public float armRaiseThreshold = 0.5f; // 判定舉手的閾值
    
    public Vector3 coordinateScale = new Vector3(-1, 1, 1);
    [Range(0.1f, 2f)]
    public float bodyTurnMultiplier = 1.0f;

    private List<BoneMap> boneMaps;
    private bool isInitialized = false;
    private Quaternion headInitialRotation;
    private Quaternion hipsInitialRotation;

    // 蹲姿參數
    // ★ 修改：不再記錄初始高度，改為記錄模型的「腿長」
    private float avatarLegLength; // 大腿 + 小腿的總長度
    private float avatarAnkleHeight; // 腳踝離地面的高度 (Offset)

    // 蹲下參數設定
    private const float STAND_ANGLE = 170f; // 站直時的角度
    private const float SQUAT_ANGLE = 70f;  // 深蹲時的角度
    private const float MIN_HEIGHT_RATIO = 0.5f; // 蹲到底時，腿長剩多少比例

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
        // 這樣就不需要管玩家一開始是站是蹲，我們直接用模型的數據
        if (rig.leftHip != null && rig.leftKnee != null && rig.leftAnkle != null)
        {
            // 計算大腿長 (髖 -> 膝)
            float upperLeg = Vector3.Distance(rig.leftHip.position, rig.leftKnee.position);
            // 計算小腿長 (膝 -> 踝)
            float lowerLeg = Vector3.Distance(rig.leftKnee.position, rig.leftAnkle.position);
            
            // 總腿長
            avatarLegLength = upperLeg + lowerLeg;
            
            // 記錄腳踝高度 (假設腳底在 Y=0，這就是腳踝到地板的距離)
            // 如果你的角色由 Root 控制，這裡可以用 rig.leftAnkle.position.y
            avatarAnkleHeight = rig.leftAnkle.position.y;
        }
        isInitialized = true;
    }

    void Update()
    {
        if (!isInitialized) return;

        // ★ 核心修改 3: Update 裡不需要再 if-else 了，直接呼叫我們存好的函式
        Vector3[] fullLandmarks = _getPoseDataFunc?.Invoke();

        if (fullLandmarks != null && fullLandmarks.Length >= 29)
        {
            // 偵測關節旋轉
            UpdateJoints(fullLandmarks);
            
            // 偵測頭部旋轉
            if (rig.head != null) UpdateHead(fullLandmarks);

            // 偵測轉身
            UpdateBodyTurn(fullLandmarks);

            // 偵測蹲姿
            UpdateHipsPosition(fullLandmarks);

            // 彎腰動作
            UpdateSpine(fullLandmarks);
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

            // ★ 手臂專用修正 (針對 MediaPipe Z 軸誤判)
            // 檢查是否為前臂 (13->15 左手肘到手腕, 14->16 右手肘到手腕)
            if (map.startIdx == 13 || map.startIdx == 14)
            {
                bool isLeftArm = (map.startIdx == 13); // 判斷是左手還是右手

                // 1. 取得上臂方向
                int shoulderIdx = isLeftArm ? 11 : 12;
                Vector3 shoulderPos = ScalePoint(landmarks[shoulderIdx]);
                Vector3 upperArmDir = (startPos - shoulderPos).normalized;

                // 2. 判斷是否「舉手」 (使用變數閾值)
                if (upperArmDir.y > armRaiseThreshold)
                {
                    // 3. 修正前臂方向
                    // ★ 關鍵修改：根據左右手，選擇不同的權重
                    float currentWeight = isLeftArm ? leftArmZWeight : rightArmZWeight;
                    
                    // 混合比例 (權重越小，強制拉直的效果越強)
                    float correctedZ = Mathf.Lerp(upperArmDir.z, targetDir.z, currentWeight);
                    
                    targetDir = new Vector3(targetDir.x, targetDir.y, correctedZ).normalized;
                }
            }


            Quaternion rotDelta = Quaternion.FromToRotation(map.initialDirection, targetDir);
            Quaternion targetRotation = rotDelta * map.initialRotation;

            float t = 1f - smoothSpeed;
            map.transform.rotation = Quaternion.Slerp(map.transform.rotation, targetRotation, t);
        }
    }

    // ★ 核心功能：使用 23-28 點計算膝蓋角度來控制蹲下
    private void UpdateHipsPosition(Vector3[] landmarks)
    {
        Transform hips = rig.leftHip.parent;
        if (hips == null) return;

        // 1. 取得關鍵點 (使用 23-28 算角度)
        Vector3 lHip   = ScalePoint(landmarks[23]);
        Vector3 lKnee  = ScalePoint(landmarks[25]);
        Vector3 lAnkle = ScalePoint(landmarks[27]);

        Vector3 rHip   = ScalePoint(landmarks[24]);
        Vector3 rKnee  = ScalePoint(landmarks[26]);
        Vector3 rAnkle = ScalePoint(landmarks[28]);

        // 2. 計算膝蓋夾角
        float lAngle = Vector3.Angle(lHip - lKnee, lAnkle - lKnee);
        float rAngle = Vector3.Angle(rHip - rKnee, rAnkle - rKnee);
        //float avgAngle = (lAngle + rAngle) * 0.5f;
        float dominantAngle = Mathf.Max(lAngle, rAngle);

        // 3. 將角度換算成高度比例 (Ratio)
        // 角度 170 -> 1.0 (站直)
        // 角度 70  -> 0.5 (深蹲)
        float t = Mathf.InverseLerp(SQUAT_ANGLE, STAND_ANGLE, dominantAngle);
        float heightRatio = Mathf.Lerp(MIN_HEIGHT_RATIO, 1.0f, t);

        // 4. 計算目標絕對高度 (Absolute Height)
        // 公式：腳踝高度 + (腿總長 * 壓縮比例)
        float targetY = avatarAnkleHeight + (avatarLegLength * heightRatio);

        // 5. 應用高度
        Vector3 newPos = hips.position;
        newPos.y = Mathf.Lerp(newPos.y, targetY, 1f - smoothSpeed);
        
        hips.position = newPos;
    }

    // 脊椎彎腰控制
    private void UpdateSpine(Vector3[] landmarks)
    {
        if (rig.spine == null) return;

        // 1. 取得關鍵點 (11,12=肩, 23,24=髖, 25,26=膝)
        Vector3 lShoulder = ScalePoint(landmarks[11]);
        Vector3 rShoulder = ScalePoint(landmarks[12]);
        Vector3 lHip      = ScalePoint(landmarks[23]);
        Vector3 rHip      = ScalePoint(landmarks[24]);
        // 新增膝蓋點
        Vector3 lKnee     = ScalePoint(landmarks[25]);
        Vector3 rKnee     = ScalePoint(landmarks[26]);

        // 2. 計算中心點
        Vector3 shoulderCenter = (lShoulder + rShoulder) * 0.5f;
        Vector3 hipCenter      = (lHip + rHip) * 0.5f;
        Vector3 kneeCenter     = (lKnee + rKnee) * 0.5f;

        // 3. 計算身體區塊向量
        // A. 軀幹向量 (上半身)：從屁股指像肩膀
        Vector3 torsoDir = (shoulderCenter - hipCenter).normalized;

        // B. 大腿向量 (下半身)：從膝蓋指像屁股 (這是新的基準 Up)
        // 這是讓彎腰更穩定的關鍵！我們不再跟世界座標的 Y 軸比，而是跟你的大腿比
        Vector3 legDir = (hipCenter - kneeCenter).normalized;

        // 4. 計算肩膀的「右方向量」 (鎖定面朝向)
        Vector3 shoulderRight = (rShoulder - lShoulder).normalized;

        // 5. 計算目標前方 (Forward)
        // 我們利用 "肩膀水平線" 和 "軀幹方向" 來算出面朝向
        Vector3 bodyForward = Vector3.Cross(shoulderRight, torsoDir).normalized;

        // 防呆
        if (torsoDir == Vector3.zero || legDir == Vector3.zero || bodyForward == Vector3.zero) return;

        // 6. 計算目標旋轉
        // ★ 關鍵演算法改變：
        // 原本是：FromToRotation(Vector3.up, torsoDir) -> 跟牆壁比
        // 現在是：FromToRotation(legDir, torsoDir) -> 跟大腿比
        // 這樣就算你整個人斜躺在沙發上，只要腰沒彎，Avatar 的腰就是直的
        Quaternion spineBend = Quaternion.FromToRotation(legDir, torsoDir);

        // 7. 結合 Hips 的旋轉
        // 為了讓 Spine 正確接在 Hips 上，我們需要把這個相對彎曲疊加上去
        // 這裡我們重新構建一個 LookRotation，以確保最穩定的結果
        
        // 使用 LookRotation 建立最終的旋轉目標：
        // Up = 軀幹方向 (torsoDir)
        // Forward = 身體前方 (bodyForward)
        Quaternion targetRotation = Quaternion.LookRotation(bodyForward, torsoDir);

        // 8. 應用旋轉 (Slerp)
        rig.spine.rotation = Quaternion.Slerp(rig.spine.rotation, targetRotation, 1f - smoothSpeed);
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