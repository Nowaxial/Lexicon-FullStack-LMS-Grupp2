using Domain.Contracts.Repositories;
using LMS.Infractructure.Data;
using LMS.Infractructure.Repositories;

namespace LMS.Infractructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private readonly Lazy<ICourseRepository> _courseRepository;
    private readonly Lazy<IProjActivityRepository> _projActivityRepository;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _courseRepository = new Lazy<ICourseRepository>(() => new CourseRepository(_context));
        _projActivityRepository = new Lazy<IProjActivityRepository>(() => new ProjActivityRepository(_context));
    }

    public ICourseRepository CourseRepository => _courseRepository.Value;
    public IProjActivityRepository ProjActivityRepository => _projActivityRepository.Value;

    public async Task CompleteAsync() => await _context.SaveChangesAsync();

    public void Dispose() => _context.Dispose();
}
