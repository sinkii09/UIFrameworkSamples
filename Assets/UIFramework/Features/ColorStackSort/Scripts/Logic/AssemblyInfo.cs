using System.Runtime.CompilerServices;

// The scramble's internal helpers encode real rules whose failure modes are invisible from the
// public surface — mutation testing showed IsImmediateUndo could be gutted entirely with the whole
// suite still green. Exposing them to the test assembly is cheaper than widening the public API.
[assembly: InternalsVisibleTo("UIFramework.ColorStackSort.Tests")]
