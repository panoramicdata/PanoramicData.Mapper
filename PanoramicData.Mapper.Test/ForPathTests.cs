using PanoramicData.Mapper.Test.Models;

namespace PanoramicData.Mapper.Test;

public class ForPathTests
{
    [Fact]
    public void ForPath_MapsToNestedProperty()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile(new ForPathProfile()));
        var mapper = config.CreateMapper();

        var source = new ForPathSource { Street = "123 Main St", City = "Springfield" };
        var dest = mapper.Map<ForPathDest>(source);

        dest.Address.Street.Should().Be("123 Main St");
        dest.Address.City.Should().Be("Springfield");
    }

    [Fact]
    public void ForPath_NullIntermediate_CreatesIntermediateObject()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile(new ForPathProfile()));
        var mapper = config.CreateMapper();

        var source = new ForPathSource { Street = "456 Oak Ave", City = "Shelbyville" };

        // Map to existing dest with null Address
        var dest = new ForPathDest { Address = null! };
        var result = mapper.Map(source, dest);

        result.Address.Should().NotBeNull();
        result.Address.Street.Should().Be("456 Oak Ave");
        result.Address.City.Should().Be("Shelbyville");
    }

    [Fact]
    public void ForPath_ConfigurationIsValid_DoesNotThrow()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile(new ForPathProfile()));

        config.AssertConfigurationIsValid();
    }

    private class ForPathProfile : Profile
    {
        public ForPathProfile()
        {
            CreateMap<ForPathSource, ForPathDest>()
                .ForPath(d => d.Address.Street, opt => opt.MapFrom(s => s.Street))
                .ForPath(d => d.Address.City, opt => opt.MapFrom(s => s.City));
        }
    }
}
