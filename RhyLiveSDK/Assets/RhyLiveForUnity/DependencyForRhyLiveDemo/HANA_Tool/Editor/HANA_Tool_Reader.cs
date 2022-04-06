using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

/*!
[2020] [kuniyan]
Please read included license

 */

namespace HanaReader
{
    public class BlendShapesFiles
    {
        public List<List<BSData>> l_bsDatas;
        public List<string> l_fileNames;
        private static BlendShapesFiles instance;
        public static BlendShapesFiles Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new BlendShapesFiles();
                    instance.ReadData();
                }

                return instance;
            }
        }

        public bool ReadData()
        {
            l_bsDatas = new List<List<BSData>>();
            l_fileNames = new List<string>();
            string filePath = Application.dataPath + "/HANA_Tool/BlendShapeData/";
            if (Directory.Exists(filePath))
            {
                DirectoryInfo dir = new DirectoryInfo(filePath);
                FileInfo[] fileInfos = dir.GetFiles("*.txt");

                if (fileInfos.Length != 0)
                {
                    foreach (FileInfo info in fileInfos)
                    {
                        var reader = new StreamReader(info.OpenRead());
                        var json = reader.ReadToEnd();
                        ListSerialize<BSData> listSerialize = JsonUtility.FromJson<ListSerialize<BSData>>(json);

                        if (listSerialize != null)
                        {
                            l_bsDatas.Add(listSerialize.blendShapeDatas);
                            l_fileNames.Add(info.Name);

                            reader.Close();
                        }
                        else
                        {
                            reader.Close();

                            EditorUtility.DisplayDialog("Error", "Failed to load JSON file", "OK");

                            return false;
                        }
                    }
                    return true;
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", "Failed to load BlendShapeData folder", "OK");
                }
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Can't find the BlendShapeData folder", "OK");
            }
            return false;
        }

        public void ClearInstance()
        {
            if (instance != null)
            {
                if (instance.l_bsDatas != null)
                {
                    foreach (List<BSData> l_bsData in instance.l_bsDatas)
                    {
                        l_bsData.Clear();
                    }
                    instance.l_bsDatas.Clear();
                }

                if (instance.l_fileNames != null)
                {
                    instance.l_fileNames.Clear();
                }

                instance = null;
            }

            if(l_bsDatas != null)
            {
                foreach (List<BSData> l_bsData in l_bsDatas)
                {
                    l_bsData.Clear();
                }
                l_bsDatas.Clear();
            }

            if(l_fileNames != null)
            {
                l_fileNames.Clear();
            }
        }
    }

    public class HANA_Tool_Reader : EditorWindow
    {
        BlendShapesFiles bsFiles;
        SkinnedMeshRenderer renderer;
        int fileSelectIndex;
        bool meshName_Face = false;

        Vector2 scrollPos = new Vector2(0, 0);

        enum SelectType
        {
            Add,
            Exchange
        }

        SelectType e_selectType = SelectType.Add;
        public static GUIContent[] tabSelect
        {
            get
            {
                return System.Enum.GetNames(typeof(SelectType)).
                    Select(x => new GUIContent(x)).ToArray();
            }
        }

        [MenuItem("HANA_Tool/Reader", false, 2)]
        static void CreateWindow()
        {
            GetWindow<HANA_Tool_Reader>("HANA_Tool_Reader");
        }

        private void OnEnable()
        {
            bsFiles = BlendShapesFiles.Instance;
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
                    if (renderer != null)
                    {
                        var sharedMesh = renderer.sharedMesh;

                        if (sharedMesh == null)
                        {
                            EditorUtility.DisplayDialog("Error", "No Mesh Object found on SkinnedMeshRenderer component", "OK");
                            return;
                        }
                    }
                }
            }


            if (GUILayout.Button("Update Files"))
            {
                bsFiles.ClearInstance();
                bsFiles = BlendShapesFiles.Instance;
            }

            if (bsFiles != null)
            {
                scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

                fileSelectIndex = GUILayout.SelectionGrid(fileSelectIndex, bsFiles.l_fileNames.ToArray(), 1, EditorStyles.radioButton);

                EditorGUILayout.EndScrollView();

                e_selectType = (SelectType)GUILayout.
                Toolbar((int)e_selectType, tabSelect, "LargeButton", GUI.ToolbarButtonSize.Fixed);

                meshName_Face = GUILayout.Toggle(meshName_Face, "Change mesh name to [Face]");

                using ( new EditorGUI.DisabledScope((renderer == null) || (bsFiles.l_bsDatas.Count == 0)) )
                {
                    if (GUILayout.Button("Read BlendShapes"))
                    {
                        var sharedMesh = renderer.sharedMesh;
                        if (sharedMesh == null)
                        {
                            EditorUtility.DisplayDialog("Error", "No Mesh Object found on SkinnedMeshRenderer component", "OK");
                            return;
                        }

                        List<BSData> bSDatas = bsFiles.l_bsDatas[fileSelectIndex];

                        foreach(BSData data in bSDatas)
                        {
                            if( (data.shapeKeyName == null) || (data.blendShapeWeight == 0) || (data.vertexCount == 0) ||
                                (data.elements == null) || (data.v3_vertices == null) )
                            {
                                EditorUtility.DisplayDialog("Error", "The data in the specified file does not fit", "OK");
                                return;
                            }
                        }

                        if (bSDatas[0].vertexCount != sharedMesh.vertexCount)
                        {
                            EditorUtility.DisplayDialog("Error", "The number of vertices in the file does not match the number of vertices in the avatar\nIt is likely not to work properly", "Interruption");
                            return;
                        }

                        var mesh_custom = Instantiate<Mesh>(sharedMesh);

                        ChangeBlendShapes(sharedMesh, mesh_custom, bSDatas, e_selectType);

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


                        EditorUtility.DisplayDialog("log", "Finished reading blendshapes and added them to the mesh", "OK");
                    }
                }
            }
        }

        private void ChangeBlendShapes( Mesh sharedMesh, Mesh customMesh, List<BSData> bsDatas, SelectType e_selectType)
        {
            if (e_selectType == SelectType.Exchange)
            {
                customMesh.ClearBlendShapes();
            }

            List<string> ls_bsName = new List<string>();

            for (int i = 0; i < customMesh.blendShapeCount; i++)
            {
                ls_bsName.Add(customMesh.GetBlendShapeName(i));
            }

            for (int shapeDataCount = 0; shapeDataCount < bsDatas.Count; shapeDataCount++)
            {
                BSData bsData = bsDatas[shapeDataCount];
                int elementNum = 0;

                Vector3[] v3_vertices = new Vector3[sharedMesh.vertexCount];
                Vector3[] v3_normals = new Vector3[sharedMesh.vertexCount];
                Vector3[] v3_tangents = new Vector3[sharedMesh.vertexCount];


                for (int i = 0; i < sharedMesh.vertexCount; i++)
                {
                    if ((elementNum < bsData.elements.Length) && (i == bsData.elements[elementNum]))
                    {
                        v3_vertices[i] = bsData.v3_vertices[elementNum];
                        v3_normals[i] = Vector3.zero;
                        v3_tangents[i] = Vector3.zero;

                        elementNum++;
                    }
                    else
                    {
                        v3_vertices[i] = Vector3.zero;
                        v3_normals[i] = Vector3.zero;
                        v3_tangents[i] = Vector3.zero;
                    }
                }

                if (!ls_bsName.Contains(bsData.shapeKeyName))
                {
                    ls_bsName.Add(bsData.shapeKeyName);
                    customMesh.AddBlendShapeFrame(bsData.shapeKeyName, bsData.blendShapeWeight, v3_vertices, v3_normals, v3_tangents);
                }
            }
        }

        private void OnDisable()
        {
            bsFiles.ClearInstance();
        }
    }
}
