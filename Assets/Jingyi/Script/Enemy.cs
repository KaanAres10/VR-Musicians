using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float speed = 3f;  
     Transform player;

    void Awake()
    {
        GameObject target = GameObject.FindWithTag("Player");
        if (target != null)
        {
            player = target.transform;
        }
        else
        {
            Debug.LogError("No GameObject with tag 'Player' found in the scene!");
        }
    }


    void Update()
    {
        if (player == null) return;

        Vector3 dir = (player.position - transform.position).normalized;
        transform.position += dir * speed * Time.deltaTime;
    }

 
    public void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
    }

    // take damage
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("1 health damage");
            other.GetComponent<Player>().TakeDamage(1);
            
        }

        if (other.CompareTag("PlayerBat"))
        {

            GameManager.Instance.AddScore(1);

            Debug.Log("hit enemy");
            Destroy(gameObject);
        }

        if (other.CompareTag("Bullet"))
        {

            GameManager.Instance.AddScore(1);

            Debug.Log("hit enemy");
            Destroy(gameObject);
        }
    }


}
