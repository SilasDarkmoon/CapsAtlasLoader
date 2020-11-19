using System;
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
        //        var subs = UnityEditor.U2D.SpriteAtlasExtensions.GetPackables(atlas); // should change to atlas.GetSprites
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
                                    if (!atlas.isVariant)
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
        [MenuItem("Atlas/Rename Atlas", priority = 100030)]
        public static void RenameCurrentAtlas()
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
                        if (!atlas.isVariant)
                        {
                            RenameAtlasName(atlaspath);
                        }
                    }
                }
                for (int i = 0; i < selections.Length; ++i)
                {
                    var sel = selections[i];
                    var atlaspath = AssetDatabase.GUIDToAssetPath(sel);
                    if (atlaspath != null)
                    {
                        var atlas = AssetDatabase.LoadAssetAtPath<UnityEngine.U2D.SpriteAtlas>(atlaspath);
                        if (atlas.isVariant)
                        {
                            RenameAtlasName(atlaspath);
                        }
                    }
                }
            }
        }

        [MenuItem("Atlas/Create Atlas Variant", priority = 100040)]
        public static void CreateAtlasVariant()
        {
            var guids = Selection.assetGUIDs;
            if (guids != null)
            {
                for (int i = 0; i < guids.Length; ++i)
                {
                    var guid = guids[i];
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (path.EndsWith(".spriteatlas"))
                    {
                        var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
                        if (atlas && !atlas.isVariant)
                        {
                            string type, mod, dist;
                            string norm = ResManager.GetAssetNormPath(path, out type, out mod, out dist);
                            var movePath = "Assets/";
                            if (!string.IsNullOrEmpty(mod))
                            {
                                movePath += "Mods/" + mod + "/";
                            }
                            movePath += "CapsRes/dist/largeatlas";
                            if (!string.IsNullOrEmpty(dist))
                            {
                                movePath += "_" + dist;
                            }
                            movePath += "/" + norm;
                            var movedir = Path.GetDirectoryName(movePath);
                            Directory.CreateDirectory(movedir);
                            AssetDatabase.ImportAsset(movedir);
                            AssetDatabase.MoveAsset(path, movePath);

                            SpriteAtlas vatlas = new SpriteAtlas();
                            vatlas.SetIsVariant(true);
                            vatlas.SetMasterAtlas(atlas);
                            vatlas.SetVariantScale(0.5f);
                            vatlas.SetIncludeInBuild(false);
                            AssetDatabase.CreateAsset(vatlas, path);
                        }
                    }
                }
            }
        }
        [MenuItem("Atlas/Move Master Atlas Back", priority = 100050)]
        public static void MoveBackMasterAtlas()
        {
            var guids = Selection.assetGUIDs;
            if (guids != null)
            {
                for (int i = 0; i < guids.Length; ++i)
                {
                    var guid = guids[i];
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (path.EndsWith(".spriteatlas"))
                    {
                        var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
                        if (atlas && atlas.isVariant)
                        {
                            var master = new SerializedObject(atlas).FindProperty("m_MasterAtlas").objectReferenceValue as SpriteAtlas;
                            var masterpath = AssetDatabase.GetAssetPath(master);
                            AssetDatabase.DeleteAsset(path);
                            AssetDatabase.MoveAsset(masterpath, path);
                        }
                    }
                }
            }
        }

        public static string[] GetPackedPathsInAtlas(string atlasPath)
        {
            List<string> rv = new List<string>();
            var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
            if (atlas)
            {
                var packables = atlas.GetPackables();
                if (packables != null)
                {
                    //Debug.Log("Packables in " + atlasPath);
                    for (int j = 0; j < packables.Length; ++j)
                    {
                        var pack = packables[j];
                        //Debug.Log(pack);
                        var path = AssetDatabase.GetAssetPath(pack);
                        if (System.IO.Directory.Exists(path))
                        {
                            var subs = PlatDependant.GetAllFiles(path);
                            if (subs != null)
                            {
                                for (int i = 0; i < subs.Length; ++i)
                                {
                                    var sub = subs[i].Replace('\\', '/');
                                    if (AssetDatabase.LoadAssetAtPath<Texture>(sub))
                                    {
                                        rv.Add(sub);
                                    }
                                }
                            }
                        }
                        else
                        {
                            rv.Add(path);
                        }
                    }
                }
            }
            //Debug.Log("Sprites in " + atlasPath);
            //for (int i = 0; i < rv.Count; ++i)
            //{
            //    Debug.Log(rv[i]);
            //}
            return rv.ToArray();
        }

        [MenuItem("Atlas/Show Sprite In Which Atlas", priority = 100110)]
        private static void ShowSpriteWhichAtlas()
        {
            LoadCachedAtlas2();
            EditorApplication.projectWindowItemOnGUI += ProjectWindowItemOnGUI;
        }

        [MenuItem("Atlas/Goto Packed atlas", priority = 100120)]
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

        [MenuItem("Atlas/Show Multi-Binded Sprites", priority = 100130)]
        public static void ShowMultiBindedSprites()
        {
            Dictionary<string, string> sprite2atlas = new Dictionary<string, string>();
            foreach (var kvp in _CachedAtlas)
            {
                var atlas = kvp.Value;
                var loaded = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlas);
                if (loaded)
                {
                    if (!loaded.isVariant)
                    {
                        var packed = GetPackedPathsInAtlas(atlas);
                        foreach (var sprite in packed)
                        {
                            string existing;
                            if (sprite2atlas.TryGetValue(sprite, out existing))
                            {
                                Debug.LogError(sprite + " is in multiple atlas: " + existing + " & " + atlas);
                            }
                            else
                            {
                                sprite2atlas[sprite] = atlas;
                            }
                        }
                    }
                }
            }
        }
        [MenuItem("Atlas/Show SpriteAtlas with Same Tag", priority = 100140)]
        public static void ShowSpriteAtlasNameConflict()
        {
            Dictionary<string, string> tag2path = new Dictionary<string, string>();
            var allassets = AssetDatabase.GetAllAssetPaths();
            for (int i = 0; i < allassets.Length; ++i)
            {
                var path = allassets[i];
                if (path.EndsWith(".spriteatlas"))
                {
                    var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
                    if (!atlas)
                    {
                        Debug.LogError("Failed to load " + path);
                    }
                    else
                    {
                        if (!atlas.isVariant)
                        {
                            var tag = atlas.tag;
                            var consideredTag = Path.GetFileNameWithoutExtension(path);
                            if (consideredTag.ToLower() != consideredTag)
                            {
                                Debug.LogError("Atlas with upper-case name: " + path);
                            }
                            if (consideredTag != tag)
                            {
                                Debug.LogError("Atlas tag differ from filename: " + path + ", tag: " + tag);
                            }
                            if (tag2path.ContainsKey(tag))
                            {
                                Debug.LogError("Atlas tag conflict: " + path + ", tag: " + tag + ", conflict: " + tag2path[tag]);
                            }
                            else
                            {
                                tag2path[tag] = path;
                            }
                        }
                    }
                }
            }
        }

        [MenuItem("Atlas/SetTextureCompression", priority = 100610)]
        public static void SetTextureCompression()
        {
            var assets = Selection.objects;
            int size = assets.Length;
            if (assets != null && size > 0)
            {
                var tmpPath = CapsModEditor.FindAssetInMods("TextureTemplate.png");
                var textureTmpImporter = TextureImporter.GetAtPath(tmpPath) as TextureImporter;
                TextureImporterPlatformSettings androidTmpSettings = textureTmpImporter.GetPlatformTextureSettings("Android");
                TextureImporterPlatformSettings iosTmpSettings = textureTmpImporter.GetPlatformTextureSettings("iOS");
                for (int i = 0; i < size; i++)
                {
                    
                    Texture tex = assets[i] as Texture;
                    string path = AssetDatabase.GetAssetPath(tex);
                    string name = Path.GetFileName(path);
                    EditorUtility.DisplayProgressBar("===设置中===", name, i / size);
                    if (tex && (path.EndsWith(".png") ||
                        path.EndsWith(".jpg") ||
                        path.EndsWith(".tga") ||
                        path.EndsWith(".psd") ||
                        path.EndsWith(".bmp") ||
                        path.EndsWith(".tif") ||
                        path.EndsWith(".gif")))
                    {
                        TextureImporter texImporter = TextureImporter.GetAtPath(path) as TextureImporter;

                        TextureImporterPlatformSettings androidSettings = texImporter.GetPlatformTextureSettings("Android");
                        androidSettings.overridden = true;
                        androidSettings.maxTextureSize = 512;
                        androidSettings.format = androidTmpSettings.format;
                        androidSettings.compressionQuality = androidTmpSettings.compressionQuality;
                        texImporter.SetPlatformTextureSettings(androidSettings);

                        TextureImporterPlatformSettings iosSettings = texImporter.GetPlatformTextureSettings("iOS");
                        iosSettings.overridden = true;
                        iosSettings.maxTextureSize = 512;
                        iosSettings.format = iosTmpSettings.format;
                        iosSettings.compressionQuality = iosTmpSettings.compressionQuality;
                        texImporter.SetPlatformTextureSettings(iosSettings);

                        AssetDatabase.SaveAssets();
                        DoAssetReimport(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                    }
                }

                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("成功", "处理完成！", "好的");
            }
        }

        [MenuItem("Atlas/Change ETC1 TO ETC2", priority = 100611)]
        public static void Change2ETC2()
        {
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
                            int size = joc.list.Count;
                            for (int i = 0; i < size; ++i)
                            {
                                var atlaspath = joc.list[i].str;
                                EditorUtility.DisplayProgressBar("===设置中===", atlaspath, i / size);
                                var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlaspath);
                                TextureImporterPlatformSettings tips = SpriteAtlasExtensions.GetPlatformSettings(atlas, "Android");
                                if (tips.format == TextureImporterFormat.ETC_RGB4)
                                {
                                    tips.format = TextureImporterFormat.ETC2_RGBA8Crunched;
                                    SpriteAtlasExtensions.SetPlatformSettings(atlas, tips);
                                    SpriteAtlasUtility.PackAtlases(new UnityEngine.U2D.SpriteAtlas[] { atlas }, BuildTarget.Android, false);
                                }
                            }

                            EditorUtility.ClearProgressBar();
                            EditorUtility.DisplayDialog("成功", "处理完成！", "好的");
                        }
                    }
                    catch { }
                }
                catch { }
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
                            int size = joc.list.Count;
                            for (int i = 0; i < size; ++i)
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
                var subs = GetPackedPathsInAtlas(atlasPath);
                for (int i = 0; i < subs.Length; ++i)
                {
                    string spPath = subs[i];
                    string guid = AssetDatabase.AssetPathToGUID(spPath);
                    _CachedAtlasSpriteGUID[guid] = atlasName;
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
            var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlaspath);
            if (!atlas)
            {
                return;
            }
            if (atlas.isVariant)
            {
                var master = new SerializedObject(atlas).FindProperty("m_MasterAtlas").objectReferenceValue as SpriteAtlas;
                var tag = master.tag;
                var currentname = Path.GetFileNameWithoutExtension(atlaspath);
                if (currentname == tag)
                {
                    return;
                }
                AssetDatabase.RenameAsset(atlaspath, tag + ".spriteatlas");
            }
            else
            {
                string type;
                string mod;
                string dist;
                string folder = Path.GetDirectoryName(atlaspath);
                string ret = ResManager.GetAssetNormPath(folder, out type, out mod, out dist);

                StringBuilder sb = new StringBuilder();
                //sb.Append("m").Append("-").Append(mod.ToLower()).Append("-").Append("d").Append("-").Append(dist.ToLower()).Append("-");
                sb.Append(ret.Replace('/', '-')).Append("-");
                string newNamePre = sb.ToString().ToLower();
                if (Path.GetFileNameWithoutExtension(atlaspath).StartsWith(newNamePre))
                {
                    return;
                }

                int subIndex = 0;
                while (true)
                {
                    ++subIndex;
                    bool isExists = _CachedAtlas.ContainsKey(newNamePre + subIndex);
                    if (!isExists)
                    {
                        break;
                    }
                }
                string newName = newNamePre + subIndex + ".spriteatlas";
                AssetDatabase.RenameAsset(atlaspath, newName);
            }
        }

        private static void DoAssetReimport(string path, ImportAssetOptions options)
        {
            try
            {
                AssetDatabase.StartAssetEditing();
                AssetDatabase.ImportAsset(path, options);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
        }
    }
}