package main

type PlayerType int

const (
    SPECTATOR PlayerType = iota
    OFFICER
    COMMANDER
)

// Holds player related data
type Player struct {
    userName string
    role     PlayerType
    score    int
    user     *User
}

// Sets the current use associated with this player
func (plr *Player) setUser(usr *User) {
    playerMap.plrC <- struct{}{}
    plr.user = usr
    playerMap.plrC <- struct{}{}
}

// Deassociate the current user if it equal the provided parameter
func (plr *Player) unsetUserIfEquals(usr *User) {
    playerMap.plrC <- struct{}{}
    if plr.user == usr {
        plr.user = nil
    }
    playerMap.plrC <- struct{}{}
}

// Sends a user state update
func (plr *Player) sendStateUpdate() {
    msg := map[string]interface{}{
        "type": "USER_UPDATE",
        "data": map[string]interface{}{
            "state": plr.getStateString(),
        },
    }

    plr.user.sendMsg(msg)
}

// Sends a user state data update
func (plr *Player) sendDataUpdate(enemies map[int]*Enemy, asteroids map[int]*Asteroid) {
    // players with no active user don't need updating
    if plr.user == nil {
        return
    }

    // TODO: add other objects
    msg := make(map[string]interface{})
    msg["type"] = "STATE_UPDATE"
    dataSegment := make([]map[string]interface{}, 0)

    // Add enemies to the message
    for _, enemy := range enemies {
        dataSegment = append(dataSegment, map[string]interface{}{
            "type": "ship",
            "position": map[string]interface{}{
                "x": enemy.posX,
                "y": enemy.posY,
            },
        })
    }

    // Add asteroids to the message
    for _, ast := range asteroids {
        dataSegment = append(dataSegment, map[string]interface{}{
            "type": "asteroid",
            "position": map[string]interface{}{
                "x": ast.posX,
                "y": ast.posY,
            },
        })
    }

    msg["data"] = dataSegment
    // incry += 0.1
    // msg := map[string]interface{}{
    //     "type": "STATE_UPDATE",
    //     "data": []map[string]interface{}{
    //         map[string]interface{}{
    //             "type": "ship",
    //             "position": map[string]interface{}{
    //                 "x": 10,
    //                 "y": math.Mod((incry + 14), 100),
    //             },
    //         },
    //         map[string]interface{}{
    //             "type": "debris",
    //             "position": map[string]interface{}{
    //                 "x": 20,
    //                 "y": math.Mod((incry + 53), 100),
    //             },
    //             "size": 10,
    //         },
    //         map[string]interface{}{
    //             "type": "asteroid",
    //             "position": map[string]interface{}{
    //                 "x": 32,
    //                 "y": math.Mod((incry + 15), 100),
    //             },
    //         },
    //     },
    // }

    plr.user.sendMsg(msg)
}

// Get a string representing the state, used as instruction for the phone
// web page transitions
func (plr *Player) getStateString() (out string) {
    switch plr.role {
    case SPECTATOR:
        out = "SPECTATOR"
    case OFFICER:
        out = "OFFICER"
    case COMMANDER:
        out = "COMMANDER"
    }

    return
}
