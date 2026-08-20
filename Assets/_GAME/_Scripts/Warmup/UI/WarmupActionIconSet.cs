using Sirenix.OdinInspector;
using UnityEngine;

namespace GameYT.Warmup
{
    [CreateAssetMenu(
        fileName = "ActionIconSet",
        menuName = "Game YT/Obstacle Timeline/Action Icon Set")]
    public sealed class WarmupActionIconSet : ScriptableObject
    {
        [Title("Action Icons")]
        [Required, AssetsOnly]
        [SerializeField] private Sprite jump;

        [Required, AssetsOnly]
        [SerializeField] private Sprite duck;

        [Required, AssetsOnly]
        [SerializeField] private Sprite punch;

        [Required, AssetsOnly]
        [LabelText("Mirror Me")]
        [SerializeField] private Sprite mirrorMe;

        [Title("Optional Lane Icons")]
        [Required, AssetsOnly]
        [SerializeField] private Sprite dodgeLeft;

        [Required, AssetsOnly]
        [SerializeField] private Sprite dodgeRight;

        public bool TryGetIcon(
            WarmupActionType action,
            out Sprite icon)
        {
            switch (action)
            {
                case WarmupActionType.MoveLeft:
                    icon = dodgeLeft;
                    break;
                case WarmupActionType.MoveRight:
                    icon = dodgeRight;
                    break;
                case WarmupActionType.Jump:
                    icon = jump;
                    break;
                case WarmupActionType.Duck:
                    icon = duck;
                    break;
                case WarmupActionType.Punch:
                    icon = punch;
                    break;
                case WarmupActionType.Freeze:
                    icon = mirrorMe;
                    break;
                default:
                    icon = null;
                    break;
            }

            return icon != null;
        }
    }
}
