# Hướng dẫn sử dụng Input System cho Game Nối Điểm

## 📝 Tổng quan

Hệ thống input được thiết kế để phân biệt 2 loại thao tác:
1. **TAP** - Ấn nhanh rồi nhấc tay (< 0.2s)
2. **HOLD/DRAG** - Ấn giữ hoặc ấn giữ rồi vuốt (>= 0.2s hoặc kéo >= 10 pixels)

Hệ thống sử dụng `Physics2D.OverlapPoint` để detect các điểm - nhẹ và hiệu quả.

---

## 🎯 Cấu hình

### 1. Chuẩn bị các điểm (Dots)

Mỗi điểm cần có:
- **Collider2D** (CircleCollider2D hoặc BoxCollider2D)
- **Tag = "Dot"** (hoặc tag tùy chỉnh)
- **Layer** (optional - có thể để default hoặc tạo layer riêng)

```
GameObject (Dot)
├─ CircleCollider2D
├─ Tag: "Dot"
└─ Layer: Default (hoặc tạo layer "Dot")
```

### 2. Setup InputManager trong Scene

1. Tạo Empty GameObject tên "InputManager"
2. Attach script `InputManager`
3. Cấu hình trong Inspector:

```
[Input Settings]
- Hold Threshold: 0.2 (thời gian để phân biệt tap/hold)
- Drag Threshold: 10 (khoảng cách pixel để coi là drag)

[Detection Settings]
- Dot Tag: "Dot" (tag của các điểm)
- Dot Layer: Everything (hoặc chọn layer cụ thể)
```

### 3. Setup GamePlayController

1. Tạo Empty GameObject tên "GamePlayController"
2. Attach script `GamePlayController`
3. Assign references trong Inspector:

```
- Input Manager: Kéo GameObject InputManager vào
- Main Camera: Kéo Main Camera vào (hoặc để trống để tự động tìm)
```

---

## 🔧 API Reference

### InputManager

#### Detection Methods

```csharp
// Detect 1 điểm tại vị trí screen
Collider2D DetectDotAtScreenPosition(Vector2 screenPosition, Camera camera = null)

// Detect 1 điểm tại vị trí world
Collider2D DetectDotAtWorldPosition(Vector2 worldPosition)

// Detect tất cả điểm tại vị trí screen (nếu overlap)
Collider2D[] DetectAllDotsAtScreenPosition(Vector2 screenPosition, Camera camera = null)

// Detect tất cả điểm tại vị trí world
Collider2D[] DetectAllDotsAtWorldPosition(Vector2 worldPosition)
```

#### Utility Methods

```csharp
// Chuyển screen position sang world position 3D
Vector3 GetWorldPosition(Vector2 screenPosition, Camera camera = null)

// Chuyển screen position sang world position 2D
Vector2 GetWorldPosition2D(Vector2 screenPosition, Camera camera = null)
```

#### Properties

```csharp
bool IsPressed      // Đang ấn hay không
bool IsHolding      // Đang hold hay không
Vector2 CurrentPosition  // Vị trí hiện tại (screen space)
string DotTag       // Tag của các điểm
LayerMask DotLayer  // Layer mask để detect
```

#### Events

```csharp
UnityEvent<Vector2> OnTap         // Khi tap nhanh
UnityEvent<Vector2> OnHoldStart   // Khi bắt đầu hold
UnityEvent<Vector2> OnHoldUpdate  // Khi đang hold/drag
UnityEvent<Vector2> OnHoldEnd     // Khi kết thúc hold
```

---

## 💡 Ví dụ sử dụng

### Cách 1: Qua UnityEvent trong Inspector

Tạo public method trong script của bạn:

```csharp
public void OnTapDetected(Vector2 screenPos)
{
    Debug.Log($"Tapped at: {screenPos}");
}
```

Sau đó trong Inspector của InputManager:
- Mở `On Tap` event
- Click `+` để thêm listener
- Kéo GameObject chứa script vào
- Chọn method `OnTapDetected`

### Cách 2: Qua Code (như GamePlayController)

