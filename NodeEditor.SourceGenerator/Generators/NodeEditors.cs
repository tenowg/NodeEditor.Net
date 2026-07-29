using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NodeEditor.SourceGenerator.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Linq;
using System.Collections.Immutable;

namespace NodeEditor.SourceGenerator.Generators
{
    [Generator]
    internal partial class NodeEditors : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var provider = context.SyntaxProvider.ForAttributeWithMetadataName("NodeEditor.Net.Attributes.NodeCustomEditorAttribute",
                predicate: static (s, _) => s is ClassDeclarationSyntax,
                transform: (ctx, ct) => BuildIndexModel(ctx, ct))
            .Where(static m => m is not null);

            var values = context.AnalyzerConfigOptionsProvider.Select(static (options, _) =>
            {
                options.GlobalOptions.TryGetValue("build_property.RootNamespace", out var rootNamespace);
                return rootNamespace ?? "DefaultName";
            });

            var combinedProvider = provider.Collect().Combine(values);
            context.RegisterSourceOutput(combinedProvider, BuildCustomNodeRegistry);
        }

        private CustomEditorModel? BuildIndexModel(GeneratorAttributeSyntaxContext ctx, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var compilation = ctx.SemanticModel.Compilation;
            var nodeEditorAttrSymbol = compilation.GetTypeByMetadataName("NodeEditor.Net.Attributes.NodeCustomEditorAttribute");

            var classSymbol = (INamedTypeSymbol)ctx.TargetSymbol;

            var nodeCustomEditorAttribute = classSymbol.GetAttributes()
                .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, nodeEditorAttrSymbol));

            if (nodeCustomEditorAttribute == null) return null;

            var hintType = nodeCustomEditorAttribute.ConstructorArguments[0].Value?.ToString();

            if (string.IsNullOrWhiteSpace(hintType)) return null;

            return new CustomEditorModel
            {
                HintTypeName = hintType
            };
        }
    }
}
