using UnityEngine;

/// <summary>
/// AvatarRig（只使用 Mediapipe 的 13 個關鍵點）
///
/// Mediapipe index 對應:
///  0  - nose (我們用來驅動頭部)
/// 11 - left shoulder
/// 13 - left elbow
/// 15 - left wrist
/// 12 - right shoulder
/// 14 - right elbow
/// 16 - right wrist
/// 23 - left hip
/// 25 - left knee
/// 27 - left ankle
/// 24 - right hip
/// 26 - right knee
/// 28 - right ankle
/// </summary>
public class AvatarRig : MonoBehaviour
{
    //────────────── 頭部（1 個 Mediapipe 點：0）──────────────
    [Header("Head / 頭部 (0)")]
    [Tooltip("Mediapipe(0)=nose，可用 Neck 或 Head 骨頭")]
    public Transform head;

    //────────────── 左手（11-13-15）──────────────
    [Header("Left Arm / 左手 (11-13-15)")]
    public Transform leftShoulder;  // 11
    public Transform leftElbow;     // 13
    public Transform leftWrist;     // 15

    //────────────── 右手（12-14-16）──────────────
    [Header("Right Arm / 右手 (12-14-16)")]
    public Transform rightShoulder; // 12
    public Transform rightElbow;    // 14
    public Transform rightWrist;    // 16

    // ★ 新增：脊椎骨頭欄位
    [Header("Body / 身體")]
    public Transform spine;

    //────────────── 左腳（23-25-27）──────────────
    [Header("Left Leg / 左腳 (23-25-27)")]
    public Transform leftHip;       // 23
    public Transform leftKnee;      // 25
    public Transform leftAnkle;     // 27

    //────────────── 右腳（24-26-28）──────────────
    [Header("Right Leg / 右腳 (24-26-28)")]
    public Transform rightHip;      // 24
    public Transform rightKnee;     // 26
    public Transform rightAnkle;    // 28
}