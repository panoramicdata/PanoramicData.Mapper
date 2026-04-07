namespace PanoramicData.Mapper;

/// <summary>
/// Exception thrown when mapper configuration validation fails.
/// </summary>
public class AutoMapperConfigurationException : Exception
{
	/// <summary>
	/// Initializes a new instance of the <see cref="AutoMapperConfigurationException"/> class with a specified error message.
	/// </summary>
	/// <param name="message">The error message that explains the reason for the exception.</param>
	public AutoMapperConfigurationException(string message) : base(message)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="AutoMapperConfigurationException"/> class with a specified error message and a reference to the inner exception.
	/// </summary>
	/// <param name="message">The error message that explains the reason for the exception.</param>
	/// <param name="innerException">The exception that is the cause of the current exception.</param>
	public AutoMapperConfigurationException(string message, Exception innerException) : base(message, innerException)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="AutoMapperConfigurationException"/> class.
	/// </summary>
	public AutoMapperConfigurationException()
	{
	}
}

/// <summary>
/// Exception thrown when a mapping cannot be found.
/// </summary>
public class AutoMapperMappingException : Exception
{
	/// <summary>
	/// Initializes a new instance of the <see cref="AutoMapperMappingException"/> class with a specified error message.
	/// </summary>
	/// <param name="message">The error message that explains the reason for the exception.</param>
	public AutoMapperMappingException(string message) : base(message)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="AutoMapperMappingException"/> class with a specified error message and a reference to the inner exception.
	/// </summary>
	/// <param name="message">The error message that explains the reason for the exception.</param>
	/// <param name="innerException">The exception that is the cause of the current exception.</param>
	public AutoMapperMappingException(string message, Exception innerException) : base(message, innerException)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="AutoMapperMappingException"/> class.
	/// </summary>
	public AutoMapperMappingException()
	{
	}
}