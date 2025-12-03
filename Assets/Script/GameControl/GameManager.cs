using System.Collections;
using System.Collections.Generic;
// using Unity.VisualScripting;
using UnityEngine;

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
    // Game control variables
    public GameState CurrentState = GameState.Loading;

    // Wall related variables
    public GameObject[] WallPrefabs;
    public int NumberOfWalls = 10;

    [SerializeField]
    private int wallsSpawned = 0;
    public float WallSpawnInterval = 10.0f;

    [SerializeField]
    private float wallSpawnTimer = 0f;
    public float WallMovementSpeed = 6.0f;

    // Score related variables
    public int BaseScore = 100;

    [SerializeField]
    private int score = 0;

    [SerializeField]
    private int combos = 0;

    // Start is called before the first frame update
    void Start()
    {
        foreach (GameObject wall in WallPrefabs)
        {
            wall.GetComponent<Wall>().gameManager = this;
            wall.GetComponent<Wall>().movementSpeed = WallMovementSpeed;
        }
        ResetGame();
    }

    // Update is called once per frame
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
                    Debug.Log("Wall Spawned");
                }

                if (wallsSpawned >= NumberOfWalls)
                {
                    CurrentState = GameState.GameOver;
                }
                break;
            case GameState.Paused:

                break;
            case GameState.GameOver:
                // Currently, just log the final score and reset the game
                Debug.Log("Game Over! Final Score: " + score);
                ResetGame();
                break;
            case GameState.Waiting:
                // Do nothing, just wait
                break;
        }
    }

    public int GetScore()
    {
        return score;
    }

    private void SetScore(int newScore)
    {
        score = newScore;
    }

    public int GetCombos()
    {
        return combos;
    }

    private void SetCombos(int comboCount)
    {
        combos = comboCount;
    }

    public void AddScore(int points)
    {
        SetScore(GetScore() + points);
    }

    public void AddCombo()
    {
        SetCombos(GetCombos() + 1);
    }

    private void ResetGame()
    {
        SetScore(0);
        SetCombos(0);
        wallsSpawned = 0;
        wallSpawnTimer = 10.0f;
        CurrentState = GameState.Loading;
    } 

    public void GamePaused()
    {
        CurrentState = GameState.Paused;
    }

    public void GameResumed()
    {
        CurrentState = GameState.Playing;
    }

    IEnumerator WaitForSwitchState(GameState newState, float delay)
    {
        CurrentState = GameState.Waiting;
        yield return new WaitForSeconds(delay);
        CurrentState = newState;
    }

    private void SpawnWall()
    {
        int randomIndex = Random.Range(0, WallPrefabs.Length);
        int randomFlip = Random.Range(0, 2);
        Instantiate(WallPrefabs[randomIndex], new Vector3(0, 5.3f, 50), Quaternion.Euler(0, randomFlip * 180, 0));
    }

    public void HitWall()
    {
        SetCombos(0);
    }

    public void PassWall()
    {
        Debug.Log("Player passed the wall.");
        AddCombo();
        int pointsEarned = BaseScore * GetCombos();
        AddScore(pointsEarned);
    }
}
