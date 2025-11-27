using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine;

public class TwoHandsControl : MonoBehaviour
{
    private XRGrabInteractable grabInteractable;
    private XRBaseInteractor secondaryInteractor;

    public Transform secondaryAttachPoint;

    void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        grabInteractable.selectEntered.AddListener(OnGrab);
        grabInteractable.selectExited.AddListener(OnRelease);
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        XRBaseInteractor interactor = args.interactorObject as XRBaseInteractor;

        // decide whether it's the second hand
        if (secondaryInteractor == null && interactor != grabInteractable.firstInteractorSelecting)
        {
            SetSecondaryInteractor(interactor);
        }
    }

    public void SetSecondaryInteractor(XRBaseInteractor interactor)
    {
        secondaryInteractor = interactor;
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        XRBaseInteractor interactor = args.interactorObject as XRBaseInteractor;

        if (interactor == secondaryInteractor)
            secondaryInteractor = null;
    }

    void Update()
    {
        if (secondaryInteractor != null)
            UpdateTwoHandRotation();
    }

    private void UpdateTwoHandRotation()
    {
        Vector3 direction = secondaryInteractor.attachTransform.position - grabInteractable.attachTransform.position;
        if (direction.sqrMagnitude > 0.001f)
        {
            grabInteractable.transform.rotation = Quaternion.LookRotation(direction);
        }
    }
}
