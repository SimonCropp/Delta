public class Company
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string? Content { get; set; }
    public List<Employee> Employees { get; set; } = null!;
}