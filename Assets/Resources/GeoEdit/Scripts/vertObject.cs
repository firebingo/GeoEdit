using UnityEngine;
using UnityEditor;
using System.Collections;

[ExecuteInEditMode]
public class vertObject : MonoBehaviour
{
    Vector3 baseScale;
    bool selected = false;
    bool selectFlip = false; //used so it doesnt get and set sprites color every frane.

    void Start()
    {
        baseScale = new Vector3(0.12f, 0.12f, 0.12f);
        setTransform();
    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnRenderObject()
    {
        setTransform();
        if (!selectFlip)
        {
            if (!selected)
            {
                GetComponent<SpriteRenderer>().color = Color.blue;
                selectFlip = true;
            }
            else
            {
                GetComponent<SpriteRenderer>().color = Color.red;
                selectFlip = true;
            }
        }

        foreach (Transform transform in Selection.transforms)
        {
            if (transform == this.transform)
                setSelected(true);
            else
                setSelected(false);
        }
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

    void setSelected(bool i)
    {
        selected = i;
        selectFlip = false;
    }
}
