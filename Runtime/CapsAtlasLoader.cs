using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.U2D;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Object = UnityEngine.Object;
using Capstones.UnityEngineEx.CoroutineTasks;
using Capstones.WeakAttachments;

namespace Capstones.UnityEngineEx
{
    public static class CapsAtlasLoader
    {
        public const int CapsResManifestItemType_Atlas = 5;
        public class AssetInfo_Atlas : UnityEngineEx.ResManager.ClientResLoader.AssetInfo_Normal
        {
            protected override Object LoadMainAsset()
            {
                RegReferenceFromSprite();
                return base.LoadMainAsset();
            }
            protected override IEnumerator LoadMainAssetAsync(CoroutineWork req)
            {
                RegReferenceFromSprite();
                return base.LoadMainAssetAsync(req);
            }

            public void RegReferenceFromSprite()
            {
                if (DepBundles.Count > 0)
                {
                    var bi = DepBundles[DepBundles.Count - 1];
                    if (bi != null)
                    {
                        bi.LeaveAssetOpen = true;
                    }
                }
            }
        }

        public class TypedResLoader_Atlas : UnityEngineEx.ResManager.ClientResLoader.TypedResLoader_Normal, ResManager.IAssetBundleLoaderEx
        {
            public TypedResLoader_Atlas()
            {
                ResManager.AssetBundleLoaderEx.Add(this);
            }

            public override int ResItemType { get { return CapsResManifestItemType_Atlas; } }

            protected override UnityEngineEx.ResManager.ClientResLoader.AssetInfo_Base CreateAssetInfoRaw(CapsResManifestItem item)
            {
                return new AssetInfo_Atlas() { ManiItem = item };
            }

            //protected List<SpriteAtlas> _LoadedAtlas = new List<SpriteAtlas>();
            protected Dictionary<Sprite, SpriteAtlas> _SpriteAtlasMap = new Dictionary<Sprite, SpriteAtlas>();
            public void PreUnloadUnusedRes()
            {
                var atlases = Resources.FindObjectsOfTypeAll<SpriteAtlas>();
                var sprites = Resources.FindObjectsOfTypeAll<Sprite>();
                for (int i = 0; i < atlases.Length; ++i)
                {
                    var atlas = atlases[i];
                    //_LoadedAtlas.Add(atlas);
                    for (int j = 0; j < sprites.Length; ++j)
                    {
                        var sprite = sprites[j];
                        if (atlas.CanBindTo(sprite))
                        {
                            //sprite.SetAttachment(atlas);
                            _SpriteAtlasMap[sprite] = atlas;
                        }
                    }
                }
            }
            public void PostUnloadUnusedRes()
            {
                _SpriteAtlasMap.Clear();
                //_LoadedAtlas.Clear();
            }
            public bool LoadAssetBundle(string mod, string name, bool isContainingBundle, out ResManager.AssetBundleInfo bi)
            {
                bi = null;
                return false;
            }
        }
        public static TypedResLoader_Atlas Instance_TypedResLoader_Atlas = new TypedResLoader_Atlas();

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

        private static Action<SpriteAtlas> _AtlasRegFunc;
        //private static List<SpriteAtlas> _LoadedAtlas = new List<SpriteAtlas>();
        public static void LoadAtlas(string name, Action<SpriteAtlas> funcReg)
        {
            _AtlasRegFunc = funcReg;
            var atlas = ResManager.LoadRes("atlas/" + name, typeof(SpriteAtlas)) as SpriteAtlas;
            if (atlas)
            {
                //_LoadedAtlas.Add(atlas);
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