
using Unity.Cinemachine;
using UnityEngine;

public class DynamicCinemachineDistance : MonoBehaviour
{
    public CharacterMovement characterMovement; // Reference to your player's movement script
    public CinemachineFreeLook freeLookCamera;  // Reference to the FreeLook camera

    [Header("Distance Settings")]
    public float minRadius = 4f;   // Close distance (walking)
    public float maxRadius = 10f;  // Far distance (sprinting)

    void Update()
    {
        if (characterMovement == null || freeLookCamera == null) return;

        // Get player speed
        float speed = characterMovement.GetCurrentSpeed();

        // Calculate radius based on speed
        float speedRatio = Mathf.InverseLerp(0f, characterMovement.sprintSpeed, speed);
        float targetRadius = Mathf.Lerp(minRadius, maxRadius, speedRatio);

        // Apply to all rigs for simplicity
        freeLookCamera.m_Orbits[0].m_Radius = targetRadius; // Top
        freeLookCamera.m_Orbits[1].m_Radius = targetRadius; // Middle
        freeLookCamera.m_Orbits[2].m_Radius = targetRadius; // Bottom
    }
}