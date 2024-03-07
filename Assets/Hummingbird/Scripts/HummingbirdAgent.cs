using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
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
    /// Called when an ACtion is recieved from Player or Neueral-Network
    /// 
    /// Index Guide:
    /// [0] : (+1 move right, -1 move left, 0 no move)
    /// [1] : (+1 up, -1 down)
    /// [2] : (+1 forward, -1 back)
    /// [3] : (+1 Pitch Up, -1 Pitch Down)
    /// [4] : (+1 Turn Right, -1 Turn Left)
    /// 
    /// </summary>
    /// <param name="actions">Action input param</param>
    public override void OnActionReceived(ActionBuffers actions)
    {
        if (frozen)
        {
            return;
        }

        //A list of numbers that tell the Agent what to do
        float[] vectorAction = actions.ContinuousActions.Array;

        //Move Vector, Direction
        Vector3 move = new Vector3(vectorAction[0], vectorAction[1], vectorAction[2]);

        //Force computing
        rigidbody.AddForce(move * moveForce);

        //Get Rotation
        Vector3 rotatoinVector = transform.rotation.eulerAngles;

        //Get pitch and yaw
        float newPitch = vectorAction[3];
        float yawChange = vectorAction[4];

        //Compute smooth pitch/yaw rotation
        smoothPitchChange = Mathf.MoveTowards(smoothPitchChange, pitchSpeed, 2f * Time.fixedDeltaTime);
        smoothYawChange = Mathf.MoveTowards(smoothYawChange, pitchSpeed, 2f * Time.fixedDeltaTime);

        //Calculate new pitch and yaw from smoothed values
        float pitch = rotatoinVector.x + smoothPitchChange * Time.fixedDeltaTime * pitchSpeed;
        if (pitch > 180.0f)
        {
            pitch -= 360.0f;
        }
        pitch = Mathf.Clamp(pitch, -MaxPitchAngle, MaxPitchAngle);

        float yaw = rotatoinVector.y + smoothYawChange * Time.fixedDeltaTime * yawSpeed;

        //Apply pitch and yaw
        transform.rotation = Quaternion.Euler(pitch, yaw, 0);
    }

    /// <summary>
    /// Collects vector observations from environment
    /// </summary>
    /// <param name="sensor">The vector sensor</param>
    public override void CollectObservations(VectorSensor sensor)
    {
        //Observes rotation of agent
        //Vector to nearestFlower
        //Dotprouduct of flower to beak
        //----------------------------

        if (nearestFlower == null)
        {
            sensor.AddObservation(new float[10]);
            return;
        }

        //Get agents local rotation
        sensor.AddObservation(transform.localRotation.normalized); //In relation to island

        //Get vector from beak to nearestFlower, Add normalized to observ.
        Vector3 beakToFlower = nearestFlower.FlowerCenterPosition - beakTip.position;
        sensor.AddObservation(beakToFlower.normalized); //Adding the direction to the flower


        //Get Dot product to flower
            //Observe if beak is directly IN FRONT of a flower
        float beakDirectDot = Vector3.Dot(beakToFlower.normalized, -nearestFlower.FlowerUpVector.normalized);
        sensor.AddObservation(beakDirectDot);

            //Observe if beak is pointing TOWARDS a flower
        float beakRelativelyDot = Vector3.Dot(beakTip.forward.normalized, -nearestFlower.FlowerUpVector.normalized);
        sensor.AddObservation(beakRelativelyDot);

        //Observe the distance to the flower
        sensor.AddObservation(beakToFlower.magnitude / FlowerArea.AreaDiameter);

    }

    /// <summary>
    /// When Behvaior type is set to heuristic, this method will be called.
    /// Its return values will return into <see cref="OnActionReceived(ActionBuffers)"/>
    /// insteas of using the neural network.
    /// 
    /// </summary>
    /// <param name="actionsOut">output action array</param>
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        float[] vectorAction = actionsOut.ContinuousActions.Array;

        Vector3 forward = Vector3.zero;
        Vector3 left = Vector3.zero;
        Vector3 up = Vector3.zero;
        float pitch = 0.0f;
        float yaw = 0.0f;

        //Convert keyboard into movement inputs [either 1 or -1]

        // Forward/Backwards input
        if (Input.GetKey(KeyCode.W))
        {
            forward = transform.forward;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            forward = -transform.forward;
        }

        // Left/Right input
        if (Input.GetKey(KeyCode.A))
        {
            left = -transform.right;
        }
        else if (Input.GetKey(KeyCode.D))
        {
            left = transform.right;
        }

        // Up/Down input
        if (Input.GetKey(KeyCode.E))
        {
            up = transform.up;
        }
        else if (Input.GetKey(KeyCode.Q))
        {
            up = -transform.up;
        }

        //Pitch Up/Down input
        if (Input.GetKey(KeyCode.UpArrow))
        {
            pitch = 1.0f;
        }
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            pitch = -1.0f;
        }

        //Yaw Left/Right input
        if (Input.GetKey(KeyCode.RightArrow))
        {
            yaw = 1.0f;
        }
        else if (Input.GetKey(KeyCode.LeftArrow))
        {
            yaw = -1.0f;
        }

        //Combine for full movement
        Vector3 direction = (forward + up + left).normalized;

        vectorAction[0] = direction.x;
        vectorAction[1] = direction.y;
        vectorAction[2] = direction.z;
        vectorAction[3] = pitch;
        vectorAction[4] = yaw;
    }

    /// <summary>
    /// Stops agent from moving and making decisions
    /// </summary>
    public void FreezeAgent()
    {
        Debug.Assert(trainingMode == false, "Freeze/Unfreeze not supported in training");
        frozen = true;
        rigidbody.Sleep();
    }

    /// <summary>
    /// Resmue agent physics and decision making
    /// </summary>
    public void UnreezeAgent()
    {
        Debug.Assert(trainingMode == false, "Freeze/Unfreeze not supported in training");
        frozen = false;
        rigidbody.WakeUp();
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

    /// <summary>
    /// Gets the nearest flower to the agent when called
    /// </summary>
    private void UpdateNearestFlower()
    {
        foreach (Flower newFlower in flowerArea.Flowers)
        {
            if (nearestFlower == null && newFlower.HasNectar)
            {
                //Agent is already near flower that has nectar
                nearestFlower = newFlower;
            }
            else if (newFlower.HasNectar)
            {
                //Compute distance to this flower and distance to current nearestFlower
                float distanceToFlower = Vector3.Distance(newFlower.transform.position, beakTip.position);
                float distanceToCurrent = Vector3.Distance(nearestFlower.transform.position, beakTip.position);

                //If the current nearest flower is empty or newFLower is closer, then update nearest
                if (!nearestFlower.HasNectar || distanceToFlower < distanceToCurrent)
                {
                    nearestFlower = newFlower;
                }
            }
        }
    }

    
    private void TriggerEnterOrStay(Collider collider)
    {
        //Check if colliding with nectar collider

        if (collider.CompareTag("nectar"))
        {
            Vector3 closestPointToBeak = collider.ClosestPoint(beakTip.position);

            //Check if close colliion point is by the beakTip

            if (Vector3.Distance(beakTip.position, closestPointToBeak) < BeakTipRadius)
            {
                //find corresponding flower from collider
                Flower flower = flowerArea.GetFlowerFromNectar(collider);

                //Try and drink nectar
                float nectarRecieved = flower.Feed(0.01f); //Small amount due to 'Update'

                NectarObtained += nectarRecieved;

                if (trainingMode)
                {
                    //Compute reward for agent
                    float bonus = 0.02f * Mathf.Clamp01(Vector3.Dot(transform.forward.normalized, -nearestFlower.FlowerUpVector.normalized));
                    AddReward(0.01f + bonus);
                }

                //If flower nectar empty, update nearest flower
                if (!flower.HasNectar)
                {
                    UpdateNearestFlower();
                }
            }
        }
    }

    /// <summary>
    /// Called when Agent's collider enter a trigger collider
    /// </summary>
    /// <param name="other"></param>
    private void OnTriggerEnter(Collider other)
    {
        TriggerEnterOrStay(other);
    }

    /// <summary>
    /// Called when Agent's collider enter a trigger collider
    /// </summary>
    /// <param name="other"></param>
    private void OnTriggerStay(Collider other)
    {
        TriggerEnterOrStay(other);
    }

    /// <summary>
    /// Solid Collision calling
    /// </summary>
    /// <param name="collision"></param>
    private void OnCollisionEnter(Collision collision)
    {
        if (trainingMode && collision.collider.CompareTag("boundary"))
        {
            //Negative reward
            AddReward(-0.5f);
        }   
    }

    private void Update()
    {
        //Draw ray from beak to nearestFlower
        if (nearestFlower != null)
        {
            Debug.DrawLine(beakTip.position, nearestFlower.FlowerCenterPosition, Color.green);
        }
    }

    private void FixedUpdate()
    {
        //Allows other hummingbird to update next flower
        //if current flower is stolen
        if (nearestFlower != null && !nearestFlower.HasNectar)
        {
            UpdateNearestFlower();
        }
    }
}
