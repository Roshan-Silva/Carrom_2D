using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HoleController : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the object entering the hole is a coin
        if (other.CompareTag("Coin"))
        {
            // You can add logic to update score, remove the coin, etc.
            Debug.Log("Coin has sunk!");

            // Remove the coin from the scene
            Destroy(other.gameObject);
        }
    }
}
