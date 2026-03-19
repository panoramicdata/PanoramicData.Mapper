using PanoramicData.Mapper.Test.Models;

namespace PanoramicData.Mapper.Test;

public class ValueTransformerTests
{
    [Fact]
    public void AddTransform_String_TransformsAllStringProperties()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile(new TrimProfile()));
        var mapper = config.CreateMapper();

        var source = new TransformSource
        {
            Name = "  Hello  ",
            Description = "  World  ",
            Count = 5
        };

        var dest = mapper.Map<TransformDest>(source);

        dest.Name.Should().Be("Hello");
        dest.Description.Should().Be("World");
        dest.Count.Should().Be(5); // int not affected
    }

    [Fact]
    public void AddTransform_MultipleTransforms_AppliesInOrder()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile(new MultiTransformProfile()));
        var mapper = config.CreateMapper();

        var source = new TransformSource
        {
            Name = "  hello  ",
            Description = "  world  ",
            Count = 10
        };

        var dest = mapper.Map<TransformDest>(source);

        dest.Name.Should().Be("HELLO");
        dest.Description.Should().Be("WORLD");
    }

    private sealed class TrimProfile : Profile
    {
        public TrimProfile()
        {
            CreateMap<TransformSource, TransformDest>()
                .AddTransform<string>(s => s.Trim());
        }
    }

    private sealed class MultiTransformProfile : Profile
    {
        public MultiTransformProfile()
        {
            CreateMap<TransformSource, TransformDest>()
                .AddTransform<string>(s => s.Trim())
                .AddTransform<string>(s => s.ToUpperInvariant());
        }
    }
}
