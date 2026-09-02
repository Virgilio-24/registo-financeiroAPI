using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;    
using RegistoFinanceiro.Domain.Entities;

namespace RegistoFinanceiro.Infrastructure.Persistence
{
    public class RegistoFinanceiroDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
    {
        public RegistoFinanceiroDbContext(DbContextOptions<RegistoFinanceiroDbContext> options)
        : base(options)
        {
        }

        public DbSet<Vehicle> Vehicles => Set<Vehicle>();
        public DbSet<MovementCategory> MovementCategories => Set<MovementCategory>();
        public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();
        public DbSet<FinancialRecord> FinancialRecords => Set<FinancialRecord>();
        public DbSet<RecordAttachment> RecordAttachments => Set<RecordAttachment>();
        public DbSet<RecordAuditLog> RecordAuditLogs => Set<RecordAuditLog>();
        public DbSet<RefreshTokens> RefreshTokens => Set<RefreshTokens>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); 

            ConfigureVehicles(modelBuilder);
            ConfigureMovementCategories(modelBuilder);
            ConfigurePaymentMethods(modelBuilder);
            ConfigureFinancialRecords(modelBuilder);
            ConfigureRecordAttachments(modelBuilder);
            ConfigureRecordAuditLogs(modelBuilder);
            ConfigureRefreshTokens(modelBuilder);
        }
        private static void ConfigureVehicles(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Vehicle>(entity =>
            {
                entity.HasKey(v => v.Id);
                entity.Property(v => v.Id).HasDefaultValueSql("gen_random_uuid()");

                entity.Property(v => v.Plate).IsRequired();
                entity.HasIndex(v => v.Plate).IsUnique();

                entity.Property(v => v.Brand).IsRequired();
                entity.Property(v => v.Model).IsRequired();

                entity.Property(v => v.Status).IsRequired().HasDefaultValue("active");
                entity.ToTable(t => t.HasCheckConstraint(
                    "ck_vehicles_status",
                    "status IN ('active','inactive','maintenance')"));
                entity.HasIndex(v => v.Status).HasDatabaseName("idx_vehicles_status");

                entity.Property(v => v.CreatedAt).HasDefaultValueSql("now()");
                entity.Property(v => v.UpdatedAt).HasDefaultValueSql("now()");

                entity.HasMany(v => v.Records)
                    .WithOne(r => r.Vehicle)
                    .HasForeignKey(r => r.VehicleId)
                    .OnDelete(DeleteBehavior.Restrict); 
            });
        }
        private static void ConfigureMovementCategories(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MovementCategory>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Id).HasDefaultValueSql("gen_random_uuid()");

                entity.Property(c => c.Name).IsRequired();
                entity.HasIndex(c => c.Name).IsUnique();

                entity.Property(c => c.Type).IsRequired();
                entity.ToTable(t => t.HasCheckConstraint(
                    "ck_categories_type",
                    "type IN ('income','expense','both')"));
                entity.HasIndex(c => c.Type).HasDatabaseName("idx_categories_type");

                entity.Property(c => c.Active).HasDefaultValue(true);
                entity.Property(c => c.CreatedAt).HasDefaultValueSql("now()");
                entity.Property(c => c.UpdatedAt).HasDefaultValueSql("now()");
            });
        }
        private static void ConfigurePaymentMethods(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PaymentMethod>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Id).HasDefaultValueSql("gen_random_uuid()");

                entity.Property(p => p.Code).IsRequired();
                entity.HasIndex(p => p.Code).IsUnique();

                entity.Property(p => p.Name).IsRequired();
                entity.HasIndex(p => p.Name).IsUnique();

                entity.Property(p => p.Active).HasDefaultValue(true);
                entity.HasIndex(p => p.Active).HasDatabaseName("idx_payment_methods_active");

                entity.Property(p => p.CreatedAt).HasDefaultValueSql("now()");
                entity.Property(p => p.UpdatedAt).HasDefaultValueSql("now()");

                entity.HasMany(p => p.Records)
                    .WithOne(r => r.PaymentMethod)
                    .HasForeignKey(r => r.PaymentMethodId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
        private static void ConfigureFinancialRecords(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FinancialRecord>(entity =>
            {
                entity.HasKey(r => r.Id);
                entity.Property(r => r.Id).HasDefaultValueSql("gen_random_uuid()");

                entity.Property(r => r.Reference).IsRequired();
                entity.HasIndex(r => r.Reference).IsUnique().HasDatabaseName("idx_records_reference");

                entity.Property(r => r.Type).IsRequired();
                entity.ToTable(t => t.HasCheckConstraint(
                    "ck_records_type", "type IN ('income','expense')"));
                entity.HasIndex(r => r.Type).HasDatabaseName("idx_records_type");

                entity.Property(r => r.Amount).HasColumnType("numeric(12,2)");
                entity.ToTable(t => t.HasCheckConstraint("ck_records_amount_positive", "amount > 0"));

                entity.Property(r => r.Reason).IsRequired();
                entity.Property(r => r.MovementDate).HasColumnType("timestamp with time zone").IsRequired();

                entity.Property(r => r.Status).IsRequired().HasDefaultValue("draft");
                entity.ToTable(t => t.HasCheckConstraint(
                    "ck_records_status",
                    "status IN ('draft','submitted','approved','cancelled')"));
                entity.HasIndex(r => r.Status).HasDatabaseName("idx_records_status");

                entity.HasIndex(r => r.MovementDate).HasDatabaseName("idx_records_movement_date");

                // --- Relações ---
                entity.HasIndex(r => r.PaymentMethodId).HasDatabaseName("idx_records_payment_method_id");

                entity.HasOne(r => r.Category)
                    .WithMany()
                    .HasForeignKey(r => r.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(r => r.CategoryId).HasDatabaseName("idx_records_category_id");
                
                entity.HasIndex(r => r.VehicleId).HasDatabaseName("idx_records_vehicle_id");

                entity.HasOne(r => r.CreatedBy)
                    .WithMany()
                    .HasForeignKey(r => r.CreatedById)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(r => r.CreatedById).HasDatabaseName("idx_records_created_by");

                entity.HasOne(r => r.ApprovedBy)
                    .WithMany()
                    .HasForeignKey(r => r.ApprovedById)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(r => r.ApprovedById).HasDatabaseName("idx_records_approved_by");

                entity.HasOne(r => r.ParentRecord)
                    .WithMany()
                    .HasForeignKey(r => r.ParentRecordId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(r => r.ParentRecordId).HasDatabaseName("idx_records_parent_record_id");

                entity.Property(r => r.CreatedAt).HasDefaultValueSql("now()");
                entity.Property(r => r.UpdatedAt).HasDefaultValueSql("now()");
            });
        }

        private static void ConfigureRecordAttachments(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RecordAttachment>(entity =>
            {
                entity.HasKey(a => a.Id);
                entity.Property(a => a.Id).HasDefaultValueSql("gen_random_uuid()");

                entity.Property(a => a.FileName).IsRequired();
                entity.Property(a => a.StoragePath).IsRequired();
                entity.Property(a => a.MimeType).IsRequired();

                entity.ToTable(t => t.HasCheckConstraint(
                    "ck_attachments_file_size_positive", "file_size_bytes > 0"));

                entity.HasOne(a => a.FinancialRecord)
                    .WithMany(r => r.Attachments)
                    .HasForeignKey(a => a.RecordId)
                    .OnDelete(DeleteBehavior.Cascade); 
                entity.HasIndex(a => a.RecordId).HasDatabaseName("idx_attachments_record_id");

                entity.HasOne(a => a.UploadedBy)
                    .WithMany()
                    .HasForeignKey(a => a.UploadedById)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(a => a.CreatedAt).HasDefaultValueSql("now()");
            });
        }

        private static void ConfigureRecordAuditLogs(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RecordAuditLog>(entity =>
            {
                entity.HasKey(l => l.Id);
                entity.Property(l => l.Id).HasDefaultValueSql("gen_random_uuid()");

                entity.Property(l => l.Action).IsRequired();
                entity.ToTable(t => t.HasCheckConstraint(
                    "ck_audit_action",
                    "action IN ('created','updated','submitted','approved','cancelled')"));

                entity.Property(l => l.BeforeData).HasColumnType("jsonb");
                entity.Property(l => l.AfterData).HasColumnType("jsonb");

                entity.HasOne(l => l.FinancialRecord)
                    .WithMany(r => r.AuditLogs)
                    .HasForeignKey(l => l.RecordId)
                    .OnDelete(DeleteBehavior.Cascade); 
                entity.HasIndex(l => l.RecordId).HasDatabaseName("idx_audit_record_id");

                entity.HasOne(l => l.PerformedBy)
                    .WithMany()
                    .HasForeignKey(l => l.PerformedById)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(l => l.CreatedAt).HasDefaultValueSql("now()");
            });
        }

        private static void ConfigureRefreshTokens(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RefreshTokens>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.Property(t => t.Id).HasDefaultValueSql("gen_random_uuid()");

                entity.Property(t => t.TokenHash).IsRequired();
                entity.Property(t => t.ExpiresAt).IsRequired();

                entity.HasOne(t => t.User)
                    .WithMany()
                    .HasForeignKey(t => t.UserId)
                    .OnDelete(DeleteBehavior.Cascade); 
                entity.HasIndex(t => t.UserId).HasDatabaseName("idx_refresh_tokens_user_id");

                entity.Property(t => t.CreatedAt).HasDefaultValueSql("now()");
            });
        }
    }
}