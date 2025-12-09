using UnityEngine;
using TMPro; // ★ 關鍵：必須引用這個才能控制 TextMeshPro

public class GameUIManager : MonoBehaviour
{
    // 在 Inspector 中拖入你的 GameManager (如果不拖，Start 會自動抓)
    public GameManager gameManager;

    [Header("UI 元件")]
    public TextMeshProUGUI scoreText; // 分數顯示
    public TextMeshProUGUI comboText; // 連擊顯示
    public TextMeshProUGUI rateText; // 評價顯示

    void Start()
    {
        // 如果沒有手動拖入 GameManager，自動去場景找
        if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>();
        }
    }

    void Update()
    {
        // 確保有找到 GameManager 才更新 UI
        if (gameManager != null)
        {
            // 更新分數文字 (使用 public getter 方法)
            if (scoreText != null)
                scoreText.text = "SCORE: " + gameManager.GetScore().ToString();

            // 更新 Combo 文字
            if (comboText != null)
                comboText.text = "COMBO: " + gameManager.GetCombos().ToString();
            
            if (rateText != null)
            {
                rateText.text = $"RATE: {gameManager.GetLastSafeCount()} / 14";
            }
        }
    }
}