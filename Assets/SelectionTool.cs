using DefaultNamespace;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Valve.VR;

public class SelectionTool : MonoBehaviour
{
    public static SelectionTool instance;

    public SteamVR_Action_Boolean touchPadLeft;
    public SteamVR_Action_Boolean touchPadRight;
    public SteamVR_Action_Boolean grabPinch;

    public SteamVR_Input_Sources inputSource = SteamVR_Input_Sources.RightHand; //which controller
    public int currentGroup = 1;
    public bool selectionActive;

    public Color[] colors;

    private Material material;
    private MeshRenderer renderer;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        ChangeGroup(1);
        // renderer = GetComponent<MeshRenderer>();
        // renderer.enabled = false;

        // touchPadLeft.AddOnStateDownListener(OnTouchPadLeftPress, inputSource);
        // touchPadRight.AddOnStateDownListener(OnTouchPadRightPress, inputSource);
        // if (grabPinch != null)
        // {
        //     grabPinch.AddOnStateDownListener(OnRightTriggerDown, inputSource);
        //     grabPinch.AddOnStateUpListener(OnRightTriggerUp, inputSource);
        // }
    }

    private void ChangeGroup(int group)
    {
        currentGroup = group % colors.Length;
        GetComponent<Renderer>().material.color = colors[currentGroup];
    }

    public Color GetCurrentColor()
    {
        return colors[currentGroup];
    }

    private void OnTouchPadLeftPress(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        ChangeGroup(++currentGroup);
    }

    private void OnTouchPadRightPress(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        ChangeGroup(++currentGroup);
    }


    private void OnRightTriggerDown(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        selectionActive = true;
        renderer.enabled = true;
    }

    private void OnRightTriggerUp(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        selectionActive = false;
        renderer.enabled = false;
    }

    private void Update()
    {
        if (!selectionActive) return;

        else
        {
        }
    }
}