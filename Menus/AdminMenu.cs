using Orchard.Localization;
using Orchard.Security;
using Orchard.UI.Navigation;

namespace Bleroy.HqlPlayground.Menus {
    public class AdminMenu : INavigationProvider {
        public Localizer T { get; set; }
        public string MenuName { get { return "admin"; } }

        public void GetNavigation(NavigationBuilder builder) {
            builder.Add(T("HQL Playground"), "42",
                menu => menu
                    .Action("Index", "HqlPlaygroundAdmin", new { area = "Bleroy.HqlPlayground" })
                    .Permission(StandardPermissions.SiteOwner)
                    .LocalNav());
        }
    }
}
