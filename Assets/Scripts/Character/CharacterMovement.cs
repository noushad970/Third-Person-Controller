// File: CharacterMovement.cs - Fixed Ragdoll Recovery (Natural Transition)
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class CharacterMovement : MonoBehaviour
{
    [Header("=== References ===")]
    public Transform cameraTransform;
    public Transform ragdollRoot;
    public Animator animator;

    [Header("=== Speed ===")]
    public float walkSpeed = 5f;
    public float runSpeed = 8f;
    public float sprintSpeed = 12f;

    [Header("=== Acceleration ===")]
    public float walkAccel = 10f;
    public float runAccel = 15f;
    public float sprintAccel = 20f;
    public float groundDecel = 25f;

    [Header("=== Rotation ===")]
    public float rotationSpeed = 12f;

    [Header("=== Jump & Gravity ===")]
    public float jumpHeight = 2f;
    public float gravity = -9.81f;
    public float fallTimeout = 0.15f;

    [Header("=== RAGDOLL ===")]
    public float ragdollVelocityThreshold = 8f;
    public float ragdollForceThreshold = 500f;
    public float ragdollRecoverTime = 3f;

    [Header("=== Ground Check ===")]
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;
    // Add these near other runtime fields
    private bool isRecovering = false;
    private float recoveryLockDuration = 0f;
    private Coroutine recoveryLockCoroutine = null;

    // Runtime
    private CharacterController controller;
    private Vector3 velocity = Vector3.zero;
    private Vector3 moveInput;
    private Vector3 currentMoveDir;
    private float currentSpeed;
    private float targetSpeed;
    private float currentAccel;
    private bool isGrounded;
    private bool wasGrounded;
    private float fallTimer;
    private bool isRagdoll = false;
    private float ragdollTimer;
    private float lastAnimSpeed = 0f;

    // Ragdoll
    private Rigidbody[] ragdollRbs;

    // Animation hashes
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int TurnHash = Animator.StringToHash("Turn");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int JumpHash = Animator.StringToHash("Jump");
    private static readonly int LandHash = Animator.StringToHash("Land");

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        SetupRagdollFromWizard();
        currentSpeed = 0f;
        currentMoveDir = transform.forward;

        if (cameraTransform == null) cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (isRagdoll) RecoverFromRagdoll();
            else TriggerRagdollManual();
            return;
        }

        if (isRagdoll)
        {
            ragdollTimer += Time.deltaTime;
            if (ragdollTimer > ragdollRecoverTime)
                RecoverFromRagdoll();
            return;
        }

        // NEW: BLOCK INPUT DURING STAND-UP
        if (isRecovering)
        {
            velocity.x = velocity.z = 0f;
            currentSpeed = 0f;
            animator.SetFloat(SpeedHash, 0f);
            animator.SetFloat(TurnHash, 0f);
            return; // Skip all movement logic
        }
        wasGrounded = isGrounded;
        HandleInput();
        UpdateSpeed();
        HandleJump();

        // Normal movement
        
        HandleGroundCheck();
        HandleFallAnimation();
        UpdateHorizontalMovement();
        UpdateRotation();
        ApplyGravity();
        controller.Move(velocity * Time.deltaTime);
        UpdateAnimator();
        CheckRagdollTriggers();
    }

    void SetupRagdollFromWizard()
    {
        if (ragdollRoot == null)
            ragdollRoot = transform.Find("RagdollRoot");

        ragdollRbs = ragdollRoot != null ? ragdollRoot.GetComponentsInChildren<Rigidbody>() : GetComponentsInChildren<Rigidbody>();

        foreach (var rb in ragdollRbs)
            rb.isKinematic = true;

        Debug.Log($"RAGDOLL: {ragdollRbs.Length} bones ready!");
    }

    void TriggerRagdollManual()
    {
        isRagdoll = true;
        ragdollTimer = 0f;
        controller.enabled = false;
        animator.enabled = false;

        foreach (var rb in ragdollRbs)
        {
            rb.isKinematic = false;
            rb.AddForce(Vector3.down * 3f + Random.insideUnitSphere * 4f, ForceMode.Impulse);
        }

        Debug.Log("💥 RAGDOLL ON!");
    }

    // ✅ Fixed - Natural Reverse Ragdoll Recovery
    void RecoverFromRagdoll()
    {
        isRagdoll = false;
        ragdollTimer = 0f;

        // 1. Cache hips
        Transform hips = ragdollRoot.GetComponentInChildren<Animator>()?.GetBoneTransform(HumanBodyBones.Hips);
        if (hips == null) hips = animator.GetBoneTransform(HumanBodyBones.Hips);
        if (hips == null)
        {
            Debug.LogWarning("No hips bone found — skipping ragdoll recovery!");
            return;
        }

        Vector3 hipsPos = hips.position;
        Quaternion hipsRot = hips.rotation;

        // 2. Disable physics
        foreach (var rb in ragdollRbs)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // 3. Align root
        Vector3 newRootPos = hipsPos;
        newRootPos.y = GetGroundHeightBelow(newRootPos);
        transform.position = newRootPos;

        Vector3 forward = hips.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f) forward = transform.forward;
        transform.rotation = Quaternion.LookRotation(forward);

        // 4. Re-enable systems
        animator.enabled = true;
        controller.enabled = true;

        // 5. Play stand-up and LOCK MOVEMENT
        string standUpAnim = DetectStandUpAnimation(hips);
        AnimatorStateInfo stateInfo = animator.GetNextAnimatorStateInfo(0);
        if (stateInfo.fullPathHash == 0) stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        // Estimate clip length (fallback to 1.5s if unknown)
        float clipLength = 8f;
        AnimationClip clip = GetAnimationClip(animator, standUpAnim);
        if (clip != null) clipLength = clip.length;

        // Start recovery lock
        if (recoveryLockCoroutine != null) StopCoroutine(recoveryLockCoroutine);
        recoveryLockCoroutine = StartCoroutine(LockControlsDuringRecovery(clipLength));

        animator.Play(standUpAnim, 0, 0f);

        // 6. Reset velocity
        velocity = Vector3.zero;
        currentSpeed = 0f;
        currentMoveDir = transform.forward;

        Debug.Log($"RECOVERING: Playing '{standUpAnim}' ({clipLength:F2}s lock)");
    }
    private AnimationClip GetAnimationClip(Animator anim, string clipName)
    {
        if (anim.runtimeAnimatorController == null) return null;
        foreach (AnimationClip clip in anim.runtimeAnimatorController.animationClips)
        {
            if (clip.name == clipName) return clip;
        }
        return null;
    }
    private IEnumerator LockControlsDuringRecovery(float duration)
    {
        isRecovering = true;
        recoveryLockDuration = duration;

        // Wait for animation to finish (with small buffer)
        yield return new WaitForSeconds(duration + 0.1f);

        isRecovering = false;
        recoveryLockCoroutine = null;
    }
    float GetGroundHeightBelow(Vector3 pos)
    {
        if (Physics.Raycast(pos + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 2f, groundMask))
            return hit.point.y;
        return pos.y;
    }

    // ✅ Reliable orientation-based stand-up detection
    string DetectStandUpAnimation(Transform hips)
    {
        float dot = Vector3.Dot(hips.up, Vector3.up);
        if (dot > 0f)
        {
            Debug.Log("DETECTED: Face UP → BackUp");
            return "BackUp";
        }
        else
        {
            Debug.Log("DETECTED: Face DOWN → StomachUp");
            return "StomachUp";
        }
    }

    void HandleInput()
    {
        Vector3 camForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
        Vector3 camRight = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;

        moveInput = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) moveInput += camForward;
        if (Input.GetKey(KeyCode.S)) moveInput -= camForward;
        if (Input.GetKey(KeyCode.A)) moveInput -= camRight;
        if (Input.GetKey(KeyCode.D)) moveInput += camRight;
        moveInput = moveInput.normalized;
    }

    void UpdateSpeed()
    {
        if (Input.GetKey(KeyCode.LeftControl))
        { targetSpeed = sprintSpeed; currentAccel = sprintAccel; }
        else if (Input.GetKey(KeyCode.LeftShift))
        { targetSpeed = runSpeed; currentAccel = runAccel; }
        else
        { targetSpeed = walkSpeed; currentAccel = walkAccel; }

        if (moveInput.sqrMagnitude > 0.01f)
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, currentAccel * Time.deltaTime);
        else
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, groundDecel * Time.deltaTime);
    }

    void HandleJump()
    {
        if (isRecovering) return; // ADD THIS LINE
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            animator.SetTrigger(JumpHash);
        }
    }
    void UpdateRotation()
    {
        if (isRecovering) return; // ADD THIS LINE
        if (currentMoveDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(currentMoveDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }
    }

    void HandleGroundCheck()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        animator.SetBool(IsGroundedHash, isGrounded);

        if (!wasGrounded && isGrounded && velocity.y < 0)
            animator.SetTrigger(LandHash);
    }

    void HandleFallAnimation()
    {
        if (!isGrounded && velocity.y < 0) fallTimer += Time.deltaTime;
        else fallTimer = 0f;
    }

    void UpdateHorizontalMovement()
    {
        if (currentSpeed > 0.1f)
        {
            Vector3 desiredDir = moveInput.sqrMagnitude > 0.01f ? moveInput : currentMoveDir;
            currentMoveDir = Vector3.Lerp(currentMoveDir, desiredDir, rotationSpeed * Time.deltaTime).normalized;
            Vector3 move = currentMoveDir * currentSpeed;
            velocity.x = move.x;
            velocity.z = move.z;
        }
        else { velocity.x = 0f; velocity.z = 0f; }
    }



    void ApplyGravity()
    {
        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;
        else
            velocity.y += gravity * Time.deltaTime;
    }

    void UpdateAnimator()
    {
        float targetAnimSpeed = 0f;
        if (currentSpeed > 0.1f)
        {
            if (currentSpeed >= sprintSpeed * 0.9f) targetAnimSpeed = 3f;
            else if (currentSpeed >= runSpeed * 0.9f) targetAnimSpeed = 2f;
            else if (currentSpeed >= walkSpeed * 0.5f) targetAnimSpeed = 1f;
        }

        if (lastAnimSpeed > 0.1f && targetAnimSpeed < 0.1f)
        {
            if (lastAnimSpeed >= 2.5f) animator.Play("SprintStop", 0, 0f);
            else if (lastAnimSpeed >= 1.5f) animator.Play("RunStop", 0, 0f);
            else if (lastAnimSpeed >= 0.5f) animator.Play("WalkStop", 0, 0f);
        }
        else
            animator.SetFloat(SpeedHash, targetAnimSpeed, 0.1f, Time.deltaTime);

        float turn = 0f;
        if (currentSpeed > 0.1f && moveInput.sqrMagnitude > 0.1f)
            turn = transform.InverseTransformDirection(moveInput).x;
        animator.SetFloat(TurnHash, turn, 0.1f, Time.deltaTime);

        lastAnimSpeed = targetAnimSpeed;
    }

    void CheckRagdollTriggers()
    {
        if (!wasGrounded && isGrounded && velocity.y < -ragdollVelocityThreshold)
            TriggerRagdoll(-velocity.y);

        if (!isGrounded && -velocity.y > ragdollVelocityThreshold)
            TriggerRagdoll(-velocity.y * 0.5f);
    }

    void TriggerRagdoll(float impactForce = 0f)
    {
        isRagdoll = true;
        ragdollTimer = 0f;
        controller.enabled = false;
        animator.enabled = false;

        foreach (var rb in ragdollRbs)
        {
            rb.isKinematic = false;
            if (impactForce > 0)
                rb.AddForce(Vector3.down * impactForce, ForceMode.Impulse);
        }
    }

    public void ApplyImpactForce(Vector3 force)
    {
        if (force.magnitude > ragdollForceThreshold)
            TriggerRagdoll(force.magnitude);
    }

    public float GetCurrentSpeed() => currentSpeed;
    public float SprintSpeed => sprintSpeed;

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
        }
    }
}
