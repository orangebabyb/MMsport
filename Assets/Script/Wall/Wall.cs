using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wall : MonoBehaviour
{
    public GameManager gameManager;
    public float movementSpeed;
    public int totalPlayerColliders = 14; 

    // 用來記錄「不重複」的撞擊部位
    private HashSet<Collider> hitParts = new HashSet<Collider>();

    // 判斷是否已經結算過 (防止重複觸發)
    private bool isScored = false;

    void Start()
    {
        hitParts = new HashSet<Collider>();
    }

    void Update()
    {
        if (gameManager == null) return;

        if (gameManager.CurrentState == GameState.Playing)
        {
            if (transform.rotation.eulerAngles.y == 180)
                transform.Translate(movementSpeed * Time.deltaTime * Vector3.forward);
            else
                transform.Translate(movementSpeed * Time.deltaTime * Vector3.back);
        }
    } 

    private void OnTriggerEnter(Collider other)
    {
        // ---------------------------------------------------
        // 情況 A：碰到 WallRemover (牆壁任務結束，進行結算與銷毀)
        // ---------------------------------------------------
        if (other.gameObject.name == "WallRemover")
        {
            if (!isScored) // 確保只執行一次
            {
                isScored = true;

                // 1. 取得「撞擊數量」 (這就是你要的數據)
                int hitCount = hitParts.Count;

                // 2. (選用) 為了配合 GameManager 的規則，算出「安全數量」
                int safeCount = totalPlayerColliders - hitCount;
                if (safeCount < 0) safeCount = 0;

                // 3. 回報數據 (可以在這裡 Debug 看到撞了幾個)
                Debug.Log($"牆壁回收結算 -> 撞擊次數: {hitCount} / 安全通過: {safeCount}");

                // 4. 通知 GameManager 加分
                if (gameManager != null)
                {
                    gameManager.PassWall(safeCount);
                }
            }

            // 5. 銷毀牆壁
            Destroy(this.gameObject);
        }
        
        // ---------------------------------------------------
        // 情況 B：碰到 WallChecker (如果你決定只在 Remover 結算，這邊可以留空或單純做音效)
        // ---------------------------------------------------
        else if (other.gameObject.name == "WallChecker")
        {
            // 如果你把結算移到了 WallRemover，這裡通常可以留空，
            // 或是單純用來播放 "Pass" 的音效，但不計算分數。
            Debug.Log("通過檢查點 (等待 Remover 結算...)");
        }

        // ---------------------------------------------------
        // 情況 C：碰到 Player (蒐集撞擊數據)
        // ---------------------------------------------------
        else if (other.gameObject.tag == "Player")
        {
            // 視覺回饋
            BodyPartHit part = other.GetComponent<BodyPartHit>();
            if (part != null) part.OnHit(); 

            // 記錄撞擊 (不重複記)
            if (!hitParts.Contains(other))
            {
                hitParts.Add(other);
                Debug.Log($"撞到部位: {other.gameObject.name} (目前累積撞擊: {hitParts.Count})");
            }
        }
    }
}