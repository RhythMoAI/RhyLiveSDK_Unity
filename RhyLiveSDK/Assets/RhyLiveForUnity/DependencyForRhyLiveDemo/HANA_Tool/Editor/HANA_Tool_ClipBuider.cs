using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using UniGLTF;
using VRM;
using System.Linq;

using HANA_ClipDatas;

/*!
[2021] [kuniyan]
Please read included license

 */

namespace HanaClipBuilder
{
    public class HANA_Tool_ClipBuilder : EditorWindow
    {
        GameObject baseObj;
        VRMBlendShapeProxy baseBSProxy;
        BlendShapeAvatar baseBSAvator;
        SkinnedMeshRenderer renderer;
        string relativePath;
        enum SelectType
        {
            PerfectSync,
            SRanipal
        }

        SelectType selectType = SelectType.PerfectSync;
        public static GUIContent[] tabSelectType
        {
            get
            {
                return System.Enum.GetNames(typeof(SelectType)).
                    Select(x => new GUIContent(x)).ToArray();
            }
        }
        enum SelectAvatar
        {
            NewVRoid,
            VRoidBeta,
            Other
        }

        SelectAvatar selectAvatar = SelectAvatar.NewVRoid;
        public static GUIContent[] tabSelectAvatar
        {
            get
            {
                return System.Enum.GetNames(typeof(SelectAvatar)).
                    Select(x => new GUIContent(x)).ToArray();
            }
        }

        [MenuItem("HANA_Tool/ClipBuilder", false, 62)]
        static void CreateWindow()
        {
            GetWindow<HANA_Tool_ClipBuilder>("HANA_Tool_ClipBuilder");
        }

        void OnGUI()
        {
            using (var checkScope = new EditorGUI.ChangeCheckScope())
            {
                baseObj = EditorGUILayout.ObjectField("avatar",
                    baseObj, typeof(GameObject), true) as GameObject;

                if (checkScope.changed)
                {
                    if (baseObj == null)
                    {
                        ClearThis();
                    }
                    else
                    {
                        baseBSProxy = baseObj.GetComponent<VRMBlendShapeProxy>();

                        if (baseBSProxy == null)
                        {
                            EditorUtility.DisplayDialog("Error", "No VRMBlendShapeProxy component on GameObject", "OK");
                            ClearThis();
                            return;
                        }
                        else
                        {
                            baseBSAvator = baseBSProxy.BlendShapeAvatar;

                            if(!GetSkinnedMeshRenderer())
                            {
                                ClearThis();
                                return;
                            }
                        }
                    }
                }
            }

            GUILayout.Label("------------------------------------------------------------------------------------------------------------------------");

            selectType = (SelectType)GUILayout.
            Toolbar((int)selectType, tabSelectType, "LargeButton", GUI.ToolbarButtonSize.Fixed);

            GUILayout.Label("------------------------------------------------------------------------------------------------------------------------");
            GUILayout.Label("You can apply default settings for VRoid models");
            selectAvatar = (SelectAvatar)GUILayout.
            Toolbar((int)selectAvatar, tabSelectAvatar, "LargeButton", GUI.ToolbarButtonSize.Fixed);

            GUILayout.Label("------------------------------------------------------------------------------------------------------------------------");
            GUILayout.Label("Warning: This modifies the avatar's prefab (For VRM specification support)");

            using (new EditorGUI.DisabledScope((baseBSAvator == null) || (renderer == null)))
            {
                if (GUILayout.Button("Clip Build"))
                {
                    //update prefab
                    string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(baseObj);
                    PrefabUtility.ApplyPrefabInstance(baseObj, InteractionMode.UserAction);

                    if (ReadSetBlendShapeClips(baseBSAvator))
                    {
                        EditorUtility.DisplayDialog("Log", "Finished to clip build", "OK");
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Log", "Failed to add clips", "OK");
                    }
                }
            }
        }

