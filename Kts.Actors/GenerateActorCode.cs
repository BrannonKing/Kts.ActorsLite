using CodeGeneration.Roslyn;
using System;
using System.Diagnostics;

namespace Kts.Actors
{
	public enum ActorType { OrderedSynchronous, OrderedAsynchronous, PeriodicAsynchronous, MostRecentAsynchronous }

	[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
	[CodeGenerationAttribute(typeof(Generators.ActorCodeGenerator))]
	[Conditional("CodeGeneration")]
	public class GenerateActorCodeAttribute: Attribute
	{
		public GenerateActorCodeAttribute(ActorType actorType)
		{
			ActorType = actorType;
		}
		public ActorType ActorType { get; }
	}
}
