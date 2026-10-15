namespace Application.Features.Games;

/// <summary>
/// Who a seat names, from the reader's side of the table: their own account, a friend's, one
/// of their local players, or nobody. One vocabulary for every reader that lists seats — the
/// game list, the statistics page — so a badge means the same thing wherever it is drawn.
/// </summary>
public enum PlayerKind
{
    Me,
    Friend,
    Local,
    Unassigned,
}
