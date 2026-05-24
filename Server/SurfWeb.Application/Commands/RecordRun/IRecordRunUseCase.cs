namespace SurfWeb.Application.Commands.RecordRun;

public interface IRecordRunUseCase
{
    Task<RecordRunResult> ExecuteAsync(RecordRunCommand command, CancellationToken ct = default);
}
