using Microsoft.EntityFrameworkCore;
using SmartSortingServer.Models;

namespace SmartSortingServer.Data {
    public class AppDbContext : DbContext {

        // DB 연결 설정을 전달받는 생성자
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) {
        }

        // users 테이블
        public DbSet<User> Users { get; set; } = null!;

        // product_types 테이블
        public DbSet<ProductType> ProductTypes { get; set; } = null!;

        // system_components 테이블
        public DbSet<SystemComponent> SystemComponents { get; set; } = null!;

        // production_sessions 테이블
        public DbSet<ProductionSession> ProductionSessions { get; set; } = null!;

        // product_detections 테이블
        public DbSet<ProductDetection> ProductDetections { get; set; } = null!;

        // alerts 테이블
        public DbSet<Alert> Alerts { get; set; } = null!;

        // C# 모델과 DB 테이블의 연결 규칙 설정
        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);

            // User 모델 ↔ users 테이블
            modelBuilder.Entity<User>(entity => {
                entity.ToTable("users");

                // 기본키
                entity.HasKey(e => e.UserId);

                entity.Property(e => e.UserId)
                    .HasColumnName("user_id");

                entity.Property(e => e.LoginId)
                    .HasColumnName("login_id")
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(e => e.PasswordHash)
                    .HasColumnName("password_hash")
                    .HasMaxLength(255)
                    .IsRequired();

                entity.Property(e => e.Name)
                    .HasColumnName("name")
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(e => e.Role)
                    .HasColumnName("role")
                    .HasMaxLength(20)
                    .IsRequired();

                entity.Property(e => e.CreatedAt)
                    .HasColumnName("created_at");

                // login_id는 중복 불가
                entity.HasIndex(e => e.LoginId)
                    .IsUnique();
            });

            // ProductType 모델 ↔ product_types 테이블
            modelBuilder.Entity<ProductType>(entity => {
                entity.ToTable("product_types");

                // 기본키
                entity.HasKey(e => e.ProductTypeId);

                entity.Property(e => e.ProductTypeId)
                    .HasColumnName("product_type_id");

                entity.Property(e => e.ProductTypeCode)
                    .HasColumnName("product_type_code")
                    .HasMaxLength(20)
                    .IsRequired();

                entity.Property(e => e.ProductName)
                    .HasColumnName("product_name")
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(e => e.UnitPerSet)
                    .HasColumnName("unit_per_set")
                    .IsRequired();

                entity.Property(e => e.CreatedAt)
                    .HasColumnName("created_at");

                // product_type_code는 중복 불가
                entity.HasIndex(e => e.ProductTypeCode)
                    .IsUnique();
            });

            // SystemComponent 모델 ↔ system_components 테이블
            modelBuilder.Entity<SystemComponent>(entity => {
                entity.ToTable("system_components");

                // 기본키
                entity.HasKey(e => e.ComponentId);

                entity.Property(e => e.ComponentId)
                    .HasColumnName("component_id");

                entity.Property(e => e.ComponentCode)
                    .HasColumnName("component_code")
                    .HasMaxLength(30)
                    .IsRequired();

                entity.Property(e => e.ComponentName)
                    .HasColumnName("component_name")
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(e => e.ComponentType)
                    .HasColumnName("component_type")
                    .HasMaxLength(20)
                    .IsRequired();

                entity.Property(e => e.CurrentStatus)
                    .HasColumnName("current_status")
                    .HasMaxLength(20)
                    .IsRequired();

                entity.Property(e => e.StatusUpdatedAt)
                    .HasColumnName("status_updated_at");

                // component_code는 중복 불가
                entity.HasIndex(e => e.ComponentCode)
                    .IsUnique();
            });

            // ProductionSession 모델 ↔ production_sessions 테이블
            modelBuilder.Entity<ProductionSession>(entity => {
                entity.ToTable("production_sessions");

                // 기본키
                entity.HasKey(e => e.SessionId);

                entity.Property(e => e.SessionId)
                    .HasColumnName("session_id");

                entity.Property(e => e.UserId)
                    .HasColumnName("user_id");

                entity.Property(e => e.TargetChocolateSetCount)
                    .HasColumnName("target_chocolate_set_count")
                    .IsRequired();

                entity.Property(e => e.TargetCandyCount)
                    .HasColumnName("target_candy_count")
                    .IsRequired();

                entity.Property(e => e.ChocolateCount)
                    .HasColumnName("chocolate_count")
                    .IsRequired();

                entity.Property(e => e.CandyCount)
                    .HasColumnName("candy_count")
                    .IsRequired();

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .HasMaxLength(20)
                    .IsRequired();

                entity.Property(e => e.StartedAt)
                    .HasColumnName("started_at");

                entity.Property(e => e.EndedAt)
                    .HasColumnName("ended_at");

                entity.Property(e => e.UpdatedAt)
                    .HasColumnName("updated_at");

                // production_sessions.user_id → users.user_id
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId);
            });

            // ProductDetection 모델 ↔ product_detections 테이블
            modelBuilder.Entity<ProductDetection>(entity => {
                entity.ToTable("product_detections");

                // 기본키
                entity.HasKey(e => e.ProductDetectionId);

                entity.Property(e => e.ProductDetectionId)
                    .HasColumnName("product_detection_id");

                entity.Property(e => e.SessionId)
                    .HasColumnName("session_id");

                entity.Property(e => e.ProductTypeId)
                    .HasColumnName("product_type_id");

                entity.Property(e => e.Confidence)
                    .HasColumnName("confidence")
                    .HasPrecision(5, 4);

                entity.Property(e => e.ImagePath)
                    .HasColumnName("image_path")
                    .HasMaxLength(255);

                entity.Property(e => e.ClassificationStatus)
                    .HasColumnName("classification_status")
                    .HasMaxLength(20)
                    .IsRequired();

                entity.Property(e => e.DetectedAt)
                    .HasColumnName("detected_at");

                // product_detections.session_id → production_sessions.session_id
                entity.HasOne(e => e.ProductionSession)
                    .WithMany()
                    .HasForeignKey(e => e.SessionId);

                // product_detections.product_type_id → product_types.product_type_id
                entity.HasOne(e => e.ProductType)
                    .WithMany()
                    .HasForeignKey(e => e.ProductTypeId);
            });

            // Alert 모델 ↔ alerts 테이블
            modelBuilder.Entity<Alert>(entity => {
                entity.ToTable("alerts");

                // 기본키
                entity.HasKey(e => e.AlertId);

                entity.Property(e => e.AlertId)
                    .HasColumnName("alert_id");

                entity.Property(e => e.SessionId)
                    .HasColumnName("session_id");

                entity.Property(e => e.ComponentId)
                    .HasColumnName("component_id");

                entity.Property(e => e.ProductDetectionId)
                    .HasColumnName("product_detection_id");

                entity.Property(e => e.CheckedByUserId)
                    .HasColumnName("checked_by_user_id");

                entity.Property(e => e.AlertType)
                    .HasColumnName("alert_type")
                    .HasMaxLength(20)
                    .IsRequired();

                entity.Property(e => e.Priority)
                    .HasColumnName("priority")
                    .HasMaxLength(20)
                    .IsRequired();

                entity.Property(e => e.RecoveryStatus)
                    .HasColumnName("recovery_status")
                    .HasMaxLength(20);

                entity.Property(e => e.CheckStatus)
                    .HasColumnName("check_status")
                    .HasMaxLength(20);

                entity.Property(e => e.AlertMessage)
                    .HasColumnName("alert_message")
                    .HasMaxLength(1000)
                    .IsRequired();

                entity.Property(e => e.CreatedAt)
                    .HasColumnName("created_at");

                entity.Property(e => e.RecoveredAt)
                    .HasColumnName("recovered_at");

                entity.Property(e => e.CheckedAt)
                    .HasColumnName("checked_at");

                // alerts.session_id → production_sessions.session_id
                entity.HasOne(e => e.ProductionSession)
                    .WithMany()
                    .HasForeignKey(e => e.SessionId);

                // alerts.component_id → system_components.component_id
                entity.HasOne(e => e.SystemComponent)
                    .WithMany()
                    .HasForeignKey(e => e.ComponentId);

                // alerts.product_detection_id → product_detections.product_detection_id
                entity.HasOne(e => e.ProductDetection)
                    .WithMany()
                    .HasForeignKey(e => e.ProductDetectionId);

                // alerts.checked_by_user_id → users.user_id
                entity.HasOne(e => e.CheckedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.CheckedByUserId);
            });
        }
    }
}