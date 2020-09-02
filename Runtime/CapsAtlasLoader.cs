#if !UNITY_2019_4_OR_NEWER || UNITY_2019_4_0 || UNITY_2019_4_1 || UNITY_2019_4_2 || UNITY_2019_4_3 || UNITY_2019_4_4 || UNITY_2019_4_5 || UNITY_2019_4_6 || UNITY_2019_4_7 || UNITY_2019_4_8
#define FIX_LOAD_ATLAS_IN_ASSET_BUNDLE
#endif

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.U2D;
using System;
using System.Collections;
using System.Collections.Generic;

using Object = UnityEngine.Object;
using Capstones.UnityEngineEx.CoroutineTasks;

namespace Capstones.UnityEngineEx
{
    public static class CapsAtlasLoader
    {
        public const int CapsResManifestItemType_Atlas = 5;
        public class AssetInfo_Atlas : UnityEngineEx.ResManager.ClientResLoader.AssetInfo_Normal
        {
            protected override Object LoadMainAsset()
            {
#if FIX_LOAD_ATLAS_IN_ASSET_BUNDLE
                RegReferenceFromSprite();
#endif
                return base.LoadMainAsset();
            }
            protected override IEnumerator LoadMainAssetAsync(CoroutineWork req)
            {
#if FIX_LOAD_ATLAS_IN_ASSET_BUNDLE
                RegReferenceFromSprite();
#endif
                return base.LoadMainAssetAsync(req);
            }
#if FIX_LOAD_ATLAS_IN_ASSET_BUNDLE
            public void RegReferenceFromSprite()
            {
                if (DepBundles.Count > 0)
                {
                    //var bi = DepBundles[DepBundles.Count - 1];
                    //if (bi != null)
                    //{
                    //    bi.LeaveAssetOpen = true;
                    //}
                    var aname = ManiItem.Node.PPath;
                    if (aname.EndsWith(".spriteatlas"))
                    {
                        aname = aname.Substring(0, aname.Length - ".spriteatlas".Length);
                    }
                    for (int i = DepBundles.Count - 2; i >= 0; --i)
                    {
                        var dep = DepBundles[i];
                        HashSet<string> depatlases;
                        if (!_AssetBundleAtlasDepInfos.TryGetValue(dep.RealName, out depatlases))
                        {
                            depatlases = new HashSet<string>();
                            _AssetBundleAtlasDepInfos[dep.RealName] = depatlases;
                        }
                        depatlases.Add(aname);
                    }
                }
            }
#endif
        }
#if FIX_LOAD_ATLAS_IN_ASSET_BUNDLE
        private static Dictionary<string, HashSet<string>> _AssetBundleAtlasDepInfos = new Dictionary<string, HashSet<string>>();
#endif
        public class TypedResLoader_Atlas : UnityEngineEx.ResManager.ClientResLoader.TypedResLoader_Normal
#if FIX_LOAD_ATLAS_IN_ASSET_BUNDLE
            , ResManager.IAssetBundleLoaderEx
#endif
        {
#if FIX_LOAD_ATLAS_IN_ASSET_BUNDLE
            public TypedResLoader_Atlas()
            {
                ResManager.AssetBundleLoaderEx.Add(this);
            }
#endif

            public override int ResItemType { get { return CapsResManifestItemType_Atlas; } }

            protected override UnityEngineEx.ResManager.ClientResLoader.AssetInfo_Base CreateAssetInfoRaw(CapsResManifestItem item)
            {
                return new AssetInfo_Atlas() { ManiItem = item };
            }
#if FIX_LOAD_ATLAS_IN_ASSET_BUNDLE
            protected HashSet<string> _LoadingAtlasForABs = new HashSet<string>();
            public bool LoadAssetBundle(string mod, string name, bool isContainingBundle, out ResManager.AssetBundleInfo bi)
            {
                var abname = name;
                if (!string.IsNullOrEmpty(mod))
                {
                    abname = "mod/" + mod + "/" + name;
                }
                HashSet<string> depatlases;
                if (_AssetBundleAtlasDepInfos.TryGetValue(abname, out depatlases))
                {
                    if (_LoadingAtlasForABs.Add(abname))
                    {
                        foreach (var aname in depatlases)
                        {
                            LoadAtlas(aname, _AtlasRegFunc);
                        }
                        _LoadingAtlasForABs.Remove(abname); // avoid stack overflow.
                    }
                }

                bi = null;
                return false;
            }
#endif
        }
        public static TypedResLoader_Atlas Instance_TypedResLoader_Atlas = new TypedResLoader_Atlas();

#if FIX_LOAD_ATLAS_CRASH_ON_DISPOSED_SPRITE
        // In current version of UGUI, the Image.RebuildImage will crash when the image's active sprite is set to null before the atlas is loaded.
        // So we get this list using reflection, and do the filter by ourselves.
        private static List<Image> TrackedImages;
#endif
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnUnityStart()
        {
#if FIX_LOAD_ATLAS_CRASH_ON_DISPOSED_SPRITE
            TrackedImages = typeof(Image).GetField("m_TrackedTexturelessImages", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).GetValue(null) as List<Image>;
#endif
            if (ResManager.ResLoader is ResManager.ClientResLoader)
            {
                // TODO: when we change the ResLoader dynamically (from EditorResLoader to ClientResLoader), we should call this again.
                // or (from ClientResLoader to EditorResLoader) we should unregister SpriteAtlasManager.atlasRequested.
                SpriteAtlasManager.atlasRequested += LoadAtlas;
            }
        }

#if FIX_LOAD_ATLAS_IN_ASSET_BUNDLE
        private static Action<SpriteAtlas> _AtlasRegFunc;
#endif
        public static void LoadAtlas(string name, Action<SpriteAtlas> funcReg)
        {
#if FIX_LOAD_ATLAS_IN_ASSET_BUNDLE
            _AtlasRegFunc = funcReg;
#endif
            var atlas = ResManager.LoadRes("atlas/" + name, typeof(SpriteAtlas)) as SpriteAtlas;
            if (atlas)
            {
#if FIX_LOAD_ATLAS_CRASH_ON_DISPOSED_SPRITE
                for (var i = TrackedImages.Count - 1; i >= 0; --i)
                {
                    var g = TrackedImages[i];
                    var sprite = g.overrideSprite != null ? g.overrideSprite : g.sprite;
                    if (!sprite)
                    {
                        TrackedImages.RemoveAt(i);
                    }
                }
#endif
                funcReg(atlas);
            }
        }
    }
}