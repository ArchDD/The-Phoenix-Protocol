using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

public class CommandConsoleState : MonoBehaviour {

	#pragma warning disable 0649 // Disable warnings about unset private SerializeFields
	[SerializeField] private Canvas canvas;
	[SerializeField] private Text civiliansText;
	[SerializeField] private Text healthText;
	[SerializeField] private Text resourcesText;
    [SerializeField] private Text shieldsText;

    [SerializeField] private Text upgradeButtonLabel;
    [SerializeField] private Text costLabel;
    [SerializeField] private Text upgradeDescription;

    [SerializeField] Material defaultMat;
    [SerializeField] Material highlightMat;

	[SerializeField] private GameObject newsFeed;
    [SerializeField] private GameObject popupWindow;

    [SerializeField] private Color defaultColor;
    [SerializeField] private Color highlightColor;
	#pragma warning restore 0649

    private PlayerController playerController;
	private GameObject ship;
    private GameObject upgradeArea;
    private StratMap stratMap;
	private GameState gameState;
    private GameSettings settings;
    private List<ConsoleUpgrade> consoleUpgrades = new List<ConsoleUpgrade>();

    private int shieldsLevel = 1;
    private int engineLevel = 1;
    private int turretsLevel = 1;
    private int hullLevel = 1;
    private int droneLevel = 1;
    private int storageLevel = 1;
    private int componentToUpgrade = 0;

    private double second = 0; 

    private string[] upgradeNames = new string[6] {"SHIELDS", "TURRETS", "ENGINES", "HULL", "DRONE", "STORAGE"};
    private int[] upgradeCosts = new int[6];
    private string[] upgradeDescriptions = new string[6];
    private int[] upgradeMaxLevels = new int[6];

    // Indicates which upgrade is in progress.
    private int[] upgradeProgress = new int[6] {0,0,0,0,0,0};

    private ConsoleShipControl shipControl;
   
    void Start () {
        gameState = GameObject.Find("GameManager").GetComponent<GameState>();
        settings = GameObject.Find("GameSettings").GetComponent<GameSettings>();
        upgradeArea = GameObject.Find("UpgradeInfo");
        stratMap = GameObject.Find("Map").GetComponent<StratMap>();
        LoadSettings();
       
        Camera.main.GetComponent<ToggleGraphics>().UpdateGraphics();
        Camera.main.GetComponent<ToggleGraphics>().SetCommandGraphics();

        LoadShipModel();

		UpdateAllText();
      
        // Remove crosshair from this scene. 
        GameObject.Find("CrosshairCanvas(Clone)").SetActive(false);
        newsFeed.SetActive(false);
        ClosePopupWindow();

        upgradeArea.SetActive(false);

        AddUpgradeBoxes();
    }

    private void LoadSettings() 
    {
        upgradeCosts[0]  = settings.ShieldsInitialCost;
        upgradeCosts[1]  = settings.TurretsInitialCost;
        upgradeCosts[2]  = settings.EnginesInitialCost;
        upgradeCosts[3]  = settings.HullInitialCost;
        upgradeCosts[4]  = settings.DroneInitialCost;
        upgradeCosts[5]  = settings.StorageInitialCost;

        upgradeDescriptions[0] = settings.ShieldsDescription;
        upgradeDescriptions[1] = settings.TurretsDescription;
        upgradeDescriptions[2] = settings.EnginesDescription;
        upgradeDescriptions[3] = settings.HullDescription;
        upgradeDescriptions[4] = settings.DroneDescription;
        upgradeDescriptions[5] = settings.StorageDescription;

        upgradeMaxLevels[0] = settings.ShieldsUpgradeLevels;
        upgradeMaxLevels[1] = settings.TurretsUpgradeLevels;
        upgradeMaxLevels[2] = settings.EnginesUpgradeLevels;
        upgradeMaxLevels[3] = settings.HullUpgradeLevels;
        upgradeMaxLevels[4] = settings.DroneUpgradeLevels;
        upgradeMaxLevels[5] = settings.StorageUpgradeLevels;

    }

    void FixedUpdate ()
    { 
        second += Time.deltaTime;
        if(second >= 1)
        {
            UpdateAllText();
            UpdateCostTextColor();
            second = 0;
        }
    }

