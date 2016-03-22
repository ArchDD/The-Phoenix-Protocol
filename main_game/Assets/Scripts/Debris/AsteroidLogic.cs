﻿/*
    Sets a unique scale and rotation. Handles destruction effects.
*/

using UnityEngine;
using System.Collections;

public class AsteroidLogic : MonoBehaviour 
{
	private GameSettings settings;

	// Configuration parameters loaded through GameSettings
	private int maxDroppedResources; // The maximum number of resources that can be dropped by an asteroid. 

	private float health;
	private int droppedResources;                 // The amount of resources dropped by the asteroid

    private GameState gameState;
    private ObjectPoolManager explosionManager;
    private ObjectPoolManager logicManager;
    private ObjectPoolManager asteroidManager;

	void Start()
	{
		settings = GameObject.Find("GameSettings").GetComponent<GameSettings>();
        
		LoadSettings();
	}

	private void LoadSettings()
	{
		maxDroppedResources = settings.AsteroidMaxDroppedResources;
	}

	void OnEnable() 
    {
		droppedResources = System.Convert.ToInt32(Random.Range (0, maxDroppedResources));
	}
		
    // Initialise player, size, and speed
	public void SetPlayer(GameObject temp, float var, int rnd, ObjectPoolManager cachedManager, ObjectPoolManager cachedLogic, ObjectPoolManager cachedAsteroid)
	{
        asteroidManager  = cachedAsteroid;
        explosionManager = cachedManager;
        logicManager     = cachedLogic;

        float random = Random.Range(5f, var);
		transform.parent.localScale = new Vector3(random + Random.Range (0, 15), random + Random.Range (0, 15),random + Random.Range (0, 15));
        transform.parent.gameObject.GetComponent<AsteroidCollision>().SetCollisionDamage(random);
        health = (transform.parent.localScale.x + transform.parent.localScale.y + transform.parent.localScale.z) * 2f;
		transform.parent.rotation = Random.rotation;
	}

	public void SetStateReference(GameState state)
	{
		gameState = state;
	}

	// Allows asteroid to take damage and spawns destroy effect
	public void collision (float damage)
	{
        health -= damage;

		if (health <= 0 && transform.parent != null) // The null check prevents trying to destroy an object again while it's already being destroyed
        {
			// Ship automatically collects resources from destroyed asteroids. 
			gameState.AddShipResources(droppedResources);
            gameState.RemoveAsteroid(transform.parent.gameObject);

            GameObject temp = explosionManager.RequestObject();
            temp.transform.position = transform.position;
            explosionManager.EnableClientObject(temp.name, temp.transform.position, temp.transform.rotation, temp.transform.localScale);
            
			string removeName = transform.parent.gameObject.name;
            transform.parent = null;
            asteroidManager.DisableClientObject(removeName);
            asteroidManager.RemoveObject(removeName);
            logicManager.RemoveObject(gameObject.name);
        }
	}
}
