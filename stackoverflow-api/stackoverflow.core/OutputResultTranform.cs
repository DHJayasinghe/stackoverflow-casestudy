namespace stackoverflow.core;

public record class OutputResultTranform
{
    public Type Type { get; }
    public TransformCategory Category { get; }
    public string Name { get; }

    public OutputResultTranform(Type type, TransformCategory category, string name)
    {
        Type = type;
        Category = category;
        Name = name;
    }
}
