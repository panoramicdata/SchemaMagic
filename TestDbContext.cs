using System.Linq.Expressions;
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
        ConfigureManyToMany(modelBuilder);
        ConfigureOrganizationRelationships(modelBuilder);
        ConfigureWorkRelationships(modelBuilder);
    }

    private static void ConfigureManyToMany(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TaskTag>().HasKey(tt => new { tt.TaskId, tt.TagId });
        OneToMany<TaskTag, Task>(modelBuilder, tt => tt.Task, t => t.TaskTags, tt => tt.TaskId);
        OneToMany<TaskTag, Tag>(modelBuilder, tt => tt.Tag, t => t.TaskTags, tt => tt.TagId);

        modelBuilder.Entity<UserProject>().HasKey(up => new { up.UserId, up.ProjectId });
        OneToMany<UserProject, User>(modelBuilder, up => up.User, u => u.UserProjects, up => up.UserId);
        OneToMany<UserProject, Project>(modelBuilder, up => up.Project, p => p.UserProjects, up => up.ProjectId);
    }

    private static void ConfigureOrganizationRelationships(ModelBuilder modelBuilder)
    {
        OneToMany<Department, Company>(modelBuilder, d => d.Company, c => c.Departments, d => d.CompanyId);
        OneToMany<User, Department>(modelBuilder, u => u.Department, d => d.Employees, u => u.DepartmentId);
        OneToMany<Project, Company>(modelBuilder, p => p.Company, c => c.Projects, p => p.CompanyId);
        OneToMany<Project, User>(modelBuilder, p => p.ProjectManager, u => u.ManagedProjects, p => p.ProjectManagerId);
    }

    private static void ConfigureWorkRelationships(ModelBuilder modelBuilder)
    {
        OneToMany<Task, Project>(modelBuilder, t => t.Project, p => p.Tasks, t => t.ProjectId);
        OneToMany<Task, User>(modelBuilder, t => t.AssignedTo, u => u.AssignedTasks, t => t.AssignedToId);
        OneToMany<Task, User>(modelBuilder, t => t.CreatedBy, u => u.CreatedTasks, t => t.CreatedById);
        OneToMany<Document, Project>(modelBuilder, d => d.Project, p => p.Documents, d => d.ProjectId);
        OneToMany<Document, User>(modelBuilder, d => d.UploadedBy, u => u.UploadedDocuments, d => d.UploadedById);
        OneToMany<Comment, Task>(modelBuilder, c => c.Task, t => t.Comments, c => c.TaskId);
        OneToMany<Comment, User>(modelBuilder, c => c.Author, u => u.Comments, c => c.AuthorId);
        OneToMany<Attachment, Comment>(modelBuilder, a => a.Comment, c => c.Attachments, a => a.CommentId);
        OneToMany<Attachment, User>(modelBuilder, a => a.UploadedBy, u => u.UploadedAttachments, a => a.UploadedById);
        OneToMany<AuditLog, User>(modelBuilder, a => a.User, u => u.AuditLogs, a => a.UserId);
    }

    private static void OneToMany<TDependent, TPrincipal>(
        ModelBuilder modelBuilder,
        Expression<Func<TDependent, TPrincipal?>> navigation,
        Expression<Func<TPrincipal, IEnumerable<TDependent>?>> inverse,
        Expression<Func<TDependent, object?>> foreignKey)
        where TDependent : class
        where TPrincipal : class
    {
        modelBuilder.Entity<TDependent>()
            .HasOne(navigation)
            .WithMany(inverse)
            .HasForeignKey(foreignKey);
    }
}
/// <summary>
/// Represents a business organization that manages departments and projects
/// </summary>
[Comment("Business organization entity - root tenant for multi-company operations")]
public class Company
{
    [Comment("Primary key - unique company identifier")]
    public int Id { get; set; }

    /// <summary>
    /// Official company name as registered
    /// </summary>
    [Comment("Official business name")]
    public string Name { get; set; } = string.Empty;

    [Comment("Industry sector classification")]
    public string Industry { get; set; } = string.Empty;

    /// <summary>
    /// Street address of company headquarters
    /// </summary>
    public string Address { get; set; } = string.Empty;

    [Comment("City where company is headquartered")]
    public string City { get; set; } = string.Empty;

