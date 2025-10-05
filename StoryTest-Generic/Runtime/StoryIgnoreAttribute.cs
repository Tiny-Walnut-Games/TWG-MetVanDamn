#nullable enable
using System;

namespace StoryTest
    {
    /// <summary>
    /// Marks a symbol as intentionally excluded from Story Test enforcement.
    /// A reason is required to maintain narrative accountability and documentation.
    /// Use this attribute when code intentionally violates Story Test rules for valid reasons.
    /// </summary>
    /// <example>
    /// [StoryIgnore("Property is set by Unity's serialization system but never read directly")]
    /// public string SerializedField { get; set; }
    /// </example>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Enum)]
    public sealed class StoryIgnoreAttribute : Attribute
        {
        /// <summary>
        /// The reason this symbol is excluded from Story Test validation.
        /// </summary>
        public string Reason { get; }

        /// <summary>
        /// Creates a new StoryIgnore attribute with the specified reason.
        /// </summary>
        /// <param name="reason">A clear explanation of why this symbol should be ignored.</param>
        public StoryIgnoreAttribute(string reason)
            {
            Reason = reason ?? throw new ArgumentNullException(nameof(reason));
            }

        public override string ToString() => $"StoryIgnore: {Reason}";
        }
    }
