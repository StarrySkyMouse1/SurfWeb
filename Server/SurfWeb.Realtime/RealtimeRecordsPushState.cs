namespace SurfWeb.Realtime;

public sealed class RealtimeRecordsPushState
{
    private string _revision = DateTimeOffset.UtcNow.ToString("O");

    public string Revision => _revision;

    public void SetRevision(DateTimeOffset utc) =>
        _revision = utc.ToString("O");
}
