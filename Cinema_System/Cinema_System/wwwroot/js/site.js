// CineStar – site.js

// Sticky header effect
window.addEventListener('scroll', function () {
    const header = document.getElementById('main-header');
    if (!header) return;
    if (window.scrollY > 50) {
        header.classList.add('scrolled');
    } else {
        header.classList.remove('scrolled');
    }
});

// Quick booking select highlight
document.querySelectorAll('.book-field select').forEach(function (select) {
    select.addEventListener('change', function () {
        this.style.boxShadow = '0 0 0 2px #f37021';
        setTimeout(() => { this.style.boxShadow = ''; }, 1000);
    });
});