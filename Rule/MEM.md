### [2026-06-15] Fix carousel indicators not updating on slide change (By: admin)
- **What changed:** Modified carousel indicators in [Views/Home/Index.cshtml](Views/Home/Index.cshtml#L60-L65) to use Tailwind classes instead of inline styles for updating active dot state. Changed `showSlide()` function to add/remove `bg-white/80` and `bg-white/50` classes rather than setting `style.background`.
- **Why:** Inline styles couldn't override Tailwind CSS classes with `!important` flag. Now dots correctly highlight when switching slides by adding `active` class and toggling opacity classes.
- **Impact/Notes for Team:** Carousel indicators now properly update when slides change via next/prev buttons or dot clicks. Dots use Tailwind classes consistently with the initial markup.

### [2026-06-15] Fix banner responsive issue at zoom 80% (By: admin)
- **What changed:** Added `w-full h-full object-cover` classes to all banner images in [Views/Home/Index.cshtml](Views/Home/Index.cshtml#L31-L48) to ensure images fill the entire carousel container without gaps at any zoom level.
- **Why:** When zooming to 80% using Ctrl+scroll, the banner images had black/empty spaces on the sides because images weren't filling the full container width. The `object-cover` property ensures images scale to cover the container while maintaining aspect ratio.
- **Impact/Notes for Team:** Banner now displays full-width without black borders at any zoom level. All carousel slide images use consistent sizing: `w-full h-full object-cover`.

### [2026-06-14] Exclude stopped movies from filter/search and improve variable names (By: admin)
- **What changed:** Updated movie filtering/service logic to exclude movies with status `Stopped` from search results, active filters, and available status options. Refactored ambiguous variables like `vm`, `items`, `movies`, and `q` to more specific names in `MoviesController`, `HomeController`, and `MovieService`.
- **Why:** The UI should not display or allow filtering by movies that have already stopped screening. Clear variable names improve code readability and maintainability.
- **Impact/Notes for Team:** Status lists and filtered movie sets now omit `Stopped`; any future service calls should use explicit variable names for view models and paged movie collections.
