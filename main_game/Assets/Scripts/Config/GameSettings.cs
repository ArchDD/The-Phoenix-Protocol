﻿/* Relevant documentation: https://bitbucket.org/pyrolite/game/wiki/Game%20Settings */

using UnityEngine;
using System.Collections;

/// <summary>
/// Class that contains all the game settings. These are all set through the Unity IDE and should only be retrieved from here.
/// </summary>
public class GameSettings : MonoBehaviour
{
	public GameObject GameManager;

	[Header("Asteroid Logic")]
	public int AsteroidMaxDroppedResources; // The maximum number of resources that can be dropped by an asteroid. 
	public float AsteroidMinSpeed;
	public float AsteroidMaxSpeed;

	[Header("Asteroid Spawner")]
	public int MaxAsteroids;            		// Maximum number of asteroids that can exist simultaneously
	public float AsteroidMaxVariation;  		// Max variation in size (0-10)
	public float AsteroidMinDistance;   		// Minimum distance to the player that an asteroid can spawn
	public float AsteroidMaxDistance;   		// Maximum distance to the player that an asteroid can spawn
	public int MaxAsteroidsSpawnedPerFrame;
	public float AsteroidAvgSize;               // The average asteroid size. Please update this manually if you change the sizes to avoid useless computation
	public float AsteroidFieldSpacingFactor;    // Higher values make asteroid fields more sparse. TODO: 2f looks good, but is quite expensive

	[Header("Enemy Spawner")]
	public float EnemyMinSpawnDistance;
	public float EnemyMaxSpawnDistance;
	public int MaxEnemies;
	public int AIWaypointsPerEnemy;
	public int AIWaypointGenerationFactor;
	public int AIWaypointRadius;
	public float AIWaypointWidthScale;
	public float AIWaypointHeightScale;
	public Vector3 AIWaypointShift;
	public int EnemyOutpostSpawnRadius; // The radius of the sphere around an outpost in which to spawn protecting enemies
	
	[Header("Enemy Logic")]
	public float EnemyShotsPerSec;
	public float EnemyShootPeriod;                  // How long in seconds the enemy should shoot for when it fires
	public int EnemyShootPeriodPercentageVariation; // Percentage variation +/- in the length of the shooting period
	public float EnemyShieldDelay;                  // Delay in seconds to wait before recharging shield
	public float EnemyShieldRechargeRate;           // Units of shield to increase per second
	public AudioClip EnemyFireSoundPrefab;
	public bool EnemyFireSoundRandomPitch;

	[Header("Engineer Controller")]
	public float EngineerWalkSpeed;
	public float EngineerMouseLookXSensitivity;
	public float EngineerMouseLookYSensitivity;
	public bool EngineerMouseLookClampVerticalRotation;
	public float EngineerMouseLookMinimumX;
	public float EngineerMouseLookMaximumX;
	public bool EngineerMouseLookSmooth;
	public float EngineerMouseLookSmoothTime;

	[Header("Main Menu")]
	public float MainMenuRotationSpeed;

	[Header("TCP Server")]
	public int TCPListenPort;
	public int TCPMaxReceivedMessagesPerInterval;

	[Header("UDP Server")]
	public int UDPListenPort;
	public int UDPClientPort;
	public int UDPMaxReceivedMessagesPerInterval;

	[Header("Outposts")]
	public GameObject OutpostModel1Prefab;
	public float OutpostResourceCollectionDistance; // The distance from the outpost the ship has to be in order to collect resources
	public int MaxOutposts;

	[Header("Ship Movement")]
	public float PlayerShipTurnSpeed;
	public float PlayerShipMaxTurnSpeed;
	public float PlayerShipSlowDown;
	public float PlayerShipShieldDelay; // Delay in seconds to wait before recharging shield

    [Header("Ship Properties")]
    public float PlayerShipStartingHealth;
    public float PlayerShipStartingShields;
    public float PlayerShipStartingRechargeRate;
    public float PlayerShipStartingSpeed;
    public int PlayerShipStartingResources;
    public int PlayerShipStartingCivilians;
    public float PlayerShipComponentHealth;

	[Header("Bullet Movement")]
	public float BulletSpeed;

	[Header("Player Shooting")]
	public float PlayerRateOfFire;
	public Texture2D PlayerHitmarker; // Hitmarker texture
	public AudioClip PlayerFireSound; // Sound to make when firing
	public bool PlayerFireSoundRandomPitch;
}
