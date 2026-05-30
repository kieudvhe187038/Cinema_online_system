Deploy ứng dụng ASP.NET Core MVC của bạn lên Server Linux (Ubuntu), sử dụng **Nginx** làm Reverse Proxy và **Cloudflare** để quản lý DNS, bảo mật SSL và Proxy.

Bạn có thể copy toàn bộ nội dung trong block mã nguồn bên dưới để lưu thành file `DEPLOYMENT_GUIDE.md`.

```markdown
# HƯỚNG DẪN DEPLOY HỆ THỐNG WEB CINEMA (ASP.NET CORE MVC)
## Môi trường: Ubuntu Server + Nginx + Cloudflare + SQL Server

Tài liệu này hướng dẫn chi tiết quy trình đóng gói ứng dụng **ASP.NET Core MVC** (sử dụng kiến trúc phân tầng Layered Architecture, EF Core, Tailwind CSS), thiết lập môi trường Production trên Linux Server, cấu hình **Nginx** làm Reverse Proxy và cấu hình **Cloudflare** bảo mật.

---

## TỔNG QUAN KIẾN TRÚC TRIỂN KHAI


```

[Khách hàng] ---> (HTTPS) ---> [Cloudflare (DNS/SSL)] ---> (HTTP/HTTPS) ---> [Nginx Reverse Proxy] ---> (Port 5000) ---> [Kestrel (ASP.NET Core App)]
|---> [SQL Server]

```

---

## BƯỚC 1: CHUẨN BỊ ỨNG DỤNG (PHÍA LOCAL/DEVELOPMENT)

### 1.1 Cấu hình Kestrel Forwarded Headers
Vì ứng dụng chạy sau Nginx và Cloudflare, bạn cần cấu hình ứng dụng để nhận đúng IP thật của Client và giao thức (HTTP/HTTPS).

Mở file `Program.cs` và thêm đoạn mã sau vào **trước** `builder.Build()`:

```csharp
using Microsoft.AspNetCore.HttpOverrides;

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeadersOptions.XForwardedFor | ForwardedHeadersOptions.XForwardedProto;
    // Cloudflare và Nginx thường dùng các dải IP proxy này, xóa các hạn chế mạng mặc định nếu cần:
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

```

Thêm vào **ngay đầu tiên** của HTTP pipeline sau `app.Build()`:

```csharp
var app = builder.Build();

app.UseForwardedHeaders(); // Phải đặt TRƯỚC Authentication, Authorization và Routing

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

```

### 1.2 Tạo file cấu hình Production (`appsettings.Production.json`)

Tạo file này ở thư mục gốc dự án (ngang hàng `appsettings.json`) để cấu hình Connection String và các tham số trên Server:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=127.0.0.1;Database=WebCinemaDb;User Id=sa;Password=YourSecurePassword123!;TrustServerCertificate=True;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}

```

> ⚠️ **LƯU Ý BẢO MẬT:** Đảm bảo `appsettings.Development.json` đã nằm trong `.gitignore` như quy tắc Git Workflow của dự án. Không commit mật khẩu Production lên Git.

### 1.3 Biên dịch và đóng gói ứng dụng (Publish)

Từ thư mục chứa file dự án `.csproj`, chạy lệnh để build ra thư mục chuẩn bị mang lên server:

```bash
dotnet publish -c Release -o ./publish

```

Nén thư mục `publish` thành file `.zip` (ví dụ: `webcinema.zip`) để chuẩn bị upload.

---

## BƯỚC 2: CẤU HÌNH SERVER LINUX (UBUNTU)

Kết nối vào VPS của bạn qua SSH:

```bash
ssh root@your_server_ip

```

### 2.1 Cài đặt .NET 8.0 Runtime (hoặc .NET version dự án sử dụng)

```bash
# Cập nhật danh sách gói
sudo apt-get update

# Cài đặt ASP.NET Core Runtime
sudo apt-get install -y aspnetcore-runtime-8.0

```

### 2.2 Cài đặt SQL Server trên Ubuntu (Nếu chạy DB cùng Server)

Nếu bạn dùng Server DB riêng thì bỏ qua bước này. Nếu chạy chung:

```bash
# Nhập khóa GPG kho lưu trữ công cộng
wget -qO- [https://packages.microsoft.com/keys/microsoft.asc](https://packages.microsoft.com/keys/microsoft.asc) | sudo apt-key add -

# Đăng ký kho lưu trữ Ubuntu của SQL Server
sudo add-apt-repository "$(wget -qO- [https://packages.microsoft.com/config/ubuntu/$](https://packages.microsoft.com/config/ubuntu/$)(lsb_release -rs)/mssql-server-2022.list)"

# Cài đặt SQL Server
sudo apt-get update
sudo apt-get install -y mssql-server

# Cấu hình SQL Server (Đặt mật khẩu sa mạnh)
sudo /opt/mssql/bin/mssql-conf setup

# Kiểm tra dịch vụ hoạt động
systemctl status mssql-server

```

> 💡 *Mẹo:* Sau khi cài đặt, hãy sử dụng script `CreateAndSeed.sql` từ tầng SQL của dự án để khởi tạo cấu trúc Database.

### 2.3 Upload và Giải nén Source Code

Tạo thư mục chứa ứng dụng:

```bash
sudo mkdir -p /var/www/webcinema
sudo chown -R $USER:$USER /var/www/webcinema

```

Dùng SCP hoặc FileZilla upload file `webcinema.zip` lên thư mục trên và giải nén:

```bash
sudo apt install unzip
cd /var/www/webcinema
unzip webcinema.zip

```

---

## BƯỚC 3: CẤU HÌNH SYSTEMD SERVICE FOR KESTREL

