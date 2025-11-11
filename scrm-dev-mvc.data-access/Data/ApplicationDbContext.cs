
using Microsoft.EntityFrameworkCore;
using scrm_dev_mvc.Models;

namespace scrm_dev_mvc.DataAccess.Data;

public partial class ApplicationDbContext : DbContext
{
    public ApplicationDbContext()
    {
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Activity> Activities { get; set; }

    public virtual DbSet<ActivityType> ActivityTypes { get; set; }

    public virtual DbSet<Audit> Audits { get; set; }

    public virtual DbSet<Call> Calls { get; set; }

    public virtual DbSet<Company> Companies { get; set; }

    public virtual DbSet<Contact> Contacts { get; set; }

    public virtual DbSet<Deal> Deals { get; set; }

    public virtual DbSet<DealLineItem> DealLineItems { get; set; }

    public virtual DbSet<EmailMessage> EmailMessages { get; set; }

    public virtual DbSet<EmailTemplate> EmailTemplates { get; set; }

    public virtual DbSet<EmailThread> EmailThreads { get; set; }

    public virtual DbSet<GmailCred> GmailCreds { get; set; }

    public virtual DbSet<LeadStatus> LeadStatuses { get; set; }

    public virtual DbSet<Lifecycle> Lifecycles { get; set; }

    public virtual DbSet<Organization> Organizations { get; set; }

    public virtual DbSet<Priority> Priorities { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Stage> Stages { get; set; }

    public virtual DbSet<Models.Task> Tasks { get; set; }

    public virtual DbSet<Models.TaskStatus> TaskStatuses { get; set; }

    public virtual DbSet<TaskTemplate> TaskTemplates { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<WorkflowTemplate> WorkflowTemplates { get; set; }

    public virtual DbSet<Invitation> Invitations { get; set; }

    public virtual DbSet<Workflow> Workflows { get; set; }

    public virtual DbSet<WorkflowAction> WorkflowActions { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=(LocalDb)\\MSSQLLocalDB;Database=scrm-dev-mvc;Trusted_Connection=True;TrustServerCertificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Activity>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__activity__3213E83F501E9A09");

            entity.ToTable("activity");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ActivityDate)
                .HasColumnType("datetime")
                .HasColumnName("activity_date");
            entity.Property(e => e.ActivityTypeId).HasColumnName("activity_type_id");
            entity.Property(e => e.ContactId).HasColumnName("contact_id");
            entity.Property(e => e.DealId).HasColumnName("deal_id");
            entity.Property(e => e.DueDate)
                .HasColumnType("datetime")
                .HasColumnName("due_date");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.OwnerId).HasColumnName("owner_id");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");
            entity.Property(e => e.SubjectId).HasColumnName("subject_id");
            entity.Property(e => e.SubjectType)
                .HasMaxLength(50)
                .HasColumnName("subject_type");

            entity.HasOne(d => d.ActivityType).WithMany(p => p.Activities)
                .HasForeignKey(d => d.ActivityTypeId)
                .HasConstraintName("FK_activity_type");

            entity.HasOne(d => d.Contact).WithMany(p => p.Activities)
                .HasForeignKey(d => d.ContactId)
                .HasConstraintName("FK_activity_contact");

            entity.HasOne(d => d.Deal).WithMany(p => p.Activities)
                .HasForeignKey(d => d.DealId)
                .HasConstraintName("FK_activity_deal");

            entity.HasOne(d => d.Owner).WithMany(p => p.Activities)
                .HasForeignKey(d => d.OwnerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_activity_owner");
        });

        modelBuilder.Entity<ActivityType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__activity__3213E83FFFA3CED0");

            entity.ToTable("activity_type");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Audit>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__audit__3213E83F1F945816");

            entity.ToTable("audit");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.FieldName)
                .HasMaxLength(100)
                .HasColumnName("field_name");
            entity.Property(e => e.NewValue).HasColumnName("new_value");
            entity.Property(e => e.OldValue).HasColumnName("old_value");
            entity.Property(e => e.OwnerId).HasColumnName("owner_id");
            entity.Property(e => e.RecordId).HasColumnName("record_id");
            entity.Property(e => e.TableName)
                .HasMaxLength(100)
                .HasColumnName("table_name");
            entity.Property(e => e.Timestamp)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("timestamp");

