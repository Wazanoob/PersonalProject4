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

    //FlyingBird
    [SerializeField, Tooltip("Force to apply when moving")] private float m_moveForce = 2f;
    [SerializeField, Tooltip("Spped to pitch up or down")] private float m_pitchSpeed = 100f;
    [SerializeField, Tooltip("Speed to rotate around the up axis")] private float m_yawSpeed = 100f;

    //Beak
    [SerializeField, Tooltip("Transform at the tip of the beak")] private Transform m_beakTip;

    //Training or gameplay mode
    [SerializeField, Tooltip("Wheter this is training mode or gameplay mode")] private bool m_trainingMode;
}
