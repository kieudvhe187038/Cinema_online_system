using Microsoft.AspNetCore.Mvc.Razor;

namespace Cinema_System.Helpers
{
    public class SubfolderViewLocationExpander : IViewLocationExpander
    {
        private static readonly string[] _extraLocations =
        [
            "/Views/Admin/{1}/{0}.cshtml",
            "/Views/Manager/{1}/{0}.cshtml",
            "/Views/Staff/{1}/{0}.cshtml",
        ];

        public void PopulateValues(ViewLocationExpanderContext context) { }

        public IEnumerable<string> ExpandViewLocations(
            ViewLocationExpanderContext context,
            IEnumerable<string> viewLocations)
        {
            return _extraLocations.Concat(viewLocations);
        }
    }
}
