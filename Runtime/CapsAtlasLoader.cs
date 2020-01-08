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
        // TODO: 这个延迟加载是因为，如果启动场景就用到了atlas，那么在Reg的时候会直接崩溃。
        // 现在改成了延迟一帧生效。这个BUG在2019.2.3f1中存在。需要确定后续版本如果没问题后，使用版本宏将这部分代码关闭。
        private static Queue<string> _LateLoadQueue = new Queue<string>();
        private static Action<UnityEngine.U2D.SpriteAtlas> _RegFunc;
        private static bool _Started;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnUnityStart()
        {
            if (ResManager.ResLoader is ResManager.ClientResLoader)
            {
                UnityEngine.U2D.SpriteAtlasManager.atlasRequested += (name, funcReg) =>
                {
                    if (!_Started)
                    {
                        _LateLoadQueue.Enqueue(name);
                        _RegFunc = funcReg;
                        return;
                    }

                    var atlas = ResManager.LoadRes("atlas/" + name, typeof(UnityEngine.U2D.SpriteAtlas)) as UnityEngine.U2D.SpriteAtlas;
                    if (atlas)
                    {
                        funcReg(atlas);
                    }
                };
                CoroutineRunner.StartCoroutine(LateLoadWork());
            }
        }

        private static IEnumerator LateLoadWork()
        {
            try
            {
                yield return null;
            }
            finally
            { // using finally is that the coroutine runner may be destroyed when some component starts.
                _Started = true;
                foreach (var name in _LateLoadQueue)
                {
                    var atlas = ResManager.LoadRes("atlas/" + name, typeof(UnityEngine.U2D.SpriteAtlas)) as UnityEngine.U2D.SpriteAtlas;
                    if (atlas)
                    {
                        _RegFunc(atlas);
                    }
                }
                _LateLoadQueue.Clear();
            }
        }
    }
}