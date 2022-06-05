using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.EditorCoroutines.Editor;

namespace TextureTools.Editor
{
    /// <summary>
    /// methods for bleeding (extruding) the edge color of a texture to fill empty pixels.
    /// </summary>
    public class TextureEdgeBleeding : TextureWebLoad
    {
        public static bool debugIsland = false;
        public static bool debugFill = false;
        static readonly Color[] k_DebugColors = new Color[]{Color.white, Color.red, Color.blue, Color.green};
        static readonly int[,] k_Offsets = new int[,]{ {-1, -1}, { 0, -1}, { 1, -1}, {-1,  0}, { 1,  0}, {-1,  1}, { 0,  1}, { 1,  1} };

        /// <summary>
        /// Editor safe way to call WebLoadTextureAndBleed()
        /// </summary>
        /// <param name="renderer">Renderer.</param>
        /// <param name="sourceTexture">Source texture.</param>
        /// <param name="width">Width.</param>
        /// <param name="height">Height.</param>
        public void EditorBleedTexture(Renderer renderer, Texture2D sourceTexture, int width, int height)
        {
            if (!locked)
            {
                locked = true;
                TextureCoroutine = EditorCoroutineUtility.StartCoroutineOwnerless
                (
                    WebLoadTextureAndBleed(renderer, sourceTexture, width, height, callback => loadTexture = callback)
                );
            }
        }

        /// <summary>
        /// Editor safe way to call WebLoadTextureAndBleed()
        /// </summary>
        /// <param name="mesh">Mesh.</param>
        /// <param name="sourceTexture">Source texture.</param>
        /// <param name="width">Width.</param>
        /// <param name="height">Height.</param>
        public void EditorBleedTexture(Mesh mesh, Texture2D sourceTexture, int width, int height)
        {
            if (!locked)
            {
                locked = true;
                TextureCoroutine = EditorCoroutineUtility.StartCoroutineOwnerless
                (
                    WebLoadTextureAndBleed(mesh, sourceTexture, width, height, callback => loadTexture = callback)
                );
            }
        }

        /// <summary>
        /// Editor safe way to call WebLoadTextureAndBleed()
        /// </summary>
        /// <param name="uvIsland">Uv island.</param>
        /// <param name="sourceTexture">Source texture.</param>
        /// <param name="width">Width.</param>
        /// <param name="height">Height.</param>
        public void EditorBleedTexture(Texture2D uvIsland, Texture2D sourceTexture, int width, int height)
        {
            if (!locked)
            {
                locked = true;
                TextureCoroutine = EditorCoroutineUtility.StartCoroutineOwnerless
                (
                    WebLoadTextureAndBleed(uvIsland, sourceTexture, width, height, callback => loadTexture = callback)
                );
            }
        }

        /// <summary>
        /// Sets you texture for edge bleed and runs AlphaBleedingTexture() 
        /// </summary>
        /// <returns>The texture.</returns>
        /// <param name="texture">Texture.</param>
        /// <param name="uvTexture">Uv texture.</param>
        /// <param name="width">Width.</param>
        /// <param name="height">Height.</param>
        public static Texture2D BleedTexture (Texture2D texture, Texture2D uvTexture, int width, int height)
        {
            Color[] bleedMask;
            if (uvTexture.width != width || uvTexture.height != height)
            {
                bleedMask = uvTexture.GetPixelsBilinear(width, height);
            }
            else
            {
                bleedMask = uvTexture.GetPixels();
            }

            Color[] sourceColor;
            if (texture.width != width || texture.height != height)
            {
                sourceColor = texture.GetPixelsBilinear(width, height);
            }
            else
            {
                sourceColor = texture.GetPixels();
            }

            sourceColor = AlphaBleedingTexture(sourceColor, bleedMask, 
            width, height);
            var bleedTexture = new Texture2D(width, height) { name = $"{texture.name}_Bleed" };
            bleedTexture.SetPixels(sourceColor);
            bleedTexture.Apply();

            return bleedTexture;
        }

