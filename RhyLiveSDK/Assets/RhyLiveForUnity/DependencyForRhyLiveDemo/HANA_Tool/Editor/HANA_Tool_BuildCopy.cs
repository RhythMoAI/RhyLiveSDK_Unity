using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

/*!
[2021] [kuniyan]
Please read included license

 */

//A:before edit
//B:after edit
namespace HanaBuildCopy
{
    public class HANA_Tool_BuildCopy : EditorWindow
    {
        SkinnedMeshRenderer renderer_A;
        SkinnedMeshRenderer renderer_B;

        Mesh sharedMesh_A;
        Vector3[] vertices_A;
        private Vector3 vecA0;  //correction value
        private Vector3 vecA1;
        private Vector3 vecA2;

        Mesh sharedMesh_B;
        Vector3[] vertices_B;
        private Vector3 vecB0;
        private Vector3 vecB1;
        private Vector3 vecB2;

        Vector3 shiftVec3 = Vector3.zero;
        Vector3 multiVec3 = Vector3.one;

        bool switch_exYandZ;    //exchange Y and Z flag
        bool switch_merge;      //correction value input flag

        bool switch_accuracy;   //increased copy accuracy flag

        List<string> ls_blendShapeList_A;   //blendshape name list
        bool canAccuracyFlag_A;             //can increased copy accuracy flag
        int accuracyblendShape_A_01 = 0;    //blendshape num for increased coppy accuracy
        int accuracyblendShape_A_02 = 0;
        List<string> ls_blendShapeList_B;
        bool canAccuracyFlag_B;
        int accuracyblendShape_B_01 = 0;
        int accuracyblendShape_B_02 = 0;

        Vector2 scrollPos = new Vector2(0, 0);

        [MenuItem("HANA_Tool/BuildCopy")]
        static void CreateWindow()
        {
            GetWindow<HANA_Tool_BuildCopy>("BuildCopy");
        }

        void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            GUILayout.Label("------------------------------------------------------------------------------------------------------------------------");

            //before modification mesh input
            using (var checkScope = new EditorGUI.ChangeCheckScope())
            {
                renderer_A = EditorGUILayout.ObjectField
                    (
                    "Mesh:Before Modification",
                    renderer_A,
                    typeof(SkinnedMeshRenderer),
                    true
                    ) as SkinnedMeshRenderer;

                if (checkScope.changed)
                {
                    if (renderer_A == null)
                    {
                        sharedMesh_A = null;
                    }
                    else
                    {
                        sharedMesh_A = renderer_A.sharedMesh;

                        if (sharedMesh_A != null)
                        {
                            //get vertex
                            vertices_A = new Vector3[sharedMesh_A.vertexCount];

                            for (int i = 0; i < sharedMesh_A.vertexCount; i++)
                            {
                                vertices_A[i] = sharedMesh_A.vertices[i];
                            }

                            setMergeVertice(vertices_A, ref vecA0, ref vecA1, ref vecA2);
                            canAccuracyFlag_A = true;
                        }
                        else
                        {
                            EditorUtility.DisplayDialog("Error", "No Mesh Object found on SkinnedMeshRenderer component", "OK");
                            return;
                        }
                    }
                }
            }
                
            if(sharedMesh_A != null)
            {
                EditorGUI.BeginDisabledGroup(true);
                vecA0 = EditorGUILayout.Vector3Field("vertex:0", vecA0);
                vecA1 = EditorGUILayout.Vector3Field("vertex:1", vecA1);
                vecA2 = EditorGUILayout.Vector3Field("vertex:2", vecA2);
                EditorGUI.EndDisabledGroup();
            }

            GUILayout.Label("------------------------------------------------------------------------------------------------------------------------");

            //after modification mesh input
            using (var checkScope = new EditorGUI.ChangeCheckScope())
            {
                renderer_B = EditorGUILayout.ObjectField
                    (
                    "Mesh:After Modification",
                    renderer_B,
                    typeof(SkinnedMeshRenderer),
                    true
                    ) as SkinnedMeshRenderer;

                if (checkScope.changed)
                {
                    if (renderer_B == null)
                    {
                        sharedMesh_B = null;
                    }
                    else
                    {
                        sharedMesh_B = renderer_B.sharedMesh;

                        if (sharedMesh_B != null)
                        {
                            vertices_B = new Vector3[sharedMesh_B.vertexCount];

                            for (int i = 0; i < sharedMesh_B.vertexCount; i++)
                            {
                                vertices_B[i] = sharedMesh_B.vertices[i];
                            }

                            setMergeVertice(vertices_B, ref vecB0, ref vecB1, ref vecB2);
                            canAccuracyFlag_B = true;

                            switch_exYandZ = false;
                            switch_merge = false;
                            shiftVec3 = Vector3.zero;
                            multiVec3 = Vector3.one;
                        }
                        else
                        {
                            EditorUtility.DisplayDialog("Error", "No Mesh Object found on SkinnedMeshRenderer component", "OK");
                            return;
                        }
                    }
                }
            }

