using Microsoft.AspNetCore.Mvc.Razor;

namespace Cinema_System.Helpers
{
    public class SubfolderViewLocationExpander : IViewLocationExpander
    {
        // {1} = tên controller (bỏ hậu tố Controller), {0} = tên action/view.
        // View đặt theo role: /Views/{Role}/{Controller}/{Action}.cshtml.
        private static readonly string[] _extraLocations =
        [
            "/Views/Admin/{1}/{0}.cshtml",
            "/Views/Manager/{1}/{0}.cshtml",
            "/Views/Customer/{1}/{0}.cshtml",
            "/Views/Auth/{1}/{0}.cshtml",
            "/Views/Public/{1}/{0}.cshtml",
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
