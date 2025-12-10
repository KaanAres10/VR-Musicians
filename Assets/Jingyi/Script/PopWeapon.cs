using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(Rigidbody), typeof(XRGrabInteractable))]
public class PopWeapon : MonoBehaviour
{
    [Header("Shoot Force Settings")]
    public float minShootForce = 6f;
    public float maxShootForce = 20f;
    public AnimationCurve forceCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Header("Timing Bar Settings")]
    public float barCycleDuration = 1.5f;

    [Header("Color Gradient")]
    public Gradient barGradient;

    [Header("UI References")]
    public Slider timingBar;
    public Image barFillImage;

    [Header("Lifetime After Release")]
    public float destroyAfterReleaseTime = 5f;

    [Header("Rotation")]
    public float rotateSpeed = 180f;
    public Transform visualToRotate;

    private XRGrabInteractable grabInteractable;
    private Rigidbody rb;

    private bool isGrabbed = false;
    private bool isReleased = false;
    private float releaseTimer = 0f;

    private float currentValue = 0f;
    private float cycleTimer = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        grabInteractable = GetComponent<XRGrabInteractable>();

        grabInteractable.selectEntered.AddListener(OnGrab);
        grabInteractable.selectExited.AddListener(OnRelease);

        if (visualToRotate == null)
            visualToRotate = transform;

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
        // -------------------------
        // DESTROY AFTER RELEASE
        // -------------------------
        if (isReleased)
        {
            releaseTimer += Time.deltaTime;
            if (releaseTimer >= destroyAfterReleaseTime)
            {
                Debug.Log("[PopWeapon] Destroyed after release timer.");
                Destroy(gameObject);
                return;
            }
        }

        // -------------------------
        // ONLY UPDATE LOGIC IF GRABBED
        // -------------------------
        if (!isGrabbed)
            return;

        // Rotate while held
        if (visualToRotate != null)
            visualToRotate.Rotate(Vector3.up * rotateSpeed * Time.deltaTime, Space.Self);

        // Ping-pong fill meter
        if (barCycleDuration > 0.0001f)
        {
            cycleTimer += Time.deltaTime * (6f / barCycleDuration);
            currentValue = Mathf.PingPong(cycleTimer, 3f);
        }
        else
        {
            currentValue = 1.5f;
        }

        UpdateBarVisual();
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        isGrabbed = true;
        isReleased = false;
        releaseTimer = 0f;

        cycleTimer = 0f;
        currentValue = 0f;

        rb.isKinematic = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        UpdateBarVisual();
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        isGrabbed = false;
        isReleased = true;     // Start destroy countdown
        releaseTimer = 0f;

        // Compute throw force
        float normalizedTime = Mathf.InverseLerp(0f, 3f, currentValue);
        float curveT = forceCurve.Evaluate(normalizedTime);
        float chosenForce = Mathf.Lerp(minShootForce, maxShootForce, curveT);

        rb.isKinematic = false;

        Transform hand = args.interactorObject.transform;
        rb.linearVelocity = hand.forward * chosenForce;

        Debug.Log($"[PopWeapon] Released with force {chosenForce}, will destroy in {destroyAfterReleaseTime}s");
    }

    private void UpdateBarVisual()
    {
        if (timingBar != null)
            timingBar.value = currentValue;

        if (barFillImage != null)
        {
            float normalizedTime = Mathf.InverseLerp(0f, 3f, currentValue);
            barFillImage.color = barGradient.Evaluate(normalizedTime);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            GameManager.Instance.AddScore(1);
            Debug.Log("Hit enemy");
            Destroy(other.gameObject);
        }
    }
}
