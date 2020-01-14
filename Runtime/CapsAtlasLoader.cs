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
        // We find that, sometimes, atlas will be unloaded while it is actually in use.
        // This may be because that, we are not holding a reference in the C#.
        // This field make a map from WeakReference<Sprite> to Atlas, so we hold a reference to atlas, and can release the atlas's reference when the sprite is unloaded.
        // TODO: common lifetime holder.
        private static List<Pack<WeakReference, SpriteAtlas, string>> _LifetimeHolder = new List<Pack<WeakReference, SpriteAtlas, string>>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnUnityStart()
        {
            TrackedImages = typeof(Image).GetField("m_TrackedTexturelessImages", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).GetValue(null) as List<Image>;
            UnityEngineEx.ResManager.GarbageCollector.GarbageCollectorEvents[1] += UnloadUnusedAtlas;

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
                    //else if (atlas.CanBindTo(sprite))
                    //{
                    //    _LifetimeHolder.Add(new Pack<WeakReference, SpriteAtlas, string>(new WeakReference(sprite), atlas, sprite.name));
                    //}
                }
                funcReg(atlas);
            }
        }

        public static IEnumerator UnloadUnusedAtlas()
        {
            for (int i = _LifetimeHolder.Count - 1; i >= 0; --i)
            {
                var info = _LifetimeHolder[i];
                if (!info.t1.GetWeakReference<Sprite>())
                {
                    _LifetimeHolder.RemoveAt(i);
                }
            }
            yield return Resources.UnloadUnusedAssets();
        }
    }
}