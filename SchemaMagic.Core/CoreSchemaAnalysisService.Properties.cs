using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text.RegularExpressions;

namespace SchemaMagic.Core;

public static partial class CoreSchemaAnalysisService
{
	[GeneratedRegex(@"<summary>\s*(.+?)\s*</summary>", RegexOptions.Singleline)]
	private static partial Regex SummaryRegex();

	[GeneratedRegex(@"\s+")]
	private static partial Regex WhitespaceRegex();

	private static List<PropertyInfo> ExtractProperties(
		ClassDeclarationSyntax entityClass,
		string entityName,
		Dictionary<string, List<string>> foreignKeyRelationships)
	{
		var entityForeignKeys = foreignKeyRelationships.GetValueOrDefault(entityName, []);

		// Any property with a getter (auto, expression-bodied, explicit, init-only, required)
		var propertyDeclarations = entityClass.Members
			.OfType<PropertyDeclarationSyntax>()
			.Where(p =>
				p.AccessorList?.Accessors.Any(a =>
					a.Kind() is SyntaxKind.GetAccessorDeclaration or SyntaxKind.InitAccessorDeclaration) == true
				|| p.ExpressionBody is not null);

		var properties = propertyDeclarations
			.Select(property => CreatePropertyInfo(property, entityName, entityForeignKeys))
			.ToList();

		Console.WriteLine($"✅ Extracted {properties.Count} properties for {entityName}");
		return properties;
	}

	private static PropertyInfo CreatePropertyInfo(PropertyDeclarationSyntax property, string entityName, List<string> entityForeignKeys)
	{
		var propertyName = property.Identifier.Text;
		// The nullable marker is kept: the generated JavaScript reads it
		var propertyType = property.Type.ToString().Trim();

		return new PropertyInfo
		{
			Name = propertyName,
			Type = propertyType,
			IsKey = IsKeyProperty(propertyName, entityName),
			IsForeignKey = entityForeignKeys.Contains(propertyName) || IsHeuristicForeignKey(propertyName, propertyType),
			Comment = ExtractComment(property),
			MaxLength = ExtractPropertyMaxLength(property)
		};
	}

	// A [Comment("...")] attribute wins over the XML documentation summary
	private static string? ExtractComment(MemberDeclarationSyntax member) =>
		ExtractCommentAttribute(member) ?? ExtractSummaryComment(member);

	private static string? ExtractCommentAttribute(MemberDeclarationSyntax member)
	{
		var commentAttribute = member.AttributeLists
			.SelectMany(al => al.Attributes)
			.FirstOrDefault(a => a.Name.ToString() == "Comment" || a.Name.ToString().EndsWith(".Comment"));

		if (commentAttribute?.ArgumentList?.Arguments.Count > 0
			&& commentAttribute.ArgumentList.Arguments[0].Expression is LiteralExpressionSyntax literal)
		{
			var commentText = literal.Token.ValueText;
			return string.IsNullOrWhiteSpace(commentText) ? null : commentText;
		}

		return null;
	}

	private static string? ExtractSummaryComment(MemberDeclarationSyntax member)
	{
		var documentationComment = member.GetLeadingTrivia()
			.FirstOrDefault(t => t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)
				|| t.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia));

		if (documentationComment == default)
		{
			return null;
		}

		var summaryMatch = SummaryRegex().Match(documentationComment.ToString());
		if (!summaryMatch.Success)
		{
			return null;
		}

		var summaryText = WhitespaceRegex().Replace(summaryMatch.Groups[1].Value.Replace("///", "").Trim(), " ");
		return string.IsNullOrWhiteSpace(summaryText) ? null : summaryText;
	}

	// [MaxLength(n)] and [StringLength(n)] both carry the maximum length as the first argument
	private static int? ExtractPropertyMaxLength(PropertyDeclarationSyntax property)
	{
		var lengthAttribute = property.AttributeLists
			.SelectMany(al => al.Attributes)
			.FirstOrDefault(a => IsLengthAttributeName(a.Name.ToString()));

		var firstArgument = lengthAttribute?.ArgumentList?.Arguments.FirstOrDefault();
		if (firstArgument?.Expression is LiteralExpressionSyntax literal
			&& literal.IsKind(SyntaxKind.NumericLiteralExpression)
			&& int.TryParse(literal.Token.ValueText, out var length)
			&& length > 0)
		{
			return length;
		}

		return null;
	}

	private static bool IsLengthAttributeName(string name) =>
		name is "MaxLength" or "StringLength"
		|| name.EndsWith(".MaxLength", StringComparison.Ordinal)
		|| name.EndsWith(".StringLength", StringComparison.Ordinal);

	// Heuristic used when no EF snapshot is available
	private static bool IsHeuristicForeignKey(string propertyName, string propertyType) =>
		propertyName.EndsWith("Id", StringComparison.OrdinalIgnoreCase)
		&& !propertyName.Equals("Id", StringComparison.OrdinalIgnoreCase)
		&& (propertyType.Contains("int") || propertyType.Contains("Guid"));

	private static bool IsKeyProperty(string propertyName, string entityName) =>
		propertyName.Equals("Id", StringComparison.OrdinalIgnoreCase)
		|| propertyName.Equals($"{entityName}Id", StringComparison.OrdinalIgnoreCase);
}
