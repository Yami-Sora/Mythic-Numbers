# Refactoring SOLID – Ghi chú

## Tóm tắt thay đổi

Đã refactor một phần codebase theo hướng SOLID, **không ảnh hưởng** hành vi game trong Unity.

---

## 1. Open/Closed Principle (OCP) – RuleStrategyFactory

**File mới:** `Rule/RuleStrategyFactory.cs`

- Trước: `GameManagerNet.UpdateRuleStrategy()` dùng chuỗi if/else dựa trên `CurrentRuleIndex` (0–3).
- Sau: Factory tạo `IRuleSet` theo index; thêm luật mới chỉ cần sửa factory, không cần sửa `GameManagerNet`.

**Cách thêm luật mới:**
1. Tạo class kế thừa `BaseRule` hoặc `RuleDecorator`.
2. Cập nhật `RuleStrategyFactory.Create()` để map thêm index mới.

---

## 2. Single Responsibility Principle (SRP) – CardDealer

**File mới:** `GameManager/CardDealer.cs`

- Trước: `GameManagerNet` vừa quản lý board/turn vừa chia bài (DealCards, SpawnCard).
- Sau: `CardDealer` chịu trách nhiệm spawn và chia bài. `GameManagerNet` ủy quyền qua `_cardDealer.DealCards()`.

---

## 3. Unity Inspector – Không cần thay đổi

**Các liên kết và cấu hình hiện tại vẫn đúng:**

| Component | Vị trí | Trạng thái |
|-----------|--------|------------|
| **NetworkAppManager** | GameObject trong PlayCardScene | Không đổi |
| └ gameManagerNetPrefab | Inspector | Không đổi |
| └ gameRefereeNetPrefab | Inspector | Không đổi |
| └ slots[], leftHandPos, rightHandPos, turnText | Inspector | Không đổi |
| └ resultPanel, resultText | Inspector | Không đổi |
| **GameManagerNet** (prefab) | Prefab | Không đổi |
| └ cardPrefab | Inspector | Không đổi |
| **SlotClick** | Mỗi Slot GameObject | Không đổi |
| └ slotIndex | Inspector | Không đổi |
| **CanvasManager** | Canvas | Không đổi |
| **GameUIManager** | Canvas | Không đổi |
| **CardDatabase** | Scene | Không đổi |

**Không cần gán lại hoặc thêm script mới** – `RuleStrategyFactory` và `CardDealer` là class C# thông thường, không phải `MonoBehaviour`.

---

## 4. Các thay đổi có thể làm thêm (chưa thực hiện)

- **Dependency Inversion:** Dùng interface thay cho singleton; đòi hỏi DI container hoặc ServiceLocator.
- **ReconnectHandler:** Tách logic reconnect khỏi `GameRefereeNet`; cần điều chỉnh vì phụ thuộc RPC, coroutine, `NetworkBehaviour`.
- **IBoardState:** Interface cho truy cập board; ảnh hưởng nhiều file (`IRuleSet`, `BaseRule`, `CardNet`, …).
