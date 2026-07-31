using Sirenix.OdinInspector;
using UnityEngine;

namespace GameYT.Warmup
{
    public sealed class WarmupPoseSpriteTarget : MonoBehaviour
    {
        [BoxGroup("References")]
        [Required]
        [SerializeField] private SpriteRenderer poseRenderer;

        public void SetPose(Sprite poseSprite)
        {
            if (poseSprite == null)
            {
                return;
            }

            if (poseRenderer == null)
            {
                Debug.LogError(
                    "WarmupPoseSpriteTarget thiếu SpriteRenderer của Mirror Human.",
                    this);
                return;
            }

            poseRenderer.sprite = poseSprite;
        }

#if UNITY_EDITOR
        [Button("Auto Assign References")]
        private void AutoAssignReferences()
        {
            SpriteRenderer[] renderers =
                GetComponentsInChildren<SpriteRenderer>(true);

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i].gameObject.name != "Mirror Human")
                {
                    continue;
                }

                poseRenderer = renderers[i];
                UnityEditor.EditorUtility.SetDirty(this);
                return;
            }

            Debug.LogError(
                "Không tìm thấy SpriteRenderer trên child 'Mirror Human'.",
                this);
        }
#endif
    }
}
