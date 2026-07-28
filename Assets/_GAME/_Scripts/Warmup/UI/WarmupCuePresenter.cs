using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameYT.Warmup
{
    [DisallowMultipleComponent]
    public sealed class WarmupCuePresenter : MonoBehaviour
    {
        private const string GoText = "GO!";
        private const string ThreeText = "3";
        private const string TwoText = "2";
        private const string OneText = "1";

        [Title("References")]
        [Required]
        [SerializeField] private WarmupSequenceDirector director;

        [Required]
        [SerializeField] private CanvasGroup cueGroup;

        [Required]
        [SerializeField] private TMP_Text actionLabel;

        [Required]
        [SerializeField] private TMP_Text countdownLabel;

        [Required]
        [SerializeField] private Image accentImage;

        [Required]
        [SerializeField] private Image progressFill;

        [Title("Animation")]
        [MinValue(0.01f)]
        [SerializeField] private float showDuration = 0.18f;

        [MinValue(0.01f)]
        [SerializeField] private float hideDuration = 0.15f;

        private Tween _visibilityTween;
        private Tween _countdownTween;

        private void Awake()
        {
            cueGroup.alpha = 0f;
            cueGroup.blocksRaycasts = false;
        }

        private void OnEnable()
        {
            if (director == null)
            {
                return;
            }

            director.CueStarted += ShowCue;
            director.CountdownChanged += ShowCountdown;
            director.CueFinished += HideCue;
            director.ProgressChanged += SetProgress;
        }

        private void OnDisable()
        {
            if (director != null)
            {
                director.CueStarted -= ShowCue;
                director.CountdownChanged -= ShowCountdown;
                director.CueFinished -= HideCue;
                director.ProgressChanged -= SetProgress;
            }

            _visibilityTween?.Kill();
            _countdownTween?.Kill();
        }

        private void ShowCue(WarmupCue cue)
        {
            actionLabel.text = cue.Label;
            accentImage.color = GetActionColor(cue.Action);

            _visibilityTween?.Kill();
            cueGroup.alpha = 0f;
            _visibilityTween = cueGroup
                .DOFade(1f, showDuration)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        }

        private void ShowCountdown(int value)
        {
            switch (value)
            {
                case 3:
                    countdownLabel.text = ThreeText;
                    break;
                case 2:
                    countdownLabel.text = TwoText;
                    break;
                case 1:
                    countdownLabel.text = OneText;
                    break;
                default:
                    countdownLabel.text = GoText;
                    break;
            }

            _countdownTween?.Kill();
            countdownLabel.rectTransform.localScale = Vector3.one;
            _countdownTween = countdownLabel.rectTransform
                .DOPunchScale(Vector3.one * 0.18f, 0.16f, 4, 0.6f)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        }

        private void HideCue(WarmupCue cue, bool succeeded)
        {
            _visibilityTween?.Kill();
            _visibilityTween = cueGroup
                .DOFade(0f, hideDuration)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        }

        private void SetProgress(float normalizedProgress)
        {
            progressFill.fillAmount = normalizedProgress;
        }

        private static Color GetActionColor(WarmupActionType action)
        {
            switch (action)
            {
                case WarmupActionType.Run:
                    return new Color(0.2f, 0.9f, 0.35f);
                case WarmupActionType.MoveLeft:
                case WarmupActionType.MoveRight:
                    return new Color(0.15f, 0.55f, 1f);
                case WarmupActionType.Jump:
                    return new Color(1f, 0.85f, 0.1f);
                case WarmupActionType.Duck:
                    return new Color(1f, 0.45f, 0.08f);
                case WarmupActionType.Punch:
                    return new Color(1f, 0.18f, 0.18f);
                case WarmupActionType.Freeze:
                    return new Color(0.75f, 0.55f, 1f);
                default:
                    return Color.white;
            }
        }

#if UNITY_EDITOR
        public void SetupComponents(
            WarmupSequenceDirector sequenceDirector,
            CanvasGroup canvasGroup,
            TMP_Text actionText,
            TMP_Text countdownText,
            Image accent,
            Image progress)
        {
            director = sequenceDirector;
            cueGroup = canvasGroup;
            actionLabel = actionText;
            countdownLabel = countdownText;
            accentImage = accent;
            progressFill = progress;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
