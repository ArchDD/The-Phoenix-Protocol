package main

import(
    "fmt"
)

// Holds asteroid data
type Asteroid struct {
    posX int
    posY int
}

// The collection of all asteroids
type AsteroidMap struct {
    m    map[int]*Asteroid
    delC chan int
    addC chan NewAst
    copy chan map[int]Asteroid
}

// Wrapper of asteroid data, sent on a channel
type NewAst struct {
    id  int
    ast *Asteroid
}

// Manages concurrent access to the asteroid map data structure
func (asteroids *AsteroidMap) accessManager() {
    fmt.Println("Starting Asteroid accessManager.")
    for {
        select {
        case id := <-asteroids.delC:
            delete(asteroids.m, id)
        case new := <-asteroids.addC:
            asteroids.m[new.id] = new.ast
        case newCopy := <-asteroids.copy:
            for k, v := range asteroids.m {
                newCopy[k] = *v
            }
            asteroids.copy <- nil
        }
    }
}

// Request an asteroid addition
func (asteroids *AsteroidMap) add(id int, data *Asteroid) {
    asteroids.addC <- NewAst{id, data}
}

// Request an asteroid deletion
func (asteroids *AsteroidMap) remove(id int) {
    asteroids.delC <- id
}
