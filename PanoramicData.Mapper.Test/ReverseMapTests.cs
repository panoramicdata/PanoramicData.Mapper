using PanoramicData.Mapper.Test.Models;

namespace PanoramicData.Mapper.Test;

public class ReverseMapTests
{
    [Fact]
    public void ReverseMap_SimpleMapping_MapsInBothDirections()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile(new ReverseMapProfile()));
        var mapper = config.CreateMapper();

        var source = new ReverseSource { Id = 1, Name = "Test" };
        var dest = mapper.Map<ReverseDest>(source);

        dest.Id.Should().Be(1);
        dest.Name.Should().Be("Test");

        var reverse = mapper.Map<ReverseSource>(dest);
        reverse.Id.Should().Be(1);
        reverse.Name.Should().Be("Test");
    }

    [Fact]
    public void ReverseMap_ConfigurationIsValid_DoesNotThrow()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile(new ReverseMapProfile()));

        config.AssertConfigurationIsValid();
    }

    private class ReverseMapProfile : Profile
    {
        public ReverseMapProfile()
        {
            CreateMap<ReverseSource, ReverseDest>()
                .ReverseMap();
        }
    }
}
