using Microsoft.EntityFrameworkCore;

namespace SchemaMagic.Examples;

public class TestDbContext : DbContext
{
    // Core entities with extensive properties
    public DbSet<User> Users { get; set; }
    public DbSet<Company> Companies { get; set; }
    public DbSet<Department> Departments { get; set; }
    public DbSet<Project> Projects { get; set; }
    public DbSet<Task> Tasks { get; set; }
    public DbSet<Document> Documents { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<Attachment> Attachments { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<TaskTag> TaskTags { get; set; }
    public DbSet<UserProject> UserProjects { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure many-to-many relationships
        modelBuilder.Entity<TaskTag>()
            .HasKey(tt => new { tt.TaskId, tt.TagId });

        modelBuilder.Entity<TaskTag>()
            .HasOne(tt => tt.Task)
            .WithMany(t => t.TaskTags)
            .HasForeignKey(tt => tt.TaskId);

        modelBuilder.Entity<TaskTag>()
            .HasOne(tt => tt.Tag)
            .WithMany(t => t.TaskTags)
            .HasForeignKey(tt => tt.TagId);

        modelBuilder.Entity<UserProject>()
            .HasKey(up => new { up.UserId, up.ProjectId });

        modelBuilder.Entity<UserProject>()
            .HasOne(up => up.User)
            .WithMany(u => u.UserProjects)
            .HasForeignKey(up => up.UserId);

        modelBuilder.Entity<UserProject>()
            .HasOne(up => up.Project)
            .WithMany(p => p.UserProjects)
            .HasForeignKey(up => up.ProjectId);

        // Configure foreign key relationships
        modelBuilder.Entity<Department>()
            .HasOne(d => d.Company)
            .WithMany(c => c.Departments)
            .HasForeignKey(d => d.CompanyId);

        modelBuilder.Entity<User>()
            .HasOne(u => u.Department)
            .WithMany(d => d.Employees)
            .HasForeignKey(u => u.DepartmentId);

        modelBuilder.Entity<Project>()
            .HasOne(p => p.Company)
            .WithMany(c => c.Projects)
            .HasForeignKey(p => p.CompanyId);

        modelBuilder.Entity<Project>()
            .HasOne(p => p.ProjectManager)
            .WithMany(u => u.ManagedProjects)
            .HasForeignKey(p => p.ProjectManagerId);

        modelBuilder.Entity<Task>()
            .HasOne(t => t.Project)
            .WithMany(p => p.Tasks)
            .HasForeignKey(t => t.ProjectId);

        modelBuilder.Entity<Task>()
            .HasOne(t => t.AssignedTo)
            .WithMany(u => u.AssignedTasks)
            .HasForeignKey(t => t.AssignedToId);

        modelBuilder.Entity<Task>()
            .HasOne(t => t.CreatedBy)
            .WithMany(u => u.CreatedTasks)
            .HasForeignKey(t => t.CreatedById);

        modelBuilder.Entity<Document>()
            .HasOne(d => d.Project)
            .WithMany(p => p.Documents)
            .HasForeignKey(d => d.ProjectId);

        modelBuilder.Entity<Document>()
            .HasOne(d => d.UploadedBy)
            .WithMany(u => u.UploadedDocuments)
            .HasForeignKey(d => d.UploadedById);

        modelBuilder.Entity<Comment>()
            .HasOne(c => c.Task)
            .WithMany(t => t.Comments)
            .HasForeignKey(c => c.TaskId);

        modelBuilder.Entity<Comment>()
            .HasOne(c => c.Author)
            .WithMany(u => u.Comments)
            .HasForeignKey(c => c.AuthorId);

        modelBuilder.Entity<Attachment>()
            .HasOne(a => a.Comment)
            .WithMany(c => c.Attachments)
            .HasForeignKey(a => a.CommentId);

        modelBuilder.Entity<Attachment>()
            .HasOne(a => a.UploadedBy)
            .WithMany(u => u.UploadedAttachments)
            .HasForeignKey(a => a.UploadedById);

        modelBuilder.Entity<AuditLog>()
            .HasOne(a => a.User)
            .WithMany(u => u.AuditLogs)
            .HasForeignKey(a => a.UserId);
    }
}

public class Company
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Industry { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;
    public string TaxId { get; set; } = string.Empty;
    public DateTime EstablishedDate { get; set; }
    public int EmployeeCount { get; set; }
    public decimal AnnualRevenue { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string TimeZone { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;

    // Navigation properties
    public ICollection<Department> Departments { get; set; } = new List<Department>();
    public ICollection<Project> Projects { get; set; } = new List<Project>();
}

public class Department
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Floor { get; set; } = string.Empty;
    public string Building { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public decimal Budget { get; set; }
    public string CostCenter { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string Notes { get; set; } = string.Empty;

    // Foreign keys
    public int CompanyId { get; set; }

    // Navigation properties
    public Company Company { get; set; } = null!;
    public ICollection<User> Employees { get; set; } = new List<User>();
}

public class User
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string MiddleName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Salt { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string MobilePhone { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string EmployeeId { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public DateTime HireDate { get; set; }
    public DateTime? TerminationDate { get; set; }
    public decimal Salary { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string EmergencyContactName { get; set; } = string.Empty;
    public string EmergencyContactPhone { get; set; } = string.Empty;
    public string Skills { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public string ProfileImageUrl { get; set; } = string.Empty;
    public string TimeZone { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsEmailVerified { get; set; }
    public bool IsPhoneVerified { get; set; }
    public DateTime LastLoginAt { get; set; }
    public string LastLoginIp { get; set; } = string.Empty;
    public int LoginAttempts { get; set; }
    public DateTime? LockedUntil { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Foreign keys
    public int? DepartmentId { get; set; }

    // Navigation properties
    public Department? Department { get; set; }
    public ICollection<UserProject> UserProjects { get; set; } = new List<UserProject>();
    public ICollection<Project> ManagedProjects { get; set; } = new List<Project>();
    public ICollection<Task> AssignedTasks { get; set; } = new List<Task>();
    public ICollection<Task> CreatedTasks { get; set; } = new List<Task>();
    public ICollection<Document> UploadedDocuments { get; set; } = new List<Document>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<Attachment> UploadedAttachments { get; set; } = new List<Attachment>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}

public class Project
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Phase { get; set; } = string.Empty;
    public decimal Budget { get; set; }
    public string Currency { get; set; } = string.Empty;
    public decimal ActualCost { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? ActualEndDate { get; set; }
    public int EstimatedHours { get; set; }
    public int ActualHours { get; set; }
    public decimal PercentComplete { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public string HealthStatus { get; set; } = string.Empty;
    public string Methodology { get; set; } = string.Empty;
    public string Repository { get; set; } = string.Empty;
    public string DocumentationUrl { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsPublic { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string Notes { get; set; } = string.Empty;

    // Foreign keys
    public int CompanyId { get; set; }
    public int? ProjectManagerId { get; set; }

    // Navigation properties
    public Company Company { get; set; } = null!;
    public User? ProjectManager { get; set; }
    public ICollection<UserProject> UserProjects { get; set; } = new List<UserProject>();
    public ICollection<Task> Tasks { get; set; } = new List<Task>();
    public ICollection<Document> Documents { get; set; } = new List<Document>();
}

public class Task
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int EstimatedHours { get; set; }
    public int ActualHours { get; set; }
    public int StoryPoints { get; set; }
    public decimal PercentComplete { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public string Resolution { get; set; } = string.Empty;
    public string AcceptanceCriteria { get; set; } = string.Empty;
    public string TestNotes { get; set; } = string.Empty;
    public string Branch { get; set; } = string.Empty;
    public string PullRequestUrl { get; set; } = string.Empty;
    public bool IsBlocked { get; set; }
    public string BlockedReason { get; set; } = string.Empty;
    public bool IsBillable { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Foreign keys
    public int ProjectId { get; set; }
    public int? AssignedToId { get; set; }
    public int CreatedById { get; set; }

    // Navigation properties
    public Project Project { get; set; } = null!;
    public User? AssignedTo { get; set; }
    public User CreatedBy { get; set; } = null!;
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<TaskTag> TaskTags { get; set; } = new List<TaskTag>();
}

public class Document
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string OriginalName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string Hash { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public bool IsLatestVersion { get; set; }
    public string Status { get; set; } = string.Empty;
    public string AccessLevel { get; set; } = string.Empty;
    public DateTime? ExpiryDate { get; set; }
    public int Downloads { get; set; }
    public bool IsEncrypted { get; set; }
    public string Keywords { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Foreign keys
    public int ProjectId { get; set; }
    public int UploadedById { get; set; }

    // Navigation properties
    public Project Project { get; set; } = null!;
    public User UploadedBy { get; set; } = null!;
}

public class Comment
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string FormattedContent { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsInternal { get; set; }
    public bool IsEdited { get; set; }
    public DateTime? EditedAt { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Foreign keys
    public int TaskId { get; set; }
    public int AuthorId { get; set; }

    // Navigation properties
    public Task Task { get; set; } = null!;
    public User Author { get; set; } = null!;
    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
}

public class Attachment
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string OriginalName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string Hash { get; set; } = string.Empty;
    public string ThumbnailPath { get; set; } = string.Empty;
    public bool IsImage { get; set; }
    public int? ImageWidth { get; set; }
    public int? ImageHeight { get; set; }
    public DateTime CreatedAt { get; set; }

    // Foreign keys
    public int CommentId { get; set; }
    public int UploadedById { get; set; }

    // Navigation properties
    public Comment Comment { get; set; } = null!;
    public User UploadedBy { get; set; } = null!;
}

public class Tag
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int UsageCount { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public ICollection<TaskTag> TaskTags { get; set; } = new List<TaskTag>();
}

public class TaskTag
{
    // Composite key properties
    public int TaskId { get; set; }
    public int TagId { get; set; }
    
    public DateTime TaggedAt { get; set; }
    public string TaggedBy { get; set; } = string.Empty;

    // Navigation properties
    public Task Task { get; set; } = null!;
    public Tag Tag { get; set; } = null!;
}

public class UserProject
{
    // Composite key properties
    public int UserId { get; set; }
    public int ProjectId { get; set; }
    
    public string Role { get; set; } = string.Empty;
    public string Permissions { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
    public DateTime? LeftAt { get; set; }
    public bool IsActive { get; set; }
    public decimal HourlyRate { get; set; }
    public string Currency { get; set; } = string.Empty;

    // Navigation properties
    public User User { get; set; } = null!;
    public Project Project { get; set; } = null!;
}

public class AuditLog
{
    public int Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public int? EntityId { get; set; }
    public string OldValues { get; set; } = string.Empty;
    public string NewValues { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Details { get; set; } = string.Empty;

    // Foreign keys
    public int? UserId { get; set; }

    // Navigation properties
    public User? User { get; set; }
}