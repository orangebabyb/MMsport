using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AvatarPoseDriver : MonoBehaviour
{
    public AvatarRig rig;

    // Mediapipe 抓回來的 33 個節點 (x,y,z 0~1)
    Vector3[] landmarks = new Vector3[33];

    // T-pose 時骨頭預設方向 (local)，先假設骨頭朝「下」
    Vector3 boneDefaultDir = Vector3.down;

    // 存 T-pose 的原始 localRotation，之後會乘上去
    Dictionary<Transform, Quaternion> bindRot = new Dictionary<Transform, Quaternion>();

    void Start()
    {
        // 把會動的骨頭都存起來
        CacheBindRotation(rig.leftArm);
        CacheBindRotation(rig.leftForeArm);
        CacheBindRotation(rig.rightArm);
        CacheBindRotation(rig.rightForeArm);
        CacheBindRotation(rig.leftUpLeg);
        CacheBindRotation(rig.leftLeg);
        CacheBindRotation(rig.rightUpLeg);
        CacheBindRotation(rig.rightLeg);
    }

    void CacheBindRotation(Transform t)
    {
        if (t == null) return;
        bindRot[t] = t.localRotation;
    }

    /// <summary>給 Mediapipe 呼叫，把 33 個節點塞進來。</summary>
    public void SetLandmarks(Vector3[] lm)
    {
        if (lm == null || lm.Length < 33) return;
        for (int i = 0; i < 33; i++)
            landmarks[i] = lm[i];

        UpdatePose();
    }

    void UpdatePose()
    {
        Vector2 hipCenter = (Get2D(23) + Get2D(24)) * 0.5f;

        // 手臂
        ApplyLimb2D(rig.leftArm,     11, 13, hipCenter);
        ApplyLimb2D(rig.leftForeArm, 13, 15, hipCenter);

        ApplyLimb2D(rig.rightArm,     12, 14, hipCenter);
        ApplyLimb2D(rig.rightForeArm, 14, 16, hipCenter);

        // 腿
        ApplyLimb2D(rig.leftUpLeg, 23, 25, hipCenter);
        ApplyLimb2D(rig.leftLeg,   25, 27, hipCenter);

        ApplyLimb2D(rig.rightUpLeg, 24, 26, hipCenter);
        ApplyLimb2D(rig.rightLeg,   26, 28, hipCenter);
    }

    Vector2 Get2D(int idx)
    {
        return new Vector2(landmarks[idx].x, landmarks[idx].y);
    }

    void ApplyLimb2D(Transform bone, int parentIdx, int childIdx, Vector2 hipCenter)
    {
        if (bone == null) return;

        Vector2 p = Get2D(parentIdx) - hipCenter;
        Vector2 c = Get2D(childIdx) - hipCenter;
        Vector2 dir2D = (c - p).normalized;
        if (dir2D.sqrMagnitude < 1e-6f) return;

        Vector3 worldDir = new Vector3(dir2D.x, -dir2D.y, 0);

        Vector3 localDir = bone.parent.InverseTransformDirection(worldDir);

        Quaternion fromTo = Quaternion.FromToRotation(boneDefaultDir, localDir);

        Quaternion baseRot = bindRot.ContainsKey(bone) ? bindRot[bone] : bone.localRotation;

        bone.localRotation = Quaternion.Slerp(
            bone.localRotation,
            fromTo * baseRot,
            0.5f
        );
    }
}
