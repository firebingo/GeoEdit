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
    bool enableGeoEdit = false;
    GameObject vertObjectPrefab = Resources.Load("GeoEdit/Objects/vertObject") as GameObject; //cache the prefab for vertices
    List<GameObject> vertObjects = new List<GameObject>(); //list of the vertices of a selected object
    GameObject selectedObject = null; //the currently selected object (not vertex)
    GameObject vertParent = null; //the parent of the vertex objects.
    GameObject lastObject = null; //the last object selected (not vertex)
    List<GameObject> selectedVertices = new List<GameObject>(); //a list of the current selected vertices
    List<Vector3> vertexLast = new List<Vector3>(); //the last positions of vertices
    MeshFilter objectMesh = null; //the mesh of the selected object (not vertex)
    Vector3[] objectVerts = null; //an array of the positions of an object's meshe's vertices (the actual selected mesh)

    [MenuItem("Window/GeoEdit")]
    public static void ShowWindow()
    {
        GetWindow(typeof(GeoEditWindow));
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
            if (!vertParent)
                vertParent = new GameObject("vertParent");

            //if there is a object selected in the scene but no selectedObject set it.
            //and set the vertex parent's parent to the selected object.
            if (!selectedObject && Selection.activeGameObject)
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
                    objectMesh = selectedObject.GetComponent<MeshFilter>();
                    //if the mesh was set properly.
                    if (objectMesh)
                    {
                        //create a array of all the positions of the meshes' vertices.
                        objectVerts = objectMesh.mesh.vertices;

                        //create all the vertex objects and set their positions to the meshes vertices.
                        for (int i = 0; i < objectVerts.Length; ++i)
                        {
                            //need to get a world space scale so vertex objects are placed properly on a scaled object.
                            Vector3 scaledPos = selectedObject.transform.TransformPoint(objectVerts[i]);

                            vertObjects.Add((GameObject)Instantiate(vertObjectPrefab, scaledPos, new Quaternion()));
                            vertObjects[i].transform.parent = vertParent.transform;
                        }
                    }
                    lastObject = selectedObject;
                }
                //if there is no object selected in the scene or the currently selected object in the scene isint the last
                // object selected clean up everything.
                else if (!Selection.activeGameObject || lastObject != Selection.activeGameObject)
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

            for (int i = 0; i < Selection.gameObjects.Length; ++i)
            {
                //if the current selected vertices doesn't contain one of the scene selected vertices.
                if (!selectedVertices.Contains(Selection.gameObjects[i]))
                {
                    //make sure it is an actual vertex selected
                    if (Selection.gameObjects[i].transform.parent && Selection.activeGameObject.transform.parent == vertParent.transform)
                    {
                        //add the vertex and its position to the selected vertices and the last vertices.
                        selectedVertices.Add(Selection.gameObjects[i]);
                        vertexLast.Add(selectedVertices[i].transform.position);
                    }
                }
            }

            //this section is magic as far as i'm concerned.
            //if there are any selected vertices
            if (selectedVertices.Count != 0)
            {
                for (int i = 0; i < selectedVertices.Count; ++i)
                {
                    //if any of the vertices have been moved
                    if (selectedVertices[i].transform.position != vertexLast[i])
                    {
                        vertexLast[i] = selectedVertices[i].transform.position;
                        //search through the vertices and find the selected one.
                        for (int j = 0; j < vertObjects.Count; ++j)
                        {
                            if (vertObjects[j].transform.position == vertexLast[i] && objectMesh)
                            {
                                //convert the selected vertex's new position to the selected object's local space then set it's vertices.
                                objectVerts[j] = selectedObject.transform.InverseTransformPoint(vertObjects[j].transform.localPosition);
                                List<Vector3> vertList = new List<Vector3>(objectVerts);
                                objectMesh.mesh.SetVertices(vertList);
                            }
                        }
                    }
                }
            }
        }
        //if geoedit is disabled
        else
            cleanObjects();
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
        if (vertParent)
            GameObject.DestroyImmediate(vertParent);
        //clean and reintilize lists
        vertObjects.Clear();
        vertObjects = null;
        vertObjects = new List<GameObject>();
        selectedVertices.Clear();
        selectedVertices = null;
        selectedVertices = new List<GameObject>();
        vertexLast.Clear();
        vertexLast = null;
        vertexLast = new List<Vector3>();
        //set selected objects to null
        selectedObject = null;
        vertParent = null;
        lastObject = null;
        objectMesh = null;
        objectVerts = null;
    }

    //c++ 4lyfe (make sure any lists are actually nulled properly at close)
    void destructor()
    {
        cleanObjects();

        vertObjects = null;
        selectedVertices = null;
        selectedObject = null;

        vertObjectPrefab = null;
        vertParent = null;
    }

    //clean everything if the window is closed.
    void OnDestroy()
    {
        destructor();
        if (vertParent)
            GameObject.DestroyImmediate(vertParent);
    }
}