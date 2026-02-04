using System;
using UnityEngine;

public class GroundCheck : MonoBehaviour
{
    [Tooltip("How far below the player to check for ground")]
    public float GroundDistance = 0.15f;

    [Tooltip("Is the player currently grounded")]
    public bool IsGrounded = true;

    [Tooltip("Normal of the ground the player is standing on")]
    public Vector3 GroundNormal { get; private set; } = Vector3.up;

    // Fired ONCE when the player lands
    public event Action Grounded;

    private bool _wasGrounded;

    private void Update()
    {
        // Start the ray slightly above the feet to avoid self-collision
        Vector3 rayOrigin = transform.position + Vector3.up * 0.01f;

        RaycastHit hit;
        bool groundedNow = Physics.Raycast(rayOrigin, Vector3.down, out hit, GroundDistance/* , default, QueryTriggerInteraction.Ignore */);

        if (groundedNow)
        {
            GroundNormal = hit.normal;
        }
        else
        {
            GroundNormal = Vector3.up;
        }

        // Landed this frame
        if (!_wasGrounded && groundedNow)
        {
            Grounded?.Invoke();
        }

        IsGrounded = groundedNow;
        
        _wasGrounded = groundedNow;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 rayOrigin = transform.position + Vector3.up * 0.01f;
        Gizmos.color = IsGrounded ? Color.white : Color.red;
        Gizmos.DrawLine(rayOrigin, rayOrigin + Vector3.down * GroundDistance);
    }
}