using Microsoft.EntityFrameworkCore;
using PanoramicData.Mapper.Test.Models;

namespace PanoramicData.Mapper.Test;

public class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
{
	public DbSet<SimpleSource> Sources { get; set; } = null!;
	public DbSet<PersonSource> Persons { get; set; } = null!;
	public DbSet<NullableDoubleEntity> NullableDoubles { get; set; } = null!;
	public DbSet<NullablePortEntity> NullablePorts { get; set; } = null!;
	public DbSet<ProjParentEntity> ProjParents { get; set; } = null!;
	public DbSet<ProjChildEntity> ProjChildren { get; set; } = null!;
	public DbSet<ProjOrderEntity> ProjOrders { get; set; } = null!;
	public DbSet<ProjAddressEntity> ProjAddresses { get; set; } = null!;
	public DbSet<ProjTreeEntity> ProjTrees { get; set; } = null!;

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<SimpleSource>().HasKey(e => e.Id);
		modelBuilder.Entity<PersonSource>().HasKey(e => e.FirstName);
		modelBuilder.Entity<NullableDoubleEntity>().HasKey(e => e.Id);
		modelBuilder.Entity<NullablePortEntity>().HasKey(e => e.Id);
		modelBuilder.Entity<ProjParentEntity>().HasKey(e => e.Id);
		modelBuilder.Entity<ProjChildEntity>().HasKey(e => e.Id);
		modelBuilder.Entity<ProjParentEntity>()
			.HasMany(e => e.Children)
			.WithOne()
			.HasForeignKey(e => e.ProjParentEntityId);
		modelBuilder.Entity<ProjAddressEntity>().HasKey(e => e.Id);
		modelBuilder.Entity<ProjOrderEntity>().HasKey(e => e.Id);
		modelBuilder.Entity<ProjOrderEntity>()
			.HasOne(e => e.Address)
			.WithMany()
			.HasForeignKey(e => e.AddressId);
		modelBuilder.Entity<ProjTreeEntity>().HasKey(e => e.Id);
		modelBuilder.Entity<ProjTreeEntity>()
			.HasOne(e => e.Child)
			.WithMany()
			.HasForeignKey(e => e.ChildId);
	}
}
