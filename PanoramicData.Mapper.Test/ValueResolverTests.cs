using PanoramicData.Mapper.Test.Models;

namespace PanoramicData.Mapper.Test;

public class ValueResolverTests
{
    [Fact]
    public void ValueResolver_Type_ResolvesValue()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile(new ResolverTypeProfile()));
        var mapper = config.CreateMapper();

        var source = new ValueResolverSource { FirstName = "John", LastName = "Doe" };
        var dest = mapper.Map<ValueResolverDest>(source);

        dest.FullName.Should().Be("John Doe");
    }

    [Fact]
    public void ValueResolver_Instance_ResolvesValue()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile(new ResolverInstanceProfile()));
        var mapper = config.CreateMapper();

        var source = new ValueResolverSource { FirstName = "Jane", LastName = "Smith" };
        var dest = mapper.Map<ValueResolverDest>(source);

        dest.FullName.Should().Be("Jane Smith");
    }

    private class FullNameResolver : IValueResolver<ValueResolverSource, ValueResolverDest, string>
    {
        public string Resolve(ValueResolverSource source, ValueResolverDest _destination, string _destMember, ResolutionContext _context)
            => $"{source.FirstName} {source.LastName}";
    }

    private sealed class ResolverTypeProfile : Profile
    {
        public ResolverTypeProfile()
        {
            CreateMap<ValueResolverSource, ValueResolverDest>()
                .ForMember(d => d.FullName, opt => opt.MapFrom<FullNameResolver>());
        }
    }

    private sealed class ResolverInstanceProfile : Profile
    {
        public ResolverInstanceProfile()
        {
            CreateMap<ValueResolverSource, ValueResolverDest>()
                .ForMember(d => d.FullName, opt => opt.MapFrom(new FullNameResolver()));
        }
    }
}
