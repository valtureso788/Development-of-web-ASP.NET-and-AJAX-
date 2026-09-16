namespace ASP_pystoy.Services;

public interface IOperation
{
    Guid OperationId { get; }
    DateTime CreatedAt { get; }
}

public interface IOperationTransient : IOperation { }
public interface IOperationScoped : IOperation { }
public interface IOperationSingleton : IOperation { }

public class Operation : IOperationTransient, IOperationScoped, IOperationSingleton
{
    public Guid OperationId { get; }
    public DateTime CreatedAt { get; }

    public Operation()
    {
        OperationId = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }
}

public interface IOperationService
{
    IOperationTransient TransientOperation { get; }
    IOperationScoped ScopedOperation { get; }
    IOperationSingleton SingletonOperation { get; }
}

public class OperationService : IOperationService
{
    public IOperationTransient TransientOperation { get; }
    public IOperationScoped ScopedOperation { get; }
    public IOperationSingleton SingletonOperation { get; }

    public OperationService(
        IOperationTransient transientOperation,
        IOperationScoped scopedOperation,
        IOperationSingleton singletonOperation)
    {
        TransientOperation = transientOperation;
        ScopedOperation = scopedOperation;
        SingletonOperation = singletonOperation;
    }
}