    private void LoadShipModel()
    {
        // Load the ship model into the scene. 
        ship = Instantiate(Resources.Load("Prefabs/CommandShip", typeof(GameObject))) as GameObject;
        ship.transform.position = new Vector3(18f, -2.5f, -1961f);
        ship.transform.eulerAngles = new Vector3(0, 250f, 0);
        ship.AddComponent<ConsoleShipControl>();
        shipControl = ship.GetComponent<ConsoleShipControl>();
        shipControl.SetMaterials(defaultMat, highlightMat);
    }
    private void AddUpgradeBoxes()
    {
        Transform canvas = gameObject.transform.Find("Canvas");
        for(int i = 0; i < 6; i++) 
        {
            int component = i;
            GameObject upgradeBox = Instantiate(Resources.Load("Prefabs/UpgradeBox", typeof(GameObject))) as GameObject;
            upgradeBox.transform.SetParent(canvas);
            upgradeBox.transform.localScale = new Vector3(1,1,1);
            upgradeBox.transform.localPosition = new Vector3(-483, 200 - (i*80), 0);
            upgradeBox.GetComponent<ConsoleUpgrade>().SetUpgradeInfo(upgradeNames[i], upgradeCosts[i], upgradeMaxLevels[i]);
            upgradeBox.GetComponent<Button>().onClick.AddListener(delegate{OnClickUpgrade(component);});
            consoleUpgrades.Add(upgradeBox.GetComponent<ConsoleUpgrade>());
        }
    }
        
    public void givePlayerControllerReference(PlayerController controller)
    {
        playerController = controller;
    }
        
    private int GetIdFromComponentType(ComponentType type)
    {
        switch(type)
        {
            case ComponentType.ShieldGenerator:
                return 0;
            case ComponentType.Turret:
                return 1;
            case ComponentType.Engine:
                return 2;
            case ComponentType.Bridge:
                return 3;
            case ComponentType.Drone:
                return 4;
            case ComponentType.ResourceStorage:
                return 5;
        }
        return 0;
    }

    /// <summary>
    /// Checks the upgrade cost of a component
    /// </summary>
    /// <returns><c>true</c>, if upgrade cost was checked, <c>false</c> otherwise.</returns>
    /// <param name="baseCost">Base cost of component to be upgraded</param>
    /// <param name="level">Level of the component</param>
    private bool CheckUpgradeCost(int baseCost, int level) 
    {
        return (gameState.GetShipResources() >= GetUpgradeCost(baseCost, level));
    }

    /// <summary>
    /// Calculates the upgrade cost of a component
    /// </summary>
    /// <returns>The upgrade cost.</returns>
    /// <param name="baseCost">Base cost.</param>
    /// <param name="level">Level.</param>
    private int GetUpgradeCost(int baseCost, int level)
    {
        // Current simplistic cost model. Should be updated for proper usage in game.
        return baseCost * level;
    }

