# API bán hoa (C# / ASP.NET Core)

API Minimal ASP.NET Core .NET 10, dùng SQL Server qua `Microsoft.Data.SqlClient`. Swagger UI chạy ở `/swagger`. Các truy vấn dùng trực tiếp các bảng của `SQLFile5.sql`: `ACCOUNT`, `CUSTOMER`, `PRODUCT_TYPE`, `PRODUCT`, `GOODS_RECEIPT`, `GOODS_RECEIPT_DETAIL`, `SALES_ORDER`, `ORDER_DETAIL`, `PAYMENT` và các view báo cáo.

## Chạy Swagger trên máy

1. Cài SQL Server. Với database mới, nạp `ShopApi/schema.sql` hoặc `SQLFile5.sql` mới trong SSMS. Nếu đã có database và muốn giữ dữ liệu, chỉ chạy migration `ShopApi/migrations/001-api-compat.sql`.
2. Mở PowerShell trong `api/ShopApi`, đặt connection string, rồi chạy:

```powershell
$env:ConnectionStrings__ShopDb = "Server=localhost;Database=QuanLyBanHoa;Integrated Security=True;TrustServerCertificate=True;Encrypt=True"
dotnet run --urls http://localhost:5080
```

3. Mở `http://localhost:5080/swagger`. Trang quản trị đăng nhập qua `POST /api/auth/admin/login` với `username=admin1`, `password=HoaDemo@2026`; trang khách hàng dùng `POST /api/auth/customer/login` (tài khoản mẫu `user4`, cùng mật khẩu `HoaDemo@2026`). Session cookie được giữ trong Swagger UI. Nếu database cũ còn mật khẩu mẫu dạng `HASH_pass1`, hãy chạy migration ở bước 1 để cập nhật hash cho các tài khoản mẫu. Nếu dùng SQL Express, đổi server trong connection string thành `Server=.\\SQLEXPRESS;`.

Khách vãng lai đăng ký tại `POST /api/auth/register` trong Swagger, không cần đăng nhập trước. Ví dụ request:

```json
{
  "username": "khachmoi01",
  "password": "MatKhauMoi123",
  "fullName": "Nguyễn Văn An",
  "phone": "0901234567",
  "email": "an@example.com",
  "gender": "Nam",
  "dateOfBirth": "2000-01-01",
  "address": "Quận 1, TP. Hồ Chí Minh"
}
```

## Chạy bằng Podman Compose

Sao chép `.env.example` thành `.env`, đặt mật khẩu SQL Server đủ mạnh, rồi tại thư mục `api` chạy `podman compose up --build -d`. Với volume DB mới, nạp schema đúng một lần:

```powershell
podman compose exec -T db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "MAT_KHAU" -C -b -i /init/schema.sql
```

Nếu database `QuanLyBanHoa` đã tồn tại, không chạy lại schema vì SQL seed tạo dữ liệu mẫu và các bảng không phải migration idempotent. Swagger ở `http://localhost:5081/swagger`. Dừng container bằng `podman compose down`; thêm `-v` chỉ khi muốn xóa hẳn dữ liệu SQL Server.

Nếu volume đã được nạp từ `SQLFile5.sql`, dùng `podman compose exec -T db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "MAT_KHAU" -C -b -i /init/migrations/001-api-compat.sql` để giữ dữ liệu và bổ sung schema API.

## API hiện thực

