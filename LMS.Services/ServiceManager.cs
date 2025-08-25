using Service.Contracts;

namespace LMS.Services;

public class ServiceManager : IServiceManager
{
    private readonly Lazy<IAuthService> _authService;
    private readonly Lazy<ICourseService> _courseService;
    private readonly Lazy<IProjActivityService> _projActivityService;

    public IAuthService AuthService => _authService.Value;
    public ICourseService CourseService => _courseService.Value;

    public IProjActivityService ProjActivityService => _projActivityService.Value;

    public ServiceManager(
        Lazy<IAuthService> authService,
        Lazy<ICourseService> courseService,
        Lazy<IProjActivityService> projActivityService)
    {
        _authService = authService;
        _courseService = courseService;
        _projActivityService = projActivityService;
    }
}