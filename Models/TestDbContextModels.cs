using Microsoft.EntityFrameworkCore;

namespace SchemaMagic.Examples;

/// <summary>
/// Document or file attached to project
/// </summary>
[Comment("Document entity - file storage with versioning and access control")]
public class Document
{
    [Comment("Primary key")]
    public int Id { get; set; }

    /// <summary>
    /// Display name for document
    /// </summary>
    [Comment("Document display name")]
    public string Name { get; set; } = string.Empty;

    [Comment("Original uploaded filename")]
    public string OriginalName { get; set; } = string.Empty;

    /// <summary>
    /// Document description
    /// </summary>
    [Comment("Document description or summary")]
    public string Description { get; set; } = string.Empty;

    [Comment("Category - Specification, Design, Report, etc.")]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Document type
    /// </summary>
    [Comment("Document type classification")]
    public string Type { get; set; } = string.Empty;

    [Comment("Storage path or S3 key")]
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// MIME content type
    /// </summary>
    [Comment("MIME type - application/pdf, image/png, etc.")]
    public string MimeType { get; set; } = string.Empty;

    [Comment("File size in bytes")]
    public long FileSize { get; set; }

    /// <summary>
    /// File hash for integrity
    /// </summary>
    [Comment("SHA256 hash for integrity verification")]
    public string Hash { get; set; } = string.Empty;

    [Comment("Version number")]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Whether this is latest version
    /// </summary>
    [Comment("Latest version flag")]
    public bool IsLatestVersion { get; set; }

