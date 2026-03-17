# PlayFab Setup cho Matchmaking & ELO

## 1. Tạo PlayFab Title
- Vào https://developer.playfab.com/
- Tạo Title mới
- Copy **Title ID** vào `PlayFabConfig` (Assets/_Data/Config/PlayFabConfig.asset)

## 2. Tạo Player Statistics
Vào **PlayFab Game Manager** > Title > **Players** > **Statistics**:
- Thêm: `elo`, `wins`, `losses`, `totalGames` (kiểu Integer)

## 3. GameServices trong MenuScene
- Tạo GameObject "GameServices", add component `GameServices`
- Gán PlayFabConfig (Assets/_Data/Config/PlayFabConfig.asset) vào Inspector

## 4. Leaderboard UI – Hướng dẫn chi tiết

### Cấu trúc Hierarchy (con của Canvas)
```
Canvas
└── LeaderboardPanel          ← GameObject gắn script LeaderboardPanel
    ├── PanelRoot             ← GameObject (hoặc Image) làm nền/container
    │   ├── Title             ← TMP_Text: "Bảng xếp hạng"
    │   ├── LoadingText       ← TMP_Text: hiển thị "Đang tải..."
    │   ├── ErrorText         ← TMP_Text: hiển thị lỗi (ẩn mặc định)
    │   ├── ContentParent     ← GameObject có VerticalLayoutGroup (ScrollView > Viewport > Content)
    │   └── CloseButton       ← Button (Text bên trong: "Đóng")
    └── RowPrefab             ← TMP_Text dùng làm mẫu cho mỗi dòng (có thể để inactive)
```

### Từng bước tạo UI

1. **LeaderboardPanel** (GameObject con của Canvas)  
   - Chuột phải Canvas → UI → Panel (hoặc tạo Empty GameObject)  
   - Đổi tên thành `LeaderboardPanel`  
   - Add component **LeaderboardPanel** (script)

2. **PanelRoot** (con của LeaderboardPanel)  
   - Chuột phải LeaderboardPanel → UI → Panel  
   - Đặt tên `PanelRoot`  
   - Có thể thêm **Image** làm nền (hoặc giữ Panel mặc định)

3. **LoadingText** (con của PanelRoot)  
   - Chuột phải PanelRoot → UI → Text - TextMeshPro  
   - Đặt tên `LoadingText`, text mặc định: `Đang tải...`

4. **ErrorText** (con của PanelRoot)  
   - Chuột phải PanelRoot → UI → Text - TextMeshPro  
   - Đặt tên `ErrorText`, text: `Lỗi`  
   - Màu đỏ, **GameObject inactive** (hoặc ẩn bằng code)

5. **ContentParent** – ScrollView cho danh sách  
   - Chuột phải PanelRoot → UI → Scroll View  
   - Đổi tên ScrollView thành `ScrollView`  
   - Vào ScrollView → Viewport → **Content**  
   - Add component **Vertical Layout Group** vào Content  
   - Add **Content Size Fitter** (Vertical Fit: Preferred Size)  
   - Đặt tên GameObject Content là `ContentParent`

6. **CloseButton** (con của PanelRoot)  
   - Chuột phải PanelRoot → UI → Button - TextMeshPro  
   - Đặt tên `CloseButton`, text con: `Đóng`

7. **RowPrefab** (con của LeaderboardPanel hoặc PanelRoot)  
   - Chuột phải PanelRoot → UI → Text - TextMeshPro  
   - Đặt tên `RowPrefab`, text mẫu: `#1  PlayerName  ELO 1000`  
   - **Set inactive** (chỉ dùng làm prefab clone)

### Gán references trong Inspector (script LeaderboardPanel)

| Ô trong Inspector       | Kéo thả GameObject nào         |
|-------------------------|---------------------------------|
| Panel Root              | `PanelRoot`                     |
| Content Parent          | `ContentParent` (trong ScrollView) |
| Loading Text            | `LoadingText` (TMP_Text)        |
| Error Text              | `ErrorText` (TMP_Text)          |
| Close Button            | `CloseButton` (Button)          |
| Row Prefab              | `RowPrefab` (TMP_Text)          |

### Kích hoạt Leaderboard

- Tạo nút "Ranking" trong Menu  
- Trong **Button > OnClick**:
  - Kéo GameObject **LeaderboardPanel** vào ô Runtime Only
  - Chọn function: **LeaderboardPanel > Show()**
