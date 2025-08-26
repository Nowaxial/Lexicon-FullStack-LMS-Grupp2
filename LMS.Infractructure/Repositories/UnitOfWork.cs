using Domain.Contracts.Repositories;
using LMS.Infractructure.Data;

namespace LMS.Infractructure.Repositories;

public class UnitOfWork : IUnitOfWork, IDisposible
{
    private readonly ApplicationDbContext _context;

    private readonly Lazy<IProjActivityRepository> _projActivityRepository;

    private readonly Lazy<ICourseRepository> _courseRepository;

    public ICourseRepository CourseRepository => _courseRepository.Value;
    public IProjActivityRepository ProjActivityRepository => _projActivityRepository.Value;

    public UnitOfWork(ApplicationDbContext context, Lazy<ICourseRepository> courseRepository, Lazy<IProjActivityRepository> projActivityRepository)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _courseRepository = courseRepository ?? throw new ArgumentNullException(nameof(courseRepository));
        _projActivityRepository = projActivityRepository ?? throw new ArgumentNullException(nameof(projActivityRepository));
    }

    public async Task CompleteAsync() => await _context.SaveChangesAsync();

    public void Dispose() => _context.Dispose();
}