    /// <summary>
    /// State or province
    /// </summary>
    public string State { get; set; } = string.Empty;

    [Comment("ZIP or postal code")]
    public string PostalCode { get; set; } = string.Empty;

    /// <summary>
    /// Country of incorporation
    /// </summary>
    [Comment("Country code - ISO 3166-1 alpha-2")]
    public string Country { get; set; } = string.Empty;

    [Comment("Main phone number")]
    public string Phone { get; set; } = string.Empty;

    /// <summary>
    /// Primary contact email for the company
    /// </summary>
    public string Email { get; set; } = string.Empty;

    [Comment("Corporate website URL")]
    public string Website { get; set; } = string.Empty;

    /// <summary>
    /// Tax identification number
    /// </summary>
    [Comment("EIN or Tax ID - used for compliance")]
    public string TaxId { get; set; } = string.Empty;

    [Comment("Date company was established")]
    public DateTime EstablishedDate { get; set; }

    /// <summary>
    /// Total headcount across all departments
    /// </summary>
    public int EmployeeCount { get; set; }

    [Comment("Annual revenue in base currency")]
    public decimal AnnualRevenue { get; set; }

    /// <summary>
    /// Currency code for financial transactions
    /// </summary>
    [Comment("ISO 4217 currency code (USD, EUR, GBP, etc.)")]
    public string Currency { get; set; } = string.Empty;

    [Comment("IANA timezone identifier")]
    public string TimeZone { get; set; } = string.Empty;

    /// <summary>
    /// Indicates if company is operational
    /// </summary>
    [Comment("Active status - false for dissolved companies")]
    public bool IsActive { get; set; }

    [Comment("Record creation timestamp")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last modification time
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    [Comment("Username who created this record")]
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// Username who last updated this record
    /// </summary>
    public string UpdatedBy { get; set; } = string.Empty;

    /// <summary>
    /// All departments belonging to this company
    /// </summary>
    public ICollection<Department> Departments { get; set; } = new List<Department>();

    /// <summary>
    /// All projects managed by this company
    /// </summary>
    public ICollection<Project> Projects { get; set; } = new List<Project>();
}

/// <summary>
/// Organizational unit within a company
/// </summary>
[Comment("Department entity - organizational subdivision with budget and staff")]
public class Department
{
    [Comment("Primary key")]
    public int Id { get; set; }

    /// <summary>
    /// Department name
    /// </summary>
    [Comment("Department display name")]
    public string Name { get; set; } = string.Empty;

