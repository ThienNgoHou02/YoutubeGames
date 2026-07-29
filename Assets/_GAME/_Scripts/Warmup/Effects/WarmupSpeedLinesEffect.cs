using Sirenix.OdinInspector;
using UnityEngine;

namespace GameYT.Warmup
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ParticleSystem))]
    public sealed class WarmupSpeedLinesEffect : MonoBehaviour
    {
        private const float PlayThreshold = 0.02f;

        [Title("References")]
        [Required]
        [SerializeField] private WarmupPlayerController player;

        [Required]
        [SerializeField] private Camera targetCamera;

        [Required, AssetsOnly]
        [SerializeField] private Material particleMaterial;

        [Title("Wind Lines")]
        [SerializeField] private Color lineColor =
            new Color(1f, 1f, 1f, 0.78f);

        [MinValue(0f)]
        [SerializeField] private float emissionRate = 58f;

        [MinValue(8)]
        [SerializeField] private int maxParticles = 120;

        [MinValue(1f)]
        [SerializeField] private float spawnDistance = 18f;

        [MinMaxSlider(1f, 50f, true)]
        [SerializeField] private Vector2 speedRange = new Vector2(28f, 42f);

        [MinMaxSlider(0.05f, 2f, true)]
        [SerializeField] private Vector2 lifetimeRange =
            new Vector2(0.48f, 0.78f);

        [MinMaxSlider(0.005f, 0.2f, true)]
        [SerializeField] private Vector2 widthRange =
            new Vector2(0.035f, 0.09f);

        [Range(1f, 45f)]
        [SerializeField] private float coneAngle = 26f;

        [MinValue(0f)]
        [SerializeField] private float coneRadius = 1.2f;

        [Range(0f, 0.3f)]
        [SerializeField] private float velocityScale = 0.1f;

        [MinValue(0.1f)]
        [SerializeField] private float intensityBlendSpeed = 7f;

        [Title("Runtime Debug")]
        [ShowInInspector, ReadOnly, ProgressBar(0f, 1.5f)]
        private float _currentIntensity;

        private ParticleSystem _particles;

        private void Awake()
        {
            CacheReferences();
            ApplyConfiguration();
        }

        private void OnEnable()
        {
            if (_particles == null)
            {
                return;
            }

            _particles.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void Update()
        {
            if (_particles == null)
            {
                return;
            }

            float targetIntensity = GetTargetIntensity();
            float blend =
                1f - Mathf.Exp(-intensityBlendSpeed * Time.deltaTime);
            _currentIntensity = Mathf.Lerp(
                _currentIntensity,
                targetIntensity,
                blend);

            UpdateParticleIntensity(_currentIntensity);
        }

        private void OnDisable()
        {
            if (_particles == null)
            {
                return;
            }

            _particles.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear);
            _currentIntensity = 0f;
        }

        private void CacheReferences()
        {
            _particles = GetComponent<ParticleSystem>();

            if (targetCamera == null && player != null)
            {
                targetCamera = player.GetComponentInChildren<Camera>(true);
            }
        }

        private void ApplyConfiguration()
        {
            if (_particles == null)
            {
                return;
            }

            var main = _particles.main;
            main.duration = 1f;
            main.loop = true;
            main.prewarm = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            main.maxParticles = Mathf.Max(8, maxParticles);
            main.startSpeed = CreateOrderedCurve(speedRange);
            main.startLifetime = CreateOrderedCurve(lifetimeRange);
            main.startSize = CreateOrderedCurve(widthRange);
            main.startColor = lineColor;

            var emission = _particles.emission;
            emission.enabled = true;
            emission.rateOverTime = Mathf.Max(0f, emissionRate);

            var shape = _particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.position = Vector3.forward * Mathf.Max(1f, spawnDistance);
            shape.rotation = new Vector3(0f, 180f, 0f);
            shape.angle = Mathf.Clamp(coneAngle, 1f, 45f);
            shape.radius = Mathf.Max(0f, coneRadius);
            shape.radiusThickness = 1f;

            var velocity = _particles.velocityOverLifetime;
            velocity.enabled = false;

            var colorOverLifetime = _particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color =
                new ParticleSystem.MinMaxGradient(CreateFadeGradient());

            var particleRenderer =
                _particles.GetComponent<ParticleSystemRenderer>();
            particleRenderer.renderMode = ParticleSystemRenderMode.Stretch;
            particleRenderer.alignment = ParticleSystemRenderSpace.View;
            particleRenderer.cameraVelocityScale = 0f;
            particleRenderer.lengthScale = 1f;
            particleRenderer.velocityScale = velocityScale;

            if (particleMaterial != null)
            {
                particleRenderer.sharedMaterial = particleMaterial;
            }
        }

        private float GetTargetIntensity()
        {
            if (player == null)
            {
                return 1f;
            }

            float configuredSpeed = player.ConfiguredRunSpeed;
            if (configuredSpeed <= 0.01f)
            {
                return 0f;
            }

            return Mathf.Clamp(
                player.CurrentRunSpeed / configuredSpeed,
                0f,
                1.5f);
        }

        private void UpdateParticleIntensity(float intensity)
        {
            var emission = _particles.emission;
            emission.rateOverTime =
                Mathf.Max(0f, emissionRate * intensity);

            if (intensity > PlayThreshold)
            {
                if (!_particles.isPlaying)
                {
                    _particles.Play();
                }

                return;
            }

            if (_particles.isPlaying)
            {
                _particles.Stop(
                    true,
                    ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                return;
            }

            CacheReferences();
            ApplyConfiguration();
        }

        private static ParticleSystem.MinMaxCurve CreateOrderedCurve(
            Vector2 range)
        {
            Vector2 orderedRange = OrderRange(range);
            return new ParticleSystem.MinMaxCurve(
                orderedRange.x,
                orderedRange.y);
        }

        private static Vector2 OrderRange(Vector2 range)
        {
            float minimum = Mathf.Max(
                0.001f,
                Mathf.Min(range.x, range.y));
            float maximum = Mathf.Max(
                minimum,
                Mathf.Max(range.x, range.y));
            return new Vector2(minimum, maximum);
        }

        private static Gradient CreateFadeGradient()
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.12f),
                    new GradientAlphaKey(1f, 0.75f),
                    new GradientAlphaKey(0f, 1f)
                });
            return gradient;
        }

#if UNITY_EDITOR
        public void SetupEditorReferences(
            WarmupPlayerController playerReference,
            Camera cameraReference,
            Material materialReference)
        {
            player = playerReference;
            targetCamera = cameraReference;
            particleMaterial = materialReference;
            CacheReferences();
            ApplyConfiguration();
        }

        [Button("Validate References")]
        private void ValidateReferences()
        {
            CacheReferences();

            if (player == null ||
                targetCamera == null ||
                particleMaterial == null)
            {
                Debug.LogError(
                    "Speed Lines thiếu Player, Camera hoặc Particle Material.",
                    this);
                return;
            }

            Debug.Log("Speed Lines references hợp lệ.", this);
        }
#endif
    }
}
