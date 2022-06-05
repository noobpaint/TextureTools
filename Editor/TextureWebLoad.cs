using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;
using System.Linq;
using Unity.EditorCoroutines.Editor;

namespace TextureTools.Editor
{
    /// <summary>
    /// WebLoad Texture using WWW class.
    /// </summary>
    public class TextureWebLoad
    {
        static readonly string[] k_ValidTextureFormats = {".png", ".PNG", ".Png"}; 
        public bool locked 
        {
            get
            {
                if (null == m_LoadTexture)
                {
                    return m_Locked;
                }

                m_Locked = false;

                return m_Locked;
            }

            protected set
            {
                m_Locked = value;
            }
        }
        
        bool m_Locked;

        public Texture2D loadTexture
        {
            get => m_LoadTexture;
            protected set => m_LoadTexture = value;
        }
        
        Texture2D m_LoadTexture;

        protected EditorCoroutine TextureCoroutine;

        /// <summary>
        /// Reset this instance.
        /// </summary>
        public void reset ()
        {
            if (null != TextureCoroutine)
            {
                EditorCoroutineUtility.StopCoroutine(TextureCoroutine);
                TextureCoroutine = null;
            }
            
            m_Locked = false;
            m_LoadTexture = null;
        }

        /// <summary>
        /// Editor safe way to call WebLoadTexture()
        /// calls EditorCoroutine and loads texture to loadTexture
        /// </summary>
        /// <param name="sourceTexture">Source texture.</param>
        public void EditorWebLoad(Texture2D sourceTexture)
        {
            if (!m_Locked)
            {
                m_Locked = true;
                TextureCoroutine = EditorCoroutineUtility.StartCoroutineOwnerless
                (
                    WebLoadTexture(sourceTexture, callback => m_LoadTexture = callback)
                );
            }
        }

        /// <summary>
        /// Editor safe way to call WebLoadTexture()
        /// calls EditorCoroutine and loads texture to loadTexture
        /// </summary>
        /// <param name="sourceTexture">Source texture.</param>
        /// <param name="width">Width.</param>
        /// <param name="height">Height.</param>
        public void EditorWebLoad(Texture2D sourceTexture, int width, int height)
        {
            if (!m_Locked)
            {
                m_Locked = true;
                TextureCoroutine = EditorCoroutineUtility.StartCoroutineOwnerless
                (
                WebLoadTexture(sourceTexture, width, height, callback => m_LoadTexture = callback)
                );
            }
        }

        /// <summary>
        /// Loads a png or jpg texture using WWW class
        /// to avoid texture compression or gives access 
        /// to raw texture size.
        /// </summary>
        /// <returns>The load texture.</returns>
        /// <param name="texture">Texture.</param>
        /// <param name="result">Result.</param>
        public static IEnumerator WebLoadTexture (Texture2D texture, System.Action<Texture2D> result)
        {
            Texture2D loadTexture = null;
            var loadCoroutine = EditorCoroutineUtility.StartCoroutineOwnerless
            (
            WebLoadTexture(texture, texture.width, texture.height, callback => loadTexture = callback)
            );
            
            while(null == loadTexture)
            {
                yield return null;
            }
            
            result(loadTexture);
            EditorCoroutineUtility.StopCoroutine(loadCoroutine);
        }

        /// <summary>
        /// Loads a png or jpg texture using WWW class
        /// to avoid texture compression or gives access 
        /// to raw texture size.
        /// </summary>
        /// <returns>The load texture.</returns>
        /// <param name="texture">Texture.</param>
        /// <param name="width">Width.</param>
        /// <param name="height">Height.</param>
        /// <param name="result">Result.</param>
        public static IEnumerator WebLoadTexture (Texture2D texture, int width, int height, System.Action<Texture2D> result)
        {
            var doWebLoad = !texture.name.Contains("_WebLoad");

            Texture2D outTexture;
            if (doWebLoad)
            {   
                var path = AssetDatabase.GetAssetPath(texture);
                if (string.IsNullOrEmpty(path))
                {
                    outTexture = texture;
                    doWebLoad = false;
                }
                else
                {
                    var assetPath = AssetDatabase.GetAssetPath(texture);
                    if (!k_ValidTextureFormats.Contains(Path.GetExtension(assetPath)))
                    {
                        result(null);
                        Debug.LogAssertion($"Could not load asset at {assetPath}, only 'png' files are supported!");
                        yield break;
                    }

                    path = "file://" + Path.GetFullPath(AssetDatabase.GetAssetPath(texture));

                    using (var webLoad = new WWW(path))
                    {
                        while (webLoad.texture == null)
                        {
                            yield return null;
                        }
                        outTexture = webLoad.texture;

                        outTexture.wrapMode = TextureWrapMode.Clamp;
                        outTexture.Apply();

                        if (outTexture.width != width || outTexture.height != height)
                        {
                            outTexture.ResizeAndFill(width, height);
                        }
                    }
                }
            }
            else
            {
                outTexture = texture;
            }

            if (doWebLoad)
            {
                while(null == outTexture)
                {
                    yield return null;
                }

                outTexture.name += "_WebLoad";
            }

            result(outTexture);
        }
    }
}
