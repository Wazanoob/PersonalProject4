using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//From UnityLearn Tutorial "ML Agents - HummingBirds"

/// <summary>
/// Manages a collection of flower plants and attached flowers
/// </summary>
public class FlowerArea : MonoBehaviour
{
    //The diameter of the area where the agent and flowers can be
    //used for observing relative distance from agent to flower
    public const float AREA_DIAMETER = 20f;

    //The list of all flower plants in this flower area 
    //and flower plants have multiple flowers
    private List<GameObject> m_flowerPlants = new List<GameObject>();

    //A lookup dictionary for looking up a flower from a nectar collider
    private Dictionary<Collider, Flower> m_nectarFlowerDictionary = new Dictionary<Collider, Flower>();

    /// <summary>
    /// The list of all flowers in the flower area
    /// </summary>
    public List<Flower> Flowers { get; private set; } = new List<Flower>();

    private void Awake()
    {
        //Find all flowers that are children of this GameObject/ Transform
        FindChildFlowers(transform);
    }

    /// <summary>
    /// Reset the flowers and flower plants
    /// </summary>
    public void ResetFlower()
    {
        //Rotate each flower plant around the Y axis and subtly around X and Z
        foreach (GameObject flowerPlant in m_flowerPlants)
        {
            float xRotation = Random.Range(-5f, 5f);
            float yRotation = Random.Range(-180f, 180f);
            float zRotation = Random.Range(-5f, 5f);

            //Apply the randomness into the rotation
            flowerPlant.transform.localRotation = Quaternion.Euler(xRotation, yRotation, zRotation);
        }

        //Reset each flower
        foreach (Flower flower in Flowers)
        {
            flower.ResetFlower();
        }
    }

    /// <summary>
    /// Gets the <see cref="Flower"/> that a nectar collider belongs to
    /// </summary>
    /// <param name="collider">The nectar collider</param>
    /// <returns>The matching flower</returns>
    public Flower GetFlowerFromNectar(Collider collider)
    {
        return m_nectarFlowerDictionary[collider];
    }

    /// <summary>
    /// Recursively finds all flowers and flower plants that are children of a parent transform
    /// </summary>
    /// <param name="parent">The parent of the children to check</param>
    private void FindChildFlowers(Transform parent)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);

            if (child.CompareTag("Flower_Plant"))
            {
                //Found a flower plant, add it to the flowerPlants list
                m_flowerPlants.Add(child.gameObject);

                //Look for flowers within the flower plant
                FindChildFlowers(child);
            }
            else
            {
                //Not a flower plant, look for a Flower component
                Flower flower = child.GetComponent<Flower>();
                if (flower != null)
                {
                    //Found a flower, add it to the flower list
                    Flowers.Add(flower);

                    //Add the nectar collider to the lookup dictionary
                    m_nectarFlowerDictionary.Add(flower.nectarCollider, flower);
                }else
                {
                    //Check children
                    FindChildFlowers(child);
                }
            }
        }
    }
}
