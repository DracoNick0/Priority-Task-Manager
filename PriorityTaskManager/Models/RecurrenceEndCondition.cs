using System.Text.Json.Serialization;

namespace PriorityTaskManager.Models
{
    /// <summary>
    /// Describes when a recurring series stops producing occurrences. Modeled as a polymorphic
    /// hierarchy (rather than nullable fields on one type) so each stored instance only serializes
    /// the field its own condition actually needs, and new condition kinds can be added additively.
    /// </summary>
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
    [JsonDerivedType(typeof(NeverEndCondition), typeDiscriminator: "never")]
    [JsonDerivedType(typeof(AfterOccurrencesEndCondition), typeDiscriminator: "afterOccurrences")]
    [JsonDerivedType(typeof(UntilDateEndCondition), typeDiscriminator: "untilDate")]
    public abstract class RecurrenceEndCondition
    {
        /// <summary>
        /// Creates a deep copy of this end condition.
        /// </summary>
        public abstract RecurrenceEndCondition Clone();
    }

    /// <summary>
    /// The series never ends on its own; occurrences are generated for whatever range a caller requests.
    /// </summary>
    public sealed class NeverEndCondition : RecurrenceEndCondition
    {
        /// <inheritdoc />
        public override RecurrenceEndCondition Clone() => new NeverEndCondition();
    }

    /// <summary>
    /// The series ends after a fixed number of occurrences have been generated, counting from the series start date.
    /// </summary>
    public sealed class AfterOccurrencesEndCondition : RecurrenceEndCondition
    {
        /// <summary>
        /// Gets or sets the total number of occurrences the series produces before ending.
        /// </summary>
        public int OccurrenceCount { get; set; }

        /// <inheritdoc />
        public override RecurrenceEndCondition Clone() => new AfterOccurrencesEndCondition { OccurrenceCount = OccurrenceCount };
    }

    /// <summary>
    /// The series ends after a fixed date; no occurrence dated later than <see cref="UntilDate"/> is generated.
    /// </summary>
    public sealed class UntilDateEndCondition : RecurrenceEndCondition
    {
        /// <summary>
        /// Gets or sets the last date (inclusive) an occurrence is allowed to fall on.
        /// </summary>
        public DateTime UntilDate { get; set; }

        /// <inheritdoc />
        public override RecurrenceEndCondition Clone() => new UntilDateEndCondition { UntilDate = UntilDate };
    }
}
