using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;

/// <summary>
/// A humming bird ML-Agent
/// </summary>
public class HummingbirdAgent : Agent
{
    [Tooltip("Force to apply when moving")]
    public float moveForce = 2.0f;

    [Tooltip("Speed to pitch up or down")]
    public float pitchSpeed = 100.0f;

    [Tooltip("Up axis rotate speed")]
    public float yawSpeed = 100.0f;

    [Tooltip("Beak tip-transform")]
    public Transform beakTip;

    [Tooltip("Agents Camera")]
    public Camera agentCamera;

    [Tooltip("TrainingMode/Gameplay Mode")]
    public bool trainingMode;

    //Agent rigidbody
    new private Rigidbody rigidbody;

    //Flower area agent is in
    private FlowerArea flowerArea;

    //Nearest Flower
    private Flower nearestFlower;

    //Smooth pitch change
    private float smoothPitchChange = 0.0f;

    //Smooth yaw change
    private float smoothYawChange = 0.0f;

    //Maximum pitch angle
    private const float MaxPitchAngle = 80.0f;

    //Max distance for collision between beak and gameobject
    private const float BeakTipRadius = 0.008f;

    //Is agent frozen, not flying
    private bool frozen = false;

    /// <summary>
    /// How much nectar obtained this episode
    /// </summary>
    public float NectarObtained
    {
        get;
        private set;
    }

    public override void Initialize()
    {
        rigidbody = GetComponent<Rigidbody>();
        flowerArea = GetComponentInParent<FlowerArea>();

        //!trainingMode, no max step, infinite play
        if (!trainingMode)
        {
            MaxStep = 0;
        }
    }

    /// <summary>
    /// Reset agent on new episode
    /// </summary>
    public override void OnEpisodeBegin()
    {
        if (trainingMode)
        {
            flowerArea.ResetFlowers();
        }

        //Reset nectar gained
        NectarObtained = 0.0f;

        //Reset velocities
        rigidbody.velocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;

        bool inFrontOfFlower = true;
        if (trainingMode)
        {

            //Spawn agent in front of a flower half the time, training only
            inFrontOfFlower = UnityEngine.Random.value > 0.5f;
        }

        //Move agent to a VALID loaction and not inside something
        MoveToSafeRandomPosition(inFrontOfFlower);

        //Get the nearest flower agent has moved to
        UpdateNearestFlower();
    }

    /// <summary>
    /// Moves agent to a valid non colliding position
    /// </summary>
    /// <param name="inFrontOfFlower">To place agent in front of a flower</param>
    private void MoveToSafeRandomPosition(bool inFrontOfFlower)
    {
        bool safePositionFound = false;
        int placeAttempts = 100;
        Vector3 potentialPos = Vector3.zero;
        Quaternion potentialRot = new Quaternion();

        while (!safePositionFound && placeAttempts > 0)
        {
            placeAttempts--;
            if (inFrontOfFlower)
            {
                //Get a random flower from flowerArea
                Flower randomFlower = flowerArea.Flowers[UnityEngine.Random.Range(0, flowerArea.Flowers.Count)];

                //Set agent 10-20cm away from picked flower
                float distanceFromFlower = UnityEngine.Random.Range(0.1f, 0.2f);
                potentialPos = randomFlower.transform.position + randomFlower.FlowerUpVector * distanceFromFlower;

                //Point agent's head to flower
                Vector3 toFLower = randomFlower.FlowerCenterPosition - potentialPos;
                potentialRot = Quaternion.LookRotation(toFLower, Vector3.up);
            }
            else
            {
                //Pick a height from the ground
                float height = UnityEngine.Random.Range(1.2f, 2.5f);

                //Pick a random radius from center of island
                float radius = UnityEngine.Random.Range(2.0f, 7.0f);

                //Pick a random direction rotated arounf y-axis
                Quaternion direction = Quaternion.Euler(0.0f, UnityEngine.Random.Range(-180.0f, 180.0f), 0.0f);

                //Combine all for position
                potentialPos = flowerArea.transform.position + Vector3.up * height + direction * Vector3.forward * radius;

                //Random pitch and yaw
                float pitch = UnityEngine.Random.Range(-60.0f, 60.0f);
                float yaw = UnityEngine.Random.Range(-180.0f, 180.0f);
                potentialRot = Quaternion.Euler(pitch, yaw, 0.0f);
            }

            //Check for agent collisions, 0.05f = 10cm
            Collider[] colliders = Physics.OverlapSphere(potentialPos, 0.05f);
            safePositionFound = colliders.Length == 0;
        }

        Debug.Assert(safePositionFound, "Could not find valid spawn point");

        transform.position = potentialPos;
        transform.rotation = potentialRot;
    }
    private void UpdateNearestFlower()
    {

    }    
}
