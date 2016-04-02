﻿/*
    Manage network initialisation
*/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;

public class ServerManager : NetworkBehaviour
{
	#pragma warning disable 0649 // Disable warnings about unset private SerializeFields
	[SerializeField] private GameObject poolManager;
	#pragma warning restore 0649

    private GameState gameState;
    private NetworkManager networkManager;
    private Dictionary<uint, RoleEnum> netIdToRole;
    private Dictionary<uint, NetworkConnection> netIdToConn;
    private Dictionary<int, GameObject> screenIdToCrosshair;
    private PlayerController playerController;
    private NetworkMessageDelegate originalAddPlayerHandler;
    private GameObject musicManager, missionManager;
    public GameObject spawner;
    public GameObject cutsceneManager;
    public GameObject gameTimer;
    private GameObject portal;
    private GameObject readyScreen;

    private uint serverId;
    public int clientIdCount()
    {
        return netIdToRole.Count;
    }
    // Used to spawn network objects
    public static void NetworkSpawn(GameObject spawnObject)
    {
        NetworkServer.Spawn(spawnObject);
    }

    void Awake()
    {
        foreach(Transform child in poolManager.transform)
        {
            child.gameObject.GetComponent<ObjectPoolManager>().SpawnObjects();
        }
    }

    void Start()
    {
        Cursor.visible = true; //leave as true for development, false for production
        // Server and clients need to know screenId matching crosshairs
        screenIdToCrosshair = new Dictionary<int, GameObject>();
        if (MainMenu.startServer)
        {
            // Spawn Object Pooling Controller first
            // Save the original add player handler to save ourselves some work
            // and register a new one
            originalAddPlayerHandler = NetworkServer.handlers[MsgType.AddPlayer];
            NetworkServer.RegisterHandler(MsgType.AddPlayer, OnClientAddPlayer);

			netIdToRole = new Dictionary<uint, RoleEnum>();
            netIdToConn = new Dictionary<uint, NetworkConnection>();

            // Assign clients
            gameState = gameObject.GetComponent<GameState>();

            // Set up the game state
            gameState.Setup();
            this.gameObject.transform.GetChild(0).gameObject.SetActive(true);
            CreateServerSetup();
        }
    }
    
    [ClientRpc]
	public void RpcAddCrosshairObject(int screenId, GameObject crosshairObject)
	{
		screenIdToCrosshair.Add(screenId, crosshairObject);

		PlayerController localController = null;
		if (ClientScene.localPlayers[0].IsValid)
			localController = ClientScene.localPlayers[0].gameObject.GetComponent<PlayerController>();

		if (screenId != localController.GetScreenIndex() || localController.GetRole() != RoleEnum.Camera)
		{
			screenIdToCrosshair[screenId].SetActive(false);
		}
	}

    public GameObject GetCrosshairObject(int screenId)
    {
        return screenIdToCrosshair[screenId];
    }
		
    // Called automatically by Unity when a player joins
    private void OnClientAddPlayer(NetworkMessage netMsg)
    {
        originalAddPlayerHandler(netMsg);

        uint netId = netMsg.conn.playerControllers[0].gameObject.GetComponent<NetworkIdentity>().netId.Value;
        netIdToConn.Add(netId, netMsg.conn);
    }

    /// <summary>
    /// Associates each player controller in the list of player controller IDs
    /// with the Engineer role
    /// </summary>
    /// <param name="playerControllerIds">IDs of the player controllers to be associated as Engineer</param>
    public void SetEngineers(uint[] playerControllerIds)
    {
        foreach (uint id in playerControllerIds)
        {
            if (id != 0)
				netIdToRole.Add(id, RoleEnum.Engineer);
            else
                Debug.LogError("The host cannot be an engineer!");
        }
    }

    /// <summary>
    /// Associates the player controller with given ID to the role Commander
    /// </summary>
    /// <param name="playerControllerId">ID of the player controller to be associated as Commander</param>
    public void SetCommander(uint playerControllerId)
    {
        netIdToRole.Add(playerControllerId, RoleEnum.Commander);
    }

    /// <summary>
    /// Notifies all engineers of a new upgrade/repair
    /// job to do
    /// </summary>
    /// <param name="upgrade">Whether the job is an upgrade or a repair</param>
    /// <param name="part">The part to upgrade/repair</param>
	public void NotifyEngineer(bool upgrade, ComponentType part)
    {
        // Notify every engineer of the upgrade/repair
        foreach (KeyValuePair<uint, RoleEnum> client in netIdToRole)
		{
            if (client.Value == RoleEnum.Engineer)
            {
                // Create the message to send
                EngineerJobMessage msg = new EngineerJobMessage();
                msg.upgrade = upgrade;
                msg.part = part;

                // Send to the engineer
                NetworkServer.SendToClient(netIdToConn[client.Key].connectionId, MessageID.ENGINEER_JOB, msg);
            }
        }
    }

