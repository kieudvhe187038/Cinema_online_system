// CineStar – chặn nhập chữ vào các ô chỉ nhận số.
//
// Trình duyệt KHÔNG tự chặn: Firefox cho gõ chữ thoải mái vào <input type="number">,
// Chrome vẫn cho gõ e/E/+/-. Script này chặn ngay từ thao tác gõ/dán nên ô số không bao
// giờ chứa chữ (validation phía server vẫn giữ nguyên, đây chỉ là lớp chặn ở giao diện).
//
// Áp dụng tự động cho: input[type=number], input[type=tel], input[inputmode=numeric]
// và bất kỳ ô nào gắn data-numeric.
// Tùy biến trên từng ô:
//   data-numeric="off"        -> bỏ qua ô này
//   data-numeric="decimal"    -> cho nhập dấu chấm thập phân
//   data-numeric-allow=":."   -> cho thêm các ký tự liệt kê (vd ô giờ HH:mm, ô tiền 75.000)
(function () {
    'use strict';

    var SELECTOR = 'input[type="number"], input[type="tel"], input[inputmode="numeric"], input[data-numeric]';
    var DIGITS = '0123456789';
    // Các loại input đọc/ghi được vùng chọn (type=number thì trình duyệt chặn selectionStart).
    var SELECTABLE_TYPES = ['text', 'tel', 'search', 'url', 'password'];

    function isTarget(element) {
        return !!element
            && typeof element.matches === 'function'
            && element.matches(SELECTOR)
            && (element.getAttribute('data-numeric') || '').toLowerCase() !== 'off';
    }

    function isSelectable(input) {
        return SELECTABLE_TYPES.indexOf((input.type || 'text').toLowerCase()) >= 0;
    }

    // Bộ ký tự hợp lệ của một ô: luôn có chữ số, cộng thêm dấu thập phân / dấu âm / ký tự khai báo riêng.
    function allowedChars(input) {
        var allowed = DIGITS;
        var mode = (input.getAttribute('data-numeric') || '').toLowerCase();
        var isNumberType = (input.type || '').toLowerCase() === 'number';

        // Dấu thập phân: chỉ khi step cho phép số lẻ (step="any" hoặc step="0.01"...).
        var step = (input.getAttribute('step') || '').toLowerCase();
        if (mode === 'decimal' || (isNumberType && (step === 'any' || step.indexOf('.') >= 0))) {
            allowed += '.';
        }

        // Dấu âm: chỉ với ô số thật sự và không bị chặn bởi min >= 0.
        var min = input.getAttribute('min');
        if (isNumberType && (min === null || parseFloat(min) < 0)) {
            allowed += '-';
        }

        return allowed + (input.getAttribute('data-numeric-allow') || '');
    }

    function keepAllowed(text, allowed) {
        var result = '';
        for (var i = 0; i < text.length; i++) {
            if (allowed.indexOf(text.charAt(i)) >= 0) result += text.charAt(i);
        }
        return result;
    }

    // Chèn phần đã lọc vào đúng vị trí con trỏ (chỉ dùng cho ô đọc được vùng chọn).
    function insertText(input, text) {
        var start = input.selectionStart;
        var end = input.selectionEnd;
        if (start === null || start === undefined) return;

        if (typeof input.setRangeText === 'function') {
            input.setRangeText(text, start, end, 'end');
        } else {
            input.value = input.value.slice(0, start) + text + input.value.slice(end);
            input.setSelectionRange(start + text.length, start + text.length);
        }
        // Báo cho các script khác (định dạng tiền, ghép giờ HH:mm...) biết giá trị đã đổi.
        input.dispatchEvent(new Event('input', { bubbles: true }));
    }

    // Chặn/lọc nội dung sắp được chèn. Trả về true nếu đã xử lý (đã preventDefault).
    function filterInsertion(event, input, incoming) {
        var allowed = allowedChars(input);
        var cleaned = keepAllowed(incoming, allowed);
        if (cleaned === incoming) return false;

        event.preventDefault();
        if (cleaned && isSelectable(input)) insertText(input, cleaned);
        return true;
    }

    // Gõ phím, dán, kéo-thả đều đi qua beforeinput (sự kiện này nổi bọt nên bắt ở document
    // là đủ cho cả nội dung được thêm động về sau).
    document.addEventListener('beforeinput', function (event) {
        var input = event.target;
        if (!isTarget(input) || !event.cancelable) return;
        if (!event.inputType || event.inputType.indexOf('insert') !== 0) return;

        var incoming = event.data;
        if (incoming == null && event.dataTransfer) incoming = event.dataTransfer.getData('text');
        if (!incoming) return;

        filterInsertion(event, input, incoming);
    });

    // Dự phòng cho trình duyệt không đưa nội dung dán vào beforeinput (Safari).
    // Sự kiện paste chạy TRƯỚC beforeinput nên chặn ở đây là không bị xử lý hai lần.
    document.addEventListener('paste', function (event) {
        var input = event.target;
        if (!isTarget(input) || !event.cancelable) return;

        var clipboard = event.clipboardData || window.clipboardData;
        if (!clipboard) return;

        var incoming = clipboard.getData('text');
        if (!incoming) return;

        filterInsertion(event, input, incoming);
    });
})();
