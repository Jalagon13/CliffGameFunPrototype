using UnityEngine;

public class GroundCheck : MonoBehaviour
{
    [Tooltip("How far below the player to check for ground")]
    public float GroundDistance = 0.15f;

    [Tooltip("Is the player currently grounded")]
    public bool IsGrounded = true;

    // Fired ONCE when the player lands
    public event System.Action Grounded;

    bool wasGrounded;

    void Update()
    {
        // Start the ray slightly above the feet to avoid self-collision
        Vector3 rayOrigin = transform.position + Vector3.up * 0.01f;

        bool groundedNow = Physics.Raycast(
            rayOrigin,
            Vector3.down,
            GroundDistance
        );

        // Landed this frame
        if (!wasGrounded && groundedNow)
        {
            Grounded?.Invoke();
        }

        IsGrounded = groundedNow;
        wasGrounded = groundedNow;
    }

    void OnDrawGizmosSelected()
    {
        Vector3 rayOrigin = transform.position + Vector3.up * 0.01f;
        Gizmos.color = IsGrounded ? Color.white : Color.red;
        Gizmos.DrawLine(rayOrigin, rayOrigin + Vector3.down * GroundDistance);
    }
}