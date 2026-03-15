using PanoramicData.Mapper.Test.Models;

namespace PanoramicData.Mapper.Test;

public class OpenGenericTests
{
    [Fact]
    public void OpenGeneric_ClosesTypesAndMapsConventionally()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile(new OpenGenericProfile()));
        var mapper = config.CreateMapper();

        var source = new Wrapper<int> { Value = 42, Label = "Answer" };
        var dest = mapper.Map<Wrapper<int>, WrapperDto<int>>(source);

        dest.Value.Should().Be(42);
        dest.Label.Should().Be("Answer");
    }

    [Fact]
    public void OpenGeneric_DifferentClosedTypes_MapIndependently()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile(new OpenGenericProfile()));
        var mapper = config.CreateMapper();

        var intSource = new Wrapper<int> { Value = 10, Label = "Int" };
        var stringSource = new Wrapper<string> { Value = "hello", Label = "String" };

        var intDest = mapper.Map<Wrapper<int>, WrapperDto<int>>(intSource);
        var stringDest = mapper.Map<Wrapper<string>, WrapperDto<string>>(stringSource);

        intDest.Value.Should().Be(10);
        intDest.Label.Should().Be("Int");
        stringDest.Value.Should().Be("hello");
        stringDest.Label.Should().Be("String");
    }

    private class OpenGenericProfile : Profile
    {
        public OpenGenericProfile()
        {
            CreateMap(typeof(Wrapper<>), typeof(WrapperDto<>));
        }
    }
}