    /// <summary>
    /// Sends the upgrade request for a component of the ship
    /// </summary>
    /// <param name="type">Type of component</param>
    /// <param name="baseCost">Base cost of the component</param>
    /// <param name="level">Level of the component</param>
    private bool UpgradeComponent(ComponentType type, int baseCost, int level)
    {
        if(CheckUpgradeCost(baseCost, level))
        {
            // Update the ships resources
            gameState.UseShipResources(GetUpgradeCost(baseCost, level));

            // Show level indictor for new level.
            consoleUpgrades[GetIdFromComponentType(type)].UpdateLevelIndicator(level);

            // Send request to engineer to upgrade
            playerController.CmdAddUpgrade(type);

            // Update resources text with new value.
            UpdateAllText();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Confirms the upgrade, is called when the engineer has completed the upgrade.
    /// </summary>
    /// <param name="type">Type.</param>
    public void ConfirmUpgrade(ComponentType type)
    {
        upgradeProgress[GetIdFromComponentType(type)] = 0;
        upgradeButtonLabel.text = "Upgrade";
        switch(type)
        {
            case ComponentType.ShieldGenerator:
                shieldsLevel++;
                break;
            case ComponentType.Turret:
                turretsLevel++;
                break;
            case ComponentType.Engine:
                engineLevel++;
                break;
            case ComponentType.Bridge:
                hullLevel++;
                break;
            case ComponentType.Drone:
                droneLevel++;
                break;
            case ComponentType.ResourceStorage:
                storageLevel++;
            break;
        }

    }
        
    public void HighlightComponent(int component)
    {
        shipControl.HighlightComponent(component);
    }

    public void OnClickUpgrade(int component)
    {
        upgradeArea.SetActive(true);
        componentToUpgrade = component;
        HighlightComponent(component);
        upgradeDescription.text = upgradeDescriptions[component];
        switch (component)
        {
            // Shields Upgrade
            case 0: 
                costLabel.text = GetUpgradeCost(upgradeCosts[component], shieldsLevel).ToString();
                break;
            // Turrets Upgrade
            case 1:
                costLabel.text = GetUpgradeCost(upgradeCosts[component], turretsLevel).ToString();
                break;
            // Engine Upgrade
            case 2:
                costLabel.text = GetUpgradeCost(upgradeCosts[component], engineLevel).ToString();
                break;
            // Hull Upgrade
            case 3:
                costLabel.text = GetUpgradeCost(upgradeCosts[component], hullLevel).ToString();
                break;
            // Drone Upgrade
            case 4:
                costLabel.text = GetUpgradeCost(upgradeCosts[component], droneLevel).ToString();
                break;
            // Resource Storage Upgrade
            case 5:
                costLabel.text = GetUpgradeCost(upgradeCosts[component], storageLevel).ToString();
                break;
        }
        UpdateCostTextColor();
        if(upgradeProgress[component] == 1) 
            upgradeButtonLabel.text = "Waiting";
        else
            upgradeButtonLabel.text = "Upgrade";
    }

    public void RepairShip(int component)
    {

    }
    //Called whenever an upgrade is purchased (by clicking yellow button)
    public void UpgradeShip()
    {
        int tmpLevel = 0;
        // If we are already waiting then we don't want to upgrade again.
        if(upgradeProgress[componentToUpgrade] == 1)
            return;
        
        switch (componentToUpgrade)
        {
            // Shields Upgrade
            case 0: 
                if(!UpgradeComponent(ComponentType.ShieldGenerator, upgradeCosts[componentToUpgrade], shieldsLevel))
                    return;
                tmpLevel = shieldsLevel;
                break;
            // Turrets Upgrade
            case 1:
                if(!UpgradeComponent(ComponentType.Turret, upgradeCosts[componentToUpgrade], turretsLevel))
                    return;
                tmpLevel = turretsLevel;                
                break;
            // Engine Upgrade
            case 2:
                if(!UpgradeComponent(ComponentType.Engine, upgradeCosts[componentToUpgrade], engineLevel))
                    return;
                tmpLevel = engineLevel; 
                break;
            // Hull Upgrade
            case 3:
                if(!UpgradeComponent(ComponentType.Bridge, upgradeCosts[componentToUpgrade], hullLevel))
                    return;
                tmpLevel = hullLevel; 
                break;
            // Drone Upgrade
            case 4:
                if(!UpgradeComponent(ComponentType.Drone, upgradeCosts[componentToUpgrade], droneLevel))
                    return;
                tmpLevel = droneLevel; 
                break;
            // Resource Storage Upgrade
            case 5:
                if(!UpgradeComponent(ComponentType.ResourceStorage, upgradeCosts[componentToUpgrade], storageLevel))
                    return;
                tmpLevel = storageLevel; 
                break;
        }
        consoleUpgrades[componentToUpgrade].UpdateCost(GetUpgradeCost(upgradeCosts[componentToUpgrade], tmpLevel + 1));

        upgradeProgress[componentToUpgrade] = 1;
        upgradeButtonLabel.text = "Waiting";
    }
        
	/// <summary>
	/// Update all text values on screen.
	/// </summary>
	private void UpdateAllText()
	{
		// Get resources and health from the gamestate.
        int shipCivilians   = gameState.GetCivilians();
		int shipResources   = gameState.GetShipResources();
		float shipHealth    = gameState.GetShipHealth();
		float shipShields   = gameState.GetShipShield();

		// Update the text
        civiliansText.text = shipCivilians.ToString();;
        resourcesText.text = shipResources.ToString();;
        healthText.text    = shipHealth.ToString();;
        shieldsText.text   = shipShields.ToString();;
	}

    private void UpdateCostTextColor()
    {
        if(int.Parse(costLabel.text) > gameState.GetShipResources())
        {
            costLabel.color = new Color(95f/255f, 24f/255f, 24f/255f, 1);
        }
        else
        {
            costLabel.color = new Color(176f/255f, 176f/255f, 176f/255f, 1);
        }
          
    }
    
    public void FoundOutpost(GameObject outpost)
    {
        newsFeed.SetActive(true);
        //newsFeed.GetComponent<Text>().text = message;
        stratMap.NewOutpost(outpost);
    }


    public void ShowMissionPopup(int missionId)
    {
        popupWindow.SetActive(true);
    }

    public void ClosePopupWindow()
    {
        popupWindow.SetActive(false);
    }
}
