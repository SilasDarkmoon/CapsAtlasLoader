using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.U2D;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Object = UnityEngine.Object;

namespace Capstones.UnityEngineEx
{
    public static class CapsAtlasLoader
    {
        // In current version of UGUI, the Image.RebuildImage will crash when the image's active sprite is set to null before the atlas is loaded.
        // So we get this list using reflection, and do the filter by ourselves.
        private static List<Image> TrackedImages;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnUnityStart()
        {
            TrackedImages = typeof(Image).GetField("m_TrackedTexturelessImages", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).GetValue(null) as List<Image>;

            if (ResManager.ResLoader is ResManager.ClientResLoader)
            {
                // TODO: when we change the ResLoader dynamically (from EditorResLoader to ClientResLoader), we should call this again.
                // or (from ClientResLoader to EditorResLoader) we should unregister SpriteAtlasManager.atlasRequested.
                SpriteAtlasManager.atlasRequested += LoadAtlas;
            }
        }

        public static void LoadAtlas(string name, Action<SpriteAtlas> funcReg)
        {
            var atlas = ResManager.LoadRes("atlas/" + name, typeof(SpriteAtlas)) as SpriteAtlas;
            if (atlas)
            {
                for (var i = TrackedImages.Count - 1; i >= 0; --i)
                {
                    var g = TrackedImages[i];
                    var sprite = g.overrideSprite != null ? g.overrideSprite : g.sprite;
                    if (!sprite)
                    {
                        TrackedImages.RemoveAt(i);
                    }
                }
                funcReg(atlas);
            }
        }
    }
}