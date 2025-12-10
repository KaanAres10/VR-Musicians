using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 3f;
    public float jumpTriggerDistance = 3f;
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
    public float damageInterval = 0.3f;   // hit every 0.3 sec
    public float damageAmount = 3;          // 3 damage each hit
    private float damageTimer = 0f;
    
    [Header("Music / Genre")]
    public TrackGenreReader trackReader; 

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

        // ---------- STATE LOGIC ----------
        if (dist <= idleTriggerDistance)
        {
            // VERY CLOSE -> IDLE (after jump)
            if (!isIdle)
            {
                isIdle   = true;
                isJumping = false;

                animator.SetBool("IsIdle", true);
                animator.SetBool("IsJumping", true);
                animator.SetBool("IsWalking", false);
            }
        }
        else if (dist <= jumpTriggerDistance)
        {
            // CLOSE -> JUMP
            if (!isJumping)
            {
                isJumping = true;
                isIdle    = false;

                animator.SetBool("IsJumping", true);
                animator.SetBool("IsIdle", false);
                animator.SetBool("IsWalking", false);
            }
        }
        else
        {
            // FAR -> WALK
            if (isIdle || isJumping)
            {
                isIdle    = false;
                isJumping = false;

                animator.SetBool("IsIdle", false);
                animator.SetBool("IsJumping", false);
                animator.SetBool("IsWalking", true);
            }
        }

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
            Debug.Log(damageAmount);
        }
        else
        {
            damageAmount = 3;
            Debug.Log(damageAmount);

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
            animator.SetTrigger("Die");
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
            GameManager.Instance.AddScore(1);
            Die();
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