```csharp
private void Start()
{
    inputManager.OnTap.AddListener(HandleTap);
    inputManager.OnHoldStart.AddListener(HandleHoldStart);
    inputManager.OnHoldUpdate.AddListener(HandleHoldUpdate);
    inputManager.OnHoldEnd.AddListener(HandleHoldEnd);
}

private void OnDestroy()
{
    inputManager.OnTap.RemoveListener(HandleTap);
    inputManager.OnHoldStart.RemoveListener(HandleHoldStart);
    inputManager.OnHoldUpdate.RemoveListener(HandleHoldUpdate);
    inputManager.OnHoldEnd.RemoveListener(HandleHoldEnd);
}

private void HandleTap(Vector2 screenPosition)
{
    Collider2D dot = inputManager.DetectDotAtScreenPosition(screenPosition);
    if (dot != null)
    {
        Debug.Log($"Tapped on: {dot.name}");
    }
}
```

---

## 🎮 Flow của Game Nối Điểm

### Flow TAP (Chọn điểm đơn lẻ)
```
1. User tap vào màn hình
2. OnTap event được gọi
3. Detect điểm tại vị trí tap
4. Nếu có điểm: Highlight/Select điểm
```

### Flow HOLD/DRAG (Nối các điểm)
```
1. User bắt đầu hold trên điểm
2. OnHoldStart được gọi
   → Detect điểm đầu tiên
   → Thêm vào danh sách connectedDots
   → Tạo LineRenderer

3. User kéo ngón tay qua các điểm
4. OnHoldUpdate được gọi liên tục
   → Detect điểm tại vị trí hiện tại
   → Nếu là điểm mới: thêm vào connectedDots
   → Cập nhật LineRenderer
   → Highlight điểm đang hover

5. User nhấc tay lên
6. OnHoldEnd được gọi
   → Kiểm tra danh sách connectedDots
   → Validate xem có đúng thứ tự không
   → Tính điểm hoặc reject
   → Xóa LineRenderer
```

---

## 🛠️ Customize

### Thay đổi tag hoặc layer

Trong Inspector của InputManager:
- **Dot Tag**: Đổi thành tag của bạn (ví dụ: "ConnectPoint")
- **Dot Layer**: Chọn layer riêng để tối ưu performance

### Điều chỉnh sensitivity

- **Hold Threshold**: 
  - Giảm (0.1s) → Nhạy hơn, dễ trigger hold
  - Tăng (0.3s) → Phải giữ lâu hơn mới trigger hold

- **Drag Threshold**:
  - Giảm (5px) → Nhạy hơn, kéo ngắn cũng trigger hold
  - Tăng (20px) → Phải kéo xa hơn mới trigger hold

### Tối ưu performance

1. **Sử dụng Layer riêng cho dots**: Giảm số lượng collider cần check
2. **Giảm kích thước collider**: Chỉ bao quanh phần cần thiết
3. **Sử dụng CircleCollider2D**: Nhanh hơn các loại collider phức tạp

---

## 📋 TODO List trong GamePlayController

Các phần cần implement thêm:

- [ ] `OnDotTapped()` - Logic khi tap vào điểm
- [ ] `OnStartConnecting()` - Tạo LineRenderer, highlight điểm đầu
- [ ] `OnDotHoverEnter()` - Scale up/highlight khi hover
- [ ] `OnDotHoverExit()` - Scale down khi không hover
- [ ] `OnDotConnected()` - Play sound/effect khi nối điểm mới
- [ ] `OnUpdateConnecting()` - Cập nhật LineRenderer theo ngón tay
- [ ] `OnFinishConnecting()` - Validate thứ tự, tính điểm
- [ ] `OnCancelConnecting()` - Xóa LineRenderer, reset state

---

## ⚠️ Lưu ý

1. **Đảm bảo các điểm có Collider2D** - Không detect được nếu thiếu
2. **Đặt đúng tag** - Tag phải khớp với cấu hình trong InputManager
3. **Camera phải được assign** - Hoặc để trống để tự động dùng Camera.main
4. **Physics2D settings** - Đảm bảo collision matrix cho phép detect layer của dots

---

## 🐛 Debug

Mở Console window trong Unity để xem các log:

```
[TAP] Detected Dot: Dot_1
[HOLD START] Starting from Dot: Dot_1
[HOLD UPDATE] Connected to new Dot: Dot_2
[HOLD UPDATE] Connected to new Dot: Dot_3
[HOLD END] Finished connecting. Total dots connected: 3
[FINISH CONNECTING] Connected 3 dots:
  1. Dot_1
  2. Dot_2
  3. Dot_3
```

Nếu không detect được điểm:
- Kiểm tra tag của GameObject
- Kiểm tra Collider2D có enabled không
- Kiểm tra layer mask trong InputManager
- Kiểm tra camera reference

