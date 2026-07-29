# Hướng dẫn setup Warmup Obstacle Timeline

Tài liệu này hướng dẫn cách dùng hệ gameplay obstacle cho từng video mà không
cần sửa code.

## 1. Cấu trúc hệ thống

```text
Step Timeline Asset
    Thời lượng + nhịp obstacle + action + lane
                    |
                    v
Video Obstacle Prefab Set
    Prefab riêng của từng video
                    |
                    v
WarmupObstacleTimelineDirector
    Dựng course + điều khiển Player + phát event HUD
                    |
                    v
Warmup HUD
    Mét chạy + phase progress + boss HP
```

- `Step Timeline` được dùng lại cho nhiều video.
- Mỗi video nên có một `WarmupObstaclePrefabSet` riêng.
- Thay art hoặc chủ đề video chỉ cần thay prefab trong Prefab Set.

## 2. Setup lần đầu

Chỉ chạy menu này khi chưa có bộ asset Step 1–6:

```text
Tools
└── Immersive Warmup
    └── Obstacle Timeline
        └── Build Video0 Demo
```

Menu sẽ:

1. Tạo `Step1Timeline` đến `Step6Timeline`.
2. Tạo `Video0ObstaclePrefabSet`.
3. Gắn Step 1 vào scene `WarnUp`.
4. Nối `Warmup HUD`.
5. Tắt hệ `WarmupSequenceDirector` cũ.

> Không chạy lại `Build Video0 Demo` sau khi đã chỉnh timeline thủ công vì
> menu này sẽ ghi lại dữ liệu Step mẫu.

## 3. Chọn Step để chạy

Cách nhanh nhất:

```text
Tools > Immersive Warmup > Obstacle Timeline > Timeline Dashboard
```

Dashboard cho phép chọn Step 1–6, chỉnh nhanh tốc độ, xem timeline trực quan,
thống kê khoảng cách giữa obstacle, validate dữ liệu và Apply vào scene.

Hoặc dùng menu trực tiếp:

```text
Tools > Immersive Warmup > Obstacle Timeline > Apply Step 1
Tools > Immersive Warmup > Obstacle Timeline > Apply Step 2
...
Tools > Immersive Warmup > Obstacle Timeline > Apply Step 6
```

Hoặc chọn object `Warmup Game` trong scene rồi kéo asset cần dùng vào:

```text
WarmupObstacleTimelineDirector
├── Phase: StepXTimeline
├── Prefab Set: VideoXObstaclePrefabSet
├── Player: Player
└── Obstacle Root: Obstacle Timeline Content
```

Các menu `Apply Step` mặc định gán lại `Video0ObstaclePrefabSet`. Với Video1
trở lên, sau khi chọn Step hãy kéo lại đúng `VideoXObstaclePrefabSet`, hoặc
gán cả `Phase` và `Prefab Set` trực tiếp trên director.

## 4. Tạo Prefab Set cho video mới

Ví dụ cần làm `Video1`:

1. Chuẩn bị folder:

   ```text
   Assets/_GAME/obstacle_Prefab/Video1
   ```

2. Trong Project, chọn:

   ```text
   Create > Game YT > Obstacle Timeline > Video Prefab Set
   ```

3. Đặt tên:

   ```text
   Video1ObstaclePrefabSet
   ```

4. Điền `Video Id = Video1`.
5. Kéo prefab vào đúng nhóm:

   - `Jump Prefabs`: vật cản phải nhảy qua.
   - `Pose Wall Prefabs`: sprite tạo dáng.
   - `Duck Barrier Prefabs`: vật chắn phía trên để cúi.
   - `Lane Blocker Prefabs`: vật cản một lane.
   - `Boss Wall Prefabs`: tường cần đấm.

6. Kéo `Video1ObstaclePrefabSet` vào field `Prefab Set` của director.

Một nhóm có thể chứa nhiều prefab. Field `Prefab Variation` trong event dùng
index để chọn biến thể:

```text
0 = prefab đầu tiên
1 = prefab thứ hai
2 = prefab thứ ba
```

Nếu index lớn hơn số prefab, hệ thống tự xoay vòng về prefab hợp lệ.

