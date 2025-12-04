using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class GameSceneUIManager : MonoBehaviour
{   
    public GameObject GameCanvas, PauseCanvas, ScoreCanvas, AdviceCanvas;
    public TextMeshProUGUI Movement; 
    // Start is called before the first frame update
    void Start()
    {
        if (Movement != null)
        {
            Movement.text = TrainingMode.SelectedTrainingMode;
        }
    }
    public void OnClickPause()
    {
        GameCanvas.SetActive(false);
        PauseCanvas.SetActive(true);
    }

    public void OnClickResume()
    {
        PauseCanvas.SetActive(false);
        GameCanvas.SetActive(true);
    }

    public void OnClickEnd()
    {
        SceneManager.LoadScene("StartScene");
    }

    public void OnClickNext()
    {
        ScoreCanvas.SetActive(false);
        AdviceCanvas.SetActive(true);
    }

    public void OnClickBack()
    {
        AdviceCanvas.SetActive(false);
        ScoreCanvas.SetActive(true);
    }
}
