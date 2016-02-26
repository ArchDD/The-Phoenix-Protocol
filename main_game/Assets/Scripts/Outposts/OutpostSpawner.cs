﻿using UnityEngine;
using System.Collections;

public class OutpostSpawner : MonoBehaviour 
{
	// The different outpost models
	[SerializeField] GameObject outpost1;

	[SerializeField] GameObject resources;     // The resources prefab
	[SerializeField] float collectionDistance; // The distance from the outpost the ship has to be in order to collect resources

	[SerializeField] GameObject gameManager;
	private GameState gameState;
	private EnemySpawner enemySpawner;
	private const float OUTPOST_MIN_DISTANCE = 1000;

	private GameObject player, outpost, logic, spawnLocation, outpostManager;

	[SerializeField] int maxOutposts;
	private int numOutposts = 0;

	void Start ()
	{
		gameState    = gameManager.GetComponent<GameState>();
		enemySpawner = gameState.GetComponentInChildren<EnemySpawner>();

		logic = Instantiate(Resources.Load("Prefabs/OutpostLogic", typeof(GameObject))) as GameObject;
        //print("starting outpost manager");
        outpostManager = Instantiate(Resources.Load("Prefabs/OutpostManager", typeof(GameObject))) as GameObject;
        OutPostManager outpostManagerScript = outpostManager.GetComponent<OutPostManager>();
        outpostManagerScript.giveGameStateReference(gameState);
        spawnLocation = new GameObject();
		spawnLocation.name = "OutpostSpawnLocation";
	}
		
	void Update() {
		if (gameState.GetStatus() == GameState.Status.Started)
		{
			if(numOutposts < maxOutposts)
			{
				if(player == null) 
					player = gameState.GetPlayerShip();
			
				spawnLocation.transform.position = player.transform.position;
				// The range (90,-90) is in in front of the ship. 
				spawnLocation.transform.eulerAngles = new Vector3(Random.Range(-10,10), Random.Range(90,-90), Random.Range(90,-90));

				// Loop until we find a position that is not close to another outpost
				do {
					spawnLocation.transform.Translate(transform.forward * Random.Range(1000,2000));
				} while(!CheckOutpostProximity(spawnLocation.transform.position));

				SpawnOutpost ();
				numOutposts++;
			}
		}

	}

	/// <summary>
	/// Checks if the new outpost is closer than OUTPOST_MIN_DISTANCE to any existing outposts
	/// </summary>
	/// <returns><c>true</c>, if outpost is further than OUTPOST_MIN_DISTANCE away from any outposts, <c>false</c> otherwise.</returns>
	/// <param name="position">Position.</param>
	private bool CheckOutpostProximity(Vector3 position) 
	{
		foreach(GameObject outpost in gameState.GetOutpostList()) {
			if(Vector3.Distance(position, outpost.transform.position) < OUTPOST_MIN_DISTANCE) {
				return false;
			}
		}
		return true;
	}

	private void SpawnOutpost() 
	{
		// Set up like this as we may have different outposts models like we do with asteroids. 
		outpost = outpost1;

		// Spawn object and logic
		GameObject outpostObject    = Instantiate(outpost, spawnLocation.transform.position, Quaternion.identity) as GameObject;
		GameObject outpostLogic     = Instantiate(logic, spawnLocation.transform.position, Quaternion.identity) as GameObject;
		GameObject outpostResources = Instantiate(resources, spawnLocation.transform.position, Quaternion.identity) as GameObject;

		// Initialise logic
		outpostLogic.transform.parent = outpostObject.transform;
		outpostLogic.transform.localPosition = Vector3.zero;
		outpostObject.AddComponent<OutpostCollision>();

		//outpostLogic.GetComponent<OutpostLogic>().SetPlayer(state.GetPlayerShip(), maxVariation, rnd);
		outpostLogic.GetComponent<OutpostLogic>().SetStateReference(gameState);

		// Set the resources collider on a child object to avoid shooting and outpost collision issues
		outpostResources.transform.parent = outpostObject.transform;
		outpostResources.GetComponent<ResourcesCollision>().SetOutpost(outpostObject);
		outpostResources.GetComponent<SphereCollider>().radius = collectionDistance;

		Rigidbody rigid = outpostObject.AddComponent<Rigidbody>();
		rigid.isKinematic = true;
		gameState.AddOutpostList(outpostObject);
		ServerManager.NetworkSpawn(outpostObject);

		// Request the enemy spawner to spawn protecting ships around this outpost
		int numGuards = Random.Range(5, 10); // TODO: might want to set this manually based on difficulty
		enemySpawner.RequestSpawnForOutpost(numGuards, spawnLocation.transform.position);
	}

}