using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

/*!
[2021] [kuniyan]
Please read included license

Reference: gatosyocora MeshDeleterWithTexture
 */

namespace HANADeleteTransparentMesh
{
    public class HANA_Tool_DeleteTransparentMesh : EditorWindow
    {
        [MenuItem("HANA_Tool/DeleteTransparentMesh")]
        static void CreateWindow()
        {
            GetWindow<HANA_Tool_DeleteTransparentMesh>("HANA_Tool_DeleteTransparentMesh");
        }

        GameObject obj;
        List<SkinnedMeshRenderer> renderers;

        void OnGUI()
        {
            using (var checkScope = new EditorGUI.ChangeCheckScope())
            {

                obj = EditorGUILayout.ObjectField("Avatar",
                    obj, typeof(GameObject), true) as GameObject;

                if (checkScope.changed)
                {
                    if(renderers != null)
                    {
                        renderers.Clear();
                    }
                    renderers = null;
                    renderers = new List<SkinnedMeshRenderer>();

                    GetSkinnedMeshRenderer(obj);

                    if (renderers.Count == 0)
                    {
                        Init();
                        EditorUtility.DisplayDialog("Error", "No Mesh Object found on SkinnedMeshRenderer component", "OK");
                    }
                }
            }

            GUILayout.Label("Removes polygons in areas where the avatar's texture is transparent");

            using (new EditorGUI.DisabledScope((renderers == null) || (renderers.Count == 0)))
            {
                if (GUILayout.Button("DeleteTransparentMesh"))
                {
                    for (int i = 0; i < renderers.Count; i++)
                    {
                        DeleteTransparentMesh(renderers[i]);
                    }

                    EditorUtility.DisplayDialog("Log", "Finished to delete polygons", "OK");
                }
            }
        }

        //Find SkinnedMeshRenderer recursively.
        void GetSkinnedMeshRenderer(GameObject obj)
        {
            if((renderers == null) || (obj == null))
            {
                return;
            }

            var renderer = obj.GetComponent<SkinnedMeshRenderer>();
            if(renderer != null)
            {
                renderers.Add(renderer);
            }

            for (int i = 0; i < obj.transform.childCount; i++)
            {
                GetSkinnedMeshRenderer(obj.transform.GetChild(i).gameObject);
            }
        }