            if (sharedMesh_B != null)
            {
                EditorGUI.BeginDisabledGroup(true);
                vecB0 = EditorGUILayout.Vector3Field("vertex:0", vecB0);
                vecB1 = EditorGUILayout.Vector3Field("vertex:1", vecB1);
                vecB2 = EditorGUILayout.Vector3Field("vertex:2", vecB2);
                EditorGUI.EndDisabledGroup();

                GUILayout.Label("Adjust [Mesh:After Modification] format to be similar to [Mesh:Before Modification] format (does not have to match)");

                if (GUILayout.Button("exchange Y and Z"))
                {
                    exchangeVertices(ref vertices_B);
                    setMergeVertice(vertices_B, ref vecB0, ref vecB1, ref vecB2);
                    switch_exYandZ = !switch_exYandZ;
                }

                GUILayout.Label("Correction value: Input Before's and After's numerical difference (you can input decimals or minus)");
                EditorGUI.BeginDisabledGroup(switch_merge);
                shiftVec3 = EditorGUILayout.Vector3Field("Addition(addition first)", shiftVec3);
                multiVec3 = EditorGUILayout.Vector3Field("Multiplication", multiVec3);
                GUILayout.Label("Warning: Compare each vertex and correct for misalignment by referring to the vertex number of the closest combination");

                if (GUILayout.Button("Apply misalignment correction value"))
                {
                    shiftVertices(ref vertices_B, shiftVec3);
                    if (multiVec3 != Vector3.one)
                    {
                        multiVertices(ref vertices_B, multiVec3);
                    }
                    setMergeVertice(vertices_B, ref vecB0, ref vecB1, ref vecB2);
                    switch_merge = true;
                }

                EditorGUI.EndDisabledGroup();
                GUILayout.Label("Warning: If you enter a misaligned correction value and it does not apply, please press Reset All Changes once and try again");

                if (GUILayout.Button("Reset All Changes"))
                {
                    for (int i = 0; i < sharedMesh_B.vertexCount; i++)
                    {
                        vertices_B[i].Set(sharedMesh_B.vertices[i].x, sharedMesh_B.vertices[i].y, sharedMesh_B.vertices[i].z);
                    }

                    setMergeVertice(vertices_B, ref vecB0, ref vecB1, ref vecB2);

                    switch_exYandZ = false;
                    switch_merge = false;
                    shiftVec3 = Vector3.zero;
                    multiVec3 = Vector3.one;
                }
            }

            GUILayout.Label("------------------------------------------------------------------------------------------------------------------------");

            //Increase copy accuracy
            switch_accuracy = GUILayout.Toggle(switch_accuracy, "Increase copy accuracy");
            if( switch_accuracy )
            {
                GUILayout.Label("If you don't need to use this, uncheck it and close it. It's buggy!");
                GUILayout.Label("Set two sets of Blend Shapes with the same look (Before01=After01, same for 02)");

                if (sharedMesh_A != null)
                {
                    if (canAccuracyFlag_A)
                    {
                        ls_blendShapeList_A = new List<string>();

                        for (int i = 0; i < sharedMesh_A.blendShapeCount; i++)
                        {
                            ls_blendShapeList_A.Add(sharedMesh_A.GetBlendShapeName(i));
                        }

                        canAccuracyFlag_A = false;
                    }

                    accuracyblendShape_A_01 = EditorGUILayout.Popup("BlendShape Before_01",accuracyblendShape_A_01, ls_blendShapeList_A.ToArray());
                    accuracyblendShape_A_02 = EditorGUILayout.Popup("BlendShape Before_02",accuracyblendShape_A_02, ls_blendShapeList_A.ToArray());
                }

                if (sharedMesh_B != null)
                {
                    if (canAccuracyFlag_B)
                    {
                        ls_blendShapeList_B = new List<string>();

                        for (int i = 0; i < sharedMesh_B.blendShapeCount; i++)
                        {
                            ls_blendShapeList_B.Add(sharedMesh_B.GetBlendShapeName(i));
                        }

                        canAccuracyFlag_B = false;
                    }

                    accuracyblendShape_B_01 = EditorGUILayout.Popup("BlendShape After_01",accuracyblendShape_B_01, ls_blendShapeList_B.ToArray());
                    accuracyblendShape_B_02 = EditorGUILayout.Popup("BlendShape After_02",accuracyblendShape_B_02, ls_blendShapeList_B.ToArray());
                }
            }

