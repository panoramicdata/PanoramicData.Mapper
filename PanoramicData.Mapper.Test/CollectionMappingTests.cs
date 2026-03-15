using PanoramicData.Mapper.Test.Models;

namespace PanoramicData.Mapper.Test;

public class CollectionMappingTests
{
    [Fact]
    public void Map_List_MapsAllElements()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new ElementProfile());
        });
        var mapper = config.CreateMapper();

        var source = new List<SimpleSource>
        {
            new() { Id = 1, Name = "A", Description = "D1", Amount = 10m },
            new() { Id = 2, Name = "B", Description = "D2", Amount = 20m }
        };

        var dest = mapper.Map<List<SimpleDestination>>(source);

        dest.Should().HaveCount(2);
        dest[0].Id.Should().Be(1);
        dest[0].Name.Should().Be("A");
        dest[1].Id.Should().Be(2);
        dest[1].Name.Should().Be("B");
    }

    [Fact]
    public void Map_Array_MapsAllElements()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new ElementProfile());
        });
        var mapper = config.CreateMapper();

        var source = new[]
        {
            new SimpleSource { Id = 1, Name = "X" },
            new SimpleSource { Id = 2, Name = "Y" },
            new SimpleSource { Id = 3, Name = "Z" }
        };

        var dest = mapper.Map<SimpleDestination[]>(source);

        dest.Should().HaveCount(3);
        dest[0].Name.Should().Be("X");
        dest[1].Name.Should().Be("Y");
        dest[2].Name.Should().Be("Z");
    }

    [Fact]
    public void Map_EmptyCollection_ReturnsEmptyCollection()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new ElementProfile());
        });
        var mapper = config.CreateMapper();

        var source = new List<SimpleSource>();

        var dest = mapper.Map<List<SimpleDestination>>(source);

        dest.Should().BeEmpty();
    }

    [Fact]
    public void Map_GenericOverload_List_MapsAllElements()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new ElementProfile());
        });
        var mapper = config.CreateMapper();

        var source = new List<SimpleSource>
        {
            new() { Id = 5, Name = "Five" }
        };

        var dest = mapper.Map<List<SimpleSource>, List<SimpleDestination>>(source);

        dest.Should().HaveCount(1);
        dest[0].Id.Should().Be(5);
        dest[0].Name.Should().Be("Five");
    }

    [Fact]
    public void Map_RuntimeTypes_List_MapsAllElements()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new ElementProfile());
        });
        var mapper = config.CreateMapper();

        var source = new List<SimpleSource>
        {
            new() { Id = 7, Name = "Seven" }
        };

        var result = mapper.Map(source, typeof(List<SimpleSource>), typeof(List<SimpleDestination>));

        var dest = result.Should().BeOfType<List<SimpleDestination>>().Subject;
        dest.Should().HaveCount(1);
        dest[0].Id.Should().Be(7);
    }

    [Fact]
    public void Map_NoElementTypeMap_ThrowsAutoMapperMappingException()
    {
        var config = new MapperConfiguration(cfg => { });
        var mapper = config.CreateMapper();

        var source = new List<SimpleSource> { new() { Id = 1 } };
        var act = () => mapper.Map<List<SimpleDestination>>(source);

        act.Should().Throw<AutoMapperMappingException>();
    }

    private class ElementProfile : Profile
    {
        public ElementProfile()
        {
            CreateMap<SimpleSource, SimpleDestination>();
        }
    }
}
