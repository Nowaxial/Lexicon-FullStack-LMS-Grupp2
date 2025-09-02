namespace Service.Contracts;
public interface IServiceManager
{
    IAuthService AuthService { get; }
    ICourseService CourseService { get; }
    IUserService UserService { get; }
    IModuleService ModuleService { get; }
    IProjActivityService ProjActivityService { get; }
    INotificationService NotificationService { get; }
}