using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using PickURide.Infrastructure.Data.Entities;

namespace PickURide.Infrastructure.Data;

public partial class PickURideDbContext : DbContext
{
    public PickURideDbContext(DbContextOptions<PickURideDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Admin> Admins { get; set; }

    public virtual DbSet<BlacklistedToken> BlacklistedTokens { get; set; }

    public virtual DbSet<Driver> Drivers { get; set; }

    public virtual DbSet<DriverAttendance> DriverAttendances { get; set; }

    public virtual DbSet<DriverLocationHistory> DriverLocationHistories { get; set; }

    public virtual DbSet<DriverOvertimeDuty> DriverOvertimeDuties { get; set; }

    public virtual DbSet<DriverShift> DriverShifts { get; set; }

    public virtual DbSet<DriverShiftApplication> DriverShiftApplications { get; set; }

    public virtual DbSet<FareSetting> FareSettings { get; set; }

    public virtual DbSet<FareDistanceSlab> FareDistanceSlabs { get; set; }

    public virtual DbSet<Feedback> Feedbacks { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Ride> Rides { get; set; }

    public virtual DbSet<RideMessage> RideMessages { get; set; }

    public virtual DbSet<RideStop> RideStops { get; set; }

    public virtual DbSet<Shift> Shifts { get; set; }

    public virtual DbSet<SupportChat> SupportChats { get; set; }

    public virtual DbSet<Tip> Tips { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<Policy> Policies { get; set; }

    public virtual DbSet<PromoCode> PromoCodes { get; set; }

    public virtual DbSet<PromoRedemption> PromoRedemptions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Admin>(entity =>
        {
            entity.HasKey(e => e.AdminId).HasName("PK__Admins__719FE488F9701263");

            entity.HasIndex(e => e.Email, "UQ__Admins__A9D1053467E70044").IsUnique();

            entity.Property(e => e.AdminId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.FullName).HasMaxLength(150);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.PhoneNumber).HasMaxLength(50);
            entity.Property(e => e.Role).HasMaxLength(50);
        });

        modelBuilder.Entity<BlacklistedToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Blacklis__3214EC07759DEA88");

            entity.Property(e => e.ExpiryDate).HasColumnType("datetime");
            entity.Property(e => e.TokenId).HasMaxLength(500);
        });

