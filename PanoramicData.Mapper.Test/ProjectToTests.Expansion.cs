using Microsoft.EntityFrameworkCore;
using PanoramicData.Mapper.Test.Models;

namespace PanoramicData.Mapper.Test;

/// <summary>
/// ExplicitExpansion opt-out and membersToExpand opt-in (MS-24516), MaxDepth on
/// self-referential maps, and the guards that members which already projected correctly
/// still pass through unchanged.
/// </summary>
public partial class ProjectToTests
{
	[Fact]
	public void ProjectTo_ExplicitExpansion_ExcludesMemberByDefault()
	{
		var config = new MapperConfiguration(cfg => cfg.AddProfile(new ExplicitExpansionProfile()));

		using var context = CreateContext();
		SeedParentWithChildren(context);

		var projected = context.ProjParents
			.ProjectTo<ProjParentDto>(config)
			.ToList();

		projected.Should().ContainSingle();
		projected[0].Name.Should().Be("Sub-A");
		projected[0].Children.Should().BeEmpty("ExplicitExpansion excludes the member unless it is requested");
	}

	[Fact]
	public void ProjectTo_ExplicitExpansion_IncludesMemberWhenRequested()
	{
		var config = new MapperConfiguration(cfg => cfg.AddProfile(new ExplicitExpansionProfile()));

		using var context = CreateContext();
		SeedParentWithChildren(context);

		var projected = context.ProjParents
			.ProjectTo<ProjParentDto>(config, p => p.Children)
			.ToList();

		projected.Should().ContainSingle();
		projected[0].Children.Should().HaveCount(2);
		projected[0].Children.Should().ContainSingle(c => c.Sku == "ENT" && c.Seats == 5);
	}

	[Fact]
	public void Map_ExplicitExpansion_StillMapsMember()
	{
		// ExplicitExpansion only affects ProjectTo; the in-memory Map path must still populate it.
		var config = new MapperConfiguration(cfg => cfg.AddProfile(new ExplicitExpansionProfile()));
		var mapper = config.CreateMapper();

		var entity = new ProjParentEntity
		{
			Id = 1,
			Name = "Sub-A",
			Children = { new ProjChildEntity { Id = 1, Sku = "ENT", Seats = 5 } }
		};

		var dto = mapper.Map<ProjParentDto>(entity);

		dto.Children.Should().ContainSingle();
		dto.Children.First().Sku.Should().Be("ENT");
	}

	[Fact]
	public void ProjectTo_SelfReferential_NoMaxDepth_StopsAtCycle()
	{
		var config = new MapperConfiguration(cfg => cfg.AddProfile(new TreeProjectProfile()));

		using var context = CreateContext();
		SeedTree(context);

		var projected = context.ProjTrees
			.Where(t => t.Id == 1)
			.ProjectTo<ProjTreeDto>(config)
			.Single();

		projected.Name.Should().Be("Root");
		projected.Child.Should().NotBeNull();
		projected.Child!.Name.Should().Be("Child");
		projected.Child.Child.Should().BeNull("recursion stops at the first cycle when no MaxDepth is configured");
	}

	[Fact]
	public void ProjectTo_SelfReferential_WithMaxDepth_ExpandsToConfiguredDepth()
	{
		var config = new MapperConfiguration(cfg => cfg.AddProfile(new TreeMaxDepthProjectProfile()));

		using var context = CreateContext();
		SeedTree(context);

		var projected = context.ProjTrees
			.Where(t => t.Id == 1)
			.ProjectTo<ProjTreeDto>(config)
			.Single();

		projected.Name.Should().Be("Root");
		projected.Child!.Name.Should().Be("Child");
		projected.Child.Child!.Name.Should().Be("Grandchild");
		projected.Child.Child.Child.Should().BeNull("MaxDepth(2) expands two levels of children");
	}

	// --- Backward-compatibility guards: members that already worked must be unchanged ---

	[Fact]
	public void ProjectTo_SameTypeComplexMembers_NoElementMap_PassThroughUnchanged()
	{
		// Marker/Tags reuse the SAME complex type on source and destination with no element map, and
		// Notes is a collection of primitives. None of these need element-wise mapping; ProjectTo must
		// copy them straight through, exactly as it did before nested projection was added.
		var config = new MapperConfiguration(cfg => cfg.AddProfile(new PassThroughProfile()));

		var source = new List<PassThroughSource>
		{
			new()
			{
				Id = 1,
				Marker = new PassThroughTag { Label = "m" },
				Tags = { new PassThroughTag { Label = "vip" } },
				Notes = { "a", "b" }
			}
		}.AsQueryable();

		var projected = source.ProjectTo<PassThroughDest>(config).ToList();

		projected.Should().ContainSingle();
		projected[0].Id.Should().Be(1);
		projected[0].Marker.Should().NotBeNull();
		projected[0].Marker!.Label.Should().Be("m");
		projected[0].Tags.Should().ContainSingle();
		projected[0].Tags[0].Label.Should().Be("vip");
		projected[0].Notes.Should().BeEquivalentTo(["a", "b"]);
	}

	[Fact]
	public void AssertConfigurationIsValid_WithExplicitExpansion_DoesNotThrow()
	{
		var config = new MapperConfiguration(cfg => cfg.AddProfile(new ExplicitExpansionProfile()));

		var act = config.AssertConfigurationIsValid;

		act.Should().NotThrow();
	}
}
