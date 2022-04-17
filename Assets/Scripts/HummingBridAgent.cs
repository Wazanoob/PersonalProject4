using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;

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
    }
}
