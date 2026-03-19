using PanoramicData.Mapper.Test.Models;

namespace PanoramicData.Mapper.Test;

public class ConstructionTests
{
    [Fact]
    public void ConstructUsing_CustomFactory_CreatesDestination()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile(new ConstructUsingProfile()));
        var mapper = config.CreateMapper();

        var source = new ConstructSource { First = "John", Last = "Doe", Value = 42 };
        var dest = mapper.Map<ConstructDest>(source);

        dest.Combined.Should().Be("John Doe");
        dest.Value.Should().Be(42);
    }

    [Fact]
    public void ForCtorParam_MapsConstructorParameters()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile(new CtorParamProfile()));
        var mapper = config.CreateMapper();

        var source = new CtorParamSource { FirstName = "Alice", Age = 30 };
        var dest = mapper.Map<CtorParamDest>(source);

        dest.Name.Should().Be("Alice");
        dest.Age.Should().Be(30);
    }

    private sealed class ConstructUsingProfile : Profile
    {
        public ConstructUsingProfile()
        {
            CreateMap<ConstructSource, ConstructDest>()
                .ConstructUsing(src => new ConstructDest($"{src.First} {src.Last}"));
        }
    }

    private sealed class CtorParamProfile : Profile
    {
        public CtorParamProfile()
        {
            CreateMap<CtorParamSource, CtorParamDest>()
                .ForCtorParam("name", opt => opt.MapFrom(s => s.FirstName))
                .ForCtorParam("age", opt => opt.MapFrom(s => s.Age));
        }
    }
}
