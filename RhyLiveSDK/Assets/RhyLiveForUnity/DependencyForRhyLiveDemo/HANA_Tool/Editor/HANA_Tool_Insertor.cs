using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

/*!
[2021] [kuniyan]
Please read included license

 */

namespace HanaInsertor
{
    public class HANA_Tool_Insertor : EditorWindow
    {
        SkinnedMeshRenderer renderer;

        List<string> _blendShapeNameList;
        string[] _blendShapeNameArray;
        string[] _blendShapeUserNameArray;
        List<bool> _blendShapeInsertCheckList;

        Vector2 _scrollPos = new Vector2(0, 0);

        [MenuItem("HANA_Tool/Insertor", false, 23)]
        static void CreateWindow()
        {
            GetWindow<HANA_Tool_Insertor>("HANA_Tool_Insertor");
        }

        void ClearData()
        {
            renderer = null;
            if (_blendShapeNameList != null)
            {
                _blendShapeNameList.Clear();
                _blendShapeNameList = null;
            }
            if(_blendShapeNameArray != null)
            {
                _blendShapeNameArray = null;
            }
            if(_blendShapeUserNameArray != null)
            {
                _blendShapeUserNameArray = null;
            }
            if(_blendShapeInsertCheckList != null)
            {
                _blendShapeInsertCheckList.Clear();
                _blendShapeInsertCheckList = null;
            }
        }


        void OnGUI()
        {
            using (var checkScope = new EditorGUI.ChangeCheckScope())
            {
                renderer = EditorGUILayout.ObjectField
                    (
                    "SkinnedMeshRenderer",
                    renderer,
                    typeof(SkinnedMeshRenderer),
                    true
                    ) as SkinnedMeshRenderer;

                if (checkScope.changed)
                {
                    if (renderer == null)
                    {
                        _blendShapeNameList = null;
                        _blendShapeInsertCheckList = null;
                    }
                    else
                    {
                        var sharedMesh = renderer.sharedMesh;

                        if (sharedMesh != null)
                        {
                            _blendShapeNameList = new List<string>();
                            _blendShapeInsertCheckList = new List<bool>();

                            for (int i = 0; i < sharedMesh.blendShapeCount; i++)
                            {
                                _blendShapeNameList.Add(sharedMesh.GetBlendShapeName(i));
                                _blendShapeInsertCheckList.Add(false);
                            }

                            _blendShapeNameArray = _blendShapeNameList.ToArray();
                            _blendShapeUserNameArray = _blendShapeNameList.ToArray();
                        }
                        else
                        {
                            EditorUtility.DisplayDialog("Error", "No Mesh Object found on SkinnedMeshRenderer component", "OK");
                            return;
                        }
                    }
                }
            }

            if (_blendShapeNameArray != null)
            {
                GUILayout.Label("Check the box where you want to insert it");

                GUILayout.Label("------------------------------------------------------------------------------------------------------------------------");

                _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

                for (int i = 0; i < _blendShapeNameArray.Length; i++)
                {
                    GUILayout.Label(_blendShapeNameArray[i]);

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    _blendShapeInsertCheckList[i] = GUILayout.Toggle(_blendShapeInsertCheckList[i], "");
                    if (_blendShapeInsertCheckList[i] == true)
                    {
                        _blendShapeUserNameArray[i] = EditorGUILayout.TextField(_blendShapeUserNameArray[i], GUILayout.Height(16));
                    }
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndScrollView();
                GUILayout.Label("------------------------------------------------------------------------------------------------------------------------");
            }

            using (new EditorGUI.DisabledScope(renderer == null))
            {
                if (GUILayout.Button("Insert BlendShape"))
                {
                    var sharedMesh = renderer.sharedMesh;
                    if (sharedMesh == null)
                    {
                        EditorUtility.DisplayDialog("Error", "No Mesh Object found on SkinnedMeshRenderer component", "OK");
                        return;
                    }

                    if( _blendShapeNameList == null)
                    {
                        EditorUtility.DisplayDialog("Error", "The BlendShapeName list has not been generated", "OK");
                        return;
                    }

                    var customMesh = Instantiate<Mesh>(sharedMesh);
                    customMesh.ClearBlendShapes();

                    var frameIndex = 0;
                    Vector3[] vertices, normals, tangents;
                    List<string> nameCheckList = new List<string>();
                    int sameNameCnt = 0;

                    for (int blendShapeIndex = 0; blendShapeIndex < sharedMesh.blendShapeCount; blendShapeIndex++)
                    {
                        if (_blendShapeNameList[blendShapeIndex] != null)
                        {
                            vertices = new Vector3[sharedMesh.vertexCount];
                            normals = new Vector3[sharedMesh.vertexCount];
                            tangents = new Vector3[sharedMesh.vertexCount];

                            sharedMesh.GetBlendShapeFrameVertices(blendShapeIndex, frameIndex, vertices, vertices, tangents);
                            float weight = sharedMesh.GetBlendShapeFrameWeight(blendShapeIndex, frameIndex);
                            string name = sharedMesh.GetBlendShapeName(blendShapeIndex);

                            //同じ名前がないかチェック あったらナンバーを振る
                            sameNameCnt = 0;
                            foreach (string checkName in nameCheckList)
                            {
                                if (name == checkName)
                                {
                                    sameNameCnt++;
                                }
                            }
                            if (sameNameCnt != 0)
                            {
                                EditorUtility.DisplayDialog("Log", "The same name exists。\nPlease fix it", "OK");
                                customMesh.Clear();
                                customMesh = null;
                                return;
                            }

                            customMesh.AddBlendShapeFrame(name, weight, vertices, normals, tangents);
                            nameCheckList.Add(name);

                            if (_blendShapeInsertCheckList[blendShapeIndex] == true)
                            {
                                vertices = new Vector3[sharedMesh.vertexCount];
                                normals = new Vector3[sharedMesh.vertexCount];
                                tangents = new Vector3[sharedMesh.vertexCount];
                                weight = 100.0f;
                                name = _blendShapeUserNameArray[blendShapeIndex];

                                //同じ名前がないかチェック あったらナンバーを振る
                                sameNameCnt = 0;
                                foreach (string checkName in nameCheckList)
                                {
                                    if(name == checkName)
                                    {
                                        sameNameCnt++;
                                    }
                                }
                                if(sameNameCnt != 0)
                                {
                                    EditorUtility.DisplayDialog("Log", "The same name exists\nPlease fix it", "OK");
                                    customMesh.Clear();
                                    customMesh = null;
                                    return;
                                }

                                customMesh.AddBlendShapeFrame(name, weight, vertices, normals, tangents);
                                nameCheckList.Add(name);
                            }
                        }
                    }

                    Undo.RecordObject(renderer, "Renderer " + renderer.name);
                    renderer.sharedMesh = customMesh;

                    string createAssetPath = null;
                    createAssetPath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(sharedMesh)) + "/" + sharedMesh.name + "_custom.asset";
                    
                    AssetDatabase.CreateAsset(customMesh, AssetDatabase.GenerateUniqueAssetPath(createAssetPath));
                    AssetDatabase.SaveAssets();

                    EditorUtility.DisplayDialog("Log", "Finished add blank blendshapes to the mesh", "OK");

                    ClearData();
                }
            }
        }
    }
}
