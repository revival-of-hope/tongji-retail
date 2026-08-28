namespace RetailSystem.Api.Services;

public static class OracleDatabaseErrors
{
    public const string InvalidIdentifierCode = "ORA-00904";
    public const string SerializationConflictCode = "ORA-08177";
    public const string UniqueConstraintCode = "ORA-00001";
    public const string CheckConstraintCode = "ORA-02290";
    public const string MissingParentCode = "ORA-02291";
    public const string ReferencedChildCode = "ORA-02292";

    public static bool IsInvalidIdentifier(Exception exception) =>
        ContainsCode(exception, InvalidIdentifierCode);

    public static bool IsSerializationConflict(Exception exception) =>
        ContainsCode(exception, SerializationConflictCode);

    public static bool IsUniqueConstraintConflict(Exception exception) =>
        ContainsCode(exception, UniqueConstraintCode);

    public static bool IsConstraintConflict(Exception exception) =>
        ContainsCode(exception, UniqueConstraintCode)
        || ContainsCode(exception, CheckConstraintCode)
        || ContainsCode(exception, MissingParentCode)
        || ContainsCode(exception, ReferencedChildCode);

    public static bool ContainsCode(Exception exception, string errorCode)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);

        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current.Message.Contains(errorCode, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
