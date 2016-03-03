var serverSocket;

// "enum" for the role types
roles = {
    OFFICER: 0,
    SPECTATOR: 1
}

// Initialise the web socket connection
function initSocket() {
    if(typeof serverSocket === 'undefined') {
        serverSocket = new WebSocket("ws://" + window.location.host + "/web_socket");

        // After connection initialisation
        serverSocket.onopen = function() {
            enableAdminPanel()
            console.log("Web Socket Connection initialised");
        }

        // Receiving from server
        serverSocket.onmessage = function(e) { onMessage(e) }

        // Web Socket Error
        serverSocket.onerror = function(e) {
            conosle.log("Web Socket Error: " + e.data);
        }

        // After closing the connection
        serverSocket.onclose = function() {
            serverSocket = undefined;
            disableAdminPanel()
            console.log("Web Socket Connection Closed");
        }
    }
}

// Act based on type of command
function onMessage(event) {
    var msg = JSON.parse(event.data)

    displayState(msg.State)
    displayStatus(msg.Ready)
    displayOfficers(msg.Officers)
    displaySpectators(msg.Spectators)

    if (msg.State == "STP") {
        if (msg.Ready) {
            enableStartGame()
        } else {
            disableStartGame()
        }
    } else {
        // disable all buttons
        $('.btn').each(function() {
            $(this).attr("disabled", true)
        });
    }

}

function sendStartGameSignal() {
        // block button until next update is received
        $('#startGameButton').attr("disabled", true)
        var msg = {
            type: "GM_STRT"
        }

        serverSocket.send(JSON.stringify(msg));
}

function sendSetPlayerSignal(userId, role) {
    typeStr = ""
    switch (role) {
        case roles.OFFICER:
            typeStr = "SET_OFFIC"
            break;
        case roles.SPECTATOR:
            typeStr = "SET_SPEC"
            break;
        default:
            console.log("Unknown role to request to be set.")
            return
    }

    var msg = {
        type: typeStr,
        data: userId
    }

    serverSocket.send(JSON.stringify(msg));
}