using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

using VRM;

using HANA_ClipDatas;
/*!
[2021] [kuniyan]
Please read included license

 */

namespace HanaClipToShape
{
    public class HANA_Tool_ClipToShape : EditorWindow
    {
        GameObject _baseObj;
        VRMBlendShapeProxy _baseBlendShapeProxy;
        BlendShapeAvatar _baseBlendShapeAvator;
        SkinnedMeshRenderer _renderer;
        string _relativePath;

        Vector2 _scrollPos = new Vector2(0, 0);

        [MenuItem("HANA_Tool/ClipToShape", false, 42)]
        static void CreateWindow()
        {
            GetWindow<HANA_Tool_ClipToShape>("HANA_Tool_ClipToShape");
        }

        void ClearData()
        {
            _baseObj = null;
            _baseBlendShapeProxy = null;
            _baseBlendShapeAvator = null;
            _relativePath = null;
            _renderer = null;
        }
        private bool GetSkinnedMeshRenderer()
        {
            if (_baseBlendShapeAvator == null)
            {
                EditorUtility.DisplayDialog("Error", "No BlendShapeAvatar found on VRMBlendShapeProxy", "OK");
                ClearData();
                return false;
            }
            else if ((_baseBlendShapeAvator.Clips == null) || (_baseBlendShapeAvator.Clips.Count == 0))
            {
                EditorUtility.DisplayDialog("Error", "No BlendShapeClip found on BlendShapeAvatar", "OK");
                ClearData();
                return false;
            }
            else
            {
                string meshObjName = null;
                foreach (BlendShapeClip clip in _baseBlendShapeAvator.Clips)
                {
                    if ((clip.Values != null) && (clip.Values.Length != 0))
                    {
                        meshObjName = clip.Values[0].RelativePath;
                        if (meshObjName != null)
                        {
                            break;
                        }
                    }
                }

                if (meshObjName == null)
                {
                    EditorUtility.DisplayDialog("Error", "Faild to get Meshes name from BlendShapeClip", "OK");
                    ClearData();
                }
                else
                {
                    Transform baseMeshTransform = _baseObj.transform.Find(meshObjName);
                    if (baseMeshTransform == null)
                    {
                        EditorUtility.DisplayDialog("Error", "No Mesh Object found", "OK");
                        ClearData();
                    }
                    else
                    {
                        _renderer = baseMeshTransform.GetComponent<SkinnedMeshRenderer>();
                        if (_renderer == null)
                        {
                            EditorUtility.DisplayDialog("Error", "No SkinnedMeshRenderer component found on Object", "OK");
                            ClearData();
                        }
                        else
                        {
                            _relativePath = meshObjName;
                            return true;
                        }
                    }
                }
            }
            return false;
        }


