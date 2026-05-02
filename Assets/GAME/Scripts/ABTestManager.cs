using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ABTestManager : MonoBehaviour
{
    [Header("Target References")]
    [Tooltip("Reference to the RightGun script.")]
    public RightGun rightGun;

    [Tooltip("Reference to the Obstacles GameObject.")]
    public GameObject obstaclesObject;

    [Tooltip("Reference to the Shield script.")]
    public Shield shield;

    [Header("Input Settings")]
    [Tooltip("Key to switch the fire mode of the right gun.")]
    public Key switchFireModeKey = Key.M;

    [Tooltip("Key to toggle the Obstacles object.")]
    public Key toggleObstaclesKey = Key.N;

    [Tooltip("Key to toggle Shield homing reflection.")]
    public Key toggleShieldHomingKey = Key.B;

    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current == null) return;

        if (rightGun != null && Keyboard.current[switchFireModeKey].wasPressedThisFrame)
        {
            if (rightGun.currentFireMode == RightGun.FireMode.Laser)
            {
                rightGun.currentFireMode = RightGun.FireMode.PulseBullet;
                Debug.Log("ABTestManager: Switched RightGun Fire Mode to PulseBullet.");
            }
            else
            {
                rightGun.currentFireMode = RightGun.FireMode.Laser;
                Debug.Log("ABTestManager: Switched RightGun Fire Mode to Laser.");
            }
        }

        if (obstaclesObject != null && Keyboard.current[toggleObstaclesKey].wasPressedThisFrame)
        {
            obstaclesObject.SetActive(!obstaclesObject.activeSelf);
            Debug.Log($"ABTestManager: Toggled Obstacles to {obstaclesObject.activeSelf}");
        }

        if (shield != null && Keyboard.current[toggleShieldHomingKey].wasPressedThisFrame)
        {
            shield.homing = !shield.homing;
            Debug.Log($"ABTestManager: Toggled Shield Homing to {shield.homing}");
        }
    }
}