            //start build copy
            using (new EditorGUI.DisabledScope((renderer_A == null) || (renderer_B == null) || (sharedMesh_A == null) || (sharedMesh_B == null)))
            {
                if (GUILayout.Button("BuildCopy BlendShape"))
                {
                    //nearest num
                    int[] nearNums_base = new int[vertices_A.Length];

                    checkNearVertice( ref nearNums_base, vertices_A, vertices_B);

                    //increase copy accuracy
                    if (switch_accuracy)
                    {
                        int[] nearNums_sub_01 = new int[vertices_A.Length];
                        checkNearBlendShapeVertice(ref nearNums_sub_01, sharedMesh_A, accuracyblendShape_A_01, sharedMesh_B, accuracyblendShape_B_01);
                        int[] nearNums_sub_02 = new int[vertices_A.Length];
                        checkNearBlendShapeVertice(ref nearNums_sub_02, sharedMesh_A, accuracyblendShape_A_02, sharedMesh_B, accuracyblendShape_B_02);

                        majorityNearNums(ref nearNums_base, nearNums_sub_01, nearNums_sub_02);
                    }

                    //generate mesh
                    var mesh_custom = Instantiate<Mesh>(sharedMesh_A);
                    mesh_custom.ClearBlendShapes();

                    //set the shapekey information to generate mesh
                    var frameIndex = 0;
                    Vector3[] vertices, normals, tangents;
                    Vector3[] vertices_temp, normals_temp, tangents_temp;

                    for (int i = 0; i < sharedMesh_B.blendShapeCount; i++)
                    {
                        vertices_temp = new Vector3[sharedMesh_B.vertexCount];
                        normals_temp = new Vector3[sharedMesh_B.vertexCount];
                        tangents_temp = new Vector3[sharedMesh_B.vertexCount];

                        sharedMesh_B.GetBlendShapeFrameVertices(i, frameIndex, vertices_temp, normals_temp, tangents_temp);

                        //exchange Y and Z
                        if (switch_exYandZ)
                        {
                            exchangeVertices(ref vertices_temp);
                            exchangeVertices(ref normals_temp);
                            exchangeVertices(ref tangents_temp);
                        }

                        //merge vectors with misalignment correction values
                        if (switch_merge)
                        {
                            if (multiVec3 != Vector3.one)
                            {
                                multiVertices(ref vertices_temp, multiVec3);
                                multiVertices(ref normals_temp, multiVec3);
                                multiVertices(ref tangents_temp, multiVec3);
                            }
                        }

                        vertices = new Vector3[sharedMesh_A.vertexCount];
                        normals = new Vector3[sharedMesh_A.vertexCount];
                        tangents = new Vector3[sharedMesh_A.vertexCount];

                        for ( int j = 0; j < sharedMesh_A.vertexCount; j++)
                        {
                            vertices[j] = vertices_temp[nearNums_base[j]];
                            normals[j] = normals_temp[nearNums_base[j]];
                            tangents[j] = tangents_temp[nearNums_base[j]];
                        }

                        var blendShapeWeight = sharedMesh_B.GetBlendShapeFrameWeight(i, frameIndex);
                        string s_shapeKeyName = sharedMesh_B.GetBlendShapeName(i);
                        mesh_custom.AddBlendShapeFrame(s_shapeKeyName, blendShapeWeight, vertices, normals, tangents);
                    }

                    Undo.RecordObject(renderer_A, "Renderer " + renderer_A.name);
                    renderer_A.sharedMesh = mesh_custom;

                    var path = Path.GetDirectoryName(AssetDatabase.GetAssetPath(sharedMesh_A)) + "/" + sharedMesh_A.name + "_custom.asset";
                    AssetDatabase.CreateAsset(mesh_custom, AssetDatabase.GenerateUniqueAssetPath(path));
                    AssetDatabase.SaveAssets();

                    EditorUtility.DisplayDialog("log", "Finished buildcopy blendshapes", "OK");
                }
            }

            EditorGUILayout.EndScrollView();
        }

