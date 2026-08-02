using ColorStackSort.Logic;
using UnityEngine;

namespace ColorStackSort
{
    /// <summary>
    /// Where a level's recipe comes from. Normally <see cref="DifficultyCurve"/>; the fixed knobs
    /// below exist only as a debug override, for reproducing a board someone reported.
    /// </summary>
    [CreateAssetMenu(menuName = "ColorStackSort/Settings", fileName = "ColorStackSortSettings")]
    public sealed class ColorStackSortSettings : ScriptableObject
    {
        [Header("Level source")]
        [Tooltip("Off (normal play): every level's shape and seed come from DifficultyCurve.\n" +
                 "On (debug): every level uses the fixed recipe below and the same seed, so the " +
                 "board never changes. Use this to reproduce a reported board — not to tune " +
                 "difficulty, which lives in DifficultyCurve.")]
        [SerializeField] private bool _useFixedRecipe;

        [Tooltip("Salt mixed into every level's seed. Changing it reshuffles the whole game at " +
                 "once, which is how a bad board gets retired without touching the curve. When " +
                 "the fixed recipe above is on, this IS the seed.")]
        [SerializeField] private int _seed;

        [Header("Board shape (fixed recipe only)")]
        [Tooltip("Colours on the board. Each colour fills exactly one container when solved. " +
                 "The upper bound is legibility, not logic — the generator allows 256, but a row " +
                 "that wide will not fit a portrait phone.")]
        [Range(2, 12)]
        [SerializeField] private int _colorCount = 4;

        [Tooltip("Balls per container. Equals items-per-colour by construction — keep them equal " +
                 "or a colour could never be collected into one container.")]
        [Range(2, 8)]
        [SerializeField] private int _capacity = 4;

        [Tooltip("Spare empty containers. At least one, or every container starts full and " +
                 "nothing can ever move.")]
        [Range(1, 4)]
        [SerializeField] private int _emptyContainers = 2;

        [Tooltip("Length budget for the reverse-scramble walk. This is NOT a difficulty dial: a " +
                 "longer walk is a longer random walk, not a harder puzzle, and it can wander back " +
                 "toward easy boards. Difficulty comes from the shape above — more colours, deeper " +
                 "containers, fewer empties.")]
        [Min(1)]
        [SerializeField] private int _scrambleSteps = 60;

        /// <summary>Salt for the per-level seed, or the seed itself under the fixed recipe.</summary>
        public int BaseSeed => _seed;

        public bool UsesFixedRecipe => _useFixedRecipe;

        /// <summary>The board shape for a level — the curve's, unless the debug override is on.</summary>
        public LevelParams ParamsForLevel(int levelIndex) =>
            _useFixedRecipe ? ToLevelParams() : DifficultyCurve.ForLevel(levelIndex);

        /// <summary>
        /// The seed for a level. Under the fixed recipe this ignores the level entirely — that is
        /// the point: every level renders the one board being investigated.
        /// </summary>
        public int SeedForLevel(int levelIndex) =>
            _useFixedRecipe ? _seed : DifficultyCurve.SeedForLevel(levelIndex, _seed);

        /// <summary>
        /// Builds the fixed-recipe generator input. Throws on an invalid combination: the Range
        /// attributes only constrain the inspector, and a hand-edited .asset file bypasses them.
        /// </summary>
        public LevelParams ToLevelParams()
        {
            var parameters = new LevelParams(_colorCount, _capacity, _emptyContainers, _scrambleSteps);
            parameters.Validate();
            return parameters;
        }
    }
}
