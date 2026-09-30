# CSharpShop

Đồ án C#: ASP.NET Core API, SQL Server và giao diện quản trị WinForms trên .NET 10.

## Cấu trúc

- `CSharpShop.Api`: controller, HTTP, response wrapper và global exception middleware.
- `CSharpShop.Application`: model, DTO, service, interface repository và AutoMapper profile.
- `CSharpShop.Infrastructure`: truy cập SQL Server bằng Microsoft.Data.SqlClient.
- `CSharpShop.Admin`: giao diện Windows Forms, hiện là form ban đầu.

Hiện có API GET `/api/categories`. JWT, các chức năng CRUD và giao diện quản trị đầy đủ sẽ được bổ sung. Id loại sản phẩm do admin nhập.

## Chạy trên Windows

Cài .NET SDK 10 và SQL Server. Database `CSharpShopDb` cần có bảng `dbo.Categories` và dữ liệu tương ứng. Bản ZIP nguồn này không chứa script tạo database.

Cấu hình phát triển nằm trong `src/CSharpShop.Api/appsettings.Development.json`, dùng `Server=localhost` và Windows Authentication, không chứa mật khẩu. Không đưa mật khẩu, JWT key hoặc cấu hình server thật vào Git; dùng biến môi trường hoặc .NET User Secrets.

```powershell
dotnet restore CSharpShop.sln
dotnet build CSharpShop.sln
dotnet run --project src/CSharpShop.Api/CSharpShop.Api.csproj
```

Giữ terminal API chạy và kiểm tra Postman: `GET http://localhost:5142/api/categories`.

Trong terminal khác:

```powershell
dotnet run --project src/CSharpShop.Admin/CSharpShop.Admin.csproj
```

Dừng API bằng Ctrl+C trước khi build lại nếu Windows báo file đang được sử dụng.

## CI và bản đóng gói

Workflow `.github/workflows/ci.yml` chạy khi push vào `main`/`master`, khi mở hoặc cập nhật pull request, hoặc khi bấm Run workflow. Runner Windows cài SDK 10, restore và build cả solution Release. Nếu có project test được đánh dấu IsTestProject hoặc tham chiếu Microsoft.NET.Test.Sdk, workflow chạy test. Hiện chưa có project test.

Smoke check khởi động API, kiểm tra `/weatherforecast` và kiểm tra global exception handler qua `/api/test-error` ở môi trường Development. Nó không kiểm tra kết nối SQL Server hay mapping Categories; GitHub runner không có database trên máy cá nhân.

Sau CI thành công, push vào main/master hoặc chạy thủ công tạo hai artifacts trong trang Actions:

- API: bản framework-dependent, cần ASP.NET Core Runtime 10 trên máy chạy.
- Admin Windows x64: bản self-contained, chứa runtime.

Artifacts giữ 14 ngày. Pull request chỉ kiểm tra, không xuất bản package. Đây là CI và đóng gói phục vụ continuous delivery; chưa tự động deploy tới server hay tạo GitHub Release. CD deploy cần bổ sung server, secrets và cấu hình SQL Server. WinForms hiện chưa kết nối API.

Bản API publish mặc định chạy Production; `/api/test-error` chỉ có ở Development. Trước khi chạy bản publish, cấu hình `ConnectionStrings__ShopDb` bằng biến môi trường phù hợp trên máy đích. Không dùng connection string Windows Authentication của máy cá nhân như cấu hình cho server khác.

## Đưa lên GitHub lần đầu

Tạo repository trống (không chọn tạo README/.gitignore/license). Mở terminal tại thư mục có CSharpShop.sln. Thay URL bên dưới bằng URL repository của bạn.

```powershell
git init
git add .
git status
git commit -m "Set up CSharpShop CI and publish packages"
git branch -M main
git remote add origin https://github.com/YOUR_USERNAME/CSharpShop.git
git push -u origin main
```

Nếu thư mục đã có Git hoặc remote, kiểm tra `git status` và `git remote -v` trước; không ghi đè remote hay dùng force push. Với repo đã có lịch sử, clone repo rồi đưa các file này vào checkout đó.

Sau push: mở GitHub → Actions → CSharpShop CI and packages. Chờ workflow xanh rồi tải artifacts. Nếu thất bại, mở step màu đỏ để xem lỗi.

## Kiểm tra trước khi giao

Bản chuẩn bị đã kiểm tra cấu trúc YAML, đường dẫn project, target framework và ZIP không có bin/obj. Môi trường chuẩn bị không có .NET SDK hay PowerShell/Windows nên chưa thực thi build hoặc smoke check; kết quả CI đầu tiên trên GitHub là xác nhận thực tế.
