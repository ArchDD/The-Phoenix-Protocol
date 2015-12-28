﻿using UnityEngine;
using System.Collections;

public class AsteroidLogic : MonoBehaviour 
{
	public GameObject player;
	float maxVariation; // Percentage variation in size
	[SerializeField] float health;
	[SerializeField] float minSpeed;
	[SerializeField] float maxSpeed;
	[SerializeField] GameObject destroyEffect;

	public float speed;
	int type;
    private GameState gameState;
	
	public void SetPlayer(GameObject temp, float var, int rnd)
	{
		player = temp;
		type = rnd;
		maxVariation = var;
		transform.parent.localScale = new Vector3(10f + Random.Range (-var, var), 10f + Random.Range (-var, var),10f + Random.Range (-var, var));
		transform.parent.rotation = Random.rotation;
		speed = Random.Range(minSpeed,maxSpeed);
	}

    public void SetStateReference(GameState state)
    {
        gameState = state;
    }

    public void collision (float damage)
	{
		health -= damage;
        if (health <= 0)
        {
          Instantiate(destroyEffect, transform.position, transform.rotation);
          ServerManager.NetworkSpawn(destroyEffect);
			    gameState.RemoveAsteroid(transform.parent.gameObject);
          Destroy(transform.parent.gameObject);	
        }
	}
}
