# Kế hoạch bảo trì và nâng cấp Lumina Flow trong 7 ngày

Mục tiêu: một thay đổi nhỏ có kiểm thử mỗi ngày, giữ Unity và web cùng quy tắc, cải thiện bản web, lưu tiến trình ổn định và có bản sao mã nguồn để khôi phục. Agent chạy trên GitHub Actions, không cần máy cá nhân luôn bật.

Nhánh mặc định hiện là `work`. Mỗi ngày agent lấy code đã được chấp nhận trên nhánh mặc định, sửa trên nhánh `agent/maintenance-*`, kiểm thử rồi tạo **draft pull request**. Bạn dành khoảng 10–15 phút mỗi ngày để xem ảnh, xem diff, kiểm tra CI và quyết định merge. Không có auto-merge.

## Lịch 7 ngày

| Ngày | Việc agent thực hiện | Bạn kiểm tra / đầu ra cần có |
| --- | --- | --- |
| 1 — Baseline | Chạy JS, C#, 236 tổ hợp phản xạ hiện tại; tái hiện lỗi CSS/asset/offline nếu có; sửa lỗi tái hiện được trước. | CI xanh; ảnh desktop/mobile; ghi rõ những gì chưa được kiểm tra trong Unity. |
| 2 — Giao diện | Độ rõ chữ, kích thước nút chạm, màn hình nhỏ, focus bàn phím, reduced motion; chọn một vấn đề cụ thể. | So sánh ảnh trước/sau; thử trên điện thoại thật. |
| 3 — Gương | Phản xạ bốn hướng, tách tia, portal, filter, undo/hint, vòng lặp và cảm biến/cửa; đối chiếu C#/web. | Test tái hiện lỗi và parity đạt; không thay đổi format màn chơi tùy tiện. |
| 4 — Maintain / hiệu năng | Đo trước khi tối ưu; giảm tính toán/render dư hoặc làm gọn một vùng code khó bảo trì. | Số đo trước/sau hoặc lý do refactor, không tuyên bố FPS thiết bị nếu chưa đo. |
| 5 — Update / dữ liệu | Đọc dependency audit; xem bản vá phù hợp; kiểm tra save/recovery, mã chia sẻ và giới hạn đầu vào. | Không nâng major tự động; không đổi key, xóa save hay thêm SDK quảng cáo. Việc cần mạng/Unity mà agent không có phải được ghi là blocker. |
| 6 — Mirror / khôi phục | Kiểm tra catalog Unity → web, HTML offline và thử khôi phục Git bundle trong thư mục tạm. | Catalog khớp; bundle phục hồi được; tải một bản backup về máy. |
| 7 — Tổng kiểm | Chạy regression; tổng hợp sửa đổi đã merge, lỗi còn lại và checklist bản thử nghiệm. | Bạn mở Unity, thử Android/iOS nếu có thiết bị; chốt backlog tuần sau. Không tự phát hành store. |

Đây là mục tiêu theo ngày, không phải cam kết tạo đủ bảy PR: nếu không có sửa đổi hợp lý, agent báo cáo và không mở PR rỗng. Nếu ngày trước chưa merge, agent ngày sau vẫn bắt đầu từ code đã chấp nhận trên nhánh mặc định. Nên xử lý PR trước lần chạy hôm sau.

## Cài đặt trên GitHub

