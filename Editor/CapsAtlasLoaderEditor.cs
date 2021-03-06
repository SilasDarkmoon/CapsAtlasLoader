﻿using System;
using System.Linq;
using System.Collections;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Capstones.UnityEngineEx;
using UnityEngine.U2D;
using UnityEditor.Graphs;
using UnityEditor.U2D;

namespace Capstones.UnityEditorEx
{
    [InitializeOnLoad]
    public class CapsAtlasLoaderEditor
    {
        internal static readonly Dictionary<string, string> _CachedAtlas = new Dictionary<string, string>();
        internal static readonly Dictionary<string, string> _CachedAtlasRev = new Dictionary<string, string>();
        //internal static readonly Dictionary<string, List<string>> _TexInAtlas = new Dictionary<string, List<string>>();
        static readonly Dictionary<string, string> _CachedAtlasSpriteGUID = new Dictionary<string, string>();
        static readonly Dictionary<string, string> _CachedAtlasPath = new Dictionary<string, string>();

        static CapsAtlasLoaderEditor()
        {
            if (PlatDependant.IsFileExist("EditorOutput/Runtime/atlas.txt"))
            {
                LoadCachedAtlas();
            }
            else
            {
                CacheAllAtlas();
                SaveCachedAtlas();
            }

            UnityEngine.U2D.SpriteAtlasManager.atlasRequested += (name, funcReg) =>
            {
                if (UnityEditor.EditorSettings.spritePackerMode == UnityEditor.SpritePackerMode.AlwaysOnAtlas && !(ResManager.ResLoader is ResManager.ClientResLoader))
                {
                    string assetName;
                    if (_CachedAtlas.TryGetValue(name, out assetName))
                    {
                        var atlas = AssetDatabase.LoadAssetAtPath<UnityEngine.U2D.SpriteAtlas>(assetName);
                        if (atlas)
                        {
                            funcReg(atlas);
                        }
                    }
                }
            };
        }

        public static void LoadCachedAtlas()
        {
            _CachedAtlas.Clear();
            _CachedAtlasRev.Clear();
            //_TexInAtlas.Clear();
            if (PlatDependant.IsFileExist("EditorOutput/Runtime/atlas.txt"))
            {
                string json = "";
                using (var sr = PlatDependant.OpenReadText("EditorOutput/Runtime/atlas.txt"))
                {
                    json = sr.ReadToEnd();
                }
                try
                {
                    var jo = new JSONObject(json);
                    try
                    {
                        var joc = jo["atlas"] as JSONObject;
                        if (joc != null && joc.type == JSONObject.Type.ARRAY)
                        {
                            for (int i = 0; i < joc.list.Count; ++i)
                            {
                                var val = joc.list[i].str;
                                var name = System.IO.Path.GetFileNameWithoutExtension(val);
                                _CachedAtlas[name] = val;
                                _CachedAtlasRev[val] = name;
                            }
                        }
                        //joc = jo["tex"] as JSONObject;
                        //if (joc != null && joc.type == JSONObject.Type.OBJECT)
                        //{
                        //    for (int i = 0; i < joc.list.Count; ++i)
                        //    {
                        //        var key = joc.keys[i];
                        //        var val = joc.list[i];
                        //        if (val != null && val.type == JSONObject.Type.ARRAY)
                        //        {
                        //            var list = new List<string>();
                        //            _TexInAtlas[key] = list;
                        //            for (int j = 0; j < val.list.Count; ++j)
                        //            {
                        //                list.Add(val.list[i].str);
                        //            }
                        //        }
                        //    }
                        //}
                    }
                    catch { }
                }
                catch { }
            }
        }
        public static void SaveCachedAtlas()
        {
            var jo = new JSONObject(JSONObject.Type.OBJECT);
            var joc = new JSONObject(JSONObject.Type.ARRAY);
            jo["atlas"] = joc;
            foreach (var asset in _CachedAtlasRev.Keys)
            {
                joc.Add(asset);
            }

            //joc = new JSONObject(JSONObject.Type.OBJECT);
            //jo["tex"] = joc;
            //foreach (var kvp in _TexInAtlas)
            //{
            //    var name = kvp.Key;
            //    var list = kvp.Value;
            //    if (list != null && list.Count > 0)
            //    {
            //        var jlist = new JSONObject(JSONObject.Type.ARRAY);
            //        joc[name] = jlist;
            //        for (int i = 0; i < list.Count; ++i)
            //        {
            //            jlist.Add(list[i]);
            //        }
            //    }
            //}

            using (var sw = PlatDependant.OpenWriteText("EditorOutput/Runtime/atlas.txt"))
            {
                sw.Write(jo.ToString(true));
            }
        }

