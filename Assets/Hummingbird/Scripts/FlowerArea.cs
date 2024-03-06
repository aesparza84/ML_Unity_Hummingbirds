using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages a collection of flowers within an area
/// </summary>
public class FlowerArea : MonoBehaviour
{
    //Diameter of the rea where the agent and flowers
    //can be used for observing relative distance from agent to flower
    public const float AreaDiameter = 20.0f;

    //Collecttion of all flower clusters.
    private List<GameObject> flowerPlants;

    //A lookup dictionary for looking up a flower from a nectar collider
    private Dictionary<Collider, Flower> nectarFlowerDictionary;

    /// <summary>
    /// List of Flower componenets from the clusters
    /// </summary>
    public List<Flower> Flowers 
    {
        get;
        private set;
    }

    /// <summary>
    /// Reset clusters and plants
    /// </summary>
    public void ResetFlowers()
    {
        //Rotate plants around their stems
        foreach (GameObject item in flowerPlants)
        {
            float xRot = UnityEngine.Random.Range(-5.0f, 5.0f);
            float yRot = UnityEngine.Random.Range(-180.0f, 180.0f);
            float zRot = UnityEngine.Random.Range(-5.0f, 5.0f);
            item.transform.localRotation = Quaternion.Euler(xRot, yRot, zRot);
        }

        //Reset each flower
        foreach (Flower item in Flowers)
        {
            item.ResetFlower();
        }
    }

    /// <summary>
    /// Gets the <see cref="Flower"/> that a nectar collider belongs to
    /// </summary>
    /// <param name="collider">Nectar collider</param>
    /// <returns>Matching flower via dictionary key</returns>
    public Flower GetFlowerFromNectar(Collider collider)
    {
        return nectarFlowerDictionary[collider];
    }

    private void Awake()
    {
        flowerPlants = new List<GameObject>();
        nectarFlowerDictionary = new Dictionary<Collider, Flower>();
        Flowers = new List<Flower>();
    }

    private void Start()
    {
        //Find all child gameobjects that are Flowers
        FindChildFlowers(transform);
    }

    /// <summary>
    /// Recursively finds all flower sna cluster that are children of a parent tranform
    /// </summary>
    /// <param name="parent">The parent of children to check</param>
    private void FindChildFlowers(Transform parent)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);

            if (child.CompareTag("flower_plant"))
            {
                //Found a flower cluster, add to list
                flowerPlants.Add(child.gameObject);

                //Look for flowers in cluster
                FindChildFlowers(child);
            }
            else
            {
                //Not a cluster, look Flower
                Flower flower = child.GetComponent<Flower>();
                if (flowerPlants != null)
                {
                    //Add to Flower list
                    Flowers.Add(flower);

                    //Add nectarColl to dictionary
                    nectarFlowerDictionary.Add(flower.nectarCollider, flower);

                    //There are not flowers that are chcildren of other flowers
                }
                else
                {
                    //Flower not found, keep checking
                    FindChildFlowers(child);
                }
            }
        }
    }
}