1. Đưa các file cấu hình trong thay đổi này vào nhánh mặc định `work`. Đặc biệt cần `AGENTS.md`, `.github/`, `scripts/automation.py` và `tests/test_automation.py`.
2. Vào repository → **Settings → Actions → General**, cho phép GitHub Actions và các action được dùng trong workflow. Trong **Workflow permissions**, bật **Allow GitHub Actions to create and approve pull requests**. Workflow tự khai báo quyền cần thiết cho từng job; bạn vẫn là người duyệt/merge PR.
3. Tạo API key riêng cho project trên [OpenAI Platform](https://platform.openai.com/api-keys). Vào repository → **Settings → Secrets and variables → Actions → Secrets → New repository secret**, đặt tên **`OPENAI_API_KEY`**. Dán key trực tiếp ở GitHub, không gửi vào chat hoặc commit vào mã nguồn. API được tính phí riêng với gói ChatGPT; kiểm tra billing và thiết lập ngân sách/cảnh báo phù hợp. Timeout không phải giới hạn chi phí cứng.
4. Trong tab **Variables**, tạo:

   | Variable | Giá trị |
   | --- | --- |
   | `LUMINA_AGENT_START_DATE` | Ngày bắt đầu theo giờ Việt Nam, `YYYY-MM-DD`, ví dụ `2026-10-30`. |
   | `LUMINA_AGENT_ENABLED` | `false` trong lúc chuẩn bị, sau đó đổi thành **`true`** khi sẵn sàng. |
   | `LUMINA_AGENT_MODEL` | Tùy chọn; tên model được API project của bạn hỗ trợ. Để trống để Codex CLI dùng model mặc định. |

5. Vào **Actions → Lumina CI → Run workflow**, chọn `work`. Chờ kiểm thử đạt trước khi bật agent. CI này không gọi OpenAI API.
6. Đến ngày bắt đầu, đặt enabled thành `true`, vào **Actions → Lumina 7-day agent → Run workflow**, chọn `work` để chạy thử lần đầu. Xem log, artifact `maintenance-candidate` và draft PR. Run thủ công cũng bị kiểm tra cửa sổ ngày; chạy trước ngày bắt đầu hoặc sau ngày 7 sẽ bỏ qua agent.
7. Từ đó lịch tự chạy lúc **09:17 giờ Việt Nam**; backup lúc **08:15**. Lịch GitHub có thể trễ, không đảm bảo đúng phút. Chạy thủ công hoặc re-run thêm sẽ phát sinh lần gọi API thêm; không chạy lặp nhiều lần cùng ngày nếu không cần.

Ví dụ bắt đầu **30/10/2026**: ngày 7 là **05/11/2026**; từ **06/11/2026** agent và backup không thực hiện công việc nữa. Workflow lịch vẫn có thể khởi động job kiểm tra điều kiện, nhưng không gọi Codex ngoài cửa sổ này. Tắt ngay bằng cách đổi `LUMINA_AGENT_ENABLED=false` hoặc **Actions → workflow → Disable workflow**. Không tự gia hạn; muốn đợt mới cần chọn ngày bắt đầu mới.

GitHub chỉ chạy lịch trên default branch. Nếu sau này đổi default branch, đưa bộ workflow vào nhánh đó; CI push filter `work` cũng cần đổi tương ứng.

## Agent làm gì, với quyền nào?

- `gate`: kiểm tra opt-in và ngày theo `Asia/Bangkok` (cùng UTC+7 với Việt Nam).
- `agent`: checkout chính xác commit của default branch, cài/chạy kiểm thử trước; Codex dùng workspace profile, `drop-sudo`, không được cấp token ghi repository. Các lệnh của agent không có quyền truy cập mạng; dữ liệu audit được chuẩn bị trước.
- `verify`: runner mới áp dụng patch, kiểm tra danh sách đường dẫn và chạy lại kiểm thử. Patch quá 20 file, 250 KiB, chứa symlink/submodule, sửa workflow/chính sách hoặc có whitespace lỗi sẽ bị chặn.
- `publish`: runner mới chỉ áp dụng patch đã kiểm tra, commit/push nhánh mới, mở draft PR và yêu cầu CI chạy trên chính nhánh đó. Token ghi chỉ nằm ở job này; job không chạy game hay npm script từ bản sửa.
- `ci`: kiểm tra JS, cửa sổ tự động, C# và parity, Chromium desktop/mobile/offline; ảnh và HTML trong artifact `web-verification`.

GITHUB_TOKEN thường không tự kích hoạt workflow từ push/PR do chính nó tạo. Vì thế job publish gọi `workflow_dispatch` cho `ci.yml`; nó cần quyền `actions: write`. Không cần thêm PAT toàn quyền. Trong PR, chờ CI của commit trên nhánh agent đạt trước khi merge. Repo/organization có ruleset riêng có thể cần admin cho phép Actions tạo PR.

Các action được pin bằng commit; Codex CLI được pin `0.165.0`. Cấu hình đã đối chiếu với [Codex Action chính thức](https://github.com/openai/codex-action) và [hướng dẫn quyền](https://github.com/openai/codex-action/blob/main/docs/security.md). Agent không được tự đổi các pin hoặc mở rộng quyền; bạn cập nhật cấu hình này sau review.

## Mirror / backup

Hiện có **Git bundle** hằng ngày, gồm refs/lịch sử Git đã fetch, kiểm tra bằng `git bundle verify`, lưu trong Actions artifact 30 ngày. Đây là bản sao Git; không phải bản sao Issues, PR, GitHub Secrets, Settings, file chưa commit hay build cache. Artifact cùng repository cũng không thay thế bản sao ở một nhà cung cấp độc lập: tải về máy hoặc lưu sang nơi khác nếu cần chống mất quyền truy cập repo.

Tải artifact ở **Actions → Lumina Git backup → run → git-backup-…**, giải nén, rồi:

```sh
git bundle verify Lumina-Flow.bundle
git clone Lumina-Flow.bundle Lumina-Flow-restored
cd Lumina-Flow-restored
git log -3 --oneline
```

Nếu muốn đồng bộ sang một repo Git thứ hai, cần chọn repo đích và cấp quyền ghi riêng cho repo đó. Chưa cấu hình remote mirror vì chưa có địa chỉ đích. Sau khi có remote `backup`, cách đồng bộ không xóa nhánh đích là:

```sh
git push backup --all
git push backup --tags
```

Không dùng `git push --mirror` mặc định: nó có thể force-update và xóa refs ở repo đích. Khóa deploy key/GitHub App dành cho repo đích phải nằm trong Secrets, không nằm trong URL remote.

## Việc cần bạn làm trong Unity

CI Linux hiện kiểm tra C# thuần, không chạy Unity Editor. Trong tuần này, ít nhất một lần mở scene Main bằng Unity 2022.3 LTS, xác nhận không có lỗi compile, URP/Bloom hoạt động, màn hình dọc/safe area và cảm ứng đúng. Nếu cần build Android/iOS tự động, bước tiếp theo là runner có Unity + license phù hợp; iOS còn cần macOS/Xcode/signing. Đây là việc riêng sau khi baseline hiện tại chạy ổn, không được báo đã hoàn tất từ các test C#.

## Khắc phục lỗi cài đặt

| Hiện tượng | Kiểm tra |
| --- | --- |
| Job agent bị skip | Enabled phải là chuỗi `true`; ngày hiện tại phải nằm trong bảy ngày tính theo UTC+7; workflow cần có trên default branch. |
| Không có mục Run workflow | File chưa nằm trên default branch hoặc Actions bị tắt. |
| API 401/403 hoặc hết quota | Secret đúng tên, key còn hiệu lực, API billing/model được cấp quyền. Không in key ra log. |
| Không mở được PR | Bật quyền Actions tạo PR; kiểm tra ruleset/organization policy. Patch vẫn có thể tải từ artifact để review thủ công. |
| CI không chạy sau khi PR tạo | Kiểm tra bước dispatch và quyền Actions; có thể chạy Lumina CI thủ công, chọn đúng nhánh `agent/maintenance-*`. |
| Chưa có patch/PR | Đọc report; không có thay đổi hợp lý thì đây là kết quả bình thường. |
| Muốn dừng | Set enabled=false; hủy run đang chạy nếu có; disable workflow nếu muốn ngừng cả job gate. |
