namespace Service.Contracts;
public interface IServiceManager
{
    IAuthService AuthService { get; }
    ICourseService CourseService { get; }
    IProjActivityService ProjActivityService { get; }
}