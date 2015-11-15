using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

//public class GeoEdit : MonoBehaviour
//{
//    bool geoEnabled;

//    // Use this for initialization
//    void Start()
//    {

//    }

//    // Update is called once per frame
//    void Update()
//    {
//    }
//}

public class GeoEditWindow : EditorWindow
{
    bool enableGeoEdit;
    GameObject vertObjectPrefab = Resources.Load("GeoEdit/Objects/vertObject") as GameObject;
    List<GameObject> vertObjects = new List<GameObject>();
    GameObject selectedObject = null;
    GameObject vertParent = null;
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
        //if geoedit is disabled
        if (enableGeoEdit)
        {
            //if the vertex parent object doenst exist create it.
            if(!vertParent)
                vertParent = new GameObject("vertParent");

            //if there is a object selected in the scene but no selectedObject set it.
            //and set the vertex parent's parent to the selected object.
            if(!selectedObject && Selection.activeGameObject)
            {
                selectedObject = Selection.activeGameObject;
                vertParent.transform.parent = selectedObject.transform;
            }
            //if there is a selected object.
            if (selectedObject)
            {
                //if there is a object selected in the scene and the last object selected isint the selected object.
                //this check is so this only happens once. Might be able to do it with a bool if it isin't used later.
                if (Selection.activeGameObject && lastObject != selectedObject)
                {   
                    //get the mesh of the selected object.
                    MeshFilter objectMesh = selectedObject.GetComponent<MeshFilter>();
                    //if the mesh was set properly.
                    if (objectMesh)
                    {
                        //create a array of all the positions of the meshes' vertices.
                        Vector3[] verts = objectMesh.sharedMesh.vertices;

                        //create all the vertex objects and set their positions to the meshes vertices.
                        for (int i = 0; i < verts.Length; ++i)
                        {
                            //need to get a world space scale so vertex objects are placed properly on a scaled object.
                            Vector3 scaledPos = selectedObject.transform.TransformPoint(verts[i]);

                            vertObjects.Add((GameObject)Instantiate(vertObjectPrefab, scaledPos, new Quaternion()));
                            vertObjects[i].transform.parent = vertParent.transform;
                        }
                    }
                    lastObject = selectedObject;
                }
                //if there is no object selected in the scene or the currently selected object in the scene isint the last
                // object selected clean up everything.
                else if(!Selection.activeGameObject || lastObject != Selection.activeGameObject)
                {
                    //don't clean up if the current object selected is one of the vertex objects of the last selected object.
                    if (!(Selection.activeGameObject
                        && Selection.activeGameObject.transform.parent && Selection.activeGameObject.transform.parent.transform.parent //ensure that the object selected is a vertex.
                        && Selection.activeGameObject.transform.parent.transform.parent == vertParent.transform.parent))
                    {
                        cleanObjects();
                    }
                }
            }
        }
        //if geoedit is disabled
        else
        {
            cleanObjects();
            if (vertParent)
                GameObject.DestroyImmediate(vertParent);
        }
    }

    //clean up the scene
    void cleanObjects()
    {
        //clean all vertex objects
        if (vertObjects.Count > 0)
        {
            for (int i = 0; i < vertObjects.Count; ++i)
            {
                GameObject.DestroyImmediate(vertObjects[i]);
                vertObjects[i] = null;
            }
        }
        //reintilize the list
        vertObjects = new List<GameObject>();
        //set selected objects to null
        selectedObject = null;
        lastObject = null;
    }

    //clean everything if the window is closed.
    void OnDestroy()
    {
        cleanObjects();
        if (vertParent)
            GameObject.DestroyImmediate(vertParent);
    }
}