namespace Protodef;

public sealed class ProtodefCustomType : ProtodefType
{
    public ProtodefCustomType(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public override string ToString()
    {
        return Name;
    }

    public override object Clone()
    {
        return new ProtodefCustomType(Name);
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is not ProtodefCustomType other)
        {
            return false;
        }

        return Name == other.Name;
    }
}