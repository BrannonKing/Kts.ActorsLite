using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Kts.ActorsLite
{
	public interface IActor<T>
	{
		Task Push(T value);
		Task PushMany(IReadOnlyList<T> values);
		Task Push(T value, CancellationToken token);
		Task PushMany(IReadOnlyList<T> values, CancellationToken token);
	}

	public interface IActor<T, R>: IActor<T>
	{
		new Task<R> Push(T value);
		new Task<R[]> PushMany(IReadOnlyList<T> values);
		new Task<R> Push(T value, CancellationToken token);
		new Task<R[]> PushMany(IReadOnlyList<T> values, CancellationToken token);
	}
}
