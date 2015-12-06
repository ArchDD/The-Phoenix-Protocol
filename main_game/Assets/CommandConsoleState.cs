using UnityEngine;
using UnityEngine.UI;



public class CommandConsoleState : MonoBehaviour {
    public Canvas canvas;
    public int TotalPower;
    public int Health;
    public Text PowerText;
    public Text MassText;
    public Text EnginLabel;
    public Text ShieldsLabel;
    public Text ShieldsUpgradeLabel;
    public Text GunsUpgradeLabel;
    public Text EngineUpgradeLabel;
    public GameObject ShieldsButton;
    public GameObject GunsButton;
    public GameObject EngineButton;
    private PlayerController playerControlScript;
    private int remPower;
    private int mass;
    private bool upgrade = false;
    private bool engineOn = true;
    private bool shieldsOn = true;
    private bool gunsOn = true;
    private int shieldsLevel = 1;
    private int engineLevel = 1;
    private int gunsLevel = 1;
    private double second = 0; 

    // Use this for initialization
    void Start () {
        mass = 0;
        remPower = 0;
        UpdatePower();
        UpdateMass();
        ShieldsUpgradeLabel.text = shieldsLevel * 100 + "M";
        GunsUpgradeLabel.text = gunsLevel * 100 + "M";
        EngineUpgradeLabel.text = engineLevel * 100 + "M";
        //ShieldsLabel.text = "Component got";
        ShieldsButton.SetActive(false);
        GunsButton.SetActive(false);
        EngineButton.SetActive(false);
        mass = 100;
    }

    public void Upgrade(bool isOn)
    {
        upgrade = !upgrade;
        ShieldsButton.SetActive(upgrade);
        GunsButton.SetActive(upgrade);
        EngineButton.SetActive(upgrade);
    }

    public void gimme(PlayerController playerControl)
    {
        playerControlScript = playerControl;
        playerControlScript.test();
    }

    public void test()
    {
        print("blehhrsh");
    }

    public void Engin(bool isOn)
    {
        if (upgrade)
        {
            EnginLabel.text = EnginLabel.text + "I";
            upgrade = false;
        }
        //enginOn = isOn;
        UpdatePower();
    }

    //Called whenever power is toggled on a system  0 = Shields, 1 = Guns, 2 = Engines
    public void PowerToggle(int system)
    {
        switch(system){
            case 0:
                shieldsOn = !shieldsOn;
                break;
            case 1:
                gunsOn = !gunsOn;
                break;
            case 2:
                engineOn = !engineOn;
                break;
        }
        UpdatePower();
    }

    public void Shields(bool isOn)
    {
        shieldsOn = isOn;
        UpdatePower();
    }
    

    //Called whenever an upgrade is purchased (by clicking yellow button) 0 = Shields, 1 = Guns, 2 = Engines
    public void ThingUpgrade(int where)
    {
        switch (where)
        {
            case 0:
                if(mass >= 100 * shieldsLevel)
                {
                    mass -= 100 * shieldsLevel;
                    shieldsLevel++;
                    ShieldsUpgradeLabel.text = shieldsLevel * 100 + "M";
                    playerControlScript.CmdUpgrade(0);
                }
                break;
            case 1:
                if(mass >= 100 * gunsLevel)
                {
                    mass -= 100 * gunsLevel;
                    gunsLevel++;
                    GunsUpgradeLabel.text = gunsLevel * 100 + "M";
                }
                break;
            case 2:
                if (mass >= 100 * engineLevel)
                {
                    mass -= 100 * engineLevel;
                    engineLevel++;
                    playerControlScript.CmdUpgrade(2);
                    EngineUpgradeLabel.text = engineLevel * 100 + "M";
                }
                break;

        }
        upgrade = false;
        UpdateMass();
    }

   /* private GameObject Level(int where)
    {
        switch (where)
        {
            case 0:
                return shieldsLevel;
                break;
            case 1:
                return gunsLevel;
                break;
            case 2:
                return engineLevel;
                break;
        }
    }*/


    void UpdatePower()
    {
        remPower = TotalPower;
        if (engineOn) remPower -= 3;
        if (shieldsOn) remPower -= 2;
        if (gunsOn) remPower -= 5;
        PowerText.text = "Power: " + remPower;
        //player = GetComponentInParent<PlayerControl>();
        //if (player != null) print("player != null");
        //else print("player is null");
    }

    void UpdateMass()
    {
        MassText.text = "Mass:  " + mass;
    }


    void FixedUpdate ()
    { 
        second += Time.deltaTime;
        if(second >= 1)
        {
            mass += 5;
            second = 0;
            UpdateMass();
        }
    }

    void onGui()
    {
       
    }

        // Update is called once per frame
    void Update () {
        
	}
}
