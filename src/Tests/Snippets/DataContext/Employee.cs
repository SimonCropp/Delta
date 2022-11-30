using System.ComponentModel.DataAnnotations.Schema;

public class Employee : IRowVersion
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }
    public ulong RowVersion { get; set; }
    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public string? Content { get; set; }
    public int Age { get; set; }
}