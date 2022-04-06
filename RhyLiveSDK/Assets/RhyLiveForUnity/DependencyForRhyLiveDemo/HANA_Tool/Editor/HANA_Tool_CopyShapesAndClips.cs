using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using UniGLTF;
using VRM;
using System.Linq;

/*!
[2021] [kuniyan]
Please read included license

 */

namespace HanaCopyShapesAndClips
{
    public class HANA_Tool_CopyShapesAndClips : EditorWindow
    {
        //destination
        GameObject targetObj;
        VRMBlendShapeProxy targetBSProxy;
        BlendShapeAvatar targetBSAvator;
        SkinnedMeshRenderer targetRenderer;
        string targetRelativePath;
        //source
        GameObject sourceObj;
        VRMBlendShapeProxy sourceBSProxy;
        BlendShapeAvatar sourceBSAvator;
        SkinnedMeshRenderer sourceRenderer;
        string sourceRelativePath;

        [MenuItem("HANA_Tool/CopyShapesAndClips",false, 62)]
        static void CreateWindow()
        {
            GetWindow<HANA_Tool_CopyShapesAndClips>("CopyShapesAndClips");
        }

        void OnGUI()
        {
            //get destination
            using (var checkScope = new EditorGUI.ChangeCheckScope())
            {
                targetObj = EditorGUILayout.ObjectField("destination avatar",
                    targetObj, typeof(GameObject), true) as GameObject;

                if (checkScope.changed)
                {
                    if (targetObj == null)
                    {
                        ClearTarget();
                    }
                    else
                    {
                        if (!GetObjData(targetObj, ref targetBSProxy, ref targetBSAvator, ref targetRenderer, ref targetRelativePath))
                        {
                            ClearTarget();
                        }
                    }
                }
            }

            //get source
            using (var checkScope = new EditorGUI.ChangeCheckScope())
            {
                sourceObj = EditorGUILayout.ObjectField("source avatar",
                    sourceObj, typeof(GameObject), true) as GameObject;

                if (checkScope.changed)
                {
                    if (sourceObj == null)
                    {
                        ClearSource();
                    }
                    else
                    {
                        if(!GetObjData(sourceObj, ref sourceBSProxy, ref sourceBSAvator, ref sourceRenderer, ref sourceRelativePath))
                        {
                            ClearSource();
                        }
                    }
                }
            }

            GUILayout.Label("------------------------------------------------------------------------------------------------------------------------");
            GUILayout.Label("This tool can use for only VRM avatar");
            GUILayout.Label("Make sure you have the correct destination and source");
            GUILayout.Label("------------------------------------------------------------------------------------------------------------------------");

            using (new EditorGUI.DisabledScope((targetObj == null) || (sourceObj == null)))
            {
                if (GUILayout.Button("Copy Shapes and Clips"))
                {
                    if(targetRenderer.sharedMesh.vertexCount != sourceRenderer.sharedMesh.vertexCount)
                    {
                        EditorUtility.DisplayDialog("Error", "The number of vertices in the avatars do not match", "OK");
                        return;
                    }
                    if(!CopyBlendShapes())
                    {
                        EditorUtility.DisplayDialog("Error", "Failed to copy shapes", "OK");
                        return;
                    }
                    //update prefab
                    string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(targetObj);
                    PrefabUtility.ApplyPrefabInstance(targetObj, InteractionMode.UserAction);

                    if (CopyBlendShapeClips())
                    {
                        EditorUtility.DisplayDialog("Log", "Finished copy shapes and clips", "OK");
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Log", "Failed to copy clips", "OK");
                    }
                }
            }
        }

        private bool CopyBlendShapes()
        {
            var sourceMesh = sourceRenderer.sharedMesh;
            var targetMesh = targetRenderer.sharedMesh;

            if ((sourceMesh == null) || (targetMesh == null) || (sourceMesh == targetMesh))
            {
                EditorUtility.DisplayDialog("Error", "No Mesh found on SkinnedMeshRenderer", "OK");
                return false;
            }

            var customMesh = Instantiate<Mesh>(targetMesh);
            customMesh.ClearBlendShapes();

            Vector3[] vertices = new Vector3[targetMesh.vertexCount];
            Vector3[] normals = new Vector3[targetMesh.vertexCount];
            Vector3[] tangents = new Vector3[targetMesh.vertexCount];

            for (int shapeNum = 0; shapeNum < sourceMesh.blendShapeCount; shapeNum++)
            {
                sourceMesh.GetBlendShapeFrameVertices(shapeNum, 0, vertices, normals, tangents);

                customMesh.AddBlendShapeFrame(sourceMesh.GetBlendShapeName(shapeNum), 100, vertices, normals, tangents);
            }

            Undo.RecordObject(targetRenderer, "Renderer " + targetRenderer.name);
            targetRenderer.sharedMesh = customMesh;

            string path = null;
            string assetPath = AssetDatabase.GetAssetPath(targetMesh);
            path = Path.GetDirectoryName(assetPath) + "/" + "Face.asset";
            AssetDatabase.CreateAsset(customMesh, AssetDatabase.GenerateUniqueAssetPath(path));
            AssetDatabase.SaveAssets();

            return true;
        }

