using PanoramicData.Mapper.Test.Models;

namespace PanoramicData.Mapper.Test;

public class NestedMappingTests
{
    [Fact]
    public void Map_NestedComplexType_MapsRecursively()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new NestedProfile());
        });
        var mapper = config.CreateMapper();

        var source = new OrderSource
        {
            Id = 1,
            Address = new AddressSource { Street = "123 Main St", City = "Springfield" }
        };

        var dest = mapper.Map<OrderDest>(source);

        dest.Id.Should().Be(1);
        dest.Address.Should().NotBeNull();
        dest.Address.Street.Should().Be("123 Main St");
        dest.Address.City.Should().Be("Springfield");
    }

    [Fact]
    public void Map_NestedComplexType_NullSource_LeavesDestDefault()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new NestedProfile());
        });
        var mapper = config.CreateMapper();

        var source = new OrderSource
        {
            Id = 2,
            Address = null!
        };

        var dest = mapper.Map<OrderDest>(source);

        dest.Id.Should().Be(2);
        dest.Address.Should().NotBeNull(); // default from initializer
        dest.Address.Street.Should().BeEmpty();
    }

    [Fact]
    public void Map_NestedCollectionProperty_MapsElements()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new NestedCollectionProfile());
        });
        var mapper = config.CreateMapper();

        var source = new OrderWithCollectionSource
        {
            Id = 10,
            Items =
            [
                new LineItemSource { Product = "Widget", Quantity = 3 },
                new LineItemSource { Product = "Gadget", Quantity = 1 }
            ]
        };

        var dest = mapper.Map<OrderWithCollectionDest>(source);

        dest.Id.Should().Be(10);
        dest.Items.Should().HaveCount(2);
        dest.Items[0].Product.Should().Be("Widget");
        dest.Items[0].Quantity.Should().Be(3);
        dest.Items[1].Product.Should().Be("Gadget");
        dest.Items[1].Quantity.Should().Be(1);
    }

    [Fact]
    public void AssertConfigurationIsValid_NestedMapping_DoesNotThrow()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new NestedProfile());
        });

        var act = () => config.AssertConfigurationIsValid();

        act.Should().NotThrow();
    }

    private sealed class NestedProfile : Profile
    {
        public NestedProfile()
        {
            CreateMap<AddressSource, AddressDest>();
            CreateMap<OrderSource, OrderDest>();
        }
    }

    private sealed class NestedCollectionProfile : Profile
    {
        public NestedCollectionProfile()
        {
            CreateMap<LineItemSource, LineItemDest>();
            CreateMap<OrderWithCollectionSource, OrderWithCollectionDest>();
        }
    }
}
