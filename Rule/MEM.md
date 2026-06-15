### [2026-06-14] Exclude stopped movies from filter/search and improve variable names (By: admin)
- **What changed:** Updated movie filtering/service logic to exclude movies with status `Stopped` from search results, active filters, and available status options. Refactored ambiguous variables like `vm`, `items`, `movies`, and `q` to more specific names in `MoviesController`, `HomeController`, and `MovieService`.
- **Why:** The UI should not display or allow filtering by movies that have already stopped screening. Clear variable names improve code readability and maintainability.
- **Impact/Notes for Team:** Status lists and filtered movie sets now omit `Stopped`; any future service calls should use explicit variable names for view models and paged movie collections.
