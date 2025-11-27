using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class bone_information : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        var skinned = GetComponentInChildren<SkinnedMeshRenderer>();
        foreach (var b in skinned.bones)
        {
            Debug.Log("Bone: " + b.name);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
