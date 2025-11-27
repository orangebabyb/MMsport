using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AvatarRig : MonoBehaviour
{
    [Header("Body")]
    public Transform hipsCtrl;
    public Transform spine;
    public Transform chest;
    public Transform upperChest;
    public Transform neck;
    public Transform head;

    [Header("Left Arm")]
    public Transform leftShoulder;
    public Transform leftArm;      // upper arm
    public Transform leftForeArm;  // lower arm
    public Transform leftHand;

    [Header("Right Arm")]
    public Transform rightShoulder;
    public Transform rightArm;
    public Transform rightForeArm;
    public Transform rightHand;

    [Header("Left Leg")]
    public Transform leftUpLeg;
    public Transform leftLeg;
    public Transform leftFoot;

    [Header("Right Leg")]
    public Transform rightUpLeg;
    public Transform rightLeg;
    public Transform rightFoot;
}
