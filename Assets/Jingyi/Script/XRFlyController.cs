using UnityEngine;
using UnityEngine.InputSystem;

public class VRFlyController : MonoBehaviour
{
    [Header("Input Actions")]
    public InputActionProperty flyUpAction;     // Button for going up
    public InputActionProperty flyDownAction;   // Button for going down

    [Header("Settings")]
    public float flySpeed = 2f;

    [Header("Player Root (XR Rig)")]
    public Transform player;  // Assign your XR Rig or Camera Offset

    private void Update()
    {
        if (player == null) return;

        float vertical = 0f;

        if (flyUpAction.action.IsPressed())
            vertical += 1f;

        if (flyDownAction.action.IsPressed())   
            vertical -= 1f;

        if (vertical != 0f)
        {
            player.transform.position += Vector3.up * vertical * flySpeed * Time.deltaTime;
        }
    }
}