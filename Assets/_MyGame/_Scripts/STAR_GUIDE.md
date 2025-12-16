# Star Component - Hướng dẫn sử dụng với DOTween

## 🌟 Tổng quan

Class `Star` quản lý các trạng thái và hành vi visual của các điểm sao trong game nối điểm. **Đã tích hợp DOTween** cho animations mượt mà và chuyên nghiệp.

---

## 🎯 3 Trạng thái (StarState)

| State | Màu sắc | Scale | Animation |
|-------|---------|-------|-----------|
| **Idle** | White (trắng) | 1.0x | None |
| **Hover** | Yellow (vàng) | 1.2x | Pulse (đập nhẹ) |
| **Connected** | Green (xanh) | 1.1x | None |

---

## 🛠️ Setup trong Unity

### Bước 1: Tạo GameObject Star

```
Star GameObject
├─ SpriteRenderer (hình ngôi sao)
├─ CircleCollider2D (để detect input)
├─ Star Component (script này)
└─ Tag: "Dot"
```

### Bước 2: Configure trong Inspector

#### Visual Settings
- **Sprite Renderer**: Kéo SpriteRenderer component vào (hoặc để trống để auto-find)
- **Visual Transform**: Transform để scale (để trống = transform gốc)

#### State Colors
```
Idle Color:      #FFFFFF (White)
Hover Color:     #FFFF00 (Yellow)  
Connected Color: #00FF00 (Green)
```

#### Scale Settings
```
Idle Scale:      1.0
Hover Scale:     1.2
Connected Scale: 1.1
```

#### Animation Settings (DOTween)
```
Scale Duration:  0.2s  (thời gian scale animation)
Color Duration:  0.2s  (thời gian color animation)
Scale Ease:      OutBack (bounce effect)
Color Ease:      OutQuad (smooth)
```

#### Pulse Animation (Hover)
```
Use Pulse Animation: ✓ (bật)
Pulse Duration:      0.5s
Pulse Scale:         0.1 (mức độ phóng to thêm)
```

---

## 💻 API Reference

### Properties

```csharp
StarState CurrentState { get; }  // Trạng thái hiện tại
int StarID { get; set; }        // ID để validate thứ tự
bool IsConnected { get; }       // Đã connected chưa
```

### Methods - State Control

```csharp
void SetState(StarState newState)  // Đặt trạng thái trực tiếp
void OnHoverEnter()                // Chuyển sang Hover (nếu chưa Connected)
void OnHoverExit()                 // Về Idle (nếu đang Hover)
void OnConnected()                 // Đánh dấu Connected
void ResetState()                  // Reset về Idle
```

### Methods - Customization

```csharp
// Đặt màu cho từng state
void SetStateColors(Color idle, Color hover, Color connected)

// Đặt scale cho từng state  
void SetStateScales(float idle, float hover, float connected)

// Đặt animation settings
void SetAnimationSettings(float scaleDur, float colorDur, Ease scaleEasing, Ease colorEasing)

// Đặt pulse settings
void SetPulseSettings(bool enable, float duration, float scale)
```

---

## 🎮 Cách sử dụng

### 1. Đã tích hợp trong GamePlayController

Star component **đã được tích hợp hoàn toàn** với GamePlayController:

✅ **Tap vào sao** → Toggle Hover/Idle  
✅ **Hold & Drag bắt đầu** → Sao đầu tiên Connected  
✅ **Drag qua sao** → Hover → Connected  
✅ **Kết thúc drag** → Validate và giữ/reset state  

**Bạn không cần code thêm gì!** Chỉ cần:
1. Gắn Star component vào GameObject sao
2. Setup trong Inspector
3. Chạy game và test!

### 2. Sử dụng từ code khác (optional)

```csharp
// Lấy reference
Star star = GetComponent<Star>();

// Thay đổi state
star.OnHoverEnter();     // Scale lên 1.2x, màu vàng, đập nhẹ
star.OnConnected();      // Scale 1.1x, màu xanh
star.ResetState();       // Về idle

// Kiểm tra state
if (star.CurrentState == StarState.Connected)
{
    Debug.Log("Sao này đã được nối!");
}

// Customize colors
star.SetStateColors(
    idle: Color.white,
    hover: Color.cyan,
    connected: new Color(1f, 0.5f, 0f) // Orange
);

// Customize animation speed
star.SetAnimationSettings(
    scaleDur: 0.15f,        // Nhanh hơn
    colorDur: 0.15f,
    scaleEasing: Ease.OutElastic,  // Springy
    colorEasing: Ease.OutQuad
);
```

---

## 🎨 DOTween Easing Options

### Scale Ease (Khuyến nghị)

| Ease Type | Hiệu ứng | Dùng cho |
|-----------|----------|----------|
| **OutBack** ⭐ | Bounce ra ngoài | Scale up (hover) |
| **OutElastic** | Springy, rung lắc | Hiệu ứng vui nhộn |
| **OutBounce** | Nhiều bounce | Cartoonish |
| **OutQuad** | Smooth, chậm dần | Subtle animation |
| **Linear** | Tốc độ đều | Mechanical |

### Color Ease (Khuyến nghị)

| Ease Type | Hiệu ứng | Dùng cho |
|-----------|----------|----------|
| **OutQuad** ⭐ | Smooth chậm dần | Đổi màu tự nhiên |
| **Linear** | Tốc độ đều | Đơn giản |
| **InOutSine** | Smooth in-out | Mượt mà nhất |

---

## 🎬 Animation Flow

