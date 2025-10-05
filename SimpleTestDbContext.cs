using Microsoft.EntityFrameworkCore;

namespace SchemaMagic.SimpleTest;

public class SimpleTestDbContext : DbContext
{
    public DbSet<Company> Companies { get; set; }
    public DbSet<User> Users { get; set; }
}

public class Company
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Industry { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    
    // Navigation property
    public ICollection<User> Employees { get; set; } = new List<User>();
}

public class User
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    
    // Foreign key
    public int CompanyId { get; set; }
    
    // Navigation property  
    public Company Company { get; set; } = null!;
}