    private void Update ()
    {
        if(Input.GetKeyDown(KeyCode.R))
            Reset();
    }

    /// <summary>
    /// Sends a job finished message to the command console
    /// </summary>
    /// <param name="isUpgrade">Whether the job is an upgrade or a repair</param>
    /// <param name="component">The component that the job was completed on</param>
    public void SendJobFinished(bool isUpgrade, ComponentType component)
    {
        // Find the commander's netId
        foreach (KeyValuePair<uint,RoleEnum> client in netIdToRole)
        {
            if (client.Value == RoleEnum.Commander)
            {
                EngineerJobMessage msg = new EngineerJobMessage();
                msg.upgrade = isUpgrade;
                msg.part = component;

                NetworkServer.SendToClient(netIdToConn[client.Key].connectionId, MessageID.JOB_FINISHED, msg);
            }
        }
    }

    /// <summary>
    /// Returns the list of officers as a string in the following format
    /// officer_name:officer_id,other_officer_name:other_officer_id,
    /// </summary>
    public void SendOfficers()
    {
        Dictionary<string, uint> officerList = gameObject.GetComponent<TCPServer>().PlayerNameToPlayerID;
        string data = "";

        // Build the string representing all the officers. Note that when splitting
        // by "," the last element will be empty and should be ignored
        foreach (KeyValuePair<string, uint> officerData in officerList)
        {
            data += officerData.Key + ":" + officerData.Value.ToString() + ",";
        }

        // Send the data to the commander
        foreach (KeyValuePair<uint, RoleEnum> client in netIdToRole)
        {
            if (client.Value == RoleEnum.Commander)
            {
                // Create the message to send
                OfficerListMessage msg = new OfficerListMessage();
                msg.officerData = data;

                // Send the data to the commander
                NetworkServer.SendToClient(netIdToConn[client.Key].connectionId, MessageID.OFFICER_LIST, msg);
            }
        }
    }

    /// <summary>
    /// Sends the data to the specified officer
    /// </summary>
    /// <param name="officerName">Username of officer</param>
    /// <param name="officerId">User ID of officer</param>
    /// <param name="data">The data</param>
    public void SendToOfficer(string officerName, uint officerId, string data)
    {
        //TODO: Pack the data as JSON
    }

    /// <summary>
    /// Broadcasts the data to all officers
    /// </summary>
    /// <param name="data"></param>
    public void BroadcastToOfficers(string data)
    {
        //TODO: Pack the data as JSON
    }

    private void CreateServerSetup()
    {
        //Spawn server lobby
        Instantiate(Resources.Load("Prefabs/ServerLobby", typeof(GameObject)));
    }

    public void StartGame()
    {
        // Spawn Cutscene Manager
        cutsceneManager = Instantiate(Resources.Load("Prefabs/CutsceneManager", typeof(GameObject))) as GameObject;
        ServerManager.NetworkSpawn(cutsceneManager);

        // Spawn networked ship
        GameObject playerShip = Instantiate(Resources.Load("Prefabs/PlayerShip", typeof(GameObject))) as GameObject;
        gameState.PlayerShip = playerShip;
        ServerManager.NetworkSpawn(playerShip);

        // Get the engineer start position
        NetworkStartPosition engineerStartPos = playerShip.GetComponentInChildren<NetworkStartPosition>();

        // Spawn the engineers at the engineer start position
        foreach (KeyValuePair<uint, RoleEnum> client in netIdToRole)
        {
            if (client.Value == RoleEnum.Engineer)
            {
                // Create the engineer object
                GameObject engineer = Instantiate(Resources.Load("Prefabs/Engineer", typeof(GameObject)),
                    engineerStartPos.transform.position, engineerStartPos.transform.rotation) as GameObject;
                gameState.AddToEngineerList(engineer);

                // Spawn the engineer with local authority
                NetworkServer.SpawnWithClientAuthority(engineer, netIdToConn[client.Key]);

                // Let the client know of the object it controls
                ControlledObjectMessage response = new ControlledObjectMessage();
                response.controlledObject = engineer;
                NetworkServer.SendToClient(netIdToConn[client.Key].connectionId, MessageID.OWNER, response);
            }
        }

        //On start, RPC every player to mount camera on ship
        if (ClientScene.localPlayers[0].IsValid)
            playerController = ClientScene.localPlayers[0].gameObject.GetComponent<PlayerController>();

        if (playerController != null)
            playerController.RpcRoleInit();

        // Spawn music controller only on server
        musicManager = Instantiate(Resources.Load("Prefabs/MusicManager", typeof(GameObject))) as GameObject;

        // Spawn mission manager only on server
        missionManager = Instantiate(Resources.Load("Prefabs/MissionManager", typeof(GameObject))) as GameObject;
        missionManager.GetComponent<MissionManager>().SetPlayerController(playerController);

        //Spawn shield
        GameObject playerShield = Instantiate(Resources.Load("Prefabs/Shield", typeof(GameObject))) as GameObject;
        ServerManager.NetworkSpawn(playerShield);

		// Spawn portal
		portal = Instantiate(Resources.Load("Prefabs/Portal", typeof(GameObject))) as GameObject;
        portal.transform.position = new Vector3(0,1000,1000);
		gameState.Portal = portal;
		ServerManager.NetworkSpawn(portal);
        portal.AddComponent<PortalLogic>();
        playerController.RpcPortalInit(portal);
        RpcSetPortal(portal);

        //Instantiate ship logic on server only
        GameObject playerShipLogic = Instantiate(Resources.Load("Prefabs/PlayerShipLogic", typeof(GameObject))) as GameObject;
        playerShipLogic.transform.parent = playerShip.transform;
            
        //Instantiate ship shoot logic on server only
        GameObject playerShootLogic = Instantiate(Resources.Load("Prefabs/PlayerShootLogic", typeof(GameObject))) as GameObject;
        playerShootLogic.transform.parent = playerShip.transform;
        //playerShootLogic.GetComponent<PlayerShooting>().Setup();

        GameObject commander = Instantiate(Resources.Load("Prefabs/CommanderAbilities", typeof(GameObject))) as GameObject;
        commander.transform.parent = playerShip.transform;
        commander.transform.localPosition = Vector3.zero;

        //Set up the game state
        playerController.SetControlledObject(playerShip);

        spawner = GameObject.Find("Spawner");
        gameTimer = GameObject.Find("GameTimerText");
    }

