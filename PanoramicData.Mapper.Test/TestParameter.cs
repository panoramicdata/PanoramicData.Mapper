namespace PanoramicData.Mapper.Test;

/// <summary>
/// For test doubles that implement an interface method but have no use for some of its arguments.
/// </summary>
internal static class TestParameter
{
	internal static void NotUsed(params object?[] values)
	{
		ArgumentNullException.ThrowIfNull(values);
	}
}
