# color.md — Quy Tắc Phối Màu (CineStar Design System)

> Bảng màu chính thức của hệ thống. Mọi UI mới **bắt buộc** dùng các token này, không hardcode mã màu rời rạc.
> Nguồn gốc: `Cinema_System/wwwroot/css/site.css` (`:root`).

---

## 1. Bảng Màu Cốt Lõi (CSS Variables)

| Token | Mã màu | Vai trò |
| ----- | ------ | ------- |
| `--primary` | `#f37021` | Màu nhấn chính (cam) — CTA, nút đặt vé, link active, logo |
| `--primary-dark` | `#c95a10` | Trạng thái hover/active của primary |
| `--secondary` | `#004b8d` | Màu phụ (xanh dương đậm) — header phụ, nhãn thông tin |
| `--secondary-dark` | `#001c3a` | Nền tối, overlay đậm, footer |
| `--surface` | `#f8f9fa` | Nền chính của trang |
| `--surface-container` | `#edeeef` | Nền card, panel, ô input |
| `--on-surface` | `#191c1d` | Màu chữ chính trên nền sáng |
| `--on-surface-variant` | `#584237` | Chữ phụ, mô tả, placeholder |
| `--outline-variant` | `#e0c0b2` | Viền nhạt, đường phân cách |
| `--white` | `#ffffff` | Chữ trên nền màu, nền sạch |

### Màu trạng thái (Status / Badge)
| Mục đích | Mã màu | Ghi chú |
| -------- | ------ | ------- |
| Cảnh báo độ tuổi T16/T18 (nguy hiểm) | `#dc2626` | `.badge-age.t16` |
| Cảnh báo nhẹ / P (vàng) | `#eab308` | `.badge-age.p` |
| Overlay phim | `rgba(0,36,72,.65)` | Lớp phủ trên poster |

---

## 2. Nguyên Tắc Sử Dụng

1. **Luôn dùng biến CSS**, không viết mã hex trực tiếp trong `.cshtml` hay CSS mới:
   - ✔ `background: var(--primary);`
   - ✘ `background: #f37021;`
2. **CTA & hành động chính** (Đặt vé, Đăng nhập, Submit) → `--primary`, hover → `--primary-dark`.
3. **Hành động phụ / liên kết điều hướng** → `--secondary`.
4. **Chữ:** nội dung chính `--on-surface`; mô tả/phụ `--on-surface-variant`; chữ trên nền màu `--white`.
5. **Nền:** trang `--surface`; thẻ/panel `--surface-container`; vùng tối/footer `--secondary-dark`.
6. **Viền & phân cách:** `--outline-variant`.

---

## 3. Quy Ước Với Tailwind CSS

Project dùng Tailwind theo hướng Utility-First (theo `RULE.md`). Khi cần dùng màu thương hiệu trong class Tailwind, **ánh xạ token vào `tailwind.config`** thay vì dùng class màu mặc định:

```js
// tailwind.config.js
theme: {
  extend: {
    colors: {
      primary:   { DEFAULT: '#f37021', dark: '#c95a10' },
      secondary: { DEFAULT: '#004b8d', dark: '#001c3a' },
      surface:   { DEFAULT: '#f8f9fa', container: '#edeeef' },
      'on-surface': { DEFAULT: '#191c1d', variant: '#584237' },
      outline:   '#e0c0b2',
    }
  }
}
```

Sau đó dùng: `class="bg-primary hover:bg-primary-dark text-white"`.

- Không trộn lẫn class màu chung của Tailwind (`bg-blue-600`, `bg-orange-500`) với token thương hiệu trong cùng một component.
- Component cũ dùng `site.css` thì giữ nguyên biến CSS; component mới ưu tiên Tailwind đã ánh xạ.

---

## 4. Tương Phản & Khả Năng Tiếp Cận (A11y)

- Chữ thường trên nền phải đạt tỷ lệ tương phản tối thiểu **4.5:1** (WCAG AA).
- Không đặt `--on-surface-variant` (chữ phụ) lên nền `--primary` — tương phản kém. Dùng `--white`.
- Trạng thái focus của input/nút phải thấy rõ (viền `--primary` hoặc ring).

---

## 5. Checklist Khi Thêm Màu Mới

- [ ] Màu mới có thể thay bằng token sẵn có không? (Ưu tiên tái sử dụng)
- [ ] Nếu thật sự cần, đã khai báo thành biến trong `:root` của `site.css` chưa?
- [ ] Đã kiểm tra tương phản với chữ phủ lên nó chưa?
- [ ] Đã đặt tên biến theo vai trò (semantic), không theo màu (`--danger` thay vì `--red`)?
