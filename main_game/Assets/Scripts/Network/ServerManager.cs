﻿/*
    2015-2016 Team Pyrolite
    Project "Sky Base"
    Authors: Dillon Keith Diep, Andrei Poenaru, Marc Steene
    Description: Manage network initialisation
*/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;

public class ServerManager : NetworkBehaviour {
    private GameState gameState;
    private NetworkManager networkManager;
    private int clientId = 0;
    private Dictionary<int, int> clientIds;
    private GameObject thePlayer;
    bool gameStarted;
    GameObject spawner;

    public int clientIdCount()
    {
        return clientIds.Count;
    }
    // Used to spawn network objects
    public static void NetworkSpawn(GameObject spawnObject)
    {
        NetworkServer.Spawn(spawnObject);
    }

    void Start()
    {
        Cursor.visible = true; //leave as true for development, false for production
        //networkManager = gameObject.GetComponent<NetworkManager>();

        if(MainMenu.startServer)
        {
            // Register handler for the get role message
            NetworkServer.RegisterHandler(789, OnClientPickRole);

            clientIds = new Dictionary<int,int>();
            // Host is client Id #0. Using -1 as the role for the Host
            clientIds.Add(0,-1);
            clientId = 0;
            gameStarted = false;
            // assign clients
            gameState = gameObject.GetComponent<GameState>();
            // Set up the game state
            gameState.Setup();
            this.gameObject.transform.GetChild(0).gameObject.SetActive(true);
            CreateServerSetup();
        }
    }

    private void OnClientPickRole(NetworkMessage netMsg)
    {
        PickRoleMessage msg = netMsg.ReadMessage<PickRoleMessage>();

        if (msg.role == (int) RoleEnum.ENGINEER)
        {
            // Add the engineer client to the client ID list, with the engineer role
            clientIds.Add(netMsg.conn.connectionId, msg.role);
        }
        else if (msg.role == (int) RoleEnum.CAMERA)
        {
            // Do camera things
        }
    }

    void CreateServerSetup()
    {
        //Spawn server lobby
        GameObject serverSetupCamera = Instantiate(Resources.Load("Prefabs/ServerSetupCamera", typeof(GameObject))) as GameObject;
    }

    public void StartGame()
    {
        //Get player controller
        if (ClientScene.localPlayers[0].IsValid)
            thePlayer = ClientScene.localPlayers[0].gameObject;

        //Spawn networked ship
        GameObject playerShip = Instantiate(Resources.Load("Prefabs/PlayerShip", typeof(GameObject))) as GameObject;
        gameState.SetPlayerShip(playerShip);
        ServerManager.NetworkSpawn(playerShip);

        // Get the engineer start position
        NetworkStartPosition engineerStartPos = playerShip.GetComponentInChildren<NetworkStartPosition>();

        // Spawn the engineers at the engineer start position
        foreach (KeyValuePair<int,int> client in clientIds)
        {
            if (client.Value == (int) RoleEnum.ENGINEER)
            {
                GameObject engineer = Instantiate(Resources.Load("Prefabs/Engineer", typeof(GameObject)),
                    engineerStartPos.transform.position, engineerStartPos.transform.rotation) as GameObject;
                gameState.AddEngineerList(engineer);
                NetworkSpawn(engineer);

                // Let the client know of the object it controls
                ControlledObjectMessage response = new ControlledObjectMessage();
                response.controlledObject = engineer;
                NetworkServer.SendToClient(client.Key, 890, response);
            }
        }

        //Instantiate local cameras for players after spawning ship
        thePlayer.GetComponent<PlayerController>().CallSetCamera();

        //Spawn shield
        GameObject playerShield = Instantiate(Resources.Load("Prefabs/Shield", typeof(GameObject))) as GameObject;
        ServerManager.NetworkSpawn(playerShield);

        //Instantiate ship logic on server only
        GameObject playerShipLogic = Instantiate(Resources.Load("Prefabs/PlayerShipLogic", typeof(GameObject))) as GameObject;
        //playerShipLogic.GetComponent<ShipMovement>().SetControlObject(playerShip);
        playerShipLogic.transform.parent = playerShip.transform;
            
        //Instantiate ship shoot logic on server only
        GameObject playerShootLogic = Instantiate(Resources.Load("Prefabs/PlayerShootLogic", typeof(GameObject))) as GameObject;
        playerShootLogic.transform.parent = playerShip.transform;

        //Instantiate crosshairs
        GameObject crosshairCanvas = Instantiate(Resources.Load("Prefabs/CrosshairCanvas", typeof(GameObject))) as GameObject;

        //Set up the game state
        thePlayer.GetComponent<PlayerController>().SetControlledObject(playerShip);

        //Start the game
        playerShip.GetComponentInChildren<ShipMovement>().StartGame();
        gameState.SetStatus(GameState.Status.Started);
        gameStarted = true;
    }

}
