# Warmup Obstacle Timeline

Hướng dẫn setup đầy đủ bằng tiếng Việt:
`SETUP_GUIDE_VI.md`.

## Luồng dữ liệu

```text
Step1..Step6 Timeline Asset
            |
            +---- EncounterTime / Action / Lane / Transform
            |
VideoX Obstacle Prefab Set
            |
            +---- Jump / Pose / Duck / Lane / Boss prefab
            |
WarmupObstacleTimelineDirector
            |
            +---- Player auto-run
            +---- Runtime obstacles
            +---- Warmup HUD: Slider / KilometRun / SliderHp
```

Timeline của Step được dùng lại giữa các video. Mỗi video chỉ cần một
`WarmupObstaclePrefabSet` riêng.

## Setup Video0

1. Chờ Unity compile xong.
2. Chạy menu:
   `Tools > Immersive Warmup > Obstacle Timeline > Build Video0 Demo`.
3. Mở `Video0ObstaclePrefabSet` và kéo prefab vào đúng nhóm.
4. Mở `Timeline Dashboard` để chọn, validate và Apply Step vào scene.

Đường dẫn:

`Tools > Immersive Warmup > Obstacle Timeline > Timeline Dashboard`

Mặc định timeline lấy tốc độ từ `WarmupPlayerConfig`. Chỉ chọn
`Phase Override` khi một Step thực sự cần tốc độ riêng. Director dùng cùng tốc
độ đã resolve cho player, course và HUD.

`Step1Timeline` mặc định dài 40 giây và chỉ có Jump. Các Step còn lại:

- Step 2: 60 giây, 10 Pose Wall, collider bị tắt để character tự đi qua.
- Step 3: 72 giây, phối Jump + Pose + Duck.
- Step 4: 66 giây, thêm chuỗi lách lane có đường quay về lane giữa.
- Step 5: 68 giây, thêm Boss Wall 4 HP.
- Step 6: 64 giây, mật độ thử thách cao hơn.

## Boss Wall

Nếu boss prefab chưa có `WarmupBossWall`, director sẽ tự thêm lúc runtime.
Để tùy biến tốt hơn, gắn sẵn component lên prefab rồi kéo:

- `Intact Renderer`
- `Punch Vfx`
- `Punch Audio`
- `WarmupPaperShardBurst`

`WarmupPaperShardBurst` dùng 16 mảnh lớn, tạo một lần khi vỡ và không chạy
logic riêng trong `Update`.
