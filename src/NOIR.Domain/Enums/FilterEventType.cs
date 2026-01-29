namespace NOIR.Domain.Enums;

/// <summary>
/// Types of filter analytics events tracked for usage analysis.
/// </summary>
public enum FilterEventType
{
    /// <summary>
    /// A filter was applied to the product list.
    /// </summary>
    FilterApplied,

    /// <summary>
    /// A filter was removed from the product list.
    /// </summary>
    FilterRemoved,

    /// <summary>
    /// All filters were cleared at once.
    /// </summary>
    FilterCleared,

    /// <summary>
    /// A search query was performed.
    /// </summary>
    SearchPerformed,

    /// <summary>
    /// Filter results were viewed by the user.
    /// </summary>
    ResultsViewed,

    /// <summary>
    /// A product was clicked from filtered results.
    /// </summary>
    ProductClicked
}
