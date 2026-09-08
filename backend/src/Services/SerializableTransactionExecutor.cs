using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RetailSystem.Api.Data;

namespace RetailSystem.Api.Services;

/// <summary>
/// Runs a complete SERIALIZABLE operation with a fresh DbContext for every
/// attempt. Oracle can raise ORA-08177 for a genuine concurrent update and,
/// when deferred segment creation is enabled, for the first insert into an
/// empty table. Retrying the whole transaction is safe because no response is
/// returned until the transaction commits.
/// </summary>
public sealed class SerializableTransactionExecutor(
    DbContextOptions<AppDbContext> options,
    ILogger<SerializableTransactionExecutor> logger)
{
    private const int MaxAttempts = 3;

    public async Task<T> ExecuteAsync<T>(
        Func<AppDbContext, CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            await using var db = new AppDbContext(options);
            IDbContextTransaction? transaction = null;

            try
            {
                if (db.Database.IsRelational())
                {
                    transaction = await db.Database.BeginTransactionAsync(
                        IsolationLevel.Serializable,
                        cancellationToken);
                }

                var result = await operation(db, cancellationToken);

                if (transaction is not null)
                    await transaction.CommitAsync(cancellationToken);

                return result;
            }
            catch (Exception ex) when (
                attempt < MaxAttempts
                && OracleDatabaseErrors.IsSerializationConflict(ex))
            {
                if (transaction is not null)
                {
                    try
                    {
                        await transaction.RollbackAsync(cancellationToken);
                    }
                    catch (Exception rollbackException)
                    {
                        logger.LogWarning(
                            rollbackException,
                            "Rollback failed after Oracle serialization conflict on attempt {Attempt}",
                            attempt);
                    }
                }

                var delay = TimeSpan.FromMilliseconds(
                    40 * attempt + Random.Shared.Next(10, 40));

                logger.LogWarning(
                    ex,
                    "Oracle serialization conflict on attempt {Attempt}/{MaxAttempts}; retrying after {DelayMs} ms",
                    attempt,
                    MaxAttempts,
                    delay.TotalMilliseconds);

                await Task.Delay(delay, cancellationToken);
            }
            finally
            {
                if (transaction is not null)
                    await transaction.DisposeAsync();
            }
        }

        throw new InvalidOperationException("Serializable transaction retry loop exited unexpectedly");
    }
}
