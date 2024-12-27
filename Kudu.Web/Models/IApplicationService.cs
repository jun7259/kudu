using Kudu.SiteManagement;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kudu.Web.Models
{
    public interface IApplicationService
    {
        Task AddApplication(string name);
        Task<bool> DeleteApplication(string name);
        IEnumerable<string> GetApplications();
        IApplication GetApplication(string name);
        bool RemoveLiveSiteBinding(string name, string siteBinding);
        bool RemoveServiceSiteBinding(string name, string siteBinding);
        bool AddSiteBinding(string name, KuduBinding binding);
        bool AddVirtualApplication(string name, string virutalPath, string physicalPath);
        bool SetVirtualApplication(string name, IDictionary<string, string> virtualApplications);
        bool RemoveVirtualApplication(string name, string virutalPath);
    }
}
