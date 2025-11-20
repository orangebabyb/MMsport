using UnityEngine;
using UnityEngine.SceneManagement; // *** 載入場景的關鍵命名空間 ***

public class SceneChanger : MonoBehaviour
{
    // 方法一：透過場景名稱載入 (推薦，較不易出錯)
    public void LoadSceneByName(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}