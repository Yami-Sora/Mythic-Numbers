# BẢN THẢO KẾ HOẠCH: NUMBERS MYTHIC (PROJECT "ĐỘ KIẾP")

**Định hướng:** Game Card Battle 2D Online (Triple Triad style) x Gacha x Tu Tiên.  
**Vibe hiện tại:** Lấy Meme tấu hài làm động lực code. (Tương lai sẽ thuê artist vẽ lại art xịn xò, đổi tên skill nghiêm túc sau).

## 1. CƠ CHẾ CHỈ SỐ "ĐỘT PHÁ CẢNH GIỚI" (Infinite Stat Cap)
Để tránh UI bị "cook" do số quá to (tràn viền) khi late game, chỉ số hiển thị của bài chỉ nằm trong khoảng 1 - 99.
- **Luân Hồi Chỉ Số:** Khi stat thực tế vượt quá 99, con số trên thẻ sẽ reset về 1 nhưng **ĐỔI MÀU CẢNH GIỚI**.
- **Cấp độ màu (Cảnh Giới):** Trắng (Phàm Nhân) ➔ Xanh Lá (Trúc Cơ) ➔ Xanh (Kim Đan) ➔ Tím (Nguyên Anh) ➔ Cam (Hóa Thần) ➔ Đỏ (Đại Đế) ➔ Cầu Vồng (Thần Thoại).
- **Code Logic:** Data lưu số nguyên thực (VD: 150), UI tự động dịch ra thành Xanh Lá 50. Lúc đập nhau vẫn tính toán bằng số thực.

## 2. HỆ THỐNG THĂNG TIẾN SỨC MẠNH (Meta-Game)

### A. Hệ thống "Tinh Thạch" (Gems / Equipments)
Đây là Core hút máu... à nhầm, giữ chân player. Mỗi lá bài là một "Pháp bảo" có thể đục lỗ khảm Tinh Thạch để buff chỉ số cho 4 cạnh (Top, Right, Bottom, Left).
- Tối đa 12 lỗ Tinh Thạch trên 1 lá bài (Chia đều mỗi cạnh 3 lỗ).
- **ĐUI (Đừng Up In-game):** Trên bàn đấu thực tế KHÔNG hiển thị Tinh Thạch để tránh rác mắt, chỉ hiển thị con số đã buff và màu sắc. Tinh Thạch chỉ xem được ở màn hình Xếp Bài (Deck Builder) hoặc ấn giữ xem chi tiết (CardFocusUI).

### B. "Bát Tinh Độ Kiếp" (Up Dupes / Star System)
**Luật thép:** Nâng Sao KHÔNG BAO GIỜ buff cơ chế cốt lõi của Skill (để tránh mất cân bằng). Nâng Sao CHỈ dùng để mở khóa slot Tinh Thạch và Trần Cảnh Giới.
- **0 Sao (Mặc định):** Có sẵn 4 slot Tinh Thạch (mỗi cạnh 1 slot).
- **1 ➔ 8 Sao:** Mỗi lần up thêm 1 Sao sẽ mở thêm 1 slot Tinh Thạch (Max 12 slot ở 8 Sao).
- **Yêu cầu nâng cấp:**
  - Từ 1 ➔ 3 Sao: Cần bài rác (Fodder) cùng Rank + Vàng.
  - Từ 4 ➔ 6 Sao: Cần Bản Sao (Dupes) + Vật phẩm hiếm.
  - Từ 7 ➔ 8 Sao: Cần nhiều Dupes + Tài nguyên Đỏ cực hiếm.
*Cơ chế này giúp "giải cứu" đống bài rác player quay ra từ Gacha, biến chúng thành phôi nâng cấp hữu ích.*

## 3. CÁC CHẾ ĐỘ CHƠI (Game Modes)

### A. Chế độ chính (Core Loop)
- **Leo Thông Thiên Tháp (PvE / Roguelike):** Chế độ cốt lõi. Player xếp deck 5 lá để leo tháp. Càng lên cao AI quái càng trâu, rule càng dị (Reverse, Random...). Qua ải rớt vé Gacha, Tinh Thạch. AI không cần quá thông minh, chỉ cần lấy "Stat to đè chết người" ép player đi cày.
- **Đấu Trường Ảo Ảnh (Async PvP):** Leo Rank bằng cách xếp "Đội hình thủ nhà". Đi đánh account của player khác (do AI tự động điều khiển bài của họ). Giải quyết bài toán thiếu CCU (người chơi online cùng lúc) ở giai đoạn đầu game.

### B. Chế độ phụ (Side Content)
- **Bí Cảnh Tài Nguyên (Daily Dungeons):** Map farm Vàng, Exp, Nguyên liệu.
  - *Cực kỳ quan trọng:* Phải có nút "Càn Quét" (Sweep) để player quét ải đã 3 sao trong 1 nốt nhạc, không bị biến thành game "làm công ăn lương".
- **Đại Hội Võ Lâm (Real-time PvP):** Đánh online trực tiếp thời gian thực. Sẽ là chế độ giải trí cuối tuần, sự kiện đặc biệt, hoặc giải đấu có thưởng to (Skin bài, Khung viền).

## 4. KINH TẾ & KHỐNG CHẾ TIẾN ĐỘ (Economy & Pacing)
Để ngăn mấy anh "pháp sư Trung Hoa" cày nát game trong 2 ngày:
- **Khí Hải (Stamina / Energy):** Vào ải tốn thể lực. Level đầu tặng tràn bình để kích thích độ "nghiện", lên cao bắt đầu xiết lại để bán bình hồi thể lực.
- **Cảnh Giới Bản Thân (Account Level):** Khóa tiến độ.
  - *Ví dụ:* Acc Level 10 thì bài dù mạnh đến mấy cũng chỉ max được màu Xanh Lá 99. Muốn up lên Kim Đan phải rèn luyện Level Account.
  - Dùng Account Level để từ từ mở khóa tính năng (Slot tinh thạch thứ 5, Chợ Đen, Bang Hội...).

## 5. LỘ TRÌNH CODE CHO SOLO DEV (Action Plan)
- **Phase 1: Giao Diện Lừa Tình (UI/UX Foundation)**
  - Sửa CardNet và UI để xử lý logic "1-99 + Đổi Màu" và ẩn Tinh Thạch khi vào bàn chơi.
- **Phase 2: Túi Đồ Không Gian (Inventory & Upgrades)**
  - Code màn hình Deck Builder. Hệ thống ép Tinh Thạch (đục lỗ 1-12) và tính toán chỉ số tổng.
  - Logic hiến tế bài rác, cắn Dupes để Up Sao thẻ bài.
- **Phase 3: Chinh Phạt Tháp (PvE Core)**
  - Xây dựng luồng game Đánh Ải (Màn hình chọn Level ➔ Vào trận với AI ➔ Win rớt đồ).
- **Phase 4: Hút Máu Đại Pháp (Monetization & Gacha)**
  - Tích hợp hệ thống Stamina, Account Level.
  - Xây cái giếng Gacha để roll bài và Tinh thạch.
