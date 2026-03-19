using PanoramicData.Mapper.Test.Models;

namespace PanoramicData.Mapper.Test;

public class EnumMappingTests
{
	[Fact]
	public void Map_IntToEnum_SameNameProperty_MapsCorrectly()
	{
		var config = new MapperConfiguration(cfg =>
			cfg.AddProfile(new IntToEnumProfile()));
		var mapper = config.CreateMapper();

		var source = new IntToEnumSource { Status = 3 };
		var dest = mapper.Map<EnumDestination>(source);

		dest.Status.Should().Be(MyStatus.Deleted);
	}

	[Fact]
	public void Map_EnumToInt_SameNameProperty_MapsCorrectly()
	{
		var config = new MapperConfiguration(cfg =>
			cfg.AddProfile(new EnumToIntProfile()));
		var mapper = config.CreateMapper();

		var source = new EnumToIntSource { Status = MyStatus.Active };
		var dest = mapper.Map<IntDestination>(source);

		dest.Status.Should().Be(1);
	}

	[Fact]
	public void Map_IntToEnum_InvalidValue_StillCasts()
	{
		var config = new MapperConfiguration(cfg =>
			cfg.AddProfile(new IntToEnumProfile()));
		var mapper = config.CreateMapper();

		var source = new IntToEnumSource { Status = 999 };
		var dest = mapper.Map<EnumDestination>(source);

		dest.Status.Should().Be((MyStatus)999);
	}

	[Fact]
	public void Map_IntToEnum_WithIgnore_DoesNotMap()
	{
		var config = new MapperConfiguration(cfg =>
			cfg.AddProfile(new IntToEnumIgnoreProfile()));
		var mapper = config.CreateMapper();

		var source = new IntToEnumSource { Status = 3 };
		var dest = mapper.Map<EnumDestination>(source);

		dest.Status.Should().Be(MyStatus.Unknown);
	}

	[Fact]
	public void Map_IntToEnum_WithExplicitMapFrom_UsesExplicitMapping()
	{
		var config = new MapperConfiguration(cfg =>
			cfg.AddProfile(new IntToEnumExplicitProfile()));
		var mapper = config.CreateMapper();

		var source = new IntToEnumSource { Status = 3 };
		var dest = mapper.Map<EnumDestination>(source);

		dest.Status.Should().Be(MyStatus.Inactive);
	}

	[Fact]
	public void Map_NullableIntToNullableEnum_NullValue_MapsNull()
	{
		var config = new MapperConfiguration(cfg =>
			cfg.AddProfile(new NullableIntToNullableEnumProfile()));
		var mapper = config.CreateMapper();

		var source = new NullableIntToNullableEnumSource { Status = null };
		var dest = mapper.Map<NullableEnumDestination>(source);

		dest.Status.Should().BeNull();
	}

	[Fact]
	public void Map_NullableIntToNullableEnum_WithValue_MapsCorrectly()
	{
		var config = new MapperConfiguration(cfg =>
			cfg.AddProfile(new NullableIntToNullableEnumProfile()));
		var mapper = config.CreateMapper();

		var source = new NullableIntToNullableEnumSource { Status = 2 };
		var dest = mapper.Map<NullableEnumDestination>(source);

		dest.Status.Should().Be(MyStatus.Inactive);
	}

	[Fact]
	public void Map_IntToNullableEnum_MapsCorrectly()
	{
		var config = new MapperConfiguration(cfg =>
			cfg.AddProfile(new IntToNullableEnumProfile()));
		var mapper = config.CreateMapper();

		var source = new IntToNullableEnumSource { Status = 1 };
		var dest = mapper.Map<NullableEnumDestination>(source);

		dest.Status.Should().Be(MyStatus.Active);
	}

	[Fact]
	public void Map_NullableIntToEnum_WithValue_MapsCorrectly()
	{
		var config = new MapperConfiguration(cfg =>
			cfg.AddProfile(new NullableIntToEnumProfile()));
		var mapper = config.CreateMapper();

		var source = new NullableIntToEnumSource { Status = 3 };
		var dest = mapper.Map<EnumDestination>(source);

		dest.Status.Should().Be(MyStatus.Deleted);
	}

	[Fact]
	public void Map_NullableIntToEnum_NullValue_DefaultsToZero()
	{
		var config = new MapperConfiguration(cfg =>
			cfg.AddProfile(new NullableIntToEnumProfile()));
		var mapper = config.CreateMapper();

		var source = new NullableIntToEnumSource { Status = null };
		var dest = mapper.Map<EnumDestination>(source);

		dest.Status.Should().Be(MyStatus.Unknown);
	}

	[Fact]
	public void Map_EnumToEnum_SameType_MapsCorrectly()
	{
		var config = new MapperConfiguration(cfg =>
			cfg.AddProfile(new EnumToEnumProfile()));
		var mapper = config.CreateMapper();

		var source = new EnumToEnumSource { Status = MyStatus.Deleted };
		var dest = mapper.Map<EnumToEnumDestination>(source);

		dest.Status.Should().Be(MyStatus.Deleted);
	}

	[Fact]
	public void Map_IntToEnum_AfterMapStillRuns()
	{
		var config = new MapperConfiguration(cfg =>
			cfg.AddProfile(new IntToEnumAfterMapProfile()));
		var mapper = config.CreateMapper();

		var source = new IntToEnumSource { Status = 3 };
		var dest = mapper.Map<EnumDestination>(source);

		dest.Status.Should().Be(MyStatus.Inactive);
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
				.AfterMap((s, d) => d.Status = MyStatus.Inactive);
		}
	}
}
