﻿using UnityEngine;
using System.Collections;

public class GameStatsManager : MonoBehaviour {
    private GameState state;
    // Use this for initialization
    void Start()
    {
        state = this.gameObject.GetComponent<GameState>();
        if (MainMenu.startServer)
        {
            StartCoroutine(SendRequest());
        }
    }

    IEnumerator SendRequest()
    {
        while (true)
        {
            if (state != null)
            {
                int[] playerScores = state.GetPlayerScores();
                string jsonMsg = "{\"playerscores\":[";
                if (playerScores != null)
                {
                    /*foreach (uint id in playerScores)
                    {
                        jsonMsg += "{" + id + "," + playerScores[id] + "},";
                    }
                    jsonMsg = jsonMsg.Remove(jsonMsg.Length - 1);
                    jsonMsg = jsonMsg.Remove(jsonMsg.Length - 1);
                    jsonMsg += "}]";
                    print(jsonMsg);*/
                    foreach (uint playerScore in playerScores)
                    {
						jsonMsg += playerScore + ",";
                    }
                    jsonMsg = jsonMsg.Remove(jsonMsg.Length - 1);
                    jsonMsg += "]}";
                    print(jsonMsg);
                    string url = "http://localhost:8080/bar";
                    WWWForm form = new WWWForm();
                    form.AddField("JSON:", jsonMsg);
                    WWW www = new WWW(url, form);
                    yield return www;
                    if (www.error == null)
                    {
                        //Debug.Log("WWW Ok!: " + www.data);
                    }
                    else
                    {
                        //Debug.Log("WWW Error: " + www.error);
                    }
                }
                else print("playerscores is null");
            }
            yield return new WaitForSeconds(5);
        }
    }


    // Update is called once per frame
    void Update () {
	
	}
}