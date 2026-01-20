# HƯỚNG DẪN SETUP NARRATOR SYSTEM

## Bước 1: Tạo Canvas UI

1. **Tạo Canvas**:
   - Right-click Hierarchy → UI → Canvas
   - Set Canvas Scaler:
     - UI Scale Mode: Scale With Screen Size
     - Reference Resolution: 1920x1080
     - Match: 0.5 (Width/Height)

## Bước 2: Tạo Narrator Panel

1. **Tạo Panel**:
   - Right-click Canvas → UI → Panel
   - Rename: "NarratorPanel"
   - Anchor: Bottom (Alt + Shift + click bottom anchor preset)
   - Height: ~200-300 pixels
   
2. **Styling Panel** (Optional):
   - Add semi-transparent background (Image component)
   - Color: Black with Alpha ~0.7
   - Add border/frame sprite nếu muốn

## Bước 3: Thêm TextMeshPro Text

1. **Tạo TMP Text**:
   - Right-click NarratorPanel → UI → Text - TextMeshPro
   - Rename: "NarratorText"
   - Anchor: Stretch all (Alt + Shift + click stretch preset)
   - Margins: Left: 50, Right: 50, Top: 30, Bottom: 30

2. **Configure Text Settings**:
   - Font: Chọn font phù hợp
   - Font Size: 36-48
   - Alignment: Center/Left tùy ý
   - Color: White
   - Enable Word Wrapping
   - Overflow Mode: Overflow (cho TextAnimator hoạt động)

## Bước 4: Thêm TextAnimator Components

1. **Add TextAnimator**:
   - Select NarratorText GameObject
   - Add Component → Febucci → TextAnimator → **TextAnimator**
   
2. **Add TextAnimatorPlayer**:
   - Select NarratorText GameObject (same object)
   - Add Component → Febucci → TextAnimator → **TextAnimatorPlayer**

## Bước 5: Thêm NarratorController

1. **Add Script**:
   - Select NarratorText GameObject
   - Add Component → **NarratorController**

2. **Configure trong Inspector**:
   ```
   Components (Auto-assigned if on same GameObject):
   ├─ Text Animator Player: [Auto]
   ├─ Text Animator: [Auto]
   └─ Tmp Text: [Auto]
   
   Appearance Settings:
   ├─ Appearance Effect: Typewriter (chọn effect từ dropdown)
   ├─ Typewriter Speed: 30 (characters/second)
   ├─ Skip With Input: ✓ (click/tap để skip)
   ├─ Auto Hide After Complete: ☐ (tùy chọn)
   └─ Auto Hide Delay: 2.0
   
   Events:
   ├─ On Text Start: (events khi bắt đầu)
   └─ On Text Complete: (events khi hoàn thành)
   ```

## Bước 6: Configure TextAnimatorPlayer Settings

Select NarratorText → TextAnimatorPlayer component:
```
Settings:
├─ Typewriter Player Mode: ShowVisibleCharacters (default)
├─ Wait For NaN: ☐
├─ Wait For: 0.05-0.1 (delay giữa các ký tự)
├─ Use Typewriter Sounds: ✓ (optional)
└─ Events: OnTypewriterStart, OnTextShown
```

## Bước 7: Sử dụng trong Code

```csharp
using UnityEngine;

public class GameController : MonoBehaviour
{
    [SerializeField] private NarratorController narrator;
    
    void Start()
    {
        // Hiển thị text đơn
        narrator.ShowText("Chào mừng đến với game!");
        
        // Hiển thị nhiều dòng tuần tự
        string[] dialogue = new string[]
        {
            "Đây là level đầu tiên...",
            "Hãy kết nối các điểm để tạo hình...",
            "Chúc may mắn!"
        };
        narrator.ShowTextSequence(dialogue, delayBetweenLines: 1.5f);
    }
    
    public void OnButtonClick()
    {
        // Show custom text
        narrator.ShowText("Bạn đã click nút!");
    }
}
```

## Các Hiệu Ứng Có Sẵn (Appearance Effect Enum)

1. **None**: Hiện luôn toàn bộ text
2. **Typewriter**: Đánh máy cơ bản (mặc định)
3. **Fade**: Fade từ trong suốt → đậm
4. **Vertical**: Từ trên/dưới bay vào
5. **Horizontal**: Từ trái/phải trượt vào
6. **Size**: Scale từ nhỏ → lớn
7. **Offset**: Random offset xuất hiện
8. **Rotation**: Xoay vào vị trí
9. **Wave**: Hiệu ứng sóng
10. **Shake**: Rung lắc

## Tips & Tricks

### Custom Tags trong Text
Bạn có thể dùng rich text tags:
```csharp
narrator.ShowText("Đây là <color=red>text màu đỏ</color>!");
narrator.ShowText("Text <b>đậm</b> và <i>nghiêng</i>");
```

### Skip Typewriter
```csharp
// Cho phép người chơi click để skip
narrator.skipWithInput = true; // trong Inspector

// Hoặc code
narrator.SkipToEnd();
```

### Change Speed Dynamically
```csharp
narrator.SetTypewriterSpeed(60f); // Nhanh hơn
narrator.SetTypewriterSpeed(15f); // Chậm hơn
```

### Events
```csharp
narrator.OnTextStart.AddListener(() => {
    Debug.Log("Bắt đầu hiển thị text!");
});

narrator.OnTextComplete.AddListener(() => {
    Debug.Log("Text đã hiển thị xong!");
});
```

## Troubleshooting

**Q: Text không hiển thị typewriter?**
A: Kiểm tra:
- TextAnimatorPlayer component đã được add
- Overflow Mode = Overflow trong TMP Text
- Wait For value không quá nhỏ

**Q: Click không skip được?**
A: Đảm bảo:
- skipWithInput = true
- Canvas có GraphicRaycaster component
- Không có object khác chặn input

**Q: Effect không hoạt động?**
A: Kiểm tra:
- TextAnimator component đã được add
- Built-in Appearances Database được assign (thường auto)
- Tag effect đúng format (ví dụ: `<fade>text</fade>`)

## Hierarchy Cuối Cùng
```
Canvas
└─ NarratorPanel (Panel/Image)
   └─ NarratorText (TextMeshPro)
      ├─ TextAnimator
      ├─ TextAnimatorPlayer
      └─ NarratorController
```

---
**Hoàn thành!** Bây giờ bạn có thể sử dụng NarratorController để hiển thị dialogue với nhiều hiệu ứng khác nhau.
