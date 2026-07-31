using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GameYT.Warmup
{
    [CreateAssetMenu(
        fileName = "VideoObstaclePrefabSet",
        menuName = "Game YT/Obstacle Timeline/Video Prefab Set")]
    public sealed class WarmupObstaclePrefabSet : ScriptableObject
    {
        [Title("Video")]
        [SerializeField]
        [ValidateInput(nameof(HasVideoId), "Video ID không được để trống.")]
        private string videoId = "Video0";

        [Title("Prefab theo gameplay")]
        [SerializeField] private GameObject[] jumpPrefabs = Array.Empty<GameObject>();
        [SerializeField] private GameObject[] poseWallPrefabs = Array.Empty<GameObject>();
        [SerializeField] private GameObject[] duckBarrierPrefabs = Array.Empty<GameObject>();
        [SerializeField] private GameObject[] laneBlockerPrefabs = Array.Empty<GameObject>();
        [SerializeField] private GameObject[] bossWallPrefabs = Array.Empty<GameObject>();

        public string VideoId => videoId;

        public bool HasPrefab(WarmupObstacleType type)
        {
            GameObject[] prefabs = GetArray(type);
            if (prefabs == null)
            {
                return false;
            }

            for (int i = 0; i < prefabs.Length; i++)
            {
                if (prefabs[i] != null)
                {
                    return true;
                }
            }

            return false;
        }

        public GameObject GetRandomPrefab(WarmupObstacleType type)
        {
            GameObject[] prefabs = GetArray(type);
            if (prefabs == null || prefabs.Length == 0)
            {
                return null;
            }

            int validPrefabCount = 0;
            for (int i = 0; i < prefabs.Length; i++)
            {
                if (prefabs[i] != null)
                {
                    validPrefabCount++;
                }
            }

            if (validPrefabCount == 0)
            {
                return null;
            }

            int randomIndex = UnityEngine.Random.Range(0, validPrefabCount);
            for (int i = 0; i < prefabs.Length; i++)
            {
                GameObject prefab = prefabs[i];
                if (prefab == null)
                {
                    continue;
                }

                if (randomIndex == 0)
                {
                    return prefab;
                }

                randomIndex--;
            }

            return null;
        }

        private GameObject[] GetArray(WarmupObstacleType type)
        {
            switch (type)
            {
                case WarmupObstacleType.Jump:
                    return jumpPrefabs;
                case WarmupObstacleType.PoseWall:
                    return poseWallPrefabs;
                case WarmupObstacleType.DuckBarrier:
                    return duckBarrierPrefabs;
                case WarmupObstacleType.LaneBlocker:
                    return laneBlockerPrefabs;
                case WarmupObstacleType.BossWall:
                    return bossWallPrefabs;
                default:
                    return null;
            }
        }

        private bool HasVideoId(string value)
        {
            return !string.IsNullOrWhiteSpace(value);
        }

#if UNITY_EDITOR
        public void SetData(
            string id,
            GameObject[] jumps,
            GameObject[] poseWalls,
            GameObject[] duckBarriers,
            GameObject[] laneBlockers,
            GameObject[] bossWalls)
        {
            videoId = string.IsNullOrWhiteSpace(id) ? "Video" : id.Trim();
            jumpPrefabs = jumps ?? Array.Empty<GameObject>();
            poseWallPrefabs = poseWalls ?? Array.Empty<GameObject>();
            duckBarrierPrefabs = duckBarriers ?? Array.Empty<GameObject>();
            laneBlockerPrefabs = laneBlockers ?? Array.Empty<GameObject>();
            bossWallPrefabs = bossWalls ?? Array.Empty<GameObject>();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
