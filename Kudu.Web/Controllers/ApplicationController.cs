﻿using Kudu.Client.Infrastructure;
using Kudu.SiteManagement;
using Kudu.SiteManagement.Certificates;
using Kudu.SiteManagement.Configuration.Section;
using Kudu.SiteManagement.Context;
using Kudu.Web.Infrastructure;
using Kudu.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Kudu.Web.Controllers
{
    public class ApplicationController : Controller
    {
        private readonly IApplicationService _applicationService;
        private readonly KuduEnvironment _environment;
        private readonly IKuduContext _context;
        private readonly ICertificateSearcher _certificates;
        private readonly ICredentialProvider _credentialProvider;

        public ApplicationController(IApplicationService applicationService,
                                     ICredentialProvider credentialProvider,
                                     KuduEnvironment environment,
                                     IKuduContext context,
                                     ICertificateSearcher certificates)
        {
            _applicationService = applicationService;
            _credentialProvider = credentialProvider;
            _environment = environment;
            _context = context;
            _certificates = certificates;
        }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            ViewBag.showAdmingWarning = !_environment.IsAdmin && _environment.RunningAgainstLocalKuduService;
            base.OnActionExecuting(filterContext);
        }

        public ViewResult Index()
        {
            var applications = (from name in _applicationService.GetApplications()
                                orderby name
                                select name).ToList();

            return View(applications);
        }

        public Task<ActionResult> Details(string slug)
        {
            return GetApplicationView("settings", "Details", slug);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<JsonResult> CreateProject(string name, IDictionary<string, string> modulePaths)
        {
            string slug = name.GenerateSlug();
            try
            {
                await _applicationService.AddApplication(slug);
                _applicationService.SetVirtualApplication(slug, modulePaths);
                var application = _applicationService.GetApplication(slug);
                ICredentials credentials = _credentialProvider.GetCredentials();
                var repositoryInfo = await application.GetRepositoryInfo(credentials);
                var gitUrl = "";
                if (repositoryInfo != null) gitUrl = repositoryInfo.GitUrl.ToString();
                return Json(new
                {
                    Data = new
                    {
                        Name = application.Name,
                        ServiceUrl = application.ServiceUrl,
                        SiteUrl = application.SiteUrl,
                        gitUrl = gitUrl
                    },
                    Message = "",
                });
            }
            catch (SiteExistsException)
            {
                return Json(new
                {
                    Mesaage = "Site already exists",
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    Mesaage = ex.Message,
                });
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public JsonResult UpdateProject(string name, IDictionary<string, string> modulePaths)
        {
            try
            {
                IApplication application = _applicationService.GetApplication(name);

                if (application == null)
                {
                    return Json(
                        new { Mesaage = "Site not found" }
                        );
                }

                _applicationService.SetVirtualApplication(name, modulePaths);

                return Json(new
                {
                    Data = application,
                    Message = "",
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    Mesaage = ex.Message,
                });
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<JsonResult> DeleteProject(string name)
        {
            try
            {
                IApplication application = _applicationService.GetApplication(name);

                if (application == null)
                {
                    return Json(
                        new { Mesaage = "Site not found" }
                        );
                }

                await _applicationService.DeleteApplication(name);

                return Json(new
                {
                    Message = "",
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    Mesaage = ex.Message,
                });
            }
        }

        [HttpGet]
        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(string name)
        {
            string slug = name.GenerateSlug();

            try
            {
                await _applicationService.AddApplication(slug);

                return RedirectToAction("Details", new { slug });
            }
            catch (SiteExistsException)
            {
                ModelState.AddModelError("Name", "Site already exists");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
            }

            return View("Create");
        }

        [HttpPost]
        public async Task<ActionResult> Delete(string slug)
        {
            if (await _applicationService.DeleteApplication(slug))
            {
                return RedirectToAction("Index");
            }

            return HttpNotFound();
        }

        [HttpPost]
        [ActionName("add-custom-site-binding")]
        public async Task<ActionResult> AddCustomSiteBinding(string slug, string siteSchema, string siteIp, string sitePort, string siteHost, string siteRequireSni, string siteCertificate)
        {
            IApplication application = _applicationService.GetApplication(slug);

            if (application == null)
            {
                return HttpNotFound();
            }

            _applicationService.AddSiteBinding(slug, new KuduBinding
            {
                Schema = siteSchema.Equals("https://", StringComparison.OrdinalIgnoreCase) ? UriScheme.Https : UriScheme.Http,
                Ip = siteIp,
                Port = int.Parse(sitePort),
                Host = siteHost,
                Sni = bool.Parse(siteRequireSni),
                Certificate = siteCertificate,
                SiteType = SiteType.Live
            });

            return await GetApplicationView("settings", "Details", slug);
        }

        [HttpPost]
        [ActionName("remove-custom-site-binding")]
        public async Task<ActionResult> RemoveCustomSiteBinding(string slug, string siteBinding)
        {
            IApplication application = _applicationService.GetApplication(slug);

            if (application == null)
            {
                return HttpNotFound();
            }

            _applicationService.RemoveLiveSiteBinding(slug, siteBinding);

            return await GetApplicationView("settings", "Details", slug);
        }

        [HttpPost]
        [ActionName("add-service-site-binding")]
        public async Task<ActionResult> AddServiceSiteBinding(string slug, string siteSchema, string siteIp, string sitePort, string siteHost, string siteRequireSni, string siteCertificate)
        {
            IApplication application = _applicationService.GetApplication(slug);

            if (application == null)
            {
                return HttpNotFound();
            }

            _applicationService.AddSiteBinding(slug, new KuduBinding
            {
                Schema = siteSchema.Equals("https://", StringComparison.OrdinalIgnoreCase) ? UriScheme.Https : UriScheme.Http,
                Ip = siteIp,
                Port = int.Parse(sitePort),
                Host = siteHost,
                Sni = bool.Parse(siteRequireSni),
                Certificate = siteCertificate,
                SiteType = SiteType.Service
            });

            return await GetApplicationView("settings", "Details", slug);
        }

        [HttpPost]
        [ActionName("remove-service-site-binding")]
        public async Task<ActionResult> RemoveServiceSiteBinding(string slug, string siteBinding)
        {
            IApplication application = _applicationService.GetApplication(slug);

            if (application == null)
            {
                return HttpNotFound();
            }

            _applicationService.RemoveServiceSiteBinding(slug, siteBinding);

            return await GetApplicationView("settings", "Details", slug);
        }

        private async Task<ActionResult> GetApplicationView(string tab, string viewName, string slug)
        {
            var application = _applicationService.GetApplication(slug);

            ICredentials credentials = _credentialProvider.GetCredentials();
            var repositoryInfo = await application.GetRepositoryInfo(credentials);
            var appViewModel = new ApplicationViewModel(application, _context, _certificates.FindAll());
            appViewModel.RepositoryInfo = repositoryInfo;

            ViewBag.slug = slug;
            ViewBag.tab = tab;
            ViewBag.appName = appViewModel.Name;
            ViewBag.siteBinding = String.Empty;

            ModelState.Clear();

            return View(viewName, appViewModel);
        }

        [HttpPost]
        [ActionName("add-virtual-application")]
        public async Task<ActionResult> AddVirtualApplication(string slug, string virtualPath, string physicalPath)
        {
            IApplication application = _applicationService.GetApplication(slug);

            if (application == null)
            {
                return HttpNotFound();
            }

            _applicationService.AddVirtualApplication(slug, virtualPath, physicalPath);

            return await GetApplicationView("settings", "Details", slug);
        }

        [HttpPost]
        [ActionName("set-virtual-application")]
        public async Task<ActionResult> SetVirtualApplication(string slug, IDictionary<string, string> virtualapp)
        {
            IApplication application = _applicationService.GetApplication(slug);

            if (application == null)
            {
                return HttpNotFound();
            }

            _applicationService.SetVirtualApplication(slug, virtualapp);

            return await GetApplicationView("settings", "Details", slug);
        }

        [HttpPost]
        [ActionName("remove-virtual-application")]
        public async Task<ActionResult> RemoveVirtualApplication(string slug, string virtualPath)
        {
            IApplication application = _applicationService.GetApplication(slug);

            if (application == null)
            {
                return HttpNotFound();
            }

            _applicationService.RemoveVirtualApplication(slug, virtualPath);

            return await GetApplicationView("settings", "Details", slug);
        }

    }
}