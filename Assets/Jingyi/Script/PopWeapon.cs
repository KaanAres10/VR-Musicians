using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables; // XRGrabInteractable (depending on XRITK version)

[RequireComponent(typeof(Rigidbody), typeof(XRGrabInteractable))]
public class PopWeapon : MonoBehaviour
{
    [Header("Shoot Force Settings")]
    [Tooltip("Force when timing value is at minimum (0).")]
    public float minShootForce = 6f;

    [Tooltip("Force when timing value is at maximum (3).")]
    public float maxShootForce = 20f;

    [Tooltip("Curve to shape how force grows from 0→1. X = normalized time (0–1), Y = normalized force (0–1).")]
    public AnimationCurve forceCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Header("Timing Bar Settings")]
    [Tooltip("Time (seconds) for a full 0→3→0 ping-pong cycle.")]
    public float barCycleDuration = 1.5f;

    [Header("Color Gradient")]
    [Tooltip("Gradient evaluated from 0–1 (mapped from 0–3 timing).")]
    public Gradient barGradient;

    [Header("UI References")]
    [Tooltip("Slider that shows the timing bar (World Space Canvas).")]
    public Slider timingBar;

    [Tooltip("Fill Image of the bar to color it (e.g. the Slider's Fill image).")]
    public Image barFillImage;

    private XRGrabInteractable grabInteractable;
    private Rigidbody rb;

    private bool isGrabbed = false;
    // Now represents the raw timing in [0, 3].
    private float currentValue = 0f;
    private float cycleTimer = 0f;     // internal time for PingPong

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        grabInteractable = GetComponent<XRGrabInteractable>();

        grabInteractable.selectEntered.AddListener(OnGrab);
        grabInteractable.selectExited.AddListener(OnRelease);

        // Init UI if present
        if (timingBar != null)
        {
            timingBar.minValue = 0f;
            timingBar.maxValue = 3f;
            timingBar.value = 0f;
        }

        UpdateBarVisual();
    }

    private void OnDestroy()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrab);
            grabInteractable.selectExited.RemoveListener(OnRelease);
        }
    }

    private void Update()
    {
        if (!isGrabbed)
            return;

        if (barCycleDuration > 0.0001f)
        {
            
            cycleTimer += Time.deltaTime * (6f / barCycleDuration);

            // 0→3→0 repeatedly
            float t = Mathf.PingPong(cycleTimer, 3f);
            currentValue = t; // store 0–3
        }
        else
        {
            currentValue = 1.5f; // middle of 0–3
        }

        UpdateBarVisual();
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        isGrabbed = true;

        // Reset timer each grab
        cycleTimer = 0f;
        currentValue = 0f;
        UpdateBarVisual();

        // Disable physics while held
        rb.isKinematic = true;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        isGrabbed = false;

        // Normalize timing 0–3 → 0–1
        float normalizedTime = Mathf.InverseLerp(0f, 3f, currentValue);

        // Evaluate curve (shape) then remap to force range
        float curveT = forceCurve.Evaluate(normalizedTime);          // 0–1
        float chosenForce = Mathf.Lerp(minShootForce, maxShootForce, curveT);

        Debug.Log($"[PopWeapon] Release: raw={currentValue:F2}, norm={normalizedTime:F2}, curve={curveT:F2}, force={chosenForce:F2}");

        // Re-enable physics
        rb.isKinematic = false;

        // Shoot in the released hand's forward direction
        Transform hand = args.interactorObject.transform;
        rb.velocity = hand.forward * chosenForce;
    }


    private void UpdateBarVisual()
    {
        // Slider value (0–3)
        if (timingBar != null)
            timingBar.value = currentValue;

        if (barFillImage == null)
            return;

        // Normalize to 0–1 for gradient
        float normalizedTime = Mathf.InverseLerp(0f, 3f, currentValue);

        if (barGradient != null)
        {
            Color c = barGradient.Evaluate(normalizedTime);
            barFillImage.color = c;
        }
    }

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
