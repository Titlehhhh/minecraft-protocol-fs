using System;
using System.ComponentModel;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Repositories;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Server;
using Protodef;
using Toon.Format;

namespace McpServer.Tools;

[McpServerToolType]
public static class CodeGenTool
{
    [McpServerTool(
         Name = "generate_packet"
     ), Description("Generates a C# packet class from a Minecraft protocol packet identifier. " +
                    "The tool resolves the packet schema across protocol versions, " +
                    "runs code generation using sampling, saves the generated code as an artifact, " +
                    "and returns a link to download the resulting .cs file.")]
    public static async Task<GenerationResult> GeneratePacket(
        ModelContextProtocol.Server.McpServer thisServer,
        IProtocolRepository repository,
        IArtifactsRepository artifacts,
        LinkGenerator linkGenerator,
        IConfiguration configuration,
        
        
        [Description(
            "Packet identifier to generate code for. " +
            "Example: 'play.toServer.packet_look' or 'play.toClient.packet_action_bar'."
        )]
        string id,
        CancellationToken cancellationToken)
    {
        var history = repository.GetTypeHistory(id);

        var json = JsonSerializer.SerializeToNode(history, ProtodefType.DefaultJsonOptions)!;
        var toon = ToonEncoder.EncodeNode(json, new ToonEncodeOptions());


        string system = """
                        You are a deterministic C# code generator.

                        Your task is to fill in a provided packet class skeleton.

                        Rules:
                        - Do NOT change the structure of the skeleton.
                        - Replace placeholders and instructional comments with concrete code.
                        - Remove comments only where code is generated.
                        - Do NOT add new fields, methods, or helper logic.
                        - Do NOT invent types or IO methods.
                        - Use ONLY the IO methods explicitly listed in the input.
                        - Implement version-specific logic strictly using switch(protocolVersion).
                        - Common fields must be serialized/deserialized in ALL versions.
                        - Exactly ONE version-specific field structure must be active per version.
                        - For unsupported protocol versions, always throw.
                        - Output ONLY valid C# code. No explanations.

                        """;

        string prompt = """
                        CLASS SKELETON:

                        // Packet metadata:
                        // - Name: <PacketName>
                        // - State: <PacketState>
                        // - Direction: <PacketDirection>
                        [PacketInfo("<PacketName>", PacketState.<State>, PacketDirection.<Direction>)]
                        public sealed partial class <PacketClassName> : IClientPacket
                        {
                            // =====================================================================
                            // Supported protocol ranges
                            // =====================================================================
                            // Each protocol range corresponds to ONE version-specific field structure.
                            public static readonly ProtocolRange[] SupportedVersionsStatic =
                            {
                                // new(<from>, <to>),
                                // new(<from>, MinecraftVersion.LatestProtocol)
                            };

                            // =====================================================================
                            // Common fields
                            // =====================================================================
                            // These fields exist in ALL protocol versions.
                            // They are always read/written before version-specific fields.
                            //
                            // Example:
                            // public float Yaw { get; set; }
                            // public float Pitch { get; set; }


                            // =====================================================================
                            // Version-specific field containers
                            // =====================================================================
                            // Exactly ONE of these properties must be non-null at runtime,
                            // depending on the protocol version.
                            //
                            // Naming convention:
                            // - V<From>_<To>Fields
                            // - Use 'Last' for the latest protocol range.
                            //
                            // Example:
                            // public VFirst_767Fields? VFirst_767 { get; set; }
                            // public V768_LastFields? V768_Last { get; set; }


                            // =====================================================================
                            // Serialization
                            // =====================================================================
                            // Rules:
                            // - Select version by protocolVersion
                            // - Read common fields first
                            // - Then read/write version-specific fields
                            // - Throw if version-specific fields are missing
                            internal void Serialize(ref MinecraftPrimitiveWriter writer, int protocolVersion)
                            {
                                switch (protocolVersion)
                                {
                                    // Example:
                                    // case >= <from> and <= <to>:
                                    // {
                                    //     var fields = <VersionProperty>
                                    //         ?? throw new InvalidOperationException("<PacketName> <VersionName> fields missing.");
                                    //
                                    //     // Write common fields
                                    //     writer.WriteFloat(Yaw);
                                    //     writer.WriteFloat(Pitch);
                                    //
                                    //     // Write version-specific fields
                                    //     writer.WriteBoolean(fields.OnGround);
                                    //     return;
                                    // }

                                    default:
                                        ThrowHelper.ThrowProtocolNotSupported(
                                            nameof(<PacketClassName>),
                                            protocolVersion,
                                            SupportedVersionsStatic);
                                        return;
                                }
                            }

                            // =====================================================================
                            // Deserialization
                            // =====================================================================
                            // Rules:
                            // - Select version by protocolVersion
                            // - Read common fields first
                            // - Instantiate ONLY the matching version-specific structure
                            // - Set all other version-specific properties to null
                            internal void Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
                            {
                                switch (protocolVersion)
                                {
                                    // Example:
                                    // case >= <from> and <= <to>:
                                    //     // Read common fields
                                    //     Yaw = reader.ReadFloat();
                                    //     Pitch = reader.ReadFloat();
                                    //
                                    //     // Read version-specific fields
                                    //     <VersionProperty> = new <VersionStruct>
                                    //     {
                                    //         OnGround = reader.ReadBoolean()
                                    //     };
                                    //
                                    //     // Reset other version structures
                                    //     <OtherVersionProperty> = null;
                                    //     return;

                                    default:
                                        ThrowHelper.ThrowProtocolNotSupported(
                                            nameof(<PacketClassName>),
                                            protocolVersion,
                                            SupportedVersionsStatic);
                                        return;
                                }
                            }

                            // =====================================================================
                            // Interface forwarding
                            // =====================================================================
                            void IPacket.Serialize(ref MinecraftPrimitiveWriter writer, int protocolVersion)
                                => Serialize(ref writer, protocolVersion);

                            void IPacket.Deserialize(ref MinecraftPrimitiveReader reader, int protocolVersion)
                                => Deserialize(ref reader, protocolVersion);

                            // =====================================================================
                            // Version-specific field structures
                            // =====================================================================
                            // These structs contain ONLY fields that differ between protocol versions.
                            // They must NOT include common fields.
                            //
                            // Example:
                            // public struct VFirst_767Fields
                            // {
                            //     public bool OnGround { get; set; }
                            // }
                            //
                            // public struct V768_LastFields
                            // {
                            //     public byte Flags { get; set; }
                            // }
                        }

                        AVAILABLE IO METHODS:

                        Writer:
                        - writer.WriteBoolean(bool)
                        - writer.WriteByte(byte)
                        - writer.WriteUnsignedByte(byte)
                        - writer.WriteFloat(float)
                        - writer.WriteDouble(double)
                        - writer.WriteVarInt(int)
                        - writer.WriteString(string)
                        - writer.WriteAnonymousNbtTag(NbtTag, int)

                        Reader:
                        - reader.ReadBoolean() -> bool
                        - reader.ReadByte() -> byte
                        - reader.ReadUnsignedByte() -> byte
                        - reader.ReadFloat() -> float
                        - reader.ReadDouble() -> double
                        - reader.ReadVarInt() -> int
                        - reader.ReadString() -> string
                        - reader.ReadNbtTag() -> NbtTag

                        """;

        prompt += $"\n SCHEMA (TOON):\n\n{toon}";

        var messages = new ChatMessage[]
        {
            new(ChatRole.System, system),
            new(ChatRole.User, prompt)
        };

        var chat = thisServer.AsSamplingChatClient();

        var response = await chat.GetResponseAsync(
            messages,
            new ChatOptions
            {
                Temperature = 0,
                MaxOutputTokens = 2000,
            },
            cancellationToken);

        var code = response.ToString();

        var fileName = $"{history.Name}.cs";
        var artifact = await artifacts.SaveTextAsync(fileName, code, cancellationToken: cancellationToken);

        var baseUrl = configuration["PublicBaseUrl"] ?? throw new InvalidOperationException();

        var path = linkGenerator.GetPathByName("GetArtifacts", new { artifact.Id });

        path = $"{baseUrl}{path}";

        return new GenerationResult
        {
            Link = path,
            Name = fileName
        };
    }
}