        void DeleteTransparentMesh(SkinnedMeshRenderer renderer)
        {
            var materials = renderer.sharedMaterials;
            var sharedMesh = renderer.sharedMesh;
            if(materials.Length != sharedMesh.subMeshCount)
            {
                //Mismatch between the number of materials and the number of submeshes
                return;
            }

            var textures = new Texture2D[materials.Length];
            for (int matNum = 0; matNum < materials.Length; matNum++)
            {
                textures[matNum] = GetMainTexture(materials[matNum]);
            }

            var uv1 = sharedMesh.uv.ToList();
            var vertices = sharedMesh.vertices.ToList();
            bool[] vertDeleteFlags = new bool[vertices.Count];
            for(int i = 0; i < vertDeleteFlags.Length; i++)
            {
                vertDeleteFlags[i] = true;
            }

            List<bool[]> triDeleteFlagsList = new List<bool[]>();
            for(int matNum = 0; matNum < materials.Length; matNum++)
            {
                if(textures[matNum] == null)
                {
                    continue;
                }

                var texture = CreateReadabeTexture2D(textures[matNum]);

                //Look for colored pixels in the polygon, if not, the polygon can be erased.
                int[] tris = sharedMesh.GetTriangles(matNum);
                bool[] triDeleteFlags = new bool[tris.Length];
                for (int triIndex = 0; triIndex < tris.Length; triIndex += 3)
                {
                    //Initialize polygon delete flag
                    triDeleteFlags[triIndex] = true;
                    triDeleteFlags[triIndex + 1] = true;
                    triDeleteFlags[triIndex + 2] = true;

                    //Obtain polygon vertex map. If the UV is a repeat shader, it will be more than 1.0, so only the decimal point is obtained to determine the texture position.
                    Vector2 tri_A = uv1[tris[triIndex]];
                    if(tri_A.x > 1.0f) tri_A.x = tri_A.x % 1;
                    if (tri_A.y > 1.0f) tri_A.y = tri_A.y % 1;
                    Vector2 tri_B = uv1[tris[triIndex + 1]];
                    if (tri_B.x > 1.0f) tri_B.x = tri_B.x % 1;
                    if (tri_B.y > 1.0f) tri_B.y = tri_B.y % 1;
                    Vector2 tri_C = uv1[tris[triIndex + 2]];
                    if (tri_C.x > 1.0f) tri_C.x = tri_C.x % 1;
                    if (tri_C.y > 1.0f) tri_C.y = tri_C.y % 1;

                    //Scaling by minimum and maximum maps of triangles since searching all textures is heavy.
                    var lowX = Mathf.FloorToInt(GetSmallest(tri_A.x, tri_B.x, tri_C.x) * texture.width);
                    var lowY = Mathf.FloorToInt(GetSmallest(tri_A.y, tri_B.y, tri_C.y) * texture.height);
                    if (lowX < 0) lowX = 0;
                    if (lowY < 0) lowY = 0;
                    var highX = Mathf.CeilToInt(GetBiggest(tri_A.x, tri_B.x, tri_C.x) * texture.width);
                    var highY = Mathf.CeilToInt(GetBiggest(tri_A.y, tri_B.y, tri_C.y) * texture.height);
                    if (texture.width < highX) highX = texture.width;
                    if (texture.height < highY) highY = texture.height;

                    //Find pixels in a texture
                    for (int x = lowX; x < highX; x++)
                    {
                        for(int y = lowY; y < highY; y++)
                        {
                            //If the alpha is Transparent, skip it.
                            if (texture.GetPixel(x, y).a == 0)
                            {
                                continue;
                            }

                            //Adjust to uv map
                            float texX = (float)x / texture.width;
                            float texY = (float)y / texture.height;
                            var texPos = new Vector2(texX, texY);

                            //Check if the maps are inside a triangle.
                            if (CheckVertInTriangle(texPos, tri_A, tri_B, tri_C))
                            {
                                triDeleteFlags[triIndex] = false;
                                triDeleteFlags[triIndex + 1] = false;
                                triDeleteFlags[triIndex + 2] = false;
                                break;
                            }
                        }

                        if(!triDeleteFlags[triIndex])
                        {
                            break;
                        }
                    }
                }
                triDeleteFlagsList.Add(triDeleteFlags);

                //Once the delete flag is set, check to see if the target vertex is being used by another polygon.
                for (int triIndex = 0; triIndex < tris.Length; triIndex += 3)
                {
                    if(triDeleteFlags[triIndex])
                    {
                        if(!CheckIndexInArrayCanDelete(tris[triIndex], tris, triDeleteFlags) ||
                            !CheckIndexInArrayCanDelete(tris[triIndex + 1], tris, triDeleteFlags) ||
                                !CheckIndexInArrayCanDelete(tris[triIndex + 2], tris, triDeleteFlags))
                        {
                            //Vertex that should not be deleted.
                            if (tris[triIndex] < vertDeleteFlags.Length)
                            {
                                vertDeleteFlags[tris[triIndex]] = false;
                            }
                            if (tris[triIndex + 1] < vertDeleteFlags.Length)
                            {
                                vertDeleteFlags[tris[triIndex + 1]] = false;
                            }
                            if (tris[triIndex + 2] < vertDeleteFlags.Length)
                            {
                                vertDeleteFlags[tris[triIndex + 2]] = false;
                            }
                        }
                    }
                    else
                    {
                        //Vertex that should not be deleted.
                        if (tris[triIndex] < vertDeleteFlags.Length)
                        {
                            vertDeleteFlags[tris[triIndex]] = false;
                        }
                        if (tris[triIndex + 1] < vertDeleteFlags.Length)
                        {
                            vertDeleteFlags[tris[triIndex + 1]] = false;
                        }
                        if (tris[triIndex + 2] < vertDeleteFlags.Length)
                        {
                            vertDeleteFlags[tris[triIndex + 2]] = false;
                        }
                    }
                }

                EditorUtility.DisplayProgressBar("Progress_SetDeleteFlag",
                    "Texture:" + matNum + "(" +
                    Mathf.Floor((float)(matNum + 1) / materials.Length * 100) + "%)", (float)(matNum + 1) / materials.Length);
            }

            //Delete a vertex
            var mesh_custom = Instantiate<Mesh>(sharedMesh);
            mesh_custom.Clear();
            mesh_custom.MarkDynamic();

            var boneWeights = sharedMesh.boneWeights.ToList();
            var normals = sharedMesh.normals.ToList();
            var tangents = sharedMesh.tangents.ToList();
            var uv2 = sharedMesh.uv.ToList();
            var uv3 = sharedMesh.uv.ToList();
            var uv4 = sharedMesh.uv.ToList();

            var nonDeleteVertices = vertices.Where((v, index) => vertDeleteFlags[index] == false).ToList();
            var nonDeleteWeights = boneWeights.Where((v, index) => vertDeleteFlags[index] == false).ToArray();
            var nonDeleteNormals = normals.Where((v, index) => vertDeleteFlags[index] == false).ToArray();
            var nonDeleteTangents = tangents.Where((v, index) => vertDeleteFlags[index] == false).ToArray();
            var nonDeleteUV = uv1.Where((v, index) => vertDeleteFlags[index] == false).ToList();
            var nonDeleteUV2 = uv2.Where((v, index) => vertDeleteFlags[index] == false).ToList();
            var nonDeleteUV3 = uv3.Where((v, index) => vertDeleteFlags[index] == false).ToList();
            var nonDeleteUV4 = uv4.Where((v, index) => vertDeleteFlags[index] == false).ToList();

            mesh_custom.SetVertices(nonDeleteVertices);
            mesh_custom.boneWeights = nonDeleteWeights;
            mesh_custom.normals = nonDeleteNormals;
            mesh_custom.tangents = nonDeleteTangents;
            mesh_custom.SetUVs(0, nonDeleteUV);
            mesh_custom.SetUVs(1, nonDeleteUV2);
            mesh_custom.SetUVs(2, nonDeleteUV3);
            mesh_custom.SetUVs(3, nonDeleteUV4);

            //Process polygons per submesh
            mesh_custom.subMeshCount = sharedMesh.subMeshCount;

            //Vertex number rearrangement
            int[] newVertNums = new int[sharedMesh.vertexCount];
            int vertNumCnt = 0;
            for(int i = 0; i < sharedMesh.vertexCount; i++)
            {
                if(vertDeleteFlags[i] == true)
                {
                    newVertNums[i] = -1;
                }
                else
                {
                    newVertNums[i] = vertNumCnt;
                    vertNumCnt++;
                }
            }

            for (int subMeshIndex = 0; subMeshIndex < sharedMesh.subMeshCount; subMeshIndex++)
            {
                var subMeshTriangles = sharedMesh.GetTriangles(subMeshIndex);
                var triDeleteFlags = triDeleteFlagsList[subMeshIndex];

                for (int i = 0; i < subMeshTriangles.Count(); i += 3)
                {
                    //If any one of the three vertices of a polygon is deleted, delete that polygon.
                    if (triDeleteFlags[i] || triDeleteFlags[i + 1] || triDeleteFlags[i + 2])
                    {
                        subMeshTriangles[i] = -1;
                        subMeshTriangles[i + 1] = -1;
                        subMeshTriangles[i + 2] = -1;
                    }
                    else
                    {
                        subMeshTriangles[i] = newVertNums[subMeshTriangles[i]];
                        subMeshTriangles[i + 1] = newVertNums[subMeshTriangles[i + 1]];
                        subMeshTriangles[i + 2] = newVertNums[subMeshTriangles[i + 2]];
                    }
                }

                //Delete unnecessary polygons.
                var triangleList = subMeshTriangles.Where(v => v != -1).ToArray();
                mesh_custom.SetTriangles(triangleList, subMeshIndex);

                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayProgressBar("Progress_DeleteVerticesAndTriangles",
                    Mathf.Floor((float)(subMeshIndex + 1 ) / sharedMesh.subMeshCount * 100) + "%", (float)( subMeshIndex + 1 ) / sharedMesh.subMeshCount);
            }

            //Set the BlendShape
            string blendShapeName;
            float frameWeight;
            var blendVertices = new Vector3[sharedMesh.vertexCount];
            var blendNormals = new Vector3[sharedMesh.vertexCount];
            var blendTangents = new Vector3[sharedMesh.vertexCount];
            for (int blendshapeIndex = 0; blendshapeIndex < sharedMesh.blendShapeCount; blendshapeIndex++)
            {
                blendShapeName = sharedMesh.GetBlendShapeName(blendshapeIndex);
                frameWeight = sharedMesh.GetBlendShapeFrameWeight(blendshapeIndex, 0);

                sharedMesh.GetBlendShapeFrameVertices(blendshapeIndex, 0, blendVertices, blendNormals, blendTangents);

                var nonDeleteBlendVerteices = blendVertices.Where((value, index) => vertDeleteFlags[index] == false).ToArray();
                var nonDeleteBlendNormals = blendNormals.Where((value, index) => vertDeleteFlags[index] == false).ToArray();
                var nonDeleteBlendTangents = blendTangents.Where((value, index) => vertDeleteFlags[index] == false).ToArray();

                mesh_custom.AddBlendShapeFrame(blendShapeName, frameWeight, nonDeleteBlendVerteices, nonDeleteBlendNormals, nonDeleteBlendTangents);
            }

            EditorUtility.ClearProgressBar();

            //The set of Mesh.
            Undo.RecordObject(renderer, "Renderer " + renderer.name);
            renderer.sharedMesh = mesh_custom;

            string createAssetPath = null;
            createAssetPath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(sharedMesh)) + "/" + sharedMesh.name + "_custom.asset";
            AssetDatabase.CreateAsset(mesh_custom, AssetDatabase.GenerateUniqueAssetPath(createAssetPath));
            AssetDatabase.SaveAssets();

