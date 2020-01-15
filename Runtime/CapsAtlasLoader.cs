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
        // This may be because that, an auto garbage collect is triggered after the scene is loaded.
        // while the scene objs are going to refer atlas, but the refer has not been made, as the garbage collect happens too early.
        // TODO: common lifetime holder.
        private static List<SpriteAtlas> _LifetimeHolder = new List<SpriteAtlas>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnUnityStart()
        {
            TrackedImages = typeof(Image).GetField("m_TrackedTexturelessImages", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).GetValue(null) as List<Image>;

            if (ResManager.ResLoader is ResManager.ClientResLoader)
            {
                // TODO: when we change the ResLoader dynamically (from EditorResLoader to ClientResLoader), we should call this again.
                // or (from ClientResLoader to EditorResLoader) we should unregister SpriteAtlasManager.atlasRequested.
                SpriteAtlasManager.atlasRequested += LoadAtlas;

                UnityEngine.SceneManagement.SceneManager.sceneLoaded += (scene, mode) => CheckAtlasLifetime();
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
                _LifetimeHolder.Add(atlas);
                funcReg(atlas);
            }
        }

        private static void CheckAtlasLifetime()
        {
            //CoroutineRunner.StartCoroutine(CheckAtlasLifetimeWork());
        }
        public static IEnumerator CheckAtlasLifetimeWork()
        {
            yield return Resources.UnloadUnusedAssets();
            yield return null;
            List<Pack<string, WeakReference>> old = new List<Pack<string, WeakReference>>();
            for (int i = 0; i < _LifetimeHolder.Count; ++i)
            {
                var atlas = _LifetimeHolder[i];
                if (atlas)
                {
                    old.Add(new Pack<string, WeakReference>(atlas.tag, new WeakReference(atlas)));
                }
            }
            _LifetimeHolder.Clear();
            try
            {
                yield return Resources.UnloadUnusedAssets();
            }
            finally
            {
                for (int i = 0; i < old.Count; ++i)
                {
                    var info = old[i];
                    var atlas = info.t2.GetWeakReference<SpriteAtlas>();
                    if (atlas)
                    {
                        _LifetimeHolder.Add(atlas);
                    }
                    else
                    {
                        PlatDependant.LogError($"Atlas released. {info.t1}");
                    }
                }
            }
        }
    }
}