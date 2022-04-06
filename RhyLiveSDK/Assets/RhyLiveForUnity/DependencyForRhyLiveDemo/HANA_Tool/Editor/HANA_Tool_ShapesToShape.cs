using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

using HANA_Previews;

/*!
[2020] [kuniyan]
Please read included license

 */

namespace HanaShapesToShape
{
    public class MyShape
    {
        public string shapeName { get; private set; }
        public float weight;
        public Vector3[] vertices { get; private set; }
        public Vector3[] normals { get; private set; }
        public Vector3[] tangents { get; private set; }

        public MyShape( string name, Mesh mesh, int shapeNum )
        {
            shapeName = name;
            vertices = new Vector3[mesh.vertexCount];
            normals = new Vector3[mesh.vertexCount];
            tangents = new Vector3[mesh.vertexCount];
            mesh.GetBlendShapeFrameVertices(shapeNum, 0, vertices, normals, tangents);
        }

        ~MyShape()
        {
            shapeName = null;
            vertices = null;
            normals = null;
            tangents = null;
        }
    }

    public class MyBlendShape
    {
        public string blendShapeName;
        public List<MyShape> myShapes;

        public MyBlendShape( string name, Mesh mesh )
        {
            blendShapeName = name;
            myShapes = new List<MyShape>();
            if (mesh != null)
            {
                for (int i = 0; i < mesh.blendShapeCount; i++)
                {
                    myShapes.Add(new MyShape(mesh.GetBlendShapeName(i), mesh, i));
                    if (mesh.GetBlendShapeName(i) == name)
                    {
                        myShapes[i].weight = 100.0f;
                    }
                }
            }
        }
        
        ~MyBlendShape()
        {
            blendShapeName = null;
            if (myShapes != null)
            {
                myShapes.Clear();
            }
            myShapes = null;
        }
    }

    public class ShapeConvertSettingWindow : EditorWindow
    {
        GameObject tempObj;
        SkinnedMeshRenderer renderer;
        MyBlendShape blendShape;
        private int previewSize;

        private HANA_Preview preview;

        Vector2 scrollPos = new Vector2(0, 0);

        private void OnEnable()
        {
            minSize = new Vector2Int(480, 480);

            preview = new HANA_Preview();
        }

        public void SetThis( ref List<MyBlendShape> _blendShape, int num, GameObject _obj )
        {
            if( ( _blendShape == null ) || ( _blendShape.Count <= num ) || ( _obj == null ) )
            {
                EditorUtility.DisplayDialog("Error", "The value to initialize the edit screen is invalid", "OK");
                return;
            }

            if (blendShape == null)
            {
                blendShape = _blendShape[num];
            }

            if( tempObj == null )
            { 
                tempObj = Instantiate<GameObject>(_obj);
            }

            renderer = tempObj.GetComponent<SkinnedMeshRenderer>();

            for (int shapeCnt = 0; shapeCnt < blendShape.myShapes.Count; shapeCnt++)
            {
                renderer.SetBlendShapeWeight(shapeCnt, blendShape.myShapes[shapeCnt].weight);
            }

            preview.AddGameObject(tempObj);
        }

