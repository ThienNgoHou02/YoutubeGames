using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace GameYT.Warmup
{
    [DisallowMultipleComponent]
    public sealed class WarmupGameplayHud : MonoBehaviour
    {
        [Title("References")]
        [InfoBox(
            "Director được gán tại scene. Nếu để trống, HUD tự tìm một lần khi OnEnable.",
            InfoMessageType.Info)]
        [SerializeField] private WarmupObstacleTimelineDirector director;

        [Required]
        [SerializeField] private Slider phaseProgressSlider;

        [Required]
        [SerializeField] private Text kilometRun;

        [Required]
        [SerializeField] private Slider bossHealthSlider;

        [SerializeField] private Text bossHealthLabel;

        [SerializeField] private GameObject bossHealthRoot;

        private string[] _meterLabels;
        private int _displayedMeter = -1;

        private void Awake()
        {
            if (phaseProgressSlider != null)
            {
                phaseProgressSlider.minValue = 0f;
                phaseProgressSlider.maxValue = 1f;
                phaseProgressSlider.value = 0f;
                phaseProgressSlider.interactable = false;
            }

            if (bossHealthSlider != null)
            {
                bossHealthSlider.interactable = false;
            }

            if (bossHealthLabel != null)
            {
                bossHealthLabel.text = "4 / 4";
            }

            if (kilometRun != null)
            {
                kilometRun.text = "0 m";
            }

            SetBossHealthVisible(false);
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
            director.ProgressChanged += HandleProgressChanged;
            director.DistanceChanged += HandleDistanceChanged;
            director.BossFightStarted += HandleBossFightStarted;
            director.BossHealthChanged += HandleBossHealthChanged;
            director.BossFightFinished += HandleBossFightFinished;

            if (director.Phase != null)
            {
                HandlePhaseStarted(director.Phase);
            }
        }

        private void OnDisable()
        {
            if (director == null)
            {
                return;
            }

            director.PhaseStarted -= HandlePhaseStarted;
            director.ProgressChanged -= HandleProgressChanged;
            director.DistanceChanged -= HandleDistanceChanged;
            director.BossFightStarted -= HandleBossFightStarted;
            director.BossHealthChanged -= HandleBossHealthChanged;
            director.BossFightFinished -= HandleBossFightFinished;
        }

        private void HandlePhaseStarted(WarmupPhaseTimelineAsset phase)
        {
            float runSpeed =
                director != null && director.ActiveRunSpeed > 0f
                    ? director.ActiveRunSpeed
                    : phase.MetersPerSecond;
            int maximumMeter =
                Mathf.CeilToInt(phase.Duration * runSpeed);
            BuildMeterLabelCache(maximumMeter);
            _displayedMeter = -1;
            HandleProgressChanged(0f);
            HandleDistanceChanged(0f);
            SetBossHealthVisible(false);
        }

        private void HandleProgressChanged(float normalizedProgress)
        {
            if (phaseProgressSlider != null)
            {
                phaseProgressSlider.value = Mathf.Clamp01(normalizedProgress);
            }
        }

        private void HandleDistanceChanged(float distance)
        {
            int meter = Mathf.Max(0, Mathf.FloorToInt(distance));
            if (meter == _displayedMeter || kilometRun == null)
            {
                return;
            }

            _displayedMeter = meter;
            kilometRun.text =
                _meterLabels != null && meter < _meterLabels.Length
                    ? _meterLabels[meter]
                    : meter.ToString("0") + " m";
        }

        private void HandleBossFightStarted(WarmupBossWall wall)
        {
            SetBossHealthVisible(true);
            HandleBossHealthChanged(
                wall.CurrentHitPoints,
                wall.MaxHitPoints);
        }

        private void HandleBossHealthChanged(int currentHealth, int maxHealth)
        {
            if (bossHealthSlider == null)
            {
                return;
            }

            bossHealthSlider.minValue = 0f;
            bossHealthSlider.maxValue = Mathf.Max(1, maxHealth);
            bossHealthSlider.value = Mathf.Clamp(currentHealth, 0, maxHealth);

            if (bossHealthLabel != null)
            {
                bossHealthLabel.text =
                    currentHealth.ToString("0") + " / " +
                    maxHealth.ToString("0");
            }
        }

        private void HandleBossFightFinished()
        {
            SetBossHealthVisible(false);
        }

        private void SetBossHealthVisible(bool isVisible)
        {
            GameObject target =
                bossHealthRoot != null
                    ? bossHealthRoot
                    : bossHealthSlider != null
                        ? bossHealthSlider.gameObject
                        : null;
            target?.SetActive(isVisible);
        }

        private void BuildMeterLabelCache(int maximumMeter)
        {
            if (_meterLabels != null &&
                _meterLabels.Length == maximumMeter + 1)
            {
                return;
            }

            _meterLabels = new string[maximumMeter + 1];
            for (int i = 0; i <= maximumMeter; i++)
            {
                _meterLabels[i] = i.ToString("0") + " m";
            }
        }

#if UNITY_EDITOR
        [Button("Auto Assign References")]
        private void AutoAssignReferences()
        {
            Slider[] sliders = GetComponentsInChildren<Slider>(true);
            for (int i = 0; i < sliders.Length; i++)
            {
                if (sliders[i].gameObject.name == "SliderHp")
                {
                    bossHealthSlider = sliders[i];
                    bossHealthRoot = sliders[i].gameObject;
                }
                else if (sliders[i].gameObject.name == "Slider")
                {
                    phaseProgressSlider = sliders[i];
                }
            }

            Text[] texts = GetComponentsInChildren<Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i].gameObject.name == "KilometRun")
                {
                    kilometRun = texts[i];
                }
                else if (texts[i].gameObject.name == "BossHealthText")
                {
                    bossHealthLabel = texts[i];
                }
            }

            UnityEditor.EditorUtility.SetDirty(this);
        }

        public void SetupComponents(
            WarmupObstacleTimelineDirector timelineDirector,
            Slider progressSlider,
            Text distanceText,
            Slider healthSlider,
            Text healthLabel,
            GameObject healthRoot)
        {
            director = timelineDirector;
            phaseProgressSlider = progressSlider;
            kilometRun = distanceText;
            bossHealthSlider = healthSlider;
            bossHealthLabel = healthLabel;
            bossHealthRoot = healthRoot;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