        modelBuilder.Entity<Driver>(entity =>
        {
            entity.HasKey(e => e.DriverId).HasName("PK__Drivers__F1B1CD04025D0326");

            entity.HasIndex(e => e.Email, "UQ__Drivers__A9D10534CF894436").IsUnique();

            entity.Property(e => e.DriverId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Address).HasMaxLength(250);
            entity.Property(e => e.CarInsurance).HasMaxLength(100);
            entity.Property(e => e.CarLicensePlate).HasMaxLength(50);
            entity.Property(e => e.CarRegistration).HasMaxLength(100);
            entity.Property(e => e.CarVin)
                .HasMaxLength(50)
                .HasColumnName("CarVIN");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.LicenseNumber).HasMaxLength(50);
            entity.Property(e => e.PasswordHash).HasMaxLength(200);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.Sin)
                .HasMaxLength(50)
                .HasColumnName("SIN");
            entity.Property(e => e.Status).HasMaxLength(20);
            entity.Property(e => e.VehicleColor).HasMaxLength(200);
            entity.Property(e => e.VehicleName).HasMaxLength(200);
            entity.Property(e => e.StripeAccountId).HasColumnName("StripeAccountId");
        });

        modelBuilder.Entity<DriverAttendance>(entity =>
        {
            entity.HasKey(e => e.AttendanceId).HasName("PK__DriverAt__8B69261C9EA56398");

            entity.Property(e => e.AttendanceId).ValueGeneratedNever();
            entity.Property(e => e.AttendanceType)
                .HasMaxLength(50)
                .HasDefaultValue("Duty");

            entity.HasOne(d => d.Driver).WithMany(p => p.DriverAttendances)
                .HasForeignKey(d => d.DriverId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DriverAttendances_Driver");
        });

        modelBuilder.Entity<DriverLocationHistory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__DriverLo__3214EC073C688BDF");

            entity.ToTable("DriverLocationHistory");

            entity.Property(e => e.LoggedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Driver).WithMany(p => p.DriverLocationHistories)
                .HasForeignKey(d => d.DriverId)
                .HasConstraintName("FK__DriverLoc__Drive__787EE5A0");
        });

        modelBuilder.Entity<DriverOvertimeDuty>(entity =>
        {
            entity.HasKey(e => e.OvertimeDutyId).HasName("PK__DriverOv__B8A7567FA671305A");

            entity.Property(e => e.OvertimeDutyId).ValueGeneratedNever();

            entity.HasOne(d => d.Driver).WithMany(p => p.DriverOvertimeDuties)
                .HasForeignKey(d => d.DriverId)
                .HasConstraintName("FK_DriverOvertimeDuties_Driver");

            entity.HasOne(d => d.Shift).WithMany(p => p.DriverOvertimeDuties)
                .HasForeignKey(d => d.ShiftId)
                .HasConstraintName("FK_DriverOvertimeDuties_Shift");
        });

        modelBuilder.Entity<DriverShift>(entity =>
        {
            entity.HasKey(e => e.ShiftId).HasName("PK__DriverSh__C0A83881D157E1BB");

            entity.Property(e => e.ShiftId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Status).HasMaxLength(50);

            entity.HasOne(d => d.Driver).WithMany(p => p.DriverShifts)
                .HasForeignKey(d => d.DriverId)
                .HasConstraintName("FK__DriverShi__Drive__70DDC3D8");
        });

        modelBuilder.Entity<DriverShiftApplication>(entity =>
        {
            entity.HasKey(e => e.ApplicationId).HasName("PK__DriverSh__C93A4C9970D787B0");

            entity.Property(e => e.ApplicationId).ValueGeneratedNever();
            entity.Property(e => e.AppliedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Pending");

            entity.HasOne(d => d.Driver).WithMany(p => p.DriverShiftApplications)
                .HasForeignKey(d => d.DriverId)
                .HasConstraintName("FK_DriverShiftApplications_Driver");

            entity.HasOne(d => d.Shift).WithMany(p => p.DriverShiftApplications)
                .HasForeignKey(d => d.ShiftId)
                .HasConstraintName("FK_DriverShiftApplications_Shift");
        });

        modelBuilder.Entity<FareSetting>(entity =>
        {
            entity.HasKey(e => e.SettingId).HasName("PK__FareSett__54372B1D2D3DA1BB");

            entity.Property(e => e.AdminPercentage).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.AreaType).HasMaxLength(50);
            entity.Property(e => e.BaseFare).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.PerKmRate).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.PerMinuteRate).HasColumnType("decimal(10, 2)");
        });

        modelBuilder.Entity<FareDistanceSlab>(entity =>
        {
            entity.HasKey(e => e.SlabId).HasName("PK_FareDistanceSlabs");

            entity.ToTable("FareDistanceSlabs");

            entity.Property(e => e.FromKm).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.ToKm).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.RatePerKm).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.SortOrder).HasDefaultValue(0);

            entity.HasOne(d => d.Setting)
                .WithMany(p => p.FareDistanceSlabs)
                .HasForeignKey(d => d.SettingId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_FareDistanceSlabs_FareSettings");

            entity.HasIndex(e => new { e.SettingId, e.SortOrder }, "IX_FareDistanceSlabs_SettingId_SortOrder");
        });

        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.HasKey(e => e.FeedbackId).HasName("PK__Feedback__6A4BEDD67CB47A10");

            entity.ToTable("Feedback");

            entity.Property(e => e.FeedbackId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Comments).HasMaxLength(500);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Ride).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.RideId)
                .HasConstraintName("FK__Feedback__RideId__628FA481");

            entity.HasOne(d => d.User).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Feedback__UserId__6383C8BA");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__Payments__9B556A38932D1F0D");

            entity.Property(e => e.PaymentId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.AdminShare).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.CustomerPaid).HasMaxLength(100);
            entity.Property(e => e.DriverShare).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.PaidAmount).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.PaymentMethod).HasMaxLength(50);
            entity.Property(e => e.PaymentStatus).HasMaxLength(20);
            entity.Property(e => e.PaymentToken).HasMaxLength(500);
            entity.Property(e => e.PromoCode).HasMaxLength(50);
            entity.Property(e => e.TipAmount).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.TransferStatus).HasMaxLength(20);
            entity.Property(e => e.TransferId).HasMaxLength(500);
            entity.Property(e => e.TransferredAt).HasColumnType("datetime");

            entity.HasOne(d => d.Ride).WithMany(p => p.Payments)
                .HasForeignKey(d => d.RideId)
                .HasConstraintName("FK__Payments__RideId__5DCAEF64");
        });

        modelBuilder.Entity<Ride>(entity =>
        {
            entity.HasKey(e => e.RideId).HasName("PK__Rides__C5B8C4F427288C86");

            entity.Property(e => e.RideId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.AdminCommission)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Distance)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.DriverPayment)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.FareEstimate).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.FareFinal).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.PromoCode).HasMaxLength(50);
            entity.Property(e => e.PromoDiscount).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.RideType).HasMaxLength(20);
            entity.Property(e => e.ScheduledTime).HasColumnType("datetime");
            entity.Property(e => e.Status).HasMaxLength(20);

            entity.HasOne(d => d.Driver).WithMany(p => p.Rides)
                .HasForeignKey(d => d.DriverId)
                .HasConstraintName("FK__Rides__DriverId__5535A963");

            entity.HasOne(d => d.User).WithMany(p => p.Rides)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Rides__UserId__5441852A");
        });

        modelBuilder.Entity<RideMessage>(entity =>
        {
            entity.HasKey(e => e.MessageId).HasName("PK__RideMess__C87C0C9CB77ACF04");

            entity.Property(e => e.MessageId).ValueGeneratedNever();
            entity.Property(e => e.SenderRole).HasMaxLength(50);

            entity.HasOne(d => d.Ride).WithMany(p => p.RideMessages)
                .HasForeignKey(d => d.RideId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RideMessages_Rides");
        });

        modelBuilder.Entity<RideStop>(entity =>
        {
            entity.HasKey(e => e.RideStopId).HasName("PK__RideStop__E8A7C6F68774D4A1");

            entity.Property(e => e.RideStopId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Location).HasMaxLength(250);

            entity.HasOne(d => d.Ride).WithMany(p => p.RideStops)
                .HasForeignKey(d => d.RideId)
                .HasConstraintName("FK__RideStops__RideI__59FA5E80");
        });

        modelBuilder.Entity<Shift>(entity =>
        {
            entity.HasKey(e => e.ShiftId).HasName("PK__Shifts__C0A83881A611F704");

            entity.Property(e => e.ShiftId).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Description).HasMaxLength(255);
        });

        modelBuilder.Entity<SupportChat>(entity =>
        {
            entity.HasKey(e => e.ChatId).HasName("PK__SupportC__A9FBE7C6A616A3B2");

            entity.Property(e => e.ChatId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.SenderRole).HasMaxLength(20);
            entity.Property(e => e.SentAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
        });

        modelBuilder.Entity<Tip>(entity =>
        {
            entity.HasKey(e => e.TipId).HasName("PK__Tips__2DB1A1C89A08B378");

            entity.Property(e => e.TipId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Amount).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Ride).WithMany(p => p.Tips)
                .HasForeignKey(d => d.RideId)
                .HasConstraintName("FK__Tips__RideId__693CA210");

            entity.HasOne(d => d.User).WithMany(p => p.Tips)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Tips__UserId__6A30C649");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CC4C0F6F8D1A");

            entity.HasIndex(e => e.Email, "UQ__Users__A9D1053411F43B0C").IsUnique();

            entity.Property(e => e.UserId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.PasswordHash).HasMaxLength(200);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.AuditLogId).HasName("PK_AuditLogs");

            entity.Property(e => e.AuditLogId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Timestamp)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.UserType).HasMaxLength(50);
            entity.Property(e => e.Action).HasMaxLength(100).IsRequired();
            entity.Property(e => e.EntityType).HasMaxLength(100);
            entity.Property(e => e.Status).HasMaxLength(50).IsRequired();
            entity.Property(e => e.IpAddress).HasMaxLength(50);
            entity.Property(e => e.UserAgent).HasMaxLength(500);

            entity.HasIndex(e => new { e.UserId, e.UserType }, "IX_AuditLogs_UserId_UserType");
            entity.HasIndex(e => e.Timestamp, "IX_AuditLogs_Timestamp");
            entity.HasIndex(e => e.Action, "IX_AuditLogs_Action");
            entity.HasIndex(e => new { e.EntityType, e.EntityId }, "IX_AuditLogs_EntityType_EntityId");
            entity.HasIndex(e => e.Status, "IX_AuditLogs_Status");
        });

        modelBuilder.Entity<Policy>(entity =>
        {
            entity.HasKey(e => e.PolicyId).HasName("PK_Policies");

            entity.Property(e => e.PolicyId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.PolicyType).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.IsActive).HasDefaultValue(false);

            entity.HasIndex(e => new { e.PolicyType, e.IsActive }, "IX_Policies_Type_IsActive");
            entity.HasIndex(e => new { e.PolicyType, e.Version }, "IX_Policies_Type_Version");
            entity.HasIndex(e => e.CreatedAt, "IX_Policies_CreatedAt");

            entity.HasIndex(e => new { e.PolicyType, e.Version })
                .IsUnique()
                .HasDatabaseName("UQ_Policies_Type_Version");
        });

        modelBuilder.Entity<PromoCode>(entity =>
        {
            entity.HasKey(e => e.PromoCodeId).HasName("PK_PromoCodes");

            entity.Property(e => e.PromoCodeId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.Property(e => e.FlatAmount).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.MinFare).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.ExpiryUtc).HasColumnType("datetime");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.PerUserLimit).HasDefaultValue(1);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())").HasColumnType("datetime");

            entity.HasIndex(e => e.Code).IsUnique().HasDatabaseName("UX_PromoCodes_Code");
        });

        modelBuilder.Entity<PromoRedemption>(entity =>
        {
            entity.HasKey(e => e.PromoRedemptionId).HasName("PK_PromoRedemptions");

            entity.Property(e => e.PromoRedemptionId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.DiscountAmount).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.RedeemedAt).HasDefaultValueSql("(getdate())").HasColumnType("datetime");

            entity.HasOne(d => d.PromoCode)
                .WithMany(p => p.PromoRedemptions)
                .HasForeignKey(d => d.PromoCodeId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_PromoRedemptions_PromoCodes");

            entity.HasIndex(e => new { e.PromoCodeId, e.UserId }, "IX_PromoRedemptions_PromoCodeId_UserId");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
