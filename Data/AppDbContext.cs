using Microsoft.EntityFrameworkCore;

namespace Juleplanlaegning.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Person> People => Set<Person>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<Bucket> Buckets => Set<Bucket>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<MapImage> MapImages => Set<MapImage>();
    public DbSet<TodoList> TodoLists => Set<TodoList>();
    public DbSet<TodoTask> TodoTasks => Set<TodoTask>();
    public DbSet<TaskPerson> TaskPeople => Set<TaskPerson>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Person>(e =>
        {
            e.Property(p => p.Name).HasMaxLength(200);
        });

        modelBuilder.Entity<Plan>(e =>
        {
            e.Property(p => p.Name).HasMaxLength(200);
        });

        modelBuilder.Entity<Bucket>(e =>
        {
            e.Property(b => b.Name).HasMaxLength(200);
            e.Property(b => b.Color).HasMaxLength(20);
            e.HasOne(b => b.Plan)
                .WithMany(p => p.Buckets)
                .HasForeignKey(b => b.PlanId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Assignment>(e =>
        {
            e.HasIndex(a => new { a.PlanId, a.PersonId }).IsUnique();
            e.HasOne<Plan>().WithMany().HasForeignKey(a => a.PlanId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(a => a.Person).WithMany().HasForeignKey(a => a.PersonId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(a => a.Bucket).WithMany().HasForeignKey(a => a.BucketId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TodoList>(e =>
        {
            e.Property(l => l.Name).HasMaxLength(200);
            e.HasMany(l => l.Tasks).WithOne().HasForeignKey(t => t.TodoListId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TodoTask>(e =>
        {
            e.Property(t => t.Title).HasMaxLength(500);
            e.HasMany(t => t.People).WithOne().HasForeignKey(tp => tp.TodoTaskId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TaskPerson>(e =>
        {
            e.HasKey(tp => new { tp.TodoTaskId, tp.PersonId });
            e.HasOne(tp => tp.Person).WithMany().HasForeignKey(tp => tp.PersonId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
