public class SampleDbContext :
    DbContext
{
    public DbSet<Employee> Employees { get; set; } = null!;
    public DbSet<Company> Companies { get; set; } = null!;

    public SampleDbContext(DbContextOptions options) :
        base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var companyBuilder = modelBuilder.Entity<Company>();
        companyBuilder
            .HasMany(c => c.Employees)
            .WithOne(e => e.Company)
            .IsRequired();
        companyBuilder.TryAddRowVersionProperty();

        var employeeBuilder = modelBuilder.Entity<Employee>();
        employeeBuilder.TryAddRowVersionProperty();
    }
}