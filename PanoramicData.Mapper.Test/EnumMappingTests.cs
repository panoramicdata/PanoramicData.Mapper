using PanoramicData.Mapper.Test.Models;

namespace PanoramicData.Mapper.Test;

public class EnumMappingTests
{
	[Fact]
	public void Map_IntToEnum_SameNameProperty_MapsCorrectly()
	{
		var dest = Map<IntToEnumProfile, IntToEnumSource, EnumDestination>(source => source.Status = 3);

		dest.Status.Should().Be(MyStatus.Deleted);
	}

	[Fact]
	public void Map_EnumToInt_SameNameProperty_MapsCorrectly()
	{
		var dest = Map<EnumToIntProfile, EnumToIntSource, IntDestination>(source => source.Status = MyStatus.Active);

		dest.Status.Should().Be(1);
	}

	[Fact]
	public void Map_IntToEnum_InvalidValue_StillCasts()
	{
		var dest = Map<IntToEnumProfile, IntToEnumSource, EnumDestination>(source => source.Status = 999);

		dest.Status.Should().Be((MyStatus)999);
	}

	[Fact]
	public void Map_IntToEnum_WithIgnore_DoesNotMap()
	{
		var dest = Map<IntToEnumIgnoreProfile, IntToEnumSource, EnumDestination>(source => source.Status = 3);

		dest.Status.Should().Be(MyStatus.Unknown);
	}

	[Fact]
	public void Map_IntToEnum_WithExplicitMapFrom_UsesExplicitMapping()
	{
		var dest = Map<IntToEnumExplicitProfile, IntToEnumSource, EnumDestination>(source => source.Status = 3);

		dest.Status.Should().Be(MyStatus.Inactive);
	}

	[Fact]
	public void Map_NullableIntToNullableEnum_NullValue_MapsNull()
	{
		var dest = Map<NullableIntToNullableEnumProfile, NullableIntToNullableEnumSource, NullableEnumDestination>(source => source.Status = null);

		dest.Status.Should().BeNull();
	}

	[Fact]
	public void Map_NullableIntToNullableEnum_WithValue_MapsCorrectly()
	{
		var dest = Map<NullableIntToNullableEnumProfile, NullableIntToNullableEnumSource, NullableEnumDestination>(source => source.Status = 2);

		dest.Status.Should().Be(MyStatus.Inactive);
	}

	[Fact]
	public void Map_IntToNullableEnum_MapsCorrectly()
	{
		var dest = Map<IntToNullableEnumProfile, IntToNullableEnumSource, NullableEnumDestination>(source => source.Status = 1);

		dest.Status.Should().Be(MyStatus.Active);
	}

	[Fact]
	public void Map_NullableIntToEnum_WithValue_MapsCorrectly()
	{
		var dest = Map<NullableIntToEnumProfile, NullableIntToEnumSource, EnumDestination>(source => source.Status = 3);

		dest.Status.Should().Be(MyStatus.Deleted);
	}

	[Fact]
	public void Map_NullableIntToEnum_NullValue_DefaultsToZero()
	{
		var dest = Map<NullableIntToEnumProfile, NullableIntToEnumSource, EnumDestination>(source => source.Status = null);

		dest.Status.Should().Be(MyStatus.Unknown);
	}

	[Fact]
	public void Map_EnumToEnum_SameType_MapsCorrectly()
	{
		var dest = Map<EnumToEnumProfile, EnumToEnumSource, EnumToEnumDestination>(source => source.Status = MyStatus.Deleted);

		dest.Status.Should().Be(MyStatus.Deleted);
	}

	[Fact]
	public void Map_IntToEnum_AfterMapStillRuns()
	{
		var dest = Map<IntToEnumAfterMapProfile, IntToEnumSource, EnumDestination>(source => source.Status = 3);

		dest.Status.Should().Be(MyStatus.Inactive);
	}

	private static TDest Map<TProfile, TSource, TDest>(Action<TSource> initSource)
		where TProfile : Profile, new()
		where TSource : class, new()
		where TDest : class
	{
		var config = new MapperConfiguration(cfg =>
			cfg.AddProfile(new TProfile()));
		var mapper = config.CreateMapper();

		var source = new TSource();
		initSource(source);

		return mapper.Map<TDest>(source);
	}

	private class IntToEnumProfile : Profile
	{
		public IntToEnumProfile() => CreateMap<IntToEnumSource, EnumDestination>();
	}

	private class EnumToIntProfile : Profile
	{
		public EnumToIntProfile() => CreateMap<EnumToIntSource, IntDestination>();
	}

	private class IntToEnumIgnoreProfile : Profile
	{
		public IntToEnumIgnoreProfile()
		{
			CreateMap<IntToEnumSource, EnumDestination>()
				.ForMember(d => d.Status, opt => opt.Ignore());
		}
	}

	private class IntToEnumExplicitProfile : Profile
	{
		public IntToEnumExplicitProfile()
		{
			CreateMap<IntToEnumSource, EnumDestination>()
				.ForMember(d => d.Status, opt => opt.MapFrom(s => MyStatus.Inactive));
		}
	}

	private class NullableIntToNullableEnumProfile : Profile
	{
		public NullableIntToNullableEnumProfile() =>
			CreateMap<NullableIntToNullableEnumSource, NullableEnumDestination>();
	}

	private class IntToNullableEnumProfile : Profile
	{
		public IntToNullableEnumProfile() =>
			CreateMap<IntToNullableEnumSource, NullableEnumDestination>();
	}

	private class NullableIntToEnumProfile : Profile
	{
		public NullableIntToEnumProfile() =>
			CreateMap<NullableIntToEnumSource, EnumDestination>();
	}

	private class EnumToEnumProfile : Profile
	{
		public EnumToEnumProfile() => CreateMap<EnumToEnumSource, EnumToEnumDestination>();
	}

	private class IntToEnumAfterMapProfile : Profile
	{
		public IntToEnumAfterMapProfile()
		{
			CreateMap<IntToEnumSource, EnumDestination>()
				.AfterMap((_, d) => d.Status = MyStatus.Inactive);
		}
	}
}
