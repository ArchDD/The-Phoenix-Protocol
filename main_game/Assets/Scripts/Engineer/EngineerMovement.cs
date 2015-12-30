﻿using System;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class EngineerMovement : MonoBehaviour
{
    [SerializeField] private bool m_IsWalking;
    [SerializeField] private float m_WalkSpeed;
    [SerializeField] private float m_RunSpeed;
    [SerializeField] [Range(0f, 1f)] private float m_RunstepLenghten;
    [SerializeField] private float m_JumpSpeed;
    [SerializeField] private float m_StickToGroundForce;
    [SerializeField] private float m_GravityMultiplier;
    [SerializeField] private MouseLook m_MouseLook;
    [SerializeField] private bool m_UseFovKick;
    [SerializeField] private bool m_UseHeadBob;
    [SerializeField] private float m_StepInterval;
    [SerializeField] private Text upgradeText;

    private Camera m_Camera;
    private bool m_Jump;
    private float m_YRotation;
    private Vector2 m_Input;
    private Vector3 m_MoveDir = Vector3.zero;
    private CollisionFlags m_CollisionFlags;
    private bool m_PreviouslyGrounded;
    private float m_StepCycle;
    private float m_NextStep;
    private EngineerController controller;

    // Use this for initialization
    private void Start()
    {
        m_Camera = gameObject.GetComponent<Camera>();
        m_StepCycle = 0f;
        m_NextStep = m_StepCycle / 2f;
        m_MouseLook.Init(transform, m_Camera.transform);
        controller = gameObject.transform.parent.GetComponent<EngineerController>();
    }

    // Update is called once per frame
    private void Update()
    {
        RotateView();
        m_Jump = Input.GetButton("Jump");

        // Do forward raycast from camera to the center of the screen to see if an upgradeable object is in front of the player
        int x = Screen.width / 2;
        int y = Screen.height / 2;
        Ray ray = m_Camera.ScreenPointToRay(new Vector3(x, y, 0));
        RaycastHit hitInfo;

        if (Physics.Raycast(ray, out hitInfo, 5.0f))
        {
            if (hitInfo.collider.CompareTag("Upgrade"))
            {
                upgradeText.text = "Press and hold E to upgrade";
            }
            else
            {
                ResetUpgradeText();
            }
        }
        else
        {
            ResetUpgradeText();
        }
    }

    private void FixedUpdate()
    {
        float speed;
        GetInput(out speed);

        // Move the player if they have moved
        if (m_Input.x != 0 && m_Input.y != 0)
        {
            //controller.CmdMove(m_Input, m_Jump, !m_IsWalking);
        }
       

        //m_CollisionFlags = m_CharacterController.Move(m_MoveDir * Time.fixedDeltaTime);
        ProgressStepCycle(speed);
    }

    private void ProgressStepCycle(float speed)
    {
        //if (m_CharacterController.velocity.sqrMagnitude > 0 && (m_Input.x != 0 || m_Input.y != 0))
        //{
        //    m_StepCycle += (m_CharacterController.velocity.magnitude + (speed * (m_IsWalking ? 1f : m_RunstepLenghten))) *
        //                 Time.fixedDeltaTime;
        //}

        if (!(m_StepCycle > m_NextStep))
        {
            return;
        }

        m_NextStep = m_StepCycle + m_StepInterval;
    }

    private void GetInput(out float speed)
    {
        // Read input
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        bool waswalking = m_IsWalking;

#if !MOBILE_INPUT
        // On standalone builds, walk/run speed is modified by a key press.
        // keep track of whether or not the character is walking or running
        m_IsWalking = !Input.GetKey(KeyCode.LeftShift);
#endif
        // set the desired speed to be walking or running
        speed = m_IsWalking ? m_WalkSpeed : m_RunSpeed;
        m_Input = new Vector2(horizontal, vertical);

        // normalize input if it exceeds 1 in combined length:
        if (m_Input.sqrMagnitude > 1)
        {
            m_Input.Normalize();
        }

        // handle speed change to give an fov kick
        // only if the player is going to a run, is running and the fovkick is to be used
        //if (m_IsWalking != waswalking && m_UseFovKick && m_CharacterController.velocity.sqrMagnitude > 0)
        //{
        //    StopAllCoroutines();
        //    StartCoroutine(!m_IsWalking ? m_FovKick.FOVKickUp() : m_FovKick.FOVKickDown());
        //}
    }


    private void RotateView()
    {
        m_MouseLook.LookRotation(transform, m_Camera.transform);
        // Send the rotaion to the server
        //controller.CmdSetRotation(transform.rotation);
    }


    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Rigidbody body = hit.collider.attachedRigidbody;
        //dont move the rigidbody if the character is on top of it
        if (m_CollisionFlags == CollisionFlags.Below)
        {
            return;
        }

        if (body == null || body.isKinematic)
        {
            return;
        }
        //body.AddForceAtPosition(m_CharacterController.velocity * 0.1f, hit.point, ForceMode.Impulse);
    }

    private void ResetUpgradeText()
    {
       // upgradeText.text = "";
    }
}