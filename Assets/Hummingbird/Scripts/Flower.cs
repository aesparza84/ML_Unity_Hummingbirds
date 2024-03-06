using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages a single flower with nectar
/// </summary>
public class Flower : MonoBehaviour
{
    [Tooltip("Color attributes")]
    public Color fullFlowerColor = new Color(1f, 0f, 0.3f);
    public Color emptyFlowerColor = new Color(0.5f, 0f, 0.1f);

    /// <summary>
    /// Trigger for nectar 
    /// </summary>
    [HideInInspector]
    public Collider nectarCollider;

    //flower petals collider
    private Collider flowerCollider;

    //flower material
    private Material flowerMaterial;

    /// <summary>
    /// The 'up' orientation of the flower
    /// </summary>
    public Vector3 FlowerUpVector
    {
        get
        {
            return nectarCollider.transform.up;
        }
    }

    /// <summary>
    /// The center of the flower
    /// </summary>
    public Vector3 FlowerCenterPosition
    {
        get { return nectarCollider.transform.position; }
    }

    //We can observe it, but only set within this class
    public float NectarAmount
    {
        get;
        private set;
    }

    public bool HasNectar
    {
        get { return NectarAmount > 0.0f; }
    }

    /// <summary>
    /// Attempts to remove nectar from the flower
    /// </summary>
    /// <param name="amount">The amount of nectar to remove</param>
    /// <returns>The amount of nectar successfuly removed</returns>
    public float Feed(float amount)
    {
        //Get how much nextar is being subtracted
        float nectarTaken = Mathf.Clamp(amount, 0f, NectarAmount);

        NectarAmount -= amount;
        
        //Check for negative nectar amount
        if (nectarTaken <= 0.0f)
        {
            NectarAmount = 0.0f;

            //Disable flower and nectar colliders
            flowerCollider.gameObject.SetActive(false);
            nectarCollider.gameObject.SetActive(false);

            //Change color to show its empty
            flowerMaterial.SetColor("_BaseColor", emptyFlowerColor);
        }

        //Return nectar taken
        return nectarTaken;
    }

    /// <summary>
    /// Resets the flower
    /// </summary>
    public void ResetFlower()
    {
        NectarAmount = 1.0f;
        nectarCollider.gameObject.SetActive(true);
        flowerCollider.gameObject.SetActive(true);
        flowerMaterial.SetColor("_BaseColor", fullFlowerColor);
    }
}
