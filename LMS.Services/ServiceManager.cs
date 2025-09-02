using Service.Contracts;

namespace LMS.Services;

public class ServiceManager : IServiceManager
{
    private readonly Lazy<IAuthService> _authService;
    private readonly Lazy<ICourseService> _courseService;
    private readonly Lazy<IUserService> _userService;
    private readonly Lazy<IModuleService> _moduleService;
    private readonly Lazy<IProjActivityService> _projActivityService;
    private readonly Lazy<INotificationService> _notificationService;

    public IAuthService AuthService => _authService.Value;
    public ICourseService CourseService => _courseService.Value;
    public IUserService UserService => _userService.Value;
    public IModuleService ModuleService => _moduleService.Value;
    public IProjActivityService ProjActivityService => _projActivityService.Value;
    public INotificationService NotificationService => _notificationService.Value;

    public ServiceManager(
        Lazy<IAuthService> authService,
        Lazy<ICourseService> courseService,
        Lazy<IUserService> userService,
        Lazy<IModuleService> moduleService,
        Lazy<IProjActivityService> projActivityService,
        Lazy<INotificationService> notificationService)
    {
        _authService = authService;
        _courseService = courseService;
        _userService = userService;
        _moduleService = moduleService;
        _projActivityService = projActivityService;
        _notificationService = notificationService;
    }
}
