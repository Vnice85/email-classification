using System;
using System.Collections.Generic;
using EmailClassification.Domain.Enum;
using Microsoft.EntityFrameworkCore;

namespace EmailClassification.Infrastructure.Persistence;

public partial class EmaildbContext : DbContext
{
    public EmaildbContext()
    {
    }

    public EmaildbContext(DbContextOptions<EmaildbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AppUser> AppUsers { get; set; }

    public virtual DbSet<Email> Emails { get; set; }

    public virtual DbSet<EmailDirection> EmailDirections { get; set; }

    public virtual DbSet<EmailLabel> EmailLabels { get; set; }

    public virtual DbSet<Token> Tokens { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

        modelBuilder.Entity<EmailDirection>().HasData(
          new EmailDirection { DirectionId = (int)DirectionStatus.INBOX, DirectionName = DirectionStatus.INBOX.ToString() },
          new EmailDirection { DirectionId = (int)DirectionStatus.SENT, DirectionName = DirectionStatus.SENT.ToString() },
          new EmailDirection { DirectionId = (int)DirectionStatus.DRAFT, DirectionName = DirectionStatus.DRAFT.ToString() },
          new EmailDirection { DirectionId = (int)DirectionStatus.TRASH, DirectionName = DirectionStatus.TRASH.ToString() }
        );

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("app_user_pkey");

            entity.ToTable("app_user");

            entity.Property(e => e.UserId)
                .HasMaxLength(255)
                .HasColumnName("user_id");
            entity.Property(e => e.ProfileImage)
                .HasMaxLength(255)
                .HasColumnName("profile_image");
            entity.Property(e => e.UserName)
                .HasMaxLength(100)
                .HasColumnName("user_name");
        });

        modelBuilder.Entity<Email>(entity =>
        {
            entity.HasKey(e => e.EmailId).HasName("email_pkey");

            entity.ToTable("email");

            entity.HasIndex(e => e.UserId, "IX_email_user_id");

            entity.HasIndex(e => e.DirectionId, "email_direction_id_index");

            entity.HasIndex(e => e.LabelId, "email_label_id_index");

            entity.Property(e => e.EmailId)
                .HasMaxLength(255)
                .HasColumnName("email_id");
            entity.Property(e => e.Body).HasColumnName("body");
            entity.Property(e => e.DirectionId).HasColumnName("direction_id");
            entity.Property(e => e.FromAddress)
                .HasMaxLength(255)
                .HasColumnName("from_address");
            entity.Property(e => e.HistoryId)
                .HasMaxLength(255)
                .HasColumnName("history_id");
            entity.Property(e => e.LabelId).HasColumnName("label_id");
            entity.Property(e => e.PlainText).HasColumnName("plain_text");
            entity.Property(e => e.ReceivedDate).HasColumnName("received_date");
            entity.Property(e => e.SentDate).HasColumnName("sent_date");
            entity.Property(e => e.Snippet)
                .HasMaxLength(255)
                .HasColumnName("snippet");
            entity.Property(e => e.Subject)
                .HasMaxLength(255)
                .HasColumnName("subject");
            entity.Property(e => e.ToAddress)
                .HasMaxLength(255)
                .HasColumnName("to_address");
            entity.Property(e => e.UserId)
                .HasMaxLength(255)
                .HasColumnName("user_id");

            entity.HasOne(d => d.Direction).WithMany(p => p.Emails)
                .HasForeignKey(d => d.DirectionId)
                .HasConstraintName("email_direction_id_fkey");

            entity.HasOne(d => d.Label).WithMany(p => p.Emails)
                .HasForeignKey(d => d.LabelId)
                .HasConstraintName("email_label_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.Emails)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("email_user_id_fkey");
        });

        modelBuilder.Entity<EmailDirection>(entity =>
        {
            entity.HasKey(e => e.DirectionId).HasName("email_direction_pkey");

            entity.ToTable("email_direction");

            entity.Property(e => e.DirectionId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("direction_id");
            entity.Property(e => e.DirectionName)
                .HasMaxLength(50)
                .HasColumnName("direction_name");
        });

        modelBuilder.Entity<EmailLabel>(entity =>
        {
            entity.HasKey(e => e.LabelId).HasName("email_label_pkey");

            entity.ToTable("email_label");

            entity.Property(e => e.LabelId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("label_id");
            entity.Property(e => e.LabelName)
                .HasMaxLength(100)
                .HasColumnName("label_name");
        });

        modelBuilder.Entity<Token>(entity =>
        {
            entity.ToTable("token");

            entity.HasIndex(e => e.UserId, "IX_token_user_id");

            entity.Property(e => e.TokenId).HasColumnName("token_id");
            entity.Property(e => e.AccessToken).HasColumnName("access_token");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.Provider).HasColumnName("provider");
            entity.Property(e => e.RefreshToken).HasColumnName("refresh_token");
            entity.Property(e => e.UserId)
                .HasMaxLength(255)
                .HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Tokens).HasForeignKey(d => d.UserId);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