        public static void CacheAllAtlas()
        {
            var assets = AssetDatabase.GetAllAssetPaths();
            for (int i = 0; i < assets.Length; ++i)
            {
                var asset = assets[i];
                if (asset.EndsWith(".spriteatlas"))
                {
                    AddAtlasToCache(asset);
                }
            }
        }

        public static bool AddAtlasToCache(string assetpath)
        {
            var atlas = AssetDatabase.LoadAssetAtPath<UnityEngine.U2D.SpriteAtlas>(assetpath);
            if (atlas && !atlas.isVariant)
            {
                var name = atlas.tag;
                string oldasset;
                if (_CachedAtlas.TryGetValue(name, out oldasset))
                {
                    if (oldasset == assetpath)
                    {
                        return false;
                        //return ParsePackedTex(atlas, name);
                    }
                    else
                    {
                        string folder = System.IO.Path.GetDirectoryName(assetpath).Replace('\\', '/') + "/";
                        var ext = System.IO.Path.GetExtension(assetpath);
                        string rawname = name;
                        ulong seq = 0;
                        int index = -1;
                        for (int i = name.Length - 1; i >= 0; --i)
                        {
                            var ch = name[i];
                            if (ch < '0' || ch > '9')
                            {
                                break;
                            }
                            index = i;
                        }
                        if (index >= 0)
                        {
                            rawname = name.Substring(0, index);
                            ulong.TryParse(name.Substring(index), out seq);
                        }

                        if (!_CachedAtlas.ContainsKey(rawname))
                        {
                            var newasset = folder + rawname + ext;
                            _CachedAtlas[rawname] = newasset;
                            _CachedAtlasRev[newasset] = rawname;
                            //ParsePackedTex(atlas, rawname);
                            AssetDatabase.MoveAsset(assetpath, newasset);
                        }
                        else
                        {
                            while (true)
                            {
                                var newname = rawname + seq.ToString();
                                if (!_CachedAtlas.ContainsKey(newname))
                                {
                                    var newasset = folder + newname + ext;
                                    _CachedAtlas[newname] = newasset;
                                    _CachedAtlasRev[newasset] = newname;
                                    //ParsePackedTex(atlas, newname);
                                    AssetDatabase.MoveAsset(assetpath, newasset);
                                    break;
                                }
                                ++seq;
                            }
                        }
                        return true;
                    }
                }
                else
                {
                    _CachedAtlas[name] = assetpath;
                    _CachedAtlasRev[assetpath] = name;
                    //ParsePackedTex(atlas, name);
                    return true;
                }
            }
            return false;
        }
        public static void RemoveAtlasFromCache(string assetpath)
        {
            string name;
            if (_CachedAtlasRev.TryGetValue(assetpath, out name))
            {
                _CachedAtlasRev.Remove(assetpath);
                _CachedAtlas.Remove(name);
                //_TexInAtlas.Remove(name);
            }
        }

        //private static bool ParsePackedTex(UnityEngine.U2D.SpriteAtlas atlas, string name)
        //{
        //    if (atlas)
        //    {
        //        name = name ?? atlas.tag;
        //        HashSet<string> oldset = new HashSet<string>();
        //        List<string> oldlist;
        //        if (_TexInAtlas.TryGetValue(name, out oldlist))
        //        {
        //            oldset.UnionWith(oldlist);
        //        }

        //        bool changed = false;
        //        var subs = UnityEditor.U2D.SpriteAtlasExtensions.GetPackables(atlas);
        //        HashSet<string> newset = new HashSet<string>();
        //        if (subs != null)
        //        {
        //            List<string> newlist = new List<string>();
        //            for (int i = 0; i < subs.Length; ++i)
        //            {
        //                var sub = subs[i];
        //                var path = AssetDatabase.GetAssetPath(sub);
        //                if (!string.IsNullOrEmpty(path))
        //                {
        //                    newlist.Add(path);
        //                    newset.Add(path);
        //                    if (!oldset.Contains(path))
        //                    {
        //                        changed = true;
        //                    }
        //                }
        //            }
        //            _TexInAtlas[name] = newlist;
        //        }
        //        else
        //        {
        //            _TexInAtlas.Remove(name);
        //        }
        //        return changed || newset.Count != oldset.Count;
        //    }
        //    return false;
        //}

