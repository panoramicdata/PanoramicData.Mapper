using PanoramicData.Mapper.Test.Models;

namespace PanoramicData.Mapper.Test;

public class TypeConverterTests
{
    [Fact]
    public void ConvertUsing_Lambda_ConvertsFully()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile(new LambdaConverterProfile()));
        var mapper = config.CreateMapper();

        var source = new ConverterSource { Value = "hello", Multiplier = 3 };
        var dest = mapper.Map<ConverterDest>(source);

        dest.Result.Should().Be("hellohellohello");
    }

    [Fact]
    public void ConvertUsing_TypeConverter_ConvertsFully()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile(new TypeConverterProfile()));
        var mapper = config.CreateMapper();

        var source = new ConverterSource { Value = "abc", Multiplier = 2 };
        var dest = mapper.Map<ConverterDest>(source);

        dest.Result.Should().Be("abcabc");
    }

    [Fact]
    public void ConvertUsing_ConverterInstance_ConvertsFully()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile(new ConverterInstanceProfile()));
        var mapper = config.CreateMapper();

        var source = new ConverterSource { Value = "x", Multiplier = 5 };
        var dest = mapper.Map<ConverterDest>(source);

        dest.Result.Should().Be("xxxxx");
    }

    [Fact]
    public void ConvertUsing_ConverterInstance_ReceivesResolutionContext()
    {
        ContextCapturingProfile.CapturedContext = null;
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile(new ContextCapturingProfile()));
        var mapper = config.CreateMapper();

        mapper.Map<ConverterDest>(new ConverterSource { Value = "a", Multiplier = 1 });

        ContextCapturingProfile.CapturedContext.Should().NotBeNull();
    }

    [Fact]
    public void ConvertUsing_ConverterInstance_ReceivesDefaultDestination()
    {
        DestCapturingProfile.CapturedDest = new ConverterDest { Result = "sentinel" };
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile(new DestCapturingProfile()));
        var mapper = config.CreateMapper();

        mapper.Map<ConverterDest>(new ConverterSource { Value = "a", Multiplier = 1 });

        // Instance converter via MappingExpression wraps with default! destination
        DestCapturingProfile.CapturedDest.Should().BeNull();
    }

    [Fact]
    public void ConvertUsing_ConfigurationIsValid_DoesNotThrow()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile(new LambdaConverterProfile()));

        // ConvertUsing maps should not trigger validation errors
        config.AssertConfigurationIsValid();
    }

    private class RepeatConverter : ITypeConverter<ConverterSource, ConverterDest>
    {
        public ConverterDest Convert(ConverterSource source, ConverterDest _destination, ResolutionContext context)
            => new() { Result = string.Concat(Enumerable.Repeat(source.Value, source.Multiplier)) };
    }

    private class LambdaConverterProfile : Profile
    {
        public LambdaConverterProfile()
        {
            CreateMap<ConverterSource, ConverterDest>()
                .ConvertUsing(src => new ConverterDest
                {
                    Result = string.Concat(Enumerable.Repeat(src.Value, src.Multiplier))
                });
        }
    }

    private class TypeConverterProfile : Profile
    {
        public TypeConverterProfile()
        {
            CreateMap<ConverterSource, ConverterDest>()
                .ConvertUsing<RepeatConverter>();
        }
    }

    private class ConverterInstanceProfile : Profile
    {
        public ConverterInstanceProfile()
        {
            CreateMap<ConverterSource, ConverterDest>()
                .ConvertUsing(new RepeatConverter());
        }
    }

    private class ContextCapturingConverter(Action<ResolutionContext> capture) : ITypeConverter<ConverterSource, ConverterDest>
    {
        public ConverterDest Convert(ConverterSource source, ConverterDest destination, ResolutionContext context)
        {
            capture(context);
            return new ConverterDest { Result = source.Value };
        }
    }

    private class ContextCapturingProfile : Profile
    {
        public static ResolutionContext? CapturedContext { get; set; }

        public ContextCapturingProfile()
        {
            CreateMap<ConverterSource, ConverterDest>()
                .ConvertUsing(new ContextCapturingConverter(ctx => CapturedContext = ctx));
        }
    }

    private class DestCapturingConverter(Action<ConverterDest?> capture) : ITypeConverter<ConverterSource, ConverterDest>
    {
        public ConverterDest Convert(ConverterSource source, ConverterDest destination, ResolutionContext context)
        {
            capture(destination);
            return new ConverterDest { Result = source.Value };
        }
    }

    private class DestCapturingProfile : Profile
    {
        public static ConverterDest? CapturedDest { get; set; }

        public DestCapturingProfile()
        {
            CreateMap<ConverterSource, ConverterDest>()
                .ConvertUsing(new DestCapturingConverter(d => CapturedDest = d));
        }
    }
}