            triDeleteFlagsList.Clear();
            triDeleteFlagsList = null;
        }

        Texture2D GetMainTexture(Material mat)
        {
            var texture = mat.mainTexture as Texture2D;

            //If the shader does not contain a _MainTex, it will be null.
            //Get the Texture that is set at the beginning of the Material.
            if (texture == null)
            {
                var propertyNum = ShaderUtil.GetPropertyCount(mat.shader);
                string propertyName;

                //Get the texture from the shader property.
                for (int index = 0; index < propertyNum; index++)
                {
                    propertyName = ShaderUtil.GetPropertyName(mat.shader, index);
                    texture = mat.GetTexture(propertyName) as Texture2D;

                    if (texture != null)
                    {
                        break;
                    }
                }
            }

            return texture;
        }

        //Check if the coordinates are inside a triangle.
        bool CheckVertInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            Vector2 ab = b - a;
            Vector2 bp = p - b;

            Vector2 bc = c - b;
            Vector2 cp = p - c;

            Vector2 ca = a - c;
            Vector2 ap = p - a;

            //cross product
            double c1 = ab.x * bp.y - bp.x * ab.y;
            double c2 = bc.x * cp.y - cp.x * bc.y;
            double c3 = ca.x * ap.y - ap.x * ca.y;

