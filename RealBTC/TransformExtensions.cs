using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RealBTC.Utils
{
    public static class TransformExtensions
    {
        public static IEnumerable<Transform> FindChildrenByNames(this Transform parent, params string[] names)
        {
            if (names == null || names.Length == 0)
                return Enumerable.Empty<Transform>();

            return parent
                .GetComponentsInChildren<Transform>(true)
                .Where(t => names.Any(n =>
                    string.Equals(t.name, n, System.StringComparison.OrdinalIgnoreCase)));
        }
        
        public static IEnumerable<Transform> FindAllChildrenByName(this Transform parent, string name)
        {
            return parent
                .GetComponentsInChildren<Transform>(true)
                .Where(t => string.Equals(t.name, name, System.StringComparison.OrdinalIgnoreCase));
        }
        
        public static void DisableOtherComponents<T>(this GameObject go) where T : Component
        {
            var comps = go.GetComponents<Component>();
            foreach (var c in comps)
            {
                // 跳过 Transform（永远不能禁用） 和要保留的组件
                if (c is Transform || c is T)
                    continue;

                // 组件是否有 enabled 属性
                var type = c.GetType();
                var prop = type.GetProperty("enabled");
                if (prop != null && prop.PropertyType == typeof(bool))
                {
                    prop.SetValue(c, false);
                }
            }
        }
        
        public static T KeepThisMonoDisableOthers<T>(this Transform transform) where T : MonoBehaviour
        {
            if (transform == null) return null;

            T keep = null;

            // 只获取当前 Transform 上的 MonoBehaviour
            var all = transform.GetComponents<MonoBehaviour>();

            foreach (var comp in all)
            {
                if (comp is T tComp)
                {
                    if (keep == null)
                        keep = tComp; // 第一个 T 类型组件保留
                    else
                        comp.enabled = false; // 可选：禁用其他同类型组件
                }
                else
                {
                    comp.enabled = false; // 关闭其他 MonoBehaviour
                }
            }

            return keep;
        }
        public static T KeepThisMonoDestroyOthers<T>(this Transform transform) where T : MonoBehaviour
        {
            if (transform == null) return null;

            T keep = null;

            // 只获取当前 Transform 上的 MonoBehaviour
            var all = transform.GetComponents<MonoBehaviour>();

            foreach (var comp in all)
            {
                if (comp is T tComp)
                {
                    if (keep == null)
                        keep = tComp; // 第一个 T 类型组件保留
                    else
                        comp.enabled = false; // 可选：禁用其他同类型组件
                }
                else
                {
                    Object.Destroy(comp);
                    //comp.enabled = false; // 关闭其他 MonoBehaviour
                }
            }

            return keep;
        }
    }
}