using SurfWeb.Domain.Common;
using SurfWeb.Domain.ValueObjects;

namespace SurfWeb.Domain.Aggregates.Players;

public sealed class Player : AggregateRoot<PlayerId>
{
    private readonly Dictionary<StyleId, HashSet<MapName>> _completedMapsByStyle = [];

    private Player(PlayerId id, string displayName) : base(id)
    {
        DisplayName = displayName;
    }

    public string DisplayName { get; private set; }

    public IReadOnlyDictionary<StyleId, IReadOnlyCollection<MapName>> CompletedMapsByStyle =>
        _completedMapsByStyle.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyCollection<MapName>)pair.Value.ToArray());

    public static Player Create(PlayerId id, string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Display name is required.", nameof(displayName));
        }

        return new Player(id, displayName.Trim());
    }

    public void RegisterCompletion(MapName mapName, StyleId styleId)
    {
        if (!_completedMapsByStyle.TryGetValue(styleId, out var completedMaps))
        {
            completedMaps = [];
            _completedMapsByStyle[styleId] = completedMaps;
        }

        completedMaps.Add(mapName);
    }

    public int CompletionCountFor(StyleId styleId) =>
        _completedMapsByStyle.TryGetValue(styleId, out var completedMaps)
            ? completedMaps.Count
            : 0;
}
