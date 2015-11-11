using UnityEngine;
using UnityEditor;
using System.Collections;

[ExecuteInEditMode]
public class vertObject : MonoBehaviour
{
    Vector3 baseScale;

    void Start()
    {
        baseScale = transform.localScale;
        setTransform();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnRenderObject()
    {
        setTransform();
    }

    void setTransform()
    {
        if (Camera.current)
        {
            transform.LookAt(Camera.current.transform.position, Vector3.up);
            float distance = (transform.position - Camera.current.transform.position).magnitude;
            transform.localScale = baseScale * distance;
            if (transform.localScale.x > 2)
            {
                transform.localScale = baseScale * 2;
            }
        }
    }
}
