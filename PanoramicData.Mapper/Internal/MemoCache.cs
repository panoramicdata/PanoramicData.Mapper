using System.Collections.Concurrent;

namespace PanoramicData.Mapper.Internal;

/// <summary>
/// A thread-safe memoisation cache that runs the factory at most once per key, and counts
/// lookups and factory runs so callers and tests can see whether the cache is being hit.
/// </summary>
internal sealed class MemoCache<TKey, TValue> where TKey : notnull
{
	private readonly ConcurrentDictionary<TKey, Lazy<TValue>> _entries = new();
	private int _lookups;
	private int _misses;

	internal int Lookups => Volatile.Read(ref _lookups);

	internal int Misses => Volatile.Read(ref _misses);

	internal TValue GetOrAdd(TKey key, Func<TKey, TValue> factory)
	{
		Interlocked.Increment(ref _lookups);
		var entry = _entries.GetOrAdd(key, k => new Lazy<TValue>(() =>
		{
			Interlocked.Increment(ref _misses);
			return factory(k);
		}));
		return entry.Value;
	}
}