        /// <summary>
        /// Uses a mask texture(island) to fill non uv mapped (ocean) 
        /// sections of the texture, marching from the islands 
        /// outside edge (shore) till the ocean is filled in.
        /// </summary>
        /// <returns>The bleeding texture.</returns>
        /// <param name="sourceColors">Source colors.</param>
        /// <param name="bleedMask">Bleed mask.</param>
        /// <param name="width">Width.</param>
        /// <param name="height">Height.</param>
        public static Color[] AlphaBleedingTexture(Color[] sourceColors, Color[] bleedMask, int width, int height)
        {
            if (sourceColors.Length != bleedMask.Length || (width*height) != sourceColors.Length)
            {
                Debug.LogError("Bad Data for Texture Bleed!");
                return null;
            }

            var outTexture = new Texture2D(width, height);
            var outColors = outTexture.GetPixels();
            var size = sourceColors.Length;

            var opaque = new int[size];
            var loose = new bool[size];
            var pending = new List<int>();
            var pendingNext = new List<int>();
            
            //pre process bleedMask
            //find all pixels not adjacent to island
            for (var i = 0; i < size; i++)
            {
                if (bleedMask[i].a < 0.5f)
                {
                    var isLoose = true; //not adjacent to colored pixels

                    var y = i % width;
                    var x = (i - y) / width;

                    //8 tap adjacent pixels
                    for (var o = 0; o < 8; o++)
                    {
                        var xOffset = k_Offsets [o,0] + x;
                        var yOffset = k_Offsets [o,1] + y;

                        //check if pixel within bounds
                        if(xOffset >= 0 && xOffset < width
                           && yOffset >= 0 && yOffset <height)
                        {
                            var index = (xOffset * width) + yOffset;
                            if (bleedMask[index].a > 0.5f)
                            {
                                isLoose = false;
                                break;
                            }
                        }
                    }

                    if (!isLoose)
                    {
                        pending.Add(i); //has color pixels adjacent
                        loose[i] = false;
                        opaque[i] = 0;
                        if (debugIsland)
                            outColors[i] = k_DebugColors[1];
                    }
                    else
                    {
                        loose[i] = true;
                        opaque[i] = 0;
                        if (debugIsland)
                            outColors[i] = k_DebugColors[2];
                    }
                }
                else
                {
                    loose[i] = false;
                    opaque[i] = 1;
                    if (debugIsland)
                        outColors[i] = k_DebugColors[0];
                    else
                        outColors[i] = sourceColors[i];
                }
            }

            var debugCount = 0;

            if (!debugIsland)
            {
                while (pending.Count > 0)
                {
                    pendingNext.Clear();

                    for (var p = 0; p < pending.Count; p++)
                    {
                        var i = pending[p];
                        var y = i % width;
                        var x = (i - y) / width;

                        var fillColor = Vector4.zero;

                        var count = 0;

                        for (var o = 0; o < 8; o++)
                        {
                            var xOffset = k_Offsets [o,0] + x;
                            var yOffset = k_Offsets [o,1] + y;

                            if(xOffset >= 0 && xOffset < width
                               && yOffset >= 0 && yOffset < height)
                            {
                                var index = (xOffset * width) + yOffset;
                                if(opaque[index] == 1)
                                {
                                    fillColor += (Vector4)outColors[index];
                                    count++;
                                }
                            }
                        }

                        if (count > 0)
                        {
                            if (debugFill)
                                outColors[i] = (Vector4)k_DebugColors[debugCount];
                            else
                                outColors[i] = fillColor / count;
                            opaque[i] = 1;


                            for (var o = 0; o < 8; o++)
                            {
                                var xOffset = k_Offsets [o,0] + x;
                                var yOffset = k_Offsets [o,1] + y;

                                if(xOffset >= 0 && xOffset < width
                                   && yOffset >= 0 && yOffset < height)
                                {
                                    var index = (xOffset * width) + yOffset;

                                    if (loose[index])
                                    {
                                        pendingNext.Add(index);
                                        loose[index] = false;
                                    }
                                }
                            }
                        }
                        else
                        {
                            pendingNext.Add(i);
                        }
                    }

                    if (pendingNext.Count > 0)
                    {
                        for (var p = 0; p < pending.Count; p++)
                        {
                            opaque[pending[p]] = 1;
                        }
                    }

                    pending.Clear();
                    pending.AddRange(pendingNext.ToArray());
                    if (debugFill)
                    {
                        debugCount++;
                        if (debugCount >= k_DebugColors.Length)
                            debugCount = 0;
                    }

                }
            }
            return outColors;
        }

