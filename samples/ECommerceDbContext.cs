using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace SchemaMagic.Samples;

/// <summary>
/// E-commerce example demonstrating inheritance and complex relationships
/// </summary>
public class ECommerceDbContext : DbContext
{
	public DbSet<Product> Products { get; set; }
	public DbSet<Category> Categories { get; set; }
	public DbSet<Customer> Customers { get; set; }
	public DbSet<Order> Orders { get; set; }
	public DbSet<OrderItem> OrderItems { get; set; }
	public DbSet<Review> Reviews { get; set; }
	public DbSet<Address> Addresses { get; set; }

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		// Product inheritance hierarchy
		modelBuilder.Entity<Product>()
			.HasDiscriminator<string>("ProductType")
			.HasValue<PhysicalProduct>("Physical")
			.HasValue<DigitalProduct>("Digital");

		// Category self-referencing relationship
		modelBuilder.Entity<Category>()
			.HasOne(c => c.ParentCategory)
			.WithMany(c => c.SubCategories)
			.HasForeignKey(c => c.ParentCategoryId)
			.OnDelete(DeleteBehavior.Restrict);

		// Order relationship with optional approver (0..1)
		modelBuilder.Entity<Order>()
			.HasOne(o => o.ApprovedBy)
			.WithMany()
			.HasForeignKey(o => o.ApprovedById)
			.OnDelete(DeleteBehavior.Restrict);

		// Address types
		modelBuilder.Entity<Address>()
			.HasDiscriminator<string>("AddressType")
			.HasValue<BillingAddress>("Billing")
			.HasValue<ShippingAddress>("Shipping");

		base.OnModelCreating(modelBuilder);
	}
}

// Base entities
public abstract class AuditableEntity
{
	public int Id { get; set; }
	public DateTime CreatedAt { get; set; }
	public DateTime? UpdatedAt { get; set; }
	public string? CreatedBy { get; set; }
	public string? UpdatedBy { get; set; }
}

// Products with inheritance
public abstract class Product : AuditableEntity
{
	[Required, MaxLength(200)]
	public required string Name { get; set; }
	[MaxLength(1000)]
	public string? Description { get; set; }
	public decimal Price { get; set; }
	public bool IsActive { get; set; }
	public string? ImageUrl { get; set; }

	// Foreign keys
	public int CategoryId { get; set; }

	// Navigation properties
	public virtual Category Category { get; set; } = null!;
	public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
	public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
}

public class PhysicalProduct : Product
{
	public double Weight { get; set; }
	public string? Dimensions { get; set; }
	public int StockQuantity { get; set; }
	public string? Sku { get; set; }
	public bool RequiresShipping { get; set; }
}

public class DigitalProduct : Product
{
	public long FileSizeBytes { get; set; }
	public string? DownloadUrl { get; set; }
	public int MaxDownloads { get; set; }
	public DateTime? LicenseExpiryDate { get; set; }
}

public class Category : AuditableEntity
{
	[Required, MaxLength(100)]
	public required string Name { get; set; }
	[MaxLength(500)]
	public string? Description { get; set; }
	public string? IconUrl { get; set; }
	public int SortOrder { get; set; }

	// Self-referencing foreign key
	public int? ParentCategoryId { get; set; }

	// Navigation properties
	public virtual Category? ParentCategory { get; set; }
	public virtual ICollection<Category> SubCategories { get; set; } = new List<Category>();
	public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}

public class Customer : AuditableEntity
{
	[Required, MaxLength(100)]
	public required string FirstName { get; set; }
	[Required, MaxLength(100)]
	public required string LastName { get; set; }
	[Required, EmailAddress, MaxLength(200)]
	public required string Email { get; set; }
	[Phone, MaxLength(20)]
	public string? PhoneNumber { get; set; }
	public DateTime? DateOfBirth { get; set; }
	public bool IsActive { get; set; }

	// Foreign keys
	public int? PreferredShippingAddressId { get; set; } // 0..1 relationship - customer may not have a preferred address

	// Navigation properties
	public virtual Address? PreferredShippingAddress { get; set; }
	public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
	public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
	public virtual ICollection<Address> Addresses { get; set; } = new List<Address>();
}

public class Order : AuditableEntity
{
	public string OrderNumber { get; set; } = null!;
	public DateTime OrderDate { get; set; }
	public decimal SubTotal { get; set; }
	public decimal TaxAmount { get; set; }
	public decimal ShippingAmount { get; set; }
	public decimal TotalAmount { get; set; }
	public OrderStatus Status { get; set; }

	// Foreign keys
	public int CustomerId { get; set; }
	public int? BillingAddressId { get; set; } // 0..1 relationship - order may not have billing address yet
	public int? ShippingAddressId { get; set; } // 0..1 relationship - digital orders may not need shipping
	public int? ApprovedById { get; set; } // 0..1 relationship - order may not be approved yet

	// Navigation properties
	public virtual Customer Customer { get; set; } = null!;
	public virtual Address? BillingAddress { get; set; }
	public virtual Address? ShippingAddress { get; set; }
	public virtual Customer? ApprovedBy { get; set; } // Reference to user who approved the order
	public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}

public class OrderItem
{
	public int Id { get; set; }
	public int Quantity { get; set; }
	public decimal UnitPrice { get; set; }
	public decimal TotalPrice { get; set; }

	// Foreign keys
	public int OrderId { get; set; }
	public int ProductId { get; set; }

	// Navigation properties
	public virtual Order Order { get; set; } = null!;
	public virtual Product Product { get; set; } = null!;
}

public class Review : AuditableEntity
{
	public int Rating { get; set; } // 1-5 stars
	[MaxLength(1000)]
	public string? Comment { get; set; }
	public bool IsVerifiedPurchase { get; set; }
	public bool IsApproved { get; set; }

	// Foreign keys
	public int ProductId { get; set; }
	public int CustomerId { get; set; }

	// Navigation properties
	public virtual Product Product { get; set; } = null!;
	public virtual Customer Customer { get; set; } = null!;
}

// Address with inheritance
public abstract class Address : AuditableEntity
{
	[Required, MaxLength(200)]
	public required string Street { get; set; }
	[Required, MaxLength(100)]
	public required string City { get; set; }
	[Required, MaxLength(100)]
	public required string State { get; set; }
	[Required, MaxLength(20)]
	public required string PostalCode { get; set; }
	[Required, MaxLength(100)]
	public required string Country { get; set; }
	public bool IsDefault { get; set; }

	// Foreign keys
	public int CustomerId { get; set; }

	// Navigation properties
	public virtual Customer Customer { get; set; } = null!;
}

public class BillingAddress : Address
{
	public string? TaxId { get; set; }
	public string? CompanyName { get; set; }
}

public class ShippingAddress : Address
{
	public string? DeliveryInstructions { get; set; }
	public bool RequiresSignature { get; set; }
}

public enum OrderStatus
{
	Pending,
	Processing,
	Shipped,
	Delivered,
	Cancelled,
	Refunded
}
