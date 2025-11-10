// File: RagdollSetup.cs
using UnityEngine;

public class RagdollSetup : MonoBehaviour
{
    public Transform head, spine, leftUpperArm, leftLowerArm, leftHand;
    public Transform rightUpperArm, rightLowerArm, rightHand;
    public Transform leftUpperLeg, leftLowerLeg, leftFoot;
    public Transform rightUpperLeg, rightLowerLeg, rightFoot;

    [ContextMenu("Create Ragdoll")]
    void CreateRagdoll()
    {
        CreatePart(head, 1f, 0.2f);
        CreatePart(spine, 1f, 0.3f);
        CreatePart(leftUpperArm, 0.8f, 0.15f);
        CreatePart(leftLowerArm, 0.8f, 0.12f);
        CreatePart(leftHand, 0.5f, 0.1f);
        CreatePart(rightUpperArm, 0.8f, 0.15f);
        CreatePart(rightLowerArm, 0.8f, 0.12f);
        CreatePart(rightHand, 0.5f, 0.1f);
        CreatePart(leftUpperLeg, 1.2f, 0.18f);
        CreatePart(leftLowerLeg, 1f, 0.15f);
        CreatePart(leftFoot, 0.6f, 0.12f);
        CreatePart(rightUpperLeg, 1.2f, 0.18f);
        CreatePart(rightLowerLeg, 1f, 0.15f);
        CreatePart(rightFoot, 0.6f, 0.12f);

        DestroyImmediate(this);
    }

    void CreatePart(Transform bone, float mass, float radius)
    {
        if (!bone) return;
        Rigidbody rb = bone.gameObject.AddComponent<Rigidbody>();
        rb.mass = mass;
        rb.linearDamping = 0.5f;
        rb.angularDamping = 0.05f;
        rb.isKinematic = true;

        CapsuleCollider col = bone.gameObject.AddComponent<CapsuleCollider>();
        col.radius = radius;
        col.height = 0.4f;
        col.direction = 1;
    }
}