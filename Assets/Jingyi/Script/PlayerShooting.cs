using UnityEngine;
using UnityEngine.InputSystem;   // XR input system

public class PlayerShooting : MonoBehaviour
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
    public GameObject enemyHitVfxPrefab; 

    private void Start()
    {
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 2;
            lineRenderer.enabled = true;    // always visible
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
            Shoot(hitSomething, hitEnemy ? hit.collider : null, endPoint);
            nextFireTime = Time.time + fireRate;
        }
    }

    private void Shoot(bool hitSomething, Collider hitCollider, Vector3 hitPoint)
    {
        // Damage enemy ONLY if ray is on an enemy when you fire
        bool hitEnemy = hitSomething && hitCollider != null && hitCollider.CompareTag(enemyTag);

        // Damage enemy ONLY if ray is on an enemy when you fire
        if (hitEnemy)
        {
            // Spawn hit VFX on enemy
            if (enemyHitVfxPrefab != null)
            {
                // Align with surface normal so sparks fly out correctly
                Quaternion rot = Quaternion.LookRotation(hitPoint - laserPoint.position);
                GameObject hitVfx = Instantiate(enemyHitVfxPrefab, hitPoint, rot);
                Destroy(hitVfx, 2f);
                GameManager.Instance.AddScore(1);
            }

            // If you have health script, call that here instead of Destroy:
            // hitCollider.GetComponent<EnemyHealth>()?.TakeDamage(1);
            Destroy(hitCollider.gameObject);
        }

        // Sound
        if (gunAudio != null)
            gunAudio.Play();

        // Muzzle VFX at the gun
        if (muzzleFlashPrefab != null)
        {
            GameObject vfx = Instantiate(muzzleFlashPrefab, laserPoint);
            vfx.transform.localPosition = Vector3.zero;
            vfx.transform.localRotation = Quaternion.Euler(0f, 270f, 0f); // or Vector3.zero if prefab fixed
            Destroy(vfx, 0.1f);
        }


        // (Optional) impact VFX at hit point:
        // if (hitSomething && impactVfxPrefab != null)
        // {
        //     Instantiate(impactVfxPrefab, hitPoint, Quaternion.identity);
        // }
    }
}
