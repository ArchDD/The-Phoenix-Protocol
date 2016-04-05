﻿using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class TCPServer : MonoBehaviour
{    
    public enum MsgType
	{
        GameEnded
    }

	private GameSettings settings;

	// Configuration parameters loaded through GameSettings
    private int listenPort;
    private int maxReceivedMessagesPerInterval;
    
    // Constants for splitting the received messages
    private readonly String[] semiColon = {";"};
    private readonly String[] colon = {":"};
    private readonly String[] comma = {","};
    private readonly String[] plus = {"+"};

    private GameState gameState;
    private UDPServer udpServer;
    private ServerManager serverManager;
    private TcpListener tcpServer = null;
    private Socket client = null;
    private bool connected = false; // no easy way to tell from library
    private byte[] recvBuff = new byte[1024]; // allocate 1KB receive buffer
    
	// Use this for initialization
	void Start ()
    {
        
	}
    
    public void Initialise()
    {
        settings = GameObject.Find("GameSettings").GetComponent<GameSettings>();
		LoadSettings();
        Debug.Log("Starting TCP server on port: " + listenPort);
        tcpServer = new TcpListener(IPAddress.Any, listenPort);
        gameState = this.gameObject.GetComponent<GameState>();
        udpServer = this.gameObject.GetComponent<UDPServer>();
        serverManager = this.gameObject.GetComponent<ServerManager>();

        // Start listening for client requests
        tcpServer.Start();
        
        StartCoroutine(ConnectionHandler());
        StartCoroutine(SendUpdatedObjects());
    }

	private void LoadSettings()
	{
		listenPort 					   = settings.TCPListenPort;
		maxReceivedMessagesPerInterval = settings.TCPMaxReceivedMessagesPerInterval;
	}
	
	// Update is called once per frame
	void Update ()
    {
        
	}
    
    // Handles incomming messages from the phone server
    IEnumerator ConnectionHandler()
    {
        while (true)
        {
            // Probe for a new connection
            while (client == null || !connected)
            {
                if (tcpServer.Pending())
                {
                    client = tcpServer.AcceptSocket();
                    connected = true;
                    Debug.Log("TCP Client Connected! " + ((IPEndPoint)client.RemoteEndPoint).ToString());
                    
                    // Tell the UDP channel the address of the phone server
                    udpServer.SetClientAddress(((IPEndPoint)client.RemoteEndPoint).Address);
                }
                yield return new WaitForSeconds(0.2f);
            }
            
            // Receive data untill the connection is closed
            while (connected)
            {
                int numRead;
                String newData;
                String[] messages;
                int receivedMessages = 0;
                // read data if availale
                while (client.Available > 0 && receivedMessages < maxReceivedMessagesPerInterval)
                {
                    numRead = client.Receive(recvBuff, recvBuff.Length, 0);
                    newData = Encoding.ASCII.GetString(recvBuff, 0, numRead);
                    // It is possible to get multiple messages in a single receive
                    messages = newData.Split(semiColon, StringSplitOptions.RemoveEmptyEntries);
                    // bla
                    foreach (String msg in messages)
                    {
                        HandleMessage(msg);
                        receivedMessages++;
                    }
                }
                // check if the connection is closed
                if ((client.Available == 0) && client.Poll(1000, SelectMode.SelectRead))
                {
                    Debug.Log("TCP Connection Closed! " + ((IPEndPoint)client.RemoteEndPoint).ToString());
                    connected = false;
                    client.Shutdown(SocketShutdown.Both);
                    client.Close();
                    
                    // Stops the transmission on the UDP channnel
                    udpServer.SetClientAddress(IPAddress.Any);
                }
                yield return new WaitForSeconds(0.2f);
            }
        }
    }
    
    /// <summary>
	/// Sends a signal to the phone server, notifying of a change
    /// in state. The valid signals are as follows:
    /// SETUP_STAGE: this is used to notify that a game has ended
    ///              and the system should enter the pre-game
    ///              state for the next game.
	/// </summary>
    /// <return>
    /// Indicates whether the signal was sent succesfully.
    /// </return>
    public bool SendSignal(MsgType type)
    {
        switch (type)
        {
            case MsgType.GameEnded:
                return SendMsg("{\"type\":\"GM_END\"}");
            default:
                return false;
        }
    }
    
    // Multiplexes the received message into unique actions
    private void HandleMessage(String msg)
    {
        String[] fields;
        String[] subFields;
        String[] parts = msg.Split(colon, StringSplitOptions.RemoveEmptyEntries);
        switch(parts[0])
        {
            case "START":
                Dictionary<uint, Officer> officerMap = gameState.GetOfficerMap();
                // TODO: implement the actions caused by this message
                Debug.Log("Received a Start Game signal with data:");
                fields = parts[1].Split(comma, StringSplitOptions.RemoveEmptyEntries);

                // Clear the officer dictionary to avoid having officers from last game in there
                officerMap.Clear();
                foreach (String plr in fields)
                {
                    subFields = plr.Split(plus, StringSplitOptions.RemoveEmptyEntries);
                    String userName = subFields[0];
                    uint userId = UInt32.Parse(subFields[1]);
                    officerMap.Add(userId, new Officer(userId, userName));
                    Debug.Log("Username: " + userName + " id:" + userId);
                }
                // Send the map of officers to clients that need it
                serverManager.SendOfficers();
                ReadyScreen readyScreen = GameObject.Find("ReadyCanvas(Clone)").GetComponent<ReadyScreen>();
                readyScreen.InitialiseGame();
                break;
            case "CTRL":
                int idOfControlled = Int32.Parse(parts[1]);
                Debug.Log("Received an Enemy Controll signal: id: " + idOfControlled);

                // Set enemy to controlled by spectator
                if (udpServer.InstanceIDToEnemy.ContainsKey(idOfControlled))
                    udpServer.InstanceIDToEnemy[idOfControlled].GetComponentInChildren<EnemyLogic>().SetHacked(true);
                else
                    Debug.LogError("Tried to take control of invalid enemy");

                break;
            case "RESET":
                serverManager.Reset();
                break;
            default:
                Debug.Log("Received an unexpected message: " + msg);
                break;
        }
    }
    
    IEnumerator SendUpdatedObjects()
    {
        while (true)
        {
            if(connected)
            {
                try
                {
                    SendNotificationsUpdate();                    
                } catch(Exception e) {
                    Debug.LogError(e);
                }
            }
            yield return new WaitForSeconds(0.07f);
        }
    }
    
    // Send the changes in notifications
    private void SendNotificationsUpdate()
    {
        List<Notification> newNotif = gameState.GetNewNotifications();
        List<Notification> remNotif = gameState.GetRemovedNotifications();
        bool hasNew = newNotif != null && newNotif.Count > 0;
        bool hasRemoved = remNotif != null && remNotif.Count > 0;
        if (hasNew || hasRemoved)
        {
            string jsonMsg = "{\"type\":\"NOTIFY_UPD\",\"data\":[";
            if (hasNew)
            {
                foreach (Notification notif in newNotif)
                {
                    jsonMsg += "{\"type\":\"" + ComponentTypeToString(notif.Component) + "\",";
                    jsonMsg += "\"isUpgrade\":" + notif.IsUpgrade.ToString().ToLower() + ",";
                    jsonMsg += "\"toSet\":true},";
                }
            }
            if (hasRemoved)
            {
                foreach (Notification notif in remNotif)
                {
                    jsonMsg += "{\"type\":\"" + ComponentTypeToString(notif.Component) + "\",";
                    jsonMsg += "\"isUpgrade\":" + notif.IsUpgrade.ToString().ToLower() + ",";
                    jsonMsg += "\"toSet\":false},";
                }
            }
            jsonMsg = jsonMsg.Remove(jsonMsg.Length - 1);
            jsonMsg += "]}";
            bool success = SendMsg(jsonMsg);
            if (success) {
                gameState.ClearNewNotifications();
                gameState.ClearRemovedNotifications();
            }
        }
    }
    
    // Send a JSON encoded message to the phone server
    // return value indicates the success of the send
    private bool SendMsg(String jsonMsg)
    {
        if (client == null || !connected)
        {
            return false;
        }
        else
        {
            Byte[] data = Encoding.ASCII.GetBytes(jsonMsg);
            try
            {
                client.Send(data);
                // Debug.Log("Sent: " + jsonMsg);
                return true;
            }
            // Might not be the best way to deal with exceptions here
            // but it hasn't caused problems yet
            catch (SocketException)
            {
                connected = false;
                return false;
            }
        }
    }
    
    private String ComponentTypeToString(ComponentType comp)
    {
        switch(comp) {
            case ComponentType.ShieldGenerator:
                return "SHIELDS";
            case ComponentType.Turret:
                return "TURRETS";
            case ComponentType.Engine:
                return "ENGINES";
            case ComponentType.Hull:
                return "HULL";
        }
        
        return "";
    }
}
