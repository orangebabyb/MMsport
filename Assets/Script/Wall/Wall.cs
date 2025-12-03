using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wall : MonoBehaviour
{
    public GameManager gameManager;

    public float movementSpeed;

    private bool hasBeenHit = false;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        switch (gameManager.CurrentState)
        {
            case GameState.Loading:
                break;
            case GameState.Playing:
                if (transform.rotation.eulerAngles.y == 180)
                    transform.Translate(movementSpeed * Time.deltaTime * Vector3.forward);
                else
                    transform.Translate(movementSpeed * Time.deltaTime * Vector3.back);
                break;
            case GameState.Paused:
                break;
            case GameState.GameOver:
                break;
        }
    }

    //         

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.name == "WallChecker")
        {
            if (!hasBeenHit)
            {
                gameManager.PassWall();
            }
        }
        else if (other.gameObject.name == "WallRemover")
        {
            Destroy(this.gameObject);
        }
        else if (other.gameObject.tag == "Player")
        {
            Debug.Log("Player hit the wall!");
            hasBeenHit = true;
            gameManager.HitWall();
        }
    }
}
