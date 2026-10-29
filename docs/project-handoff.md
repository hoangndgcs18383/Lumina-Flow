# LUMINA FLOW: NEON LASER PUZZLE - PROJECT HANDOFF DOCUMENT

## 1. TỔNG QUAN DỰ ÁN (PROJECT OVERVIEW)
* **Tên dự án:** Lumina Flow
* **Thể loại:** 2D Puzzle / Laser Reflection Game
* **Nền tảng mục tiêu:** Mobile (iOS/Android)
* **Phong cách đồ họa (Art Style):** 2D Stylized Neon / Cyberpunk / Synthwave.
* **Core Loop:** Người chơi xoay các khối quang học (Gương, Lăng kính) để điều hướng tia laser từ nguồn (Emitter) xuyên qua các Pha lê (Crystal) và chạm tới Lõi mục tiêu (Target Core) để qua màn.

## 2. TECH STACK & DEPENDENCIES
* **Engine:** Unity (phiên bản 2021.3 LTS hoặc mới hơn).
* **Render Pipeline:** URP (Universal Render Pipeline) - Sử dụng Post-Processing Bloom để làm sáng tia laser (Neon Glow).
* **Vật lý:** 2D Physics (`Physics2D.RaycastNonAlloc` để tối ưu FPS).
* **Plugins bắt buộc:**
  * **DOTween (Demigiant):** Xử lý mượt mà toàn bộ animation (xoay gương, bật pop-up UI, thu phóng).

## 3. CƠ CHẾ GAMEPLAY (GAMEPLAY MECHANICS)
Hệ thống quản lý vật thể trên Grid (Bàn cờ) bao gồm các khối (Pieces) kế thừa từ lớp cơ sở `InteractablePiece`:
1. **Emitter:** Phát ra tia laser ban đầu.
2. **Mirror:** Gương phản xạ, bẻ tia laser góc 90 độ.
3. **Splitter:** Lăng kính, vừa cho tia laser đi thẳng, vừa tách thêm 1 tia góc 90 độ.
4. **Portal:** Cổng dịch chuyển (Wormhole). Tia laser đi vào cổng A sẽ chui ra ở cổng B giữ nguyên hướng.
5. **Target Core:** Lõi mục tiêu. Phải kích hoạt toàn bộ lõi mới tính là qua màn (Win Condition). Có kiểm tra trùng khớp màu sắc (Color Match).
6. **Crystal:** Pha lê. Tia laser đâm xuyên qua. Dùng để tính hệ thống 3 Sao (Stars System).
7. **Wall:** Tường chắn vật lý.
8. **Color Filter:** Kính lọc màu. Đi xuyên qua nhưng đổi màu tia laser (`LaserRayData.color`).
9. **Sensor & Door:** Cửa cơ học mở bằng cảm biến quang học.

## 4. KIẾN TRÚC HỆ THỐNG ĐÃ HOÀN THIỆN (COMPLETED SYSTEMS)
* **Laser Engine (`LaserManager`):** Sử dụng thuật toán Duyệt theo chiều rộng (BFS) với `Queue<LaserRayData>` để vẽ nhánh tia sáng. Dùng `Physics2D.RaycastNonAlloc` để không xả rác bộ nhớ (Zero GC Alloc).
* **Level Data (`LevelDataSO` & `LevelSpawner`):** Sử dụng `ScriptableObject` lưu trữ tọa độ lưới. Hệ thống tự động spawn Prefabs dựa trên ID màn chơi.
* **Custom Editor (`LevelEditorWindow`):** Tool nội bộ trong Unity Editor để kéo thả, vẽ Grid, xoay hướng và lưu Level thành file Asset.
* **Save/Load (`SaveManager` & `CryptoUtility`):** Quản lý tiến trình (Số sao, Level mở khóa). Lưu dưới dạng JSON và **Mã hóa AES-256** chống cheat.
* **UGC / Share Code (`LevelSerializer`):** Serialize `LevelDataSO` thành chuỗi Base64 String để người chơi copy/paste chia sẻ bản đồ cho nhau không cần Server.
* **Audio & Synesthesia (`AudioManager`):** Phát nhạc nền, hiệu ứng. Tích hợp âm thanh nốt ngũ cung (Pentatonic) leo thang theo số lần phản xạ của tia laser.
* **Haptics (`HapticManager`):** Rung phản hồi gốc (Native Haptic) nhẹ khi xoay khối và rung báo hiệu khi qua màn.
* **Undo System (`MoveManager`):** Sử dụng Command Pattern với `Stack<PlayerMove>` để lùi bước.
* **Skins System (`SkinShopUI`):** Mua và đổi màu (Material/Color) tia laser dựa trên số sao tích lũy.

## 5. CẤU TRÚC THƯ MỤC CỐT LÕI (CORE DIRECTORY STRUCTURE)
```text
Assets/
├── Scripts/
│   ├── Core/
│   │   ├── GameManager.cs, LevelSpawner.cs, LevelSerializer.cs
│   │   ├── SaveManager.cs, CryptoUtility.cs, HapticManager.cs
│   ├── Gameplay/
│   │   ├── LaserManager.cs, InteractablePiece.cs, MoveManager.cs
│   │   ├── Mirror, Splitter, PortalPiece, TargetCore, CrystalPiece
│   │   ├── ColorFilterPiece.cs, SensorPiece.cs, LaserDoor.cs
│   ├── Data/
│   │   ├── LevelDataSO.cs, SaveData.cs, LaserSkinSO.cs
│   ├── Audio/
│   │   ├── AudioManager.cs
│   ├── UI/
│   │   ├── UIManager.cs, LevelSelectionMenu.cs, LevelButton.cs
│   │   ├── LevelShareUI.cs, SkinShopUI.cs
│   ├── Visuals/
│       ├── VFXManager.cs
├── Editor/
│   ├── LevelEditorWindow.cs
```

## 6. INSTRUCTIONS FOR THE NEXT AI / DEVELOPER
* **Ngữ cảnh (Context):** Dự án này đã hoàn thành toàn bộ hệ thống lõi (Core Framework). Các script được viết theo chuẩn Clean Code, Singleton Manager và Observer/Event (một phần).
* **Nhiệm vụ tiềm năng tiếp theo (Next Steps):**
  1. **Level Design:** Tạo từ 20-50 màn chơi bằng Custom Editor Tool dựa trên các nguyên tắc thiết kế Puzzle (Kishōtenketsu, Sawtooth curve).
  2. **Monetization (Kiếm tiền):** Tích hợp SDK Quảng cáo (Unity Ads / AdMob) cho tính năng "Xem quảng cáo lấy Gợi ý (Hint)" hoặc "Nhân đôi số Sao".
  3. **Analytics:** Tích hợp Firebase Analytics để đo lường xem người chơi bị kẹt (Drop off) nhiều nhất ở Level nào để điều chỉnh độ khó.
  4. **Performance Profiling:** Build thử nghiệm lên thiết bị Android/iOS thật, dùng Unity Profiler kiểm tra Overdraw ở các tia LineRenderer và hạt Particle.