## 5. Yêu cầu cho từng loại prefab

### Jump

- Có collider.
- Pivot nên nằm ở tâm hoặc chân vật cản.
- Chiều cao sau khi áp `Scale Multiplier` nên khoảng `0.7–1.1 m`.
- Không gắn Rigidbody động.

### Pose Wall

- Thường dùng `SpriteRenderer`.
- Có thể không cần collider.
- Timeline mặc định dùng `Disable All` để character tự động đi xuyên qua.
- Sprite nên quay mặt về hướng Player chạy tới.

### Duck Barrier

- Có collider.
- Đáy obstacle nên cao hơn chiều cao cúi của Player.
- Đủ thấp để Player đứng thẳng sẽ va chạm.

### Lane Blocker

- Có collider.
- Chiều rộng nên xấp xỉ một lane.
- Không chắn cả ba lane.

### Boss Wall

- Có collider.
- Nên gắn sẵn `WarmupBossWall`.
- Nên có `WarmupPaperShardBurst`.
- Có thể kéo Particle System vào `Punch Vfx`.

Nếu boss prefab chưa có `WarmupBossWall`, director sẽ tự thêm lúc runtime,
nhưng khi đó không thể cấu hình sẵn VFX/Audio từ prefab.

## 6. Chỉnh một Timeline Event

Mở `StepXTimeline`, sau đó mở foldout của event cần chỉnh.

### Encounter

- `Time`: thời điểm Player gặp obstacle.
- `Type`: loại gameplay obstacle.
- `Lane`: vị trí Left, Center hoặc Right.

Vị trí obstacle theo trục chạy được tính gần đúng:

```text
Distance = Encounter Time × Run Speed
```

`Speed Source` có hai chế độ:

- `Player Config`: mặc định, dùng `Auto Run Speed` trong `WarmupPlayerConfig`.
- `Phase Override`: chỉ dùng khi Step cần tốc độ riêng.

Course, HUD và vị trí obstacle luôn dùng cùng một tốc độ đã resolve, nên đổi
`WarmupPlayerConfig` không làm lệch encounter time.

Ví dụ:

```text
Time = 10 sec
Run Speed = 13 m/s
Obstacle Distance = 130 m
```

### Viewer Cue

- `Action`: thao tác Player cần làm.
- `HUD Label`: label hướng dẫn như `JUMP!`, `LEFT!`, `DUCK!`.
- `Show Before`: cue xuất hiện trước obstacle bao nhiêu giây.

Director phát dữ liệu này qua event `CueStarted`. `Warmup HUD` hiện tại chỉ
hiển thị mét, phase progress và boss HP; nó không vẽ action label. Có thể nối
một presenter riêng vào `CueStarted` nếu video cần overlay hướng dẫn.

### Prefab Source

- `Variation`: chọn prefab trong `Video Prefab Set`.
- `Override`: prefab riêng chỉ dùng cho event này.

Nếu có `Override`, hệ thống bỏ qua prefab tương ứng trong Prefab Set.

### Transform Override

- `Position`: offset vị trí sau khi hệ thống đặt obstacle.
- `Rotation`: góc xoay bổ sung.
- `Scale`: scale nhân với scale gốc của prefab.

Không sửa transform của prefab chỉ để phục vụ một event. Hãy dùng các field
override này.

### Behaviour

`Collider` có ba chế độ:

- `Use Prefab`: giữ nguyên collider.
- `Disable All`: tắt toàn bộ collider, phù hợp Pose Wall.
- `Trigger All`: chuyển collider thành trigger.

Boss Wall có thêm:

- `Boss Hit Points`: mặc định `4`.
- `Boss Stop Distance`: khoảng cách Player dừng trước tường, mặc định `1.6 m`.

## 7. Thêm, xóa và sắp xếp event

- Nhấn nút `+` trên danh sách để thêm event.
- Nhấn `X` trên item để xóa.
- Kéo handle bên trái để đổi thứ tự.
- Asset tự sort lại theo `Encounter Time` khi validate.

Khoảng nghỉ khuyến nghị:

