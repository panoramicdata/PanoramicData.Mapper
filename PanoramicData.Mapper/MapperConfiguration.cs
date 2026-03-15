using PanoramicData.Mapper.Internal;
using System.Reflection;
using System.Text;

namespace PanoramicData.Mapper;

/// <summary>
/// Configuration for the mapper. Create an instance and pass configuration to the constructor.
/// </summary>
public sealed class MapperConfiguration : IConfigurationProvider
{
	private readonly List<TypeMap> _typeMaps = [];

	/// <summary>
	/// Creates a new mapper configuration using a configuration action.
	/// </summary>
	public MapperConfiguration(Action<IMapperConfigurationExpression> configure)
	{
		var expression = new MapperConfigurationExpression();
		configure(expression);

		foreach (var profile in expression.Profiles)
		{
			_typeMaps.AddRange(profile.TypeMaps);
		}
	}

	/// <summary>
	/// Creates a mapper from this configuration.
	/// </summary>
	public IMapper CreateMapper() => new Mapper(this);

	/// <inheritdoc />
	public TypeMap? FindTypeMap(Type sourceType, Type destinationType)
		=> _typeMaps.FirstOrDefault(m => m.SourceType == sourceType && m.DestinationType == destinationType);

	/// <inheritdoc />
	public IReadOnlyCollection<TypeMap> GetAllTypeMaps() => _typeMaps.AsReadOnly();

	/// <summary>
	/// Validates that all mappings are complete (no unmapped destination members).
	/// Throws if any destination properties are not mapped, ignored, or convention-matched.
	/// </summary>
	public void AssertConfigurationIsValid()
	{
		var errors = new StringBuilder();

		foreach (var typeMap in _typeMaps)
		{
			var unmapped = typeMap.GetUnmappedDestinationMembers();
			if (unmapped.Count > 0)
			{
				errors.AppendLine(
					$"Unmapped members found for {typeMap.SourceType.Name} -> {typeMap.DestinationType.Name}:");
				foreach (var member in unmapped)
				{
					errors.AppendLine($"  - {member}");
				}
			}
		}

		if (errors.Length > 0)
		{
			throw new AutoMapperConfigurationException(errors.ToString());
		}
	}

	private sealed class MapperConfigurationExpression : IMapperConfigurationExpression
	{
		internal List<Profile> Profiles { get; } = [];

		public void AddMaps(params Assembly[] assemblies)
		{
			foreach (var assembly in assemblies)
			{
				var profileTypes = assembly.GetTypes()
					.Where(t => typeof(Profile).IsAssignableFrom(t) && !t.IsAbstract && t.GetConstructor(Type.EmptyTypes) is not null);

				foreach (var profileType in profileTypes)
				{
					var profile = (Profile)(Activator.CreateInstance(profileType)
						?? throw new InvalidOperationException($"Cannot create instance of profile {profileType.FullName}"));
					Profiles.Add(profile);
				}
			}
		}

		public void AddProfile<TProfile>() where TProfile : Profile, new()
		{
			Profiles.Add(new TProfile());
		}

		public void AddProfile(Profile profile)
		{
			Profiles.Add(profile);
		}
	}
}