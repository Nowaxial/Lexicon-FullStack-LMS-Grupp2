using Domain.Contracts.Repositories;
using LMS.Infractructure.Data;
using LMS.Infractructure.Repositories;

public class UnitOfWork : IUnitOfWork, IDisposable
{
    private readonly ApplicationDbContext _context;

    private readonly Lazy<ICourseRepository> _courseRepository;
    private readonly Lazy<IModuleRepository> _moduleRepository;

    public ICourseRepository CourseRepository => _courseRepository.Value;
    public IModuleRepository ModuleRepository => _moduleRepository.Value;
    public UnitOfWork(ApplicationDbContext context, Lazy<ICourseRepository> courseRepository, Lazy<IModuleRepository> moduleRepository)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _courseRepository = courseRepository ?? throw new ArgumentNullException(nameof(courseRepository));
        _moduleRepository = moduleRepository ?? throw new ArgumentNullException(nameof(moduleRepository));
    }

    public async Task CompleteAsync() => await _context.SaveChangesAsync();
    public void Dispose() => _context.Dispose();
}