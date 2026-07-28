# Immersive Warmup

> **Trạng thái hiện tại:** đây là prototype để kiểm tra Player POV và gameplay cơ bản.
> `MonsterForestRunSequence.asset`, cue và map hiện tại chỉ là dữ liệu minh họa, chưa phải
> kịch bản hoặc map chính thức. Chỉ xây dựng nội dung hoàn chỉnh sau khi đã gom đủ asset
> và chốt kịch bản.

## Điều khiển

- `↑`: Nhảy
- Giữ `↓`: luôn cúi; thả phím để đứng dậy
- `←`: chuyển sang lane trái
- `→`: chuyển sang lane phải
- `J`: Đấm
- Player tự chạy về phía trước.

## Luồng hệ thống

```text
InputManager
    -> WarmupPlayerController
        -> CharacterController
        -> WarmupPunchInteractor
            -> IPunchable

WarmupSequenceAsset
    -> WarmupSequenceDirector
        -> WarmupCuePresenter
        -> Player speed / action feedback
```

## Asset chính

- `Assets/_GAME/_Data/Warmup/WarmupPlayerConfig.asset`
  - cấu hình dùng chung cho tốc độ chạy, làn đường, nhảy, cúi, camera và đấm.
- `Assets/_GAME/_Data/Warmup/MonsterForestRunSequence.asset`
  - timeline cue prototype 7 phút 30 giây, chỉ dùng để minh họa và test.
- `Assets/_GAME/_Scripts/Player/Player.prefab`
  - Player POV đã có camera head-bob, input, CharacterController và punch.
- `Assets/_GAME/_Scenes/WarnUp.unity`
  - scene mẫu có HUD, Sequence Director và Punch Gate.

## Cấu hình Player

Chọn `Assets/_GAME/_Data/Warmup/WarmupPlayerConfig.asset` để chỉnh. Inspector đã được
Việt hóa; rê chuột lên từng thuộc tính để xem tooltip giải thích.

| Nhóm | Thông số | Mặc định | Ý nghĩa |
|---|---|---:|---|
| Tự động chạy | Tốc độ tự động chạy | `6` | Tốc độ Player tiến về phía trước, tính theo đơn vị/giây. Tốc độ được giữ đều. |
| Tự động chạy | Khoảng cách giữa các làn | `2.2` | Khoảng cách ngang giữa hai làn liền kề. |
| Tự động chạy | Thời gian lách sang làn | `0.14` giây | Một cú lách tăng tốc, đạt tốc độ cao nhất giữa đoạn rồi hãm nhanh vào tâm làn. |
| Tự động chạy | Chỉ số làn tối đa mỗi bên | `1` | `1` tương ứng ba làn: trái `-1`, giữa `0`, phải `1`. |
| Nhảy | Độ cao nhảy | `1.25` | Độ cao tối đa của một lần nhảy. |
| Nhảy | Trọng lực | `-25` | Gia tốc kéo Player xuống; luôn dùng giá trị âm. |
| Cúi người | Chiều cao khi đứng | `1.8` | Chiều cao CharacterController ở tư thế đứng. |
| Cúi người | Chiều cao khi cúi | `1` | Chiều cao CharacterController ở tư thế cúi. |
| Camera | Góc nhìn | `75` | FOV camera; tăng để cảm giác chạy nhanh và gấp hơn. |
| Camera | Độ cao camera khi đứng | `1.6` | Độ cao camera khi Player đứng. |
| Camera | Độ cao camera khi cúi | `1.05` | Độ cao camera khi cúi; giảm để tạo cảm giác cúi sâu hơn. |
| Camera | Thời gian camera đổi độ cao | `0.08` giây | Độ mượt khi camera chuyển giữa đứng và cúi. |
| Đấm | Tầm đấm | `1.4` | Khoảng cách tối đa để phát hiện mục tiêu. |
| Đấm | Bán kính đấm | `0.65` | Bán kính vùng kiểm tra mục tiêu. |
| Đấm | Thời gian hồi đấm | `0.3` giây | Khoảng nghỉ tối thiểu giữa hai lần đấm. |
| Đấm | Lực đấm | `8` | Lực vật lý truyền vào mục tiêu khi đánh trúng. |

Nút `Khôi phục cấu hình mặc định` ở cuối Inspector sẽ đưa toàn bộ thông số về các
giá trị trong bảng. Thay đổi asset này sẽ ảnh hưởng mọi Player đang tham chiếu nó.

Cơ chế cúi sử dụng trạng thái giữ phím, không dùng timer. Player sẽ giữ nguyên
collider và độ cao camera ở tư thế cúi cho đến khi người chơi thả phím `↓`, phù hợp
để đi qua cống hoặc chướng ngại vật dài.

## Thêm cue khi bắt đầu sản xuất nội dung

1. Chọn `MonsterForestRunSequence.asset`.
2. Thêm phần tử trong `Cues`.
3. Điền `Start Time`, `Lead Time`, `Action Window`, `Action`, `Label`.
4. Dùng `Speed Multiplier` để đổi nhịp chạy theo biome/chase.

Label nên ngắn, viết hoa và không quá 16 ký tự để đọc rõ trên màn hình nhỏ.

## Camera POV

`WarmupHeadCameraMotion` nằm trên `Camera Pivot/Head Motion`.

- Camera bob theo nhịp hai bước chân, biên độ thấp và luôn dao động quanh vị trí gốc.
- Đường chân trời được giữ ổn định; roll/pitch liên tục chỉ dùng ở mức rất nhỏ để tránh say.
- FOV tăng nhẹ theo tốc độ để tạo cảm giác chạy nhanh mà không cần rung camera mạnh.
- Chuyển làn tạo một cú lách vai và roll ngắn theo hướng né, sau đó hồi ngay về tâm.
- Nhảy, cúi, tiếp đất và đấm chỉ tạo impulse ngắn rồi hồi về bằng damping.
- `Cường độ chuyển động` cho phép giảm nhanh toàn bộ bob/roll/pitch về `0` nếu cần chế độ chống say.
- Không rung Transform của Player, nên collider và gameplay vẫn ổn định.

Thiết lập mặc định ưu tiên trải nghiệm xem YouTube lâu: chuyển động có cảm giác nhưng
không giật, không rung ngẫu nhiên và không làm camera lệch khỏi tâm liên tục.

## Thêm obstacle đấm

1. Thêm Collider vào obstacle.
2. Thêm component `PunchableObstacle`.
3. Nếu có model vỡ, gán `Intact Visual` và `Broken Visual`.
4. Có thể gán ParticleSystem, AudioSource và UnityEvent `On Punched`.

Obstacle được reset khi dùng lại, phù hợp Object Pooling. Không cần Destroy.

## Tạo episode mới

Chỉ thực hiện phần này sau khi đã chốt kịch bản và có đủ asset cần thiết.

1. Duplicate `MonsterForestRunSequence.asset`.
2. Thay sequence cue và speed theo nội dung mới.
3. Thay biome, obstacle prefab, boss, companion và audio.
4. Giữ nguyên Player prefab, HUD, InputManager và Sequence Director.

Mỗi episode nên thay content data/prefab, không copy hoặc sửa core player.

## Editor tool

Menu `Tools > Immersive Warmup > Setup MVP` dùng để cấu hình lại prefab/scene nếu
reference bị mất. Tool giữ lại Sequence Asset hiện có, không ghi đè cue đã chỉnh.
