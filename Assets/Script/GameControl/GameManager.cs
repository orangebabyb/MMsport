using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq; 

public enum GameState
{
    Loading,
    Playing,
    Paused,
    GameOver,
    Waiting
}

public class GameManager : MonoBehaviour
{
    // --- 評分規則 ---
    [System.Serializable]
    public class ScoreRule
    {
        public int minSafeColliders; 
        public int scoreReward;      
        public int comboReward;
        public GameObject feedbackObject;      
    }

    [Header("評分規則設定")]
    public List<ScoreRule> scoreRules = new List<ScoreRule>();
    
    // ★★★ 新增：音效系統 (BGM + SFX) ★★★
    [Header("音效設定")]
    public AudioSource musicSource; // 專門放背景音樂 (BGM)
    public AudioSource sfxSource;   // 專門放音效 (SFX)
    
    [Header("音效檔案")]
    public AudioClip bgmClip;      // 背景音樂檔案
    public AudioClip successClip;  // 成功音效
    public AudioClip failClip;     // 失敗音效

    // --- 遊戲變數 ---
    public GameState CurrentState = GameState.Loading;
    public GameObject[] WallPrefabs;
    public int NumberOfWalls = 10;
    private int wallsSpawned = 0;
    public float WallSpawnInterval = 10.0f;
    private float wallSpawnTimer = 0f;
    public float WallMovementSpeed = 6.0f;
    [SerializeField] private int score = 0;
    [SerializeField] private int combos = 0;
    [SerializeField] private int lastSafeCount = 0;

    void Start()
    {
        Debug.Log("Game Start!");
        
        // 1. 自動抓取 AudioSource (如果沒拉的話)
        if (sfxSource == null) sfxSource = GetComponent<AudioSource>();
        
        // ★★★ 2. 啟動背景音樂 ★★★
        if (musicSource != null && bgmClip != null)
        {
            musicSource.clip = bgmClip;
            musicSource.loop = true;  // 設定為循環播放
            musicSource.volume = 0.5f; // 背景音樂稍微小聲一點，以免蓋過音效
            musicSource.Play();
        }

        // 規則排序與初始化
        scoreRules = scoreRules.OrderByDescending(r => r.minSafeColliders).ToList();
        foreach (var rule in scoreRules)
        {
            if (rule.feedbackObject != null) rule.feedbackObject.SetActive(false);
        }

        foreach (GameObject wall in WallPrefabs)
        {
            if(wall.GetComponent<Wall>())
            {
                wall.GetComponent<Wall>().gameManager = this;
                wall.GetComponent<Wall>().movementSpeed = WallMovementSpeed;
            }
        }
        ResetGame();
    }

    // Update 保持不變
    void Update()
    {
        switch (CurrentState)
        {
            case GameState.Loading:
                StartCoroutine(WaitForSwitchState(GameState.Playing, 3.0f));
                break;
            case GameState.Playing:
                wallSpawnTimer += Time.deltaTime;
                if (wallSpawnTimer >= WallSpawnInterval)
                {
                    SpawnWall();
                    wallSpawnTimer = 0.0f;
                    wallsSpawned++;
                }
                if (wallsSpawned >= NumberOfWalls) CurrentState = GameState.GameOver;
                break;
            case GameState.Paused: break;
            case GameState.GameOver:
                ResetGame();
                break;
            case GameState.Waiting: break;
        }
    }

    // Getter / Setter 省略...
    public int GetScore() { return score; }
    private void SetScore(int newScore) { score = newScore; }
    public int GetCombos() { return combos; }
    private void SetCombos(int comboCount) { combos = comboCount; }
    public void AddScore(int points) { SetScore(GetScore() + points); }
    public void AddCombo(int amount = 1) { SetCombos(GetCombos() + amount); }

    private void ResetGame()
    {
        SetScore(0);
        SetCombos(0);
        wallsSpawned = 0;
        wallSpawnTimer = 10.0f;
        CurrentState = GameState.Loading;
    } 

    public void GamePaused() { CurrentState = GameState.Paused; }
    public void GameResumed() { CurrentState = GameState.Playing; }

    IEnumerator WaitForSwitchState(GameState newState, float delay)
    {
        CurrentState = GameState.Waiting;
        yield return new WaitForSeconds(delay);
        CurrentState = newState;
    }

    private void SpawnWall()
    {
        if (WallPrefabs.Length == 0) return;
        int randomIndex = Random.Range(0, WallPrefabs.Length);
        int randomFlip = Random.Range(0, 2);
        Instantiate(WallPrefabs[randomIndex], new Vector3(0, 5.3f, 50), Quaternion.Euler(0, randomFlip * 180, 0));
    }

    public void HitWall()
    {
        SetCombos(0);
        foreach (var rule in scoreRules)
        {
            if (rule.feedbackObject != null) rule.feedbackObject.SetActive(false);
        }

        // ★★★ 播放失敗音效 (Fail) ★★★
        if (sfxSource != null && failClip != null)
        {
            sfxSource.pitch = 1.0f; // 失敗時音調重置正常
            sfxSource.PlayOneShot(failClip);
        }
        
        Debug.Log("Hit Wall! Combo Reset.");
    }

    public void PassWall(int safeColliderCount)
    {
        ScoreRule matchedRule = null;
        lastSafeCount = safeColliderCount;

        foreach (var rule in scoreRules)
        {
            if (rule.feedbackObject != null) rule.feedbackObject.SetActive(false);
        }

        foreach (var rule in scoreRules)
        {
            if (safeColliderCount >= rule.minSafeColliders)
            {
                matchedRule = rule;
                break; 
            }
        }

        if (matchedRule != null)
        {
            // 加分邏輯
            AddCombo(matchedRule.comboReward);
            int pointsEarned = matchedRule.scoreReward * (GetCombos() > 0 ? GetCombos() : 1);
            AddScore(pointsEarned);
            
            if (matchedRule.feedbackObject != null) matchedRule.feedbackObject.SetActive(true);

            // ★★★ 播放成功音效 (Success) ★★★
            if (sfxSource != null && successClip != null)
            {
                // 音調隨 Combo 變高，增加爽感 (最高 1.5 倍)
                sfxSource.pitch = 1.0f + Mathf.Clamp(GetCombos() * 0.1f, 0f, 0.5f);
                sfxSource.PlayOneShot(successClip);
            }
        }
        else
        {
            // 沒加分 -> 視為失敗
            HitWall(); // 這裡會去呼叫 HitWall，播放失敗音效
        }
    }

    public int GetLastSafeCount()
    {
        return lastSafeCount;
    }
}