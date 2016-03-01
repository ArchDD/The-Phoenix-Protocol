package main

import (
    "fmt"
    "golang.org/x/net/websocket"
    "net"
    "net/http"
    "time"
)

const ADMIN_WEB_DIR string = "../web/admin_web"
const ADMIN_PORT string = "52932"
const USER_WEB_DIR string = "../web/phone_web"
const USER_PORT string = "8080"
const LOCAL_UDP_PORT string = "45678"
const GAME_SERVER_ADDRESS string = "192.168.56.1"
const GAME_SERVER_UDP_PORT string = "2345"
const GAME_SERVER_TCP_PORT string = "2346"
const DATA_UPDATE_INTERVAL time.Duration = 33 * time.Millisecond
const NUM_OFFICERS int = 0
const OFFER_VALIDITY_DURATION time.Duration = 20 * time.Second

// Structures dealing with the Game Server Connections
var gameServerUDPConn *net.UDPConn
var gameServerTCPConn *net.TCPConn

// Holds and handles game state related information
var gameState *GameState = &GameState{
    status:      INIT,
    updateStopC: nil,
}

// Holds the player ship data and modification channels
var playerShip *PlayerShipController = &PlayerShipController{
    data:   &PlayerShip{},
    setC:   make(chan *PlayerShip),
    getC:   make(chan *PlayerShip),
    resetC: make(chan struct{}),
}

// Holds the player data and modification channels
var playerMap *PlayerMap = &PlayerMap{
    mOfficers:   make(map[string]*Player),
    mSpec:       make(map[string]*Player),
    addC:        make(chan *Player),
    setOfficerC: make(chan *Player),
    plrC:        make(chan struct{}),
    resetC:      make(chan struct{}),
    startC:      make(chan struct{}),
    sortlC:      make(chan []*Player),
    updateC:     make(chan struct{}),
}

// Main structure holding all enemy data
var enemyMap *EnemyMap = &EnemyMap{
    m:      make(map[int]*Enemy),
    delC:   make(chan int),
    setC:   make(chan NewEnemy),
    resetC: make(chan struct{}),
    copyC:  make(chan map[int]*Enemy),
}

// Main structure holding all asteroid data
var asteroidMap *AsteroidMap = &AsteroidMap{
    m:      make(map[int]*Asteroid),
    delC:   make(chan int),
    addC:   make(chan NewAst),
    resetC: make(chan struct{}),
    copyC:  make(chan map[int]*Asteroid),
}

// Starts all necessary subroutines
func main() {
    go stdinHandler()
    go gameServerTCPConnectionHandler()
    go gameServerUDPConnectionHandler()
    go playerMap.accessManager()
    go playerShip.accessManager()
    go asteroidMap.accessManager()
    go enemyMap.accessManager()

    // Server for the users
    usersServerMux := http.NewServeMux()
    usersServerMux.Handle("/web_socket", websocket.Handler(userWebSocketHandler))
    usersServerMux.Handle("/", http.FileServer(http.Dir(USER_WEB_DIR)))

    // Server for the admin
    adminServerMux := http.NewServeMux()
    adminServerMux.Handle("/web_socket", websocket.Handler(adminWebSocketHandler))
    adminServerMux.Handle("/", http.FileServer(http.Dir(ADMIN_WEB_DIR)))

    // TODO: remove when admin console is implemented
    go gameState.enterSetupState()

    go listenWrapper(usersServerMux, USER_PORT)
    listenWrapper(adminServerMux, ADMIN_PORT)
}

// A wrapper used to check for errors even when spawned as a goroutine
func listenWrapper(srv *http.ServeMux, port string) {
    fmt.Println("Starting Web Server on port " + port + ".")
    err := http.ListenAndServe(":"+port, srv)
    if err != nil {
        panic("Error starting web server on port " + port + " : " + err.Error())
    }
}
