using UnityEngine;

public class Enemy : MonoBehaviour
{
    /// <summary>
    /// Called when the laser hits this enemy.
    /// </summary>
    public void Hit()
    {
        // For testing, we just immediately destroy the GameObject.
        Debug.Log("Enemy hit! Destroying " + gameObject.name);
        Destroy(gameObject);
    }
}
