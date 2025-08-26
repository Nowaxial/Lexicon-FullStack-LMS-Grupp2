using Domain.Contracts.Repositories;
using LMS.Infractructure.Data;

public class UnitOfWork : IUnitOfWork, IDisposable
{
    private readonly ApplicationDbContext _context;

    private readonly Lazy<ICourseRepository> _courseRepository;

    public ICourseRepository CourseRepository => _courseRepository.Value;

    public UnitOfWork(ApplicationDbContext context, Lazy<ICourseRepository> courseRepository)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _courseRepository = courseRepository ?? throw new ArgumentNullException(nameof(courseRepository));
    }


    public async Task CompleteAsync() => await _context.SaveChangesAsync();
    public void Dispose() => _context.Dispose();
}