using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GameYT.Warmup
{
    [DisallowMultipleComponent]
    public sealed class WarmupPaperShardBurst : MonoBehaviour
    {
        private const int Columns = 4;
        private const int Rows = 2;
        private const int ShardCount = Columns * Rows * 2;

        [Title("References")]
        [Tooltip("Để trống sẽ dùng material của renderer tường.")]
        [SerializeField] private Material shardMaterial;

        [Title("Break Feel")]
        [MinValue(0.1f)]
        [SerializeField] private float burstDistance = 1.25f;

        [MinValue(0f)]
        [SerializeField] private float forwardForce = 1.8f;

        [MinValue(0.1f)]
        [SerializeField] private float duration = 1.15f;

        [MinValue(0f)]
        [SerializeField] private float gravityDrop = 1.4f;

        [SerializeField] private int randomSeed = 7319;

        private Transform _shardRoot;
        private Transform[] _shards;
        private Vector3[] _initialLocalPositions;
        private Mesh[] _runtimeMeshes;
        private Sequence _breakSequence;

        public void ConfigureMaterial(Material material)
        {
            shardMaterial = material;
        }

        public void Play(Renderer sourceRenderer)
        {
            if (sourceRenderer == null)
            {
                return;
            }

            EnsureShards(sourceRenderer);
            sourceRenderer.enabled = false;

            _breakSequence?.Kill();
            _breakSequence = DOTween.Sequence()
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);

            var random = new System.Random(randomSeed);
            for (int i = 0; i < _shards.Length; i++)
            {
                Transform shard = _shards[i];
                shard.gameObject.SetActive(true);

                float horizontal =
                    Mathf.Lerp(-burstDistance, burstDistance, (float)random.NextDouble());
                float vertical =
                    Mathf.Lerp(0.2f, burstDistance, (float)random.NextDouble());
                float forward =
                    Mathf.Lerp(forwardForce * 0.6f, forwardForce, (float)random.NextDouble());
                Vector3 target =
                    shard.localPosition +
                    new Vector3(horizontal, vertical - gravityDrop, -forward);
                Vector3 rotation = new Vector3(
                    random.Next(-100, 101),
                    random.Next(-160, 161),
                    random.Next(-120, 121));

                _breakSequence.Join(
                    shard
                        .DOLocalMove(target, duration)
                        .SetEase(Ease.OutCubic));
                _breakSequence.Join(
                    shard
                        .DOLocalRotate(rotation, duration, RotateMode.FastBeyond360)
                        .SetEase(Ease.OutQuad));
            }

            _breakSequence.OnComplete(HideShards);
        }

        public void ResetBurst(Renderer sourceRenderer)
        {
            _breakSequence?.Kill();
            _breakSequence = null;
            HideShards();

            if (sourceRenderer != null)
            {
                sourceRenderer.enabled = true;
            }
        }

        private void OnDisable()
        {
            _breakSequence?.Kill();
            _breakSequence = null;
        }

        private void OnDestroy()
        {
            if (_runtimeMeshes == null)
            {
                return;
            }

            for (int i = 0; i < _runtimeMeshes.Length; i++)
            {
                if (_runtimeMeshes[i] != null)
                {
                    Destroy(_runtimeMeshes[i]);
                }
            }
        }

        private void EnsureShards(Renderer sourceRenderer)
        {
            if (_shards != null)
            {
                ResetShardTransforms(sourceRenderer);
                return;
            }

            Bounds localBounds = GetLocalBounds(sourceRenderer);
            _shardRoot = new GameObject("Paper Shards").transform;
            _shardRoot.SetParent(sourceRenderer.transform, false);

            _shards = new Transform[ShardCount];
            _initialLocalPositions = new Vector3[ShardCount];
            _runtimeMeshes = new Mesh[ShardCount];

            Vector3 size = localBounds.size;
            float cellWidth = size.x / Columns;
            float cellHeight = size.y / Rows;
            float frontZ = localBounds.center.z - size.z * 0.51f;
            int shardIndex = 0;

            for (int row = 0; row < Rows; row++)
            {
                for (int column = 0; column < Columns; column++)
                {
                    float xMin = localBounds.min.x + column * cellWidth;
                    float xMax = xMin + cellWidth;
                    float yMin = localBounds.min.y + row * cellHeight;
                    float yMax = yMin + cellHeight;

                    CreateTriangle(
                        sourceRenderer,
                        shardIndex++,
                        new Vector3(xMin, yMin, frontZ),
                        new Vector3(xMax, yMin, frontZ),
                        new Vector3(xMax, yMax, frontZ));
                    CreateTriangle(
                        sourceRenderer,
                        shardIndex++,
                        new Vector3(xMin, yMin, frontZ),
                        new Vector3(xMax, yMax, frontZ),
                        new Vector3(xMin, yMax, frontZ));
                }
            }
        }

        private void CreateTriangle(
            Renderer sourceRenderer,
            int index,
            Vector3 pointA,
            Vector3 pointB,
            Vector3 pointC)
        {
            Vector3 center = (pointA + pointB + pointC) / 3f;
            var shardObject = new GameObject("Shard " + index);
            Transform shard = shardObject.transform;
            shard.SetParent(_shardRoot, false);
            shard.localPosition = center;

            var mesh = new Mesh
            {
                name = "Warmup Paper Shard " + index
            };
            mesh.vertices = new[]
            {
                pointA - center,
                pointB - center,
                pointC - center
            };
            mesh.uv = new[]
            {
                CalculateUv(pointA, sourceRenderer),
                CalculateUv(pointB, sourceRenderer),
                CalculateUv(pointC, sourceRenderer)
            };
            mesh.triangles = new[] { 0, 1, 2 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            shardObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer shardRenderer = shardObject.AddComponent<MeshRenderer>();
            shardRenderer.sharedMaterial =
                shardMaterial != null
                    ? shardMaterial
                    : sourceRenderer.sharedMaterial;
            shardRenderer.shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;
            shardRenderer.receiveShadows = false;
            CopySourceAppearance(sourceRenderer, shardRenderer);

            _shards[index] = shard;
            _initialLocalPositions[index] = center;
            _runtimeMeshes[index] = mesh;
            shardObject.SetActive(false);
        }

        private static void CopySourceAppearance(
            Renderer sourceRenderer,
            Renderer shardRenderer)
        {
            Material sourceMaterial = sourceRenderer.sharedMaterial;
            Material targetMaterial = shardRenderer.sharedMaterial;
            if (sourceMaterial == null || targetMaterial == null)
            {
                return;
            }

            var propertyBlock = new MaterialPropertyBlock();
            if (targetMaterial.HasProperty("_MainTex"))
            {
                Texture texture = null;
                if (sourceMaterial.HasProperty("_MainTex"))
                {
                    texture = sourceMaterial.GetTexture("_MainTex");
                }
                else if (sourceMaterial.HasProperty("_BaseMap"))
                {
                    texture = sourceMaterial.GetTexture("_BaseMap");
                }

                if (texture != null)
                {
                    propertyBlock.SetTexture("_MainTex", texture);
                }
            }

            if (targetMaterial.HasProperty("_Color"))
            {
                Color color = Color.white;
                if (sourceMaterial.HasProperty("_Color"))
                {
                    color = sourceMaterial.GetColor("_Color");
                }
                else if (sourceMaterial.HasProperty("_BaseColor"))
                {
                    color = sourceMaterial.GetColor("_BaseColor");
                }

                propertyBlock.SetColor("_Color", color);
            }

            shardRenderer.SetPropertyBlock(propertyBlock);
        }

        private static Vector2 CalculateUv(Vector3 point, Renderer renderer)
        {
            Bounds bounds = GetLocalBounds(renderer);
            float u = Mathf.InverseLerp(bounds.min.x, bounds.max.x, point.x);
            float v = Mathf.InverseLerp(bounds.min.y, bounds.max.y, point.y);
            return new Vector2(u, v);
        }

        private static Bounds GetLocalBounds(Renderer renderer)
        {
            MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                return meshFilter.sharedMesh.bounds;
            }

            Vector3 localCenter =
                renderer.transform.InverseTransformPoint(renderer.bounds.center);
            Vector3 localSize =
                renderer.transform.InverseTransformVector(renderer.bounds.size);
            localSize.x = Mathf.Abs(localSize.x);
            localSize.y = Mathf.Abs(localSize.y);
            localSize.z = Mathf.Abs(localSize.z);
            return new Bounds(localCenter, localSize);
        }

        private void ResetShardTransforms(Renderer sourceRenderer)
        {
            if (_shardRoot == null)
            {
                return;
            }

            _shardRoot.SetParent(sourceRenderer.transform, false);
            for (int i = 0; i < _shards.Length; i++)
            {
                _shards[i].localPosition = _initialLocalPositions[i];
                _shards[i].localRotation = Quaternion.identity;
            }
        }

        private void HideShards()
        {
            if (_shards == null)
            {
                return;
            }

            for (int i = 0; i < _shards.Length; i++)
            {
                _shards[i].gameObject.SetActive(false);
            }
        }
    }
}
