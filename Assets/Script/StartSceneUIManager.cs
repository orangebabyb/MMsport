using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class StartSceneUIManager : MonoBehaviour
{   
    public GameObject StartCanvas, StageSelectCanvas, TrainingSelectCanvas;


    public void OnClickPerformanceTest()
    {
        StartCanvas.SetActive(false);
        StageSelectCanvas.SetActive(true);
    }

    public void OnClickCoachTraining()
    {
        StartCanvas.SetActive(false);
        TrainingSelectCanvas.SetActive(true);
    }


    public void OnClickStageSelectBack()
    {   
        StageSelectCanvas.SetActive(false);
        StartCanvas.SetActive(true);
    }

    public void OnTrainingSelectBack()
    {
        TrainingSelectCanvas.SetActive(false);
        StartCanvas.SetActive(true);
    }


    public void LoadEasyScene()
    {
        SceneManager.LoadScene("TreeScene");
    }

    public void LoadMediumScene()
    {
        SceneManager.LoadScene("DessetScene");
    }

    public void LoadHardScene()
    {
        SceneManager.LoadScene("LavaScene");
    }

    public void OnClickTrainingMode(Button clickedButton)
    {
        TrainingMode.SelectedTrainingMode = clickedButton.name;
        SceneManager.LoadScene("TrainingScene");
    }
}