        private class CapsAtlasPostprocessor : AssetPostprocessor
        {
            private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
            {
                bool dirty = false;
                if (deletedAssets != null)
                {
                    for (int i = 0; i < deletedAssets.Length; ++i)
                    {
                        var asset = deletedAssets[i];
                        if (asset.EndsWith(".spriteatlas"))
                        {
                            dirty = true;
                            RemoveAtlasFromCache(asset);
                        }
                    }
                }
                if (movedFromAssetPaths != null)
                {
                    for (int i = 0; i < movedFromAssetPaths.Length; ++i)
                    {
                        var asset = movedFromAssetPaths[i];
                        if (asset.EndsWith(".spriteatlas"))
                        {
                            dirty = true;
                            RemoveAtlasFromCache(asset);
                        }
                    }
                }
                if (importedAssets != null)
                {
                    for (int i = 0; i < importedAssets.Length; ++i)
                    {
                        var asset = importedAssets[i];
                        if (asset.EndsWith(".spriteatlas"))
                        {
                            dirty |= AddAtlasToCache(asset);
                        }
                    }
                }
                if (movedAssets != null)
                {
                    for (int i = 0; i < movedAssets.Length; ++i)
                    {
                        var asset = movedAssets[i];
                        if (asset.EndsWith(".spriteatlas"))
                        {
                            dirty |= AddAtlasToCache(asset);
                        }
                    }
                }
                if (dirty)
                {
                    SaveCachedAtlas();
                }
            }
        }

