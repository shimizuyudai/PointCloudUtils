using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PointCloudUtils
{
    public class TextureUtils
    {

        public static void RenderTexture2Texture2D(RenderTexture renderTexture, Texture2D texture2D)
        {
            var rt = RenderTexture.active;
            RenderTexture.active = renderTexture;
            texture2D.ReadPixels(new Rect(0, 0, texture2D.width, texture2D.height), 0, 0);
            texture2D.Apply();
            RenderTexture.active = rt;
        }

        public static Texture2D CreateTexture2DFromRenderTexture(RenderTexture renderTexture)
        {
            TextureFormat textureFormat;
            switch (renderTexture.format)
            {
                case RenderTextureFormat.ARGB32:
                    textureFormat = TextureFormat.ARGB32;
                    break;

                case RenderTextureFormat.ARGBHalf:
                    textureFormat = TextureFormat.RGBAHalf;
                    break;

                case RenderTextureFormat.ARGBFloat:
                    textureFormat = TextureFormat.RGBAFloat;
                    break;

                default:
                    textureFormat = TextureFormat.ARGB32;
                    break;
            }
            var texture = new Texture2D(renderTexture.width, renderTexture.height, textureFormat, false);
            RenderTexture2Texture2D(renderTexture, texture);
            return texture;
        }

        public static void PingPongTextures(Texture[] textures)
        {
            var temp = textures[0];
            textures[0] = textures[1];
            textures[1] = temp;
        }
    }
}