        void OnGUI()
        {
            using (var checkScope = new EditorGUI.ChangeCheckScope())
            {
                _baseObj = EditorGUILayout.ObjectField("Avatar Object",
                    _baseObj, typeof(GameObject), true) as GameObject;

                if (checkScope.changed)
                {
                    if (_baseObj == null)
                    {
                        ClearData();
                    }
                    else
                    {
                        _baseBlendShapeProxy = _baseObj.GetComponent<VRMBlendShapeProxy>();

                        if (_baseBlendShapeProxy == null)
                        {
                            EditorUtility.DisplayDialog("Error", "No VRMBlendShapeProxy component on GameObject", "OK");
                            ClearData();
                            return;
                        }
                        else
                        {
                            _baseBlendShapeAvator = _baseBlendShapeProxy.BlendShapeAvatar;

                            if (!GetSkinnedMeshRenderer())
                            {
                                ClearData();
                                return;
                            }
                        }
                    }
                }
            }

            GUILayout.Label("------------------------------------------------------------------------------------------------------------------------");
            GUILayout.Label("Once this conversion is made, it cannot be undone.\nBefore using this tool,\nplease save your avatar that adjusted Clips as a backup");
            GUILayout.Label("------------------------------------------------------------------------------------------------------------------------");
            var style = new GUIStyle(EditorStyles.label);
            style.richText = true;
            EditorGUILayout.LabelField("<color=red>*Caution*</color>", style);
            EditorGUILayout.LabelField("<color=red>If you convert with the Clip editing screen displayed in Inspector,</color>", style);
            EditorGUILayout.LabelField("<color=red>it will not be updated</color>", style);
            EditorGUILayout.LabelField("<color=red>Please display a different object in the Inspector before converting</color>", style);
            GUILayout.Label("------------------------------------------------------------------------------------------------------------------------");

            using (new EditorGUI.DisabledScope(_renderer == null))
            {
                if (GUILayout.Button("Convert"))
                {
                    var sharedMesh = _renderer.sharedMesh;
                    if (sharedMesh == null)
                    {
                        EditorUtility.DisplayDialog("Error", "No Mesh Object found on SkinnedMeshRenderer component", "OK");
                        return;
                    }

                    //一度セーブしてクリップの調整値を保存する
                    AssetDatabase.SaveAssets();

                    var customMesh = Instantiate<Mesh>(sharedMesh);
                    customMesh.ClearBlendShapes();

                    var frameIndex = 0;
                    Vector3[] totalVertices, totalNormals, totalTangents;
                    Vector3[] tempVertices, tempNormals, tempTangents;

                    foreach(BlendShapeClip clip in _baseBlendShapeAvator.Clips)
                    {
                        totalVertices = new Vector3[sharedMesh.vertexCount];
                        totalNormals = new Vector3[sharedMesh.vertexCount];
                        totalTangents = new Vector3[sharedMesh.vertexCount];

                        foreach(BlendShapeBinding bind in clip.Values)
                        {
                            tempVertices = new Vector3[sharedMesh.vertexCount];
                            tempNormals = new Vector3[sharedMesh.vertexCount];
                            tempTangents = new Vector3[sharedMesh.vertexCount];

                            sharedMesh.GetBlendShapeFrameVertices(bind.Index, frameIndex, tempVertices, tempNormals, tempTangents);
                            for(int vertPos = 0; vertPos < sharedMesh.vertexCount; vertPos++)
                            {
                                totalVertices[vertPos] += tempVertices[vertPos] * ( bind.Weight / 100.0f);
                                totalNormals[vertPos] += tempNormals[vertPos] * (bind.Weight / 100.0f);
                                totalTangents[vertPos] += tempTangents[vertPos] * (bind.Weight / 100.0f);
                            }
                        }

                        string blendShapeName = clip.BlendShapeName;
                        if(HANA_ClipData.clipNameTable_def.ContainsKey(blendShapeName))
                        {
                            blendShapeName = HANA_ClipData.clipNameTable_def[blendShapeName];
                        }
                        customMesh.AddBlendShapeFrame(blendShapeName, 100.0f, totalVertices, totalNormals, totalTangents);
                    }

                    Undo.RecordObject(_renderer, "Renderer " + _renderer.name);
                    _renderer.sharedMesh = customMesh;

                    string createAssetPath = null;
                    createAssetPath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(sharedMesh)) + "/" + sharedMesh.name + "_custom.asset";

                    AssetDatabase.CreateAsset(customMesh, AssetDatabase.GenerateUniqueAssetPath(createAssetPath));
                    AssetDatabase.SaveAssets();

                    //Update Prefab
                    string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(_baseObj);
                    PrefabUtility.ApplyPrefabInstance(_baseObj, InteractionMode.UserAction);

                    int shapeCnt = 0;
                    foreach (BlendShapeClip clip in _baseBlendShapeAvator.Clips)
                    {
                        BlendShapeBinding[] newbinds = new BlendShapeBinding[1];
                        BlendShapeBinding newbind = new BlendShapeBinding();
                        newbind.RelativePath = _relativePath;
                        newbind.Index = shapeCnt;
                        newbind.Weight = 100.0f;
                        newbinds[0] = newbind;
                        clip.Values = newbinds;
                        EditorUtility.SetDirty(clip);

                        shapeCnt++;
                    }

                    EditorUtility.SetDirty(_baseBlendShapeAvator);
                    AssetDatabase.SaveAssets();

                    EditorUtility.DisplayDialog("Log", "Finished to convert", "OK");

                    ClearData();
                }
            }
        }
    }
}
