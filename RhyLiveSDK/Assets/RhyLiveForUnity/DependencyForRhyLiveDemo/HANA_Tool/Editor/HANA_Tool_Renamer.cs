using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

/*!
[2020] [kuniyan]
Please read included license

 */

namespace HanaRenamer
{
    public class HANA_Tool_Renamer : EditorWindow
    {
        SkinnedMeshRenderer renderer;
        List<string> originShapeNameList;
        List<string> shapeNameList;
        string meshName;

        Vector2 scrollPos = new Vector2(0, 0);

        [MenuItem("HANA_Tool/Renamer", false, 21)]
        static void CreateWindow()
        {
            GetWindow<HANA_Tool_Renamer>("HANA_Tool_Renamer");
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
                        shapeNameList = null;
                    }
                    else
                    {
                        var sharedMesh = renderer.sharedMesh;

                        if (sharedMesh != null)
                        {
                            originShapeNameList = new List<string>();
                            shapeNameList = new List<string>();
                            meshName = sharedMesh.name;

                            for (int i = 0; i < sharedMesh.blendShapeCount; i++)
                            {
                                originShapeNameList.Add(sharedMesh.GetBlendShapeName(i));
                                shapeNameList.Add(originShapeNameList[i]);
                            }
                        }
                        else
                        {
                            EditorUtility.DisplayDialog("Error", "No Mesh Object found on SkinnedMeshRenderer component", "OK");
                            return;
                        }
                    }
                }
            }

            if (renderer != null)
            {
                if (shapeNameList != null)
                {
                    var sharedMesh = renderer.sharedMesh;
                    if (sharedMesh == null)
                    {
                        EditorUtility.DisplayDialog("Error", "No Mesh Object found on SkinnedMeshRenderer component", "OK");
                        return;
                    }

                    scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.BeginVertical();

                    for (int shapeNum = 0; shapeNum < sharedMesh.blendShapeCount; shapeNum++)
                    {
                        GUILayout.Label(originShapeNameList[shapeNum], GUILayout.Height(16));
                    }

                    EditorGUILayout.EndVertical();
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.BeginVertical();

                    for (int shapeNum = 0; shapeNum < sharedMesh.blendShapeCount; shapeNum++)
                    {
                        shapeNameList[shapeNum] = EditorGUILayout.TextField(shapeNameList[shapeNum], GUILayout.Height(16));
                    }

                    EditorGUILayout.EndVertical();
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndScrollView();
                }
            }

            GUILayout.Label("------------------------------------------------------------------------------------------------------------------------");

            if (meshName != null)
            {
                meshName = EditorGUILayout.TextField("Mesh name", meshName);
            }

            using (new EditorGUI.DisabledScope(renderer == null))
            {
                if (GUILayout.Button("Rename BlendShapes"))
                {
                    var sharedMesh = renderer.sharedMesh;

                    for (int shapeNum = 0; shapeNum < shapeNameList.Count; shapeNum++)
                    {
                        var name = shapeNameList[shapeNum];
                        for (int checkNum = shapeNum + 1; checkNum < shapeNameList.Count; checkNum++)
                        {
                            if (name == shapeNameList[checkNum])
                            {
                                EditorUtility.DisplayDialog("Error", "There is more than one same name in the blendshape after editing", "OK");
                                return;
                            }
                        }
                    }

                    if (sharedMesh == null)
                    {
                        EditorUtility.DisplayDialog("Error", "No Mesh Object found on SkinnedMeshRenderer component", "OK");
                        return;
                    }

                    var customMesh = Instantiate<Mesh>(sharedMesh);
                    customMesh.ClearBlendShapes();

                    Vector3[] vertices = new Vector3[sharedMesh.vertexCount];
                    Vector3[] normals = new Vector3[sharedMesh.vertexCount];
                    Vector3[] tangents = new Vector3[sharedMesh.vertexCount];

                    for (int shapeNum = 0; shapeNum < sharedMesh.blendShapeCount; shapeNum++)
                    {
                        sharedMesh.GetBlendShapeFrameVertices(shapeNum, 0, vertices, normals, tangents);

                        customMesh.AddBlendShapeFrame(shapeNameList[shapeNum], 100, vertices, normals, tangents);
                    }

                    Undo.RecordObject(renderer, "Renderer " + renderer.name);
                    renderer.sharedMesh = customMesh;

                    string path = null;
                    path = Path.GetDirectoryName(AssetDatabase.GetAssetPath(sharedMesh)) + "/" + meshName + ".asset";
                    AssetDatabase.CreateAsset(customMesh, AssetDatabase.GenerateUniqueAssetPath(path));
                    AssetDatabase.SaveAssets();


                    EditorUtility.DisplayDialog("log", "Rename complete", "OK");
                }
            }
        }
    }
}
