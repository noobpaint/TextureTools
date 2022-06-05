using UnityEngine;

namespace TextureTools.Editor
{
    /// <summary>
    /// Texture2D extension methods.
    /// </summary>
    public static class Texture2DExtensionMethods
    {
        /// <summary>
        /// Gets the pixel centers of a texture.
        /// </summary>
        /// <returns>The pixel centers.</returns>
        /// <param name="texture2D">Texture2 d.</param>
        public static float[,] GetPixelCenters(this Texture2D texture2D)
        {
            return GetPixelCenters(texture2D, texture2D.width, 
            texture2D.height);
        }

        /// <summary>
        /// Gets the pixel centers of texture size width by height.
        /// </summary>
        /// <returns>The pixel centers.</returns>
        /// <param name="texture2D">Texture2 d.</param>
        /// <param name="width">Width.</param>
        /// <param name="height">Height.</param>
        public static float[,] GetPixelCenters(this Texture2D texture2D, 
        int width, int height)
        {
            //generate array of offsets for get pixels
            //get uv coord at center of pixel for sampling
            var xStep = 1.0f / (width*2.0f);
            var yStep = 1.0f / (height*2.0f);
            var size = width * height;

            //generate x centers
            var xSteps = new float[width];
            for (var i = 0; i < width; i++)
            {
                xSteps[i] = (i * xStep * 2) + xStep;
            }
            
            //generate y centers 
            var ySteps = new float[height];
            for (var i = 0; i < height; i++)
            {
                ySteps[i] = (i * yStep * 2) + yStep;
            }
            
            //array of float offsets starting at bottom left going right
            //bottom to top
            var offsetArray = new float[size,2];
            for (var i = 0; i < size; i++)
            {
                var x = i % width;
                var y = (i - x) / width;

                offsetArray[i,0] = xSteps[x];
                offsetArray[i,1] = ySteps[y];
            }
            
            return offsetArray;
        }

        /// <summary>
        /// Gets the pixels using bilinear filtering
        /// to flattened 2D array, where pixels are laid 
        /// out left to right, bottom to top.
        /// </summary>
        /// <returns>The pixels bilinear.</returns>
        /// <param name="texture">Texture.</param>
        public static Color[] GetPixelsBilinear(this Texture2D texture)
        {
            return GetPixelsBilinear(texture, texture.width, texture.height);
        }

        /// <summary>
        /// Gets the pixels using bilinear filtering
        /// to flattened 2D array, where pixels are laid 
        /// out left to right, bottom to top.
        /// </summary>
        /// <returns>The pixels bilinear.</returns>
        /// <param name="texture">Texture.</param>
        /// <param name="width">Width.</param>
        /// <param name="height">Height.</param>
        public static Color[] GetPixelsBilinear(this Texture2D texture, int width, int height)
        {
            var size = width * height;

            var pixels = new Color[size];

            var offsetArray = texture.GetPixelCenters(width,height);

            for (var i = 0; i < size; i++)
            {
                pixels[i] = texture.GetPixelBilinear(offsetArray[i,0], 
                    offsetArray[i,1]);
            }
            
            return pixels;
        }

        /// <summary>
        /// Resizes Texture and fills with get pixels bilinear.
        /// </summary>
        /// <returns>The and fill.</returns>
        /// <param name="texture">Texture.</param>
        /// <param name="width">Width.</param>
        /// <param name="height">Height.</param>
        public static Texture2D ResizeAndFill(this Texture2D texture, 
        int width, int height)
        {
            var size = width * height;

            var pixels = texture.GetPixelsBilinear(width, height);

            texture.Resize(width, height);

            texture.SetPixels(pixels);
            texture.Apply();

            return texture;
        }

        public static Texture2D Copy(this Texture2D texture)
        {
            return Object.Instantiate<Texture2D>(texture);
        }
    }
}
