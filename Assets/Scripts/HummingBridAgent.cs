using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

//From UnityLearn Tutorial "ML Agents - HummingBirds"

/// <summary>
/// A hummingbird Machine Learning Agent
/// </summary>
public class HummingBridAgent : Agent
{
    //Ref
    [SerializeField, Tooltip("The Agent's camera")] private Camera m_agentCamera;
    private Rigidbody m_rigidbody;
    private FlowerArea m_flowerArea;
    private Flower m_nearestFlower;

    //FlyingBird
    [SerializeField, Tooltip("Force to apply when moving")] private float m_moveForce = 2f;
    [SerializeField, Tooltip("Spped to pitch up or down")] private float m_pitchSpeed = 100f;
    [SerializeField, Tooltip("Speed to rotate around the up axis")] private float m_yawSpeed = 100f;

    //Beak
    [SerializeField, Tooltip("Transform at the tip of the beak")] private Transform m_beakTip;

    //Training or gameplay mode
    [SerializeField, Tooltip("Wheter this is training mode or gameplay mode")] private bool m_trainingMode;

    //Smooth rotation and movement
    private float m_smoothPitchChange = 0f;
    private float m_smoothYawChange = 0f;

    //Constraint rotation
    private const float MAX_PITCH_ANGLE= 80f;

    //Maximum distance between the tip and the collider to accept 
    private const float BEAK_TIP_RADIUS = 0.008f;

    //Freez the bird
    private bool m_frozen = false;

    /// <summary>
    /// The amount of nectar the agent has obtained this episod
    /// </summary>
    public float NectarObtained { get; private set; }

    /// <summary>
    /// Initialize the agent
    /// </summary>
    public override void Initialize()
    {
        m_rigidbody = GetComponent<Rigidbody>();
        m_flowerArea = GetComponentInParent<FlowerArea>();

        //If not training, no max step, play forever
        if (!m_trainingMode) MaxStep = 0;
    }

    /// <summary>
    /// Reset the agent when an episod begins
    /// </summary>
    public override void OnEpisodeBegin()
    {
        if (m_trainingMode)
        {
            //Only reset flowers in training when there is one agent per area
            m_flowerArea.ResetFlower();
        }

        //Reset nectar obtained
        NectarObtained = 0;

        //Zero out velocities so that movement stop before new episod begins
        m_rigidbody.velocity = Vector3.zero;
        m_rigidbody.angularVelocity = Vector3.zero;

        //Default to spawning in front of a flower
        bool inFrontOfFlower = true;
        if (m_trainingMode)
        {
            //Spawn in front of flower 50% of time
            inFrontOfFlower = Random.value > .5f;
        }

        //Move the agent to a new random position
        MoveToSafeRandomPosition(inFrontOfFlower);

        //Recalculate the nearest flower now that the agent has moved
        UpdateNearestFlower();
    }

    public void Update()
    {
        //Draw a line from the beaktip to the nearest flower
        if (m_nearestFlower != null)
        {
            Debug.DrawLine(m_beakTip.position, m_nearestFlower.FlowerCenterPosition, Color.green);
        }
    }

    public void FixedUpdate()
    {
        if (m_nearestFlower != null && !m_nearestFlower.HasNectar)
        {
            UpdateNearestFlower();
        }
    }

    /// <summary>
    /// Called when an action is received from either the player input or the neural network
    /// 
    /// VectorAction[i] represents:
    /// Index 0: move vector x (+1 = right, -1 = left)
    /// Index 1: move vector y (+1 = up, -1 = down)
    /// Index 2: move Vector z (+1 = forward, -1 = baward)
    /// Index 3: pitch angle (+1 = pitch up, -1 = pitch down)
    /// Index 4: yaw angle (+1 = turn right, -1 turn left)
    /// </summary>
    /// <param name="actions">The actions to take</param>
    public override void OnActionReceived(ActionBuffers actions)
    {
        //Dont take actions if frozen
        if (m_frozen) return;

        //Calculate movement vector
        Vector3 move = new Vector3(actions.ContinuousActions[0], actions.ContinuousActions[1], actions.ContinuousActions[2]);

        //Add force in the direction of the move vector
        m_rigidbody.AddForce(move * m_moveForce);

        //Get the current rotation
        Vector3 rotationVector = transform.rotation.eulerAngles;

        //Calculate pitch and yaw rotation
        float pitchChange = actions.ContinuousActions[3];
        float yawChange = actions.ContinuousActions[4];

        //Calculate smmoth rotation changes
        m_smoothPitchChange = Mathf.MoveTowards(m_smoothPitchChange, pitchChange, 2f * Time.fixedDeltaTime);
        m_smoothYawChange = Mathf.MoveTowards(m_smoothYawChange, yawChange, 2f * Time.fixedDeltaTime);

        //Calculate new pitch and yaw based on smooth values
        //Clamp pitch to avoid flipping upside down
        float pitch = rotationVector.x + m_smoothPitchChange * Time.fixedDeltaTime * m_pitchSpeed;
        if (pitch > 180f) pitch -= 360f;
        pitch = Mathf.Clamp(pitch, -MAX_PITCH_ANGLE, MAX_PITCH_ANGLE);

        float yaw = rotationVector.y + m_smoothYawChange * Time.fixedDeltaTime * m_yawSpeed;

        //Apply the new rotation
        transform.rotation = Quaternion.Euler(pitch, yaw, 0);
    }

