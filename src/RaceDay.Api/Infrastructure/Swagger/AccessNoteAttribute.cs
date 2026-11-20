namespace RaceDay.Api.Infrastructure.Swagger;

[AttributeUsage(AttributeTargets.Method)]
public sealed class AccessNoteAttribute(string note) : Attribute
{
    public string Note { get; } = note;
}
