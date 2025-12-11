using UnityEngine;
using UnityEngine.InputSystem;   // XR input system
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

public class RockWeapon : MonoBehaviour
{
    [Header("Input (XR Trigger)")]
    public InputActionProperty triggerAction;   // Assign RightHand -> Interaction -> Activate

    [Header("Raycast Shooting")]
    public Transform laserPoint;
    public float fireRate = 0.1f;
    public float maxDistance = 50f;
    public LayerMask hitMask;           // Set in Inspector (e.g. Everything)
    public string enemyTag = "Enemy";   // Tag used on enemy objects

    private float nextFireTime;

    [Header("Laser Line")]
    public LineRenderer lineRenderer;
    public Color defaultColor = Color.red;
    public Color hitColor = Color.green;

    [Header("Sound")]
    public AudioSource gunAudio;

    [Header("VFX")]
    public GameObject muzzleFlashPrefab;

    [Header("Enemy Hit VFX (Random Pick)")]
    public GameObject[] enemyHitVfxPrefabs;  

    [Header("Haptics (Right Hand)")]
    public HapticImpulsePlayer rightHandHaptics;  // right controller's HapticImpulsePlayer
    public float hapticAmplitude = 1f;
    public float hapticDuration = 0.12f;

    [Header("Knockback")]
    public float knockbackForce = 25f;
    public float upForce = 3f;

    private void Start()
    {
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 2;
            lineRenderer.enabled = true;    // always visible
        }

        // Optional: auto-find right hand haptics if not set
        if (rightHandHaptics == null)
        {
            var rightHand = GameObject.Find("RightHand Controller");
            if (rightHand != null)
                rightHandHaptics = rightHand.GetComponent<HapticImpulsePlayer>();
        }
    }

    private void Update()
    {
        // --- Always cast ray from laserPoint ---
        Ray ray = new Ray(laserPoint.position, laserPoint.forward);
        RaycastHit hit;
        Vector3 endPoint;
        bool hitSomething = Physics.Raycast(ray, out hit, maxDistance, hitMask);
        bool hitEnemy = false;

        if (hitSomething)
        {
            endPoint = hit.point;
            hitEnemy = hit.collider.CompareTag(enemyTag);
        }
        else
        {
            endPoint = laserPoint.position + laserPoint.forward * maxDistance;
        }

        // --- Always show laser line ---
        if (lineRenderer != null)
        {
            lineRenderer.SetPosition(0, laserPoint.position);
            lineRenderer.SetPosition(1, endPoint);

            Color c = hitEnemy ? hitColor : defaultColor;

            if (hitEnemy)
            {
                Debug.Log("Im here");
            }

            lineRenderer.startColor = c;
            lineRenderer.endColor = c;

            // Also update material color (URP/HDRP often uses this instead)
            if (lineRenderer.material != null)
            {
                if (lineRenderer.material.HasProperty("_BaseColor"))
                {
                    lineRenderer.material.SetColor("_BaseColor", c);   // URP Unlit/ Lit
                }
                else if (lineRenderer.material.HasProperty("_Color"))
                {
                    lineRenderer.material.color = c;                   // Standard shader
                }
            }
        }

        // --- Shooting (damage / VFX) only when holding trigger ---
        bool triggerHeld = triggerAction.action.ReadValue<float>() > 0.1f;

        if (triggerHeld && Time.time >= nextFireTime)
        {
            Shoot(hitSomething, hitSomething ? hit.collider : null, hitSomething ? hit.point : (laserPoint.position + laserPoint.forward * maxDistance));
            nextFireTime = Time.time + fireRate;
        }
    }

    private void Shoot(bool hitSomething, Collider hitCollider, Vector3 hitPoint)
    {
        // Damage enemy ONLY if ray is on an enemy when you fire
        bool hitEnemy = hitSomething && hitCollider != null && hitCollider.CompareTag(enemyTag);

        if (hitEnemy)
        {
            // Get rigidbody once (attachedRigidbody handles child colliders nicely)
            Rigidbody rb = hitCollider.attachedRigidbody;

            // 1) Enemy hit VFX
            if (enemyHitVfxPrefabs != null && enemyHitVfxPrefabs.Length > 0)
            {
                int index = Random.Range(0, enemyHitVfxPrefabs.Length);
                GameObject chosenVfx = enemyHitVfxPrefabs[index];

                if (chosenVfx != null)
                {
                    Quaternion rot = Quaternion.LookRotation(hitPoint - laserPoint.position);
                    GameObject vfx = Instantiate(chosenVfx, hitPoint, rot);
                    Destroy(vfx, 2f);
                }
            }

            // 2) Score
            GameManager.Instance.AddScore(1);

            // 3) Haptics (simple strong pulse – raycast doesn’t really have "velocity")
            if (rightHandHaptics != null)
            {
                Debug.Log("here");
                if (rightHandHaptics.SendHapticImpulse(hapticAmplitude, hapticDuration))
                {
                    Debug.Log("Haptic Impulse");
                }
            }

            // 4) Knockback BEFORE destroy (if enemy has Rigidbody)
            if (rb != null)
            {
                // Direction from gun to enemy, with upward lift
                Vector3 dir = (hitCollider.transform.position - laserPoint.position).normalized;
                dir.y += upForce;
                dir.Normalize();

                rb.AddForce(dir * knockbackForce, ForceMode.Impulse);
            }

            // 5) Destroy enemy after effects
            Destroy(hitCollider.gameObject);
        }

        // Sound
        if (gunAudio != null)
        {
            gunAudio.Play();
        }
        else
        {
            Debug.Log("No audio source found");
        }

        // Muzzle VFX at the gun
        if (muzzleFlashPrefab != null)
        {
            GameObject vfx = Instantiate(muzzleFlashPrefab, laserPoint);
            vfx.transform.localPosition = Vector3.zero;
            vfx.transform.localRotation = Quaternion.Euler(0f, 270f, 0f); 
            Destroy(vfx, 0.1f);
        }
    }
}
