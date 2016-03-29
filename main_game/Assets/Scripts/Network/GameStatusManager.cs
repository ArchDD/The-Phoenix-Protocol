﻿using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;

public class GameStatusManager : NetworkBehaviour
{
	private GameState gameState;
	private GameObject gameOverCanvas;
	private bool gameOverScreen = false;

    private GameObject server;
    private PlayerController playerController;
    private MusicManager musicManager;

	private ObjectPoolManager enemyLogicManager;

	// Use this for initialization
	void Start () {
		server = GameObject.Find("GameManager");

		gameState = server.GetComponent<GameState>();

        if (ClientScene.localPlayers[0].IsValid)
            playerController = ClientScene.localPlayers[0].gameObject.GetComponent<PlayerController>();

		enemyLogicManager = GameObject.Find("EnemyLogicManager").GetComponent<ObjectPoolManager>();
	}
	
	// Update is called once per frame
	void Update () {
		if((gameState.Status == GameState.GameStatus.Died ||
			gameState.Status == GameState.GameStatus.Won) && !gameOverScreen)
		{
            
            DisableThrusterSound();
            
            // Set an overlay on the screen
			gameOverCanvas = Instantiate(Resources.Load("Prefabs/GameOverCanvas", typeof(GameObject))) as GameObject;
			if(gameState.Status == GameState.GameStatus.Died) 
            {
                gameOverCanvas.transform.Find("StatusText").gameObject.GetComponent<Text>().text = "Your ship and the crew were killed.";
            }
			else
            {
                gameOverCanvas.transform.Find("StatusText").gameObject.GetComponent<Text>().text = "You reached the portal!";
            }

            if(playerController.GetRole() == RoleEnum.Camera)
            {
                // Disable the portal on all screens
                GameObject.Find("Portal(Clone)").SetActive(false);

                // If it is the server
                if(playerController.netId.Value == server.GetComponent<ServerManager>().GetServerId())
                {
                    // Do not display stats on server.
                    gameOverCanvas.transform.Find("GameOverStats").gameObject.SetActive(false);
                    Debug.Log("Final Game Score: " + this.gameObject.GetComponent<GameStatsManager>().CalculateAndSendGameScore());

					// Disable the enemies to stop them from shooting
					enemyLogicManager.DisableAll();
                  
                    // Manage end game music
                    if(musicManager == null)
                        musicManager = GameObject.Find("MusicManager(Clone)").GetComponent<MusicManager>();

                    if(gameState.Status == GameState.GameStatus.Died) 
                        musicManager.PlayMusic(2);
                    else
                        musicManager.PlayMusic(2);

                    // Disable game timer. 
                    GameObject gameTimer = GameObject.Find("GameTimerText");
                    gameTimer.SetActive(false);
                }
                else 
                {
                    // Show stats on side screens.
                    gameOverCanvas.transform.Find("StatusText").gameObject.SetActive(false);
                    gameOverCanvas.transform.Find("GameOverText").gameObject.SetActive(false);
                }
            } 
            else 
            {
                gameOverCanvas.transform.Find("GameOverStats").gameObject.SetActive(false);
            }
           

			gameOverScreen = true;
		}
	}


    private void DisableThrusterSound()
    {
        GameObject playerShip = GameObject.Find("PlayerShip(Clone)");
        if(playerShip != null)
            playerShip.GetComponent<AudioSource>().mute = true;
    }

}
