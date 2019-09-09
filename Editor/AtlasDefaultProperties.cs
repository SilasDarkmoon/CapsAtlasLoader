using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Capstones.UnityEngineEx;

namespace Capstones.UnityEditorEx
{
    [CreateAssetMenu(fileName = "AtlasProperties.asset")]
    public class AtlasDefaultProperties : ScriptableObject
    {
        public TextureImporterFormat iOSFormat;
        public TextureImporterFormat AndroidFormat;
    }
}
