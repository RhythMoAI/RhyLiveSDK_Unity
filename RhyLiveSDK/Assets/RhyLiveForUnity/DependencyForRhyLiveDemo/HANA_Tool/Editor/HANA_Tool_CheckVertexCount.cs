using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;


/*!
[2020] [kuniyan]
Please read included license

 */

namespace HanaCheckVertexCount
{
    public class HANA_Tool_CheckVertexCount : EditorWindow
    {
        SkinnedMeshRenderer renderer_A;
        SkinnedMeshRenderer renderer_B;
        SkinnedMeshRenderer renderer_C;
        SkinnedMeshRenderer renderer_D;
        SkinnedMeshRenderer renderer_E;
        SkinnedMeshRenderer renderer_F;

        Mesh sharedMesh_A;
        Mesh sharedMesh_B;
        Mesh sharedMesh_C;
        Mesh sharedMesh_D;
        Mesh sharedMesh_E;
        Mesh sharedMesh_F;

        int vertCnt_A;
        int vertCnt_B;
        int vertCnt_C;
        int vertCnt_D;
        int vertCnt_E;
        int vertCnt_F;


        [MenuItem("HANA_Tool/CheckVertexCount")]
        static void CreateWindow()
        {
            GetWindow<HANA_Tool_CheckVertexCount>("HanaCheckVertexCount");
        }

        void OnGUI()
        {
            using (var checkScope = new EditorGUI.ChangeCheckScope())
            {
                renderer_A = EditorGUILayout.ObjectField
                    (
                    "SkinnedMeshRenderer",
                    renderer_A,
                    typeof(SkinnedMeshRenderer),
                    true
                    ) as SkinnedMeshRenderer;

                if (checkScope.changed)
                {
                    if (renderer_A == null)
                    {
                        vertCnt_A = 0;
                    }
                    else
                    {
                        sharedMesh_A = renderer_A.sharedMesh;

                        if (sharedMesh_A != null)
                        {
                            vertCnt_A = sharedMesh_A.vertexCount;
                        }
                        else
                        {
                            vertCnt_A = 0;
                            EditorUtility.DisplayDialog("Error", "No Mesh Object found on SkinnedMeshRenderer component", "OK");
                            return;
                        }
                    }
                }

                if (sharedMesh_A != null)
                {
                    GUILayout.Label("vertex count:" + vertCnt_A);
                }
            }

            using (var checkScope = new EditorGUI.ChangeCheckScope())
            {
                renderer_B = EditorGUILayout.ObjectField
                    (
                    "SkinnedMeshRenderer",
                    renderer_B,
                    typeof(SkinnedMeshRenderer),
                    true
                    ) as SkinnedMeshRenderer;

                if (checkScope.changed)
                {
                    if (renderer_B == null)
                    {
                        vertCnt_B = 0;
                    }
                    else
                    {
                        sharedMesh_B = renderer_B.sharedMesh;

                        if (sharedMesh_B != null)
                        {
                            vertCnt_B = sharedMesh_B.vertexCount;
                        }
                        else
                        {
                            vertCnt_B = 0;
                            EditorUtility.DisplayDialog("Error", "No Mesh Object found on SkinnedMeshRenderer component", "OK");
                            return;
                        }
                    }
                }

                if (sharedMesh_B != null)
                {
                    GUILayout.Label("vertex count:" + vertCnt_B);
                }
            }

            using (var checkScope = new EditorGUI.ChangeCheckScope())
            {
                renderer_C = EditorGUILayout.ObjectField
                    (
                    "SkinnedMeshRenderer",
                    renderer_C,
                    typeof(SkinnedMeshRenderer),
                    true
                    ) as SkinnedMeshRenderer;

                if (checkScope.changed)
                {
                    if (renderer_C == null)
                    {
                        vertCnt_C = 0;
                    }
                    else
                    {
                        sharedMesh_C = renderer_C.sharedMesh;

                        if (sharedMesh_C != null)
                        {
                            vertCnt_C = sharedMesh_C.vertexCount;
                        }
                        else
                        {
                            vertCnt_C = 0;
                            EditorUtility.DisplayDialog("Error", "No Mesh Object found on SkinnedMeshRenderer component", "OK");
                            return;
                        }
                    }
                }

                if (sharedMesh_C != null)
                {
                    GUILayout.Label("vertex count:" + vertCnt_C);
                }
            }


            using (var checkScope = new EditorGUI.ChangeCheckScope())
            {
                renderer_D = EditorGUILayout.ObjectField
                    (
                    "SkinnedMeshRenderer",
                    renderer_D,
                    typeof(SkinnedMeshRenderer),
                    true
                    ) as SkinnedMeshRenderer;

                if (checkScope.changed)
                {
                    if (renderer_D == null)
                    {
                        vertCnt_D = 0;
                    }
                    else
                    {
                        sharedMesh_D = renderer_D.sharedMesh;

                        if (sharedMesh_D != null)
                        {
                            vertCnt_D = sharedMesh_D.vertexCount;
                        }
                        else
                        {
                            vertCnt_D = 0;
                            EditorUtility.DisplayDialog("Error", "No Mesh Object found on SkinnedMeshRenderer component", "OK");
                            return;
                        }
                    }
                }

                if (sharedMesh_D != null)
                {
                    GUILayout.Label("vertex count:" + vertCnt_D);
                }
            }

            using (var checkScope = new EditorGUI.ChangeCheckScope())
            {
                renderer_E = EditorGUILayout.ObjectField
                    (
                    "SkinnedMeshRenderer",
                    renderer_E,
                    typeof(SkinnedMeshRenderer),
                    true
                    ) as SkinnedMeshRenderer;

                if (checkScope.changed)
                {
                    if (renderer_E == null)
                    {
                        vertCnt_E = 0;
                    }
                    else
                    {
                        sharedMesh_E = renderer_E.sharedMesh;

                        if (sharedMesh_E != null)
                        {
                            vertCnt_E = sharedMesh_E.vertexCount;
                        }
                        else
                        {
                            vertCnt_E = 0;
                            EditorUtility.DisplayDialog("Error", "No Mesh Object found on SkinnedMeshRenderer component", "OK");
                            return;
                        }
                    }
                }

                if (sharedMesh_E != null)
                {
                    GUILayout.Label("vertex count:" + vertCnt_E);
                }
            }

            using (var checkScope = new EditorGUI.ChangeCheckScope())
            {
                renderer_F = EditorGUILayout.ObjectField
                    (
                    "SkinnedMeshRenderer",
                    renderer_F,
                    typeof(SkinnedMeshRenderer),
                    true
                    ) as SkinnedMeshRenderer;

                if (checkScope.changed)
                {
                    if (renderer_F == null)
                    {
                        vertCnt_F = 0;
                    }
                    else
                    {
                        sharedMesh_F = renderer_F.sharedMesh;

                        if (sharedMesh_F != null)
                        {
                            vertCnt_F = sharedMesh_F.vertexCount;
                        }
                        else
                        {
                            vertCnt_F = 0;
                            EditorUtility.DisplayDialog("Error", "No Mesh Object found on SkinnedMeshRenderer component", "OK");
                            return;
                        }
                    }
                }

                if (sharedMesh_F != null)
                {
                    GUILayout.Label("vertex count:" + vertCnt_F);
                }
            }
        }
    }
}
