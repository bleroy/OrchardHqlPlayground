using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Microsoft.Win32;
using NHibernate;
using NHibernate.Exceptions;
using NHibernate.Util;
using Orchard;
using Orchard.ContentManagement;
using Orchard.Data;
using Orchard.DisplayManagement;
using Orchard.Localization;
using Orchard.Security;
using Orchard.Settings;
using Orchard.UI.Admin;
using Orchard.UI.Navigation;
using Orchard.UI.Notify;

namespace Bleroy.HqlPlayground.Controllers {
    [Admin]
    public class HqlPlaygroundAdminController : Controller {
        private readonly ISessionLocator _sessionLocator;
        private readonly ISiteService _siteService;
        private readonly IContentManager _contentManager;
        private readonly INotifier _notifier;

        public HqlPlaygroundAdminController(
            ISessionLocator sessionLocator,
            IShapeFactory shapeFactory,
            IOrchardServices services,
            ISiteService siteService,
            IContentManager contentManager,
            INotifier notifier
            ) {
            _sessionLocator = sessionLocator;
            Shape = shapeFactory;
            Services = services;
            _siteService = siteService;
            _contentManager = contentManager;
            _notifier = notifier;
            T = NullLocalizer.Instance;
        }

        dynamic Shape { get; set; }
        public IOrchardServices Services { get; set; }
        public Localizer T { get; set; }

        public ActionResult Index() {
            return IndexImplementation(null, "content", 1);
        }

        [HttpPost]
        [ActionName("Index")]
        public ActionResult IndexPost(string q, string resultType = "content", int page = 1) {
            return IndexImplementation(q, resultType, page);
        }

        private ActionResult IndexImplementation(string q, string resultType, int page) {
            if (!Services.Authorizer.Authorize(StandardPermissions.SiteOwner, T("Not authorized to list queries")))
                return new HttpUnauthorizedResult();

            dynamic pagerShape = null;
            long countResult = -1;
            IEnumerable<IContent> contentResults = null;
            IEnumerable<object[]> recordResults = null;
            try {
                var pager = new Pager(_siteService.GetSiteSettings(), new PagerParameters {Page = page});

                if (!String.IsNullOrWhiteSpace(q)) {
                    var query = _sessionLocator.For(typeof (object))
                        .CreateQuery(q);

                    var queryResults = _sessionLocator.For(typeof (object))
                        .CreateQuery(q)
                        .SetFirstResult((pager.Page - 1)*pager.PageSize)
                        .SetMaxResults(pager.PageSize)
                        .List();

                    if (queryResults.Any()) {
                        switch (resultType) {
                            case "scalar":
                                countResult = (long) queryResults[0];
                                break;
                            case "records":
                                recordResults = queryResults.Cast<object[]>();
                                break;
                            default:
                                var ids = queryResults[0] is int
                                    ? queryResults.Cast<int>()
                                    : queryResults.Cast<object[]>().Select(r => (int) r[0]);
                                contentResults = _contentManager
                                    .GetMany<IContent>(ids, VersionOptions.Published, QueryHints.Empty);
                                break;
                        }
                    }
                    pagerShape = Shape.Pager(pager).TotalItemCount(query.List().Count);
                }
            }
            catch (QueryException qex) {
                NotifyOfException(qex);
            }
            catch (GenericADOException adoex) {
                NotifyOfException(adoex);
            }

            var viewModel = Shape.HqlPlayGroundIndexViewModel(
                Query: q,
                ResultType: String.IsNullOrWhiteSpace(resultType) ? "content" : resultType,
                Pager: pagerShape,
                CountResult: countResult,
                ContentResults: contentResults,
                RecordResults: recordResults
                );
            return View("Index", viewModel);
        }

        private void NotifyOfException(Exception qex) {
            if (qex.InnerException != null) {
                _notifier.Error(T("{0}<br/><br/>{1}<br/><br/>{2}",
                    qex.Message,
                    qex.InnerException.Message,
                    qex.StackTrace.Replace(Environment.NewLine, "<br/>")));
            }
            else {
                _notifier.Error(T("{0}<br/><br/>{1}",
                    qex.Message,
                    qex.StackTrace.Replace(Environment.NewLine, "<br/>")));
            }
        }
    }
}