public class SampleDbContext(DbContextOptions options) :
    DbContext(options)
{
    public DbSet<Employee> Employees { get; set; } = null!;
    public DbSet<Company> Companies { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        var company = builder.Entity<Company>();
        company.HasKey(_ => _.Id);
        company
            .HasMany(_ => _.Employees)
            .WithOne(_ => _.Company)
            .IsRequired();
        company
            .Property(_ => _.RowVersion)
            .IsRowVersion()
            .HasConversion<byte[]>();

        var employee = builder.Entity<Employee>();
        employee.HasKey(_ => _.Id);
        employee
            .Property(_ => _.RowVersion)
            .IsRowVersion()
            .HasConversion<byte[]>();
    }
}