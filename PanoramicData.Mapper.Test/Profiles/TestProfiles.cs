using PanoramicData.Mapper.Test.Models;

namespace PanoramicData.Mapper.Test.Profiles;

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
		CreateMap<SimpleSource, DestinationWithIgnoredProps>()
			.ForMember(d => d.Secret, opt => opt.Ignore())
			.ForMember(d => d.Timestamp, opt => opt.Ignore());
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
		CreateMap<CloneableEntity, CloneableEntity>()
			.ForMember(d => d.Id, opt => opt.Ignore())
			.ForMember(d => d.CreatedDateTimeUtc, opt => opt.Ignore())
			.ForMember(d => d.LastModifiedDateTimeUtc, opt => opt.Ignore())
			.AfterMap((src, dst) =>
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