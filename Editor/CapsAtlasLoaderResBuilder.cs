using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Capstones.UnityEngineEx;

namespace Capstones.UnityEditorEx
{
    public class CapsAtlasLoaderResBuilder : CapsResBuilder.IResBuilderEx
    {
        private string _Output;
        private readonly Dictionary<string, string> _OldMap = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _NewMap = new Dictionary<string, string>();
        private readonly HashSet<string> _FullSet = new HashSet<string>();

        public void Prepare(string output)
        {
            _Output = output;
            _OldMap.Clear();
            _NewMap.Clear();
            _FullSet.Clear();

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
                                var val = joc.list[i].str;
                                _OldMap[key] = val;
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
                                        _NewMap[path] = name;
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
                var val = kvp.Value;
                if (!_NewMap.ContainsKey(key) || _NewMap[key] != val)
                {
                    _FullSet.Add(key);
                }
            }
            foreach (var kvp in _NewMap)
            {
                var key = kvp.Key;
                var val = kvp.Value;
                if (!_OldMap.ContainsKey(key) || _OldMap[key] != val)
                {
                    _FullSet.Add(key);
                }
            }
        }
        public void Cleanup()
        {
        }
        public void OnSuccess()
        {
            var jo = new JSONObject(JSONObject.Type.OBJECT);
            var joc = new JSONObject(_NewMap);
            jo["tex"] = joc;

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

        public string FormatBundleName(string asset, string mod, string dist, string norm)
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
                }
            }
            return null;
        }
        public bool CreateItem(CapsResManifestNode node)
        {
            return false;
        }
        public void ModifyItem(CapsResManifestItem item)
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

                var newpath = rootpath + _Building.AtlasName;
                CapsResManifestNode newnode = item.Manifest.AddOrGetItem(newpath);
                var newitem = new CapsResManifestItem(newnode);
                newitem.Type = (int)CapsResManifestItemType.Redirect;
                newitem.BRef = item.BRef;
                newitem.Ref = item;
                newnode.Item = newitem;
            }
        }

        public void GenerateBuildWork(string bundleName, IList<string> assets, ref AssetBundleBuild abwork, CapsResBuilder.CapsResBuildWork modwork, int abindex)
        {
            if (assets != null)
            {
                for (int i = 0; i < assets.Count; ++i)
                {
                    var asset = assets[i];
                    if (_FullSet.Contains(asset))
                    {
                        modwork.ForceRefreshABs.Add(abindex);
                        break;
                    }
                }
            }
        }
    }

    [InitializeOnLoad]
    public static class CapsAtlasResBuilderEntry
    {
        private static CapsAtlasLoaderResBuilder _Builder = new CapsAtlasLoaderResBuilder();
        static CapsAtlasResBuilderEntry()
        {
            CapsResBuilder.ResBuilderEx.Add(_Builder);
        }
    }
}