| Method | Đường dẫn | Chức năng |
|---|---|---|
| POST | `/api/auth/register` | Đăng ký khách hàng và thông tin liên hệ/giao hàng |
| POST | `/api/auth/admin/login`, `/api/auth/customer/login` | Đăng nhập riêng cho trang quản trị và trang khách hàng |
| POST | `/api/auth/register` | Khách vãng lai tự đăng ký tài khoản khách hàng |
| POST | `/api/auth/login`, `/logout`, GET `/me` | Đăng nhập tương thích cũ, đăng xuất, thông tin phiên |
| PATCH | `/api/me` | Cập nhật hồ sơ khách hàng |
| GET | `/api/product-types`, `/api/products`, `/api/products/{id}` | Danh mục, lọc/tìm kiếm/phân trang và chi tiết |
| GET/POST/PUT/DELETE | `/api/admin/product-types` | Quản lý loại sản phẩm |
| POST/PUT/DELETE | `/api/admin/products` | Quản lý sản phẩm, ảnh, giá và hiện trạng |
| GET/POST/PATCH | `/api/admin/users`, `/users/{id}/status`, `/users/{id}/password` | Quản lý khách hàng, khóa tài khoản, khởi tạo mật khẩu |
| GET/POST/PUT | `/api/cart`, `/api/cart/items` | Xem và cập nhật giỏ hàng |
| POST/GET | `/api/admin/receipts` | Tạo/tìm/sửa phiếu nhập còn nháp |
| POST | `/api/admin/receipts/{id}/complete` | Hoàn tất phiếu; trigger cộng kho và cập nhật giá vốn bình quân |
| GET | `/api/admin/prices`, `/api/admin/price-history` | Giá vốn, tỉ lệ, giá bán và lịch sử phiếu nhập |
| POST/GET | `/api/orders` | Đặt hàng từ giỏ hoặc danh sách `items`, xem lịch sử |
| GET | `/api/orders/{id}` | Xem chi tiết đơn của khách đang đăng nhập |
| GET/PATCH | `/api/admin/orders`, `/orders/{id}/status`, `/orders/{id}/payment` | Lọc đơn, chi tiết, cập nhật trạng thái và thanh toán |
| GET | `/api/admin/inventory`, `/inventory/low-stock`, `/inventory/movements` | Tra cứu tồn theo ngày, cảnh báo, tổng nhập/xuất |

## Khác biệt schema đã điều chỉnh

- `SQLFile5.sql` gốc xóa database `QuanLyBanHoa` trước khi tạo; không chạy file đó trên database đang có dữ liệu. `ShopApi/schema.sql` giữ cấu trúc và dữ liệu mẫu nhưng chỉ tạo database nếu chưa tồn tại. Bản này cũng bổ sung `ProductCode`, `SupplierName` vì BB-DoAn yêu cầu mã và nhà cung cấp.
- BB-DoAn yêu cầu thanh toán trực tuyến ở trạng thái chưa xử lý; CHECK constraint trong file gốc không chấp nhận phương thức này. Bản schema API thêm `N'Trực tuyến'` vào constraint.
- Schema gốc không lưu giỏ hàng và địa chỉ giao ở thời điểm đặt. Bản schema API thêm `SHOPPING_CART_ITEM` và `SALES_ORDER.DeliveryAddress`; địa chỉ mặc định lấy từ `CUSTOMER.Address`, request có thể truyền địa chỉ khác.
- Trigger trong bản schema API tính giá vốn nhập bình quân có trọng số theo yêu cầu BB-DoAn; bản `SQLFile5.sql` gốc cập nhật theo giá nhập mới nhất nên không khớp yêu cầu. Trigger chỉ trừ tồn khi admin đổi đơn từ `Mới` sang `Đang xử lý`, và hoàn tồn khi hủy từ `Đang xử lý`. API không trừ tồn lúc tạo đơn để tránh trừ hai lần.
- Tài khoản admin mẫu `admin1`, `admin2`, `admin3` và khách mẫu `user4`–`user26` dùng mật khẩu `HoaDemo@2026`. Migration chuyển hash placeholder của các tài khoản mẫu hiện có sang hash tương ứng để có thể đăng nhập thử. Đổi mật khẩu sau khi thử nghiệm.
- Bản schema gốc không có lịch sử tồn theo dấu thời gian; báo cáo tồn tại ngày được tính lại từ phiếu nhập đã hoàn tất và đơn đã xử lý/giao. Lịch sử trạng thái giao dịch không thể khôi phục theo thời điểm với schema này.

Mật khẩu tài khoản mới được băm PBKDF2-HMAC-SHA256. Tích hợp thanh toán trực tuyến thực tế chưa được thực hiện theo đúng yêu cầu đề bài.
