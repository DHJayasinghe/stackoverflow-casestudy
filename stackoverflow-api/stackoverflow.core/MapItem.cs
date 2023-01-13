namespace stackoverflow.core;

public sealed class MapItem
{
    public Type Type { get; private set; }
    public DataRetriveType DataRetriveType { get; private set; }
    public string PropertyName { get; private set; }

    public MapItem(Type type, DataRetriveType dataRetriveType, string propertyName)
    {
        Type = type;
        DataRetriveType = dataRetriveType;
        PropertyName = propertyName;
    }
}
