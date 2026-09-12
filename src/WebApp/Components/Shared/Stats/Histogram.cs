namespace WebApp.Components.Shared.Stats;

/// <summary>
/// Whole numbers cut into equal bands, every band between the lowest and the highest included —
/// a gap in the middle of the range is itself a fact about how you play. The two distribution
/// charts on the statistics page each bucketed their own list with the same rule and each walked
/// the list once per band; one pass here, and the charts keep only what differs, which is the
/// band width.
/// </summary>
public static class Histogram
{
    /// <param name="bucketSize">
    /// How wide a band is. One band per value at 1, labelled with the value; wider bands are
    /// labelled as a range, "20–39". Bands start at multiples of the width, by floor division
    /// rather than C#'s truncation toward zero — a negative value (a heavy modifier on a defeat)
    /// divided the other way would land its band <em>above</em> itself, and the game it belongs
    /// to would be counted nowhere.
    /// </param>
    public static IReadOnlyList<ColumnChart.Bar> Of(IReadOnlyList<int> values, int bucketSize)
    {
        if (values.Count == 0) return [];

        var counts = values.GroupBy(v => Bucket(v, bucketSize)).ToDictionary(g => g.Key, g => g.Count());
        var first = Bucket(values.Min(), bucketSize);
        var last = Bucket(values.Max(), bucketSize);

        var bars = new List<ColumnChart.Bar>();
        for (var start = first; start <= last; start += bucketSize)
        {
            var label = bucketSize == 1 ? start.ToString() : $"{start}–{start + bucketSize - 1}";
            bars.Add(new ColumnChart.Bar(label, counts.GetValueOrDefault(start)));
        }
        return bars;
    }

    private static int Bucket(int value, int bucketSize) =>
        (int)Math.Floor(value / (double)bucketSize) * bucketSize;
}
