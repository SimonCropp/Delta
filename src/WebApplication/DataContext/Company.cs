public class Company
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }
    public ulong RowVersion { get; set; }
    public string? Content { get; set; }
    public List<Employee> Employees { get; set; } = null!;
}