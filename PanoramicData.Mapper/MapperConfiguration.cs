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
	private readonly List<(Type SourceType, Type DestType)> _openGenericMaps = [];

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
			_openGenericMaps.AddRange(profile.OpenGenericMaps);
		}

		// IncludeBase: propagate configuration from base TypeMaps to derived TypeMaps
		foreach (var typeMap in _typeMaps)
		{
			if (typeMap.IncludedBaseTypes is not null)
			{
				var (baseSource, baseDest) = typeMap.IncludedBaseTypes.Value;
				var baseMap = _typeMaps.FirstOrDefault(m => m.SourceType == baseSource && m.DestinationType == baseDest);
				baseMap?.CopyConfigurationTo(typeMap);
			}
		}

		// Include: propagate configuration from parent to declared derived maps
		foreach (var typeMap in _typeMaps)
		{
			foreach (var (derivedSource, derivedDest) in typeMap.IncludedDerivedTypes)
			{
				var derivedMap = _typeMaps.FirstOrDefault(m => m.SourceType == derivedSource && m.DestinationType == derivedDest);
				if (derivedMap is not null)
				{
					typeMap.CopyConfigurationTo(derivedMap);
				}
			}
		}

		// Wire up resolver for nested/collection mapping support
		foreach (var typeMap in _typeMaps)
		{
			typeMap.TypeMapResolver = FindTypeMap;
		}
	}

	/// <summary>
	/// Creates a mapper from this configuration.
	/// </summary>
	public IMapper CreateMapper() => new Mapper(this);

	/// <inheritdoc />
	public TypeMap? FindTypeMap(Type sourceType, Type destinationType)
	{
		// Exact match first
		var exact = _typeMaps.FirstOrDefault(m => m.SourceType == sourceType && m.DestinationType == destinationType);
		if (exact is not null)
		{
			return exact;
		}

		// Inheritance: check for IncludeAllDerived base maps
		foreach (var typeMap in _typeMaps)
		{
			if (typeMap.IncludeAllDerivedFlag &&
				typeMap.SourceType.IsAssignableFrom(sourceType) &&
				typeMap.DestinationType.IsAssignableFrom(destinationType))
			{
				// Create a derived TypeMap on-the-fly, cache it
				var derived = new TypeMap(sourceType, destinationType);
				typeMap.CopyConfigurationTo(derived);
				derived.TypeMapResolver = FindTypeMap;
				_typeMaps.Add(derived);
				return derived;
			}
		}

		// Inheritance: check Include-declared derived pairs
		foreach (var typeMap in _typeMaps)
		{
			foreach (var (derivedSource, derivedDest) in typeMap.IncludedDerivedTypes)
			{
				if (derivedSource == sourceType && derivedDest == destinationType)
				{
					var derived = new TypeMap(sourceType, destinationType);
					typeMap.CopyConfigurationTo(derived);
					derived.TypeMapResolver = FindTypeMap;
					_typeMaps.Add(derived);
					return derived;
				}
			}
		}

		// Open generic resolution
		if (sourceType.IsGenericType && destinationType.IsGenericType)
		{
			var srcGenDef = sourceType.GetGenericTypeDefinition();
			var destGenDef = destinationType.GetGenericTypeDefinition();
			var openMatch = _openGenericMaps.FirstOrDefault(m => m.SourceType == srcGenDef && m.DestType == destGenDef);
			if (openMatch != default)
			{
				var closed = new TypeMap(sourceType, destinationType);
				closed.TypeMapResolver = FindTypeMap;
				_typeMaps.Add(closed);
				return closed;
			}
		}

		return null;
	}

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
			// Skip maps that use ConvertUsing - they bypass member mapping entirely
			if (typeMap.ConverterFunc is not null || typeMap.ConverterType is not null)
			{
				continue;
			}

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