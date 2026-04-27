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

        dest.Should().ContainSingle();
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

        var result = mapper.Map<List<SimpleSource>, List<SimpleDestination>>(source);

        var dest = result.Should().BeOfType<List<SimpleDestination>>().Subject;
        dest.Should().ContainSingle();
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

    [Fact]
    public void MapGeneric_NoElementTypeMap_ThrowsAutoMapperMappingException()
    {
        var config = new MapperConfiguration(cfg => { });
        var mapper = config.CreateMapper();

        var source = new List<SimpleSource> { new() { Id = 1 } };
        var act = () => mapper.Map<List<SimpleSource>, List<SimpleDestination>>(source);

        act.Should().Throw<AutoMapperMappingException>();
    }

    [Fact]
    public void Map_IEnumerableDestination_MapsAllElements()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new ElementProfile());
        });
        var mapper = config.CreateMapper();

        var source = new List<SimpleSource>
        {
            new() { Id = 1, Name = "A" },
            new() { Id = 2, Name = "B" }
        };

        var dest = mapper.Map<IEnumerable<SimpleDestination>>(source);

        dest.Should().HaveCount(2);
    }

    [Fact]
    public void Map_ArrayToList_MapsAllElements()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new ElementProfile());
        });
        var mapper = config.CreateMapper();

        var source = new[] { new SimpleSource { Id = 1, Name = "FromArray" } };

        var dest = mapper.Map<List<SimpleDestination>>(source);

        dest.Should().ContainSingle();
        dest[0].Name.Should().Be("FromArray");
    }

    [Fact]
    public void MapWithOptions_Collection_MapsAllElements()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new ElementProfile());
        });
        var mapper = config.CreateMapper();

        var source = new List<SimpleSource>
        {
            new() { Id = 1, Name = "Opt1" },
            new() { Id = 2, Name = "Opt2" }
        };

        var dest = mapper.Map<List<SimpleSource>, List<SimpleDestination>>(source, opts => { });

        dest.Should().HaveCount(2);
        dest[0].Name.Should().Be("Opt1");
    }

    private class ElementProfile : Profile
    {
        public ElementProfile()
        {
            CreateMap<SimpleSource, SimpleDestination>();
        }
    }

    // --- Interface collection property tests ---

    [Fact]
    public void Map_CollectionProperty_IList_MapsCorrectly()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile(new OrderWithInterfaceProfile()));
        var mapper = config.CreateMapper();

        var source = new OrderSourceWithItems
        {
            Id = 1,
            Items = [new() { Product = "Widget", Quantity = 3 }]
        };

        var dest = mapper.Map<OrderDestWithIList>(source);

        dest.Id.Should().Be(1);
        dest.Items.Should().ContainSingle();
        dest.Items[0].Product.Should().Be("Widget");
        dest.Items[0].Quantity.Should().Be(3);
    }

    [Fact]
    public void Map_CollectionProperty_ICollection_MapsCorrectly()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile(new OrderWithInterfaceProfile()));
        var mapper = config.CreateMapper();

        var source = new OrderSourceWithItems
        {
            Id = 2,
            Items = [new() { Product = "Gadget", Quantity = 5 }]
        };

        var dest = mapper.Map<OrderDestWithICollection>(source);

        dest.Id.Should().Be(2);
        dest.Items.Should().ContainSingle();
        dest.Items.First().Product.Should().Be("Gadget");
    }

    [Fact]
    public void Map_CollectionProperty_IEnumerable_MapsCorrectly()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile(new OrderWithInterfaceProfile()));
        var mapper = config.CreateMapper();

        var source = new OrderSourceWithItems
        {
            Id = 3,
            Items = [new() { Product = "Doohickey", Quantity = 1 }]
        };

        var dest = mapper.Map<OrderDestWithIEnumerable>(source);

        dest.Id.Should().Be(3);
        dest.Items.Should().ContainSingle();
        dest.Items.First().Product.Should().Be("Doohickey");
    }

    private class OrderWithInterfaceProfile : Profile
    {
        public OrderWithInterfaceProfile()
        {
            CreateMap<LineItemSource, LineItemDest>();
            CreateMap<OrderSourceWithItems, OrderDestWithIList>();
            CreateMap<OrderSourceWithItems, OrderDestWithICollection>();
            CreateMap<OrderSourceWithItems, OrderDestWithIEnumerable>();
        }
    }

    [Fact]
    public void Map_CollectionProperty_IList_WithMapFrom_MapsCorrectly()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile(new OrderWithIListMapFromProfile()));
        var mapper = config.CreateMapper();

        var source = new OrderSourceWithItems
        {
            Id = 10,
            Items = [new() { Product = "Sprocket", Quantity = 7 }]
        };

        var dest = mapper.Map<OrderDestWithIList>(source);

        dest.Id.Should().Be(10);
        dest.Items.Should().ContainSingle();
        dest.Items[0].Product.Should().Be("Sprocket");
        dest.Items[0].Quantity.Should().Be(7);
    }

    private class OrderWithIListMapFromProfile : Profile
    {
        public OrderWithIListMapFromProfile()
        {
            CreateMap<LineItemSource, LineItemDest>();
            CreateMap<OrderSourceWithItems, OrderDestWithIList>()
                .ForMember(d => d.Items, opt => opt.MapFrom(s => s.Items));
        }
    }
}
