namespace PanoramicData.Mapper;

/// <summary>
/// Exception thrown when mapper configuration validation fails.
/// </summary>
public class AutoMapperConfigurationException : Exception
{
	public AutoMapperConfigurationException(string message) : base(message)
	{
	}

	public AutoMapperConfigurationException(string message, Exception innerException) : base(message, innerException)
	{
	}

	public AutoMapperConfigurationException()
	{
	}
}

/// <summary>
/// Exception thrown when a mapping cannot be found.
/// </summary>
public class AutoMapperMappingException : Exception
{
	public AutoMapperMappingException(string message) : base(message)
	{
	}

	public AutoMapperMappingException(string message, Exception innerException) : base(message, innerException)
	{
	}

	public AutoMapperMappingException()
	{
	}
}