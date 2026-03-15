using PanoramicData.Mapper.Test.Models;

namespace PanoramicData.Mapper.Test;

public class FlatteningTests
{
    [Fact]
    public void Map_Flattening_MapsNestedPropertyByConvention()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new FlattenProfile());
        });
        var mapper = config.CreateMapper();

        var source = new CustomerSource
        {
            Id = 1,
            Customer = new CustomerNameSource { Name = "Alice", Age = 30 }
        };

        var dest = mapper.Map<FlatCustomerDest>(source);

        dest.Id.Should().Be(1);
        dest.CustomerName.Should().Be("Alice");
        dest.CustomerAge.Should().Be(30);
    }

    [Fact]
    public void Map_Flattening_NullIntermediate_ReturnsNull()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new FlattenProfile());
        });
        var mapper = config.CreateMapper();

        var source = new CustomerSource
        {
            Id = 2,
            Customer = null!
        };

        var dest = mapper.Map<FlatCustomerDest>(source);

        dest.Id.Should().Be(2);
        dest.CustomerName.Should().BeNull();
    }

    [Fact]
    public void Map_Flattening_DeepNesting_MapsCorrectly()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new DeepFlattenProfile());
        });
        var mapper = config.CreateMapper();

        var source = new DeepSource
        {
            Id = 5,
            Order = new Level1Source
            {
                Item = new Level2Source { Name = "Widget" }
            }
        };

        var dest = mapper.Map<DeepFlatDest>(source);

        dest.Id.Should().Be(5);
        dest.OrderItemName.Should().Be("Widget");
    }

    [Fact]
    public void Map_Flattening_GetMethod_MapsCorrectly()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new GetterProfile());
        });
        var mapper = config.CreateMapper();

        var source = new GetterSource("42") { Id = 1 };

        var dest = mapper.Map<GetterDest>(source);

        dest.Id.Should().Be(1);
        dest.Total.Should().Be("42");
    }

    [Fact]
    public void AssertConfigurationIsValid_FlattenedMembers_DoesNotThrow()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new FlattenProfile());
        });

        var act = () => config.AssertConfigurationIsValid();

        act.Should().NotThrow();
    }

    [Fact]
    public void AssertConfigurationIsValid_DeepFlatten_DoesNotThrow()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new DeepFlattenProfile());
        });

        var act = () => config.AssertConfigurationIsValid();

        act.Should().NotThrow();
    }

    private class FlattenProfile : Profile
    {
        public FlattenProfile()
        {
            CreateMap<CustomerSource, FlatCustomerDest>();
        }
    }

    private class DeepFlattenProfile : Profile
    {
        public DeepFlattenProfile()
        {
            CreateMap<DeepSource, DeepFlatDest>();
        }
    }

    private class GetterProfile : Profile
    {
        public GetterProfile()
        {
            CreateMap<GetterSource, GetterDest>();
        }
    }
}
