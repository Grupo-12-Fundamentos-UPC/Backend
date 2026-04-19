namespace HairyPaws.Application.Common.CQRS;

public interface IQueryHandler<in TQuery, TResponse>
{
    Task<TResponse> Handle(TQuery query, CancellationToken cancellationToken);
}
