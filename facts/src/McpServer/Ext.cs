using System.Collections.Generic;
using System.Linq;
using Protodef;

namespace McpServer;

public static class Ext
{
    extension(IEnumerable<KeyValuePair<string, ProtodefType>> enumerable)
    {
        public IEnumerable<KeyValuePair<string, ProtodefType>> RemoveNative()
        {
            return enumerable
                .Where(x => !x.Value.IsCustom("native"));
        }
    }

    extension(IEnumerable<ProtodefType> enumerable)
    {
        public IEnumerable<ProtodefType> RemoveNative()
        {
            return enumerable
                .Where(x => !x!.IsCustom("native"));
        }
    }
}