        //get nearest vertex num
        void checkNearVertice( ref int[] nearNums, Vector3[] verticesA, Vector3[] verticesB )
        {
            for (int i = 0; i < vertices_A.Length; i++)
            {
                float magni = 5.0f;
                for (int j = 0; j < vertices_B.Length; j++)
                {
                    Vector3 magVec = vertices_A[i] - vertices_B[j];

                    if(magni > magVec.sqrMagnitude)
                    {
                        nearNums[i] = j;
                        magni = magVec.sqrMagnitude;
                    }
                }
            }
        }

        //get nearest blendshape num
        void checkNearBlendShapeVertice(ref int[] nearNums, Mesh sharedMesh_A, int blendShapeNum_A, Mesh sharedMesh_B, int blendShapeNum_B)
        {
            var frameIndex = 0;

            Vector3[] vertices_A = new Vector3[sharedMesh_A.vertexCount];
            Vector3[] normals_A = new Vector3[sharedMesh_A.vertexCount];
            Vector3[] tangents_A = new Vector3[sharedMesh_A.vertexCount];
            sharedMesh_A.GetBlendShapeFrameVertices(blendShapeNum_A, frameIndex, vertices_A, normals_A, tangents_A);
            Vector3[] vertices_base_A = new Vector3[sharedMesh_A.vertexCount];
            for(int i = 0; i < sharedMesh_A.vertexCount; i++)
            {
                vertices_base_A[i] = sharedMesh_A.vertices[i];
            }

            Vector3[] vertices_B = new Vector3[sharedMesh_B.vertexCount];
            Vector3[] normals_B = new Vector3[sharedMesh_B.vertexCount];
            Vector3[] tangents_B = new Vector3[sharedMesh_B.vertexCount];
            sharedMesh_B.GetBlendShapeFrameVertices(blendShapeNum_B, frameIndex, vertices_B, normals_B, tangents_B);
            Vector3[] vertices_base_B = new Vector3[sharedMesh_B.vertexCount];
            for (int i = 0; i < sharedMesh_B.vertexCount; i++)
            {
                vertices_base_B[i] = sharedMesh_B.vertices[i];
            }

            //exchange Y and Z
            if (switch_exYandZ)
            {
                exchangeVertices(ref vertices_base_B);
                exchangeVertices(ref vertices_B);
            }

            //merge vectors with misalignment correction values
            if (switch_merge)
            {
                if (multiVec3 != Vector3.one)
                {
                    multiVertices(ref vertices_base_B, multiVec3);
                    multiVertices(ref vertices_B, multiVec3);
                }
            }

            for (int i = 0; i < vertices_A.Length; i++)
            {
                float magni = 5.0f;
                for (int j = 0; j < vertices_B.Length; j++)
                {
                    Vector3 magVec = ( vertices_A[i] + vertices_base_A[i] ) - ( vertices_B[j] + vertices_base_B[j] );

                    if (magni > magVec.sqrMagnitude)
                    {
                        nearNums[i] = j;
                        magni = magVec.sqrMagnitude;
                    }
                }
            }
        }

        //increase the accuracy by make a majority vote using nearest vertex data
        void majorityNearNums( ref int[] nearNum_base, int[] nearNum_01, int[] nearNum_02)
        {
            for( int i = 0; i < nearNum_base.Length; i++)
            {
                if( nearNum_base[i] != nearNum_01[i])
                {
                    if(nearNum_01[i] == nearNum_02[i])
                    {
                        nearNum_base[i] = nearNum_01[i];
                    }
                }
            }
        }

        //shift (add) vertices
        void shiftVertices( ref Vector3[] vertices, Vector3 shiftVec3 )
        {
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = vertices[i] + shiftVec3;
            }
        }

        //shift (multi) vertices
        void multiVertices(ref Vector3[] vertices, Vector3 multiVec3)
        {
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i].Set(vertices[i].x * multiVec3.x, vertices[i].y * multiVec3.y, vertices[i].z * multiVec3.z);
            }
        }

        //exchange Y and Z
        void exchangeVertices( ref Vector3[] vertices )
        {
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i].Set(vertices[i].x, vertices[i].z, vertices[i].y);
            }
        }

        //set the correction value input box
        void setMergeVertice( Vector3[] vertices, ref Vector3 vec_0, ref Vector3 vec_1, ref Vector3 vec_2 )
        {
            vec_0 = vertices[0];
            vec_1 = vertices[1];
            vec_2 = vertices[2];
        }
    }
}
