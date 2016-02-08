﻿using UnityEngine;
using System.Collections;
using WiimoteApi;
using System;

public class CrosshairMovement : MonoBehaviour {

    private int controlling = 0;
    private const int N_CROSSHAIRS = 4;
	float posReadDelay = 0.0001f;
	private bool[] init = new bool[N_CROSSHAIRS];
	private Vector3 initPos;
	private float movx;
	private float movy;
	bool canMove = true;
	public Vector3[] crosshairPosition = new Vector3[N_CROSSHAIRS];
	private Vector3[] oldCrosshairPosition = new Vector3[N_CROSSHAIRS];
	private Vector3[] crosshairPositionTmp = new Vector3[N_CROSSHAIRS];
	float oldAccel, newAccel;
    GameObject[] crosshairs;

	// Use this for initialization
	void Start ()
    {
        GameObject crosshairContainer = GameObject.Find("Crosshairs");

        crosshairs = new GameObject[4];

        // Find crosshairs
        for(int i = 0; i < 4; ++i)
        {
            crosshairs[i] = crosshairContainer.transform.GetChild(i).gameObject;
			init[i] = true;
        }
			
		StartCoroutine(FindRemotes());

	}
	
	// Update is called once per frame
    void Update()
    {
		Transform selectedCrosshair;

		// If there is a wii remote connected.
		if (WiimoteManager.Wiimotes.Count > 0) 
		{
			// Loop through each wii remote id
			for(int remoteId = 0; remoteId < WiimoteManager.Wiimotes.Count; remoteId++) 
			{
				selectedCrosshair = crosshairs[remoteId].transform;

				// Fixes some strange bug where the z value gets set to a rediculous value.
				oldCrosshairPosition[remoteId].z = 0.0f;
				crosshairPosition[remoteId].z = 0.0f;

				// Do some interpolation to help smoothing
				if(crosshairPositionTmp[remoteId] == oldCrosshairPosition[remoteId]) 
				{
					if(Math.Abs(selectedCrosshair.position.x) < Math.Abs(crosshairPosition[remoteId].x) &&
					   Math.Abs(selectedCrosshair.position.y) < Math.Abs(crosshairPosition[remoteId].y)) 
					{
						selectedCrosshair.position = selectedCrosshair.position + (crosshairPosition[remoteId]/50);
					}
				} 
				else 
				{
					selectedCrosshair.position = oldCrosshairPosition[remoteId];
					crosshairPositionTmp[remoteId] = oldCrosshairPosition[remoteId];
				}

			}
		} 
		else 
		{
			// Check to see if any of the crosshair keys have been pressed
			for (int i = 1; i <= N_CROSSHAIRS; i++) 
			{
				if (Input.GetKeyDown (i.ToString ())) 
				{
					controlling = i-1;
					Debug.Log ("Controlling " + controlling);
				}
			}
		}

    }

	void FixedUpdate ()
    {
        // Get the currently controlled crosshair
        Transform selectedCrosshair = crosshairs[controlling].transform;

		// Control crosshair by mouse if there is no wii remote connected.
		if (WiimoteManager.Wiimotes.Count < 1) 
		{
			// Update its position to the current mouse position
			this.crosshairPosition[0] = selectedCrosshair.position;
			this.crosshairPosition[0].x = Input.mousePosition.x;
			this.crosshairPosition[0].y = Input.mousePosition.y;
			selectedCrosshair.position = this.crosshairPosition[0];
		} 
		else 
		{
			int remoteId = 0;
			foreach(Wiimote remote in WiimoteManager.Wiimotes) 
			{
				selectedCrosshair = crosshairs[remoteId].transform;
				if (this.init[remoteId]) 
				{
					// Set the LEDs on each wii remote to indicate which player is which
					if(remoteId == 0) remote.SendPlayerLED (true, false, false, false);
					if(remoteId == 1) remote.SendPlayerLED (false, true, false, false);
					if(remoteId == 2) remote.SendPlayerLED (false, false, true, false);
					if(remoteId == 3) remote.SendPlayerLED (false, false, false, true);
					// Set up the IR camera on the wii remote
					remote.SetupIRCamera (IRDataType.BASIC);
					this.init[remoteId] = false;
				}
				try 
				{
					if (remote.ReadWiimoteData () > 0) 
					{ 
						float[] pointer = remote.Ir.GetPointingPosition ();

						// If the delay is over and the crosshair can be updated
						if(canMove) 
						{
							if(pointer[0] != -1 && pointer[1] != -1) 
							{
								oldAccel = newAccel;
								// Get data from the accelerometer
								newAccel = remote.Accel.GetCalibratedAccelData()[1] + remote.Accel.GetCalibratedAccelData()[2];

								// If there is little movement, don't bother doing this. (Should stop shaking)
								if(Math.Abs(newAccel - oldAccel) > 0.03) 
								{
									oldCrosshairPosition[remoteId] = crosshairPosition[remoteId];

									Vector3 position = selectedCrosshair.position;
									position.x = pointer[0] * Screen.width;
									position.y = pointer[1] * Screen.height;
									crosshairPosition[remoteId] = position;
									

									canMove = false;
									StartCoroutine("Delay");
								}
							}
						}
					}
				} 
				catch (Exception e) 
				{
					// Sometimes the wii remote will disconnect so for this we re connect the remote and run the initialise function again.
					WiimoteManager.FindWiimotes ();
					this.init[remoteId] = true;
				}  
				remoteId++;
			}
		}
    }

	IEnumerator FindRemotes()
	{	
		WiimoteManager.FindWiimotes ();
		yield return new WaitForSeconds(5f);
	}

	IEnumerator Delay()
	{
		yield return new WaitForSeconds(posReadDelay);
		canMove = true;
	}

}
