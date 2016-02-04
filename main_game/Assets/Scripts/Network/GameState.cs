﻿/*
    2015-2016 Team Pyrolite
    Project "Sky Base"
    Authors: Dillon Keith Diep, Andrei Poenaru
    Description: The game state resides solely on the server, holding a collection of data that allows clients to replicate
*/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameState : MonoBehaviour {

    public enum Status { Setup, Started };

	// The amount of resources the player starts off with.
	// https://bitbucket.org/pyrolite/game/wiki/Collecting%20Resources
	private const int BASE_SHIP_RESOURCES = 100;

    private Status status = Status.Setup;
    private List<GameObject> asteroidList;
    private List<GameObject> newAsteroids;
    private List<uint> removedAsteroids;
    private List<GameObject> enemyList;
    private List<GameObject> engineerList;
    private GameObject playerShip;
	private int[] playerScore;

	// The total ship resources that has been collected over the whole game.
	// This is used for the final score.
	private int totalShipResources = BASE_SHIP_RESOURCES;

	// The ships resources value that is shown to the commander, this is used to purchase upgrades. 
	private int currentShipResources = BASE_SHIP_RESOURCES;
    
    void Update()
    {
    	if(Input.GetKeyDown (KeyCode.Escape))
    	{
    		Application.Quit ();
    	}
    }

    public Status GetStatus()
    {
        return status;
    }

    public void SetStatus(Status newStatus)
    {
        status = newStatus;
    }

    // Asteroid list getters and setters
    public List<GameObject> GetAsteroidList()
    {
        return asteroidList;
    }

    public int GetAsteroidListCount()
    {
        return asteroidList.Count;
    }

    public void AddAsteroidList(GameObject asteroidObject)
    {
        asteroidList.Add(asteroidObject);
        newAsteroids.Add(asteroidObject);
    }

    public void RemoveAsteroid(GameObject removeObject)
    {        
        bool wasDeleted = newAsteroids.Remove(removeObject);
        if (!wasDeleted) removedAsteroids.Add((uint)removeObject.GetInstanceID());
        asteroidList.Remove(removeObject);
        AsteroidSpawner.numAsteroids--;
    }

    public void RemoveAsteroidAt(int i)
    {
        bool wasDeleted = newAsteroids.Remove(asteroidList[i]);
        if (!wasDeleted) removedAsteroids.Add((uint)asteroidList[i].GetInstanceID());
        asteroidList.RemoveAt(i);
        AsteroidSpawner.numAsteroids--;
    }

    public GameObject GetAsteroidAt(int i)
    {
        return asteroidList[i];
    }

    // Enemy list getters and setters
    public List<GameObject> GetEnemyList()
    {
        return enemyList;
    }

    public int GetEnemyListCount()
    {
        return enemyList.Count;
    }

    public void AddEnemyList(GameObject enemyObject)
    {
        enemyList.Add(enemyObject);
    }

    public void RemoveEnemyAt(int i)
    {
        enemyList.RemoveAt(i);
    }

    public GameObject GetEnemyAt(int i)
    {
        return enemyList[i];
    }

    // Engineer list getters and setters
    public List<GameObject> GetEngineerList()
    {
        return engineerList;
    }

    public int GetEngineerCount()
    {
        return engineerList.Count;
    }

    public void AddEngineerList(GameObject engineerObject)
    {
        engineerList.Add(engineerObject);
    }

    public void RemoveEngineerAt(int i)
    {
        engineerList.RemoveAt(i);
    }

    public GameObject GetEngineerAt(int i)
    {
        return engineerList[i];
    }

    public GameObject GetPlayerShip()
    {
        return playerShip;
    }

    public void SetPlayerShip(GameObject newPlayerShip)
    {
        playerShip = newPlayerShip;
    }

    public List<GameObject> GetNewAsteroids()
    {
        return newAsteroids;
    }

    public void ClearNewAsteroids()
    {
        newAsteroids = new List<GameObject>();
    }

    public List<uint> GetRemovedAsteroids()
    {
        return removedAsteroids;
    }

    public void ClearRemovedAsteroids()
    {
        removedAsteroids = new List<uint>();
    }

    private void InitializeVariables()
    {
        asteroidList = new List<GameObject>();
        enemyList = new List<GameObject>();
        newAsteroids = new List<GameObject>();
        removedAsteroids = new List<uint>();
        engineerList = new List<GameObject>();
		playerScore = new int[4];
		ResetPlayerScores();
    }

    public void Setup()
    {
        InitializeVariables();
    }

	public void ResetPlayerScores() 
	{
		for(int id = 0; id < 4; id++) 
		{
			playerScore[id] = 0;
		}
	}
	public void AddPlayerScore(int id, int score) 
	{
		playerScore[id] += score;
		Debug.Log("Score for player " + id + " is now " + playerScore[id]);
	}

	/*
	 * Gets the current ship resources
	*/
	public int GetShipResources() {
		return currentShipResources;
	}

	/*
	 * Gets the total ship resources for the whole game
	*/
	public int GetTotalShipResources() {
		return totalShipResources;
	}

	/*
	 * Adds a value to the total ship resources
	*/
	public void AddShipResources(int resources) {
		currentShipResources += resources;
		totalShipResources += resources;
		Debug.Log("Current Reaources:  " + currentShipResources);
	}

	/*
	 * Subtracts a value to the total ship resources
	*/
	public void UseShipResources(int resources) {
		currentShipResources -= resources;
	}
}
