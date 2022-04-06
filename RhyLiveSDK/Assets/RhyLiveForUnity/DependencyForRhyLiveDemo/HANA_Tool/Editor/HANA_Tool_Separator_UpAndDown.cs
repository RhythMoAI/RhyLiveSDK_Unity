using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

using HANA_Previews;

/*!
[2021] [kuniyan]
Please read included license

 */

namespace HanaSeparatorUpAndDown
{
    public class ShapeSeparatorSettingWindow : EditorWindow
    {
        GameObject _tempObj;
        SkinnedMeshRenderer _renderer;
        List<float> _heightList;
        int _shapeNum = 0;

        GameObject _heightLine;

        private int _previewSize;
        private HANA_Preview _preview;

        Vector2 _scrollPos = new Vector2(0, 0);

        private void OnEnable()
        {
            minSize = new Vector2Int(480, 480);

            _preview = new HANA_Preview();

            _heightLine = Instantiate((GameObject)Resources.Load("HANA_Tool_Resources/Prefabs/HeightLine"));
        }

        public void SetFaceObjToPreviewWindow(int num, GameObject obj, ref List<float> heightList)
        {
            if ((obj == null) || (heightList == null))
            {
                EditorUtility.DisplayDialog("Error", "The value to initialize the edit screen is abnormal", "OK");
                return;
            }

            if(_heightLine == null)
            {
                EditorUtility.DisplayDialog("Error", "Failed to get the object for setting the height", "OK");
                return;
            }

            if (_tempObj == null)
            {
                _tempObj = Instantiate<GameObject>(obj);
            }

            _renderer = _tempObj.GetComponent<SkinnedMeshRenderer>();
            if(_renderer.sharedMesh.blendShapeCount <= num)
            {
                EditorUtility.DisplayDialog("Error", "The value to initialize the edit screen is abnormal", "OK");
                return;
            }

            _shapeNum = num;
            _heightList = heightList;
            _renderer.SetBlendShapeWeight(num, 100.0f);

            _preview.AddGameObject(_tempObj);
            _preview.AddGameObject(_heightLine);
        }

        private void OnGUI()
        {
            if ((_tempObj == null) || (_renderer == null) || (_heightLine == null))
            {
                return;
            }

            var rect = new Rect(0, 0, position.width, position.height);
            if (!rect.Contains(Event.current.mousePosition))
            {
                _preview.ResetMouseMode();
            }

            using (var checkScope = new EditorGUI.ChangeCheckScope())
            {
                _previewSize = EditorGUILayout.Popup("Preview Size", _previewSize, new string[] { "small", "medium", "large" });

                if (checkScope.changed)
                {
                    ChangePreviewSize(_previewSize);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                if (_preview.Render())
                {
                    Repaint();
                }

                GUILayout.FlexibleSpace();
            }

            GUILayout.Label("------------------------------------------------------------------------------------------------------------------------");
            GUILayout.Label("Adjust the slider to adjust the height of the split line(the height of the red surface).");

            _heightList[_shapeNum] = EditorGUILayout.Slider(_heightList[_shapeNum], 0.0f, 3.0f, GUILayout.Height(16));
            _heightLine.transform.position = new Vector3( 0, _heightList[_shapeNum], 0);

            if (GUILayout.Button("Finished adjusting"))
            {
                Close();
            }
        }

        private void ChangePreviewSize(int sizePtn)
        {
            if (_preview == null)
            {
                return;
            }

            switch (_previewSize)
            {
                case 1:
                    _preview.ChangeTextureSize(480);
                    break;
                case 2:
                    _preview.ChangeTextureSize(640);
                    break;
                default:
                    _preview.ChangeTextureSize(320);
                    break;
            }
        }

        private void OnDisable()
        {
            _renderer = null;
            if (_tempObj != null)
            {
                DestroyImmediate(_tempObj);
            }
            if (_preview != null)
            {
                _preview.ClearThis();
            }
            if(_heightLine != null)
            {
                DestroyImmediate(_heightLine);
            }
            _heightList = null;
        }
    }

