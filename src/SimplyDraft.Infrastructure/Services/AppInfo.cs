using System.Reflection;
using SimplyDraft.Core.Abstractions.Infrastructure;
using SimplyDraft.Infrastructure.Constants;

namespace SimplyDraft.Infrastructure.Services;

public sealed class AppInfo : IAppInfo
{
    public string Product {get;}
    public string Company {get;}
    public string Authors {get;}
    public string Copyright {get;}
    public string InfoVersion {get;}

    public AppInfo(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        Product = assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product
            ?? InfrastructureConstants.Service.AppInfo.ProductDefault;
        
        Company = assembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company
            ?? InfrastructureConstants.Service.AppInfo.CompanyDefault;
        
        Authors = assembly.GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(a => a.Key == "Authors")?.Value
            ?? InfrastructureConstants.Service.AppInfo.AuthorsDefault;
        
        Copyright = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright
            ?? InfrastructureConstants.Service.AppInfo.CopyrightDefault;
        
        InfoVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? "";
    }
}