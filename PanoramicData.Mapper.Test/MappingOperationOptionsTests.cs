using PanoramicData.Mapper.Test.Models;

namespace PanoramicData.Mapper.Test;

public class MappingOperationOptionsTests
{
    private sealed class SimpleMapProfile : Profile
    {
        public SimpleMapProfile() => CreateMap<SimpleSource, SimpleDestination>();
    }

    private sealed class SimpleMapWithProfileAfterMapProfile : Profile
    {
        public SimpleMapWithProfileAfterMapProfile()
            => CreateMap<SimpleSource, SimpleDestination>()
                .AfterMap((_, d) => d.Name += " - Profile");
    }

    [Fact]
    public void Map_WithAfterMap_ExecutesAfterMapping()
    {
        var mapper = MapperFactory.Create<SimpleMapProfile>();

        var source = new SimpleSource { Id = 1, Name = "Test" };

        var dest = mapper.Map<SimpleSource, SimpleDestination>(
            source,
            opts => opts.AfterMap((src, d) => d.Name = src.Name + " - Modified"));

        dest.Id.Should().Be(1);
        dest.Name.Should().Be("Test - Modified");
    }

    [Fact]
    public void Map_WithBeforeMap_ExecutesBeforeMapping()
    {
        var mapper = MapperFactory.Create<SimpleMapProfile>();

        var source = new SimpleSource { Id = 1, Name = "Test" };
        var beforeCalled = false;

        var dest = mapper.Map<SimpleSource, SimpleDestination>(
            source,
            opts => opts.BeforeMap((_, _) => beforeCalled = true));

        dest.Id.Should().Be(1);
        beforeCalled.Should().BeTrue();
    }

    [Fact]
    public void Map_WithAfterMap_CanModifyMultipleProperties()
    {
        var mapper = MapperFactory.Create<SimpleMapProfile>();

        var source = new SimpleSource { Id = 5, Name = "Original" };

        var dest = mapper.Map<SimpleSource, SimpleDestination>(
            source,
            opts => opts.AfterMap((src, d) =>
            {
                d.Id = src.Id * 10;
                d.Name = "Overridden";
            }));

        dest.Id.Should().Be(50);
        dest.Name.Should().Be("Overridden");
    }

    [Fact]
    public void Map_WithMultipleAfterMaps_ExecutesAllInOrder()
    {
        var mapper = MapperFactory.Create<SimpleMapProfile>();

        var source = new SimpleSource { Id = 1, Name = "Test" };

        var dest = mapper.Map<SimpleSource, SimpleDestination>(
            source,
            opts =>
            {
                opts.AfterMap((_, d) => d.Name += " - First");
                opts.AfterMap((_, d) => d.Name += " - Second");
            });

        dest.Name.Should().Be("Test - First - Second");
    }

    [Fact]
    public void Map_WithItems_CanPassContextualData()
    {
        var mapper = MapperFactory.Create<SimpleMapProfile>();

        var source = new SimpleSource { Id = 1, Name = "Test" };

        var dest = mapper.Map<SimpleSource, SimpleDestination>(
            source,
            opts =>
            {
                opts.Items["suffix"] = " - Custom";
                opts.AfterMap((_, d) => d.Name += (string)opts.Items["suffix"]);
            });

        dest.Name.Should().Be("Test - Custom");
    }

    [Fact]
    public void Map_WithNullOpts_ThrowsArgumentNullException()
    {
        var mapper = MapperFactory.Create<SimpleMapProfile>();

        var source = new SimpleSource { Id = 1, Name = "Test" };

        var act = () => mapper.Map<SimpleSource, SimpleDestination>(
            source,
            (Action<IMappingOperationOptions<SimpleSource, SimpleDestination>>)null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Map_WithAfterMap_ProfileAfterMapAlsoExecutes()
    {
        var mapper = MapperFactory.Create<SimpleMapWithProfileAfterMapProfile>();

        var source = new SimpleSource { Id = 1, Name = "Test" };

        var dest = mapper.Map<SimpleSource, SimpleDestination>(
            source,
            opts => opts.AfterMap((_, d) => d.Name += " - Inline"));

        // Profile AfterMap runs during Map, then inline AfterMap runs after
        dest.Name.Should().Be("Test - Profile - Inline");
    }
}
