using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sereno
{
    /// <summary>
    /// Class permitting to apply texture on a map
    /// </summary>
    public class MapTextureGameObject : MonoBehaviour
    {
        /// <summary>
        /// The Quad to apply to map
        /// </summary>
        public Renderer MapQuad;

        void Start()
        {}

        void Update()
        {}

        /// <summary>
        /// Apply the texture to the corresponding game object
        /// </summary>
        /// <param name="tex">The texture to apply</param>
        /// <param name="tiling">The tiling to apply</param>
        /// <param name="offset">The offset to apply on the texture</param>
        public void ApplyTexture(Texture2D tex, Vector2 tiling, Vector2 offset)
        {
            int texNameID = Shader.PropertyToID("_MainTex");
            MapQuad.material.SetTexture(texNameID, tex);
            MapQuad.material.SetTextureScale(texNameID,  tiling);
            MapQuad.material.SetTextureOffset(texNameID, offset);
        }
    }
}