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

    [Fact]
    public void ReverseMap_WithForMemberOnForward_DoesNotAffectReverse()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile(new ReverseWithForMemberProfile()));
        var mapper = config.CreateMapper();

        // Forward: Name is mapped from custom expression
        var source = new PersonSource { FirstName = "John", LastName = "Doe", Age = 30 };
        var dest = mapper.Map<PersonDest>(source);
        dest.FullName.Should().Be("John Doe");

        // Reverse: FullName convention-maps; FirstName/LastName have no matching source on PersonDest
        var reverse = mapper.Map<PersonSource>(dest);
        reverse.Age.Should().Be(30);
    }

    [Fact]
    public void ReverseMap_WithForMemberOnReverse_AppliesReverseConfig()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile(new ReverseWithReverseMemberProfile()));
        var mapper = config.CreateMapper();

        var dest = new ReverseDest { Id = 1, Name = "Reversed" };
        var source = mapper.Map<ReverseSource>(dest);

        source.Id.Should().Be(1);
        source.Name.Should().Be("REVERSED");
    }

    [Fact]
    public void ReverseMap_WithIgnoreOnForward_ReverseStillMaps()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile(new ReverseWithIgnoreProfile()));
        var mapper = config.CreateMapper();

        // Forward: Name is ignored
        var source = new ReverseSource { Id = 1, Name = "Ignored" };
        var dest = mapper.Map<ReverseDest>(source);
        dest.Name.Should().Be(string.Empty);

        // Reverse: Name should still map normally (ignore is on forward only)
        var dest2 = new ReverseDest { Id = 2, Name = "Hello" };
        var reverse = mapper.Map<ReverseSource>(dest2);
        reverse.Id.Should().Be(2);
        reverse.Name.Should().Be("Hello");
    }

    private class ReverseWithForMemberProfile : Profile
    {
        public ReverseWithForMemberProfile()
        {
            CreateMap<PersonSource, PersonDest>()
                .ForMember(d => d.FullName, opt => opt.MapFrom(s => s.FirstName + " " + s.LastName))
                .ReverseMap();
        }
    }

    private class ReverseWithReverseMemberProfile : Profile
    {
        public ReverseWithReverseMemberProfile()
        {
            CreateMap<ReverseSource, ReverseDest>()
                .ReverseMap()
                .ForMember(d => d.Name, opt => opt.MapFrom(s => s.Name.ToUpperInvariant()));
        }
    }

    private class ReverseWithIgnoreProfile : Profile
    {
        public ReverseWithIgnoreProfile()
        {
            CreateMap<ReverseSource, ReverseDest>()
                .ForMember(d => d.Name, opt => opt.Ignore())
                .ReverseMap();
        }
    }
}
