using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallBehavior : MonoBehaviour
{
    // 不要在這裡寫死預設值，改由 GameManager 控制
    private float moveSpeed = 5.0f; 
    
    private bool hasPassedPlayer = false;
    private bool hasHitPlayer = false;
    
    private float playerZ = 0f;
    private float destroyZ = -10f;

    // ★ API：給 GameManager 呼叫用的
    public void SetSpeed(float speed)
    {
        this.moveSpeed = speed;
    }

    void Update()
    {
        // 持續移動
        transform.Translate(Vector3.back * moveSpeed * Time.deltaTime);

        // 檢查通過 (加分邏輯)
        if (!hasPassedPlayer && !hasHitPlayer && transform.position.z < playerZ)
        {
            hasPassedPlayer = true;
            GameManager.Instance.AddScore(10);
        }

        // 銷毀
        if (transform.position.z < destroyZ)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !hasHitPlayer)
        {
            hasHitPlayer = true;
            GameManager.Instance.DeductScore(5);
            
            // 撞到變色提示
            Renderer rend = GetComponent<Renderer>();
            if (rend != null) rend.material.color = Color.red;
        }
    }
}
