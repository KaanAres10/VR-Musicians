using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 40f;
  
    public float lifeTime = 3f;

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    
}
