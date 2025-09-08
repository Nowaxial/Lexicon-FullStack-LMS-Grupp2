namespace LMS.Shared.DTOs.UsersDtos
{
    public record UserDto
    {
        public required string Id { get; init; }
        public string? FirstName { get; init; }
        public string? LastName { get; init; }

        // Computed property (always returns FirstName + LastName)
        public string FullName => $"{FirstName} {LastName}".Trim();

        public string? UserName { get; init; }
        public string? Email { get; init; }
        public List<string> Roles { get; set; } = new();

        public bool? IsTeacher { get; init; }
    }
}