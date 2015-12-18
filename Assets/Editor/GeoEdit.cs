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

    bool faceManipulation = false;
    bool vertexManipulation = false;
    bool extruding = false;

    // VERTEX OBJECT
    /// <summary> The vertex object </summary>
    GameObject vertObjectPrefab = Resources.Load("GeoEdit/Objects/vertObject") as GameObject; // Cache the prefab for vertices
    /// <summary> The parent of the vertex objects </summary>
    GameObject vertParent = null;
    /// <summary> The vertex objects of a selected object </summary>
    List<GameObject> vertObjects = new List<GameObject>();

    // VERTEX
    /// <summary> The currently selected vertices </summary>
    List<GameObject> selectedVertices = new List<GameObject>();
    /// <summary> The last positions of the selected vertices </summary>
    List<Vector3> vertexLast = new List<Vector3>();
    
    // FACE
    /// <summary> The position of the face last frame </summary>
    List<Vector3> faceLast = new List<Vector3>();
    /// <summary> The indices of each vertex in a face </summary>
    List<List<int>> faceData = new List<List<int>>();
    /// <summary> The indices of the selected vertices </summary>
    List<int> selectedFaceVertices = new List<int>();
    /// <summary> The index of the selected face </summary>
    int selectedFace = -1;


    // MESH
    /// <summary> The mesh of the selected object (not vertex) </summary>
    MeshFilter objectMesh = null;
    /// <summary> The currently selected object (not vertex) </summary>
    GameObject selectedObject = null;
    /// <summary> The last object selected (not vertex) </summary>
    GameObject lastObject = null;
    /// <summary> The positions of an object mesh's vertices (the actual selected mesh) </summary>
    Vector3[] objectVerts = null;

    [MenuItem("Window/GeoEdit")]
    public static void ShowWindow()
    {
        GetWindow(typeof(GeoEditWindow));
    }

    void OnGUI()
    {
        // If face manipulation is on and Vertex is pressed, turn face manipulation off
        // If vertex manipulation is on and Face is pressed, turn vertex manipulation off
        bool f = faceManipulation;
        bool v = vertexManipulation;
        faceManipulation = EditorGUILayout.Toggle("Face Manipulation", faceManipulation);
        vertexManipulation = EditorGUILayout.Toggle("Vertex Manipulation", vertexManipulation);

        // Never let them both be active at the same time
        if (f) if (vertexManipulation) { faceManipulation = false; Selection.activeGameObject = selectedObject; cleanObjects(); }
        if (v) if (faceManipulation) { vertexManipulation = false; Selection.activeGameObject = selectedObject; cleanObjects(); }

        if (faceManipulation && !extruding && GUILayout.Button("Extrude"))
            extruding = true;
        if (faceManipulation && extruding && GUILayout.Button("Move face to extude"))
            extruding = false;
    }

    void Update()
    {
        if(vertexManipulation)
        {
            // If the vertex parent object doesn't exist create it
            if (!vertParent)
                vertParent = new GameObject("vertParent");

            // If we haven't a record of a selectedObject, but the scene has one, make note of that
            // Then, also set the vertex parent's parent to the selected object
            if (!selectedObject && Selection.activeGameObject)
            {
                selectedObject = Selection.activeGameObject;
                vertParent.transform.parent = selectedObject.transform;
            }

            /// This section will set up the list of mesh vertices and create the vertex objects ///
            // If there is a selected object
            if (selectedObject)
            {
                // If there is an object selected in the scene and the last object selected isn't the selected object.
                // This check is so this only happens once. Might be able to do it with a bool if it isn't used later.
                if (Selection.activeGameObject && lastObject != selectedObject) // If the object currently selected is new
                {
                    // Get the mesh of the selected object.
                    objectMesh = selectedObject.GetComponent<MeshFilter>();

                    // If the mesh was set properly.
                    if (objectMesh)
                    {
                        // Create an array of all the positions of the mesh's vertices.
                        objectVerts = objectMesh.mesh.vertices;

                        // Create all the vertex objects and set their positions to the meshes vertices.
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
                /// This section will clean everything up ///
                // If there is no object selected in the scene or the currently selected object in the scene isn't the last object selected, clean up everything
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
            else
                cleanObjects();

            /// Vertex selection and Automatic vertex grouping ///
            // Go through selected objects
            for (int i = 0; i < Selection.gameObjects.Length; ++i)
            {
                // If a vertex is selected in the scene, but we haven't recorded that selection
                if (!selectedVertices.Contains(Selection.gameObjects[i]))
                {
                    // Make sure it is an actual vertex selected
                    if (Selection.gameObjects[i].transform.parent && Selection.activeGameObject.transform.parent == vertParent.transform)
                    {
                        // Search through the vertex objects
                        for (int j = 0; j < vertObjects.Count; j++)
                        {
                            // If this vertex is in the same position as the selected vertex, select it too
                            if (vertObjects[j].transform.position == Selection.gameObjects[i].transform.position)
                            {
                                // Make sure that this vertex isn't already selected
                                if (!selectedVertices.Contains(vertObjects[j]))
                                {
                                    //add the vertex and its position to the selected vertices and the last vertices
                                    selectedVertices.Add(vertObjects[j]);
                                    vertexLast.Add(vertObjects[j].transform.position);
                                }
                            }
                        }
                    }
                }
            }

            /// Moving vertices ///
            // If there are any selected vertices
            if (selectedVertices.Count != 0)
            {
                // Search through selectedVertices
                for (int i = 0; i < selectedVertices.Count; ++i)
                {
                    // If any of the vertices have been moved
                    if (selectedVertices[i].transform.position != vertexLast[i])
                    {
                        // Find any and all vertices of the same position as this vertex
                        Vector3 vertLastTemp = vertexLast[i];
                        Vector3 selectedVertTemp = selectedVertices[i].transform.position;

                        // Get the indices of all the related vertexLasts
                        List<int> relatedVertexLast = new List<int>();
                        for (int j = 0; j < vertexLast.Count; j++)
                        {
                            if (vertexLast[j] == vertLastTemp)
                                relatedVertexLast.Add(j);
                        }

                        // Get the indices of all the related vertexObjects
                        List<int> relatedVertexObjects = new List<int>();
                        for (int j = 0; j < vertObjects.Count; j++)
                        {
                            if ((vertObjects[j].transform.position == vertLastTemp || vertObjects[j].transform.position == selectedVertTemp))
                                relatedVertexObjects.Add(j);
                        }

                        // Go through all the indices found, setting everthing to their new positions P.S Both index lists should be the same, if they're not, something went wrong
                        for (int j = 0; j < relatedVertexLast.Count; j++)
                        {
                            selectedVertices[relatedVertexLast[j]].transform.position = selectedVertTemp;                              // Set the selectedVertex to new position
                            vertexLast[relatedVertexLast[j]] = selectedVertTemp;                                                       // Set the vertexLast to new position

                            vertObjects[relatedVertexObjects[j]].transform.position = selectedVertTemp;                                // Set the vertObject to new position
                            objectVerts[relatedVertexObjects[j]] = selectedObject.transform.InverseTransformPoint(selectedVertTemp);   // Grab the object's vertex in world space and convert it into local space
                        }
                    }
                }

                objectMesh.mesh.vertices = objectVerts; // update the mesh vertices which work in local space
            }

        }   // End if (vertexManipulation)
        else if (faceManipulation)
        {

            // If the vertex parent object doesn't exist create it
            if (!vertParent)
                vertParent = new GameObject("vertParent");

            // If we haven't a record of a selectedObject, but the scene has one, make note of that
            // Then, also set the vertex parent's parent to the selected object
            if (!selectedObject && Selection.activeGameObject)
            {
                selectedObject = Selection.activeGameObject;
                vertParent.transform.parent = selectedObject.transform;
            }

            /// This section will set up the list of mesh vertices and create the face data ///
            // If there is a selected object
            if (selectedObject)
            {
                // If there is an object selected in the scene and the last object selected isn't the selected object
                // This check is so this only happens once. Might be able to do it with a bool if it isn't used later.
                if (Selection.activeGameObject && lastObject != selectedObject) // If the object currently selected is new
                {
                    //get the mesh of the selected object.
                    objectMesh = selectedObject.GetComponent<MeshFilter>();

                    //if the mesh was set properly.
                    if (objectMesh)
                    {
                        //create an array of all the positions of the mesh's vertices.
                        objectVerts = objectMesh.mesh.vertices;

                        int faceCount = 0;
                        // Look through the triangles and find the faces
                        for (int i = 0; i < objectMesh.mesh.triangles.Length - 2; i += 3)
                        {
                            faceData.Add(new List<int>());

                            //Debug.Log("Face: " + faceCount);
                            bool relatedTriangleFound;
                            // While the face is incomplete, keep building it
                            do
                            {
                                relatedTriangleFound = false;
                                faceData[faceCount].Add(objectMesh.mesh.triangles[i]);
                                faceData[faceCount].Add(objectMesh.mesh.triangles[i + 1]);
                                faceData[faceCount].Add(objectMesh.mesh.triangles[i + 2]);
                                //Debug.Log("Triangle: " + objectMesh.mesh.triangles[i] + ", " + objectMesh.mesh.triangles[i + 1] + ", " + objectMesh.mesh.triangles[i + 2]);

                                // For each index in the triangle we just added
                                for (int t = i; t < i + 3; t++)
                                {
                                    // If no more triangles exist
                                    if (i + 3 > objectMesh.mesh.triangles.Length - 2)
                                        break;

                                    // See if it matches any of the indices in the next triangle
                                    if (objectMesh.mesh.triangles[t] == objectMesh.mesh.triangles[i + 3] || objectMesh.mesh.triangles[t] == objectMesh.mesh.triangles[i + 4] || objectMesh.mesh.triangles[t] == objectMesh.mesh.triangles[i + 5])
                                    { relatedTriangleFound = true; break; }
                                }

                                if (relatedTriangleFound)
                                    i += 3; // Go to the beginning of the next triangle

                            } while (relatedTriangleFound);

                            Vector3 posAvg = Vector3.zero;
                            List<int> used = new List<int>();
                            // Find the average point of the vertices
                            for (int j = 0; j < faceData[faceCount].Count; j++)
                            {
                                if (!used.Contains(faceData[faceCount][j]))
                                {
                                    posAvg += objectVerts[faceData[faceCount][j]];
                                    used.Add(faceData[faceCount][j]);
                                }
                            }
                            posAvg /= used.Count;
                            posAvg = selectedObject.transform.TransformPoint(posAvg);

                            // Create the faceObject
                            vertObjects.Add((GameObject)Instantiate(vertObjectPrefab, posAvg, new Quaternion()));
                            vertObjects[faceCount].transform.parent = vertParent.transform;
                            //faceData[faceCount].updateFaceLast(vertObjects[faceCount].transform.position);
                            faceLast.Add(vertObjects[faceCount].transform.position);

                            // Move on to the next face
                            faceCount++;
                        }
                    }
                    lastObject = selectedObject;
                }
                // If there is no object selected in the scene or the currently selected object in the scene isn't the last object selected, clean up everything
                else if (!Selection.activeGameObject || lastObject != Selection.activeGameObject)
                {
                    // Don't clean up if the current object selected is one of the vertex objects of the last selected object.
                    if (!(Selection.activeGameObject
                        && Selection.activeGameObject.transform.parent && Selection.activeGameObject.transform.parent.transform.parent // Ensure that the object selected is a vertex.
                        && Selection.activeGameObject.transform.parent.transform.parent == vertParent.transform.parent))
                    {
                        cleanObjects();
                    }
                }

                /// Face selection and vertex grouping ///
                // Go through selected objects
                for (int i = 0; i < Selection.gameObjects.Length; ++i)
                {
                    // If an object is selected in the scene, but we haven't recorded that selection
                    if (selectedFace == -1 || vertObjects[selectedFace] != Selection.gameObjects[i])
                    {
                        if (vertObjects.Contains(Selection.gameObjects[i]))
                        {
                            selectedFace = vertObjects.FindIndex(delegate (GameObject go) { return go == Selection.gameObjects[i]; });
                            selectedFaceVertices.Clear();
                            selectedFaceVertices = new List<int>();

                            // Loop through the indices of the face
                            for (int t = 0; t < faceData[selectedFace].Count; t++)
                            {
                                if (selectedFaceVertices.Contains(faceData[selectedFace][t]))
                                    continue;

                                selectedFaceVertices.Add(faceData[selectedFace][t]);

                                /// Vertex Grouping ///
                                // Search through objectVerts for any related vertices
                                for (int v = 0; v < objectVerts.Length; v++)
                                {
                                    // If the vertex we're looking at is equal to the vertex that's part of the face, and we haven't already selected this vertex
                                    if (objectVerts[faceData[selectedFace][t]] == objectVerts[v] && !selectedFaceVertices.Contains(v))
                                    {
                                        // Add the current index to the list of selected vertex indices
                                        selectedFaceVertices.Add(v);
                                    }
                                }
                            }
                        }
                    }
                }

                /// Extruding Face ///
                // If there are any selected faces, and we are extruding
                if (selectedFaceVertices.Count != 0 && extruding)
                {
                    // If the face has moved
                    if (vertObjects[selectedFace].transform.position != faceLast[selectedFace])
                    {
                        extruding = false;

                        selectedFaceVertices.Clear();
                        selectedFaceVertices = new List<int>();

                        // Fill selectedFaceVertices with the face vertices, no grouping
                        for (int i = 0; i < faceData[selectedFace].Count; i++)
                        {
                            if (selectedFaceVertices.Contains(faceData[selectedFace][i])) continue;

                            selectedFaceVertices.Add(faceData[selectedFace][i]);
                        }

                        /// Given any two vertices, they will only be an edge if they share one triangle.
                        /// Start finding adjacent vertices (otherwise known as edges)
                        List<List<int>> edgeVerts = new List<List<int>>();  // Vertices used to make an edges with other vertices

                        for (int i = 0; i < selectedFaceVertices.Count; i++)
                        {
                            edgeVerts.Add(new List<int>());
                            List<int> toRemove = new List<int>();
                            // Search through the selected face by triangles
                            for (int t = 0; t < faceData[selectedFace].Count / 3; t++)
                            {
                                int triStart = t * 3;
                                // If the triangle contains our vertex
                                if (faceData[selectedFace][triStart] == selectedFaceVertices[i]
                                    || faceData[selectedFace][triStart + 1] == selectedFaceVertices[i]
                                    || faceData[selectedFace][triStart + 2] == selectedFaceVertices[i])
                                {
                                    // Search through the triangle
                                    for (int j = 0; j < 3; j++)
                                    {
                                        // Skip our vertex or vertices we already know need to be removed
                                        if (faceData[selectedFace][triStart + j] == selectedFaceVertices[i] || toRemove.Contains(faceData[selectedFace][triStart + j]))
                                            continue;

                                        if (edgeVerts[i].Contains(faceData[selectedFace][triStart + j]))
                                        {
                                            toRemove.Add(faceData[selectedFace][triStart + j]);
                                            continue;
                                        }
                                        edgeVerts[i].Add(faceData[selectedFace][triStart + j]);
                                    }
                                }
                            }

                            for (int j = 0; j < toRemove.Count; j++)
                            {
                                edgeVerts[i].Remove(toRemove[j]); // edgeVerts[i].RemoveAll(delegate (int vert) { return vert == toRemove[j]; });
                            }
                        }

                        // Record the old positions
                        List<Vector3> oldVertPositions = new List<Vector3>();
                        for (int i = 0; i < objectVerts.Length; i++)
                            oldVertPositions.Add(objectVerts[i]);

                        // Move the vertices
                        Vector3 movement = vertObjects[selectedFace].transform.position - faceLast[selectedFace];

                        for (int i = 0; i < selectedFaceVertices.Count; i++)
                            objectVerts[selectedFaceVertices[i]] += movement;

                        faceLast[selectedFace] = vertObjects[selectedFace].transform.position;

                        // Convert static arrays to dynamic lists
                        List<Vector3> objectVertsNew = new List<Vector3>();
                        for (int i = 0; i < objectVerts.Length; i++)
                            objectVertsNew.Add(objectVerts[i]);

                        List<int> triangleListNew = new List<int>();
                        for (int i = 0; i < objectMesh.mesh.triangles.Length; i++)
                            triangleListNew.Add(objectMesh.mesh.triangles[i]);

                        /// Start building triangles
                        for (int i = 0; i < selectedFaceVertices.Count; i++)
                        {
                            for (int e = 0; e < edgeVerts[i].Count; e++)
                            {
                                Debug.Assert(oldVertPositions[selectedFaceVertices[i]] != objectVerts[selectedFaceVertices[i]], "Old and new currVert positions are equal! " + objectVerts[selectedFaceVertices[i]]);
                                Debug.Assert(oldVertPositions[edgeVerts[i][e]] != objectVerts[edgeVerts[i][e]], "Old and new edge vert positions are equal! " + objectVerts[edgeVerts[i][e]]);

                                // Search through the triangles for current vert and adjVert
                                for (int t = 0; t < faceData[selectedFace].Count / 3; t++)
                                {
                                    int triStart = t * 3;
                                    int currVertPos = -1;
                                    int edgeVertPos = -1;

                                    // Search through the triangle
                                    for (int j = 0; j < 3; j++)
                                    {
                                        // For whichever vert this is, if it is, record its position in the triangle
                                        if (faceData[selectedFace][triStart + j] == selectedFaceVertices[i])
                                            currVertPos = j;
                                        else if (faceData[selectedFace][triStart + j] == edgeVerts[i][e])
                                            edgeVertPos = j;
                                    }

                                    // If we didn't find the correct triangle
                                    if (currVertPos == -1 || edgeVertPos == -1)
                                    {
                                        currVertPos = 0;
                                        edgeVertPos = 0;
                                        continue;
                                    }

                                    // Incidentally, this is where the first vertex we add will end up
                                    int firstVertex = objectVertsNew.Count;
                                    // Triangles move clockwise, so, if something is 2 away from currVert, or directly behind currVert, there needs to be a vertex between the currVert and edgeVert 
                                    if (currVertPos - edgeVertPos == -1 || currVertPos - edgeVertPos == 2)
                                    {
                                        // Triangle 1
                                        objectVertsNew.Add(oldVertPositions[edgeVerts[i][e]]);          // Old edge
                                        objectVertsNew.Add(objectVerts[selectedFaceVertices[i]]);       // New currVert
                                        objectVertsNew.Add(oldVertPositions[selectedFaceVertices[i]]);  // Old currVert

                                        // Triangle 2
                                        objectVertsNew.Add(objectVerts[edgeVerts[i][e]]);               // New edge
                                    }
                                    else
                                    {
                                        // Triangle 1
                                        objectVertsNew.Add(oldVertPositions[selectedFaceVertices[i]]);  // Old currVert
                                        objectVertsNew.Add(objectVerts[edgeVerts[i][e]]);               // New edge
                                        objectVertsNew.Add(oldVertPositions[edgeVerts[i][e]]);          // Old edge

                                        // Triangle 2
                                        objectVertsNew.Add(objectVerts[selectedFaceVertices[i]]);       // New currVert
                                    }

                                    // Triangle 1
                                    triangleListNew.Add(firstVertex);
                                    triangleListNew.Add(firstVertex + 1);
                                    triangleListNew.Add(firstVertex + 2);

                                    // Triangle 2
                                    triangleListNew.Add(firstVertex);
                                    triangleListNew.Add(firstVertex + 3);
                                    triangleListNew.Add(firstVertex + 1);

                                    // Make sure we don't make this face again by accident
                                    for (int j = 0; j < selectedFaceVertices.Count; j++)
                                    {
                                        if (selectedFaceVertices[j] == edgeVerts[i][e])
                                            edgeVerts[j].Remove(selectedFaceVertices[i]);
                                    }
                                }
                            }
                        }

                        objectMesh.mesh.SetVertices(objectVertsNew);
                        objectMesh.mesh.triangles = triangleListNew.ToArray();
                        objectVerts = objectMesh.mesh.vertices;
                        objectMesh.mesh.RecalculateNormals();

                        /// Vertex Re-grouping ///
                        selectedFaceVertices.Clear();

                        // Loop through the indices of the face
                        for (int t = 0; t < faceData[selectedFace].Count; t++)
                        {
                            if (selectedFaceVertices.Contains(faceData[selectedFace][t]))
                                continue;

                            selectedFaceVertices.Add(faceData[selectedFace][t]);

                            /// Vertex Grouping ///
                            // Search through objectVerts for any related vertices
                            for (int v = 0; v < objectVerts.Length; v++)
                            {
                                // If the vertex we're looking at is equal to the vertex that's part of the face, and we haven't already selected this vertex
                                if (objectVerts[faceData[selectedFace][t]] == objectVerts[v] && !selectedFaceVertices.Contains(v))
                                {
                                    // Add the current index to the list of selected vertex indices
                                    selectedFaceVertices.Add(v);
                                }
                            }
                        }
                    }
                }
                /// Moving faces ///
                // If there are any selected faces
                else if (selectedFaceVertices.Count != 0)
                {
                    // If the face has moved
                    if (vertObjects[selectedFace].transform.position != faceLast[selectedFace])
                    {
                        // Get the movement that occured from faceLast to the current face position
                        Vector3 movement = vertObjects[selectedFace].transform.position - faceLast[selectedFace];

                        // Apply that movement to all the selectedFaceVertices
                        for (int i = 0; i < selectedFaceVertices.Count; i++)
                        {
                            objectVerts[selectedFaceVertices[i]] += movement;
                        }

                        faceLast[selectedFace] = vertObjects[selectedFace].transform.position;

                        // Update the other face objects
                        Vector3 posAvg;
                        List<int> used;
                        // Find the average point of the vertices
                        for (int f = 0; f < faceData.Count; f++)
                        {
                            posAvg = Vector3.zero;
                            used = new List<int>();
                            for (int i = 0; i < faceData[f].Count; i++)
                            {
                                if (!used.Contains(faceData[f][i]))
                                {
                                    posAvg += objectVerts[faceData[f][i]];
                                    used.Add(faceData[f][i]);
                                }
                            }
                            posAvg /= used.Count;
                            posAvg = selectedObject.transform.TransformPoint(posAvg);

                            // Update the position
                            vertObjects[f].transform.position = posAvg;
                            faceLast[f] = vertObjects[f].transform.position;
                        }
                    }
                }

                // update the mesh vertices which work in local space
                if (objectMesh)
                    objectMesh.mesh.vertices = objectVerts;

            } // end if (selectedObject)
            else
                cleanObjects();

        }// end if (faceManipulation)
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

        faceLast.Clear();
        faceLast = null;
        faceLast = new List<Vector3>();

        faceData.Clear();
        faceData = null;
        faceData = new List<List<int>>();

        selectedFaceVertices.Clear();
        selectedFaceVertices = null;
        selectedFaceVertices = new List<int>();

        //set selected objects to null
        selectedObject = null;
        vertParent = null;
        lastObject = null;
        objectMesh = null;
        objectVerts = null;
        selectedFace = -1;
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