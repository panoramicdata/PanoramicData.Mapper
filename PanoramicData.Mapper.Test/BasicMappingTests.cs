using PanoramicData.Mapper.Test.Models;
using PanoramicData.Mapper.Test.Profiles;

namespace PanoramicData.Mapper.Test;

public class BasicMappingTests
{
	[Fact]
	public void MapperConfiguration_NullAction_Throws()
	{
		var act = () => new MapperConfiguration(null!);

		act.Should().Throw<Exception>();
	}

	[Fact]
	public void Map_SimpleConvention_MapsAllProperties()
	{
		var config = new MapperConfiguration(cfg => cfg.AddProfile<SimpleProfile>());
		var mapper = config.CreateMapper();

		var source = new SimpleSource
		{
			Id = 42,
			Name = "Test",
			Description = "A description",
			CreatedDate = new DateTime(2026, 1, 15),
			Amount = 99.95m
		};

		var dest = mapper.Map<SimpleDestination>(source);

		dest.Id.Should().Be(42);
		dest.Name.Should().Be("Test");
		dest.Description.Should().Be("A description");
		dest.CreatedDate.Should().Be(new DateTime(2026, 1, 15));
		dest.Amount.Should().Be(99.95m);
	}

	[Fact]
	public void Map_WithGenericTypes_MapsCorrectly()
	{
		var config = new MapperConfiguration(cfg => cfg.AddProfile<SimpleProfile>());
		var mapper = config.CreateMapper();

		var source = new SimpleSource { Id = 1, Name = "Hello" };
		var dest = mapper.Map<SimpleSource, SimpleDestination>(source);

		dest.Id.Should().Be(1);
		dest.Name.Should().Be("Hello");
	}

	[Fact]
	public void Map_MissingTypeMap_ThrowsAutoMapperMappingException()
	{
		var config = new MapperConfiguration(cfg => { });
		var mapper = config.CreateMapper();

		var source = new SimpleSource { Id = 1 };
		var act = () => mapper.Map<SimpleDestination>(source);

		act.Should().Throw<AutoMapperMappingException>();
	}

	[Fact]
	public void MapGeneric_MissingTypeMap_ThrowsAutoMapperMappingException()
	{
		var config = new MapperConfiguration(cfg => { });
		var mapper = config.CreateMapper();

		var act = () => mapper.Map<SimpleSource, SimpleDestination>(new SimpleSource());

		act.Should().Throw<AutoMapperMappingException>();
	}

	[Fact]
	public void MapRuntimeTypes_MissingTypeMap_ThrowsAutoMapperMappingException()
	{
		var config = new MapperConfiguration(cfg => { });
		var mapper = config.CreateMapper();

		var act = () => mapper.Map(new SimpleSource(), typeof(SimpleSource), typeof(SimpleDestination));

		act.Should().Throw<AutoMapperMappingException>();
	}

	[Fact]
	public void MapRuntimeTypesToExisting_MissingTypeMap_ThrowsAutoMapperMappingException()
	{
		var config = new MapperConfiguration(cfg => { });
		var mapper = config.CreateMapper();

		var act = () => mapper.Map(new SimpleSource(), new SimpleDestination(), typeof(SimpleSource), typeof(SimpleDestination));

		act.Should().Throw<AutoMapperMappingException>();
	}

	[Fact]
	public void MapWithOptions_MissingTypeMap_ThrowsAutoMapperMappingException()
	{
		var config = new MapperConfiguration(cfg => { });
		var mapper = config.CreateMapper();

		var act = () => mapper.Map<SimpleSource, SimpleDestination>(new SimpleSource(), opts => { });

		act.Should().Throw<AutoMapperMappingException>();
	}

	[Fact]
	public void Map_NullSource_ThrowsArgumentNullException()
	{
		var config = new MapperConfiguration(cfg => cfg.AddProfile<SimpleProfile>());
		var mapper = config.CreateMapper();

		var act = () => mapper.Map<SimpleDestination>((object)null!);

		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void MapGeneric_NullSource_ThrowsArgumentNullException()
	{
		var config = new MapperConfiguration(cfg => cfg.AddProfile<SimpleProfile>());
		var mapper = config.CreateMapper();

		var act = () => mapper.Map<SimpleSource, SimpleDestination>(null!);

		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void MapToExisting_NullSource_ThrowsArgumentNullException()
	{
		var config = new MapperConfiguration(cfg => cfg.AddProfile<SimpleProfile>());
		var mapper = config.CreateMapper();

		var act = () => mapper.Map<SimpleSource, SimpleDestination>(null!, new SimpleDestination());

		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void MapToExisting_NullDestination_ThrowsArgumentNullException()
	{
		var config = new MapperConfiguration(cfg => cfg.AddProfile<SimpleProfile>());
		var mapper = config.CreateMapper();

		var act = () => mapper.Map(new SimpleSource(), (SimpleDestination)null!);

		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void MapRuntimeTypes_NullSource_ThrowsArgumentNullException()
	{
		var config = new MapperConfiguration(cfg => cfg.AddProfile<SimpleProfile>());
		var mapper = config.CreateMapper();

		var act = () => mapper.Map(null!, typeof(SimpleSource), typeof(SimpleDestination));

		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void MapRuntimeTypesToExisting_NullSource_ThrowsArgumentNullException()
	{
		var config = new MapperConfiguration(cfg => cfg.AddProfile<SimpleProfile>());
		var mapper = config.CreateMapper();

		var act = () => mapper.Map(null!, new SimpleDestination(), typeof(SimpleSource), typeof(SimpleDestination));

		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void MapRuntimeTypesToExisting_NullDestination_ThrowsArgumentNullException()
	{
		var config = new MapperConfiguration(cfg => cfg.AddProfile<SimpleProfile>());
		var mapper = config.CreateMapper();

		var act = () => mapper.Map(new SimpleSource(), null!, typeof(SimpleSource), typeof(SimpleDestination));

		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void MapWithOptions_NullSource_ThrowsArgumentNullException()
	{
		var config = new MapperConfiguration(cfg => cfg.AddProfile<SimpleProfile>());
		var mapper = config.CreateMapper();

		var act = () => mapper.Map<SimpleSource, SimpleDestination>(null!, opts => { });

		act.Should().Throw<ArgumentNullException>();
	}
}