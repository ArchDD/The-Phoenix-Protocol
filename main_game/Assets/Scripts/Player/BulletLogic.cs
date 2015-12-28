﻿/*
    2015-2016 Team Pyrolite
    Project "Sky Base"
    Authors: Marc Steene
    Description: Handles bullet properties and destruction
*/

using UnityEngine;
using System.Collections;

public class BulletLogic : MonoBehaviour 
{

	public float speed = 100f;
	[SerializeField] float accuracy; // 0 = perfectly accurate, 1 = very inaccurate
	[SerializeField] float damage; 
	[SerializeField] Color bulletColor;
    [SerializeField] GameObject impact;
	[SerializeField] float xScale;
	[SerializeField] float yScale;
	[SerializeField] float zScale;
	GameObject obj;
	GameObject destination;
	PlayerShooting player;
	bool started = false;
	int playerId;

	public void SetDestination(Vector3 destination)
	{
		obj = transform.parent.gameObject;
		Rigidbody rigidbody = obj.AddComponent<Rigidbody>();
		rigidbody.useGravity = false;
		rigidbody.isKinematic = false;
        rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
		SphereCollider sphere = obj.AddComponent<SphereCollider>();
		sphere.isTrigger = true;
		obj.AddComponent<BulletCollision>();
		obj.transform.localScale = new Vector3(xScale, yScale, zScale);
		obj.transform.LookAt (destination);
		obj.transform.Rotate (Random.Range (-accuracy, accuracy), Random.Range (-accuracy, accuracy), Random.Range (-accuracy, accuracy));
    	obj.GetComponent<BulletMove>().SetColor(bulletColor);
		StartCoroutine ("DestroyObject");
		started = true;
	}

	public void SetID(PlayerShooting playerObj, int id)
	{
		playerId = id;
		player = playerObj;
	}
	
	public void collision(Collider col)
	{

        if(playerId == 1) player.HitMarker();

        if(col.gameObject.tag.Equals("Debris"))
        {
        //Debug.Log ("A bullet has hit asteroid");
        col.gameObject.GetComponentInChildren<AsteroidLogic>().collision(damage);
        }
        else if(col.gameObject.tag.Equals("EnemyShip"))
        {
        //Debug.Log ("A bullet has hit an enemy");
        col.gameObject.GetComponentInChildren<EnemyLogic>().collision(damage);
        }
        else if(col.gameObject.tag.Equals("Player"))
        {
        //Debug.Log ("A bullet has hit the player");
        col.gameObject.transform.parent.transform.parent.transform.parent.GetComponentInChildren<ShipMovement>().collision(damage, transform.eulerAngles.y);
        }
        GameObject impactTemp = Instantiate (impact, col.transform.position, Quaternion.identity) as GameObject;
        impactTemp.GetComponent<Renderer>().material.SetColor("_TintColor", bulletColor);
        impactTemp.GetComponent<Light>().color = bulletColor;

        Renderer[] rend = impactTemp.GetComponentsInChildren<Renderer>();
        for(int i = 0; i < rend.Length; i++)
        {
            rend[i].material.SetColor("_TintColor", bulletColor);
        }
        ServerManager.NetworkSpawn(impactTemp);
        Destroy (obj);
        }

        IEnumerator DestroyObject()
        {
            yield return new WaitForSeconds(4f);
            Destroy (obj);
        }
}
