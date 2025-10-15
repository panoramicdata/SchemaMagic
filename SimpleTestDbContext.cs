using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchemaMagic.SimpleTest;

public class SimpleTestDbContext : DbContext
{
	public DbSet<Company> Companies { get; set; }
	public DbSet<User> Users { get; set; }

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		// User self-referencing relationship for manager hierarchy
		modelBuilder.Entity<User>()
			.HasOne(u => u.Manager)
			.WithMany(u => u.DirectReports)
			.HasForeignKey(u => u.ManagerId)
			.OnDelete(DeleteBehavior.Restrict);

		base.OnModelCreating(modelBuilder);
	}
}

/// <summary>
/// Represents a business organization with employees and projects
/// </summary>
[Comment("Business organization entity tracking company information, employees, and corporate structure")]
public class Company
{
	/// <summary>
	/// Unique identifier for the company
	/// </summary>
	[Comment("Primary key - auto-generated unique identifier")]
	public int Id { get; set; }

	/// <summary>
	/// The official registered business name
	/// </summary>
	[Comment("Official company name as registered with business authorities")]
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// The industry sector the company operates in (e.g., Technology, Healthcare, Finance)
	/// </summary>
	[Comment("Industry classification - used for reporting and analysis")]
	public string Industry { get; set; } = string.Empty;

	/// <summary>
	/// Primary headquarters location
	/// </summary>
	[Comment("Primary office location city - main business address")]
	public string Location { get; set; } = string.Empty;

	[Comment("Year the company was established")]
	public int? FoundedYear { get; set; }

	/// <summary>
	/// Total number of employees worldwide
	/// </summary>
	public int? EmployeeCount { get; set; }

	[Comment("Annual revenue in USD")]
	public decimal? AnnualRevenue { get; set; }

	/// <summary>
	/// Indicates whether the company is currently active and operating
	/// </summary>
	[Comment("Active status flag - false for closed or inactive companies")]
	public bool IsActive { get; set; } = true;

	/// <summary>
	/// Date and time when the company record was created in the system
	/// </summary>
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

	[Comment("Last modification timestamp")]
	public DateTime? UpdatedAt { get; set; }

	/// <summary>
	/// Foreign key to primary contact person (optional 0..1 relationship)
	/// </summary>
	[Comment("Foreign key to primary contact User - may be null for new companies")]
	public int? PrimaryContactId { get; set; }

	/// <summary>
	/// Navigation property to primary contact user
	/// </summary>
	public User? PrimaryContact { get; set; }

	/// <summary>
	/// Collection of all employees working for this company
	/// </summary>
	public ICollection<User> Employees { get; set; } = new List<User>();
}

/// <summary>
/// System user representing an employee with authentication credentials and profile data
/// </summary>
[Comment("System user with authentication and profile information - central identity entity")]
public class User
{
	[Comment("Primary key - unique user identifier")]
	public int Id { get; set; }

	/// <summary>
	/// User's given name (first name)
	/// </summary>
	[Comment("Given name - required for user identification")]
	public string FirstName { get; set; } = string.Empty;

	/// <summary>
	/// User's family name (surname)
	/// </summary>
	[Comment("Family name/surname - required for formal communications")]
	public string LastName { get; set; } = string.Empty;

	/// <summary>
	/// Primary email address used for system login and communications
	/// </summary>
	[Comment("Primary email address - must be unique across all users")]
	public string Email { get; set; } = string.Empty;

	[Comment("Hashed password for authentication - never store plain text")]
	public string PasswordHash { get; set; } = string.Empty;

	/// <summary>
	/// User's job title or position within the company
	/// </summary>
	public string? JobTitle { get; set; }

	[Comment("Department name where user works")]
	public string? Department { get; set; }

	/// <summary>
	/// Direct manager's user ID
	/// </summary>
	[Comment("Foreign key to manager - references Users table")]
	public int? ManagerId { get; set; }

	[Comment("User's mobile phone number for 2FA and notifications")]
	public string? PhoneNumber { get; set; }

	/// <summary>
	/// Date when the user was hired by the company
	/// </summary>
	public DateTime? HireDate { get; set; }

	[Comment("User's date of birth for age verification")]
	public DateTime? DateOfBirth { get; set; }

	/// <summary>
	/// Indicates if the user account is currently active
	/// </summary>
	[Comment("Active account flag - false for terminated or suspended users")]
	public bool IsActive { get; set; } = true;

	[Comment("Email verification status - true when user confirms email")]
	public bool EmailVerified { get; set; } = false;

	/// <summary>
	/// Timestamp of the user's last successful login
	/// </summary>
	public DateTime? LastLoginAt { get; set; }

	[Comment("Account creation timestamp")]
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

	/// <summary>
	/// Last time the user profile was modified
	/// </summary>
	public DateTime? UpdatedAt { get; set; }

	/// <summary>
	/// Foreign key reference to the company this user belongs to
	/// </summary>
	[Comment("Foreign key to Companies - required relationship")]
	public int CompanyId { get; set; }

	/// <summary>
	/// Navigation property to the company this user works for
	/// </summary>
	[Comment("Navigation to parent Company entity")]
	public Company Company { get; set; } = null!;

	/// <summary>
	/// Navigation property to the manager (another User)
	/// </summary>
	public User? Manager { get; set; }

	/// <summary>
	/// Collection of users who report to this user
	/// </summary>
	public ICollection<User> DirectReports { get; set; } = new List<User>();
}