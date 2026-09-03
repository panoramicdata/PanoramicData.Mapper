using Microsoft.EntityFrameworkCore;
using PanoramicData.Mapper.Test.Models;

namespace PanoramicData.Mapper.Test;

/// <summary>
/// Projection of nested child collections and reference navigations (MS-24516).
/// </summary>
public partial class ProjectToTests
{
	[Fact]
	public void ProjectTo_NestedChildCollection_ICollection_ProjectsElements()
	{
		var config = new MapperConfiguration(cfg => cfg.AddProfile(new NestedCollectionProjectProfile()));

		using var context = CreateContext();
		SeedParentWithChildren(context);

		var projected = context.ProjParents
			.ProjectTo<ProjParentDto>(config)
			.ToList();

		projected.Should().ContainSingle();
		var parent = projected[0];
		parent.Id.Should().Be(1);
		parent.Name.Should().Be("Sub-A");
		parent.Children.Should().HaveCount(2);
		parent.Children.Should().ContainSingle(c => c.Sku == "ENT" && c.Seats == 5);
		parent.Children.Should().ContainSingle(c => c.Sku == "DEV" && c.Seats == 3);
	}

	[Fact]
	public void ProjectTo_NestedChildCollection_List_ProjectsElements()
	{
		var config = new MapperConfiguration(cfg => cfg.AddProfile(new NestedCollectionProjectProfile()));

		using var context = CreateContext();
		SeedParentWithChildren(context);

		var projected = context.ProjParents
			.ProjectTo<ProjParentListDto>(config)
			.ToList();

		projected.Should().ContainSingle();
		projected[0].Children.Should().HaveCount(2);
		projected[0].Children.Select(c => c.Sku).Should().BeEquivalentTo(["ENT", "DEV"]);
	}

	[Fact]
	public void ProjectTo_NestedChildCollection_Array_ProjectsElements()
	{
		var config = new MapperConfiguration(cfg => cfg.AddProfile(new NestedCollectionProjectProfile()));

		using var context = CreateContext();
		SeedParentWithChildren(context);

		var projected = context.ProjParents
			.ProjectTo<ProjParentArrayDto>(config)
			.ToList();

		projected.Should().ContainSingle();
		projected[0].Children.Should().HaveCount(2);
		projected[0].Children.Select(c => c.Seats).Should().BeEquivalentTo([5, 3]);
	}

	[Fact]
	public void ProjectTo_NestedChildCollection_IEnumerable_ProjectsElements()
	{
		var config = new MapperConfiguration(cfg => cfg.AddProfile(new NestedCollectionProjectProfile()));

		using var context = CreateContext();
		SeedParentWithChildren(context);

		var projected = context.ProjParents
			.ProjectTo<ProjParentEnumerableDto>(config)
			.ToList();

		projected.Should().ContainSingle();
		projected[0].Children.Should().HaveCount(2);
	}

	[Fact]
	public void ProjectTo_NestedChildCollection_Empty_ProjectsEmptyCollection()
	{
		var config = new MapperConfiguration(cfg => cfg.AddProfile(new NestedCollectionProjectProfile()));

		using var context = CreateContext();
		context.ProjParents.Add(new ProjParentEntity { Id = 9, Name = "No-children" });
		context.SaveChanges();

		var projected = context.ProjParents
			.ProjectTo<ProjParentDto>(config)
			.ToList();

		projected.Should().ContainSingle();
		projected[0].Children.Should().BeEmpty();
	}

	[Fact]
	public void ProjectTo_NestedReferenceNavigation_ProjectsNestedObject()
	{
		var config = new MapperConfiguration(cfg => cfg.AddProfile(new NestedReferenceProjectProfile()));

		using var context = CreateContext();
		context.ProjOrders.Add(new ProjOrderEntity
		{
			Id = 1,
			Customer = "Acme",
			Address = new ProjAddressEntity { Id = 1, Street = "1 High St", City = "Springfield" }
		});
		context.SaveChanges();

		var projected = context.ProjOrders
			.ProjectTo<ProjOrderDto>(config)
			.ToList();

		projected.Should().ContainSingle();
		projected[0].Customer.Should().Be("Acme");
		projected[0].Address.Should().NotBeNull();
		projected[0].Address!.Street.Should().Be("1 High St");
		projected[0].Address!.City.Should().Be("Springfield");
	}

	[Fact]
	public void ProjectTo_NestedReferenceNavigation_Null_ProjectsNull()
	{
		var config = new MapperConfiguration(cfg => cfg.AddProfile(new NestedReferenceProjectProfile()));

		using var context = CreateContext();
		context.ProjOrders.Add(new ProjOrderEntity { Id = 2, Customer = "No-address", Address = null });
		context.SaveChanges();

		var projected = context.ProjOrders
			.ProjectTo<ProjOrderDto>(config)
			.ToList();

		projected.Should().ContainSingle();
		projected[0].Address.Should().BeNull();
	}

	[Fact]
	public void ProjectTo_NestedChildCollection_MatchesInMemoryMap()
	{
		var config = new MapperConfiguration(cfg => cfg.AddProfile(new NestedCollectionProjectProfile()));
		var mapper = config.CreateMapper();

		using var context = CreateContext();
		SeedParentWithChildren(context);

		var entity = context.ProjParents.Include(p => p.Children).Single();
		var mapped = mapper.Map<ProjParentDto>(entity);
		var projected = context.ProjParents.ProjectTo<ProjParentDto>(config).Single();

		projected.Children.Select(c => (c.Sku, c.Seats))
			.Should().BeEquivalentTo(mapped.Children.Select(c => (c.Sku, c.Seats)));
	}
}