    public class HANA_Tool_Separator_UpAndDown : EditorWindow
    {
        GameObject _baseObj;
        SkinnedMeshRenderer _baseRenderer;
        Mesh _baseMesh;
        List<string> _baseShapeNameList;
        List<bool> _blendShapeSeparateCheckList;
        List<float> _separateLineHeightList;
        ShapeSeparatorSettingWindow _window;

        bool _onToolSysFlag = false;

        Vector2 _scrollPos = new Vector2(0, 0);

        [MenuItem("HANA_Tool/Separator_UpAndDown", false, 44)]
        static void CreateWindow()
        {
            GetWindow<HANA_Tool_Separator_UpAndDown>("HANA_Tool_Separator_UpAndDown");
        }

        private void OnEnable()
        {

        }

        void OnGUI()
        {
            using (var checkScope = new EditorGUI.ChangeCheckScope())
            {
                _baseObj = EditorGUILayout.ObjectField
                    (
                    "SkinnedMeshRenderer",
                    _baseObj,
                    typeof(GameObject),
                    true
                    ) as GameObject;

                if (checkScope.changed)
                {
                    if (_baseObj == null)
                    {
                        InitThis();
                    }
                    else
                    {
                        _baseRenderer = _baseObj.GetComponent<SkinnedMeshRenderer>();

                        if (_baseRenderer == null)
                        {
                            InitThis();
                            EditorUtility.DisplayDialog("Error", "No SkinnedMeshRenderer found on face mesh object", "OK");
                            return;
                        }
                        else
                        {
                            _baseMesh = _baseRenderer.sharedMesh;

                            if (_baseMesh == null)
                            {
                                InitThis();
                                EditorUtility.DisplayDialog("Error", "No Mesh found on SkinnedMeshRenderer", "OK");
                                return;
                            }
                            else
                            {
                                if (_baseMesh.blendShapeCount == 0)
                                {
                                    InitThis();
                                    EditorUtility.DisplayDialog("Error", "Blendshape is missing in Mesh", "OK");
                                    return;
                                }
                                else
                                {
                                    SetBlendShapeList(_baseMesh);

                                    _onToolSysFlag = true;
                                }
                            }
                        }
                    }
                }
            }

            if (_onToolSysFlag == true)
            {
                _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

                for (int i = 0; i < _baseShapeNameList.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();

                    GUILayout.Label(_baseShapeNameList[i]);

                    GUILayout.FlexibleSpace();
                    _blendShapeSeparateCheckList[i] = GUILayout.Toggle(_blendShapeSeparateCheckList[i], "");
                    if (_blendShapeSeparateCheckList[i] == true)
                    {
                        GUILayout.Label(_separateLineHeightList[i].ToString());

                        if (GUILayout.Button("Edit"))
                        {
                            if (_window != null)
                            {
                                _window.Close();
                            }

                            _window = GetWindow<ShapeSeparatorSettingWindow>(_baseShapeNameList[i] + "_Editor");
                            _window.SetFaceObjToPreviewWindow(i, _baseObj, ref _separateLineHeightList);
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndScrollView();
            }

            using (new EditorGUI.DisabledScope(_onToolSysFlag == false))
            {

                if (GUILayout.Button("Convert BlendShape"))
                {
                    var customMesh = Instantiate<Mesh>(_baseMesh);
                    customMesh.ClearBlendShapes();

                    //Add blendshapes
                    for (int shapeCnt = 0; shapeCnt < _baseMesh.blendShapeCount; shapeCnt++)
                    {
                        var vert = new Vector3[_baseMesh.vertexCount];
                        var nrml = new Vector3[_baseMesh.vertexCount];
                        var tang = new Vector3[_baseMesh.vertexCount];

                        _baseMesh.GetBlendShapeFrameVertices(shapeCnt, 0, vert, nrml, tang);
                        float weight = _baseMesh.GetBlendShapeFrameWeight(shapeCnt, 0);
                        string name = _baseMesh.GetBlendShapeName(shapeCnt);

                        //First, re-set the original BlendShape
                        customMesh.AddBlendShapeFrame(name, weight, vert, nrml, tang);

                        if ((_blendShapeSeparateCheckList[shapeCnt] == true) && (0.0f < _separateLineHeightList[shapeCnt]))
                        {   //Create a BlendShape with top and bottom split if the split checklist is checked.
                            var vert_up = new Vector3[_baseMesh.vertexCount];
                            var nrml_up = new Vector3[_baseMesh.vertexCount];
                            var tang_up = new Vector3[_baseMesh.vertexCount];

                            var vert_dw = new Vector3[_baseMesh.vertexCount];
                            var nrml_dw = new Vector3[_baseMesh.vertexCount];
                            var tang_dw = new Vector3[_baseMesh.vertexCount];

                            for (int vertCnt = 0; vertCnt < _baseMesh.vertexCount; vertCnt++)
                            {
                                if (_separateLineHeightList[shapeCnt] <= (_baseObj.transform.TransformPoint(_baseMesh.vertices[vertCnt] + vert[vertCnt])).y)
                                {   //BlendShape vertex is greater than or equal to the specified value.
                                    vert_up[vertCnt] = vert[vertCnt];
                                    nrml_up[vertCnt] = nrml[vertCnt];
                                    tang_up[vertCnt] = tang[vertCnt];

                                    vert_dw[vertCnt] = Vector3.zero;
                                    nrml_dw[vertCnt] = Vector3.zero;
                                    tang_dw[vertCnt] = Vector3.zero;
                                }
                                else
                                {   //Blendshape vertex is less than specified.
                                    vert_up[vertCnt] = Vector3.zero;
                                    nrml_up[vertCnt] = Vector3.zero;
                                    tang_up[vertCnt] = Vector3.zero;

                                    vert_dw[vertCnt] = vert[vertCnt];
                                    nrml_dw[vertCnt] = nrml[vertCnt];
                                    tang_dw[vertCnt] = tang[vertCnt];
                                }
                            }

                            bool sameName = false;
                            foreach (string shapeName in _baseShapeNameList)
                            {
                                if ((shapeName == name + "_Up") || (shapeName == name + "_Down"))
                                {
                                    sameName = true;
                                    break;
                                }
                            }

                            if (sameName == false)
                            {
                                customMesh.AddBlendShapeFrame(name + "_Up", weight, vert_up, nrml_up, tang_up);
                                customMesh.AddBlendShapeFrame(name + "_Down", weight, vert_dw, nrml_dw, tang_dw);
                            }
                            else
                            {
                                EditorUtility.DisplayDialog("Error", "Cannot convert because the same name as the name with _Up or _Down after splitting already exists", "OK");
                                return;
                            }
                        }
                    }

                    Undo.RecordObject(_baseRenderer, "Renderer " + _baseRenderer.name);
                    _baseRenderer.sharedMesh = customMesh;

                    var path = Path.GetDirectoryName(AssetDatabase.GetAssetPath(_baseMesh)) + "/" + _baseMesh.name + "_custom.asset";

                    AssetDatabase.CreateAsset(customMesh, AssetDatabase.GenerateUniqueAssetPath(path));
                    AssetDatabase.SaveAssets();

                    EditorUtility.DisplayDialog("Log", "Complete Separating", "OK");

                    if (_window != null)
                    {
                        _window.Close();
                    }
                    InitThis();
                }
            }
        }

        private void SetBlendShapeList(Mesh mesh)
        {
            if (mesh == null)
            {
                return;
            }

            _baseShapeNameList = new List<string>();
            _blendShapeSeparateCheckList = new List<bool>();
            _separateLineHeightList = new List<float>();

            for (int i = 0; i < mesh.blendShapeCount; i++)
            {
                _baseShapeNameList.Add(mesh.GetBlendShapeName(i));
                _blendShapeSeparateCheckList.Add(false);
                _separateLineHeightList.Add(0.0f);
            }
        }

        private void OnDisable()
        {
            if (_window != null)
            {
                _window.Close();
            }
            InitThis();
        }

        private void InitThis()
        {
            _baseObj = null;
            _baseRenderer = null;
            _baseMesh = null;
            if (_baseShapeNameList != null)
            {
                _baseShapeNameList.Clear();
            }
            if( _blendShapeSeparateCheckList != null)
            {
                _blendShapeSeparateCheckList.Clear();
            }
            if(_separateLineHeightList != null)
            {
                _separateLineHeightList.Clear();
            }
            _onToolSysFlag = false;
        }
    }
}
