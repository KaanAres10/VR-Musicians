using UnityEngine;

public class PlayerScore : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            
            GameManager.Instance.AddScore(1);

            Debug.Log("hit enemy");
            Destroy(other.gameObject);
        }
    }
}
