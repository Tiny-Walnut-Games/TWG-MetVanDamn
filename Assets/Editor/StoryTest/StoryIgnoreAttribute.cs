using System;

namespace TinyWalnutGames.StoryTest
    {
    /// <summary>
    /// Marks a symbol as intentionally excluded from Story Test enforcement.
    /// A reason is required to keep narrative accountability.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Struct)]
    public sealed class StoryIgnoreAttribute : Attribute
        {
        public string Reason { get; }
        public StoryIgnoreAttribute(string reason) => Reason = reason;
        public override string ToString() => $"StoryIgnore: {Reason}";
        }
    }
