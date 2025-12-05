using UnityEngine;

public class PlayerOnGround : MonoBehaviour
{
    public Transform player;              // XR Rig root
    public float fallSpeed = 2f;          // Smooth fall speed
    public float rayDistance = 5f;        // How far we check below player
    public string floorTag = "Floor";
    public float heightOffset = 0.1f;     // how high above the floor to stay

    private void Update()
    {
        if (player == null) return;

        RaycastHit hit;

        // Raycast downward to detect floor
        bool hitSomething = Physics.Raycast(
            player.position,
            Vector3.down,
            out hit,
            rayDistance
        );

        if (hitSomething && hit.collider.CompareTag(floorTag))
        {
            // Where we WANT to be on Y (just above floor)
            float targetY = hit.point.y + heightOffset;
            float currentY = player.position.y;

            // Only move if we're higher than target
            if (currentY > targetY + 0.01f)
            {
                float newY = Mathf.MoveTowards(
                    currentY,
                    targetY,
                    fallSpeed * Time.deltaTime
                );

                player.position = new Vector3(
                    player.position.x,
                    newY,
                    player.position.z
                );
            }

            // If already close enough to floor, do nothing
            return;
        }

        // If there is NO floor under us at all (off a cliff etc.)
        // you can choose to let the player keep falling:
        player.position += Vector3.down * fallSpeed * Time.deltaTime;
    }
}