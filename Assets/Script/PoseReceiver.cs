using UnityEngine;
using Mediapipe.Unity.Sample.PoseLandmarkDetection;

public class PoseReceiver : MonoBehaviour
{
    private PoseLandmarkerRunner runner;

    private void Start()
    {
        runner = PoseLandmarkerRunner.Instance;

        if (runner == null)
        {
            Debug.LogError("PoseReceiver 找不到 PoseLandmarkerRunner！");
            return;
        }

        // 接收事件式資料
        runner.OnLandmarkUpdated += OnPoseUpdated;
    }

    private void OnDestroy()
    {
        if (runner != null)
            runner.OnLandmarkUpdated -= OnPoseUpdated;
    }

    // -----------------------------
    // 事件推播模式（MediaPipe callback）
    // -----------------------------
    private void OnPoseUpdated(Vector3[] points)
    {
        Debug.Log($"[Event] 收到 {points.Length} 點 Nose={points[0]}");
    }

    // -----------------------------
    // 每幀主動拉取（永不漏資料）
    // -----------------------------
    private void Update()
    {
        if (runner != null && runner.HasResult)
        {
            var p = runner.LatestWorldPoints;
            Debug.Log($"[Update] 最新 Nose={p}");
        }
    }

    public Vector3[] GetPoseData()
    {
        // 確保 Runner 存在且有算出結果
        if (runner != null && runner.HasResult)
        {
            // 回傳最新的 33 個骨架點位
            return runner.LatestWorldPoints;
        }
        
        // 如果沒資料，回傳 null
        return null;
    }
}