            if((c1 > 0 && c2 > 0 && c3 > 0) || (c1 < 0 && c2 < 0 && c3 < 0))
            {   //The coordinates are inside the triangle.
                return true;
            }

            return false;
        }

        //Check if the vertex number to be deleted has not been flagged for deletion by another polygon.
        bool CheckIndexInArrayCanDelete(int num, int[] tris, bool[] flags)
        {
            for (int i = 0; i < tris.Length; i++)
            {
                if (num == tris[i])
                {
                    if (!flags[i])
                    {   //A vertex that is used elsewhere and is not subject to deletion
                        return false;
                    }
                }
            }
            //Not used elsewhere, or flagged for deletion elsewhere.
            return true;
        }

        //Make Texture2d readable
        Texture2D CreateReadabeTexture2D(Texture2D texture2d)
        {
            RenderTexture renderTexture = RenderTexture.GetTemporary(
                        texture2d.width,
                        texture2d.height,
                        0,
                        RenderTextureFormat.Default,
                        RenderTextureReadWrite.Linear);

            Graphics.Blit(texture2d, renderTexture);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTexture;
            Texture2D readableTextur2D = new Texture2D(texture2d.width, texture2d.height);
            readableTextur2D.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            readableTextur2D.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTexture);
            return readableTextur2D;
        }

        void Init()
        {
            obj = null;
            if(renderers != null)
            {
                renderers.Clear();
            }
            renderers = null;
        }

        float GetSmallest(float a, float b, float c)
        {
            if (a < b)
            {
                if (a < c)
                {
                    return a;
                }
                else
                {
                    return c;
                }
            }
            else if (b < c)
            {
                return b;
            }
            else
            {
                return c;
            }
        }

        float GetBiggest(float a, float b, float c)
        {
            if (a < b)
            {
                if (b < c)
                {
                    return c;
                }
                else
                {
                    return b;
                }
            }
            else if (a < c)
            {
                return c;
            }
            else
            {
                return a;
            }
        }
    }
}
