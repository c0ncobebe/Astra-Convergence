# Hướng dẫn tạo ConnectionLineRenderer Prefab

## Bước 1: Tạo GameObject mới trong Unity
1. Trong Unity Editor, vào **Hierarchy** panel
2. Click chuột phải → **Create Empty**
3. Đổi tên thành **"ConnectionLineRenderer"**

## Bước 2: Thêm Component ConnectionLineRenderer
1. Chọn GameObject **ConnectionLineRenderer** vừa tạo
2. Trong **Inspector** panel, click **Add Component**
3. Tìm và chọn **"Connection Line Renderer"** script

## Bước 3: Cấu hình LineRenderer
Script đã tự động thêm LineRenderer component. Bây giờ cần cấu hình:

### Line Settings:
- **Line Width**: 0.1 (hoặc tùy chỉnh theo ý thích)
- **Line Color**: Màu vàng hoặc màu bạn muốn
- **Line Material**: 
  - Tạo Material mới: Assets → Create → Material
  - Đặt tên: "LineMaterial"
  - Shader: Chọn **"Sprites/Default"** hoặc **"Universal Render Pipeline/2D/Sprite-Unlit"**
  - Gán Material này vào field **Line Material**

### Animation Settings:
- **Line Grow Speed**: 10
- **Use Gradient**: Tick/Untick tùy ý
- **Line Gradient**: (Nếu Use Gradient = true)
  - Click vào color bar để chỉnh gradient
  - Ví dụ: Vàng đầu → Cam cuối

### Effect Settings:
- **Animate Width**: True (để line có hiệu ứng xuất hiện mượt)
- **Width Animation Duration**: 0.3

## Bước 4: Cấu hình LineRenderer Component
Scroll xuống tìm **LineRenderer** component (tự động thêm), cấu hình:

- **Positions**: 0 (sẽ được script tự động set)
- **Width**: Start = 0.1, End = 0.1
- **Color**: Màu vàng hoặc theo gradient
- **Materials**: Size = 1
  - Element 0: Gán **LineMaterial** đã tạo ở trên
- **Corner Vertices**: 5
- **End Cap Vertices**: 5
- **Alignment**: Transform Z
- **Sorting Layer**: Default (hoặc layer phù hợp với game của bạn)
- **Order in Layer**: 10 (để line hiển thị phía trên stars)

## Bước 5: Tạo Prefab
1. Kéo GameObject **ConnectionLineRenderer** từ **Hierarchy** xuống folder **Assets/_MyGame/_Prefabs/**
2. Xóa GameObject trong Hierarchy (vì đã có Prefab rồi)

## Bước 6: Gán Prefab vào GamePlayController
1. Tìm GameObject có **GamePlayController** script trong Scene
2. Trong **Inspector**, tìm section **"Line Renderer"**
3. Kéo prefab **ConnectionLineRenderer** từ folder _Prefabs vào field **"Line Renderer Prefab"**

## Hoàn thành!
Bây giờ khi chạy game:
- Khi bạn hold và drag qua các stars, một đường line màu vàng sẽ xuất hiện nối các stars lại
- Đường line sẽ theo ngón tay/chuột của bạn
- Khi thả ra, nếu valid thì line sẽ được giữ lại, nếu invalid sẽ fade out

## Tùy chỉnh thêm (Optional)
### Để line đẹp hơn:
1. **Thêm glow effect**: Sử dụng Bloom trong Post Processing
2. **Animated texture**: Tạo texture chuyển động cho material
3. **Particle trail**: Thêm particle system theo line

### Material nâng cao:
```
Shader: Universal Render Pipeline/Particles/Unlit
Render Face: Both
Blend Mode: Additive
Color: Màu sáng (cho hiệu ứng glow)
```

## Troubleshooting
- **Không thấy line**: Kiểm tra Sorting Layer và Order in Layer
- **Line bị giật**: Tăng Corner Vertices lên 10-15
- **Line không smooth**: Kiểm tra Position Count và Line Width

