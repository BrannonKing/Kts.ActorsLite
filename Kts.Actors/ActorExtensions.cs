using System.Threading.Tasks;

namespace Kts.Actors
{
	public static class ActorExtensions
	{
		public static async Task Push<T,R>(this IActor<T,R> actor, T value, IActor<R> next)
		{
			var result = await actor.Push(value);
			if (next != null)
				await next.Push(result);
		}

		public static async Task<R2> Push<T, R, R2>(this IActor<T, R> actor, T value, IActor<R, R2> next)
		{
			var result = await actor.Push(value);
			return await next.Push(result);
		}
	}
}
