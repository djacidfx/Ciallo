using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace SourceGeneration;

[Generator]
#pragma warning disable RS1036
public class CommandBuilderGenerator : IIncrementalGenerator
#pragma warning restore RS1036
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
            "CommandBuilderAttribute.g.cs", AttributeSourceCode));

        IncrementalValuesProvider<CommandToGenerate?> enumsToGenerate = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "SourceGeneration.CommandBuilderAttribute",
                predicate: static (s, _) => s is ClassDeclarationSyntax n && n.AttributeLists.Count > 0,
                transform: static (ctx, _) => GetSemanticTarget(ctx))
            .Where(static commandToGenerate => commandToGenerate is not null);

        context.RegisterSourceOutput(enumsToGenerate, Execute);
    }

    private static void Execute(SourceProductionContext context, CommandToGenerate? source)
    {
        if (source is { } value)
        {
            // generate the source code and add it to the output
            string result =
                $$"""

                  """;

            context.AddSource($"CommandBuilderExtensions.{value.Name}.g.cs", SourceText.From(result, Encoding.UTF8));
        }
    }

    private static CommandToGenerate? GetSemanticTarget(GeneratorAttributeSyntaxContext ctx)
    {
        // we know the node is a EnumDeclarationSyntax thanks to IsSyntaxTargetForGeneration
        var node = (EnumDeclarationSyntax)ctx.TargetNode;

        // loop through all the attributes on the method
        foreach (AttributeListSyntax attributeListSyntax in node.AttributeLists)
        {
            foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
            {
                if (ctx.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol is not IMethodSymbol attributeSymbol)
                {
                    // weird, we couldn't get the symbol, ignore it
                    continue;
                }

                INamedTypeSymbol attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                string fullName = attributeContainingTypeSymbol.ToDisplayString();

                if (fullName == "Ciallo.Command.CommandBuilderAttribute")
                    return GetCommandToGenerate(ctx.SemanticModel, node);
            }
        }

        return null;
    }

    private static CommandToGenerate? GetCommandToGenerate(SemanticModel ctxSemanticModel, EnumDeclarationSyntax node)
    {
        throw new NotImplementedException();
    }

    public static readonly string AttributeSourceCode =
        """
        // Genereated code from Ciallo.SourceGeneration
        using System;
        namespace Ciallo.Command
        {
            [System.AttributeUsage(System.AttributeTargets.Class)]
            public class CommandBuilderAttribute : Attribute
            {
            }
        };
        """;
}

public readonly record struct CommandToGenerate
{
    public readonly string Name;

    public CommandToGenerate(string name)
    {
        Name = name;
    }
}