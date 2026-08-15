---
layout: default
title: "Getting Started"
description: "Get started with uTPro – quick start guide, documentation overview, and contact information for the Umbraco Starter Kit by T4VN."
permalink: "/getting-started/"
---

# Getting Started

> 👤 Dành cho: **Tất cả** — Developer, Content Editor, Project Manager

## 🙏 Thank You for Choosing uTPro

Thank you for trusting and using **uTPro - Umbraco Turbo Pro**. This project is built with care and passion to help developers create enterprise-grade websites faster on the Umbraco platform.

Your feedback — comments, reviews, or suggestions — is incredibly valuable. It helps improve the project and shape future updates. If you have any questions or ideas, feel free to reach out!

**SPECIAL:** We also offer a **LOW COST premium** version for those who want exclusive customization tailored to their personal style. Your support means a lot to us!

- 📧 Email: [thientu@t4vn.com](mailto:thientu@t4vn.com)  
- 🌐 Website: [t4vn.com](https://t4vn.com)  

---

## 📖 Documentation

### Dành cho Content Editor (non-tech)

Bạn là người quản trị nội dung, biên tập website? Bắt đầu từ đây:

| # | Page | Description |
|---|------|-------------|
| 7 | [Content Editing](/7-Content-Editing/) | Hướng dẫn tạo/sửa nội dung, trang, block, SEO, đa ngôn ngữ |
| 6 | [Dashboard](/6-Dashboard/) | Bảng điều khiển backoffice — kiểm tra phiên bản, thống kê |
| 8 | [Global Settings](/8-Global-Settings/) | Cài đặt chung — favicon, robots, hình ảnh, form, bảo mật |
| 10 | [Backoffice Tools](/10-Backoffice-Tools/) | Công cụ quản trị — preview block, SEO audit, file manager |
| 11 | [Search](/11-Search/) | Tìm kiếm nội dung trên website |

### Dành cho Developer (tech)

Bạn là lập trình viên muốn cài đặt, tùy chỉnh hoặc phát triển mở rộng? Đọc theo thứ tự:

| # | Page | Description |
|---|------|-------------|
| 1 | [Introduction](/1-Intro/) | Tổng quan, tính năng, kiến trúc, công nghệ |
| 2 | [Setup](/2-Setup/) | Cài đặt domain, project, database, uSync |
| 3 | [Project Structure](/3-Project-Structure/) | Kiến trúc solution, middleware pipeline, Program.cs |
| 4 | [Configurations](/4-Configurations/) | Ngôn ngữ, backoffice, bảo mật, hiệu suất, SEO, load balancing, database |
| 5 | [Script Queue](/5-Script-Queue/) | Hệ thống load JS cho block component |
| 9 | [Developer Reference](/9-Developer-Reference/) | Razor helpers & C# extensions |

### Tính năng mở rộng (Optional Packages)

Tài liệu chi tiết cho từng gói tính năng có thể cài thêm:

| Package | Description |
|---------|-------------|
| [SEO Audit & URL Viewer](/uTPro.Feature.SEOAudit/) | Kiểm tra sức khỏe SEO và phân tích chuỗi chuyển hướng |
| [Simple Form Builder](/uTPro.Feature.SimpleFormBuilder/) | Xây dựng biểu mẫu trực quan |
| [Search Plus](/uTPro.Feature.SearchPlus/) | Tìm kiếm nâng cao — từ đồng nghĩa, không dấu |
| [File Manager](/uTPro.Feature.FileManager/) | Quản lý file server và media cleanup |
| [Job Monitor](/uTPro.Feature.JobMonitor/) | Theo dõi background job |
| [Audit Log](/uTPro.Feature.AuditLog/) | Nhật ký hoạt động chi tiết |
| [Auto Translation](/uTPro.Feature.AutoTranslation/) | Dịch tự động nội dung đa ngôn ngữ |

---

## 🚀 Quick Start (Developer)

1. **Clone** the repository
2. **Configure** database connection in `appsettings.json` ([details](/2-Setup/))
3. **Build & Run** with `dotnet run`
4. **Import data** via uSync ([details](/2-Setup/#23-setup-data))
5. **Start building** your site!

See [2. Setup](/2-Setup/) for the full guide.

## 🚀 Quick Start (Content Editor)

1. Truy cập `/umbraco` trên website của bạn
2. Đăng nhập bằng tài khoản được cấp
3. Vào **Content** section để tạo/sửa nội dung
4. Đọc [7. Content Editing](/7-Content-Editing/) để biết cách sử dụng Block Grid, SEO fields, đa ngôn ngữ

![Umbraco Backoffice Login](/screenshots/uTPro/backoffice-login.png)
