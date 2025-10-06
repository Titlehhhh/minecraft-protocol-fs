using System.Collections;

namespace Protodef;

public sealed class ProtodefBitField : ProtodefType, IEnumerable<ProtodefBitFieldNode>
{
    //public override string TypeName => "Bitfield";
    private readonly List<ProtodefBitFieldNode> nodes = new();

    public ProtodefBitField(List<ProtodefBitFieldNode> nodes)
    {
        this.nodes = nodes;
    }

    private ProtodefBitField(ProtodefBitField other)
    {
        nodes = other.nodes
            .Select(x => x.Clone())
            .Cast<ProtodefBitFieldNode>()
            .ToList();
    }

    

    public override bool Equals(object? obj)
    {
        if (obj is ProtodefBitField other)
        {
            return other.nodes.SequenceEqual(nodes);
        }

        return false;
    }

    public override int GetHashCode()
    {
        return nodes.GetHashCode();
    }

    public IEnumerator<ProtodefBitFieldNode> GetEnumerator()
    {
        return nodes.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return nodes.GetEnumerator();
    }

    public override object Clone()
    {
        return new ProtodefBitField(this);
    }

    private static readonly IEqualityComparer<ProtodefBitFieldNode> NodeComparer =
        EqualityComparer<ProtodefBitFieldNode>.Default;
}