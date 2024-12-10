public class Company
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public ulong RowVersion { get; set; }
    public string? Content { get; set; }
    public List<Employee> Employees { get; set; } = null!;
}