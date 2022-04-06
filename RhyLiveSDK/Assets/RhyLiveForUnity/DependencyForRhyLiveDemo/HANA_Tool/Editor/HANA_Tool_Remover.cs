using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

/*!
[2020] [kuniyan]
Please read included license

 */

namespace HanaRemover
{
    public class HANA_Tool_Remover : EditorWindow
    {
        SkinnedMeshRenderer renderer;
        List<string> ls_currShapeKeyNameList;
        string[] a_s_currShapeKeyName;
        List<bool> lb_checkedBlendShapeKeyList;
        bool checkedAllBlendShapeKey = false;
        bool meshName_Face = false;

        Vector2 scrollPos = new Vector2(0, 0);

        [MenuItem("HANA_Tool/Remover", false, 22)]
        static void CreateWindow()
        {
            GetWindow<HANA_Tool_Remover>("HANA_Tool_Remover");
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
                                lb_checkedBlendShapeKeyList.Add(false);
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
                GUILayout.Label("Select the BlendShapes that you want to erase");

                GUILayout.Label("------------------------------------------------------------------------------------------------------------------------");

                scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

                using (var checkScope = new EditorGUI.ChangeCheckScope())
                {
                    checkedAllBlendShapeKey = GUILayout.Toggle(checkedAllBlendShapeKey, "Select all");

                    if (checkScope.changed)
                    {
                        if (checkedAllBlendShapeKey)
                        {
                            for (int i = 0; i < lb_checkedBlendShapeKeyList.Count; i++)
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
                GUILayout.Label("------------------------------------------------------------------------------------------------------------------------");
            }

            using (new EditorGUI.DisabledScope(renderer == null))
            {
                meshName_Face = GUILayout.Toggle(meshName_Face, "Change mesh name to [Face]");

                if (GUILayout.Button("Remove BlendShape"))
                {
                    var sharedMesh = renderer.sharedMesh;
                    if (sharedMesh == null)
                    {
                        EditorUtility.DisplayDialog("Error", "No Mesh Object found on SkinnedMeshRenderer component", "OK");
                        return;
                    }

                    var mesh_custom = Instantiate<Mesh>(sharedMesh);
                    mesh_custom.ClearBlendShapes();

                    var frameIndex = 0;
                    Vector3[] v3_vertices, v3_normals, v3_tangents;

                    for (int blendShapeIndex = 0; blendShapeIndex < sharedMesh.blendShapeCount; blendShapeIndex++)
                    {
                        if (lb_checkedBlendShapeKeyList[blendShapeIndex] == false)
                        {
                            v3_vertices = new Vector3[sharedMesh.vertexCount];
                            v3_normals = new Vector3[sharedMesh.vertexCount];
                            v3_tangents = new Vector3[sharedMesh.vertexCount];

                            sharedMesh.GetBlendShapeFrameVertices(blendShapeIndex, frameIndex, v3_vertices, v3_normals, v3_tangents);
                            var blendShapeWeight = sharedMesh.GetBlendShapeFrameWeight(blendShapeIndex, frameIndex);

                            string s_shapeKeyName = a_s_currShapeKeyName[blendShapeIndex];
                            mesh_custom.AddBlendShapeFrame(s_shapeKeyName, blendShapeWeight, v3_vertices, v3_normals, v3_tangents);
                        }
                    }

                    Undo.RecordObject(renderer, "Renderer " + renderer.name);
                    renderer.sharedMesh = mesh_custom;

                    string createAssetPath = null;
                    if (meshName_Face)
                    {
                        createAssetPath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(sharedMesh)) + "/" + "Face.asset";
                    }
                    else
                    {
                        createAssetPath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(sharedMesh)) + "/" + sharedMesh.name + "_custom.asset";
                    }
                    AssetDatabase.CreateAsset(mesh_custom, AssetDatabase.GenerateUniqueAssetPath(createAssetPath));
                    AssetDatabase.SaveAssets();

                    EditorUtility.DisplayDialog("log", "Removal completed", "OK");
                }
            }
        }
    }
}
