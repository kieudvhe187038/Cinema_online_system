// CineStar – chuẩn hóa chuỗi tiếng Việt phía client (bản JS của Application/Common/VietnameseText.cs).
//
// Dùng cho mọi ô lọc/gợi ý chạy bằng JavaScript để gõ KHÔNG DẤU vẫn khớp:
// "bi" -> "Bí Mật Phố Cổ", "song" -> "Hoàng Cung Dậy Sóng".
//
// Nạp ở <head> của layout vì nhiều view đặt <script> ngay trong body (chạy trước phần
// cuối layout) — file này chỉ định nghĩa biến toàn cục, không đụng DOM nên đặt ở head là an toàn.
window.VietnameseText = (function () {
    'use strict';

    // Bỏ dấu 1 ký tự. đ/Đ không tách được bằng Unicode nên thay thủ công (giống bản C#).
    function foldChar(character) {
        if (character === 'đ' || character === 'Đ') return 'd';
        var base = character.normalize('NFD').replace(/[\u0300-\u036f]/g, '');
        return (base || character).toLowerCase();
    }

    // Ánh xạ từng ký tự gốc sang ĐÚNG 1 ký tự đã bỏ dấu, nhờ vậy chỉ số tìm được trên `key`
    // dùng thẳng để cắt `chars` (phục vụ tô đậm đoạn khớp).
    // `aligned` = false khi có ký tự không ánh xạ 1-1 -> bên gọi nên bỏ tô đậm thay vì cắt sai.
    function fold(text) {
        var chars = Array.from(text || '');
        var folded = chars.map(foldChar);

        return {
            chars: chars,
            key: folded.join(''),
            aligned: folded.every(function (piece) { return piece.length === 1; })
        };
    }

    // Khóa so khớp: bỏ dấu + thường hóa + cắt khoảng trắng thừa.
    function searchKey(text) {
        return fold(text).key.trim();
    }

    // True nếu source chứa từ khóa, bỏ qua dấu và hoa-thường.
    // searchKey phải là kết quả của VietnameseText.searchKey (chuẩn hóa sẵn, không lặp mỗi phần tử).
    function contains(source, key) {
        if (!key) return true;
        if (!source) return false;
        return searchKey(source).indexOf(key) !== -1;
    }

    return { fold: fold, searchKey: searchKey, contains: contains };
})();
