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

// Search input validation: allow Vietnamese letters, numbers, spaces, and limit to 30 chars
const movieSearchInput = document.getElementById('movie-search-input');
if (movieSearchInput) {
    movieSearchInput.addEventListener('input', function () {
        const allowed = this.value.replace(/[^\p{L}\p{N} ]/gu, '');
        if (allowed !== this.value) {
            this.value = allowed;
        }
    });
}
