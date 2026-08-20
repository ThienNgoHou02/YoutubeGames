using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace GameYT.Warmup
{
    [DisallowMultipleComponent]
    public sealed class WarmupActionCuePresenter : MonoBehaviour
    {
        [Title("References")]
        [InfoBox(
            "Nếu Director để trống, presenter tự tìm một lần khi OnEnable.",
            InfoMessageType.Info)]
        [SerializeField] private WarmupObstacleTimelineDirector director;

        [Required, AssetsOnly]
        [SerializeField] private WarmupActionIconSet iconSet;

        [Required]
        [Tooltip("Image Viewer Action Cue được tạo sẵn trong Warmup HUD prefab.")]
        [SerializeField] private Image cueImage;

        [Title("Animation")]
        [MinValue(0.01f)]
        [SerializeField] private float showDuration = 0.16f;

        [MinValue(0.01f)]
        [SerializeField] private float scaleDuration = 0.2f;

        [MinValue(0.01f)]
        [SerializeField] private float hideDuration = 0.14f;

        [MinValue(0.1f)]
        [SerializeField] private float minimumVisibleDuration = 0.4f;

        [Range(0.5f, 1f)]
        [SerializeField] private float startScale = 0.82f;

        private CanvasGroup _cueGroup;
        private RectTransform _cueTransform;
        private Sequence _cueSequence;
        private bool _isBossCue;
        private bool _waitForMovementAfterBoss;
        private float _bossFinishedDistance;

        private void Awake()
        {
            EnsureCueVisual();
            HideImmediately();
        }

        private void OnEnable()
        {
            if (director == null)
            {
                director = FindObjectOfType<WarmupObstacleTimelineDirector>();
            }

            if (director == null)
            {
                return;
            }

            director.PhaseStarted += HandlePhaseStarted;
            director.CueStarted += ShowCue;
            director.BossFightFinished += HandleBossFightFinished;
            director.DistanceChanged += HandleDistanceChanged;
            director.PhaseFinished += HideImmediately;
        }

        private void OnDisable()
        {
            if (director != null)
            {
                director.PhaseStarted -= HandlePhaseStarted;
                director.CueStarted -= ShowCue;
                director.BossFightFinished -= HandleBossFightFinished;
                director.DistanceChanged -= HandleDistanceChanged;
                director.PhaseFinished -= HideImmediately;
            }

            KillCueSequence();
            HideImmediately();
        }

        private void HandlePhaseStarted(WarmupPhaseTimelineAsset _)
        {
            HideImmediately();
        }

        private void ShowCue(WarmupObstacleEvent obstacleEvent)
        {
            if (obstacleEvent == null || iconSet == null)
            {
                return;
            }

            if (obstacleEvent.IsCoin && !obstacleEvent.ShowViewerActionCue)
            {
                return;
            }

            WarmupActionType action = obstacleEvent.Action;
            if (!iconSet.TryGetIcon(action, out Sprite icon))
            {
                return;
            }

            if (cueImage == null || _cueGroup == null || _cueTransform == null)
            {
                return;
            }

            KillCueSequence();
            _isBossCue = obstacleEvent.IsBossWall;
            _waitForMovementAfterBoss = false;
            cueImage.sprite = icon;
            cueImage.enabled = true;
            _cueGroup.alpha = 0f;
            _cueTransform.localScale = Vector3.one * startScale;

            float totalDuration = Mathf.Max(
                minimumVisibleDuration,
                obstacleEvent.CueLeadTime);
            float entranceDuration = Mathf.Max(
                showDuration,
                scaleDuration);
            float holdDuration = Mathf.Max(
                0f,
                totalDuration - entranceDuration - hideDuration);

            _cueSequence = DOTween.Sequence()
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            _cueSequence.Append(
                _cueGroup.DOFade(1f, showDuration));
            _cueSequence.Join(
                _cueTransform
                    .DOScale(1f, scaleDuration)
                    .SetEase(Ease.OutBack));

            if (_isBossCue)
            {
                return;
            }

            _cueSequence.AppendInterval(holdDuration);
            _cueSequence.Append(
                _cueGroup.DOFade(0f, hideDuration));
            _cueSequence.OnComplete(ClearIcon);
        }

        private void HandleBossFightFinished()
        {
            if (!_isBossCue)
            {
                return;
            }

            _bossFinishedDistance = director != null
                ? director.DistanceMeters
                : 0f;
            _waitForMovementAfterBoss = true;
        }

        private void HandleDistanceChanged(float distance)
        {
            if (!_waitForMovementAfterBoss ||
                distance <= _bossFinishedDistance + 0.001f)
            {
                return;
            }

            _waitForMovementAfterBoss = false;
            _isBossCue = false;
            KillCueSequence();
            _cueSequence = DOTween.Sequence()
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
                .Append(_cueGroup.DOFade(0f, hideDuration))
                .OnComplete(ClearIcon);
        }

        private void EnsureCueVisual()
        {
            if (cueImage == null)
            {
                Debug.LogError(
                    "WarmupActionCuePresenter thiếu Viewer Action Cue Image.",
                    this);
                return;
            }

            _cueTransform = cueImage.rectTransform;
            _cueGroup = cueImage.GetComponent<CanvasGroup>();
            if (_cueGroup == null)
            {
                Debug.LogError(
                    "Viewer Action Cue thiếu CanvasGroup.",
                    cueImage);
                return;
            }

            _cueGroup.interactable = false;
            _cueGroup.blocksRaycasts = false;

            cueImage.preserveAspect = true;
            cueImage.raycastTarget = false;
        }

        private void HideImmediately()
        {
            KillCueSequence();
            _isBossCue = false;
            _waitForMovementAfterBoss = false;

            if (_cueGroup != null)
            {
                _cueGroup.alpha = 0f;
            }

            ClearIcon();
        }

        private void ClearIcon()
        {
            if (cueImage == null)
            {
                return;
            }

            cueImage.enabled = false;
            cueImage.sprite = null;
        }

        private void KillCueSequence()
        {
            _cueSequence?.Kill();
            _cueSequence = null;
        }

#if UNITY_EDITOR
        public void SetupComponents(
            WarmupObstacleTimelineDirector timelineDirector,
            WarmupActionIconSet actionIconSet,
            Image actionImage = null)
        {
            director = timelineDirector;
            iconSet = actionIconSet;
            cueImage = actionImage;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