        private void OnGUI()
        {
            if ((blendShape == null) || (tempObj == null) || (renderer == null))
            {
                return;
            }

            var rect = new Rect(0, 0, position.width, position.height);
            if(!rect.Contains(Event.current.mousePosition))
            {
                preview.ResetMouseMode();
            }

            blendShape.blendShapeName = EditorGUILayout.TextField("shapeName", blendShape.blendShapeName);

            using (var checkScope = new EditorGUI.ChangeCheckScope())
            {
                previewSize = EditorGUILayout.Popup("Preview Size", previewSize, new string[] { "small", "medium", "large" });

                if (checkScope.changed)
                {
                    ChangePreviewSize(previewSize);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                if(preview.Render())
                {
                    Repaint();
                }

                GUILayout.FlexibleSpace();
            }

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
            for (int i = 0; i < blendShape.myShapes.Count; i++)
            {
                GUILayout.Label(blendShape.myShapes[i].shapeName, GUILayout.Height(16));
            }
            EditorGUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginVertical();

            using (var checkScope = new EditorGUI.ChangeCheckScope())
            {
                for (int i = 0; i < blendShape.myShapes.Count; i++)
                {
                    blendShape.myShapes[i].weight = EditorGUILayout.Slider(blendShape.myShapes[i].weight, -100.0f, 200.0f, GUILayout.Height(16));
                }

                if (checkScope.changed)
                {
                    for (int shapeCnt = 0; shapeCnt < blendShape.myShapes.Count; shapeCnt++)
                    {
                        renderer.SetBlendShapeWeight(shapeCnt, blendShape.myShapes[shapeCnt].weight);
                    }
                }
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("Adjustment completed"))
            {
                Close();
            }
        }

        private void ChangePreviewSize(int sizePtn)
        {
            if(preview == null)
            {
                return;
            }

            switch (previewSize)
            {
                case 1:
                    preview.ChangeTextureSize(480);
                    break;
                case 2:
                    preview.ChangeTextureSize(640);
                    break;
                default:
                    preview.ChangeTextureSize(320);
                    break;
            }
        }

        private void OnDisable()
        {
            renderer = null;
            if (tempObj != null)
            {
                DestroyImmediate(tempObj);
            }
            blendShape = null;
            if(preview != null)
            {
                preview.ClearThis();
            }
        }
    }

    public class HANA_Tool_ShapesToShape : EditorWindow
    {
        GameObject baseObj;
        SkinnedMeshRenderer baseRenderer;
        Mesh baseMesh;
        List<MyBlendShape> tempBlendShapes;
        List<string> baseShapeNames;
        ShapeConvertSettingWindow window;

        bool onToolSysFlag = false;

        Vector2 scrollPos = new Vector2(0, 0);

        [MenuItem("HANA_Tool/ShapesToShape", false, 41)]
        static void CreateWindow()
        {
            GetWindow<HANA_Tool_ShapesToShape>("HANA_Tool_ShapesToShape");
        }

        private void OnEnable()
        {
            
        }

