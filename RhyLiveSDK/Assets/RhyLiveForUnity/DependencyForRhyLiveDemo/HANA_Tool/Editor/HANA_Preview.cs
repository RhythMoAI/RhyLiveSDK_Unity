
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using System.Linq;
using System.IO;

using Object = UnityEngine.Object;

/*!
[2020] [kuniyan]
Please read included license

 */

namespace HANA_Previews
{
    public class HANA_Preview_Data
    {
        private static HANA_Preview_Data instance;
        public Vector3 transPos;
        public Quaternion transRot;
        public int textureSize;

        private HANA_Preview_Data()
        {
            transPos = new Vector3(0, 1.5f, 1.0f);
            transRot = Quaternion.Euler(0, 180, 0);
            textureSize = 320;
        }

        public static HANA_Preview_Data Instance()
        {
            if (instance == null)
            {
                instance = new HANA_Preview_Data();
            }
            return instance;
        }
    }

    public class HANA_Preview
    {
        public Scene scene { get; private set; }
        public GameObject cameraObj;
        public Camera camera { get; private set; }
        public RenderTexture renderTexture { get; private set; }

        private HANA_Preview_Data previewData;
        public GameObject lightObj { get; private set; }
        public Vector2 mouseOldPos { get; private set; }

        private enum MOUSE_MOVE
        {
            MOUSE_MOVE_NON,
            LEFT_DRAG,
            RIGHT_DRAG,
            CENTER_DRAG
        }
        private MOUSE_MOVE mouseMode = MOUSE_MOVE.MOUSE_MOVE_NON;

        public HANA_Preview()
        {
            scene = EditorSceneManager.NewPreviewScene();

            cameraObj = new GameObject("Camera", typeof(Camera));
            if (previewData == null)
            {
                previewData = HANA_Preview_Data.Instance();
            }
            cameraObj.transform.position = previewData.transPos;
            cameraObj.transform.rotation = previewData.transRot;
            camera = cameraObj.GetComponent<Camera>();
            camera.cameraType = CameraType.Preview;
            camera.forceIntoRenderTexture = true;
            camera.fieldOfView = 40;
            camera.scene = scene;
            camera.enabled = false;
            SceneManager.MoveGameObjectToScene(cameraObj, scene);

            lightObj = new GameObject("Directional Light", typeof(Light));
            lightObj.transform.rotation = Quaternion.Euler(50, -30, 0);
            var light = lightObj.GetComponent<Light>();
            light.type = LightType.Directional;
            SceneManager.MoveGameObjectToScene(lightObj, scene);
        }

        public bool Render()
        {
            if ((cameraObj == null) || (camera == null))
            {
                return false;
            }

            if (previewData == null)
            {
                previewData = HANA_Preview_Data.Instance();
            }

            if (renderTexture == null)
            {
                renderTexture = new RenderTexture(previewData.textureSize, previewData.textureSize, 32);
            }

            camera.targetTexture = renderTexture;

            var rect = GUILayoutUtility.GetRect(previewData.textureSize, previewData.textureSize);

            var evt = Event.current;
            if (rect.Contains(evt.mousePosition))
            {
                switch (evt.type)
                {
                    case EventType.MouseDown:
                        switch (evt.button)
                        {
                            case 1:
                                mouseOldPos = evt.mousePosition;
                                mouseMode = MOUSE_MOVE.RIGHT_DRAG;
                                break;
                            case 2:
                                mouseOldPos = evt.mousePosition;
                                mouseMode = MOUSE_MOVE.CENTER_DRAG;
                                break;
                            default:
                                mouseMode = MOUSE_MOVE.MOUSE_MOVE_NON;
                                break;
                        }
                        break;
                    case EventType.MouseUp:
                        mouseMode = MOUSE_MOVE.MOUSE_MOVE_NON;
                        break;
                    case EventType.ScrollWheel:
                        ControlCameraZoom();
                        return true;
                    default:
                        break;
                }
            }

            if (evt.type == EventType.MouseUp)
            {
                mouseMode = MOUSE_MOVE.MOUSE_MOVE_NON;
            }

            var renderPipeline = Unsupported.useScriptableRenderPipeline;
            Unsupported.useScriptableRenderPipeline = false;
            camera.Render();
            Unsupported.useScriptableRenderPipeline = renderPipeline;

            GUI.DrawTexture(rect, renderTexture, ScaleMode.ScaleToFit);

            camera.targetTexture = null;

            previewData.transPos = camera.transform.position;
            previewData.transRot = camera.transform.rotation;

            Vector2 delta;
            switch (mouseMode)
            {
                case MOUSE_MOVE.RIGHT_DRAG:
                    delta = GetMousePosDelta();
                    ControlCameraAngle(delta);
                    return true;
                case MOUSE_MOVE.CENTER_DRAG:
                    delta = GetMousePosDelta();
                    ControlCameraPosition(delta);
                    return true;
                default:
                    break;
            }

            return false;
        }

        public void AddGameObject(GameObject obj)
        {
            obj.transform.position = Vector3.zero;
            obj.transform.rotation = Quaternion.identity;
            SceneManager.MoveGameObjectToScene(obj, scene);
        }

        private Vector2 GetMousePosDelta()
        {
            var nowPos = Event.current.mousePosition;
            var delta = mouseOldPos - nowPos;
            mouseOldPos = nowPos;
            return delta;
        }

        private void ControlCameraAngle(Vector2 delta)
        {
            if ((camera == null) || (delta == Vector2.zero))
            {
                return;
            }

            float x = camera.transform.rotation.eulerAngles.x - (delta.y / 5);
            float y = camera.transform.rotation.eulerAngles.y - (delta.x / 5);
            camera.transform.rotation = Quaternion.Euler(x, y, 0);
        }

        private void ControlCameraPosition(Vector2 delta)
        {   //Calculated with rotation since want to move it against the local position
            if ((camera == null) || (delta == Vector2.zero))
            {
                return;
            }
            Vector3 velosity = camera.transform.rotation * new Vector3(delta.x, -delta.y, 0);
            camera.transform.position += (velosity / 300);
        }

        private void ControlCameraZoom()
        {
            if (camera == null)
            {
                return;
            }

            float x = camera.transform.localPosition.x;
            float y = camera.transform.localPosition.y;
            float z = camera.transform.localPosition.z + (Event.current.delta.y / 50);
            camera.transform.localPosition = new Vector3(x, y, z);
        }

        public void ChangeTextureSize(int size)
        {
            if (previewData == null)
            {
                previewData = HANA_Preview_Data.Instance();
            }

            previewData.textureSize = size;

            if (camera != null)
            {
                camera.targetTexture = null;
            }
            if (renderTexture != null)
            {
                Object.DestroyImmediate(renderTexture);
                renderTexture = null;
            }

            renderTexture = new RenderTexture(previewData.textureSize, previewData.textureSize, 32);
        }

        public void ResetMouseMode()
        {
            mouseMode = MOUSE_MOVE.MOUSE_MOVE_NON;
        }

        public void ClearThis()
        {
            if (camera != null)
            {
                camera.targetTexture = null;
            }
            if (renderTexture != null)
            {
                Object.DestroyImmediate(renderTexture);
                renderTexture = null;
            }
            camera = null;
            if (cameraObj != null)
            {
                Object.DestroyImmediate(cameraObj);
                cameraObj = null;
            }

            previewData = null;

            if (lightObj != null)
            {
                Object.DestroyImmediate(lightObj);
                lightObj = null;
            }

            EditorSceneManager.ClosePreviewScene(scene);
        }

        ~HANA_Preview()
        {
            ClearThis();
        }
    }
}
