using System.Linq.Expressions;
using PanoramicData.Mapper.Test.Models;

namespace PanoramicData.Mapper.Test.Profiles;

internal static class ProfileHelper
{
	public static void IgnoreMembers<TSource, TDestination>(
		IMappingExpression<TSource, TDestination> map,
		params Expression<Func<TDestination, object?>>[] members)
	{
		foreach (var member in members)
		{
			map.ForMember(member, opt => opt.Ignore());
		}
	}
}

public class SimpleProfile : Profile
{
	public SimpleProfile()
	{
		CreateMap<SimpleSource, SimpleDestination>();
	}
}

public class IgnoreProfile : Profile
{
	public IgnoreProfile()
	{
		var map = CreateMap<SimpleSource, DestinationWithIgnoredProps>();
		ProfileHelper.IgnoreMembers(map, d => d.Secret, d => d.Timestamp);
	}
}

public class MapFromProfile : Profile
{
	public MapFromProfile()
	{
		CreateMap<SourceWithNested, FlatDestination>()
			.ForMember(d => d.InnerValue, opt => opt.MapFrom(s => s.Inner.Value))
			.ForMember(d => d.InnerNumber, opt => opt.MapFrom(s => s.Inner.Number));
	}
}

public class MapFromWithTransformProfile : Profile
{
	public MapFromWithTransformProfile()
	{
		CreateMap<SourceForTransform, DestForTransform>()
			.ForMember(d => d.ChannelWidth, opt => opt.MapFrom(s => s.ChannelWidth.Replace(" MHz", "")))
			.ForMember(d => d.Power, opt => opt.MapFrom(s => s.Power.Replace(" dBm", "")));
	}
}

public class MapFromComputedProfile : Profile
{
	public MapFromComputedProfile()
	{
		CreateMap<PersonSource, PersonDest>()
			.ForMember(d => d.FullName, opt => opt.MapFrom(s => s.FirstName + " " + s.LastName));
	}
}

public class AfterMapProfile : Profile
{
	public AfterMapProfile()
	{
		var map = CreateMap<CloneableEntity, CloneableEntity>();
		ProfileHelper.IgnoreMembers(map, d => d.Id, d => d.CreatedDateTimeUtc, d => d.LastModifiedDateTimeUtc);
		map.AfterMap((src, dst) =>
		{
			dst.Name = $"{src.Name} - Clone";
		});
	}
}

public class ForAllMembersProfile : Profile
{
	public ForAllMembersProfile()
	{
		CreateMap<SimpleSource, SimpleDestination>()
			.AfterMap((src, dest) =>
			{
				dest.Name = src.Name;
				dest.Description = src.Description;
			})
			.ForAllMembers(opt => opt.Ignore());
	}
}

public class StringNameProfile : Profile
{
	public StringNameProfile()
	{
		CreateMap<SourceWithExtra, DestinationWithExtra>()
			.ForMember(nameof(DestinationWithExtra.Computed), opt => opt.MapFrom(s => s.Extra + "!"));
	}
}

public class UnmappedProfile : Profile
{
	public UnmappedProfile()
	{
		CreateMap<SimpleSource, DestinationWithUnmappedProp>();
	}
}
