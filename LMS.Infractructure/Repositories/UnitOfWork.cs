using Domain.Contracts.Repositories;
using LMS.Infractructure.Data;

namespace LMS.Infractructure.Repositories;

public class UnitOfWork : IUnitOfWork, IDisposable
{
    private readonly ApplicationDbContext _context;

    private readonly Lazy<IProjActivityRepository> _projActivityRepository;
    private readonly Lazy<ICourseRepository> _courseRepository;
    private readonly Lazy<ICourseUserRepository> _courseUserRepository;
    private readonly Lazy<IModuleRepository> _moduleRepository;

    public ICourseRepository CourseRepository => _courseRepository.Value;
    public ICourseUserRepository CourseUserRepository => _courseUserRepository.Value;
    public IModuleRepository ModuleRepository => _moduleRepository.Value;   
    public IProjActivityRepository ProjActivityRepository => _projActivityRepository.Value;
    public UnitOfWork(ApplicationDbContext context, Lazy<ICourseRepository> courseRepository, Lazy<ICourseUserRepository> courseUserRepository, Lazy<IModuleRepository> moduleRepository, Lazy<IProjActivityRepository> projActivityRepository)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _courseRepository = courseRepository ?? throw new ArgumentNullException(nameof(courseRepository));
        _courseUserRepository = courseUserRepository ?? throw new ArgumentNullException(nameof(courseUserRepository));
        _moduleRepository = moduleRepository ?? throw new ArgumentNullException(nameof(moduleRepository));
        _projActivityRepository = projActivityRepository ?? throw new ArgumentNullException(nameof(projActivityRepository));
    }

    public async Task CompleteAsync() => await _context.SaveChangesAsync();

    public void Dispose() => _context.Dispose();
}



