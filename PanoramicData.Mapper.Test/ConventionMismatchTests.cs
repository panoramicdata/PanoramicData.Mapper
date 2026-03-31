using PanoramicData.Mapper.Test.Models;

namespace PanoramicData.Mapper.Test;

public class ConventionMismatchTests
{
	// --- string "" -> int (FormatException repro) ---

	[Fact]
	public void Map_EmptyStringToInt_DefaultsToZero()
	{
		var config = new MapperConfiguration(cfg =>
			cfg.AddProfile(new StringToIntProfile()));
		var mapper = config.CreateMapper();

		var source = new StringPropertySource { MonitorObjectId = "" };
		var dest = mapper.Map<MismatchedNumericDestination>(source);

		dest.MonitorObjectId.Should().Be(0);
	}

	[Fact]
	public void Map_ValidStringToInt_ConvertsCorrectly()
	{
		var config = new MapperConfiguration(cfg =>
			cfg.AddProfile(new StringToIntProfile()));
		var mapper = config.CreateMapper();

		var source = new StringPropertySource { MonitorObjectId = "42", Count = "7" };
		var dest = mapper.Map<MismatchedNumericDestination>(source);

		dest.MonitorObjectId.Should().Be(42);
		dest.Count.Should().Be(7);
	}

	// --- string "" -> int? (FormatException repro) ---

	[Fact]
	public void Map_EmptyStringToNullableInt_DefaultsToNull()
	{
		var config = new MapperConfiguration(cfg =>
			cfg.AddProfile(new StringToNullableIntProfile()));
		var mapper = config.CreateMapper();

		var source = new StringPropertySource { MonitorObjectId = "" };
		var dest = mapper.Map<MismatchedNullableIntDestination>(source);

		dest.MonitorObjectId.Should().BeNull();
	}

	// --- string "" -> enum (ArgumentException repro) ---

	[Fact]
	public void Map_EmptyStringToEnum_DefaultsToFirstValue()
	{
		var config = new MapperConfiguration(cfg =>
			cfg.AddProfile(new StringToEnumMismatchProfile()));
		var mapper = config.CreateMapper();

		var source = new StringPropertySource { GroupStatus = "" };
		var dest = mapper.Map<MismatchedEnumDestination>(source);

		dest.GroupStatus.Should().Be(ResourceGroupStatusType.Unknown);
	}

	[Fact]
	public void Map_InvalidStringToEnum_DefaultsToFirstValue()
	{
		var config = new MapperConfiguration(cfg =>
			cfg.AddProfile(new StringToEnumMismatchProfile()));
		var mapper = config.CreateMapper();

		var source = new StringPropertySource { GroupStatus = "NotAValidEnumMember" };
		var dest = mapper.Map<MismatchedEnumDestination>(source);

		dest.GroupStatus.Should().Be(ResourceGroupStatusType.Unknown);
	}

	[Fact]
	public void Map_ValidStringToEnum_ConvertsCorrectly()
	{
		var config = new MapperConfiguration(cfg =>
			cfg.AddProfile(new StringToEnumMismatchProfile()));
		var mapper = config.CreateMapper();

		var source = new StringPropertySource { GroupStatus = "Active" };
		var dest = mapper.Map<MismatchedEnumDestination>(source);

		dest.GroupStatus.Should().Be(ResourceGroupStatusType.Active);
	}

	// --- Profiles ---

	private class StringToIntProfile : Profile
	{
		public StringToIntProfile() => CreateMap<StringPropertySource, MismatchedNumericDestination>();
	}

	private class StringToNullableIntProfile : Profile
	{
		public StringToNullableIntProfile() => CreateMap<StringPropertySource, MismatchedNullableIntDestination>();
	}

	private class StringToEnumMismatchProfile : Profile
	{
		public StringToEnumMismatchProfile() => CreateMap<StringPropertySource, MismatchedEnumDestination>();
	}
}
