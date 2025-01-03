﻿using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kudu.SiteManagement
{
    public interface ISiteManager
    {
        IEnumerable<string> GetSites();
        Site GetSite(string applicationName);
        Task<Site> CreateSiteAsync(string applicationName);

        Task DeleteSiteAsync(string applicationName);
        bool AddSiteBinding(string applicationName, KuduBinding binding);
        bool RemoveSiteBinding(string applicationName, string siteBinding, SiteType siteType);
        Task ResetSiteContent(string applicationName);
        bool AddVirtualApplication(string applicationName, string virutalPath, string physicalPath);
        bool SetVirtualApplication(string applicationName, IDictionary<string, string> virtualApplications);
        bool RemoveVirtualApplication(string applicationName, string virutalPath);
    }
}
