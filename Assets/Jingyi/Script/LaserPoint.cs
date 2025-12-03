using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class LaserPoint : MonoBehaviour
{
    [Header("Crosshair")]
    public Transform crosshair;

    [Header("Ray Settings")]
    public float maxDistance = 50f;
    public LayerMask hitLayer;

    [Header("Line Settings")]
    public Color lineColor = Color.red;
    public float lineWidth = 0.01f;

    private LineRenderer line;

    void Awake()
    {
        
        line = GetComponent<LineRenderer>();
        line.positionCount = 2;
        line.startWidth = lineWidth;
        line.endWidth = lineWidth;
        line.material = new Material(Shader.Find("Unlit/Color"));
        line.material.color = lineColor;
    }

    void Update()
    {
        
        Vector3 startPos = transform.position;
        line.SetPosition(0, startPos);

        Ray ray = new Ray(startPos, transform.forward);
        RaycastHit hit;

        Vector3 endPos;

       
        if (Physics.Raycast(ray, out hit, maxDistance, hitLayer))
        {
            endPos = hit.point;

           
            if (crosshair != null)
            {
                crosshair.position = hit.point;
                crosshair.rotation = Quaternion.LookRotation(hit.normal);
            }
        }
        else
        {
            endPos = startPos + transform.forward * maxDistance;

            if (crosshair != null)
            {
                crosshair.position = endPos;
                crosshair.rotation = Quaternion.LookRotation(-transform.forward);
            }
        }

        
        line.SetPosition(1, endPos);
    }
}
