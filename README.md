# Lumina Flow

Game giải đố laser neon, gồm dự án **Unity cho Android/iOS** và **bản web chơi thử**. Giao diện tiếng Việt, 24 màn, cùng một catalog màn chơi và cùng quy tắc quang học.

Kho ban đầu trống; đây là bản triển khai mới dựa trên [tài liệu bàn giao](docs/project-handoff.md), không phải bản build của các script đã có sẵn trong tài liệu.

Kế hoạch bảo trì 7 ngày và hướng dẫn cài agent GitHub Actions: [docs/maintenance-7-days.md](docs/maintenance-7-days.md). Agent mặc định chưa bật; cần API secret, ngày bắt đầu và biến opt-in trước khi chạy.

## Chơi thử bản web

Không cần cài thư viện để chạy:

```sh
node scripts/serve.cjs
```

Mở **http://localhost:4173**. Có thể dùng `npm start`, hoặc mở trực tiếp `Web/index.html` trên trình duyệt thông thường. Chạm/click gương hoặc bộ tách để xoay, dùng **Hoàn tác**, **Chơi lại**, **Gợi ý**. `Ctrl/Cmd+Z` hoàn tác, `R` chơi lại. Tiến trình lưu bằng localStorage trên trình duyệt hiện tại; dữ liệu lưu riêng với Unity.

Tạo một file HTML độc lập, không cần mạng hoặc server:

```sh
npm run package:web
```

Mở `artifacts/lumina-flow.html`. Trình duyệt có chính sách tổ chức cấm `file://` có thể dùng cách chạy localhost phía trên. Bản web không tải font, ảnh, CDN hay SDK bên ngoài.

## Mở dự án Unity

1. Cài **Unity Hub**, **Unity 2022.3 LTS** (mốc dự án: `2022.3.62f1`). Cài thêm **Android Build Support + SDK/NDK + OpenJDK**, hoặc **iOS Build Support**.
2. Trong Unity Hub chọn **Add → Add project from disk**, trỏ vào thư mục chứa `Assets`, `Packages`, `ProjectSettings` này.
3. Chờ Unity Package Manager tải **URP 14.0.11** và uGUI. Công cụ khởi tạo tự tạo URP renderer, Bloom profile và cài đặt màn hình dọc.
4. Chọn **Lumina Flow → Open playable main scene** hoặc mở `Assets/Scenes/Main.unity`, rồi nhấn **Play**.
5. Nếu cần tạo lại cấu hình render, chọn **Lumina Flow → Configure project and neon rendering**.

Scene được đưa sẵn vào Build Settings. `RuntimeBootstrap` tạo camera, bàn cờ, UI và âm thanh khi chạy; không cần tự gắn prefabs hoặc kéo các Manager vào scene.

### Android

Mở **File → Build Settings → Android → Switch Platform**. Chọn thiết bị, bật **Development Build** cho lần kiểm tra đầu, rồi **Build And Run**. Để phát hành, chọn IL2CPP/ARM64 và cấu hình signing/keystore của bạn. Tên gói mặc định là `studio.lumina.flow`; đổi trong Player Settings trước khi phát hành.

### iOS

Chuyển sang **iOS**, build Xcode project. Cần **macOS + Xcode**, chọn development team và signing phù hợp, sau đó chạy trên thiết bị. Không có IPA đã ký trong kho này.

## Cơ chế đã triển khai

- Gương phản xạ 90°, bộ tách tạo nhánh thẳng và nhánh phản xạ.
- Cổng đôi giữ nguyên hướng, kính lọc đổi màu, lõi nhận đúng màu.
- Pha lê cho tia đi xuyên, tường chặn tia, cảm biến mở cửa cùng liên kết.
- Mọi lõi phải sáng mới thắng. Một sao khi thắng, thêm một sao khi lấy hết pha lê, thêm một sao khi số lần xoay không vượt `par`.
- Hoàn tác bằng lịch sử lượt xoay, chơi lại, gợi ý dựa trên metadata lời giải, mở khóa màn và lưu số sao tốt nhất.
- Mã chia sẻ Base64 của JSON UTF-8, dùng chung giữa web và Unity. Màn nhập thêm được tách khỏi tiến trình chiến dịch.
- Unity lưu cục bộ bằng AES-256-CBC + HMAC-SHA256, có bản sao phục hồi. Khóa có trong client: đây là bảo vệ dữ liệu cục bộ, không phải hệ thống xác minh điểm chống gian lận.
- Đồ họa kết hợp nền không gian với các khối tạo bằng mã; âm thanh được tạo trong mã nguồn, không phụ thuộc asset trả phí.

