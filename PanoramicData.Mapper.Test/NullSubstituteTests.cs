using PanoramicData.Mapper.Test.Models;

namespace PanoramicData.Mapper.Test;

public class NullSubstituteTests
{
    [Fact]
    public void NullSubstitute_WhenSourceNull_UsesSubstitute()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile(new NullSubProfile()));
        var mapper = config.CreateMapper();

        var source = new NullSubSource { Id = 1, Name = null, Email = null };
        var dest = mapper.Map<NullSubDest>(source);

        dest.Id.Should().Be(1);
        dest.Name.Should().Be("N/A");
        dest.Email.Should().Be("unknown@example.com");
    }

    [Fact]
    public void NullSubstitute_WhenSourceNotNull_UsesSourceValue()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile(new NullSubProfile()));
        var mapper = config.CreateMapper();

        var source = new NullSubSource { Id = 1, Name = "Alice", Email = "alice@test.com" };
        var dest = mapper.Map<NullSubDest>(source);

        dest.Id.Should().Be(1);
        dest.Name.Should().Be("Alice");
        dest.Email.Should().Be("alice@test.com");
    }

    private sealed class NullSubProfile : Profile
    {
        public NullSubProfile()
        {
            CreateMap<NullSubSource, NullSubDest>()
                .ForMember(d => d.Name, opt =>
                {
                    opt.MapFrom(s => s.Name!);
                    opt.NullSubstitute("N/A");
                })
                .ForMember(d => d.Email, opt =>
                {
                    opt.MapFrom(s => s.Email!);
                    opt.NullSubstitute("unknown@example.com");
                });
        }
    }
}
