using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

/*!
[2020] [kuniyan]
Please read included license

 */

namespace HanaWriter
{
    public class WriteBlendShape
    {
        List<BSData> l_bsDatas;

        public WriteBlendShape()
        {
            l_bsDatas = new List<BSData>();
        }

        public void SaveData(string name, float weight, int vertexCount, Vector3[] vertices)
        {
            var data = new BSData();
            List<int> l_subNums = new List<int>();
            List<Vector3> l_v3_vertices = new List<Vector3>();

            data.shapeKeyName = name;
            data.blendShapeWeight = weight;
            data.vertexCount = vertexCount;

            for (int i = 0; i < vertices.Length; i++)
            {
                if (vertices[i] == Vector3.zero)
                {
                    continue;
                }
                l_subNums.Add(i);
                l_v3_vertices.Add(vertices[i]);
            }

            data.elements = l_subNums.ToArray();
            data.v3_vertices = l_v3_vertices.ToArray();

            l_bsDatas.Add(data);
        }

        public void WriteData(string filePath)
        {
            var json = JsonUtility.ToJson(new ListSerialize<BSData>(l_bsDatas));
            var writer = new StreamWriter(filePath, false);
            writer.WriteLine(json);
            writer.Flush();
            writer.Close();
        }
    }

    public class HANA_Tool_Writer : EditorWindow
    {
        SkinnedMeshRenderer renderer;
        List<string> ls_currShapeKeyNameList;
        string[] a_s_currShapeKeyName;
        List<bool> lb_checkedBlendShapeKeyList;
        bool checkedAllBlendShapeKey = true;

        Vector2 scrollPos = new Vector2( 0, 0);

        [MenuItem("HANA_Tool/Writer", false, 1)]
        static void CreateWindow()
        {
            GetWindow<HANA_Tool_Writer>("HANA_Tool_Writer");
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
                        ls_currShapeKeyNameList = null;
                        a_s_currShapeKeyName = null;
                        lb_checkedBlendShapeKeyList = null;
                    }
                    else
                    {
                        var sharedMesh = renderer.sharedMesh;

                        if (sharedMesh != null)
                        {
                            ls_currShapeKeyNameList = new List<string>();
                            lb_checkedBlendShapeKeyList = new List<bool>();

                            for (int i = 0; i < sharedMesh.blendShapeCount; i++)
                            {
                                ls_currShapeKeyNameList.Add(sharedMesh.GetBlendShapeName(i));
                                lb_checkedBlendShapeKeyList.Add(true);
                            }
                        }
                        else
                        {
                            EditorUtility.DisplayDialog("Error", "No Mesh Object found on SkinnedMeshRenderer component", "OK");
                            return;
                        }

                        a_s_currShapeKeyName = ls_currShapeKeyNameList.ToArray();
                    }
                }
            }

            if (a_s_currShapeKeyName != null)
            {
                GUILayout.Label("Please select BlendShapes to export");

                scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

                using (var checkScope = new EditorGUI.ChangeCheckScope())
                {
                    checkedAllBlendShapeKey = GUILayout.Toggle(checkedAllBlendShapeKey, "Select all");

                    if (checkScope.changed)
                    {
                        if(checkedAllBlendShapeKey)
                        {
                            for(int i = 0; i < lb_checkedBlendShapeKeyList.Count; i++)
                            {
                                lb_checkedBlendShapeKeyList[i] = true;
                            }
                        }
                        else
                        {
                            for (int i = 0; i < lb_checkedBlendShapeKeyList.Count; i++)
                            {
                                lb_checkedBlendShapeKeyList[i] = false;
                            }
                        }
                    }
                }

                for (int i = 0; i < a_s_currShapeKeyName.Length; i++)
                {
                    lb_checkedBlendShapeKeyList[i] = GUILayout.Toggle(lb_checkedBlendShapeKeyList[i], a_s_currShapeKeyName[i]);
                }

                EditorGUILayout.EndScrollView();
            }

            using (new EditorGUI.DisabledScope(renderer == null))
            {
                if (GUILayout.Button("Write BlendShape"))
                {
                    var sharedMesh = renderer.sharedMesh;
                    if (sharedMesh == null)
                    {
                        EditorUtility.DisplayDialog("Error", "No Mesh Object found on SkinnedMeshRenderer component", "OK");
                        return;
                    }

                    string fileNames = "HANA_BlendShapeData";
                    var filePath = EditorUtility.SaveFilePanel("Save", "Assets/HANA_Tool/BlendShapeData", fileNames, "txt");
                    if (!string.IsNullOrEmpty(filePath))
                    {
                        var mesh_custom = Instantiate<Mesh>(sharedMesh);

                        var frameIndex = 0;
                        Vector3[] v3_vertices, v3_normals, v3_tangents;
                        WriteBlendShape writeBlendShape = new WriteBlendShape();

                        for (int blendShapeIndex = 0; blendShapeIndex < sharedMesh.blendShapeCount; blendShapeIndex++)
                        {
                            if (lb_checkedBlendShapeKeyList[blendShapeIndex] == true)
                            {
                                v3_vertices = new Vector3[sharedMesh.vertexCount];
                                v3_normals = new Vector3[sharedMesh.vertexCount];
                                v3_tangents = new Vector3[sharedMesh.vertexCount];

                                sharedMesh.GetBlendShapeFrameVertices(blendShapeIndex, frameIndex, v3_vertices, v3_normals, v3_tangents);
                                var blendShapeWeight = sharedMesh.GetBlendShapeFrameWeight(blendShapeIndex, frameIndex);

                                string s_shapeKeyName = a_s_currShapeKeyName[blendShapeIndex];

                                writeBlendShape.SaveData(s_shapeKeyName, blendShapeWeight, sharedMesh.vertexCount, v3_vertices);
                            }
                        }

                        writeBlendShape.WriteData(filePath);

                        AssetDatabase.Refresh();

                        EditorUtility.DisplayDialog("log", "Finished writing blendshapes data\nIt can take a few minutes for the output file to show up in Unity\nWhen overwriting, it can be hard to tell when the file is updated", "OK");
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Error", "Destination setting error", "OK");
                        return;
                    }
                }
            }
        }
    }
}
