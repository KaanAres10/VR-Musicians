using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

public class BatHit : MonoBehaviour
{
    [Header("Enemy Setup")]
    public string enemyTag = "Enemy";

    [Header("Right-hand Haptics (HapticImpulsePlayer)")]
    public HapticImpulsePlayer rightHandHaptics;
    public float hapticAmplitude = 1f;   // max intensity
    public float hapticDuration = 0.12f; // a bit longer

    [Header("Knockback")]
    public float knockbackForce = 25f;   // strong
    public float upForce = 3f;           // nice lift

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"[BatHit] Collision with: {collision.collider.name}");

        // 1) Are we even hitting the enemy tag?
        if (!collision.collider.CompareTag(enemyTag))
        {
            Debug.Log("[BatHit] Not enemy tag, ignoring.");
            return;
        }

        Debug.Log("[BatHit] Hit ENEMY!");

        // 2) HAPTIC
        if (rightHandHaptics != null)
        {
            bool ok = rightHandHaptics.SendHapticImpulse(hapticAmplitude, hapticDuration);
            Debug.Log($"[BatHit] Sent haptic: {ok}");
        }
        else
        {
            Debug.LogWarning("[BatHit] rightHandHaptics is NOT assigned!");
        }

        // 3) KNOCKBACK
        // Try to find a rigidbody on the enemy or its parent
        Rigidbody rb = collision.rigidbody;
        if (rb == null)
            rb = collision.collider.GetComponentInParent<Rigidbody>();

        if (rb != null)
        {
            // Direction away from bat + upward
            Vector3 dir = collision.transform.position - transform.position;
            dir.y += upForce;
            dir.Normalize();

            rb.AddForce(dir * knockbackForce, ForceMode.Impulse);
            Debug.Log("[BatHit] Knockback applied!");
        }
        else
        {
            Debug.LogWarning("[BatHit] No Rigidbody found on enemy or its parents!");
        }
    }
}