### Flow 1: TAP
```
[Idle] → TAP → [Hover] (scale 1.2x, màu vàng, pulse)
       → TAP again → [Idle] (scale 1.0x, màu trắng)
```

### Flow 2: DRAG nối sao
```
Start Drag trên Sao A:
[Idle] → [Connected] (scale 1.1x, màu xanh)

Drag qua Sao B:
[Idle] → [Hover] (vàng, pulse) → [Connected] (xanh)

Drag qua Sao C:
[Idle] → [Hover] → [Connected]

End Drag:
✅ Valid   → Tất cả giữ [Connected]
❌ Invalid → Tất cả về [Idle]
```

---

## ⚙️ Tùy chỉnh

### Làm animation "Juicy" (game mobile fun)

```
Scale Duration:  0.15s (nhanh)
Scale Ease:      OutElastic (springy)
Hover Scale:     1.3 (phóng to nhiều)
Pulse Scale:     0.15 (đập mạnh)
```

### Làm animation "Subtle" (game tĩnh lặng)

```
Scale Duration:  0.3s (chậm)
Scale Ease:      OutQuad (smooth)
Hover Scale:     1.1 (phóng ít)
Pulse Scale:     0.05 (đập nhẹ)
Use Pulse:       ❌ (tắt)
```

### Làm animation "Snappy" (game nhịp nhanh)

```
Scale Duration:  0.1s (rất nhanh)
Color Duration:  0.1s
Scale Ease:      OutBack
Pulse Duration:  0.3s (đập nhanh)
```

---

## 🔧 Validate thứ tự nối sao

Sử dụng `StarID` để validate:

```csharp
// Setup IDs cho các sao (trong Start của manager)
Star[] stars = FindObjectsOfType<Star>();
for (int i = 0; i < stars.Length; i++)
{
    stars[i].StarID = i; // ID từ 0 đến n-1
}

// Validate trong GamePlayController
private bool ValidateConnection(List<Collider2D> dots)
{
    for (int i = 0; i < dots.Count - 1; i++)
    {
        Star current = dots[i].GetComponent<Star>();
        Star next = dots[i + 1].GetComponent<Star>();
        
        // Kiểm tra nối tuần tự
        if (current.StarID + 1 != next.StarID)
        {
            Debug.Log($"Sai thứ tự! {current.StarID} → {next.StarID}");
            return false;
        }
    }
    
    return dots.Count >= 2;
}
```

---

## 🐛 Debug & Testing

### Scene View Debug
Chọn Star GameObject → Scene view hiện Gizmo màu:
- **Trắng**: Idle
- **Vàng**: Hover
- **Xanh**: Connected

### Console Logs (từ GamePlayController)
```
[TAP] Detected Dot: Star_1
[HOLD START] Starting from Dot: Star_1
[HOLD UPDATE] Connected to new Dot: Star_2
[HOLD UPDATE] Connected to new Dot: Star_3
[HOLD END] Finished connecting. Total dots connected: 3
[FINISH CONNECTING] Connected 3 dots:
  1. Star_1
  2. Star_2
  3. Star_3
[SUCCESS] Valid connection!
```

### Test Checklist
- [ ] Hover vào sao → Màu vàng, scale 1.2x, đập nhẹ
- [ ] Hover ra → Về trắng, scale 1.0x, dừng đập
- [ ] Drag nối sao → Sao chuyển xanh, scale 1.1x
- [ ] Nối đúng → Giữ màu xanh
- [ ] Nối sai → Reset về trắng
- [ ] Không có lỗi DOTween trong Console

---

## ⚠️ Lưu ý quan trọng

### 1. DOTween phải được cài đặt
✅ Bạn đã cài rồi! File sử dụng `using DG.Tweening`

### 2. Cleanup tự động
Star component tự động kill tất cả tweens trong `OnDestroy()` để tránh memory leak.

### 3. SetUpdate(true)
Tất cả tweens dùng `.SetUpdate(true)` → Hoạt động ngay cả khi `Time.timeScale = 0` (pause game).

### 4. Không dùng Update()
Star **KHÔNG CÓ** Update() loop → Performance tốt hơn so với Lerp thủ công.

---

## 📋 Setup Checklist

- [ ] GameObject có **SpriteRenderer** với sprite sao
- [ ] GameObject có **Collider2D** (CircleCollider2D)
- [ ] GameObject có **Tag = "Dot"**
- [ ] Gắn **Star component**
- [ ] Assign **Sprite Renderer** (hoặc để auto-find)
- [ ] Cấu hình **màu sắc** 3 states
- [ ] Cấu hình **scale** 3 states
- [ ] Cấu hình **animation settings**
- [ ] Test trong Play mode ✓

---

## 💡 Performance Tips

1. **Sử dụng Object Pooling** nếu spawn/despawn nhiều sao
2. **Giảm Pulse Duration** nếu có quá nhiều sao hover cùng lúc
3. **Tắt Pulse Animation** nếu FPS thấp
4. **Sử dụng Sprite Atlas** để giảm draw calls

---

## 🎉 Kết luận

Star component với DOTween đã sẵn sàng sử dụng! 

**Ưu điểm:**
✅ Animations mượt mà chuyên nghiệp  
✅ Dễ customize qua Inspector  
✅ Performance tốt (không dùng Update)  
✅ Tích hợp sẵn với GamePlayController  
✅ Nhiều Easing options  
✅ Auto cleanup, no memory leak  

**Chỉ cần:**
1. Gắn vào GameObject sao
2. Setup màu & scale
3. Play! 🚀

---

Chúc bạn tạo được game nối điểm tuyệt vời! 🌟✨

