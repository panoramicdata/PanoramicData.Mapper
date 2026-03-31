using PanoramicData.Mapper.Test.Models;

namespace PanoramicData.Mapper.Test;

public class ImplicitConversionTests
{
	// --- Numeric widening/narrowing ---

	[Fact]
	public void Map_IntToLong_MapsCorrectly()
	{
		var config = new MapperConfiguration(cfg =>
			cfg.AddProfile(new IntToLongProfile()));
		var mapper = config.CreateMapper();

		var source = new IntSource { Value = 42 };
		var dest = mapper.Map<LongDestination>(source);

		dest.Value.Should().Be(42L);
	}

	[Fact]
	public void Map_LongToInt_MapsCorrectly()
	{
		var config = new MapperConfiguration(cfg =>
			cfg.AddProfile(new LongToIntProfile()));
		var mapper = config.CreateMapper();

		var source = new LongSource { Value = 99 };
		var dest = mapper.Map<IntFromLongDestination>(source);

		dest.Value.Should().Be(99);
	}

	[Fact]
	public void Map_IntToDouble_MapsCorrectly()
	{
		var config = new MapperConfiguration(cfg =>
			cfg.AddProfile(new IntToDoubleProfile()));
		var mapper = config.CreateMapper();

		var source = new IntSource { Value = 7 };
		var dest = mapper.Map<IntToDoubleDestination>(source);

		dest.Value.Should().Be(7.0);
	}

	[Fact]
	public void Map_DoubleToInt_MapsCorrectly()
	{
		var config = new MapperConfiguration(cfg =>
			cfg.AddProfile(new DoubleToIntProfile()));
		var mapper = config.CreateMapper();

		var source = new DoubleSource { Value = 3.0 };
		var dest = mapper.Map<IntFromDoubleDestination>(source);

		dest.Value.Should().Be(3);
	}

	[Fact]
	public void Map_DecimalToDouble_MapsCorrectly()
	{
		var config = new MapperConfiguration(cfg =>
			cfg.AddProfile(new DecimalToDoubleProfile()));
		var mapper = config.CreateMapper();

		var source = new DecimalSource { Amount = 123.45m };
		var dest = mapper.Map<DoubleFromDecimalDestination>(source);

		dest.Amount.Should().BeApproximately(123.45, 0.001);
	}

	// --- Primitive to string ---

	[Fact]
	public void Map_IntToString_MapsCorrectly()
	{
		var config = new MapperConfiguration(cfg =>
			cfg.AddProfile(new IntToStringProfile()));
		var mapper = config.CreateMapper();

		var source = new IntToStringSource { Value = 42 };
		var dest = mapper.Map<StringFromIntDestination>(source);

		dest.Value.Should().Be("42");
	}

	[Fact]
	public void Map_LongToString_MapsCorrectly()
	{
		var config = new MapperConfiguration(cfg =>
			cfg.AddProfile(new LongToStringProfile()));
		var mapper = config.CreateMapper();

		var source = new LongToStringSource { Value = 9999999999L };
		var dest = mapper.Map<StringFromLongDestination>(source);

		dest.Value.Should().Be("9999999999");
	}

	[Fact]
	public void Map_BoolToString_MapsCorrectly()
	{
		var config = new MapperConfiguration(cfg =>
			cfg.AddProfile(new BoolToStringProfile()));
		var mapper = config.CreateMapper();

		var source = new BoolToStringSource { Active = true };
		var dest = mapper.Map<StringFromBoolDestination>(source);

		dest.Active.Should().Be("True");
	}

	// --- Enum to string ---

	[Fact]
	public void Map_EnumToString_MapsCorrectly()
	{
		var config = new MapperConfiguration(cfg =>
			cfg.AddProfile(new EnumToStringProfile()));
		var mapper = config.CreateMapper();

		var source = new EnumToStringSource { Status = MyStatus.Active };
		var dest = mapper.Map<StringFromEnumDestination>(source);

		dest.Status.Should().Be("Active");
	}

	// --- String to enum ---

	[Fact]
	public void Map_StringToEnum_MapsCorrectly()
	{
		var config = new MapperConfiguration(cfg =>
			cfg.AddProfile(new StringToEnumProfile()));
		var mapper = config.CreateMapper();

		var source = new StringToEnumSource { Status = "Deleted" };
		var dest = mapper.Map<EnumFromStringDestination>(source);

		dest.Status.Should().Be(MyStatus.Deleted);
	}

	// --- String to primitive ---

	[Fact]
	public void Map_StringToBool_MapsCorrectly()
	{
		var config = new MapperConfiguration(cfg =>
			cfg.AddProfile(new StringToBoolProfile()));
		var mapper = config.CreateMapper();

		var source = new StringToBoolSource { Active = "True" };
		var dest = mapper.Map<BoolFromStringDestination>(source);

		dest.Active.Should().BeTrue();
	}

	// --- Nullable to non-nullable ---

	[Fact]
	public void Map_NullableIntToInt_WithValue_MapsCorrectly()
	{
		var config = new MapperConfiguration(cfg =>
			cfg.AddProfile(new NullableIntToIntProfile()));
		var mapper = config.CreateMapper();

		var source = new NullableIntSource { Value = 42 };
		var dest = mapper.Map<IntFromNullableDestination>(source);

		dest.Value.Should().Be(42);
	}

	[Fact]
	public void Map_NullableIntToInt_NullValue_DefaultsToZero()
	{
		var config = new MapperConfiguration(cfg =>
			cfg.AddProfile(new NullableIntToIntProfile()));
		var mapper = config.CreateMapper();

		var source = new NullableIntSource { Value = null };
		var dest = mapper.Map<IntFromNullableDestination>(source);

		dest.Value.Should().Be(0);
	}

	// --- Profiles ---

	private class IntToLongProfile : Profile
	{
		public IntToLongProfile() => CreateMap<IntSource, LongDestination>();
	}

	private class LongToIntProfile : Profile
	{
		public LongToIntProfile() => CreateMap<LongSource, IntFromLongDestination>();
	}

	private class IntToDoubleProfile : Profile
	{
		public IntToDoubleProfile() => CreateMap<IntSource, IntToDoubleDestination>();
	}

	private class DoubleToIntProfile : Profile
	{
		public DoubleToIntProfile() => CreateMap<DoubleSource, IntFromDoubleDestination>();
	}

	private class DecimalToDoubleProfile : Profile
	{
		public DecimalToDoubleProfile() => CreateMap<DecimalSource, DoubleFromDecimalDestination>();
	}

	private class IntToStringProfile : Profile
	{
		public IntToStringProfile() => CreateMap<IntToStringSource, StringFromIntDestination>();
	}

	private class LongToStringProfile : Profile
	{
		public LongToStringProfile() => CreateMap<LongToStringSource, StringFromLongDestination>();
	}

	private class BoolToStringProfile : Profile
	{
		public BoolToStringProfile() => CreateMap<BoolToStringSource, StringFromBoolDestination>();
	}

	private class EnumToStringProfile : Profile
	{
		public EnumToStringProfile() => CreateMap<EnumToStringSource, StringFromEnumDestination>();
	}

	private class StringToEnumProfile : Profile
	{
		public StringToEnumProfile() => CreateMap<StringToEnumSource, EnumFromStringDestination>();
	}

	private class StringToBoolProfile : Profile
	{
		public StringToBoolProfile() => CreateMap<StringToBoolSource, BoolFromStringDestination>();
	}

	private class NullableIntToIntProfile : Profile
	{
		public NullableIntToIntProfile() => CreateMap<NullableIntSource, IntFromNullableDestination>();
	}
}
