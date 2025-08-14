using Service.Contracts;

namespace LMS.Services;

public class ServiceManager : IServiceManager
{
    private Lazy<IAuthService> authService;
    public IAuthService AuthService => authService.Value;

    public ServiceManager(Lazy<IAuthService> authService)
    {
        this.authService = authService;
    }
}
