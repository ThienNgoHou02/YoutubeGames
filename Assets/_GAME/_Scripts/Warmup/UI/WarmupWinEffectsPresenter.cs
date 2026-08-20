using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GameYT.Warmup
{
    [DisallowMultipleComponent]
    public sealed class WarmupWinEffectsPresenter : MonoBehaviour
    {
        [Title("Scene References")]
        [InfoBox(
            "Director, Player và Arena VFX được gán tại scene WarnUp.",
            InfoMessageType.Info)]
        [Required]
        [SerializeField] private WarmupObstacleTimelineDirector director;

        [Required]
        [SerializeField] private WarmupPlayerController player;

        [Required]
        [SerializeField] private GameObject arenaWinVfxRoot;

        [Title("HUD References")]
        [Required]
        [SerializeField] private GameObject hudWinVfxRoot;

        [Title("Configuration")]
        [MinValue(0f)]
        [SuffixLabel("sec", Overlay = true)]
        [SerializeField] private float hudWinDelay = 0.5f;

        [Tooltip(
            "Offset theo local space của Player để Arena VFX nằm đúng mặt đất.")]
        [SerializeField] private Vector3 arenaPositionOffset =
            new Vector3(0f, -0.56f, 0f);

        private ParticleSystem[] _arenaParticles;
        private ParticleSystem[] _hudParticles;
        private Tween _hudDelayTween;

        private void Awake()
        {
            CacheParticleSystems();
            ResetEffects();
        }

        private void OnEnable()
        {
            if (director == null)
            {
                return;
            }

            director.PhaseStarted += HandlePhaseStarted;
            director.PhaseFinished += HandlePhaseFinished;
        }

        private void OnDisable()
        {
            if (director != null)
            {
                director.PhaseStarted -= HandlePhaseStarted;
                director.PhaseFinished -= HandlePhaseFinished;
            }

            ResetEffects();
        }

        private void HandlePhaseStarted(WarmupPhaseTimelineAsset phase)
        {
            ResetEffects();
            PlayArenaWinEffect(phase);
        }

        private void HandlePhaseFinished()
        {
            _hudDelayTween?.Kill();
            _hudDelayTween = DOVirtual
                .DelayedCall(hudWinDelay, PlayHudWinEffect)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        }

        private void PlayArenaWinEffect(WarmupPhaseTimelineAsset phase)
        {
            if (arenaWinVfxRoot == null ||
                director == null ||
                player == null ||
                phase == null)
            {
                return;
            }

            float finishDistance = phase.Duration * director.ActiveRunSpeed;
            Vector3 localFinishPosition = arenaPositionOffset +
                                          Vector3.forward * finishDistance;
            arenaWinVfxRoot.transform.SetPositionAndRotation(
                player.transform.TransformPoint(localFinishPosition),
                player.transform.rotation);

            SetLoop(_arenaParticles, true);
            PlayParticles(arenaWinVfxRoot, _arenaParticles);
        }

        private void PlayHudWinEffect()
        {
            PlayParticles(hudWinVfxRoot, _hudParticles);
            _hudDelayTween = null;
        }

        private void CacheParticleSystems()
        {
            _arenaParticles = GetParticles(arenaWinVfxRoot);
            _hudParticles = GetParticles(hudWinVfxRoot);
        }

        private void ResetEffects()
        {
            _hudDelayTween?.Kill();
            _hudDelayTween = null;

            StopAndHide(arenaWinVfxRoot, _arenaParticles);
            StopAndHide(hudWinVfxRoot, _hudParticles);
        }

        private static ParticleSystem[] GetParticles(GameObject root)
        {
            return root != null
                ? root.GetComponentsInChildren<ParticleSystem>(true)
                : null;
        }

        private static void SetLoop(
            ParticleSystem[] particles,
            bool shouldLoop)
        {
            if (particles == null)
            {
                return;
            }

            for (int i = 0; i < particles.Length; i++)
            {
                ParticleSystem.MainModule main = particles[i].main;
                main.loop = shouldLoop;
            }
        }

        private static void PlayParticles(
            GameObject root,
            ParticleSystem[] particles)
        {
            if (root == null || particles == null)
            {
                return;
            }

            root.SetActive(true);
            for (int i = 0; i < particles.Length; i++)
            {
                particles[i].Stop(
                    false,
                    ParticleSystemStopBehavior.StopEmittingAndClear);
                particles[i].Play(false);
            }
        }

        private static void StopAndHide(
            GameObject root,
            ParticleSystem[] particles)
        {
            if (particles != null)
            {
                for (int i = 0; i < particles.Length; i++)
                {
                    particles[i].Stop(
                        false,
                        ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }

            root?.SetActive(false);
        }

#if UNITY_EDITOR
        public void SetupComponents(
            WarmupObstacleTimelineDirector timelineDirector,
            WarmupPlayerController playerController,
            GameObject arenaVfx,
            GameObject hudVfx)
        {
            director = timelineDirector;
            player = playerController;
            arenaWinVfxRoot = arenaVfx;
            hudWinVfxRoot = hudVfx;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
