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

    private bool OrderingEquals(List<ProtodefBitFieldNode> other)
    {
        if (other.Count != nodes.Count)
            return false;

        for (var i = 0; i < other.Count; i++)
        {
            var f1 = nodes[i];
            var f2 = other[i];

            if (!f1.Equals(f2))
                return false;
        }

        return true;
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
}