            entity.HasOne(d => d.Owner).WithMany(p => p.Audits)
                .HasForeignKey(d => d.OwnerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_audit_user");
        });

        modelBuilder.Entity<Call>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__call__3213E83FCCB1D8F4");

            entity.ToTable("call");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CallTime)
                .HasColumnType("datetime")
                .HasColumnName("call_time");
            entity.Property(e => e.ContactId).HasColumnName("contact_id");
            entity.Property(e => e.Direction)
                .HasMaxLength(20)
                .HasColumnName("direction");
            entity.Property(e => e.DurationSeconds).HasColumnName("duration_seconds");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.Outcome)
                .HasMaxLength(50)
                .HasColumnName("outcome");
            entity.Property(e => e.Sid)
                .HasMaxLength(255)
                .HasColumnName("sid");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Contact).WithMany(p => p.Calls)
                .HasForeignKey(d => d.ContactId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_call_contact");

            entity.HasOne(d => d.User).WithMany(p => p.Calls)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_call_user");
        });

        modelBuilder.Entity<Company>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__company__3213E83F2B20C397");

            entity.ToTable("company");
            entity.HasIndex(c => new { c.Domain, c.OrganizationId }).IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.City)
                .HasMaxLength(100)
                .HasColumnName("city");
            entity.Property(e => e.Country)
                .HasMaxLength(100)
                .HasColumnName("country");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Domain)
                .HasMaxLength(255)
                .HasColumnName("domain");
            entity.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.State)
                .HasMaxLength(100)
                .HasColumnName("state");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Companies)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_company_user");
        });

        modelBuilder.Entity<Contact>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__contact__3213E83FB765161C");

            entity.ToTable("contact");

            
            entity.HasIndex(c => new { c.Email, c.OrganizationId }).IsUnique();
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.FirstName)
                .HasMaxLength(100)
                .HasColumnName("first_name");
            entity.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            entity.Property(e => e.LastName)
                .HasMaxLength(100)
                .HasColumnName("last_name");
            entity.Property(e => e.LeadStatusId).HasColumnName("lead_status_id");
            entity.Property(e => e.LifeCycleStageId).HasColumnName("life_cycle_stage_id");
            entity.Property(e => e.Number)
                .HasMaxLength(50)
                .HasColumnName("number");
            entity.Property(e => e.OwnerId).HasColumnName("owner_id");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Company).WithMany(p => p.Contacts)
                .HasForeignKey(d => d.CompanyId)
                .HasConstraintName("FK_contact_company");

            entity.HasOne(d => d.LeadStatus).WithMany(p => p.Contacts)
                .HasForeignKey(d => d.LeadStatusId)
                .HasConstraintName("FK_contact_lead");

            entity.HasOne(d => d.LifeCycleStage).WithMany(p => p.Contacts)
                .HasForeignKey(d => d.LifeCycleStageId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_contact_lifecycle");

            entity.HasOne(d => d.Owner).WithMany(p => p.Contacts)
                .HasForeignKey(d => d.OwnerId)
                .HasConstraintName("FK_contact_owner");
        });

        modelBuilder.Entity<Deal>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__deal__3213E83F2D70B86A");

            entity.ToTable("deal");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CloseDate)
                .HasColumnType("datetime")
                .HasColumnName("close_date");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            entity.Property(e => e.OwnerId).HasColumnName("owner_id");
            entity.Property(e => e.StageId).HasColumnName("stage_id");
            entity.Property(e => e.Value)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("value");

            entity.HasOne(d => d.Company).WithMany(p => p.Deals)
                .HasForeignKey(d => d.CompanyId)
                .HasConstraintName("FK_deal_company");

            entity.HasOne(d => d.Owner).WithMany(p => p.Deals)
                .HasForeignKey(d => d.OwnerId)
                .HasConstraintName("FK_deal_user");

            entity.HasOne(d => d.Stage).WithMany(p => p.Deals)
                .HasForeignKey(d => d.StageId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_deal_stage");

            entity.HasMany(d => d.Contacts).WithMany(p => p.Deals)
                .UsingEntity<Dictionary<string, object>>(
                    "DealContactAssociation",
                    r => r.HasOne<Contact>().WithMany()
                        .HasForeignKey("ContactId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_dca_contact"),
                    l => l.HasOne<Deal>().WithMany()
                        .HasForeignKey("DealId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_dca_deal"),
                    j =>
                    {
                        j.HasKey("DealId", "ContactId").HasName("PK__deal_con__B03640C46D7A9C6B");
                        j.ToTable("deal_contact_association");
                        j.IndexerProperty<int>("DealId").HasColumnName("deal_id");
                        j.IndexerProperty<int>("ContactId").HasColumnName("contact_id");
                    });
        });

        modelBuilder.Entity<DealLineItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__deal_lin__3213E83F626389E4");

            entity.ToTable("deal_line_item");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DealId).HasColumnName("deal_id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.UnitPrice)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("unit_price");

            entity.HasOne(d => d.Deal).WithMany(p => p.DealLineItems)
                .HasForeignKey(d => d.DealId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_dli_deal");

            entity.HasOne(d => d.Product).WithMany(p => p.DealLineItems)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_dli_product");
        });

        modelBuilder.Entity<EmailMessage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__email_me__3213E83FD8D84B8A");

            entity.ToTable("email_message");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Body).HasColumnName("body");
            entity.Property(e => e.Direction)
                .HasMaxLength(20)
                .HasColumnName("direction");
            entity.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            entity.Property(e => e.SentAt)
                .HasColumnType("datetime")
                .HasColumnName("sent_at");
            entity.Property(e => e.ThreadId).HasColumnName("thread_id");

            entity.HasOne(d => d.Thread).WithMany(p => p.EmailMessages)
                .HasForeignKey(d => d.ThreadId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_em_thread");
        });

        modelBuilder.Entity<EmailTemplate>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__email_te__3213E83FE66CAEDD");

            entity.ToTable("email_template");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Body).HasColumnName("body");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Subject)
                .HasMaxLength(255)
                .HasColumnName("subject");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
            entity.Property(e => e.WorkflowId).HasColumnName("workflow_id");

            entity.HasOne(d => d.Workflow).WithMany(p => p.EmailTemplates)
                .HasForeignKey(d => d.WorkflowId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_et_wft");
        });

        modelBuilder.Entity<EmailThread>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__email_th__3213E83FCB9916B7");

            entity.ToTable("email_thread");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ContactId).HasColumnName("contact_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.IsArchived)
                .HasDefaultValue(false)
                .HasColumnName("is_archived");
            entity.Property(e => e.Subject)
                .HasMaxLength(255)
                .HasColumnName("subject");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Contact).WithMany(p => p.EmailThreads)
                .HasForeignKey(d => d.ContactId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_et_contact");

            entity.HasOne(d => d.User).WithMany(p => p.EmailThreads)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_et_user");
        });

        modelBuilder.Entity<GmailCred>(entity =>
        {
            entity.HasKey(e => e.Email).HasName("PK__gmail_cr__AB6E61658357D9CF");

            entity.ToTable("gmail_cred");

            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.GmailAccessToken).HasColumnName("gmail_access_token");
            entity.Property(e => e.GmailRefreshToken).HasColumnName("gmail_refresh_token");

            entity.HasOne(d => d.EmailNavigation).WithOne(p => p.GmailCred)
                .HasPrincipalKey<User>(p => p.Email)
                .HasForeignKey<GmailCred>(d => d.Email)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_gmail_user");
        });

        modelBuilder.Entity<Invitation>(entity =>
        {
            entity.ToTable("invitation");

            entity.HasKey(e => e.Id);

            // Ensure that every invitation code is unique in the database
            entity.HasIndex(e => e.InvitationCode).IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");

            entity.Property(e => e.InvitationCode)
                .IsRequired()
                .HasMaxLength(16) // Set a reasonable max length for the code
                .HasColumnName("invitation_code");

            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnName("email");

            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");

            entity.Property(e => e.RoleId).HasColumnName("role_id");

            entity.Property(e => e.SentDate)
                .HasDefaultValueSql("(getutcdate())") // Use getutcdate() for UTC time
                .HasColumnName("sent_date");

            entity.Property(e => e.ExpiryDate).HasColumnName("expiry_date");

            entity.Property(e => e.IsAccepted)
                .HasDefaultValue(false)
                .HasColumnName("is_accepted");

            // Define the relationship to the Organization entity
            entity.HasOne(d => d.Organization)
                .WithMany(p => p.Invitations) // Assumes Organization has a 'public ICollection<Invitation> Invitations' property
                .HasForeignKey(d => d.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade) // Deletes invitations if the organization is deleted
                .HasConstraintName("FK_invitation_organization");
        });

        modelBuilder.Entity<LeadStatus>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__lead_sta__3213E83FAC732FF9");

            entity.ToTable("lead_status");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.LeadStatusName)
                .HasMaxLength(100)
                .HasColumnName("lead_status_name");
        });

        modelBuilder.Entity<Lifecycle>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__lifecycl__3213E83F25A8654C");

            entity.ToTable("lifecycle");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.LifeCycleStageName)
                .HasMaxLength(100)
                .HasColumnName("life_cycle_stage_name");
        });

        modelBuilder.Entity<Organization>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__organiza__3213E83FDD745463");

            entity.ToTable("organization");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Priority>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__priority__3213E83F8C916A46");

            entity.ToTable("priority");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__product__3213E83F3344DAD3");

            entity.ToTable("product");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("price");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Products)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_product_user");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__role__3213E83FB0D9A839");

            entity.ToTable("role");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Stage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__stage__3213E83F8E539D3D");

            entity.ToTable("stage");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Models.Task>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__task__3213E83F62A3F754");

            entity.ToTable("task");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CompletedAt)
                .HasColumnType("datetime")
                .HasColumnName("completed_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.DueDate)
                .HasColumnType("datetime")
                .HasColumnName("due_date");
            entity.Property(e => e.PriorityId).HasColumnName("priority_id");
            entity.Property(e => e.StatusId).HasColumnName("status_id");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");

            entity.HasOne(d => d.Priority).WithMany(p => p.Tasks)
                .HasForeignKey(d => d.PriorityId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_task_priority");

            entity.HasOne(d => d.Status).WithMany(p => p.Tasks)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_task_status");
        });

        modelBuilder.Entity<Models.TaskStatus>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__task_sta__3213E83F8995A1A3");

            entity.ToTable("task_status");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.StatusName)
                .HasMaxLength(50)
                .HasColumnName("status_name");
        });

        modelBuilder.Entity<TaskTemplate>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__task_tem__3213E83F34E7FAD9");

            entity.ToTable("task_template");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Duedate)
                .HasColumnType("datetime")
                .HasColumnName("duedate");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
            entity.Property(e => e.WorkflowId).HasColumnName("workflow_id");

            entity.HasOne(d => d.Workflow).WithMany(p => p.TaskTemplates)
                .HasForeignKey(d => d.WorkflowId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tt_wft");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__user__3213E83F5D023DD2");

            entity.ToTable("user");

            entity.HasIndex(e => e.Email, "UQ__user__AB6E6164B3AFDF48").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.FirstName)
                .HasMaxLength(100)
                .HasColumnName("first_name");
            entity.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            entity.Property(e => e.LastName)
                .HasMaxLength(100)
                .HasColumnName("last_name");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
            entity.Property(e => e.RoleId).HasColumnName("role_id");

            entity.HasOne(d => d.Organization).WithMany(p => p.Users)
                .HasForeignKey(d => d.OrganizationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_user_org");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_user_role");
        });

        modelBuilder.Entity<WorkflowTemplate>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__workflow__3213E83FA390A6D2");

            entity.ToTable("workflow_template");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.WorkflowJson).HasColumnName("workflow_json");
            entity.Property(e => e.WorkflowName)
                .HasMaxLength(255)
                .HasColumnName("workflow_name");

            entity.HasOne(d => d.User).WithMany(p => p.WorkflowTemplates)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_wft_user");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
