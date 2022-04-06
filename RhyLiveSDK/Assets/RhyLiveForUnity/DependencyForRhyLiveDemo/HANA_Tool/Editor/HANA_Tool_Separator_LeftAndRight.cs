using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

/*!
[2021] [kuniyan]
Please read included license

 */

namespace HanaSeparatorLeftAndRight
{
    public class HANA_Tool_Separator_LeftAndRight : EditorWindow
    {
        GameObject _baseObj;
        SkinnedMeshRenderer _renderer;

        List<string> _blendShapeNameList;
        string[] _blendShapeNameArray;
        List<bool> _blendShapeSeparateCheckList;

        Vector2 _scrollPos = new Vector2(0, 0);

        [MenuItem("HANA_Tool/Separator_LeftAndRight", false, 43)]
        static void CreateWindow()
        {
            GetWindow<HANA_Tool_Separator_LeftAndRight>("HANA_Tool_Separator_LeftAndRight");
        }

        void ClearData()
        {
            _baseObj = null;
            _renderer = null;
            if (_blendShapeNameList != null)
            {
                _blendShapeNameList.Clear();
                _blendShapeNameList = null;
            }
            if (_blendShapeNameArray != null)
            {
                _blendShapeNameArray = null;
            }
            if (_blendShapeSeparateCheckList != null)
            {
                _blendShapeSeparateCheckList.Clear();
                _blendShapeSeparateCheckList = null;
            }
        }


