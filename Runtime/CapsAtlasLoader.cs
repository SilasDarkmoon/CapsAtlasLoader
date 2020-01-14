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
                SpriteAtlasManager.atlasRequested += (name, funcReg) =>
                {
                    var atlas = ResManager.LoadRes("atlas/" + name, typeof(SpriteAtlas)) as SpriteAtlas;
                    if (atlas)
                    {
                        var sprites = Resources.FindObjectsOfTypeAll<Sprite>();
                        for (int i = 0; i < sprites.Length; ++i)
                        {
                            var sprite = sprites[i];
                            if (!sprite.texture)
                            {
                                if (atlas.CanBindTo(sprite))
                                {
                                    _LifetimeHolder.Add(new Pack<WeakReference, SpriteAtlas, string>(new WeakReference(sprite), atlas, sprite.name));
                                }
                            }
                        }

                        if (!_LifetimeChecking)
                        {
                            CoroutineRunner.StartCoroutine(CheckLifetimeWork());
                        }

                        for (var i = TrackedImages.Count - 1; i >= 0; --i)
                        {
                            var g = TrackedImages[i];
                            if (!(g.overrideSprite != null ? g.overrideSprite : g.sprite))
                            {
                                TrackedImages.RemoveAt(i);
                            }
                        }
                        funcReg(atlas);
                    }
                };
            }
        }

        private static bool _LifetimeChecking = false;
        private static List<Pack<WeakReference, SpriteAtlas, string>> _LifetimeHolder = new List<Pack<WeakReference, SpriteAtlas, string>>();
        private static IEnumerator CheckLifetimeWork()
        {
            _LifetimeChecking = true;
            try
            {
                while (true)
                {
                    for (var i = _LifetimeHolder.Count - 1; i >= 0; --i)
                    {
                        var item = _LifetimeHolder[i];
                        if (!item.t1.GetWeakReference<Texture>())
                        {
                            PlatDependant.LogError($"Atlas of Sprite {item.t3} need to be unloaded.");
                            _LifetimeHolder.RemoveAt(i);
                        }
                    }
                    yield return null;
                }
            }
            finally
            {
                _LifetimeChecking = false;
            }
        }
    }
}