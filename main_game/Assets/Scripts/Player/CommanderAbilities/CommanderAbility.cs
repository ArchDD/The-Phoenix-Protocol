﻿using UnityEngine;
using System.Collections;

public abstract class CommanderAbility : MonoBehaviour {

    internal float cooldown;
    internal float duration;
    internal bool ready = true;
    internal GameSettings settings;
    internal GameState state;

    internal abstract void ActivateAbility();
    internal abstract void DeactivateAbility();

    internal IEnumerator CoolDown()
    {
        yield return new WaitForSeconds(cooldown);
        ready = true;
    }

    internal IEnumerator DeactivateTimer()
    {
        yield return new WaitForSeconds(duration);
        DeactivateAbility();
        StartCoroutine("CoolDown");
    }

    internal void UseAbility()
    {
        if(ready)
        {
            ready = false;
            StartCoroutine("DeactivateTimer");
            ActivateAbility();
        }
     }
	
}