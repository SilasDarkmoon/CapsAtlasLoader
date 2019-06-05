using UnityEngine;
using UnityEngine.EventSystems;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Object = UnityEngine.Object;

namespace Capstones.UnityEngineEx
{
    public static class CapsAtlasLoader
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnUnityStart()
        {
            if (ResManager.ResLoader is ResManager.ClientResLoader)
            {
                UnityEngine.U2D.SpriteAtlasManager.atlasRequested += (name, funcReg) =>
                {
                    var atlas = ResManager.LoadRes("atlas/" + name, typeof(UnityEngine.U2D.SpriteAtlas)) as UnityEngine.U2D.SpriteAtlas;
                    if (atlas)
                    {
                        funcReg(atlas);
                    }
                };
            }
        }
    }
}