    [Comment("Purpose and responsibilities of department")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Short code for department
    /// </summary>
    [Comment("Department code - used in accounting")]
    public string Code { get; set; } = string.Empty;

    [Comment("Office location or site name")]
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// Floor number in building
    /// </summary>
    public string Floor { get; set; } = string.Empty;

    [Comment("Building name or number")]
    public string Building { get; set; } = string.Empty;

    /// <summary>
    /// Department phone extension
    /// </summary>
    public string Phone { get; set; } = string.Empty;

    [Comment("Department contact email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Annual budget allocation
    /// </summary>
    [Comment("Budget in company currency")]
    public decimal Budget { get; set; }

    [Comment("GL account cost center code")]
    public string CostCenter { get; set; } = string.Empty;

    /// <summary>
    /// Whether department is currently active
    /// </summary>
    public bool IsActive { get; set; }

    [Comment("Record creation date")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    [Comment("Additional notes or comments")]
    public string Notes { get; set; } = string.Empty;

    /// <summary>
    /// Foreign key to parent company
    /// </summary>
    [Comment("FK to Companies")]
    public int CompanyId { get; set; }

    /// <summary>
    /// Navigation to parent company
    /// </summary>
    public Company Company { get; set; } = null!;

    /// <summary>
    /// All employees in this department
    /// </summary>
    public ICollection<User> Employees { get; set; } = new List<User>();
}

/// <summary>
/// System user with authentication and profile information
/// </summary>
[Comment("User entity - authentication and employee profile data")]
public class User
{
    [Comment("Primary key - unique user ID")]
    public int Id { get; set; }

    /// <summary>
    /// User's first name
    /// </summary>
    [Comment("Given name")]
    public string FirstName { get; set; } = string.Empty;

    [Comment("Family name or surname")]
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Middle name or initial
    /// </summary>
    public string MiddleName { get; set; } = string.Empty;

    [Comment("Primary email - used for login")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Unique username for authentication
    /// </summary>
    [Comment("Login username - must be unique")]
    public string Username { get; set; } = string.Empty;

    [Comment("Bcrypt hashed password")]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Salt used for password hashing
    /// </summary>
    public string Salt { get; set; } = string.Empty;

    [Comment("Office phone number")]
    public string Phone { get; set; } = string.Empty;

    /// <summary>
    /// Mobile phone for SMS and 2FA
    /// </summary>
    [Comment("Mobile number for notifications")]
    public string MobilePhone { get; set; } = string.Empty;

    [Comment("Job title or position")]
    public string JobTitle { get; set; } = string.Empty;

    /// <summary>
    /// Internal employee identification number
    /// </summary>
    [Comment("Employee ID badge number")]
    public string EmployeeId { get; set; } = string.Empty;

    [Comment("Date of birth - for age verification")]
    public DateTime? DateOfBirth { get; set; }

    /// <summary>
    /// Employment start date
    /// </summary>
    [Comment("Date employee was hired")]
    public DateTime HireDate { get; set; }

    [Comment("Date employment ended - null if active")]
    public DateTime? TerminationDate { get; set; }

    /// <summary>
    /// Annual salary amount
    /// </summary>
    [Comment("Salary in specified currency")]
    public decimal Salary { get; set; }

    [Comment("Salary currency code")]
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// Home street address
    /// </summary>
    public string Address { get; set; } = string.Empty;

    [Comment("City of residence")]
    public string City { get; set; } = string.Empty;

    /// <summary>
    /// State or province
    /// </summary>
    public string State { get; set; } = string.Empty;

    [Comment("ZIP or postal code")]
    public string PostalCode { get; set; } = string.Empty;

    /// <summary>
    /// Country of residence
    /// </summary>
    [Comment("Country code")]
    public string Country { get; set; } = string.Empty;

    [Comment("Emergency contact name")]
    public string EmergencyContactName { get; set; } = string.Empty;

    /// <summary>
    /// Emergency contact phone
    /// </summary>
    [Comment("Emergency contact phone number")]
    public string EmergencyContactPhone { get; set; } = string.Empty;

    [Comment("Comma-separated skills list")]
    public string Skills { get; set; } = string.Empty;

    /// <summary>
    /// User biography or description
    /// </summary>
    public string Bio { get; set; } = string.Empty;

    [Comment("Avatar image URL")]
    public string ProfileImageUrl { get; set; } = string.Empty;

    /// <summary>
    /// User's preferred timezone
    /// </summary>
    [Comment("IANA timezone identifier")]
    public string TimeZone { get; set; } = string.Empty;

    [Comment("Preferred UI language code")]
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// Whether user account is active
    /// </summary>
    [Comment("Account active flag")]
    public bool IsActive { get; set; }

    [Comment("Email verified status")]
    public bool IsEmailVerified { get; set; }

    /// <summary>
    /// Phone number verification status
    /// </summary>
    public bool IsPhoneVerified { get; set; }

    [Comment("Last successful login timestamp")]
    public DateTime LastLoginAt { get; set; }

    /// <summary>
    /// IP address of last login
    /// </summary>
    [Comment("Last login IP address")]
    public string LastLoginIp { get; set; } = string.Empty;

    [Comment("Failed login attempt counter")]
    public int LoginAttempts { get; set; }

    /// <summary>
    /// Account lockout expiration
    /// </summary>
    [Comment("Lockout end time - null if not locked")]
    public DateTime? LockedUntil { get; set; }

    [Comment("Record created timestamp")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last profile update time
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Foreign key to department
    /// </summary>
    [Comment("FK to Departments")]
    public int? DepartmentId { get; set; }

    /// <summary>
    /// Navigation to department
    /// </summary>
    public Department? Department { get; set; }

    /// <summary>
    /// Project assignments for this user
    /// </summary>
    public ICollection<UserProject> UserProjects { get; set; } = new List<UserProject>();

    /// <summary>
    /// Projects where user is manager
    /// </summary>
    public ICollection<Project> ManagedProjects { get; set; } = new List<Project>();

    /// <summary>
    /// Tasks assigned to this user
    /// </summary>
    public ICollection<Task> AssignedTasks { get; set; } = new List<Task>();

    /// <summary>
    /// Tasks created by this user
    /// </summary>
    public ICollection<Task> CreatedTasks { get; set; } = new List<Task>();

    /// <summary>
    /// Documents uploaded by user
    /// </summary>
    public ICollection<Document> UploadedDocuments { get; set; } = new List<Document>();

    /// <summary>
    /// Comments authored by user
    /// </summary>
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();

    /// <summary>
    /// Attachments uploaded by user
    /// </summary>
    public ICollection<Attachment> UploadedAttachments { get; set; } = new List<Attachment>();

    /// <summary>
    /// Audit trail entries for user actions
    /// </summary>
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}

/// <summary>
/// Project tracking entity with budget and timeline
/// </summary>
[Comment("Project entity - tracks initiatives with budget, timeline, and deliverables")]
public class Project
{
    [Comment("Primary key")]
    public int Id { get; set; }

    /// <summary>
    /// Project display name
    /// </summary>
    [Comment("Project name")]
    public string Name { get; set; } = string.Empty;

    [Comment("Detailed project description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Short project code
    /// </summary>
    [Comment("Project code - used in tracking")]
    public string Code { get; set; } = string.Empty;

    [Comment("Priority level - High, Medium, Low")]
    public string Priority { get; set; } = string.Empty;

    /// <summary>
    /// Current project status
    /// </summary>
    [Comment("Status - Planning, Active, On Hold, Completed, Cancelled")]
    public string Status { get; set; } = string.Empty;

    [Comment("Current project phase")]
    public string Phase { get; set; } = string.Empty;

    /// <summary>
    /// Approved budget amount
    /// </summary>
    [Comment("Total budget allocation")]
    public decimal Budget { get; set; }

    [Comment("Budget currency code")]
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// Actual money spent
    /// </summary>
    [Comment("Actual costs incurred")]
    public decimal ActualCost { get; set; }

    [Comment("Planned start date")]
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Planned end date
    /// </summary>
    [Comment("Target completion date")]
    public DateTime? EndDate { get; set; }

    [Comment("Actual completion date")]
    public DateTime? ActualEndDate { get; set; }

    /// <summary>
    /// Estimated hours to complete
    /// </summary>
    [Comment("Estimated effort in hours")]
    public int EstimatedHours { get; set; }

    [Comment("Actual hours spent")]
    public int ActualHours { get; set; }

    /// <summary>
    /// Completion percentage (0-100)
    /// </summary>
    [Comment("Percent complete - 0 to 100")]
    public decimal PercentComplete { get; set; }

    [Comment("Risk assessment - Low, Medium, High, Critical")]
    public string RiskLevel { get; set; } = string.Empty;

    /// <summary>
    /// Overall project health
    /// </summary>
    [Comment("Health status - On Track, At Risk, Off Track")]
    public string HealthStatus { get; set; } = string.Empty;

    [Comment("Development methodology - Agile, Waterfall, etc.")]
    public string Methodology { get; set; } = string.Empty;

    /// <summary>
    /// Source control repository URL
    /// </summary>
    [Comment("Git repository URL")]
    public string Repository { get; set; } = string.Empty;

    [Comment("Documentation wiki or site URL")]
    public string DocumentationUrl { get; set; } = string.Empty;

    /// <summary>
    /// Whether project is active
    /// </summary>
    [Comment("Active status flag")]
    public bool IsActive { get; set; }

    [Comment("Public visibility flag")]
    public bool IsPublic { get; set; }

    /// <summary>
    /// Project creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    [Comment("Last update timestamp")]
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Additional project notes
    /// </summary>
    public string Notes { get; set; } = string.Empty;

    /// <summary>
    /// Foreign key to company
    /// </summary>
    [Comment("FK to Companies")]
    public int CompanyId { get; set; }

    /// <summary>
    /// Foreign key to project manager
    /// </summary>
    [Comment("FK to Users - project manager")]
    public int? ProjectManagerId { get; set; }

    /// <summary>
    /// Navigation to company
    /// </summary>
    public Company Company { get; set; } = null!;

    /// <summary>
    /// Navigation to project manager
    /// </summary>
    public User? ProjectManager { get; set; }

    /// <summary>
    /// Team members assigned to project
    /// </summary>
    public ICollection<UserProject> UserProjects { get; set; } = new List<UserProject>();

    /// <summary>
    /// All tasks in this project
    /// </summary>
    public ICollection<Task> Tasks { get; set; } = new List<Task>();

    /// <summary>
    /// Documents related to project
    /// </summary>
    public ICollection<Document> Documents { get; set; } = new List<Document>();
}

/// <summary>
/// Work item or task within a project
/// </summary>
[Comment("Task entity - individual work items with assignments and tracking")]
public class Task
{
    [Comment("Primary key")]
    public int Id { get; set; }

    /// <summary>
    /// Task title or summary
    /// </summary>
    [Comment("Task title")]
    public string Title { get; set; } = string.Empty;

    [Comment("Detailed task description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Current task status
    /// </summary>
    [Comment("Status - New, In Progress, Review, Done, Cancelled")]
    public string Status { get; set; } = string.Empty;

    [Comment("Priority - Critical, High, Medium, Low")]
    public string Priority { get; set; } = string.Empty;

    /// <summary>
    /// Task type classification
    /// </summary>
    [Comment("Type - Bug, Feature, Enhancement, Research")]
    public string Type { get; set; } = string.Empty;

    [Comment("Category for grouping tasks")]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Estimated hours to complete
    /// </summary>
    [Comment("Time estimate in hours")]
    public int EstimatedHours { get; set; }

    [Comment("Actual time spent in hours")]
    public int ActualHours { get; set; }

    /// <summary>
    /// Story points for agile estimation
    /// </summary>
    [Comment("Agile story points")]
    public int StoryPoints { get; set; }

    [Comment("Completion percentage - 0 to 100")]
    public decimal PercentComplete { get; set; }

    /// <summary>
    /// Work start date
    /// </summary>
    [Comment("Task start date")]
    public DateTime? StartDate { get; set; }

    [Comment("Target due date")]
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// Actual completion date
    /// </summary>
    [Comment("Date task was completed")]
    public DateTime? CompletedDate { get; set; }

    [Comment("Resolution notes")]
    public string Resolution { get; set; } = string.Empty;

    /// <summary>
    /// Definition of done criteria
    /// </summary>
    [Comment("Acceptance criteria for completion")]
    public string AcceptanceCriteria { get; set; } = string.Empty;

    [Comment("Testing notes and results")]
    public string TestNotes { get; set; } = string.Empty;

    /// <summary>
    /// Git branch name for this task
    /// </summary>
    [Comment("Feature branch name")]
    public string Branch { get; set; } = string.Empty;

    [Comment("Pull request URL")]
    public string PullRequestUrl { get; set; } = string.Empty;

    /// <summary>
    /// Whether task is blocked
    /// </summary>
    [Comment("Blocked status flag")]
    public bool IsBlocked { get; set; }

    [Comment("Reason task is blocked")]
    public string BlockedReason { get; set; } = string.Empty;

    /// <summary>
    /// Whether time is billable to client
    /// </summary>
    [Comment("Billable flag for time tracking")]
    public bool IsBillable { get; set; }

    [Comment("Task creation timestamp")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Foreign key to project
    /// </summary>
    [Comment("FK to Projects")]
    public int ProjectId { get; set; }

    /// <summary>
    /// Foreign key to assigned user
    /// </summary>
    [Comment("FK to Users - assignee")]
    public int? AssignedToId { get; set; }

    /// <summary>
    /// Foreign key to creator
    /// </summary>
    [Comment("FK to Users - creator")]
    public int CreatedById { get; set; }

    /// <summary>
    /// Navigation to project
    /// </summary>
    public Project Project { get; set; } = null!;

    /// <summary>
    /// Navigation to assigned user
    /// </summary>
    public User? AssignedTo { get; set; }

    /// <summary>
    /// Navigation to creator
    /// </summary>
    public User CreatedBy { get; set; } = null!;

    /// <summary>
    /// Comments on this task
    /// </summary>
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();

    /// <summary>
    /// Tags applied to task
    /// </summary>
    public ICollection<TaskTag> TaskTags { get; set; } = new List<TaskTag>();
}
