using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dreamine.MVVM.Generators
{
	[Generator]
	public sealed class DreamineAutoWiringGenerator : IIncrementalGenerator
	{
		public void Initialize(IncrementalGeneratorInitializationContext context)
		{
			context.RegisterPostInitializationOutput(ctx =>
			{
				ctx.AddSource("DebugLog.g.cs", SourceText.From("// ✅ Generator Executed", Encoding.UTF8));
			});

			var modelAttrSymbol = context.CompilationProvider
				.Select<Compilation, INamedTypeSymbol?>(static (c, _) =>
					c.GetTypeByMetadataName("Dreamine.MVVM.Attributes.DreamineModelAttribute"));

			var eventAttrSymbol = context.CompilationProvider
				.Select<Compilation, INamedTypeSymbol?>(static (c, _) =>
					c.GetTypeByMetadataName("Dreamine.MVVM.Attributes.DreamineEventAttribute"));

			var propAttrSymbol = context.CompilationProvider
				.Select<Compilation, INamedTypeSymbol?>(static (c, _) =>
					c.GetTypeByMetadataName("Dreamine.MVVM.Attributes.DreaminePropertyAttribute"));

			var combinedSymbols = modelAttrSymbol
				.Combine(eventAttrSymbol)
				.Combine(propAttrSymbol)
				.Select<((INamedTypeSymbol?, INamedTypeSymbol?), INamedTypeSymbol?), (INamedTypeSymbol?, INamedTypeSymbol?, INamedTypeSymbol?)>(
					static (tuple, _) => (
						Model: tuple.Item1.Item1,
						Event: tuple.Item1.Item2,
						Property: tuple.Item2
					));

			var candidates = context.SyntaxProvider
				.CreateSyntaxProvider(
					static (s, _) => s is PropertyDeclarationSyntax { AttributeLists.Count: > 0 }
								  || s is FieldDeclarationSyntax { AttributeLists.Count: > 0 },
					static (ctx, _) => ctx
				)
				.Combine(combinedSymbols)
				.Select(static (pair, _) =>
				{
					var ctx = pair.Left;
					var (model, ev, prop) = pair.Right;
					return GetUnifiedCandidateSymbolBased(ctx, model, ev, prop);
				})
				.Where(static x => x is not null)
				.Select(static (x, _) => x!.Value)
				.Collect();

			var grouped = candidates.Select(static (list, _) =>
				list.GroupBy(item => item.Symbol.ContainingType, SymbolEqualityComparer.Default)
			);

			context.RegisterSourceOutput(grouped, (spc, groups) =>
			{
				foreach (var classGroup in groups)
				{
					var classSymbol = (INamedTypeSymbol)classGroup.Key;

					foreach (var item in classGroup)
					{
						var fieldName = item.Symbol.Name;
						var propName = item.IsField
							? char.ToUpper(fieldName.TrimStart('_')[0]) + fieldName.TrimStart('_').Substring(1)
							: fieldName;

						var exists = classSymbol.GetMembers()
							.OfType<IPropertySymbol>()
							.Any(p => p.Name == propName);
						if (exists) continue;

						var source = GenerateCodeForSingleProperty(classSymbol, item.Syntax, item.Symbol, item.Kind, item.IsField);
						var ns = Sanitize(classSymbol.ContainingNamespace.ToDisplayString());
						var fileName = $"{ns}_{classSymbol.Name}_{propName}_AutoProp.g.cs";
						spc.AddSource(fileName, SourceText.From(source, Encoding.UTF8));
					}
				}
			});
		}

		private static (SyntaxNode Syntax, ISymbol Symbol, string Kind, bool IsField)? GetUnifiedCandidateSymbolBased(
			GeneratorSyntaxContext context,
			INamedTypeSymbol? modelAttr,
			INamedTypeSymbol? eventAttr,
			INamedTypeSymbol? propertyAttr)
		{
			if (context.Node is PropertyDeclarationSyntax prop)
			{
				var symbol = context.SemanticModel.GetDeclaredSymbol(prop) as IPropertySymbol;
				if (symbol is null || prop.Initializer != null) return null;

				foreach (var attr in symbol.GetAttributes())
				{
					if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, modelAttr))
						return (prop, symbol, "model", false);
					if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, eventAttr))
						return (prop, symbol, "event", false);
					if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, propertyAttr))
						return (prop, symbol, "property", false);
				}
			}
			else if (context.Node is FieldDeclarationSyntax field)
			{
				foreach (var variable in field.Declaration.Variables)
				{
					var symbol = context.SemanticModel.GetDeclaredSymbol(variable);
					if (symbol is IFieldSymbol fieldSymbol)
					{
						foreach (var attr in fieldSymbol.GetAttributes())
						{
							if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, modelAttr))
								return (field, fieldSymbol, "model", true);
							if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, eventAttr))
								return (field, fieldSymbol, "event", true);
							if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, propertyAttr))
								return (field, fieldSymbol, "property", true);
						}
					}
				}
			}
			return null;
		}

		private static string GenerateCodeForSingleProperty(INamedTypeSymbol classSymbol, SyntaxNode syntax, ISymbol symbol, string kind, bool isField)
		{
			var ns = classSymbol.ContainingNamespace.ToDisplayString();
			var className = classSymbol.Name;

			var type = symbol switch
			{
				IFieldSymbol f => f.Type.ToDisplayString(),
				IPropertySymbol p => p.Type.ToDisplayString(),
				_ => "object"
			};

			var fieldName = symbol.Name;
			var propName = isField
				? char.ToUpper(fieldName.TrimStart('_')[0]) + fieldName.TrimStart('_').Substring(1)
				: fieldName;

			var initExpr = kind switch
			{
				"model" => $"new {type}()",
				"event" => $"DMContainer.Resolve<{type}>()",
				_ => "null!"
			};

			var sb = new StringBuilder();
			sb.AppendLine("// <auto-generated />");
			sb.AppendLine("using Dreamine.MVVM.Core;");
			sb.AppendLine($"namespace {ns}");
			sb.AppendLine("{");
			sb.AppendLine($"    public partial class {className} : ViewModelBase");
			sb.AppendLine("    {");

			if (isField)
			{
				if (kind is not "property")
					sb.AppendLine($"        private {type} {fieldName}_ = {initExpr};");

				if (kind == "property")
					sb.AppendLine($"        public {type} {propName} {{ get => {fieldName}; set => SetProperty(ref {fieldName}, value); }}");
				else
					sb.AppendLine($"        public {type} {propName} => {fieldName}_;");
			}
			else
			{
				sb.AppendLine($"        public {type} {propName} {{ get; }} = {initExpr};");
			}

			sb.AppendLine("    }");
			sb.AppendLine("}");
			return sb.ToString();
		}

		private static string Sanitize(string name)
		{
			return name.Replace('.', '_').Replace('+', '_');
		}
	}
}
