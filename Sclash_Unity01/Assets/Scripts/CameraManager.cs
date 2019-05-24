﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    //State
    [HideInInspector]
    public string cameraState = "Inactive";


    //Zoom
    [SerializeField]
    Vector2 cameraZLimits = new Vector2(-9, -19);
    [SerializeField]
    Vector2 playersDistanceForCameraZoomedUnzoomedLimits = new Vector2(5, 25);
    float distanceBetweenPlayers = 0;

    Camera cam;

    //Camera movements
    [SerializeField]
    public float smoothMovementsMultiplier = 0.01f;

    [SerializeField]
    Vector2 maxSidesZoomedUnzoomed = new Vector2(-10, -5);

    GameObject[] players;



    // Start is called before the first frame update
    void Start()
    {
        cam = Camera.main;
        if (players == null)
        {
            players = FindPlayers();
        }
        //baseCameraZ = cam.transform.localPosition.z;
    }

    public GameObject[] FindPlayers()
    {
        PlayerStats[] stats = FindObjectsOfType<PlayerStats>();
        players = new GameObject[stats.Length];

        for (int i = 0; i < stats.Length; i++)
        {
            players[i] = stats[i].gameObject;
        }

        return players;
    }

    // Update is called once per frame
    void Update()
    {
        BehaviourDependingOnState();
    }

    void BehaviourDependingOnState()
    {
        if (cameraState == "Inactive")
        {

        }
        else if (cameraState == "Battle")
        {
            MoveCameraWithPlayers();
            ZoomCameraWithPlayers();
        }
    }

    void MoveCameraWithPlayers()
    {
        Vector3 tempPos = transform.position;
        if (players.Length > 1)
        {
            tempPos.x = 0;

            for (int i = 0; i < players.Length; i++)
            {
                tempPos.x += players[i].transform.position.x;
            }

            tempPos.x = tempPos.x / players.Length;
        }
        else if (players.Length == 1)
        {
            tempPos.x = players[0].transform.position.x;
        }

        float cameraLimitsDifference = Mathf.Abs(cameraZLimits.y) - Mathf.Abs(cameraZLimits.x);
        float maxSidesZoomUnzoomeDifference = maxSidesZoomedUnzoomed.y - maxSidesZoomedUnzoomed.x;
        float maxSide = 0;

        maxSide = maxSidesZoomedUnzoomed.x - maxSidesZoomUnzoomeDifference * ((cam.transform.localPosition.z - cameraZLimits.y) / cameraLimitsDifference);

        if (tempPos.x > maxSide)
        {
            tempPos.x = maxSide;
        }
        else if (tempPos.x < -maxSide)
        {
            tempPos.x = -maxSide;
        }


        transform.position = transform.position + (tempPos - transform.position) * smoothMovementsMultiplier;
    }


    void ZoomCameraWithPlayers()
    {
        Vector3 tempPos = cam.transform.localPosition;
        float newCamZ = 0;
        distanceBetweenPlayers = 0;

        if (players.Length > 1)
        {
            distanceBetweenPlayers = Mathf.Abs(Vector3.Distance(players[0].transform.position, players[1].transform.position));
        }
        else if (players.Length == 1)
        {
        }

        float cameraZLimitsDifference = Mathf.Abs(cameraZLimits.y) - Mathf.Abs(cameraZLimits.x);
        float playersDistanceForCameraZoomLimitsDifference = Mathf.Abs(playersDistanceForCameraZoomedUnzoomedLimits.y) - Mathf.Abs(playersDistanceForCameraZoomedUnzoomedLimits.x);

        if (distanceBetweenPlayers <= playersDistanceForCameraZoomedUnzoomedLimits.x)
            newCamZ = cameraZLimits.x;
        else if (distanceBetweenPlayers >= playersDistanceForCameraZoomedUnzoomedLimits.y)
            newCamZ = cameraZLimits.y;
        else
            newCamZ = cameraZLimits.x - cameraZLimitsDifference * ((distanceBetweenPlayers - playersDistanceForCameraZoomedUnzoomedLimits.x) / playersDistanceForCameraZoomLimitsDifference);


        Vector3 newPos = new Vector3(tempPos.x, tempPos.y, newCamZ);

        cam.transform.localPosition = cam.transform.localPosition + (newPos - cam.transform.localPosition) * smoothMovementsMultiplier;
    }
}
