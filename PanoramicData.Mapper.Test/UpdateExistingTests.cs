using PanoramicData.Mapper.Test.Models;

namespace PanoramicData.Mapper.Test;

public class UpdateExistingTests
{
	[Fact]
	public void Map_ToExistingObject_UpdatesProperties()
	{
		var config = new MapperConfiguration(cfg =>
			cfg.AddProfile(new SimpleMapProfile()));
		var mapper = config.CreateMapper();

		var source = new SimpleSource
		{
			Id = 1,
			Name = "Updated",
			Description = "New desc",
			CreatedDate = new DateTime(2025, 6, 1),
			Amount = 100m
		};

		var existing = new SimpleDestination
		{
			Id = 999,
			Name = "Original",
			Description = "Old desc",
			CreatedDate = new DateTime(2020, 1, 1),
			Amount = 0m
		};

		var result = mapper.Map(source, existing);

		result.Should().BeSameAs(existing);
		existing.Id.Should().Be(1);
		existing.Name.Should().Be("Updated");
		existing.Description.Should().Be("New desc");
		existing.CreatedDate.Should().Be(new DateTime(2025, 6, 1));
		existing.Amount.Should().Be(100m);
	}

	[Fact]
	public void Map_ToExistingWithIgnoredProps_PreservesIgnored()
	{
		var config = new MapperConfiguration(cfg =>
			cfg.AddProfile(new IgnoreUpdateProfile()));
		var mapper = config.CreateMapper();

		var source = new SimpleSource { Id = 5, Name = "New" };
		var existing = new DestinationWithIgnoredProps
		{
			Id = 999,
			Name = "Old",
			Secret = "keep-this",
			Timestamp = new DateTime(2020, 1, 1)
		};

		mapper.Map(source, existing);

		existing.Id.Should().Be(5);
		existing.Name.Should().Be("New");
		existing.Secret.Should().Be("keep-this");
		existing.Timestamp.Should().Be(new DateTime(2020, 1, 1));
	}

	[Fact]
	public void Map_RuntimeTypes_MapsCorrectly()
	{
		var config = new MapperConfiguration(cfg =>
			cfg.AddProfile(new SimpleMapProfile()));
		var mapper = config.CreateMapper();

		var source = new SimpleSource { Id = 42, Name = "Runtime" };
		var result = mapper.Map<SimpleSource, SimpleDestination>(source);

		var dest = result.Should().BeOfType<SimpleDestination>().Subject;
		dest.Id.Should().Be(42);
		dest.Name.Should().Be("Runtime");
	}

	[Fact]
	public void Map_RuntimeTypesToExisting_MapsCorrectly()
	{
		var config = new MapperConfiguration(cfg =>
			cfg.AddProfile(new SimpleMapProfile()));
		var mapper = config.CreateMapper();

		var source = new SimpleSource { Id = 7, Name = "RT" };
		var existing = new SimpleDestination { Id = 0, Name = "Old" };
		var result = mapper.Map(source, existing);

		result.Should().BeSameAs(existing);
		existing.Id.Should().Be(7);
		existing.Name.Should().Be("RT");
	}

	private class SimpleMapProfile : Profile
	{
		public SimpleMapProfile()
		{
			CreateMap<SimpleSource, SimpleDestination>();
		}
	}

	private class IgnoreUpdateProfile : Profile
	{
		public IgnoreUpdateProfile()
		{
			CreateMap<SimpleSource, DestinationWithIgnoredProps>()
				.ForMember(d => d.Secret, opt => opt.Ignore())
				.ForMember(d => d.Timestamp, opt => opt.Ignore());
		}
	}
}