using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//From UnityLearn Tutorial "ML Agents - HummingBirds"

/// <summary>
/// Manages a single flower with nectar
/// </summary>
public class Flower : MonoBehaviour
{
    [Tooltip("The color when the flower is full")]
    public Color fullFlowerColor = new Color(1f, 0f, .3f);

    [Tooltip("The color when the flower is empty")]
    public Color emptyFlowerColor = new Color(0.5f, 0f, 1f);

    /// <summary>
    /// The trigger collider representing the nectar
    /// </summary>
    public Collider nectarCollider;

    //The solid collider representing the flower petals
    [SerializeField] private Collider m_flowerCollider;

    //The flower's Material
    private Material m_flowerMaterial;

    /// <summary>
    /// A vector pointing straight out of the flower
    /// </summary>
    // To let know the agent in what direction the flower is pointing
    public Vector3 FlowerUpVector
    {
        get
        {
            return nectarCollider.transform.up;
        }
    }

    /// <summary>
    /// The center position of the nectar collider
    /// </summary>
    public Vector3 FlowerCenterPosition
    {
        get
        {
            return nectarCollider.transform.position;
        }
    }

    /// <summary>
    /// The amount of nectar remaining in the flower
    /// </summary>
    public float NectarAmount { get; private set; }

    /// <summary>
    /// Wheter the flower has any nectar remaining
    /// </summary>
    public bool HasNectar
    {
        get
        {
            return NectarAmount > 0f;
        }
    }

    private void Awake()
    {
        //Find the flower's mesh renderer and get the main material
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        m_flowerMaterial = meshRenderer.material;

        //Find flower and nectar colliders
        if (m_flowerCollider == null)
        {
            m_flowerCollider = transform.Find("FlowerCollider").GetComponent<Collider>();
        }

        if (nectarCollider == null)
        {
            nectarCollider = transform.Find("FlowerNectarCollider").GetComponent<Collider>();
        }
    }

    /// <summary>
    /// Attempts to remove nectar from the flower
    /// </summary>
    /// <param name="amount"><The amount of nectar to remove/param>
    /// <returnsT>The actual amount successfully removed</returnsT></returns>
    public float Feed(float amount)
    {
        //Track how much nectar was successfully taken (cannot take more than is available)
        float nectarTaken = Mathf.Clamp(amount, 0f, NectarAmount);

        //Subtract the nectar
        NectarAmount -= amount;

        if (NectarAmount < 0f)
        {
            //No nectar remaining
            NectarAmount = 0;

            //Disable the flower and nectar colliders
            m_flowerCollider.gameObject.SetActive(false);
            nectarCollider.gameObject.SetActive(false);

            //Change the flower color to indicate that it is empty
            m_flowerMaterial.SetColor("_BaseColor", emptyFlowerColor);
        }

        //Return the amount of nectar that was taken
        //To give rewards
        return nectarTaken;
    }

    /// <summary>
    /// Resets the flower
    /// </summary>
    public void ResetFlower()
    {
        //Refill he nectar
        NectarAmount = 1f;

        //Enable the flower and nectar colliders
        m_flowerCollider.gameObject.SetActive(true);
        nectarCollider.gameObject.SetActive(true);

        //Change the flower color to indicate that it is full
        m_flowerMaterial.SetColor("_BaseColor", fullFlowerColor);
    }
}
