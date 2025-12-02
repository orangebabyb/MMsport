using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("牆壁設定")]
    public GameObject[] wallPrefabs;

    // ★ 修改：改成 Vector3，Inspector 會出現 X, Y, Z 三個輸入框
    [Header("生成位置 (世界座標)")]
    public Vector3 spawnCoordinates = new Vector3(0, 5.3f, 42f); 

    public float spawnInterval = 4f;

    [Header("難度控制")]
    [Range(1f, 20f)]
    public float globalWallSpeed = 5.0f; 

    [Header("遊戲數據")]
    public int score = 0;

    private float timer = 0f;
    private int currentWallIndex = 0;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            SpawnWall();
            timer = 0f;
        }
    }

    void SpawnWall()
    {
        if (wallPrefabs.Length == 0) return;

        GameObject prefab = wallPrefabs[currentWallIndex];

        // 1. 生成牆壁 (使用 spawnCoordinates 變數)
        // 如果牆壁方向反了，將 Quaternion.identity 改為 Quaternion.Euler(0, 180, 0)
        GameObject newWall = Instantiate(prefab, spawnCoordinates, Quaternion.identity);

        // 2. 呼叫 API 設定速度
        WallBehavior wallScript = newWall.GetComponent<WallBehavior>();
        if (wallScript != null)
        {
            wallScript.SetSpeed(globalWallSpeed);
        }

        currentWallIndex = (currentWallIndex + 1) % wallPrefabs.Length;
    }

    public void AddScore(int value)
    {
        score += value;
        Debug.Log($"Pass! 分數: {score}");
    }

    public void DeductScore(int value)
    {
        score -= value;
        Debug.Log($"Hit! 分數: {score}");
    }
}