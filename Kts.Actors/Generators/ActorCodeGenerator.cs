using CodeGeneration.Roslyn;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using System.Threading;

namespace Kts.Actors.Generators
{
	class ActorCodeGenerator : ICodeGenerator
	{
		private readonly ActorType _actorType;

		public ActorCodeGenerator(AttributeData data)
		{
			_actorType = (ActorType)data.ConstructorArguments[0].Value;
		}

		public Task<SyntaxList<MemberDeclarationSyntax>> GenerateAsync(MemberDeclarationSyntax applyTo, CSharpCompilation compilation, IProgress<Diagnostic> progress, CancellationToken cancellationToken)
		{
			var methodDS = applyTo as MethodDeclarationSyntax;
			if (methodDS == null)
				throw new ArgumentException("Expected to apply to a method only.", nameof(applyTo));

			//if (methodDS.ReturnType.fin) validate Task return type

			System.Diagnostics.Debugger.Break();
			var results = SyntaxFactory.List<MemberDeclarationSyntax>();

			var methodName = methodDS.Identifier.ValueText;


			//var parent = (ClassDeclarationSyntax)methodDS.Parent;
			//parent.AddMembers(SyntaxFactory.FieldDeclaration(
			//	SyntaxFactory.VariableDeclaration(
			//		SyntaxFactory.Type(SyntaxKind.VariableDeclaration, ),
			//		SyntaxFactory.SingletonSeparatedList(
			//			SyntaxFactory.VariableDeclarator("_" + methodName + "Actor")
			//		)
			//	)
			//));

			// rename the method with the attribute
			// add a new private property with a getter to our actor that calls the renamed method
			// add a new method with same name and footprint that calls our property

			// for his block 

			var arguments = SeparatedList<ArgumentSyntax>();
			foreach (var parameter in methodDS.ParameterList.ChildNodes().OfType<IdentifierNameSyntax>()) // not sure that's right
				arguments.Add(Argument(parameter));

			var propertyName = methodDS.Identifier.ValueText + "_Actor";
			var newMethod = MethodDeclaration(methodDS.ReturnType, methodDS.Identifier)
				.WithModifiers(methodDS.Modifiers)
				.WithParameterList(methodDS.ParameterList)
				.WithBody(
					Block(
						SingletonList<StatementSyntax>(
							ReturnStatement(
								InvocationExpression(
									MemberAccessExpression(
										SyntaxKind.SimpleMemberAccessExpression,
										IdentifierName(propertyName),
										IdentifierName("Push")))
								.WithArgumentList(ArgumentList(
										SingletonSeparatedList(Argument(TupleExpression(arguments)))))))));


            var newProperty = PropertyDeclaration(
				GenericName(
					Identifier("OrderedAsyncActor"))
				.WithTypeArgumentList(
					TypeArgumentList(
						SeparatedList<TypeSyntax>(
							new SyntaxNodeOrToken[]{
								TupleType(
									SeparatedList<TupleElementSyntax>(
										new SyntaxNodeOrToken[]{
											TupleElement(
												PredefinedType(
													Token(SyntaxKind.IntKeyword))),
											Token(SyntaxKind.CommaToken),
											TupleElement(
												PredefinedType(
													Token(SyntaxKind.IntKeyword)))})),
								Token(SyntaxKind.CommaToken),
								PredefinedType(
									Token(SyntaxKind.IntKeyword))}))),
				Identifier("AddHandler"))
			.WithModifiers(
				TokenList(
					Token(SyntaxKind.PrivateKeyword)))
			.WithAccessorList(
				AccessorList(
					SingletonList<AccessorDeclarationSyntax>(
						AccessorDeclaration(
							SyntaxKind.GetAccessorDeclaration)
						.WithExpressionBody(
							ArrowExpressionClause(
								ObjectCreationExpression(
									GenericName(
										Identifier("OrderedAsyncActor"))
									.WithTypeArgumentList(
										TypeArgumentList(
											SeparatedList<TypeSyntax>(
												new SyntaxNodeOrToken[]{
													TupleType(
														SeparatedList<TupleElementSyntax>(
															new SyntaxNodeOrToken[]{
																TupleElement(
																	PredefinedType(
																		Token(SyntaxKind.IntKeyword)))
																.WithName(
																	IdentifierName("a")),
																Token(SyntaxKind.CommaToken),
																TupleElement(
																	PredefinedType(
																		Token(SyntaxKind.IntKeyword)))
																.WithName(
																	IdentifierName("b"))})),
													Token(SyntaxKind.CommaToken),
													PredefinedType(
														Token(SyntaxKind.IntKeyword))}))))
								.WithArgumentList(
									ArgumentList(
										SingletonSeparatedList<ArgumentSyntax>(
											Argument(
												ParenthesizedLambdaExpression(
													MemberAccessExpression(
														SyntaxKind.SimpleMemberAccessExpression,
														InvocationExpression(
															IdentifierName("AddMethod"))
														.WithArgumentList(
															ArgumentList(
																SeparatedList<ArgumentSyntax>(
																	new SyntaxNodeOrToken[]{
																		Argument(
																			MemberAccessExpression(
																				SyntaxKind.SimpleMemberAccessExpression,
																				IdentifierName("t"),
																				IdentifierName("a"))),
																		Token(SyntaxKind.CommaToken),
																		Argument(
																			MemberAccessExpression(
																				SyntaxKind.SimpleMemberAccessExpression,
																				IdentifierName("t"),
																				IdentifierName("b")))}))),
														IdentifierName("Result")))
												.WithParameterList(
													ParameterList(
														SeparatedList<ParameterSyntax>(
															new SyntaxNodeOrToken[]{
																Parameter(
																	Identifier("t")),
																Token(SyntaxKind.CommaToken),
																Parameter(
																	Identifier("r"))})))))))))
						.WithSemicolonToken(
							Token(SyntaxKind.SemicolonToken)))))}))
			
			results.Add(newMethod);
			results.Add(newProperty);
			return Task.FromResult(results);
		}
	}
}
