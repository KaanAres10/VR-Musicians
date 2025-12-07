using System.Collections.Generic;
using UnityEngine;

public class MonsterDissolve : MonoBehaviour
{
    [Tooltip("How long the dissolve takes (seconds).")]
    public float dissolveDuration = 1.5f;

    [Tooltip("Name of the float property in your shader.")]
    public string dissolvePropertyName = "_DissolveAmount";

    private readonly List<Material> _materials = new List<Material>();
    private bool _isDissolving = false;
    private float _t = 0f;

    void Awake()
    {
        var renderers = GetComponentsInChildren<Renderer>();

        foreach (var r in renderers)
        {
            foreach (var mat in r.materials)
            {
                if (!_materials.Contains(mat))
                {
                    _materials.Add(mat);
                    // Start completely visible
                    if (mat.HasProperty(dissolvePropertyName))
                        mat.SetFloat(dissolvePropertyName, 0f);
                }
            }
        }
    }

    void Start()
    {
        // AUTO-TEST: Start dissolve after 0.5 seconds
        Invoke(nameof(StartDissolve), 0.5f);
    }

    public void StartDissolve()
    {
        if (_materials.Count == 0) return;
        
        Debug.Log("StartDissolve called on " + gameObject.name);

        _isDissolving = true;
        _t = 0f;
    }

    void Update()
    {
        if (!_isDissolving) return;

        _t += Time.deltaTime / dissolveDuration;
        float value = Mathf.Clamp01(_t);

        foreach (var mat in _materials)
        {
            if (mat != null && mat.HasProperty(dissolvePropertyName))
                mat.SetFloat(dissolvePropertyName, value);
        }

        if (value >= 1f)
        {
            Destroy(gameObject);
        }
    }
}
