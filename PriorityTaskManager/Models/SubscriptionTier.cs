namespace PriorityTaskManager.Models
{
    /// <summary>
    /// An account's membership tier (see docs/VISION.md's Monetization section). There are exactly two
    /// tiers: <see cref="Free"/> and <see cref="Subscription"/>. Only <see cref="Subscription"/> can use
    /// online scheduling or cross-device sync; both tiers can use task/list/event CRUD and LLM-assisted
    /// intake, though intake has a lower usage quota on <see cref="Free"/>.
    /// </summary>
    public enum SubscriptionTier
    {
        Free,
        Subscription
    }
}