- Step 1: `4.5–7 giây` giữa hai lần nhảy.
- Step 2: `5–6 giây` giữa hai Pose Wall.
- Step 3–5: `4.5–6 giây`, tránh lặp cùng action liên tiếp.
- Step 6: `3.5–4.5 giây`.
- Sau Boss Wall nên có ít nhất `4 giây` hồi nhịp.

## 8. Setup Warmup HUD

Prefab:

```text
Assets/_GAME/_Scripts/Warmup/UI/Warmup HUD.prefab
```

Prefab đã có sẵn:

```text
Warmup HUD
├── Slider          Phase progress
├── KilometRun      Mét chạy mô phỏng
├── SliderHp        Boss HP
└── WarmupGameplayHud
```

`WarmupGameplayHud` cần các reference:

- `Phase Progress Slider` → `Slider`.
- `Kilomet Run` → `KilometRun`.
- `Boss Health Slider` → `SliderHp`.
- `Boss Health Label` → `BossHealthText`.
- `Boss Health Root` → GameObject `SliderHp`.

`Director` được gán ở scene. Nếu để trống, HUD sẽ tự tìm
`WarmupObstacleTimelineDirector` một lần trong `OnEnable`.

Runtime:

- Mét bắt đầu từ `0 m`.
- Phase Slider bắt đầu từ `0` và đầy khi phase kết thúc.
- `SliderHp` chỉ bật khi gặp Boss Wall.
- Mỗi cú đấm làm giảm một HP.
- Khi boss vỡ, `SliderHp` tự tắt.

## 9. Setup VFX đấm boss

Trên boss prefab:

1. Add `WarmupBossWall`.
2. Add `WarmupPaperShardBurst`.
3. Kéo renderer tường vào `Intact Renderer`.
4. Kéo Particle System vào `Punch Vfx`.
5. Nếu có âm thanh, kéo Audio Source vào `Punch Audio`.
6. Để HP mặc định là `4`.

Particle System nên:

- Tắt `Play On Awake`.
- Dùng world simulation space nếu muốn VFX đứng tại điểm va chạm.
- Thời lượng ngắn khoảng `0.2–0.5 giây`.
- Không tạo quá nhiều particle để tránh che màn hình.

## 10. Phím test hiện tại

```text
Up Arrow       Jump
Down Arrow     Duck
Left Arrow     Move Left
Right Arrow    Move Right
J              Punch
```

## 11. Checklist trước khi quay video

- Đúng Step và đúng `Video Prefab Set`.
- Không có Missing Prefab trong Timeline.
- Jump obstacle có collider.
- Pose Wall dùng `Disable All`.
- Lane Blocker chỉ chiếm một lane.
- Player có thể cúi qua Duck Barrier.
- Boss dừng trong tầm đấm.
- Boss cần đúng 4 cú đấm.
- Punch VFX không che toàn màn hình.
- `KilometRun` bắt đầu từ `0 m`.
- Phase Slider đầy đúng lúc kết thúc.
- Không có `WarmupCuePresenter` trên `Warmup HUD`.
- Console không có NullReference hoặc MissingReference.

## 12. Lỗi thường gặp

### Không thấy HUD

- Kiểm tra `Warmup HUD` đang active.
- Root scale phải là `(1, 1, 1)`.
- Canvas phải dùng `Screen Space - Overlay`.

### Mét không tăng

- Kiểm tra field `Director`.
- Kiểm tra `WarmupObstacleTimelineDirector` đang `Is Playing`.
- Kiểm tra `Phase` đã được gán.

### Progress không chạy

- Kiểm tra reference `Phase Progress Slider`.
- Slider phải có `Min = 0`, `Max = 1`.

### Pose Wall chặn Player

- Đổi `Collision Mode` của event sang `Disable All`.

### Player dừng quá xa hoặc quá gần boss

- Chỉnh `Boss Stop Distance`.
- Giá trị khởi đầu phù hợp là `1.4–1.8 m`.
- Kiểm tra `Punch Range` và `Punch Radius` trong `WarmupPlayerConfig`.

### VFX đấm không chạy

- Gắn `WarmupBossWall` trực tiếp lên boss prefab.
- Kéo Particle System vào `Punch Vfx`.
- Kiểm tra Particle System không bị inactive.
