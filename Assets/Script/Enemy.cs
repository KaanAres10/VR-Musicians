using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float speed = 3f;  
    Transform player;

    void Start()
    {
        player = GameObject.FindWithTag("Player").transform;
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
}
