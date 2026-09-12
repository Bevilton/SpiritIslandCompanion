using ApexCharts;

namespace WebApp.Components.Shared.Stats;

/// <summary>
/// The chrome the statistics page's three column charts share — no toolbar, no data labels, the
/// app's typeface, parchment axis labels and a dashed ink grid.
/// <para>
/// Each chart used to build the whole options object for itself, some thirty-five lines of which
/// only the colours and one axis title actually differed. They had drifted where nothing was
/// holding them: a bar corner of 3 on two charts and 4 on the third, a column width of 55% on two
/// and 60% on the third, and x-axis labels a point smaller on one — three charts on one page, no
/// two of them quite the same weight. What legitimately varies is a parameter here; everything
/// else is settled once.
/// </para>
/// <para>
/// ApexCharts takes raw hex rather than CSS custom properties, so the colours come from
/// <see cref="StatsTheme"/> — see the note there.
/// </para>
/// </summary>
public static class ChartOptions
{
    /// <param name="height">The plot's height in pixels — the one thing the page sizes per chart.</param>
    /// <param name="colors">One per series, in series order.</param>
    /// <param name="xAxisTitle">What the x axis is counting, where the labels alone don't say.</param>
    /// <param name="stacked">Series stacked into one column rather than drawn side by side.</param>
    /// <param name="showLegend">
    /// Off by default: a single-series chart's legend only repeats its heading. The stacked chart
    /// turns it on, since there the two colours are the whole of what the columns are saying.
    /// </param>
    public static ApexChartOptions<TItem> Columns<TItem>(
        int height,
        List<string> colors,
        string? xAxisTitle = null,
        bool stacked = false,
        bool showLegend = false) where TItem : class =>
        new()
        {
            Chart = new Chart
            {
                Height = height,
                Stacked = stacked,
                Toolbar = new Toolbar { Show = false },
                FontFamily = StatsTheme.FontFamily,
            },
            Colors = colors,
            PlotOptions = new PlotOptions
            {
                Bar = new PlotOptionsBar { BorderRadius = 3, ColumnWidth = "55%" },
            },
            DataLabels = new DataLabels { Enabled = false },
            Legend = showLegend
                ? new Legend
                {
                    Position = LegendPosition.Top,
                    HorizontalAlign = Align.Right,
                    FontSize = "12px",
                }
                : new Legend { Show = false },
            // Whole games, never a fraction of one.
            Yaxis =
            [
                new YAxis
                {
                    DecimalsInFloat = 0,
                    Labels = new YAxisLabels { Style = new AxisLabelStyle { Colors = new Color(StatsTheme.Ink500) } },
                },
            ],
            Xaxis = new XAxis
            {
                AxisBorder = new AxisBorder { Show = false },
                AxisTicks = new AxisTicks { Show = false },
                Labels = new XAxisLabels
                {
                    Style = new AxisLabelStyle { Colors = new Color(StatsTheme.Ink500), FontSize = "11px" },
                },
                Title = xAxisTitle is null
                    ? null
                    : new AxisTitle
                    {
                        Text = xAxisTitle,
                        Style = new AxisTitleStyle { Color = StatsTheme.Ink500, FontSize = "11px", FontWeight = "500" },
                    },
            },
            Grid = new Grid { BorderColor = StatsTheme.Ink200, StrokeDashArray = 4 },
            Tooltip = new Tooltip { Theme = Mode.Light },
        };
}
