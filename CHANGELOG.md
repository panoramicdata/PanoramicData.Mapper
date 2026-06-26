# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/), and this project adheres to [Semantic Versioning](https://semver.org/).

## [Unreleased]

## [10.0.37] - 2026-06-26

### Added

- `ProjectTo` now projects nested complex members and collections of complex elements (e.g. `ICollection<TChildModel>` -> `ICollection<TChildDto>`) element-by-element, bringing it to parity with the in-memory `Map` path. Previously such a member emitted a direct cast of the source navigation collection and threw at materialization (e.g. `InvalidCastException: Unable to cast object of type 'HashSet<TChildModel>' to type 'ICollection<TChildDto>'`), which could break an entire EF Core query
- `ExplicitExpansion()` member option plus a `ProjectTo<TDestination>(IConfigurationProvider, params Expression<Func<TDestination, object?>>[] membersToExpand)` overload: members marked with `ExplicitExpansion()` are excluded from projections unless explicitly requested via `membersToExpand` (e.g. `query.ProjectTo<Dto>(config, d => d.Children)`). Affects `ProjectTo` only, never the in-memory `Map` path
- Recursion guard for nested `ProjectTo`: self-referential maps stop at the first cycle, or expand to a configured `MaxDepth`

### Fixed

- `ProjectTo` no longer emits a reference cast that throws `InvalidCastException` for a destination member typed as an interface the source does not implement; an unmapped complex collection is left at its default value instead of breaking the query

### Notes

- Backward compatible: members that already projected (identical types, covariant `IEnumerable`, scalars, directly assignable collections) take the original code path unchanged - only members that previously threw or silently defaulted are now projected

## [10.0.29] - 2026-04-27

### Fixed

- `BuildMappingAssignment` now correctly maps interface-typed collection destination properties (`IList<T>`, `ICollection<T>`, `IEnumerable<T>`) when using `ForMember` with `MapFrom`. Previously .NET 10 threw `ArgumentException: Object of type 'List<TSource>' cannot be converted to type 'IList<TDest>'` from `RuntimePropertyInfo.SetValue`

## [10.0.28] - 2026-04-16

### Changed

- Removed redundant test step from CI workflow

## [10.0.27] - 2026-04-16

### Fixed

- Corrected changelog dates for versions 10.0.22-10.0.26

## [10.0.26] - 2026-04-16

### Added

- 44 additional unit tests covering gaps in null argument validation, runtime-typed mapping exceptions, self-mapping edge cases, collection mapping edge cases, `ConvertUsing` instance converter behavior, `ReverseMap` with member configurations, `ForPath` with 3-level deep nesting, `IncludeBase` + `Include` interaction, `AddTransform` for non-string types, `ConstructUsing` + `ForMember` combination, `BeforeMap`/`AfterMap` with map-to-existing, `ForAllMembers` ignore behavior, `MapperConfiguration` null action guard, duplicate profile registration, `ValueResolver` resolution context, and `ProjectTo` null argument guards

## [10.0.25] - 2026-04-16

### Fixed

- Cleaned up compiler errors, warnings, and code analysis messages across the solution

## [10.0.22] - 2026-04-07

### Changed

- NuGet governance remediation - added `Directory.Build.props`, `Directory.Packages.props`, `CONTRIBUTING.md`, `SECURITY.md`, Dependabot configuration, and updated CI workflow

## [10.0.20] - 2026-04-05

### Fixed

- `ProjectTo` now coalesces `Nullable<T>` source properties to `default(T)` when projecting to non-nullable destination properties (e.g. `double?` -> `double`), generating `COALESCE` in SQL instead of throwing `InvalidOperationException: Nullable object must have a value`

## [10.0.19] - 2026-03-31

### Added

- Implicit self-mapping (T -> T): property-by-property copy without requiring explicit `CreateMap<T, T>()`, including support for `required` properties and inherited members
- Implicit type conversions for convention-mapped properties:
  - Numeric widening/narrowing (int <-> long, int <-> double, decimal <-> double, etc.)
  - Enum <-> integral type mapping (int <-> enum, including nullable variants)
  - Enum <-> string mapping
  - Primitive <-> string mapping (int -> string, bool <-> string, etc.)
  - Nullable to non-nullable mapping (null defaults to `default(T)`)

### Fixed

- `ProjectTo` no longer throws "No coercion operator is defined between types" when projecting `Nullable<T>` properties (e.g. `double?`) to `string` destinations - uses `ToString()` in the expression tree instead of `Expression.Convert`
- `ProjectTo` gracefully handles other incompatible type pairs by falling back to `default(T)` instead of throwing `InvalidOperationException`
- `ConvertValue` no longer throws `FormatException`/`ArgumentException` when convention-matched string properties can't convert to numeric or enum destinations - returns `default(T)` instead
