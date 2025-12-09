using UnityEngine;
using System.Collections;

public class BodyPartHit : MonoBehaviour
{
    [Header("連結設定")]
    public GameObject hitVisual; // ★ 請把你的 HitVisual 子物件 (那個紅球) 拖進來

    [Header("閃爍參數")]
    public int flashCount = 10;        // 閃幾下？
    //public float flashInterval = 0.2f;
    private float flashInterval = 0.2f; // 閃爍頻率 (秒)

    private Coroutine currentRoutine;

    public void Start()
    {
        hitVisual.SetActive(false);
    }

    // 給外部 (牆壁) 呼叫的函式
    public void OnHit()
    {
        // 如果沒有設定特效物件，就不執行
        if (hitVisual == null) return;

        // 如果正在閃，先停下來，重頭開始閃 (避免連續撞擊時錯亂)
        if (currentRoutine != null) StopCoroutine(currentRoutine);
        
        currentRoutine = StartCoroutine(FlashRoutine());
    }

    // 閃爍的協程 (Coroutine)
    IEnumerator FlashRoutine()
    {
        for (int i = 0; i < flashCount; i++)
        {
            // 亮起
            hitVisual.SetActive(true);
            yield return new WaitForSeconds(flashInterval);

            // 熄滅
            hitVisual.SetActive(false);
            yield return new WaitForSeconds(flashInterval);
        }

        // 確保最後是關閉的
        hitVisual.SetActive(false);
        currentRoutine = null;
    }
}