## Sửa và thêm màn

Trong Unity, chọn **Lumina Flow → Level editor**. Công cụ hỗ trợ vẽ ô, chọn vật thể, xoay bằng chuột phải, xóa bằng Shift-click, xem tia sáng/lời giải, chỉnh màu/liên kết, nhập/xuất JSON hoặc mã chia sẻ. **Save catalog** kiểm tra dữ liệu trước khi lưu.

Catalog chính là `Assets/Resources/Levels/catalog.json`. Tọa độ y tăng xuống dưới; hướng 0/1/2/3 lần lượt phải/xuống/trái/lên. Gương chẵn là `/`, gương lẻ là `\`. `solution: -1` nghĩa là không có gợi ý; chỉ gương và bộ tách dùng `solution` từ 0 đến 3.

`scripts/levels.cjs` tái tạo bộ 24 màn mẫu và `Web/levels.js`, đồng thời kiểm tra các lời giải. **Không chạy lệnh này sau khi sửa catalog bằng Editor nếu muốn giữ các màn vừa sửa**, vì nó ghi đè catalog bằng bộ mẫu. Khi cần đồng bộ catalog đã chỉnh sang web, chạy:

```sh
node scripts/sync-levels.cjs
```

## Kiểm thử

```sh
# Logic JavaScript, không cần cài thư viện
npm test

# Trình duyệt: cần Chromium cài sẵn; chỉ playwright-core là thư viện phát triển
npm ci
npm run test:browser
# Có thể đặt CHROMIUM_PATH nếu Chromium không nằm tại /usr/bin/chromium

# Logic C# thật, cần .NET 8 SDK
dotnet run --project Assets/Tests/Pure/LuminaFlow.Tests.csproj
```

Trên host hạn chế MSBuild, có thể dùng `python3 Assets/Tests/Pure/run-tests.py` với .NET 8 SDK. Bộ kiểm tra này biên dịch C# 7.3 với API .NET Standard 2.1, kiểm tra lưu/phục hồi và đối chiếu mã chia sẻ/kết quả giữa hai engine. Chi tiết ở [Assets/Tests/README.md](Assets/Tests/README.md).

## Khác biệt và giới hạn so với tài liệu bàn giao

- Engine dùng **BFS trên lưới xác định** thay cho `Physics2D.RaycastNonAlloc`, giúp Unity và web có cùng quy tắc và kiểm thử được độc lập. Việc tính tia chỉ xảy ra khi bàn cờ đổi; chưa tuyên bố Zero GC hoặc đã đạt chỉ tiêu FPS trên thiết bị.
- Unity dùng URP/Bloom và animation tích hợp sẵn. Để dùng **DOTween**, cài DOTween từ nguồn Demigiant, chạy DOTween Setup và thêm scripting define **`LUMINA_DOTWEEN`**. Không nhúng một bản plugin không rõ nguồn vào dự án.
- Lưu màn bằng JSON catalog thay cho `ScriptableObject`/Prefab nhằm dùng chung dữ liệu hai nền tảng. Có trình sửa màn riêng trong Unity.
- Haptics dùng phản hồi thiết bị theo khả năng nền tảng; cần kiểm tra trên điện thoại thật.
- Quảng cáo, Firebase Analytics, thanh toán và phát hành lên store chưa được tích hợp. Tài liệu gốc chỉ nêu chúng là bước tiềm năng tiếp theo.
- Môi trường thực hiện không có Unity Editor, Android SDK/Xcode hay thiết bị thật. Logic C# và bản web được kiểm thử; import Unity, shader/Bloom, IL2CPP và bản build Android/iOS cần được xác nhận trong Unity trước khi phát hành.
