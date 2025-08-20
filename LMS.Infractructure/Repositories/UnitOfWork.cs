using Domain.Contracts.Repositories;
using LMS.Infractructure.Data;
using LMS.Infractructure.Repositories;

public class UnitOfWork : IUnitOfWork, IDisposable
{
    private readonly ApplicationDbContext _context;

    private CourseRepository? _courseRepository;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public ICourseRepository CourseRepository =>
        _courseRepository ??= new CourseRepository(_context);

    public async Task CompleteAsync() => await _context.SaveChangesAsync();
    public void Dispose() => _context.Dispose();
}