Để ứng dụng .NET chạy ngầm và tự động khởi động lại khi server bị crash hoặc reboot, ta tạo một `Systemd service`.

Tạo file dịch vụ:

```bash
sudo nano /etc/systemd/system/webcinema.service

```

Dán nội dung sau vào:

```ini
[Unit]
Description=ASP.NET Core Web Cinema Application
After=network.target mssql-server.service # Đảm bảo chạy sau mạng và DB

[Service]
WorkingDirectory=/var/www/webcinema
ExecStart=/usr/bin/dotnet /var/www/webcinema/Project.dll # Thay Project.dll bằng tên file chạy của bạn
Restart=always
# Khởi động lại sau 10 giây nếu bị sập
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=webcinema
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target

```

Kích hoạt và chạy Service:

```bash
sudo systemctl enable webcinema.service
sudo systemctl start webcinema.service

# Kiểm tra trạng thái xem ứng dụng đã chạy ở port 5000 thành công chưa
sudo systemctl status webcinema.service

```

---

## BƯỚC 4: CÀI ĐẶT VÀ CẤU HÌNH NGINX REVERSE PROXY

Nginx đóng vai trò đứng đầu tiếp nhận các Request từ ngoài internet (Port 80/443) rồi chuyển tiếp vào cổng nội bộ `localhost:5000` của Kestrel.

### 4.1 Cài đặt Nginx

```bash
sudo apt update
sudo apt install nginx -y
sudo systemctl start nginx
sudo systemctl enable nginx

```

### 4.2 Cấu hình Server Block cho Dự án

Tạo file cấu hình mới:

```bash
sudo nano /etc/nginx/sites-available/webcinema

```

Nội dung file cấu hình (Hỗ trợ tối ưu hóa và chống giả mạo Header):

```nginx
server {
    listen 80;
    server_name yourdomain.com [www.yourdomain.com](https://www.yourdomain.com); # Thay bằng domain của bạn

    # Cấu hình Static Files (Tối ưu hóa tải cho wwwroot)
    location / {
        proxy_pass         [http://127.0.0.1:5000](http://127.0.0.1:5000);
        proxy_http_version 1.1;
        proxy_set_header   Upgrade $http_upgrade;
        proxy_set_header   Connection keep-alive;
        proxy_set_header   Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header   X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;
    }

    # Tối ưu hóa Cache cho tài nguyên tĩnh của wwwroot (css, js, images)
    location ~* \.(?:css|js|jpg|jpeg|gif|png|ico|svg|woff2)$ {
        root /var/www/webcinema/wwwroot;
        expires 30d;
        add_header Cache-Control "public, no-transform";
    }
}

```

Kích hoạt cấu hình và restart Nginx:

```bash
sudo ln -s /etc/nginx/sites-available/webcinema /etc/nginx/sites-enabled/
sudo nginx -t # Kiểm tra cú pháp xem có lỗi không
sudo systemctl restart nginx

```

---

## BƯỚC 5: CẤU HÌNH CLOUDFLARE (DNS & SECURITY)

### 5.1 Cấu hình DNS Records

1. Đăng nhập vào trình quản lý **Cloudflare**.
2. Chọn tên miền của bạn (`yourdomain.com`).
3. Đi tới mục **DNS** -> **Records** và thêm:
* **Type:** `A` | **Name:** `@` (hoặc domain gốc) | **IPv4 address:** `IP_CỦA_SERVER_VPS` | **Proxy status:** `Proxied` (Đám mây màu vàng).
* **Type:** `CNAME` | **Name:** `www` | **Target:** `yourdomain.com` | **Proxy status:** `Proxied`.



### 5.2 Cấu hình SSL/TLS (Tránh lỗi vòng lặp chuyển hướng vô tận)

Khi bật đám mây Proxy của Cloudflare, lưu lượng từ Client đến Cloudflare sẽ được mã hóa HTTPS. Tại đây có 2 kịch bản cấu hình trong mục **SSL/TLS -> Overview**:

* **Chế độ Flexible (Khuyên dùng nếu lười cài SSL trên VPS):**
* *Luồng:* Client `(HTTPS)` -> Cloudflare `(HTTP)` -> Nginx (Port 80).
* *Lưu ý:* Khi chọn chế độ này, **KHÔNG** được cấu hình Nginx tự động redirect port 80 sang 443, vì sẽ tạo ra lỗi `ERR_TOO_MANY_REDIRECTS`.


* **Chế độ Full hoặc Full (Strict) (Khuyên dùng bảo mật tuyệt đối):**
* *Luồng:* Client `(HTTPS)` -> Cloudflare `(HTTPS)` -> Nginx (Port 443).
* *Thực hiện:* Bạn cần cài thêm chứng chỉ SSL (Let's Encrypt hoặc Cloudflare Origin CA) lên Nginx bằng Certbot trước khi bật chế độ này.



---

## BƯỚC 6: KIỂM TRA VÀ BẢO TRÌ HỆ THỐNG

### 6.1 Lệnh kiểm tra Logs thời gian thực

Khi ứng dụng có lỗi (500 Internal Server Error, không kết nối được Database...), dùng lệnh này để xem log chi tiết phát sinh từ code C# / Kestrel:

```bash
sudo journalctl -fu webcinema.service

```

### 6.2 Cập nhật Code mới (CI/CD thủ công theo Git Workflow)

Mỗi khi có code mới được Merge vào nhánh `main` và cần deploy lại:

1. Chạy `dotnet publish` ở máy local.
2. Nén và up đè đè file mới lên thư mục `/var/www/webcinema`.
3. Khởi động lại service để áp dụng thay đổi:

```bash
sudo systemctl restart webcinema.service

```

```

```