    public void SetReadyScreen(GameObject newReadyScreen)
    {
        readyScreen = newReadyScreen;
    }

    public void Reset()
    {
        // Prevent game loop updating
        gameState.Status = GameState.GameStatus.Setup;
        Debug.Log("Resetting values");

        // Overlay ready screen
        readyScreen.GetComponent<ReadyScreen>().Reset();
            
        // Restart music - consider reset method for sound system
        musicManager.GetComponent<MusicManager>().PlayMusic(0);

        // Reactivate possibly deactivated objects
        if (!gameTimer.activeSelf)
        {
            gameTimer.SetActive(true); 
        }
        RpcActivatePortal();

        // Reset portal trigger - this needs to be before game status change
        gameState.gameObject.GetComponent<GameStatusManager>().Reset();
        
        // Reset missions
        missionManager.GetComponent<MissionManager>().ResetMissions();
        
        // Reset Player's scores
        gameState.ResetPlayerScores();
        // Reset spawner attributes, each spawner removes objects from logic using state list
        spawner.GetComponent<EnemySpawner>().Reset();
        spawner.GetComponent<AsteroidSpawner>().Reset();
        spawner.GetComponent<OutpostSpawner>().Reset();
        GameObject.Find("OutpostManager(Clone)").GetComponent<OutpostManager>().Reset();

        // Reset player ship
        gameState.PlayerShip.transform.position = new Vector3(0.0f,0.0f,-2000.0f);
        gameState.PlayerShip.transform.rotation = Quaternion.identity;
        gameState.PlayerShip.GetComponentInChildren<PlayerShooting>().Reset();
        gameState.PlayerShip.GetComponentInChildren<ShipMovement>().Reset();

        // Reset engineers
        foreach (GameObject engineer in gameState.GetEngineerList())
        {
            engineer.GetComponent<EngineerController>().Reset();
        }

        // Reset commander
        RpcCommanderReset();

        // Reset game state
        gameState.Reset();

        // Game state to be updated through ReadyScreen
    }

    [ClientRpc]
    private void RpcCommanderReset()
    {
        GameObject commander = GameObject.Find("CommanderManager(Clone)");
        if (commander != null)
        {
            commander.GetComponent<CommandConsoleState>().Reset();
        }
    }


	public void SetServerId(uint serverId) 
	{
		this.serverId = serverId;
	}

	public uint GetServerId()
	{
		return serverId;
	}

    [ClientRpc]
    private void RpcSetPortal(GameObject newPortal)
    {
        portal = newPortal;
    }

    [ClientRpc]
    private void RpcActivatePortal()
    {
        portal.SetActive(true);
    }

    public void SetCrosshairPosition(int crosshairId, int screenId, Vector2 position)
    {
        GameObject crosshairObject = screenIdToCrosshair[screenId];
        crosshairObject.GetComponent<CrosshairMovement>().SetPosition(crosshairId, position);
    }
}
