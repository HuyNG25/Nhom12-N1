using Microsoft.EntityFrameworkCore;
using ProjectMemberService.Models;

namespace ProjectMemberService.Data
{
    public class ProjectDbContext : DbContext
    {
        public ProjectDbContext(DbContextOptions<ProjectDbContext> options) : base(options) { }

        public DbSet<Project> Projects => Set<Project>();
        public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
        public DbSet<Sprint> Sprints => Set<Sprint>();
        public DbSet<Milestone> Milestones => Set<Milestone>();
        public DbSet<SystemAdmin> SystemAdmins => Set<SystemAdmin>();


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Project
            modelBuilder.Entity<Project>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(2000);
                entity.Property(e => e.Color).HasMaxLength(7);
                entity.Property(e => e.CreatedBy).IsRequired();
                entity.Property(e => e.Status).HasConversion<string>();
            });

            // ProjectMember
            modelBuilder.Entity<ProjectMember>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserId).IsRequired();
                entity.Property(e => e.DisplayName).HasMaxLength(200);
                entity.Property(e => e.Email).HasMaxLength(300);
                entity.Property(e => e.Role).HasConversion<string>();

                entity.HasOne(e => e.Project)
                      .WithMany(p => p.Members)
                      .HasForeignKey(e => e.ProjectId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Mỗi user chỉ được thêm 1 lần vào project
                entity.HasIndex(e => new { e.ProjectId, e.UserId }).IsUnique();
            });

            // Sprint
            modelBuilder.Entity<Sprint>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Goal).HasMaxLength(1000);
                entity.Property(e => e.Status).HasConversion<string>();

                entity.HasOne(e => e.Project)
                      .WithMany(p => p.Sprints)
                      .HasForeignKey(e => e.ProjectId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Milestone
            modelBuilder.Entity<Milestone>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(1000);

                entity.HasOne(e => e.Project)
                      .WithMany(p => p.Milestones)
                      .HasForeignKey(e => e.ProjectId)
                      .OnDelete(DeleteBehavior.Cascade);
            });



            // SystemAdmin
            modelBuilder.Entity<SystemAdmin>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserId).IsRequired().HasMaxLength(200);
                entity.HasIndex(e => e.UserId).IsUnique();
            });

            // ===================================================================
            // SEED DATA - 20 users, 1 Project, phân quyền đầy đủ
            // ===================================================================
            var D = DateTimeKind.Utc;
            DateTime Utc(int y, int m, int d) => DateTime.SpecifyKind(new DateTime(y, m, d), D);

            var projectId = new Guid("a1000000-0000-0000-0000-000000000001");

            // ----- PROJECT -----
            modelBuilder.Entity<Project>().HasData(new Project
            {
                Id        = projectId,
                Name      = "ScrumKanban - Dự án chính",
                Description = "Dự án quản lý công việc chính của nhóm. Sử dụng tài khoản owner_1 (Owner) để toàn quyền điều hành.",
                StartDate = Utc(2026, 1, 1),
                EndDate   = Utc(2026, 12, 31),
                Color     = "#6366F1",
                Status    = ProjectStatus.Active,
                CreatedBy = "owner_1",
                CreatedAt = Utc(2026, 1, 1),
                UpdatedAt = Utc(2026, 1, 1)
            });

            // ----- HELPER: tạo ProjectMember nhanh -----
            var members = new List<ProjectMember>();
            int idx = 0;

            ProjectMember Make(string uid, string name, string email, MemberRole role) =>
                new ProjectMember
                {
                    Id          = new Guid($"b{++idx:D7}-0000-0000-0000-000000000000"),
                    ProjectId   = projectId,
                    UserId      = uid,
                    DisplayName = name,
                    Email       = email,
                    Role        = role,
                    JoinedAt    = Utc(2026, 1, 1)
                };

            // 1 OWNER - điều hành toàn bộ
            members.Add(Make("admin",   "Nguyễn Văn Admin",    "admin@gmail.com",    MemberRole.Owner));

            // 6 MANAGERS - quản lý, sửa, tạo sprint, quản lý thành viên
            members.Add(Make("duymanh", "Nguyễn Duy Mạnh",  "manh.nguyen@gmail.com",  MemberRole.Manager));
            members.Add(Make("manager_2", "Lê Minh Châu (Manager)",   "manager2@project.dev",  MemberRole.Manager));
            members.Add(Make("manager_3", "Phạm Thùy Dung (Manager)", "manager3@project.dev",  MemberRole.Manager));
            members.Add(Make("manager_4", "Hoàng Đức Em (Manager)",   "manager4@project.dev",  MemberRole.Manager));
            members.Add(Make("manager_5", "Vũ Thị Fang (Manager)",    "manager5@project.dev",  MemberRole.Manager));
            members.Add(Make("manager_6", "Đặng Quốc Gia (Manager)",  "manager6@project.dev",  MemberRole.Manager));

            // 7 MEMBERS - xem project, xem sprint, làm việc
            members.Add(Make("tranailinh",  "Trần Ái Linh",    "linh.tran@gmail.com",   MemberRole.Member));
            members.Add(Make("member_2",  "Ngô Văn Hùng (Member)",   "member2@project.dev",   MemberRole.Member));
            members.Add(Make("member_3",  "Đinh Thị Iris (Member)",  "member3@project.dev",   MemberRole.Member));
            members.Add(Make("member_4",  "Cao Văn Kiên (Member)",   "member4@project.dev",   MemberRole.Member));
            members.Add(Make("member_5",  "Lý Thị Lan (Member)",     "member5@project.dev",   MemberRole.Member));
            members.Add(Make("member_6",  "Tô Quang Minh (Member)",  "member6@project.dev",   MemberRole.Member));
            members.Add(Make("member_7",  "Hà Thị Nga (Member)",     "member7@project.dev",   MemberRole.Member));

            // 6 VIEWERS - chỉ xem project và sprint
            members.Add(Make("viewer_1",  "Phan Văn Oanh (Viewer)",  "viewer1@project.dev",   MemberRole.Viewer));
            members.Add(Make("viewer_2",  "Đỗ Thị Phương (Viewer)",  "viewer2@project.dev",   MemberRole.Viewer));
            members.Add(Make("viewer_3",  "Trịnh Văn Quý (Viewer)",  "viewer3@project.dev",   MemberRole.Viewer));
            members.Add(Make("viewer_4",  "Lưu Thị Rum (Viewer)",    "viewer4@project.dev",   MemberRole.Viewer));
            members.Add(Make("viewer_5",  "Mai Văn Sơn (Viewer)",    "viewer5@project.dev",   MemberRole.Viewer));
            members.Add(Make("viewer_6",  "Kiều Thị Tuyết (Viewer)", "viewer6@project.dev",   MemberRole.Viewer));

            modelBuilder.Entity<ProjectMember>().HasData(members);

            // ----- SYSTEM ADMIN -----
            modelBuilder.Entity<SystemAdmin>().HasData(new SystemAdmin
            {
                Id = new Guid("d1000000-0000-0000-0000-000000000001"),
                UserId = "admin",
                CreatedAt = Utc(2026, 1, 1)
            });

            // ----- 2 DEMO SPRINTS -----
            modelBuilder.Entity<Sprint>().HasData(
                new Sprint
                {
                    Id        = new Guid("c1000000-0000-0000-0000-000000000001"),
                    ProjectId = projectId,
                    Name      = "Sprint 1 - Khởi động",
                    Goal      = "Hoàn thiện kiến trúc hệ thống và xây dựng các API cơ bản",
                    StartDate = Utc(2026, 1, 6),
                    EndDate   = Utc(2026, 1, 20),
                    Status    = SprintStatus.Completed,
                    CreatedAt = Utc(2026, 1, 1)
                },
                new Sprint
                {
                    Id        = new Guid("c2000000-0000-0000-0000-000000000002"),
                    ProjectId = projectId,
                    Name      = "Sprint 2 - Phát triển chính",
                    Goal      = "Xây dựng luồng phân quyền, quản lý thành viên, publish event",
                    StartDate = Utc(2026, 1, 20),
                    EndDate   = Utc(2026, 2, 3),
                    Status    = SprintStatus.Active,
                    CreatedAt = Utc(2026, 1, 18)
                }
            );
        }
    }
}
