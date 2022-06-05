using UnityEngine;
using System.Collections.Generic;

namespace TextureTools.Editor
{
    /// <summary>
    /// MeshExtensionMethods.cs
    /// www.noobpaint.com
    /// Mesh extension methods
    /// </summary>
    public static class MeshExtensionMethods
    {
        static readonly int k_Color = Shader.PropertyToID("_Color");

        /// <summary>
        /// Gets the UV shell for rendering a Texture.
        /// </summary>
        /// <returns>The UV shell.</returns>
        /// <param name="mesh">Mesh.</param>
        /// <param name="index">Index.</param>
        public static Mesh GetUVShell(this Mesh mesh, int index)
        {
            var shellMesh = Object.Instantiate(mesh);
            var getUVs = new List<Vector2>();
            shellMesh.GetUVs(index, getUVs);
            var meshUVs = new Vector3[shellMesh.vertexCount];
            var meshColors = new Color[shellMesh.vertexCount];

            for (var i = 0; i < mesh.vertexCount; i++)
            {
                meshUVs[i] = new Vector3(getUVs[i].x, 0f, getUVs[i].y);
                meshColors[i] = Color.white;
            }

            shellMesh.vertices = meshUVs;
            shellMesh.colors = meshColors;
            shellMesh.UploadMeshData(false);

            return shellMesh;
        }

        /// <summary>
        /// Gets the UV mask as a texture.
        /// </summary>
        /// <returns>The UV mask.</returns>
        /// <param name="mesh">Mesh.</param>
        /// <param name="channel">Channel.</param>
        /// <param name="mapSize">Map size.</param>
        public static Texture2D GetUVMask(this Mesh mesh, int channel, int mapSize = 512)
        {
            var uvMask = new Texture2D(mapSize, mapSize);

            // works at runtime just wanting to avoid using this method at runtime
            #if UNITY_EDITOR
            var renderCam = new GameObject("UVCam");
            renderCam.transform.rotation = Quaternion.LookRotation(Vector3.down);
            renderCam.transform.position = new Vector3(0.5f, 1f, 0.5f);
            renderCam.hideFlags = HideFlags.HideAndDontSave;

            var camera = renderCam.AddComponent<Camera>();
            camera.orthographic = true;
            camera.aspect = 1.0f;
            camera.orthographicSize = 0.5f;

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear;
            //using last layer mask 
            //hopefully not used in current scene
            camera.cullingMask = 1<<31;

            var rt = new RenderTexture(mapSize, mapSize, 0);
            rt.Create();
            camera.targetTexture = rt;
            RenderTexture.active = rt;

            var uvIslands = mesh.GetUVShell(channel);
            var meshRenderObj = new GameObject(string.Format(
            "UVMeshUV{0}", channel));
            meshRenderObj.hideFlags = HideFlags.HideAndDontSave;
            var meshFilter = meshRenderObj.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = uvIslands;

            meshRenderObj.layer = 31;

            var meshRenderer = meshRenderObj.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = new Material(Shader.Find(
            "GUI/Text Shader"));
            meshRenderer.sharedMaterial.SetColor(k_Color, Color.white);

            camera.Render();

            uvMask.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            uvMask.Apply();

            // Clean up render objects
            camera.targetTexture = null;
            RenderTexture.active = null;

            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(renderCam);
            Object.DestroyImmediate(meshRenderObj);
            #endif

            uvMask.name = mesh.name + "_UVMask";
            return uvMask;
        }
    }
}
