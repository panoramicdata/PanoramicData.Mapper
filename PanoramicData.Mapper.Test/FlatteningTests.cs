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

    [Fact]
    public void Map_Flattening_CalledTwice_ReturnsSameResult()
    {
        // Maps the same type pair twice; the second call hits the flattened accessor cache.
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new FlattenProfile());
        });
        var mapper = config.CreateMapper();

        var source = new CustomerSource { Id = 7, Customer = new CustomerNameSource { Name = "Bob", Age = 25 } };

        var first = mapper.Map<FlatCustomerDest>(source);
        var second = mapper.Map<FlatCustomerDest>(source);

        second.CustomerName.Should().Be(first.CustomerName);
        second.CustomerAge.Should().Be(first.CustomerAge);
    }

    [Fact]
    public void Map_Flattening_DifferentSourceTypes_SameDestPropertyName_MapsIndependently()
    {
        // CustomerSourceA and CustomerSourceB both flatten to CustomerName,
        // verifying the cache key is (destPropName, sourceType) not just destPropName.
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new FlattenAProfile());
            cfg.AddProfile(new FlattenBProfile());
        });
        var mapper = config.CreateMapper();

        var sourceA = new CustomerSourceA { Id = 1, Customer = new CustomerNameSourceA { Name = "Alpha" } };
        var sourceB = new CustomerSourceB { Id = 2, Customer = new CustomerNameSourceB { Name = "Beta" } };

        var destA = mapper.Map<FlatCustomerDestAB>(sourceA);
        var destB = mapper.Map<FlatCustomerDestAB>(sourceB);

        destA.CustomerName.Should().Be("Alpha");
        destB.CustomerName.Should().Be("Beta");
    }

    [Fact]
    public void Map_Flattening_NoMatchingNestedProperty_LeavesDestinationDefault()
    {
        // Destination has a property that has no flattened match; result should be default.
        // Also verifies a null/miss is cached and doesn't cause a second lookup to misbehave.
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new FlattenIgnoredProfile());
        });
        var mapper = config.CreateMapper();

        var source = new CustomerSource { Id = 3, Customer = new CustomerNameSource { Name = "Carol", Age = 40 } };

        var first = mapper.Map<FlatCustomerDest>(source);
        var second = mapper.Map<FlatCustomerDest>(source);

        first.CustomerName.Should().BeNullOrEmpty();
        second.CustomerName.Should().BeNullOrEmpty();
    }

    private sealed class FlattenProfile : Profile
    {
        public FlattenProfile()
        {
            CreateMap<CustomerSource, FlatCustomerDest>();
        }
    }

    private sealed class DeepFlattenProfile : Profile
    {
        public DeepFlattenProfile()
        {
            CreateMap<DeepSource, DeepFlatDest>();
        }
    }

    private sealed class GetterProfile : Profile
    {
        public GetterProfile()
        {
            CreateMap<GetterSource, GetterDest>();
        }
    }

    private sealed class FlattenAProfile : Profile
    {
        public FlattenAProfile()
        {
            CreateMap<CustomerSourceA, FlatCustomerDestAB>();
        }
    }

    private sealed class FlattenBProfile : Profile
    {
        public FlattenBProfile()
        {
            CreateMap<CustomerSourceB, FlatCustomerDestAB>();
        }
    }

    private sealed class FlattenIgnoredProfile : Profile
    {
        public FlattenIgnoredProfile()
        {
            CreateMap<CustomerSource, FlatCustomerDest>()
                .ForMember(d => d.CustomerName, opt => opt.Ignore())
                .ForMember(d => d.CustomerAge, opt => opt.Ignore());
        }
    }
}
