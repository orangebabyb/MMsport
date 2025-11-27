using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mediapipe;

public class MediapipeToAvatar : MonoBehaviour
{
    public AvatarPoseDriver avatar;   // drag 你的角色上那個 AvatarPoseDriver 進來

    Vector3[] buffer = new Vector3[33];

    // 這個函式在 Pose 範例裡是 callback，你要把內容加進去
    public void OnPoseLandmarksOutput(NormalizedLandmarkList poseLandmarks)
    {
        if (poseLandmarks == null || poseLandmarks.Landmark.Count < 33) return;

        for (int i = 0; i < 33; i++)
        {
            var lm = poseLandmarks.Landmark[i];
            buffer[i] = new Vector3(lm.X, lm.Y, lm.Z); // x,y,z 都是 0~1 的 normalized
        }

        avatar.SetLandmarks(buffer);
    }
}