        public static void SetCurrentAtlasProperties(string profile)
        {
            var path = CapsModEditor.FindAssetInMods("AtlasTemplate_" + profile + ".spriteatlas");
            if (path != null)
            {
                var template = AssetDatabase.LoadAssetAtPath<UnityEngine.U2D.SpriteAtlas>(path);
                if (template != null)
                {
                    var selections = Selection.assetGUIDs;
                    if (selections != null)
                    {
                        for (int i = 0; i < selections.Length; ++i)
                        {
                            var sel = selections[i];
                            var atlaspath = AssetDatabase.GUIDToAssetPath(sel);
                            if (atlaspath != null)
                            {
                                var atlas = AssetDatabase.LoadAssetAtPath<UnityEngine.U2D.SpriteAtlas>(atlaspath);
                                if (atlas)
                                {
                                    UnityEditor.U2D.SpriteAtlasExtensions.SetIncludeInBuild(atlas, false);
                                    UnityEditor.U2D.SpriteAtlasExtensions.SetPackingSettings(atlas, UnityEditor.U2D.SpriteAtlasExtensions.GetPackingSettings(template));
                                    UnityEditor.U2D.SpriteAtlasExtensions.SetTextureSettings(atlas, UnityEditor.U2D.SpriteAtlasExtensions.GetTextureSettings(template));

                                    UnityEditor.U2D.SpriteAtlasExtensions.SetPlatformSettings(atlas, UnityEditor.U2D.SpriteAtlasExtensions.GetPlatformSettings(template, "DefaultTexturePlatform"));
                                    var buildTargetNames = Enum.GetNames(typeof(BuildTargetGroup));
                                    for (int j = 0; j < buildTargetNames.Length; ++j)
                                    {
                                        var platsettings = UnityEditor.U2D.SpriteAtlasExtensions.GetPlatformSettings(template, buildTargetNames[j]);
                                        if (platsettings != null && platsettings.overridden)
                                        {
                                            UnityEditor.U2D.SpriteAtlasExtensions.SetPlatformSettings(atlas, platsettings);

                                            BuildTargetGroup bgroup;
                                            Enum.TryParse(buildTargetNames[j], out bgroup);
                                            for (int k = 0; k < buildTargetNames.Length; ++k)
                                            {
                                                BuildTargetGroup bgroupcur;
                                                Enum.TryParse(buildTargetNames[k], out bgroupcur);
                                                if (bgroup == bgroupcur)
                                                {
                                                    BuildTarget btar;
                                                    if (Enum.TryParse(buildTargetNames[k], out btar))
                                                    {
                                                        Debug.LogFormat("Now packing {0} on {1}.", atlas.name, btar);
                                                        UnityEditor.U2D.SpriteAtlasUtility.PackAtlases(new UnityEngine.U2D.SpriteAtlas[] { atlas }, btar, false);
                                                        Debug.LogFormat("Packing done {0} on {1}.", atlas.name, btar);
                                                    }
                                                }
                                            }
                                        }
                                    }

                                    RenameAtlasName(atlaspath);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                Debug.LogError("Please create AtlasTemplate_" + profile + ".spriteatlas in any mod folder.");
            }
        }

        [MenuItem("Atlas/Set Atlas Settings - Low", priority = 100010)]
        public static void SetCurrentAtlasPropertiesLow()
        {
            SetCurrentAtlasProperties("Low");
        }
        [MenuItem("Atlas/Set Atlas Settings - High", priority = 100020)]
        public static void SetCurrentAtlasPropertiesHigh()
        {
            SetCurrentAtlasProperties("High");
        }

        [MenuItem("Atlas/Show Sprite In Which Atlas", priority = 100040)]
        private static void ShowSpriteWhichAtlas()
        {
            LoadCachedAtlas2();
            EditorApplication.projectWindowItemOnGUI += ProjectWindowItemOnGUI;
        }

        [MenuItem("Atlas/Goto Packed atlas", priority = 100041)]
        public static void GotoPackedAtlas()
        {
            LoadCachedAtlas2();
            var assets = Selection.objects;
            if (assets != null && assets.Length > 0)
            {
                List<SpriteAtlas> trans = new List<SpriteAtlas>();
                foreach (var asset in assets)
                {
                    string spPath = AssetDatabase.GetAssetPath(asset);
                    string spGuid = AssetDatabase.AssetPathToGUID(spPath);

                    string atlasPath;
                    if (_CachedAtlasPath.TryGetValue(spGuid, out atlasPath))
                    {
                        SpriteAtlas ob = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
                        trans.Add(ob);
                    }
                }

                if (trans.Count > 0)
                {
                    ProjectWindowUtil.ShowCreatedAsset(trans[0]);
                    Selection.objects = trans.ToArray();
                }
            }
        }

        private static void LoadCachedAtlas2()
        {
            _CachedAtlasSpriteGUID.Clear();
            _CachedAtlasPath.Clear();
            if (PlatDependant.IsFileExist("EditorOutput/Runtime/atlas.txt"))
            {
                string json = "";
                using (var sr = PlatDependant.OpenReadText("EditorOutput/Runtime/atlas.txt"))
                {
                    json = sr.ReadToEnd();
                }
                try
                {
                    var jo = new JSONObject(json);
                    try
                    {
                        var joc = jo["atlas"] as JSONObject;
                        if (joc != null && joc.type == JSONObject.Type.ARRAY)
                        {
                            for (int i = 0; i < joc.list.Count; ++i)
                            {
                                var val = joc.list[i].str;
                                SaveSpriteGUID(val);
                            }
                        }
                    }
                    catch { }
                }
                catch { }
            }
        }

        private static void SaveSpriteGUID(string atlasPath)
        {
            SpriteAtlas sa = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
            if (sa != null)
            {
                string atlasName = Path.GetFileNameWithoutExtension(atlasPath);
                string[] list = atlasName.Split('-');
                int size = list.Length;
                if (size < 3)
                {
                    return;
                }
                StringBuilder sb = new StringBuilder();
                sb.Append(list[size - 4]).Append('-').Append(list[size - 3]).Append('-').Append(list[size - 1]);
                string shortName = sb.ToString();
                Sprite[] sps = new Sprite[sa.spriteCount];
                int ret = sa.GetSprites(sps);
                for (int i = 0; i < sps.Length; i++)
                {
                    Sprite item = sps[i];
                    string spPath = AssetDatabase.GetAssetPath(item.texture);
                    string guid = AssetDatabase.AssetPathToGUID(spPath);
                    _CachedAtlasSpriteGUID[guid] = shortName;
                    _CachedAtlasPath[guid] = atlasPath;
                }
            }
        }

        private static void ProjectWindowItemOnGUI(string guid, Rect rect)
        {
            string atlasName;
            if (_CachedAtlasSpriteGUID.TryGetValue(guid, out atlasName))
            {
                var centeredStyle = GUI.skin.GetStyle("Label");
                centeredStyle.alignment = TextAnchor.MiddleRight;
                centeredStyle.padding.right = 5;
                GUI.Label(rect, atlasName, centeredStyle);
                EditorApplication.RepaintProjectWindow();
            }
            else
            {
                var centeredStyle = GUI.skin.GetStyle("Label");
                centeredStyle.alignment = TextAnchor.UpperLeft;
                GUI.Label(rect, "", centeredStyle);
                EditorApplication.RepaintProjectWindow();
            }
        }

        private static void RenameAtlasName(string atlaspath)
        {
            string type;
            string mod;
            string dist;
            string folder = Path.GetDirectoryName(atlaspath);
            string ret = ResManager.GetAssetNormPath(folder, out type, out mod, out dist);

            StringBuilder sb = new StringBuilder();
            sb.Append("m").Append("-").Append(mod.ToLower()).Append("-").Append("d").Append("-").Append(dist.ToLower()).Append("-");
            sb.Append(ret.Replace('/', '-')).Append("-");
            string newNamePre = sb.ToString().ToLower();
            if (atlaspath.Contains(newNamePre))
            {
                return;
            }
            
            int subIndex = 0;
            while (true)
            {
                ++subIndex;
                bool isExists = File.Exists(folder + '/' + newNamePre + subIndex + ".spriteatlas");
                if (!isExists)
                {
                    break;
                }
            }
            string newName = newNamePre + subIndex + ".spriteatlas";
            AssetDatabase.RenameAsset(atlaspath, newName);
        }
    }
}