        void OnGUI()
        {
            using (var checkScope = new EditorGUI.ChangeCheckScope())
            {
                baseObj = EditorGUILayout.ObjectField
                    (
                    "SkinnedMeshRenderer",
                    baseObj,
                    typeof(GameObject),
                    true
                    ) as GameObject;

                if (checkScope.changed)
                {
                    if (baseObj == null)
                    {
                        InitThis();
                    }
                    else
                    {
                        baseRenderer = baseObj.GetComponent<SkinnedMeshRenderer>();

                        if (baseRenderer == null)
                        {
                            InitThis();
                            EditorUtility.DisplayDialog("Error", "Uncontained SkinnedMeshRenderer component in Object", "OK");
                            return;
                        }
                        else
                        {
                            baseMesh = baseRenderer.sharedMesh;

                            if (baseMesh == null)
                            {
                                InitThis();
                                EditorUtility.DisplayDialog("Error", "No Mesh Object found on SkinnedMeshRenderer component", "OK");
                                return;
                            }
                            else
                            {
                                if(baseMesh.blendShapeCount == 0)
                                {
                                    InitThis();
                                    EditorUtility.DisplayDialog("Error", "Blendshape is missing in Mesh", "OK");
                                    return;
                                }
                                else
                                {
                                    baseShapeNames = new List<string>();
                                    SetBlendShapeName(ref baseShapeNames, baseMesh);

                                    tempBlendShapes = new List<MyBlendShape>();
                                    SetMyBlendShapes(ref tempBlendShapes, baseMesh);

                                    onToolSysFlag = true;

                                    var previewData = HANA_Preview_Data.Instance();
                                    previewData.transPos = new Vector3(0, 1.5f, 2.0f);
                                    previewData.transRot = Quaternion.Euler(0, 180, 0);
                                }
                            }
                        }
                    }
                }
            }

            if(onToolSysFlag == true)
            {
                scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
                for(int i = 0; i < baseShapeNames.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();

                    GUILayout.Label(baseShapeNames[i]);

                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Edit"))
                    {
                        if (window != null)
                        {
                            window.Close();
                        }

                        window = GetWindow<ShapeConvertSettingWindow>(baseShapeNames[i] + "_Editor");
                        window.SetThis(ref tempBlendShapes, i,  baseObj );
                    }

                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndScrollView();
            }

            using (new EditorGUI.DisabledScope(onToolSysFlag == false) )
            {

                if (GUILayout.Button("Convert BlendShapes"))
                {
                    if(!CheckBlendShapeName(tempBlendShapes))
                    {
                        return;
                    }
                    var customMesh = Instantiate<Mesh>(baseMesh);
                    customMesh.ClearBlendShapes();

                    //add BlendShape
                    for (int shapeCnt = 0; shapeCnt < baseMesh.blendShapeCount; shapeCnt++)
                    {
                        Vector3[] customVert = new Vector3[baseMesh.vertexCount];
                        Vector3[] customNrml = new Vector3[baseMesh.vertexCount];
                        Vector3[] customTang = new Vector3[baseMesh.vertexCount];

                        var tempBlendShape = tempBlendShapes[shapeCnt];

                        //make vertex data
                        for (int vertPos = 0; vertPos < baseMesh.vertexCount; vertPos++)
                        {
                            customVert[vertPos] = Vector3.zero;

                            if (tempBlendShape.myShapes.Count == baseMesh.blendShapeCount)
                            {
                                //Add up the specified vertex number information for each edited blendshape
                                for (int customShapeNum = 0; customShapeNum < tempBlendShape.myShapes.Count; customShapeNum++)
                                {
                                    if (tempBlendShape.myShapes[customShapeNum].vertices.Length == baseMesh.vertexCount)
                                    {
                                        customVert[vertPos] += (tempBlendShape.myShapes[customShapeNum].vertices[vertPos] *
                                            (tempBlendShape.myShapes[customShapeNum].weight / 100.0f));
                                    }
                                }
                            }

                            customNrml[vertPos] = Vector3.zero;
                            customTang[vertPos] = Vector3.zero;
                        }

                        customMesh.AddBlendShapeFrame(tempBlendShape.blendShapeName, 100.0f, customVert, customNrml, customTang);
                    }

                    Undo.RecordObject(baseRenderer, "Renderer " + baseRenderer.name);
                    baseRenderer.sharedMesh = customMesh;

                    var path = Path.GetDirectoryName(AssetDatabase.GetAssetPath(baseMesh)) + "/" + baseMesh.name + "_custom.asset";

                    AssetDatabase.CreateAsset(customMesh, AssetDatabase.GenerateUniqueAssetPath(path));
                    AssetDatabase.SaveAssets();

                    EditorUtility.DisplayDialog("log", "Conversion complete", "OK");
                }
            }
        }

        private void SetMyBlendShapes(ref List<MyBlendShape> blendShapes, Mesh mesh)
        {
            if ((blendShapes == null) || (mesh == null))
            {
                return;
            }

            for (int i = 0; i < mesh.blendShapeCount; i++)
            {
                blendShapes.Add(new MyBlendShape(mesh.GetBlendShapeName(i), mesh));
            }
        }

        private void SetBlendShapeName(ref List<string> names, Mesh mesh)
        {
            if ((names == null) || (mesh == null))
            {
                return;
            }

            for (int i = 0; i < mesh.blendShapeCount; i++)
            {
                names.Add(mesh.GetBlendShapeName(i));
            }
        }

        private bool CheckBlendShapeName(List<MyBlendShape> blendShapes)
        {
            if((blendShapes == null) || (blendShapes.Count == 0))
            {
                EditorUtility.DisplayDialog("Error", "Blendshape is missing or the data is invalid", "OK");
                return false;
            }

            for(int shapeNum = 0; shapeNum < blendShapes.Count; shapeNum++)
            {
                string name = blendShapes[shapeNum].blendShapeName;

                for(int i = shapeNum + 1; i < blendShapes.Count; i++)
                {
                    if(name == blendShapes[i].blendShapeName)
                    {
                        EditorUtility.DisplayDialog("Error", "There is more than one same name in the blendshape after editing", "OK");
                        return false;
                    }
                }
            }
            return true;
        }

        private void OnDisable()
        {
            if (window != null)
            {
                window.Close();
            }
            InitThis();
        }

        private void InitThis()
        {
            baseObj = null;
            baseRenderer = null;
            baseMesh = null;
            if (tempBlendShapes != null)
            {
                tempBlendShapes.Clear();
            }
            if (baseShapeNames != null)
            {
                baseShapeNames.Clear();
            }
            onToolSysFlag = false;
        }
    }
}