        /// <summary>
        /// Uses WebLoadTexture() to get a modifiable texture
        /// then runs steps to bleed the texture edges
        /// </summary>
        /// <returns>The load texture and bleed.</returns>
        /// <param name="renderer">Renderer.</param>
        /// <param name="texture">Texture.</param>
        /// <param name="width">Width.</param>
        /// <param name="height">Height.</param>
        /// <param name="result">Result.</param>
        public static IEnumerator WebLoadTextureAndBleed (Renderer renderer, Texture2D texture, int width, int height, System.Action<Texture2D> result)
        {
            Mesh mesh = null;
            if (renderer is SkinnedMeshRenderer)
            {
                var meshRenderer = renderer as SkinnedMeshRenderer;
                mesh = meshRenderer.sharedMesh;
            }
            else if (renderer is MeshRenderer)
            {
                var meshFilter = renderer.GetComponent<MeshFilter>();
                if (meshFilter != null || meshFilter)
                {
                    mesh = meshFilter.sharedMesh;
                }
            }
            
            Texture2D outTexture = null;

            EditorCoroutineUtility.StartCoroutineOwnerless(
                WebLoadTextureAndBleed(mesh, texture, width, height, callback => outTexture = callback )
                );
            
            while(outTexture == null)
            {
                yield return null;
            }
            
            outTexture.name = texture.name;

            result(outTexture);
        }

        /// <summary>
        /// Uses WebLoadTexture() to get a modifiable texture
        /// then runs steps to bleed the texture edges
        /// </summary>
        /// <returns>The load texture and bleed.</returns>
        /// <param name="mesh">Mesh.</param>
        /// <param name="texture">Texture.</param>
        /// <param name="width">Width.</param>
        /// <param name="height">Height.</param>
        /// <param name="result">Result.</param>
        public static IEnumerator WebLoadTextureAndBleed (Mesh mesh, 
        Texture2D texture, int width, int height, 
        System.Action<Texture2D> result)
        {
            Texture2D loadTexture = null;
            var loader = EditorCoroutineUtility.StartCoroutineOwnerless(
                WebLoadTexture(texture, width, height, callback => loadTexture = callback)
            );

            while(loadTexture == null)
            {
                yield return null;
            }
            Debug.Log("Loaded for bleed");
            if (loader != null)
            {
                EditorCoroutineUtility.StopCoroutine(loader);
            }

            if (mesh != null)
            {
                Texture2D uvIsland = mesh.GetUVMask(0, width);
                Debug.Log("got UV island");

                loadTexture = TextureEdgeBleeding.BleedTexture(loadTexture, 
                    uvIsland, width, height);
            }
            else
            {
                Debug.LogError("Mesh is Null");
            }

            loadTexture.name = texture.name;

            result(loadTexture);
        }

        /// <summary>
        /// Uses WebLoadTexture() to get a modifiable texture
        /// then runs steps to bleed the texture edges
        /// </summary>
        /// <returns>The load texture and bleed.</returns>
        /// <param name="uvIsland">Uv island.</param>
        /// <param name="texture">Texture.</param>
        /// <param name="width">Width.</param>
        /// <param name="height">Height.</param>
        /// <param name="result">Result.</param>
        public static IEnumerator WebLoadTextureAndBleed (Texture2D uvIsland, Texture2D texture, int width, int height, 
            System.Action<Texture2D> result)
        {
            Texture2D loadTexture = null;
            var loader = EditorCoroutineUtility.StartCoroutineOwnerless(
            TextureWebLoad.WebLoadTexture(texture, width, height, 
            callback => loadTexture = callback));

            while(loadTexture == null)
            {
                yield return null;
            }
            
            Debug.Log("Loaded for bleed");
            if (loader != null)
            {
                EditorCoroutineUtility.StopCoroutine(loader);
            }

            if (uvIsland != null)
            {
                loadTexture = TextureEdgeBleeding.BleedTexture(loadTexture, 
                uvIsland, width, height);
            }
            else
            {
                Debug.LogError("Mesh is Null");
            }

            loadTexture.name = texture.name;

            result(loadTexture);
        }
    }
}
