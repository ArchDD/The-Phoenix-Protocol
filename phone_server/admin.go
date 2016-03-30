package main

import (
    "encoding/json"
    "fmt"
    "golang.org/x/net/websocket"
    "strconv"
    "time"
)

// Encodes the update message to the Admin console
type AdminUpdateMessage struct {
    State string
    // is the game ready to start
    Ready      bool
    Officers   []PlayerInfo
    Spectators []PlayerInfo
}

// Wrapper of player data that is to be send to the admin
type PlayerInfo struct {
    UserName string
    UserId   uint64
    Score    int
    IsOnline bool
}

// The web socket of the currently connected admin
var adminWebSocket *websocket.Conn = nil

// Used through stdin instructions to allow for a new admin instance
// Closes the current admin session and clears the global var
func unblockAdmin() {
    if adminWebSocket != nil {
        if err := adminWebSocket.Close(); err != nil {
            fmt.Println("Error force closing admin websocket: " + err.Error())
        }
        adminWebSocket = nil
    }
    fmt.Println("Admin: reset admin connection.")
}

// Returns if there is already an existing admin connection
func adminWebSocketHandler(webs *websocket.Conn) {
    if adminWebSocket == nil {
        fmt.Println("Admin: Admin client connected.")
        adminWebSocket = webs

        // start update timer and send firts update
        stopChan := make(chan struct{})
        go adminUpdateTimer(stopChan)
        updateAdmin()

        // block here while connection is active
        handleReceivedData(adminWebSocket)

        // stop update timer
        stopChan <- struct{}{}

        adminWebSocket = nil
        fmt.Println("Admin: Admin client disconnected.")
    }
}

// Listens for messages from the admin panel
// Returns when the connection is closed
func handleReceivedData(ws *websocket.Conn) {
    receivedtext := make([]byte, 1024)
    for {
        n, err := ws.Read(receivedtext)

        // stop listening for activity if an error occurs
        if err != nil {
            if err.Error() == "EOF" {
                fmt.Println("Admin: Connection Closed, EOF received.")
            } else {
                fmt.Printf("Admin: Error: %s\n", err)
            }
            return
        }

        // TODO: remove when no longer needed
        // fmt.Println("Received: ", string(receivedtext[:n]))

        // decode JSON
        var msg interface{}
        if err := json.Unmarshal(receivedtext[:n], &msg); err != nil {
            fmt.Println(err)
            continue
        }

        handleAdminMessage(msg.(map[string]interface{}))
    }
}

// Multiplexes the decoding of the received message based on its type
func handleAdminMessage(msg map[string]interface{}) {
    switch msg["type"].(string) {
    case "GM_STRT":
        fmt.Println("Admin: Received Start Game signal.")
        gameState.startGame()
    case "GM_STP":
        fmt.Println("Admin: Received Enter Setup signal.")
        gameState.enterSetupState()
    case "SET_OFFIC":
        plrId := uint64(msg["data"].(float64))
        plr := playerMap.get(plrId)
        if plr == nil {
            fmt.Println("Admin: Invalid playerId: " + strconv.FormatUint(plrId, 10))
            return
        }
        playerMap.setOfficer(plr)
        plr.setState(OFFICER)
    case "SET_SPEC":
        plrId := uint64(msg["data"].(float64))
        plr := playerMap.get(plrId)
        if plr == nil {
            fmt.Println("Admin: Invalid playerId: " + strconv.FormatUint(plrId, 10))
            return
        }
        playerMap.setSpectator(plr)
        // this should be received only during setup
        // when all spectators are on standby
        plr.setState(STANDBY)
    default:
        fmt.Println("Admin: Received unexpected message of type: ", msg["type"])
    }
}

// Goroutine for updating admin data
func adminUpdateTimer(stop chan struct{}) {
    ticker := time.NewTicker(ADMIN_UPDATE_INTERVAL)
    running := true
    for running {
        select {
        // trigger an update sequence
        case <-ticker.C:
            updateAdmin()
        // stop this goroutine
        case <-stop:
            running = false
        }
    }
}

// Sends and update to the admin consolel
func updateAdmin() {
    if adminWebSocket == nil {
        return
    }

    msg := AdminUpdateMessage{}

    // game state information
    if gameState.status == RUNNING {
        msg.State = "RUN"
    } else {
        msg.State = "STP"
    }

    msg.Ready = gameState.canEnterNextState

    officers, spectators := playerMap.getPlayerLists()

    msg.Officers = officers
    msg.Spectators = spectators

    sendMsgToAdmin(msg)
}

// Deals with sending the message and error checking
func sendMsgToAdmin(msg AdminUpdateMessage) {
    if adminWebSocket == nil {
        return
    }

    toSend, err := json.Marshal(msg)
    if err != nil {
        fmt.Println("Admin: Error sendMsg(): Failed to marshal JSON: ", err)
        return
    }

    _, err = adminWebSocket.Write(toSend)
    if err != nil {
        fmt.Println("Admin: Error sendMsg(): Failed to send to client: ", err)
    }
}