    [Comment("Document status - Draft, Review, Approved, Archived")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Access control level
    /// </summary>
    [Comment("Access level - Public, Internal, Confidential, Restricted")]
    public string AccessLevel { get; set; } = string.Empty;

    [Comment("Document expiration date")]
    public DateTime? ExpiryDate { get; set; }

    /// <summary>
    /// Download counter
    /// </summary>
    [Comment("Total download count")]
    public int Downloads { get; set; }

    [Comment("Encryption status flag")]
    public bool IsEncrypted { get; set; }

    /// <summary>
    /// Searchable keywords
    /// </summary>
    [Comment("Comma-separated keywords")]
    public string Keywords { get; set; } = string.Empty;

    [Comment("Upload timestamp")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last modification time
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Foreign key to project
    /// </summary>
    [Comment("FK to Projects")]
    public int ProjectId { get; set; }

    /// <summary>
    /// Foreign key to uploader
    /// </summary>
    [Comment("FK to Users - uploader")]
    public int UploadedById { get; set; }

    /// <summary>
    /// Navigation to project
    /// </summary>
    public Project Project { get; set; } = null!;

    /// <summary>
    /// Navigation to uploader
    /// </summary>
    public User UploadedBy { get; set; } = null!;
}

/// <summary>
/// Comment or note on a task
/// </summary>
[Comment("Comment entity - discussion threads on tasks")]
public class Comment
{
    [Comment("Primary key")]
    public int Id { get; set; }

    /// <summary>
    /// Comment text content
    /// </summary>
    [Comment("Comment plain text content")]
    public string Content { get; set; } = string.Empty;

    [Comment("HTML formatted content")]
    public string FormattedContent { get; set; } = string.Empty;

    /// <summary>
    /// Comment type
    /// </summary>
    [Comment("Type - Comment, Note, System")]
    public string Type { get; set; } = string.Empty;

    [Comment("Internal visibility flag")]
    public bool IsInternal { get; set; }

    /// <summary>
    /// Whether comment was edited
    /// </summary>
    [Comment("Edited status flag")]
    public bool IsEdited { get; set; }

    [Comment("Last edit timestamp")]
    public DateTime? EditedAt { get; set; }

    /// <summary>
    /// IP address of commenter
    /// </summary>
    [Comment("Client IP address")]
    public string IpAddress { get; set; } = string.Empty;

    [Comment("Client user agent string")]
    public string UserAgent { get; set; } = string.Empty;

    /// <summary>
    /// Comment creation time
    /// </summary>
    public DateTime CreatedAt { get; set; }

    [Comment("Last update timestamp")]
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Foreign key to task
    /// </summary>
    [Comment("FK to Tasks")]
    public int TaskId { get; set; }

    /// <summary>
    /// Foreign key to author
    /// </summary>
    [Comment("FK to Users - author")]
    public int AuthorId { get; set; }

    /// <summary>
    /// Navigation to task
    /// </summary>
    public Task Task { get; set; } = null!;

    /// <summary>
    /// Navigation to author
    /// </summary>
    public User Author { get; set; } = null!;

    /// <summary>
    /// Files attached to comment
    /// </summary>
    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
}

/// <summary>
/// File attached to a comment
/// </summary>
[Comment("Attachment entity - files attached to comments")]
public class Attachment
{
    [Comment("Primary key")]
    public int Id { get; set; }

    /// <summary>
    /// Attachment display name
    /// </summary>
    [Comment("Display name")]
    public string Name { get; set; } = string.Empty;

    [Comment("Original filename")]
    public string OriginalName { get; set; } = string.Empty;

    /// <summary>
    /// Storage path
    /// </summary>
    [Comment("File storage path")]
    public string FilePath { get; set; } = string.Empty;

    [Comment("MIME content type")]
    public string MimeType { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes
    /// </summary>
    [Comment("File size in bytes")]
    public long FileSize { get; set; }

    [Comment("SHA256 hash")]
    public string Hash { get; set; } = string.Empty;

    /// <summary>
    /// Thumbnail image path
    /// </summary>
    [Comment("Thumbnail path for images")]
    public string ThumbnailPath { get; set; } = string.Empty;

    [Comment("Image file flag")]
    public bool IsImage { get; set; }

    /// <summary>
    /// Image width in pixels
    /// </summary>
    [Comment("Image width")]
    public int? ImageWidth { get; set; }

    [Comment("Image height")]
    public int? ImageHeight { get; set; }

    /// <summary>
    /// Upload timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Foreign key to comment
    /// </summary>
    [Comment("FK to Comments")]
    public int CommentId { get; set; }

    /// <summary>
    /// Foreign key to uploader
    /// </summary>
    [Comment("FK to Users - uploader")]
    public int UploadedById { get; set; }

    /// <summary>
    /// Navigation to comment
    /// </summary>
    public Comment Comment { get; set; } = null!;

    /// <summary>
    /// Navigation to uploader
    /// </summary>
    public User UploadedBy { get; set; } = null!;
}

/// <summary>
/// Tag for categorizing tasks
/// </summary>
[Comment("Tag entity - labels for categorizing and filtering tasks")]
public class Tag
{
    [Comment("Primary key")]
    public int Id { get; set; }

    /// <summary>
    /// Tag name
    /// </summary>
    [Comment("Tag display name")]
    public string Name { get; set; } = string.Empty;

    [Comment("Tag description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Tag color code
    /// </summary>
    [Comment("Hex color code")]
    public string Color { get; set; } = string.Empty;

    [Comment("Icon name or class")]
    public string Icon { get; set; } = string.Empty;

    /// <summary>
    /// Tag category
    /// </summary>
    [Comment("Category for grouping tags")]
    public string Category { get; set; } = string.Empty;

    [Comment("Active status flag")]
    public bool IsActive { get; set; }

    /// <summary>
    /// Number of times tag is used
    /// </summary>
    [Comment("Usage counter")]
    public int UsageCount { get; set; }

    [Comment("Tag creation timestamp")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Tasks with this tag
    /// </summary>
    public ICollection<TaskTag> TaskTags { get; set; } = new List<TaskTag>();
}

/// <summary>
/// Many-to-many relationship between tasks and tags
/// </summary>
[Comment("TaskTag junction - many-to-many relationship between tasks and tags")]
public class TaskTag
{
    /// <summary>
    /// Foreign key to task (composite key)
    /// </summary>
    [Comment("FK to Tasks - part of composite key")]
    public int TaskId { get; set; }

    /// <summary>
    /// Foreign key to tag (composite key)
    /// </summary>
    [Comment("FK to Tags - part of composite key")]
    public int TagId { get; set; }

    [Comment("Timestamp when tag was applied")]
    public DateTime TaggedAt { get; set; }

    /// <summary>
    /// Username who applied tag
    /// </summary>
    [Comment("User who applied the tag")]
    public string TaggedBy { get; set; } = string.Empty;

    /// <summary>
    /// Navigation to task
    /// </summary>
    public Task Task { get; set; } = null!;

    /// <summary>
    /// Navigation to tag
    /// </summary>
    public Tag Tag { get; set; } = null!;
}

/// <summary>
/// Many-to-many relationship between users and projects
/// </summary>
[Comment("UserProject junction - team membership with roles and rates")]
public class UserProject
{
    /// <summary>
    /// Foreign key to user (composite key)
    /// </summary>
    [Comment("FK to Users - part of composite key")]
    public int UserId { get; set; }

    /// <summary>
    /// Foreign key to project (composite key)
    /// </summary>
    [Comment("FK to Projects - part of composite key")]
    public int ProjectId { get; set; }

    [Comment("User role on project - Developer, Designer, QA, etc.")]
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Permission set for user
    /// </summary>
    [Comment("Comma-separated permissions")]
    public string Permissions { get; set; } = string.Empty;

    [Comment("Date user joined project")]
    public DateTime JoinedAt { get; set; }

    /// <summary>
    /// Date user left project
    /// </summary>
    [Comment("Date user left project - null if active")]
    public DateTime? LeftAt { get; set; }

    [Comment("Active membership flag")]
    public bool IsActive { get; set; }

    /// <summary>
    /// Billing rate per hour
    /// </summary>
    [Comment("Hourly rate for billing")]
    public decimal HourlyRate { get; set; }

    [Comment("Rate currency code")]
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// Navigation to user
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// Navigation to project
    /// </summary>
    public Project Project { get; set; } = null!;
}

/// <summary>
/// Audit log for tracking system changes
/// </summary>
[Comment("AuditLog entity - comprehensive audit trail for compliance")]
public class AuditLog
{
    [Comment("Primary key")]
    public int Id { get; set; }

    /// <summary>
    /// Action performed
    /// </summary>
    [Comment("Action - Create, Update, Delete, View")]
    public string Action { get; set; } = string.Empty;

    [Comment("Entity type name")]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// ID of affected entity
    /// </summary>
    [Comment("Entity ID that was modified")]
    public int? EntityId { get; set; }

    [Comment("JSON of old values before change")]
    public string OldValues { get; set; } = string.Empty;

    /// <summary>
    /// New values after change
    /// </summary>
    [Comment("JSON of new values after change")]
    public string NewValues { get; set; } = string.Empty;

    [Comment("Client IP address")]
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// Browser user agent
    /// </summary>
    [Comment("Client user agent string")]
    public string UserAgent { get; set; } = string.Empty;

    [Comment("Session identifier")]
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// When action occurred
    /// </summary>
    [Comment("Action timestamp")]
    public DateTime Timestamp { get; set; }

    [Comment("Additional audit details")]
    public string Details { get; set; } = string.Empty;

    /// <summary>
    /// Foreign key to user who performed action
    /// </summary>
    [Comment("FK to Users - who performed the action")]
    public int? UserId { get; set; }

    /// <summary>
    /// Navigation to user
    /// </summary>
    public User? User { get; set; }
}