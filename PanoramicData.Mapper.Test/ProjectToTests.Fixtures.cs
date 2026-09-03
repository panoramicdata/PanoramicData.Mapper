using Microsoft.EntityFrameworkCore;
using PanoramicData.Mapper.Test.Models;

namespace PanoramicData.Mapper.Test;

/// <summary>
/// The EF Core context, seed data and mapping profiles shared by the ProjectTo test files.
/// </summary>
public partial class ProjectToTests
{
	private static TestDbContext CreateContext()
	{
		var options = new DbContextOptionsBuilder<TestDbContext>()
			.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
			.Options;
		return new TestDbContext(options);
	}

	private static void SeedData(TestDbContext context)
	{
		context.Sources.Add(new SimpleSource
		{
			Id = 1,
			Name = "First",
			Description = "Desc1",
			CreatedDate = new DateTime(2026, 1, 1),
			Amount = 10m
		});
		context.Sources.Add(new SimpleSource
		{
			Id = 2,
			Name = "Second",
			Description = "Desc2",
			CreatedDate = new DateTime(2026, 2, 1),
			Amount = 20m
		});
		context.SaveChanges();
	}

	private class SimpleProjectProfile : Profile
	{
		public SimpleProjectProfile()
		{
			CreateMap<SimpleSource, SimpleDestination>();
		}
	}

	private class PersonProjectProfile : Profile
	{
		public PersonProjectProfile()
		{
			CreateMap<PersonSource, PersonDest>()
				.ForMember(d => d.FullName, opt => opt.MapFrom(s => s.FirstName + " " + s.LastName));
		}
	}

	private class IgnoreProjectProfile : Profile
	{
		public IgnoreProjectProfile()
		{
			CreateMap<SimpleSource, DestinationWithIgnoredProps>()
				.ForMember(d => d.Secret, opt => opt.Ignore())
				.ForMember(d => d.Timestamp, opt => opt.Ignore());
		}
	}

	private class NullableDoubleToStringProfile : Profile
	{
		public NullableDoubleToStringProfile() => CreateMap<NullableDoubleEntity, StringScoreDestination>();
	}

	private class NullablePortProfile : Profile
	{
		public NullablePortProfile() => CreateMap<NullablePortEntity, NonNullablePortDto>();
	}

	private static void SeedTree(TestDbContext context)
	{
		var grandchild = new ProjTreeEntity { Id = 3, Name = "Grandchild" };
		var child = new ProjTreeEntity { Id = 2, Name = "Child", Child = grandchild };
		var root = new ProjTreeEntity { Id = 1, Name = "Root", Child = child };
		context.ProjTrees.AddRange(grandchild, child, root);
		context.SaveChanges();
	}

	private static void SeedParentWithChildren(TestDbContext context)
	{
		context.ProjParents.Add(new ProjParentEntity
		{
			Id = 1,
			Name = "Sub-A",
			Children =
			{
				new ProjChildEntity { Id = 1, Sku = "ENT", Seats = 5 },
				new ProjChildEntity { Id = 2, Sku = "DEV", Seats = 3 }
			}
		});
		context.SaveChanges();
	}

	private class NestedCollectionProjectProfile : Profile
	{
		public NestedCollectionProjectProfile()
		{
			CreateMap<ProjChildEntity, ProjChildDto>();
			CreateMap<ProjParentEntity, ProjParentDto>();
			CreateMap<ProjParentEntity, ProjParentListDto>();
			CreateMap<ProjParentEntity, ProjParentArrayDto>();
			CreateMap<ProjParentEntity, ProjParentEnumerableDto>();
		}
	}

	private class NestedReferenceProjectProfile : Profile
	{
		public NestedReferenceProjectProfile()
		{
			CreateMap<ProjAddressEntity, ProjAddressDto>();
			CreateMap<ProjOrderEntity, ProjOrderDto>();
		}
	}

	private class ExplicitExpansionProfile : Profile
	{
		public ExplicitExpansionProfile()
		{
			CreateMap<ProjChildEntity, ProjChildDto>();
			CreateMap<ProjParentEntity, ProjParentDto>()
				.ForMember(d => d.Children, opt => opt.ExplicitExpansion());
		}
	}

	private class TreeProjectProfile : Profile
	{
		public TreeProjectProfile() => CreateMap<ProjTreeEntity, ProjTreeDto>();
	}

	private class TreeMaxDepthProjectProfile : Profile
	{
		public TreeMaxDepthProjectProfile() => CreateMap<ProjTreeEntity, ProjTreeDto>().MaxDepth(2);
	}

	private class PassThroughProfile : Profile
	{
		public PassThroughProfile() => CreateMap<PassThroughSource, PassThroughDest>();
	}
}
