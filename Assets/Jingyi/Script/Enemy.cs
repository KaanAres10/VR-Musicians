using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
// for HapticImpulsePlayer


public class Enemy : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 3f;
    public float jumpTriggerDistance = 6f;
    public float idleTriggerDistance = 1.2f; // VERY close to player (must be < jumpTriggerDistance)
    public float deathDelay = 1.0f;
    public float rotationSpeed = 5f;

    private Transform player;
    public Animator animator;

    private bool isDead = false;
    private bool isJumping = false;
    private bool isIdle = false;
    
    private Rigidbody rb;
    
    [Header("Attack")]
    public float damageInterval = 1.0f;   // hit every 0.3 sec
    public float damageAmount = 3;          // 3 damage each hit
    private float damageTimer = 0f;
    
    [Header("Music / Genre")]
    public TrackGenreReader trackReader; 
    
    [Header("Hit Settings")]
    public string batTag = "PlayerBat";

    [Header("Haptics (Right Hand)")]
    public HapticImpulsePlayer rightHandHaptics;  // drag right controller's HapticImpulsePlayer
    public float hapticAmplitude = 1f;
    public float hapticDuration = 0.12f;

    [Header("Knockback")]
    public float knockbackForce = 25f;
    public float upForce = 3f;




    void Awake()
    {
        GameObject target = GameObject.FindWithTag("Player");
    
        rb = GetComponent<Rigidbody>();
    

        if (target != null)
        {
            player = target.transform;
        }
        else
        {
            Debug.LogError("No GameObject with tag 'Player' found in the scene!");
        }
        
        if (trackReader == null)
        {
            trackReader = FindObjectOfType<TrackGenreReader>();
            if (trackReader == null)
            {
                Debug.LogError("[Enemy] No TrackGenreReader found in scene!");
            }
        }

        if (animator == null)
        {
            Debug.LogError("Enemy has no Animator component!");
        }
    }

    void Start()
    {
        if (animator != null)
        {
            animator.SetBool("IsWalking", true);
            animator.SetBool("IsJumping", false);
            animator.SetBool("IsIdle", false);
        }
        
        if (rightHandHaptics == null)
        {
            // Look for the RightHand controller in the scene
            var rightHand = GameObject.Find("Right Controller");

            if (rightHand != null)
            {
                rightHandHaptics = rightHand.GetComponent<HapticImpulsePlayer>();
            }

            if (rightHandHaptics == null)
            {
                Debug.LogWarning($"Enemy {name}: Could not find RightHand HapticImpulsePlayer!");
            }
        }
    }
    

    void Update()
    {
        if (isDead || player == null) return;

        // Rotate to face player on XZ
        Vector3 lookDir = player.position - transform.position;
        lookDir.y = 0f;
        if (lookDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(lookDir);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                rotationSpeed * Time.deltaTime
            );
        }

        float dist = Vector3.Distance(transform.position, player.position);

        
        // Move only when not idle
        if (!isIdle)
        {
            Vector3 newPos = transform.position + transform.forward * speed * Time.deltaTime;
            rb.MovePosition(newPos);   // ← Proper physics movement
        }
        
        MusicGenre currentGenre = trackReader.getCurrentGenre();
        if (currentGenre == MusicGenre.Classic || currentGenre == MusicGenre.Country)
        {
            damageAmount = 0.1f;
        }
        else
        {
            damageAmount = 2.5f;

        }

        
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        if (animator != null)
        {
            animator.SetBool("IsWalking", false);
            animator.SetBool("IsJumping", false);
            animator.SetBool("IsIdle", false);
        }

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Destroy(gameObject, deathDelay);
    }

    public void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
    }
    
    void OnCollisionEnter(Collision col)
    {
        if (isDead) return;

        if (col.collider.CompareTag("PopWeapon") || col.collider.CompareTag("PlayerBat"))
        {
            Debug.Log("Bat Hit");
            GameManager.Instance.AddScore(1);
            Die();
            
            if (rightHandHaptics != null)
            {
                float strength = Mathf.Clamp01(col.relativeVelocity.magnitude / 5f);
                rightHandHaptics.SendHapticImpulse(hapticAmplitude * strength, hapticDuration);
            }
            
            if (rb != null)
            {
                // from bat to enemy
                Vector3 dir = transform.position - col.transform.position;

                dir.y += upForce;     // give it some upward arc
                dir.Normalize();

                rb.AddForce(dir * knockbackForce, ForceMode.Impulse);
            }
        }
    }
    
    void OnCollisionExit(Collision col)
    {
        if (col.collider.CompareTag("Player"))
        {
            damageTimer = 0f; // reset, so hit is instant next time you touch
        }
    }
    

    void OnCollisionStay(Collision col)
    {
        if (isDead) return;
        
        if (col.collider.CompareTag("Player"))
        {
            damageTimer -= Time.deltaTime;

            if (damageTimer <= 0f)
            {
                Player p = col.collider.GetComponentInParent<Player>(); // safer
                if (p != null)
                    p.TakeDamage(damageAmount);

                damageTimer = damageInterval;
            }
        }
    }
}

