namespace Taskify.WebApi.Domain;

public interface IAuditable
{
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? LastModifiedAtUtc { get; set; }
}