        private bool ReadSetBlendShapeClips(BlendShapeAvatar baseBSAvator)
        {
            Dictionary<string, string> clipNameTable;
            Dictionary<string, float> clipWeightTable;

            if (selectType == SelectType.SRanipal)
            {
                clipNameTable = HANA_ClipData.clipNameTable_SRanipal;
                clipWeightTable = HANA_ClipData.clipWeightTable_SRanipal;
            }
            else if(selectAvatar == SelectAvatar.NewVRoid)
            {
                clipNameTable = HANA_ClipData.clipNameTable_NewVRoid;
                clipWeightTable = HANA_ClipData.clipWeightTable_def;
            }
            else if (selectAvatar == SelectAvatar.VRoidBeta)
            {
                clipNameTable = HANA_ClipData.clipNameTable_VRoidBeta;
                clipWeightTable = HANA_ClipData.clipWeightTable_def;
            }
            else
            {
                clipNameTable = HANA_ClipData.clipNameTable_def;
                clipWeightTable = HANA_ClipData.clipWeightTable_def;
            }

            var sharedMesh = renderer.sharedMesh;
            string[] newClipNames = clipNameTable.Keys.ToArray();
            foreach(string clipName in newClipNames)
            {
                bool isSameClip = false;
                foreach(BlendShapeClip clip in baseBSAvator.Clips)
                {
                    if(clip.BlendShapeName == clipName)
                    {
                        isSameClip = true;
                        break;
                    }
                }

                if(isSameClip)
                {
                    continue;
                }

                BlendShapeClip newClip = ScriptableObject.CreateInstance<BlendShapeClip>();
                newClip.BlendShapeName = clipName;

                int sameNameNum = 0;
                for(sameNameNum = 0; sameNameNum < sharedMesh.blendShapeCount; sameNameNum++)
                {
                    if( (clipNameTable[clipName] == sharedMesh.GetBlendShapeName(sameNameNum)) ||
                        (clipName == sharedMesh.GetBlendShapeName(sameNameNum)))
                    {
                        break;
                    }
                }

                BlendShapeBinding[] newbinds = new BlendShapeBinding[1];
                BlendShapeBinding newbind = new BlendShapeBinding();
                newbind.RelativePath = relativePath;
                if (sameNameNum < sharedMesh.blendShapeCount)
                {
                    newbind.Index = sameNameNum;
                    if (clipWeightTable.ContainsKey(clipName))
                    {
                        newbind.Weight = clipWeightTable[clipName];
                    }
                    else
                    {
                        newbind.Weight = 0;
                    }
                }
                newbinds[0] = newbind;

                if (selectType == SelectType.SRanipal)
                {
                    var sranipalBinds = GetSRanipalBind(clipName, sharedMesh);
                    if(sranipalBinds != null)
                    {
                        newbinds = sranipalBinds;
                    }
                }

                newClip.Values = newbinds;

                string targetPath = UnityPath.FromAsset(baseBSAvator).Parent.Child("BlendShape." + clipName + ".asset").Value;
                AssetDatabase.CreateAsset(newClip, targetPath);
                AssetDatabase.ImportAsset(targetPath);

                baseBSAvator.Clips.Add(newClip);

                EditorUtility.SetDirty(baseBSAvator);
                AssetDatabase.SaveAssets();
            }

            return true;
        }

        private bool GetSkinnedMeshRenderer()
        {
            if (baseBSAvator == null)
            {
                EditorUtility.DisplayDialog("Error", "No BlendShapeAvatar found on VRMBlendShapeProxy", "OK");
                ClearThis();
                return false;
            }
            else if ((baseBSAvator.Clips == null) || (baseBSAvator.Clips.Count == 0))
            {
                EditorUtility.DisplayDialog("Error", "No BlendShapeClip found on BlendShapeAvatar", "OK");
                ClearThis();
                return false;
            }
            else
            {
                string meshObjName = null;
                foreach (BlendShapeClip clip in baseBSAvator.Clips)
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
                    ClearThis();
                }
                else
                {
                    Transform baseMeshTransform = baseObj.transform.Find(meshObjName);
                    if (baseMeshTransform == null)
                    {
                        EditorUtility.DisplayDialog("Error", "No Mesh Object found", "OK");
                        ClearThis();
                    }
                    else
                    {
                        renderer = baseMeshTransform.GetComponent<SkinnedMeshRenderer>();
                        if (renderer == null)
                        {
                            EditorUtility.DisplayDialog("Error", "No SkinnedMeshRenderer component found on Object", "OK");
                            ClearThis();
                        }
                        else
                        {
                            var sharedMesh = renderer.sharedMesh;
                            if (sharedMesh == null)
                            {
                                EditorUtility.DisplayDialog("Error", "No Mesh Object found on SkinnedMeshRenderer component", "OK");
                                ClearThis();
                            }
                            else
                            {
                                relativePath = meshObjName;
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        BlendShapeBinding[] GetSRanipalBind(string clipName, Mesh sharedMesh)
        {
            BlendShapeBinding[] newbinds = null;
            string[,] sranipalTbl = null;
            if (selectAvatar == SelectAvatar.VRoidBeta)
            {
                sranipalTbl = HANA_ClipData.sranipalBindNames_vroidBeta;
            }
            else if(selectAvatar == SelectAvatar.NewVRoid)
            {
                //Since there is no BlendShapeData that corresponds to the official version of VRoid, we are preparing it temporarily.
                //sranipalTbl = HANA_ClipData.sranipalBindNames_newVRoid;
            }

            if (sranipalTbl != null)
            {
                //Special processing VRoid uses the standard closed eyes as a countermeasure against sinking because each avatar has a different eye size.
                for (int i = 0; i < sranipalTbl.GetLength(0); i++)
                {
                    if (clipName == sranipalTbl[i, 0])
                    {
                        int shapeNum_eye = sharedMesh.GetBlendShapeIndex(sranipalTbl[i, 1]);
                        if (0 <= shapeNum_eye)
                        {
                            if (sranipalTbl.GetLength(1) == 3)
                            {
                                int shapeNum_brow = sharedMesh.GetBlendShapeIndex(sranipalTbl[i, 2]);
                                if (0 <= shapeNum_brow)
                                {
                                    newbinds = new BlendShapeBinding[2];

                                    var newbind_eye = new BlendShapeBinding();
                                    newbind_eye.RelativePath = relativePath;
                                    newbind_eye.Index = shapeNum_eye;
                                    newbind_eye.Weight = 100.0f;
                                    newbinds[0] = newbind_eye;

                                    var newbind_brow = new BlendShapeBinding();
                                    newbind_brow.RelativePath = relativePath;
                                    newbind_brow.Index = shapeNum_brow;
                                    newbind_brow.Weight = 100.0f;
                                    newbinds[1] = newbind_brow;
                                }
                            }
                        }
                    }
                }
            }

            return newbinds;
        }

        private void ClearThis()
        {
            baseObj = null;
            baseBSProxy = null;
            baseBSAvator = null;
            renderer = null;
            relativePath = null;
        }
    }
}

