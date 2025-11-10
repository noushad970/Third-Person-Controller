// File: DynamicCameraFollow.cs
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class DynamicCameraFollow : MonoBehaviour
{
    [Header("=== Target ===")]
    public Transform player;                    // Player transform
    public CharacterMovement characterMovement; // For speed-based distance

    [Header("=== Distance & Height ===")]
    public float minDistance = 4f;              // When walking/stopped
    public float maxDistance = 10f;             // When sprinting
    public float heightOffset = 2f;             // Camera height above player

    [Header("=== Rotation Settings ===")]
    public float mouseSensitivity = 100f;
    public float minVerticalAngle = -40f;       // Look down limit
    public float maxVerticalAngle = 80f;        // Look up limit

    [Header("=== Smoothness ===")]
    public float positionSmoothTime = 0.15f;
    public float rotationSmoothTime = 0.1f;

    // Runtime
    private float currentDistance;
    private float rotX = 0f;                    // Vertical rotation (mouse Y)
    private Vector3 velocity = Vector3.zero;

    void Start()
    {
        // Lock & hide cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Optional: Initialize rotX from current camera angle
        Vector3 angles = transform.eulerAngles;
        rotX = angles.x > 180 ? angles.x - 360 : angles.x;
    }

    void LateUpdate()
    {
        if (player == null || characterMovement == null) return;

        HandleMouseInput();
        UpdateDynamicDistance();
        UpdateCameraPositionAndRotation();
    }

    void HandleMouseInput()
    {
        // Mouse input
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Rotate player horizontally (Y-axis)
        player.Rotate(Vector3.up * mouseX);

        // Rotate camera vertically (X-axis)
        rotX -= mouseY;
        rotX = Mathf.Clamp(rotX, minVerticalAngle, maxVerticalAngle);
    }

    void UpdateDynamicDistance()
    {
        float speed = characterMovement.GetCurrentSpeed();
        float speedRatio = Mathf.InverseLerp(0f, characterMovement.sprintSpeed, speed);
        currentDistance = Mathf.Lerp(minDistance, maxDistance, speedRatio);
    }

    void UpdateCameraPositionAndRotation()
    {
        // Desired position: behind player + offset
        Vector3 desiredPosition = player.position
                                - player.forward * currentDistance
                                + Vector3.up * heightOffset;

        // Smooth position
        transform.position = Vector3.SmoothDamp(
            transform.position, desiredPosition, ref velocity, positionSmoothTime);

        // Smooth rotation: look at player with vertical tilt
        Vector3 lookPoint = player.position + Vector3.up * 1.5f;
        Vector3 direction = lookPoint - transform.position;

        Quaternion targetRotation = Quaternion.Euler(rotX, player.eulerAngles.y, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation,
                                            rotationSmoothTime / Time.deltaTime);
    }

    // Optional: Unlock cursor with ESC
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    // Visualize
    void OnDrawGizmosSelected()
    {
        if (player != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(player.position + Vector3.up * heightOffset, 0.5f);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, player.position + Vector3.up * 1.5f);
        }
    }
}