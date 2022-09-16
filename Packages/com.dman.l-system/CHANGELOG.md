# Changelog

## [0.9.0] - 2022-09-13

### Added

- new native support for Stems as a new turtle operation. will generate a spline-like bonded cylinder mesh for stems when used, with mapping onto a tileable uv texture

### Breaking changes

- changed "VolumetricScale" parameter on Turtle mesh operations to "ScalePower" -- set this to 3 to mimic the cube root effect when VolumetricScale was set. otherwise 1. this new feature allows us to more closely control the scaling curve of individual organs

## [0.2.1] - 2021-03-27

### Added

- new "@" thickness control operator, which effects mesh's labeled to be effected by thickness

## [0.2] - 2021-03-07

### Breaking changes

- Stochastic rule grammar changed
- Multi-symbol match rules are no longer supported, those rules must be refactored as context-sensitive rules

### Added

- Support for L1/L2 context-sensitive systems

## [0.1.12] - 2021-02-27

### Added

- removal of unityeditor references

## [0.1.11] - 2021-02-27

### Added

- unity mathematics dependency
- formal l-system state immutability. everything is already copied, only exception was the Random

## [0.1.8] - 2021-02-27

### Added

- non-alpha characters in match rules
- parameter representation object allows for checking if param by name exists

## [0.1.7] - 2021-02-07

### Added

- Missing .meta file

## [0.1.6] - 2021-02-07

Changes to make it easier to use the l-system object as a standalone tool

### Added

- Parameter representation object to make it easier to interact with the runtime parameter array
- different L-system generation parameters

## [0.1.5] - 2021-02-06

L-system language file, developer tools, more examples

### Added

- L-system language spec, and importer for `.lsystem` files
- Turtle bend operation based on world-space vector. can be used to simulate bending due to gravity
- live-reloading with developer tools while running inside unity editor
- fixed memory leak

## [0.1.4] - 2021-01-31

Turtle scale operation

### Added

- Option to tell the turtle to scale the mesh to add based on an input parameter

## [0.1.3] - 2021-01-31

Stochastic probability parameterization

### Added

- compile time replacement directives
- expression compilation inside the probability declaration

## [0.1.2] - 2021-01-30

Parametric expression evaluator additions

### Added

- support for basic C# operations in the parameter expression evaluator

## [0.1.1] - 2021-01-24

Genericized the turtle interpreter to allow for easier extension

### Added

- Generic Turtle Interpreter which should allow for custom turtle state extensions

## [0.1.0] - 2021-01-24

Initial release.

### Added

- l-system framework
