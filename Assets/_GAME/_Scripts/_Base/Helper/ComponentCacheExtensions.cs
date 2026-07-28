using System;
using System.Collections.Generic;
using UnityEngine;

namespace DuongMike
{
    /// <summary>
    /// - Flat cache for fast lookup
    /// - Auto cleanup on GameObject destroy
    /// - No polling cleanup
    /// - No caching of misses
    /// - Safe for scene reload
    /// </summary>
    public static class ComponentCacheExtensions
    {
        private readonly struct CacheKey : IEquatable<CacheKey>
        {
            public readonly int GameObjectId;
            public readonly RuntimeTypeHandle TypeHandle;

            public CacheKey(int gameObjectId, RuntimeTypeHandle typeHandle)
            {
                GameObjectId = gameObjectId;
                TypeHandle = typeHandle;
            }

            public bool Equals(CacheKey other)
            {
                return GameObjectId == other.GameObjectId && TypeHandle.Equals(other.TypeHandle);
            }

            public override bool Equals(object obj)
            {
                return obj is CacheKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = 17;
                    hash = hash * 31 + GameObjectId;
                    hash = hash * 31 + TypeHandle.GetHashCode();
                    return hash;
                }
            }
        }

        private static readonly Dictionary<CacheKey, Component> _cache = new(1024);
        private static readonly Dictionary<int, HashSet<CacheKey>> _keysByObject = new(512);

        public static T GetCachedComponent<T>(this Component source) where T : Component
        {
            if (!source) return null;
            return source.gameObject.GetCachedComponent<T>();
        }

        public static T GetCachedComponent<T>(this GameObject go) where T : Component
        {
            if (!go) return null;

            int goId = go.GetInstanceID();
            var key = new CacheKey(goId, typeof(T).TypeHandle);

            if (_cache.TryGetValue(key, out var cached) && cached)
                return (T)cached;

            if (cached != null)
                RemoveKey(goId, key);

            var component = go.GetComponent<T>();
            if (component)
            {
                Store(go, goId, key, component);
            }

            return component;
        }

        public static bool TryGetCachedComponent<T>(this Component source, out T component) where T : Component
        {
            component = source ? source.GetCachedComponent<T>() : null;
            return component != null;
        }

        public static T GetOrAdd<T>(this GameObject go) where T : Component
        {
            if (!go) return null;

            if (go.TryGetComponent<T>(out var existing))
                return existing;

            return go.AddComponent<T>();
        }

        /// <summary>
        /// Xoa toan bo cache lien quan den GameObject, bao gom ca cache component va cache key.
        /// Dung khi GameObject bi destroy, cache lien quan se tu dong xoa nhung neu muon xoa truoc, co the goi ham nay.
        /// </summary>
        public static void Invalidate(this GameObject go)
        {
            if (!go) return;
            Invalidate(go.GetInstanceID());
        }

        /// <summary>
        /// Clear toan bo cache
        /// </summary>
        public static void ClearAllCache()
        {
            _cache.Clear();
            _keysByObject.Clear();
        }

        private static void Store(GameObject go, int goId, CacheKey key, Component component)
        {
            _cache[key] = component;

            if (!_keysByObject.TryGetValue(goId, out var keys))
            {
                keys = new HashSet<CacheKey>();
                _keysByObject.Add(goId, keys);
                EnsureHook(go);
            }

            keys.Add(key);
        }

        private static void RemoveKey(int goId, CacheKey key)
        {
            _cache.Remove(key);

            if (_keysByObject.TryGetValue(goId, out var keys))
            {
                keys.Remove(key);
                if (keys.Count == 0)
                    _keysByObject.Remove(goId);
            }
        }

        internal static void Invalidate(int goId)
        {
            if (!_keysByObject.TryGetValue(goId, out var keys))
                return;

            foreach (var key in keys)
            {
                _cache.Remove(key);
            }

            _keysByObject.Remove(goId);
        }

        private static void EnsureHook(GameObject go)
        {
            if (!go.TryGetComponent<CacheLifetimeHook>(out var hook))
            {
                hook = go.AddComponent<CacheLifetimeHook>();
                hook.hideFlags = HideFlags.HideInInspector;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnSubsystemRegistration()
        {
            ClearAllCache();
        }

        private sealed class CacheLifetimeHook : MonoBehaviour
        {
            private void OnDestroy()
            {
                ComponentCacheExtensions.Invalidate(gameObject.GetInstanceID());
            }
        }
    }
}