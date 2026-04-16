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

    private class TrimProfile : Profile
    {
        public TrimProfile()
        {
            CreateMap<TransformSource, TransformDest>()
                .AddTransform<string>(s => s.Trim());
        }
    }

    private class MultiTransformProfile : Profile
    {
        public MultiTransformProfile()
        {
            CreateMap<TransformSource, TransformDest>()
                .AddTransform<string>(s => s.Trim())
                .AddTransform<string>(s => s.ToUpperInvariant());
        }
    }

    [Fact]
    public void AddTransform_Int_TransformsAllIntProperties()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile(new IntTransformProfile()));
        var mapper = config.CreateMapper();

        var source = new TransformSource { Name = "Test", Description = "Desc", Count = 5 };
        var dest = mapper.Map<TransformDest>(source);

        dest.Count.Should().Be(10);
        dest.Name.Should().Be("Test"); // string not affected
    }

    [Fact]
    public void AddTransform_MixedTypes_EachAppliedToMatchingProperties()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile(new MixedTransformProfile()));
        var mapper = config.CreateMapper();

        var source = new TransformSource { Name = "  hello  ", Description = "  world  ", Count = 3 };
        var dest = mapper.Map<TransformDest>(source);

        dest.Name.Should().Be("hello");
        dest.Description.Should().Be("world");
        dest.Count.Should().Be(6);
    }

    private class IntTransformProfile : Profile
    {
        public IntTransformProfile()
        {
            CreateMap<TransformSource, TransformDest>()
                .AddTransform<int>(x => x * 2);
        }
    }

    private class MixedTransformProfile : Profile
    {
        public MixedTransformProfile()
        {
            CreateMap<TransformSource, TransformDest>()
                .AddTransform<string>(s => s.Trim())
                .AddTransform<int>(x => x * 2);
        }
    }
}
