using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class StartSceneUIManager : MonoBehaviour
{   
    public GameObject StartCanvas, ChooseModeCanvas, StageSelectCanvas;
    
    public void OnClickChooseYourMode()
    {
        StartCanvas.SetActive(false);
        ChooseModeCanvas.SetActive(true);
    }


    public void OnClickPerformanceTest()
    {
        ChooseModeCanvas.SetActive(false);
        StageSelectCanvas.SetActive(true);
    }

    public void LoadEasyScene()
    {
        SceneManager.LoadScene("TreeScene");
    }

    public void LoadMediumScene()
    {
        SceneManager.LoadScene("DessertScene");
    }

    public void LoadHardScene()
    {
        SceneManager.LoadScene("LavaScene");
    }
}