        private bool CopyBlendShapeClips()
        {
            if(sourceBSAvator == targetBSAvator)
            {
                return false;
            }

            //destination's clip remove
            foreach (BlendShapeClip clip in targetBSAvator.Clips)
            {
                var clipPath = AssetDatabase.GetAssetPath(clip);
                AssetDatabase.DeleteAsset(clipPath);
            }
            targetBSAvator.Clips.Clear();

            //copy and add clip
            foreach(BlendShapeClip clip in sourceBSAvator.Clips)
            {
                string sourcePath = UnityPath.FromAsset(clip).Value;
                string targetPath = UnityPath.FromAsset(targetBSAvator).Parent.Child(Path.GetFileName(sourcePath)).Value;
                AssetDatabase.CopyAsset(sourcePath, targetPath);

                BlendShapeClip newClip = AssetDatabase.LoadAssetAtPath<BlendShapeClip>(targetPath);
                List<BlendShapeBinding> newbinds = new List<BlendShapeBinding>();
                foreach(BlendShapeBinding sourceBind in clip.Values)
                {
                    BlendShapeBinding newbind = new BlendShapeBinding();
                    newbind = sourceBind;
                    newbinds.Add(newbind);
                }
                newClip.Values = newbinds.ToArray();

                newClip.MaterialValues = clip.MaterialValues.ToArray();

                targetBSAvator.Clips.Add(newClip);
                EditorUtility.SetDirty(targetBSAvator);
            }

            AssetDatabase.SaveAssets();
            return true;
        }

        private bool GetObjData(GameObject obj, ref VRMBlendShapeProxy BSProxy, ref BlendShapeAvatar BSAvatar, ref SkinnedMeshRenderer renderer, ref string relativePath)
        {
            BSProxy = obj.GetComponent<VRMBlendShapeProxy>();

            if (BSProxy == null)
            {
                EditorUtility.DisplayDialog("Error", "VRMBlendShapeProxy found on GameObject", "OK");
                return false;
            }
            else
            {
                BSAvatar = BSProxy.BlendShapeAvatar;
                if (BSAvatar == null)
                {
                    EditorUtility.DisplayDialog("Error", "No BlendShapeAvatar found on VRMBlendShapeProxy", "OK");
                    return false;
                }
                else if ((BSAvatar.Clips == null) || (BSAvatar.Clips.Count == 0))
                {
                    EditorUtility.DisplayDialog("Error", "No BlendShapeClip found on BlendShapeAvatar", "OK");
                    return false;
                }
                else
                {
                    string meshObjName = null;
                    foreach (BlendShapeClip clip in BSAvatar.Clips)
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
                        EditorUtility.DisplayDialog("Error", "Failed to get Meshes name from BlendShapeClip", "OK");
                    }
                    else
                    {
                        Transform meshTransform = obj.transform.Find(meshObjName);
                        if (meshTransform == null)
                        {
                            EditorUtility.DisplayDialog("Error", "Failed to find Mesh from Meshes name", "OK");
                        }
                        else
                        {
                            renderer = meshTransform.GetComponent<SkinnedMeshRenderer>();
                            if (renderer == null)
                            {
                                EditorUtility.DisplayDialog("Error", "No SkinnedMeshRenderer found on face mesh object", "OK");
                            }
                            else
                            {
                                var sharedMesh = renderer.sharedMesh;
                                if (sharedMesh == null)
                                {
                                    EditorUtility.DisplayDialog("Error", "No Mesh found on SkinnedMeshRenderer", "OK");
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
        }

        private void ClearTarget()
        {
            targetObj = null;
            targetBSProxy = null;
            targetBSAvator = null;
            targetRenderer = null;
            targetRelativePath = null;
        }

        private void ClearSource()
        {
            sourceObj = null;
            sourceBSProxy = null;
            sourceBSAvator = null;
            sourceRenderer = null;
            sourceRelativePath = null;
        }
    }
}

