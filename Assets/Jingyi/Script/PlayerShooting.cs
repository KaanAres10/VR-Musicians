using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    public Transform shootPoint;
    public GameObject bulletPrefab;
    public float fireRate = 0.2f;

    float nextFireTime;

    private void Update()
    {
        if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + fireRate;
        }

        void Shoot()
        {
            Instantiate(bulletPrefab, shootPoint.position, shootPoint.rotation);
        }

    }
}