        void OnGUI()
        {
            using (var checkScope = new EditorGUI.ChangeCheckScope())
            {
                _baseObj = EditorGUILayout.ObjectField("SkinnedMeshRenderer",
                    _baseObj, typeof(GameObject), true) as GameObject;

                if (checkScope.changed)
                {
                    if (_baseObj == null)
                    {
                        ClearData();
                    }
                    else
                    {
                        _renderer = _baseObj.GetComponent<SkinnedMeshRenderer>();
                        if (_renderer == null)
                        {
                            EditorUtility.DisplayDialog("Error", "Uncontained SkinnedMeshRenderer component in Object", "OK");
                            ClearData();
                        }
                        else
                        {
                            var sharedMesh = _renderer.sharedMesh;

                            if (sharedMesh != null)
                            {
                                _blendShapeNameList = new List<string>();
                                _blendShapeSeparateCheckList = new List<bool>();

                                for (int i = 0; i < sharedMesh.blendShapeCount; i++)
                                {
                                    _blendShapeNameList.Add(sharedMesh.GetBlendShapeName(i));
                                    _blendShapeSeparateCheckList.Add(false);
                                }

                                _blendShapeNameArray = _blendShapeNameList.ToArray();
                            }
                            else
                            {
                                EditorUtility.DisplayDialog("Error", "No Mesh Object found on SkinnedMeshRenderer component", "OK");
                                return;
                            }
                        }
                    }
                }
            }

            if ((_blendShapeNameArray != null) && (_blendShapeSeparateCheckList != null) && (_blendShapeNameArray.Length == _blendShapeSeparateCheckList.Count))
            {
                GUILayout.Label("Check the BlendShape you want to split left and right");

                GUILayout.Label("------------------------------------------------------------------------------------------------------------------------");

                _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

                for (int i = 0; i < _blendShapeNameArray.Length; i++)
                {
                    _blendShapeSeparateCheckList[i] = GUILayout.Toggle(_blendShapeSeparateCheckList[i], _blendShapeNameArray[i]);
                }

                EditorGUILayout.EndScrollView();
                GUILayout.Label("------------------------------------------------------------------------------------------------------------------------");
            }

            using (new EditorGUI.DisabledScope(_baseObj == null))
            {
                if (GUILayout.Button("Separate BlendShape"))
                {
                    if(_renderer == null)
                    {
                        EditorUtility.DisplayDialog("Error", "Uncontained SkinnedMeshRenderer component in Object", "OK");
                        return;
                    }

                    Mesh sharedMesh = _renderer.sharedMesh;
                    if (sharedMesh == null)
                    {
                        EditorUtility.DisplayDialog("Error", "No Mesh Object found on SkinnedMeshRenderer component", "OK");
                        return;
                    }

                    if (_blendShapeNameList == null)
                    {
                        EditorUtility.DisplayDialog("Error", "The BlendShapeName list has not been generated.", "OK");
                        return;
                    }

                    var customMesh = Instantiate<Mesh>(sharedMesh);
                    customMesh.ClearBlendShapes();

                    var frameIndex = 0;
                    Vector3[] vertices, normals, tangents;

                    bool forwardDirZPluss = false;
                    var forwardVect = _baseObj.transform.forward;
                    if (0 <= forwardVect.z)
                    {   //Forward direction is Z direction and X direction is right
                        forwardDirZPluss = true;
                    }

                    for (int blendShapeIndex = 0; blendShapeIndex < sharedMesh.blendShapeCount; blendShapeIndex++)
                    {
                        if (blendShapeIndex < _blendShapeNameList.Count)
                        {
                            vertices = new Vector3[sharedMesh.vertexCount];
                            normals = new Vector3[sharedMesh.vertexCount];
                            tangents = new Vector3[sharedMesh.vertexCount];

                            sharedMesh.GetBlendShapeFrameVertices(blendShapeIndex, frameIndex, vertices, vertices, tangents);
                            float weight = sharedMesh.GetBlendShapeFrameWeight(blendShapeIndex, frameIndex);
                            string name = sharedMesh.GetBlendShapeName(blendShapeIndex);

                            customMesh.AddBlendShapeFrame(name, weight, vertices, normals, tangents);

                            if (_blendShapeSeparateCheckList[blendShapeIndex] == true)
                            {
                                var vertices_L = new Vector3[sharedMesh.vertexCount];
                                var normals_L = new Vector3[sharedMesh.vertexCount];
                                var tangents_L = new Vector3[sharedMesh.vertexCount];

                                var vertices_R = new Vector3[sharedMesh.vertexCount];
                                var normals_R = new Vector3[sharedMesh.vertexCount];
                                var tangents_R = new Vector3[sharedMesh.vertexCount];

                                for (int vertIndex = 0; vertIndex < sharedMesh.vertexCount; vertIndex++)
                                {
                                    if(0 <= sharedMesh.vertices[vertIndex].x)
                                    {   //X of the vertex is greater than or equal to 0
                                        if (forwardDirZPluss == true)
                                        {   //Object orientation in Z direction
                                            vertices_L[vertIndex] = Vector3.zero;
                                            normals_L[vertIndex] = Vector3.zero;
                                            tangents_L[vertIndex] = Vector3.zero;

                                            vertices_R[vertIndex] = vertices[vertIndex];
                                            normals_R[vertIndex] = normals[vertIndex];
                                            tangents_R[vertIndex] = tangents[vertIndex];
                                        }
                                        else
                                        {   //Object orientation in -Z direction
                                            vertices_L[vertIndex] = vertices[vertIndex];
                                            normals_L[vertIndex] = normals[vertIndex];
                                            tangents_L[vertIndex] = tangents[vertIndex];

                                            vertices_R[vertIndex] = Vector3.zero;
                                            normals_R[vertIndex] = Vector3.zero;
                                            tangents_R[vertIndex] = Vector3.zero;
                                        }
                                    }
                                    else
                                    {   //X of the vertex is less than or equal to 0
                                        if (forwardDirZPluss == true)
                                        {   //Object orientation in Z direction
                                            vertices_L[vertIndex] = vertices[vertIndex];
                                            normals_L[vertIndex] = normals[vertIndex];
                                            tangents_L[vertIndex] = tangents[vertIndex];

                                            vertices_R[vertIndex] = Vector3.zero;
                                            normals_R[vertIndex] = Vector3.zero;
                                            tangents_R[vertIndex] = Vector3.zero;
                                        }
                                        else
                                        {   //Object orientation in -Z direction
                                            vertices_L[vertIndex] = Vector3.zero;
                                            normals_L[vertIndex] = Vector3.zero;
                                            tangents_L[vertIndex] = Vector3.zero;

                                            vertices_R[vertIndex] = vertices[vertIndex];
                                            normals_R[vertIndex] = normals[vertIndex];
                                            tangents_R[vertIndex] = tangents[vertIndex];
                                        }
                                    }
                                }
                                weight = 100.0f;

                                bool sameName = false;
                                foreach(string shapeName in _blendShapeNameList)
                                {
                                    if((shapeName == name + "_L") || (shapeName == name + "_R"))
                                    {
                                        sameName = true;
                                        break;
                                    }
                                }

                                if(sameName == false)
                                {
                                    customMesh.AddBlendShapeFrame(name + "_L", weight, vertices_L, normals_L, tangents_L);
                                    customMesh.AddBlendShapeFrame(name + "_R", weight, vertices_R, normals_R, tangents_R);
                                }
                                else
                                {
                                    EditorUtility.DisplayDialog("Error", "Cannot convert because the name is already the same as the name with _L or _R after splitting.", "OK");
                                    return;
                                }
                            }
                        }
                    }

                    Undo.RecordObject(_renderer, "Renderer " + _renderer.name);
                    _renderer.sharedMesh = customMesh;

                    string createAssetPath = null;
                    createAssetPath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(sharedMesh)) + "/" + sharedMesh.name + "_custom.asset";

                    AssetDatabase.CreateAsset(customMesh, AssetDatabase.GenerateUniqueAssetPath(createAssetPath));
                    AssetDatabase.SaveAssets();

                    EditorUtility.DisplayDialog("Log", "Complete Separating", "OK");

                    ClearData();
                }
            }
        }
    }
}
