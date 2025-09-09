using Domain.Models.Entities;

public class CourseUser
{
    public int Id { get; set; } // Primary key

    public string UserId { get; set; } = null!;
    public int CourseId { get; set; }
    public bool IsTeacher { get; set; }

    public Course Course { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}