    /// <summary>
    /// Collect vector observations from the environment
    /// </summary>
    /// <param name="sensor">The vector sensor</param>
    public override void CollectObservations(VectorSensor sensor)
    {
        //If nearestFlower is null, observe an empty array and return early
        if (m_nearestFlower == null)
        {
            sensor.AddObservation(new float[10]);
            return;
        }

        //Observe the agent's local rotation (4 observations)
        sensor.AddObservation(transform.localRotation.normalized);

        //Get a vector from the beak tip to the nearest flower
        Vector3 toFlower = m_nearestFlower.FlowerCenterPosition - m_beakTip.position;

        //Observe a normalized vector (3 observations)
        sensor.AddObservation(toFlower.normalized);

        //Observe a dot product that indicates wheter the beak tip is in front of the flower (1 observation)
        // (+1 means that the beak tip is directly in front of the flower, -1 means directly behind)
        sensor.AddObservation(Vector3.Dot(toFlower.normalized, -m_nearestFlower.FlowerUpVector.normalized));

        //Observe a dot product that indicates wheter the beak is pointing toward the flower (1 observation)
        // (+1 means that the beak tip is poiting directly at the flower, -1 means directly away)
        sensor.AddObservation(Vector3.Dot(m_beakTip.forward.normalized, -m_nearestFlower.FlowerUpVector.normalized));

        //Observe the relative distance from the beak tip to the flower (1 observation)
        sensor.AddObservation(toFlower.magnitude / FlowerArea.AREA_DIAMETER);

        //10 total observations
    }

    /// <summary>
    /// When behavior type is set to "Heuristic Only" on the agent's behavior parameters,
    /// This function will be called. Return values will be fed into.
    /// </summary>
    /// <param name="actionsOut">An output action array</param>
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        //Create placeholders for all movement / turning
        Vector3 forward = Vector3.zero;
        Vector3 left = Vector3.zero;
        Vector3 up = Vector3.zero;
        float pitch = 0f;
        float yaw = 0f;

        //Convert Inputs to movement and turning
        //All values should be between -1 and +1

        //Forward / backward
        if (Input.GetKey(KeyCode.Z)) forward = transform.forward;
        else if(Input.GetKey(KeyCode.S)) forward = -transform.forward;

        //Right / left
        if (Input.GetKey(KeyCode.Q)) left = -transform.right;
        else if (Input.GetKey(KeyCode.D)) left = transform.right;

        //Up / Down
        if (Input.GetKey(KeyCode.E)) up = transform.up;
        else if (Input.GetKey(KeyCode.C)) up = -transform.up;

        //Pitch up / down
        if (Input.GetKey(KeyCode.UpArrow)) pitch = 1f;
        else if (Input.GetKey(KeyCode.DownArrow)) pitch = -1f;

        //Yaw up / down
        if (Input.GetKey(KeyCode.LeftArrow)) yaw = -1f;
        else if (Input.GetKey(KeyCode.RightArrow)) yaw = 1f;

        //Combine the movement vectors and normlaize
        Vector3 combined = (forward + left + up).normalized;

