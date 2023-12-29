using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Capstones.UnityEngineEx;

namespace Capstones.UnityEditorEx
{
    [InitializeOnLoad]
    public class CapsAtlasLoaderResBuilder : CapsResBuilder.BaseResBuilderEx<CapsAtlasLoaderResBuilder>
    {
        private static HierarchicalInitializer _Initializer = new HierarchicalInitializer(0);

        public const int CapsResManifestItemType_Atlas = 5;
        private class SpriteInAtlasInfo
        {
            public string SpriteFile;
            public string AtlasName;
            public string SpriteMD5;
        }

        private string _Output;
        private readonly Dictionary<string, SpriteInAtlasInfo> _OldMap = new Dictionary<string, SpriteInAtlasInfo>();
        private readonly Dictionary<string, SpriteInAtlasInfo> _NewMap = new Dictionary<string, SpriteInAtlasInfo>();
        private readonly HashSet<string> _SpriteSet = new HashSet<string>();
        private readonly HashSet<string> _AtlasSet = new HashSet<string>();
        private readonly HashSet<string> _AtlasAssetSet = new HashSet<string>();

        public override void Prepare(string output)
        {
            _Output = output;
            _OldMap.Clear();
            _NewMap.Clear();
            _SpriteSet.Clear();
            _AtlasSet.Clear();
            _AtlasAssetSet.Clear();

            if (!string.IsNullOrEmpty(output))
            {
                var cachefile = output + "/res/inatlas.txt";
                if (PlatDependant.IsFileExist(cachefile))
                {
                    try
                    {
                        string json = "";
                        using (var sr = PlatDependant.OpenReadText(cachefile))
                        {
                            json = sr.ReadToEnd();
                        }
                        var jo = new JSONObject(json);
                        var joc = jo["tex"] as JSONObject;
                        if (joc != null && joc.type == JSONObject.Type.OBJECT)
                        {
                            for (int i = 0; i < joc.list.Count; ++i)
                            {
                                var key = joc.keys[i];
                                var jinfo = joc.list[i];
                                if (jinfo.type == JSONObject.Type.STRING)
                                {
                                    var val = joc.list[i].str;
                                    _OldMap[key] = new SpriteInAtlasInfo() { SpriteFile = key, AtlasName = val, SpriteMD5 = null };
                                }
                                else if (jinfo.type == JSONObject.Type.OBJECT)
                                {
                                    var atlas = jinfo["atlas"];
                                    if (atlas != null)
                                    {
                                        if (atlas.type == JSONObject.Type.STRING)
                                        {
                                            var val = atlas.str;
                                            string md5 = null;
                                            var md5node = jinfo["md5"];
                                            if (md5node != null && md5node.type == JSONObject.Type.STRING)
                                            {
                                                md5 = md5node.str;
                                            }
                                            _OldMap[key] = new SpriteInAtlasInfo() { SpriteFile = key, AtlasName = val, SpriteMD5 = md5 };
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                }
            }

            var assets = AssetDatabase.GetAllAssetPaths();
            if (assets != null)
            {
                for (int i = 0; i < assets.Length; ++i)
                {
                    var asset = assets[i];
                    if (asset.EndsWith(".spriteatlas"))
                    {
                        var atlas = AssetDatabase.LoadAssetAtPath<UnityEngine.U2D.SpriteAtlas>(asset);
                        if (atlas && !atlas.isVariant)
                        {
                            var name = atlas.tag;
                            var packed = CapsAtlasLoaderEditor.GetPackedPathsInAtlas(asset);
                            if (packed != null)
                            {
                                for (int j = 0; j < packed.Length; ++j)
                                {
                                    var path = packed[j];
                                    if (!string.IsNullOrEmpty(path))
                                    {
                                        var md5 = CapsEditorUtils.GetFileMD5(path) + "-" + CapsEditorUtils.GetFileLength(path);
                                        _NewMap[path] = new SpriteInAtlasInfo() { SpriteFile = path, AtlasName = name, SpriteMD5 = md5 };
                                    }
                                }
                            }
                        }
                    }
                }
            }

            foreach (var kvp in _OldMap)
            {
                var key = kvp.Key;
                var oldinfo = kvp.Value;
                SpriteInAtlasInfo newinfo;
                if (!_NewMap.TryGetValue(key, out newinfo) || newinfo.AtlasName != oldinfo.AtlasName)
                {
                    _SpriteSet.Add(key);
                }
                else if (newinfo.SpriteMD5 != oldinfo.SpriteMD5)
                {
                    _AtlasSet.Add(newinfo.AtlasName);
                }
            }
            foreach (var kvp in _NewMap)
            {
                var key = kvp.Key;
                var newinfo = kvp.Value;
                SpriteInAtlasInfo oldinfo;
                if (!_OldMap.TryGetValue(key, out oldinfo) || newinfo.AtlasName != oldinfo.AtlasName)
                {
                    _SpriteSet.Add(key);
                }
                else if (newinfo.SpriteMD5 != oldinfo.SpriteMD5)
                {
                    _AtlasSet.Add(newinfo.AtlasName);
                }
            }
        }
        public override void Cleanup()
        {
            PlatDependant.DeleteFile("Assets/StreamingAssets/res/inatlas.txt");
        }
        public override void OnSuccess()
        {
            var jo = new JSONObject(JSONObject.Type.OBJECT);
            var joc = new JSONObject(JSONObject.Type.OBJECT);
            jo["tex"] = joc;

            foreach (var kvp in _NewMap)
            {
                var key = kvp.Key;
                var info = kvp.Value;
                var jot = new JSONObject(JSONObject.Type.OBJECT);
                joc[key] = jot;

                jot["atlas"] = new JSONObject(JSONObject.Type.STRING) { str = info.AtlasName };
                jot["md5"] = new JSONObject(JSONObject.Type.STRING) { str = info.SpriteMD5 };
            }

            var cachefile = _Output + "/res/inatlas.txt";
            using (var sw = PlatDependant.OpenWriteText(cachefile))
            {
                sw.Write(jo.ToString(true));
            }
        }

        private class BuildingItemInfo
        {
            public string Asset;
            public string Mod;
            public string Dist;
            public string Norm;
            public string AtlasName;
        }
        private BuildingItemInfo _Building;

        public override string FormatBundleName(string asset, string mod, string dist, string norm)
        {
            _Building = null;
            if (asset.EndsWith("spriteatlas"))
            {
                var atlas = AssetDatabase.LoadAssetAtPath<UnityEngine.U2D.SpriteAtlas>(asset);
                if (atlas)
                {
                    _Building = new BuildingItemInfo()
                    {
                        Asset = asset,
                        Mod = mod,
                        Dist = dist,
                        Norm = norm,
                        AtlasName = atlas.tag,
                    };
                    if (_AtlasSet.Contains(atlas.tag))
                    {
                        _AtlasAssetSet.Add(asset);
                    }
                }
            }
            return null;
        }
        public override void ModifyItem(CapsResManifestItem item)
        {
            if (_Building != null)
            {
                var asset = _Building.Asset;
                string rootpath = "Assets/CapsRes/";
                bool inPackage = false;
                if (asset.StartsWith("Assets/Mods/") || (inPackage = asset.StartsWith("Packages/")))
                {
                    int index;
                    if (inPackage)
                    {
                        index = asset.IndexOf('/', "Packages/".Length);
                    }
                    else
                    {
                        index = asset.IndexOf('/', "Assets/Mods/".Length);
                    }
                    if (index > 0)
                    {
                        rootpath = asset.Substring(0, index) + "/CapsRes/";
                    }
                }
                var dist = _Building.Dist;
                if (string.IsNullOrEmpty(dist))
                {
                    rootpath += "atlas/";
                }
                else
                {
                    rootpath = rootpath + "dist/" + dist + "/atlas/";
                }

                item.Type = CapsResManifestItemType_Atlas;

                var newpath = rootpath + _Building.AtlasName;
                CapsResManifestNode newnode = item.Manifest.AddOrGetItem(newpath);
                var newitem = new CapsResManifestItem(newnode);
                newitem.Type = (int)CapsResManifestItemType.Redirect;
                newitem.BRef = item.BRef;
                newitem.Ref = item;
                newnode.Item = newitem;
            }
        }

        public override void GenerateBuildWork(string bundleName, IList<string> assets, CapsResBuilder.IBundleBuildInfo bwork, CapsResBuilder.ICapsResBuildWork modwork, int abindex)
        {
            if (assets != null)
            {
                for (int i = 0; i < assets.Count; ++i)
                {
                    var asset = assets[i];
                    if (_SpriteSet.Contains(asset) || _AtlasAssetSet.Contains(asset))
                    {
                        modwork.ForceRefreshBundles.Add(abindex);
                        break;
                    }
                }
            }
        }
    }
}