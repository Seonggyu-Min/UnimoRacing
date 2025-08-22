using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace YSJ.Util
{
    public static class UnityUtilEx
    {
        public static T GetOrAddComponent<T>(this GameObject go) where T : Component
        {
            var comp = go.GetComponent<T>();
            if (comp == null)
                comp = go.AddComponent<T>();
            return comp;
        }

        public static GameObject FindOrCreateGameObject(string name) 
        {
            GameObject go = GameObject.Find(name);
            if (go == null)
                go = new GameObject(name);
            return go;
        }

        public static T FindNearestTarget<T>(Vector3 origin, List<GameObject> targets) where T : Component
        {
            float minDist = float.MaxValue;
            T nearest = null;

            foreach (var obj in targets)
            {
                if (obj == null) continue;

                var target = obj.GetComponent<T>();
                if (target == null) continue;

                float dist = Vector2.Distance(origin, target.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = target;
                }
            }

            return nearest;
        }

        public static void PrintLog<T>(this T type, string log, LogType logType = LogType.Log, bool isPrint = true)
        {
            if (!isPrint) return;

#if UNITY_EDITOR
            switch (logType)
            {
                case LogType.Error:
                    Debug.LogError($"[{type}]: {log}");
                    break;
                case LogType.Warning:
                    Debug.LogWarning($"[{type}]: {log}");
                    break;
                case LogType.Log:
                    Debug.Log($"[{type}]: {log}");
                    break;
            }
#endif
        }
    }
}
