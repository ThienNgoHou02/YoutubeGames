using Sirenix.OdinInspector;
using UnityEngine;

namespace GameYT.Warmup
{
    [CreateAssetMenu(
        fileName = "WarmupPlayerConfig",
        menuName = "Game YT/Immersive Warmup/Cấu hình Player")]
    public sealed class WarmupPlayerConfig : ScriptableObject
    {
        [Title("Tự động chạy")]
        [LabelText("Tốc độ tự động chạy")]
        [Tooltip("Tốc độ Player chạy thẳng về phía trước, tính theo đơn vị/giây.")]
        [MinValue(0f)]
        [SerializeField] private float autoRunSpeed = 6f;

        [LabelText("Khoảng cách giữa các làn")]
        [Tooltip("Khoảng cách theo trục ngang giữa hai làn liền kề.")]
        [MinValue(0.1f)]
        [SerializeField] private float laneWidth = 2.2f;

        [LabelText("Thời gian lách sang làn")]
        [Tooltip("Thời gian hoàn thành một cú lách làn. Giá trị nhỏ tạo cảm giác né nhanh và dứt khoát hơn.")]
        [MinValue(0.01f)]
        [SerializeField] private float laneChangeSmoothTime = 0.14f;

        [LabelText("Chỉ số làn tối đa mỗi bên")]
        [Tooltip("Giới hạn làn tính từ làn giữa. Giá trị 1 tạo ba làn: trái, giữa và phải.")]
        [MinValue(1)]
        [SerializeField] private int maximumLaneIndex = 1;

        [Title("Nhảy")]
        [LabelText("Độ cao nhảy")]
        [Tooltip("Độ cao tối đa của một lần nhảy.")]
        [MinValue(0.1f)]
        [SerializeField] private float jumpHeight = 1.25f;

        [LabelText("Trọng lực")]
        [Tooltip("Gia tốc kéo Player xuống. Giá trị phải là số âm.")]
        [MaxValue(-0.1f)]
        [SerializeField] private float gravity = -25f;

        [Title("Cúi người")]
        [LabelText("Chiều cao khi đứng")]
        [Tooltip("Chiều cao CharacterController khi Player đứng.")]
        [MinValue(0.1f)]
        [SerializeField] private float standingHeight = 1.8f;

        [LabelText("Chiều cao khi cúi")]
        [Tooltip("Chiều cao CharacterController khi Player cúi.")]
        [MinValue(0.1f)]
        [SerializeField] private float duckHeight = 1f;

        [Title("Camera góc nhìn thứ nhất")]
        [LabelText("Góc nhìn")]
        [Tooltip("Field of View của camera. Giá trị lớn tạo cảm giác tốc độ cao hơn.")]
        [Range(60f, 90f)]
        [SerializeField] private float fieldOfView = 75f;

        [LabelText("Độ cao camera khi đứng")]
        [Tooltip("Vị trí cao của camera khi Player đứng.")]
        [MinValue(0.1f)]
        [SerializeField] private float cameraStandingHeight = 1.6f;

        [LabelText("Độ cao camera khi cúi")]
        [Tooltip("Vị trí cao của camera khi Player cúi. Giảm giá trị này để cúi sâu hơn.")]
        [MinValue(0.1f)]
        [SerializeField] private float cameraDuckHeight = 1.05f;

        [LabelText("Thời gian camera đổi độ cao")]
        [Tooltip("Thời gian làm mượt chuyển động camera giữa tư thế đứng và cúi.")]
        [MinValue(0.01f)]
        [SerializeField] private float cameraHeightSmoothTime = 0.08f;

        [Title("Đấm")]
        [LabelText("Tầm đấm")]
        [Tooltip("Khoảng cách tối đa tính từ camera để cú đấm trúng mục tiêu.")]
        [MinValue(0.1f)]
        [SerializeField] private float punchRange = 1.4f;

        [LabelText("Bán kính đấm")]
        [Tooltip("Bán kính vùng kiểm tra mục tiêu của cú đấm.")]
        [MinValue(0.05f)]
        [SerializeField] private float punchRadius = 0.65f;

        [LabelText("Thời gian hồi đấm")]
        [Tooltip("Thời gian tối thiểu giữa hai lần đấm.")]
        [MinValue(0f)]
        [SerializeField] private float punchCooldown = 0.3f;

        [LabelText("Lực đấm")]
        [Tooltip("Lực vật lý truyền vào mục tiêu khi cú đấm trúng.")]
        [MinValue(0f)]
        [SerializeField] private float punchStrength = 8f;

        public float AutoRunSpeed => autoRunSpeed;
        public float LaneWidth => laneWidth;
        public float LaneChangeSmoothTime => laneChangeSmoothTime;
        public int MaximumLaneIndex => maximumLaneIndex;
        public float JumpHeight => jumpHeight;
        public float Gravity => gravity;
        public float StandingHeight => standingHeight;
        public float DuckHeight => duckHeight;
        public float FieldOfView => fieldOfView;
        public float CameraStandingHeight => cameraStandingHeight;
        public float CameraDuckHeight => cameraDuckHeight;
        public float CameraHeightSmoothTime => cameraHeightSmoothTime;
        public float PunchRange => punchRange;
        public float PunchRadius => punchRadius;
        public float PunchCooldown => punchCooldown;
        public float PunchStrength => punchStrength;

#if UNITY_EDITOR
        [Button("Khôi phục cấu hình mặc định")]
        private void ResetConfiguration()
        {
            autoRunSpeed = 6f;
            laneWidth = 2.2f;
            laneChangeSmoothTime = 0.14f;
            maximumLaneIndex = 1;
            jumpHeight = 1.25f;
            gravity = -25f;
            standingHeight = 1.8f;
            duckHeight = 1f;
            fieldOfView = 75f;
            cameraStandingHeight = 1.6f;
            cameraDuckHeight = 1.05f;
            cameraHeightSmoothTime = 0.08f;
            punchRange = 1.4f;
            punchRadius = 0.65f;
            punchCooldown = 0.3f;
            punchStrength = 8f;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
