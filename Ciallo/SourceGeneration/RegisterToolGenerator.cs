using Microsoft.CodeAnalysis;

namespace SourceGeneration;

[Generator]
public class RegisterToolGenerator : IIncrementalGenerator
{
    public static readonly string AttributeSourceCode =
        """
        // Generated code from Ciallo.SourceGeneration.RegisterToolGenerator

        using System;
        namespace Ciallo.Tool
        {
            // Register a tool class implementing ITool attribute.
            // See ToolManager for usage and implementation details.
            [System.AttributeUsage(System.AttributeTargets.Class)]
            public class RegisterToolAttribute(ToolButton button) : Attribute
            {

            }
        };
        """;

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
            "RegisterToolAttribute.g.cs", AttributeSourceCode));
    }
}