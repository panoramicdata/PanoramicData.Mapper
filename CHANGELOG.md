# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/), and this project adheres to [Semantic Versioning](https://semver.org/).

## [Unreleased]

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
