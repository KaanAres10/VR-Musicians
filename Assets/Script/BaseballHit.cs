using UnityEngine;

public class BaseballHit : MonoBehaviour
{
  
    public float hitForce = 50f; 
    
    public float upwardForce = 10f;

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            Rigidbody enemyRb = collision.gameObject.GetComponent<Rigidbody>();

            if (enemyRb != null)
            {
                
                enemyRb.constraints = RigidbodyConstraints.None;
                
                enemyRb.isKinematic = false;

              
                Vector3 direction = (collision.transform.position - transform.position).normalized;
                
               
                Vector3 horizontalPush = direction * hitForce;
                
       
                Vector3 verticalLift = Vector3.up * upwardForce;

      
                enemyRb.AddForce(horizontalPush + verticalLift, ForceMode.Impulse);
            }
        }
    }
}