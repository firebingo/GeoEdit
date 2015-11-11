using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

public class GeoEdit : MonoBehaviour
{
    bool geoEnabled;

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
    }
}

public class GeoEditWindow : EditorWindow
{
    bool enableGeoEdit;
    GameObject vertObjectPrefab = Resources.Load("GeoEdit/Objects/vertObject") as GameObject;
    List<GameObject> vertObjects = new List<GameObject>();
    GameObject selectedObject = null;
    GameObject parent = null;
    GameObject lastObject = null;

    [MenuItem("Window/GeoEdit")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(GeoEditWindow));
    }

    void OnGUI()
    {
        enableGeoEdit = EditorGUILayout.Toggle("Toggle", enableGeoEdit);
    }

    void Update()
    {

        if (enableGeoEdit)
        {
            if(!parent)
                parent = new GameObject("vertParent");

            if(!selectedObject && Selection.activeGameObject)
            {
                selectedObject = Selection.activeGameObject;
            }

            if (selectedObject)
            {
                if (Selection.activeGameObject && lastObject != selectedObject)
                {
                    MeshFilter objectMesh = selectedObject.GetComponent<MeshFilter>();
                    if (objectMesh)
                    {
                        Vector3[] verts = objectMesh.sharedMesh.vertices;
                        
                        for (int i = 0; i < verts.Length; ++i)
                        {
                            Vector3 scaledPos = selectedObject.transform.TransformPoint(verts[i]);

                            vertObjects.Add((GameObject)Instantiate(vertObjectPrefab, scaledPos, new Quaternion()));
                            vertObjects[i].transform.parent = parent.transform;
                        }
                    }
                    lastObject = selectedObject;
                }
                else if(!Selection.activeGameObject || lastObject != Selection.activeGameObject)
                {
                    cleanObjects();
                }
            }

            
        }
        else
        {
            cleanObjects();
            if (parent)
                GameObject.DestroyImmediate(parent);
        }
    }

    void cleanObjects()
    {
        if (vertObjects.Count > 0)
        {
            for (int i = 0; i < vertObjects.Count; ++i)
            {
                GameObject.DestroyImmediate(vertObjects[i]);
                vertObjects[i] = null;
            }
        }
        vertObjects = new List<GameObject>();
        selectedObject = null;
    }

    void OnDestroy()
    {
        cleanObjects();
        if (parent)
            GameObject.DestroyImmediate(parent);
    }
}