        //Add the 3 movement values, pitch, and yaw to the actionsOut array
        ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = combined.x;
        continuousActions[1] = combined.y;
        continuousActions[2] = combined.z;
        continuousActions[3] = pitch;
        continuousActions[4] = yaw;

    }

    /// <summary>
    /// Prevent the agent from moving and taking actions
    /// </summary>
    public void FreezeAgent()
    {
        Debug.Assert(m_trainingMode == false, "Freeze/Unfreeze not supported in training");
        m_frozen = true;
        m_rigidbody.Sleep();
    }

    /// <summary>
    /// Resume agent movement and actions
    /// </summary>
    public void UnfreezeAgent()
    {
        Debug.Assert(m_trainingMode == false, "Freeze/Unfreeze not supported in training");
        m_frozen = false;
        m_rigidbody.WakeUp();
    }

    /// <summary>
    /// Update the nearest flower to the agent
    /// </summary>
    private void UpdateNearestFlower()
    {
        foreach (Flower flower in m_flowerArea.Flowers)
        {
            if (m_nearestFlower == null && flower.HasNectar)
            {
                m_nearestFlower = flower;
            }else if (flower.HasNectar)
            {
                //Calculate distance to this flower and distance to the current nearest flower
                float distanceToFlower = Vector3.Distance(flower.transform.position, m_beakTip.position);
                float distanceToCurrentNearestFlower = Vector3.Distance(m_nearestFlower.transform.position, m_beakTip.transform.position);

                //If current nearest flower is empty OR this flower is closer, update nearest flower
                if (!m_nearestFlower.HasNectar || distanceToFlower < distanceToCurrentNearestFlower)
                {
                    m_nearestFlower = flower;
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TriggerEnterOrStay(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TriggerEnterOrStay(other);
    }

    /// <summary>
    /// Handles when the agent's collider enters or stays in a trigger collider
    /// </summary>
    /// <param name="other">The trigger collider</param>
    private void TriggerEnterOrStay(Collider other)
    {
        //Check if the agent is colliding with nectar
        if (other.CompareTag("Nectar"))
        {
            Vector3 closestPointToBeakTip = other.ClosestPoint(m_beakTip.position);

            //Check if the closest collision point is close to the beak tip
            //Note: a collision with anything but the beak tip should not count
            if (Vector3.Distance(m_beakTip.position, closestPointToBeakTip) < BEAK_TIP_RADIUS)
            {
                //Look up the flower for this nectar collider
                Flower flower = m_flowerArea.GetFlowerFromNectar(other);

                //Attempt to take .01 nectar
                //Note: this is per fixed timestep, it happened 50x per sec
                float nectarReceived = flower.Feed(0.01f);

                //Keep track of nectar obtained
                NectarObtained += nectarReceived;

                if (m_trainingMode)
                {
                    //Calculate rewards
                    float bonus = 0.02f * Mathf.Clamp01(Vector3.Dot(transform.forward.normalized, -m_nearestFlower.FlowerUpVector.normalized));
                    AddReward(.01f + bonus);
                }

                //If flower is empty, update the nearest flower
                if (!flower.HasNectar)
                {
                    UpdateNearestFlower();
                }
            }
        }
    }

    /// <summary>
    /// Called when the agent collides with something solid
    /// </summary>
    /// <param name="collision">The collision info</param>
    private void OnCollisionEnter(Collision collision)
    {
        if (m_trainingMode && collision.collider.CompareTag("Boundary"))
        {
            //Collided with the area boundary, give a negative reward
            AddReward(-.5f);
        }
    }

    /// <summary>
    /// Move the agent to a safe random position
    /// If in front of flower, also point the beak at the flower
    /// </summary>
    /// <param name="inFrontOfFlower">Whether to choose a spot in front of a flower</param>
    /// <exception cref="System.NotImplementedException"></exception>
    private void MoveToSafeRandomPosition(bool inFrontOfFlower)
    {
        bool safePositionFound = false;
        int attemptsRemaining = 100; // prevent an infinite loop

        Vector3 potentialPosition = Vector3.zero;
        Quaternion potentioalRotation = new Quaternion();

        //Loop until a safe position is found or we run out of attempts
        while (!safePositionFound && attemptsRemaining > 0)
        {
            attemptsRemaining--;
            if (inFrontOfFlower)
            {
                //Pick a random flower
                int random = Random.Range(0, m_flowerArea.Flowers.Count);
                Flower randomFlower = m_flowerArea.Flowers[random];

                //Position 10 to 20 cm in front of the flower
                float distanceFromFlower = Random.Range(0.1f, 0.2f);
                potentialPosition = randomFlower.transform.position + randomFlower.FlowerUpVector * distanceFromFlower;

                //Point beak at flower
                Vector3 toFlower = randomFlower.FlowerCenterPosition - potentialPosition;
                potentioalRotation = Quaternion.LookRotation(toFlower, Vector3.up);
            }else
            {
                //Pick a random height from the ground
                float height = Random.Range(1.2f, 2.5f);

                //Pick a random radius from the center of the area
                float radius = Random.Range(2f, 7f);

                //Pick a random direction rotated around the y axis
                Quaternion direction = Quaternion.Euler(0f, Random.Range(-180f, 180f), 0f);

                //Combine inputs
                potentialPosition = m_flowerArea.transform.position + Vector3.up * height + direction * Vector3.forward * radius;

                //Choose and set random starting pitch and yaw
                float pitch = Random.Range(-60f, 60f);
                float yaw = Random.Range(-180f, 180f);

                potentioalRotation = Quaternion.Euler(pitch, yaw, 0f);
            }

            //Check to see if the agent will collide with anything
            Collider[] colliders = Physics.OverlapSphere(potentialPosition, 0.05f);

            //Safe position has been found if no colliders are overlapped
            safePositionFound = colliders.Length == 0;
        }

        Debug.Assert(safePositionFound, "Could not find a safe position to spawn");

        //Set the position and rotation
        transform.position = potentialPosition;
        transform.rotation = potentioalRotation;
    }
}
