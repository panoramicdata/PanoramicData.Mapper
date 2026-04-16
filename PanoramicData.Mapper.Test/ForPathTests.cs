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

    [Fact]
    public void ForPath_ThreeLevelsDeep_MapsCorrectly()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile(new DeepPathProfile()));
        var mapper = config.CreateMapper();

        var source = new DeepPathSource { ZipCode = "90210", Country = "US" };
        var dest = mapper.Map<DeepPathDest>(source);

        dest.Location.Region.ZipCode.Should().Be("90210");
        dest.Location.Region.Country.Should().Be("US");
    }

    [Fact]
    public void ForPath_ThreeLevelsDeep_NullIntermediates_CreatesAll()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile(new DeepPathProfile()));
        var mapper = config.CreateMapper();

        var source = new DeepPathSource { ZipCode = "12345", Country = "DE" };
        var dest = new DeepPathDest { Location = null! };

        var result = mapper.Map(source, dest);

        result.Location.Should().NotBeNull();
        result.Location.Region.Should().NotBeNull();
        result.Location.Region.ZipCode.Should().Be("12345");
        result.Location.Region.Country.Should().Be("DE");
    }

    [Fact]
    public void ForPath_ThreeLevelsDeep_ConfigurationIsValid()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile(new DeepPathProfile()));

        config.AssertConfigurationIsValid();
    }

    private class DeepPathProfile : Profile
    {
        public DeepPathProfile()
        {
            CreateMap<DeepPathSource, DeepPathDest>()
                .ForPath(d => d.Location.Region.ZipCode, opt => opt.MapFrom(s => s.ZipCode))
                .ForPath(d => d.Location.Region.Country, opt => opt.MapFrom(s => s.Country));
        }
    }
}
