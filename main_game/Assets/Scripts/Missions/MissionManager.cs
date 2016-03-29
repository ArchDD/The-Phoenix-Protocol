﻿using System;
using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class MissionManager : MonoBehaviour 
{
    private GameState gameState;
    private GameSettings settings;
    private Mission[] missions;
    private PlayerController playerController;
    private OutpostManager outpostManager;
    private float startTime;
    private bool missionInit = false;

    void Start () 
    {
        gameState = GameObject.Find("GameManager").GetComponent<GameState>();
        settings = GameObject.Find("GameSettings").GetComponent<GameSettings>();
        outpostManager = GameObject.Find("OutpostManager(Clone)").GetComponent<OutpostManager>();
        LoadSettings();
    }

    public void ResetMissions() 
    {
        StopAllCoroutines();
        startTime = Time.time;
        StartCoroutine("UpdateMissions");
    }

    private void LoadSettings()
    {
        missions = settings.missionProperties;
    }

    void Update () 
    {
        if(gameState.Status == GameState.GameStatus.Started)
        {
            // Initialise any variables for missions
            if(!missionInit)
            {
                InitialiseMissions();
                StartCoroutine("UpdateMissions");
            }
        }
    }

    public void SetPlayerController(PlayerController controller)
    {
        playerController = controller;
    }


    private void InitialiseMissions()
    {
        for(int id = 0; id < missions.Length; id++)
        {
            foreach (MissionCompletion completeCondition in missions[id].completionConditions)
            {
                // If the missions completion type is an outpost, we randomly assign it a close outpost.
                if(completeCondition.completionType == CompletionType.Outpost)
                {
                    completeCondition.completionValue = outpostManager.GetClosestOutpost();
                    // If we have successfully initialised the completion value.
                    if(completeCondition.completionValue != -1)
                        missionInit = true;
                }
            }
        }
    }

    private IEnumerator UpdateMissions()
    {
        CheckMissionTriggers();
        CheckMissionsCompleted(); 
        yield return new WaitForSeconds(1f);
        StartCoroutine("UpdateMissions");
    }

    /// <summary>
    /// Checks if any of the missions have triggered. 
    /// </summary>
    private void CheckMissionTriggers() 
    {
        // Loop through mission ids
        for(int id = 0; id < missions.Length; id++)
        {
            if(CheckTrigger(id) && missions[id].hasStarted() == false)
            {
                StartMission(id);
            }
        }
    }

    private void CheckMissionsCompleted() 
    {
        for(int id = 0; id < missions.Length; id++)
        {
            if(CheckCompletion(id) && missions[id].hasStarted() == true && missions[id].isComplete() == false)
            {
                CompleteMission(id);
            }
        }
    }

    private void StartMission(int missionId)
    {
        missions[missionId].start();
        playerController.RpcStartMission(missions[missionId].name, missions[missionId].description);
    }

    private void CompleteMission(int missionId)
    {
        missions[missionId].completeMission();
        playerController.RpcCompleteMission(missions[missionId].completedDescription);
    }

    /// <summary>
    /// Checks if the mission can be started based on a trigger type and value
    /// </summary>
    /// <returns><c>true</c>, if trigger was checked, <c>false</c> otherwise.</returns>
    /// <param name="trigger">Trigger.</param>
    /// <param name="value">Value.</param>
    private bool CheckTrigger(int missionId)
    {
        // Loop through each trigger condition
        foreach (MissionTrigger trigger in missions[missionId].triggerConditions)
        {
            switch(trigger.triggerType)
            {
                case TriggerType.Health:
                    if(gameState.GetShipHealth() < trigger.triggerValue)
                    {
                        if(missions[missionId].triggerOnAny) return true;
                    }
                    else
                    {
                        if(!missions[missionId].triggerOnAny) return false;
                    }
                    break;
                case TriggerType.OutpostDistance:
                    if(outpostManager.GetClosestOutpostDistance() < trigger.triggerValue)
                    {
                        if(missions[missionId].triggerOnAny) return true;
                    }   
                    else
                    {
                        if(!missions[missionId].triggerOnAny) return false;
                    }
                    break;
                case TriggerType.Resources:
                    if(gameState.GetShipResources() > trigger.triggerValue)
                    {
                        if(missions[missionId].triggerOnAny) return true;
                    }
                    else
                    {
                        if(!missions[missionId].triggerOnAny) return false;
                    }
                    break;
                case TriggerType.Shields:
                    if(gameState.GetShipShield() < trigger.triggerValue)
                    {
                        if(missions[missionId].triggerOnAny) return true;
                    }
                    else
                    {
                        if(!missions[missionId].triggerOnAny) return false;
                    }
                    break;
                case TriggerType.Time:
                    if((Time.time - startTime) > trigger.triggerValue)
                    {
                        if(missions[missionId].triggerOnAny) return true;
                    }
                    else
                    {
                        if(!missions[missionId].triggerOnAny) return false;
                    }
                    break;
            }
        }

        if(missions[missionId].triggerOnAny)
            return false;
        else 
            return true;
    }

    private bool CheckCompletion(int missionId)
    {
        // Loop through each completion condition
        foreach (MissionCompletion completeCondition in missions[missionId].completionConditions)
        {
            switch(completeCondition.completionType)
            {
                case CompletionType.Enemies:     
                    if(gameState.GetTotalKills() >= completeCondition.completionValue)
                    {
                        if(missions[missionId].completeOnAny) return true;
                    }
                    else
                    {
                        if(!missions[missionId].completeOnAny) return false;
                    }
                    break;
                case CompletionType.Outpost:
                    if(completeCondition.completionValue != -1)
                    {
                        GameObject outpost = gameState.GetOutpostById(completeCondition.completionValue);
                        if(outpost != null && outpost.GetComponentInChildren<OutpostLogic>().resourcesCollected == true)
                        {
                            if(missions[missionId].completeOnAny) return true;
                        }
                        else
                        {
                            if(!missions[missionId].completeOnAny) return false;
                        }
                    }
                    break;
            }
        }
        if(missions[missionId].completeOnAny)
            return false;
        else 
            return true;

    }
        
    [System.Serializable] 
    public class Mission
    {
        public string name, description, completedDescription;

        // If true then any trigger condiiton will trigger this mission;
        // If false then ALL trigger conditions will have to be true to trigger the mission;
        public bool triggerOnAny;
        public MissionTrigger[] triggerConditions;

        // If true then any completion condition will complete this mission;
        // If false then ALL completion conditions will have to be true to complete the mission;
        public bool completeOnAny;
        public MissionCompletion[] completionConditions;

        // Functions can be added to be triggered 'onStart' or 'onComplete'
        public UnityEvent onStart;
        public UnityEvent onCompletion;

        private bool started = false;
        private bool complete = false;

        public bool isComplete()
        {
            return complete;
        }
        public void completeMission()
        {
            Debug.Log("Completed Mission " + name);
            if(onCompletion != null)
                onCompletion.Invoke();
            complete = true;
        }
        public bool hasStarted()
        {
            return started;
        }
        public void start()
        {
            Debug.Log("Starting Mission " + name);
            if(onStart != null)
                onStart.Invoke();
            started = true;
        }
    }

    [System.Serializable]
    public class MissionTrigger
    {
        public TriggerType triggerType;
        public int triggerValue;
    }

    [System.Serializable]
    public class MissionCompletion
    {
        public CompletionType completionType;
        public int completionValue;
    }
}

public enum TriggerType
{
    Time,                   // Trigger after a certain time
    Health,                 // Trigger if the ships health is below a specific value
    Shields,                // Trigger if the ships shields are below a specific value
    Resources,              // Trigger if the player has collected a certain amount of resources.
    OutpostDistance         // Trigger if the player is a certain distence from any outpost
}

public enum CompletionType
{
    Enemies,                // Complete mission if x enemies are destroyed
    Outpost                 // Complete mission if outpost is visited
}