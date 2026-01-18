// FathomOS.Core/Data/SqliteCertificateRepository.cs
// SQLite repository implementation for Certificate storage
// Implements ISyncableRepository<Certificate> for offline-first sync support

using Microsoft.Data.Sqlite;
using System.Text.Json;
using FathomOS.Core.Interfaces;
using FathomOS.Core.Models;
using FathomOS.Core.Logging;

namespace FathomOS.Core.Data;

/// <summary>
/// SQLite implementation of ICertificateRepository.
/// Provides full CRUD operations with offline-first sync support.
/// Includes retry logic for transient database errors.
/// </summary>
public class SqliteCertificateRepository : ICertificateRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;
    private readonly ILogger? _logger;
    private readonly RetryPolicy _retryPolicy;

    /// <summary>
    /// Creates a new certificate repository
    /// </summary>
    /// <param name="connectionFactory">Connection factory for database access</param>
    public SqliteCertificateRepository(SqliteConnectionFactory connectionFactory)
        : this(connectionFactory, null, null)
    {
    }

    /// <summary>
    /// Creates a new certificate repository with logging and retry support
    /// </summary>
    /// <param name="connectionFactory">Connection factory for database access</param>
    /// <param name="logger">Optional logger for retry and error logging</param>
    /// <param name="retryPolicy">Optional retry policy. Uses default if null.</param>
    public SqliteCertificateRepository(
        SqliteConnectionFactory connectionFactory,
        ILogger? logger,
        RetryPolicy? retryPolicy = null)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _logger = logger;
        _retryPolicy = retryPolicy ?? RetryPolicy.Default;
    }

    #region IRepository<Certificate> Implementation

    /// <inheritdoc/>
    public async Task<Certificate?> GetByIdAsync(string id)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync();
        await using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT * FROM Certificates
            WHERE CertificateId = @id;";
        command.Parameters.AddWithValue("@id", id);

        await using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return MapFromReader(reader);
        }

        return null;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Certificate>> GetAllAsync()
    {
        var certificates = new List<Certificate>();

        await using var connection = await _connectionFactory.CreateConnectionAsync();
        await using var command = connection.CreateCommand();

        command.CommandText = "SELECT * FROM Certificates ORDER BY IssuedAt DESC;";

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            certificates.Add(MapFromReader(reader));
        }

        return certificates;
    }

    /// <inheritdoc/>
    public async Task<Certificate> AddAsync(Certificate entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        entity.CreatedAt = DateTime.UtcNow;
        entity.SyncStatus = "pending";

        await DatabaseRetryHelper.ExecuteWithRetryAsync(
            async () =>
            {
                await using var connection = await _connectionFactory.CreateConnectionAsync();
                await using var transaction = connection.BeginTransaction();

                try
                {
                    await InsertCertificateAsync(connection, transaction, entity);
                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            },
            _retryPolicy,
            _logger,
            $"AddAsync certificate {entity.CertificateId}");

        return entity;
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(Certificate entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        entity.UpdatedAt = DateTime.UtcNow;

        await DatabaseRetryHelper.ExecuteWithRetryAsync(
            async () =>
            {
                await using var connection = await _connectionFactory.CreateConnectionAsync();
                await using var transaction = connection.BeginTransaction();

                try
                {
                    await UpdateCertificateAsync(connection, transaction, entity);
                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            },
            _retryPolicy,
            _logger,
            $"UpdateAsync certificate {entity.CertificateId}");
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(string id)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync();
        await using var transaction = connection.BeginTransaction();

        try
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "DELETE FROM Certificates WHERE CertificateId = @id;";
            command.Parameters.AddWithValue("@id", id);

            await command.ExecuteNonQueryAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsAsync(string id)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync();
        await using var command = connection.CreateCommand();

        command.CommandText = "SELECT COUNT(1) FROM Certificates WHERE CertificateId = @id;";
        command.Parameters.AddWithValue("@id", id);

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result) > 0;
    }

    /// <inheritdoc/>
    public async Task<int> CountAsync()
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync();
        await using var command = connection.CreateCommand();

        command.CommandText = "SELECT COUNT(*) FROM Certificates;";

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    #endregion

    #region IBatchRepository<Certificate> Implementation

    /// <inheritdoc/>
    public async Task<IEnumerable<Certificate>> AddRangeAsync(IEnumerable<Certificate> entities)
    {
        var entityList = entities?.ToList() ?? throw new ArgumentNullException(nameof(entities));
        if (!entityList.Any())
            return entityList;

        var now = DateTime.UtcNow;
        foreach (var entity in entityList)
        {
            entity.CreatedAt = now;
            entity.SyncStatus = "pending";
        }

        await _connectionFactory.ExecuteInTransactionAsync(async (connection, transaction) =>
        {
            foreach (var entity in entityList)
            {
                await InsertCertificateAsync(connection, transaction, entity);
            }
        });

        return entityList;
    }

    /// <inheritdoc/>
    public async Task UpdateRangeAsync(IEnumerable<Certificate> entities)
    {
        var entityList = entities?.ToList() ?? throw new ArgumentNullException(nameof(entities));
        if (!entityList.Any())
            return;

        var now = DateTime.UtcNow;
        foreach (var entity in entityList)
        {
            entity.UpdatedAt = now;
        }

        await _connectionFactory.ExecuteInTransactionAsync(async (connection, transaction) =>
        {
            foreach (var entity in entityList)
            {
                await UpdateCertificateAsync(connection, transaction, entity);
            }
        });
    }

    /// <inheritdoc/>
    public async Task DeleteRangeAsync(IEnumerable<string> ids)
    {
        var idList = ids?.ToList() ?? throw new ArgumentNullException(nameof(ids));
        if (!idList.Any())
            return;

        await _connectionFactory.ExecuteInTransactionAsync(async (connection, transaction) =>
        {
            foreach (var id in idList)
            {
                await using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = "DELETE FROM Certificates WHERE CertificateId = @id;";
                command.Parameters.AddWithValue("@id", id);
                await command.ExecuteNonQueryAsync();
            }
        });
    }

    #endregion

    #region ISyncableRepository<Certificate> Implementation

    /// <inheritdoc/>
    public async Task<IEnumerable<Certificate>> GetPendingSyncAsync()
    {
        var certificates = new List<Certificate>();

        await using var connection = await _connectionFactory.CreateConnectionAsync();
        await using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT * FROM Certificates
            WHERE SyncStatus = 'pending'
            ORDER BY CreatedAt ASC;";

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            certificates.Add(MapFromReader(reader));
        }

        return certificates;
    }

    /// <inheritdoc/>
    public async Task MarkSyncedAsync(string id, DateTime? syncedAt = null)
    {
        var syncTime = syncedAt ?? DateTime.UtcNow;

        await using var connection = await _connectionFactory.CreateConnectionAsync();
        await using var transaction = connection.BeginTransaction();

        try
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            // BUG FIX: Reset SyncAttempts to 0 when marking as synced
            command.CommandText = @"
                UPDATE Certificates
                SET SyncStatus = 'synced',
                    SyncedAt = @syncedAt,
                    SyncError = NULL,
                    SyncAttempts = 0,
                    UpdatedAt = @updatedAt
                WHERE CertificateId = @id;";
            command.Parameters.AddWithValue("@id", id);
            command.Parameters.AddWithValue("@syncedAt", syncTime.ToString("O"));
            command.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow.ToString("O"));

            await command.ExecuteNonQueryAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task MarkSyncedRangeAsync(IEnumerable<string> ids, DateTime? syncedAt = null)
    {
        var idList = ids?.ToList() ?? throw new ArgumentNullException(nameof(ids));
        if (!idList.Any())
            return;

        var syncTime = syncedAt ?? DateTime.UtcNow;
        var updatedAt = DateTime.UtcNow;

        await _connectionFactory.ExecuteInTransactionAsync(async (connection, transaction) =>
        {
            foreach (var id in idList)
            {
                await using var command = connection.CreateCommand();
                command.Transaction = transaction;
                // BUG FIX: Reset SyncAttempts to 0 when marking as synced
                command.CommandText = @"
                    UPDATE Certificates
                    SET SyncStatus = 'synced',
                        SyncedAt = @syncedAt,
                        SyncError = NULL,
                        SyncAttempts = 0,
                        UpdatedAt = @updatedAt
                    WHERE CertificateId = @id;";
                command.Parameters.AddWithValue("@id", id);
                command.Parameters.AddWithValue("@syncedAt", syncTime.ToString("O"));
                command.Parameters.AddWithValue("@updatedAt", updatedAt.ToString("O"));

                await command.ExecuteNonQueryAsync();
            }
        });
    }

    /// <inheritdoc/>
    public async Task MarkSyncFailedAsync(string id, string? errorMessage = null)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync();
        await using var transaction = connection.BeginTransaction();

        try
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                UPDATE Certificates
                SET SyncStatus = 'failed',
                    SyncError = @error,
                    SyncAttempts = SyncAttempts + 1,
                    UpdatedAt = @updatedAt
                WHERE CertificateId = @id;";
            command.Parameters.AddWithValue("@id", id);
            command.Parameters.AddWithValue("@error", (object?)errorMessage ?? DBNull.Value);
            command.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow.ToString("O"));

            await command.ExecuteNonQueryAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<int> GetPendingSyncCountAsync()
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync();
        await using var command = connection.CreateCommand();

        command.CommandText = "SELECT COUNT(*) FROM Certificates WHERE SyncStatus = 'pending';";

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    #endregion

    #region ICertificateRepository Implementation

    /// <inheritdoc/>
    public async Task<IEnumerable<Certificate>> GetByLicenseeCodeAsync(string licenseeCode)
    {
        if (string.IsNullOrEmpty(licenseeCode))
            throw new ArgumentNullException(nameof(licenseeCode));

        var certificates = new List<Certificate>();

        await using var connection = await _connectionFactory.CreateConnectionAsync();
        await using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT * FROM Certificates
            WHERE LicenseeCode = @licenseeCode
            ORDER BY IssuedAt DESC;";
        command.Parameters.AddWithValue("@licenseeCode", licenseeCode);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            certificates.Add(MapFromReader(reader));
        }

        return certificates;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Certificate>> GetByModuleIdAsync(string moduleId)
    {
        if (string.IsNullOrEmpty(moduleId))
            throw new ArgumentNullException(nameof(moduleId));

        var certificates = new List<Certificate>();

        await using var connection = await _connectionFactory.CreateConnectionAsync();
        await using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT * FROM Certificates
            WHERE ModuleId = @moduleId
            ORDER BY IssuedAt DESC;";
        command.Parameters.AddWithValue("@moduleId", moduleId);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            certificates.Add(MapFromReader(reader));
        }

        return certificates;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Certificate>> GetByDateRangeAsync(DateTime start, DateTime end)
    {
        // Ensure dates are in UTC
        var startUtc = start.Kind == DateTimeKind.Utc ? start : start.ToUniversalTime();
        var endUtc = end.Kind == DateTimeKind.Utc ? end : end.ToUniversalTime();

        var certificates = new List<Certificate>();

        await using var connection = await _connectionFactory.CreateConnectionAsync();
        await using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT * FROM Certificates
            WHERE IssuedAt >= @start AND IssuedAt <= @end
            ORDER BY IssuedAt DESC;";
        command.Parameters.AddWithValue("@start", startUtc.ToString("O"));
        command.Parameters.AddWithValue("@end", endUtc.ToString("O"));

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            certificates.Add(MapFromReader(reader));
        }

        return certificates;
    }

    /// <inheritdoc/>
    public async Task CacheAsync(Certificate certificate)
    {
        if (certificate == null)
            throw new ArgumentNullException(nameof(certificate));

        var exists = await ExistsAsync(certificate.CertificateId);

        if (exists)
        {
            certificate.UpdatedAt = DateTime.UtcNow;
            await UpdateAsync(certificate);
        }
        else
        {
            certificate.CreatedAt = DateTime.UtcNow;
            await AddAsync(certificate);
        }
    }

    /// <inheritdoc/>
    public async Task<CertificateSyncStatistics> GetSyncStatisticsAsync()
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync();

        int totalCount = 0, pendingCount = 0, syncedCount = 0, failedCount = 0;
        DateTime? lastSyncAt = null;

        // Get counts
        await using (var countCommand = connection.CreateCommand())
        {
            countCommand.CommandText = @"
                SELECT
                    COUNT(*) as TotalCount,
                    SUM(CASE WHEN SyncStatus = 'pending' THEN 1 ELSE 0 END) as PendingCount,
                    SUM(CASE WHEN SyncStatus = 'synced' THEN 1 ELSE 0 END) as SyncedCount,
                    SUM(CASE WHEN SyncStatus = 'failed' THEN 1 ELSE 0 END) as FailedCount
                FROM Certificates;";

            await using var reader = await countCommand.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                totalCount = reader.GetInt32(0);
                pendingCount = reader.GetInt32(1);
                syncedCount = reader.GetInt32(2);
                failedCount = reader.GetInt32(3);
            }
        }

        // Get last synced time
        await using (var lastSyncCommand = connection.CreateCommand())
        {
            lastSyncCommand.CommandText = @"
                SELECT MAX(SyncedAt) FROM Certificates
                WHERE SyncStatus = 'synced' AND SyncedAt IS NOT NULL;";

            var result = await lastSyncCommand.ExecuteScalarAsync();
            if (result != null && result != DBNull.Value)
            {
                lastSyncAt = DateTime.Parse((string)result, null,
                    System.Globalization.DateTimeStyles.RoundtripKind);
            }
        }

        return new CertificateSyncStatistics
        {
            TotalCount = totalCount,
            PendingCount = pendingCount,
            SyncedCount = syncedCount,
            FailedCount = failedCount,
            LastSyncAt = lastSyncAt
        };
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Certificate>> GetByLicenseIdAsync(string licenseId)
    {
        if (string.IsNullOrEmpty(licenseId))
            throw new ArgumentNullException(nameof(licenseId));

        var certificates = new List<Certificate>();

        await using var connection = await _connectionFactory.CreateConnectionAsync();
        await using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT * FROM Certificates
            WHERE LicenseId = @licenseId
            ORDER BY IssuedAt DESC;";
        command.Parameters.AddWithValue("@licenseId", licenseId);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            certificates.Add(MapFromReader(reader));
        }

        return certificates;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Certificate>> GetByProjectNameAsync(string projectName)
    {
        if (string.IsNullOrEmpty(projectName))
            throw new ArgumentNullException(nameof(projectName));

        var certificates = new List<Certificate>();

        await using var connection = await _connectionFactory.CreateConnectionAsync();
        await using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT * FROM Certificates
            WHERE ProjectName = @projectName
            ORDER BY IssuedAt DESC;";
        command.Parameters.AddWithValue("@projectName", projectName);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            certificates.Add(MapFromReader(reader));
        }

        return certificates;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Certificate>> GetFailedSyncAsync()
    {
        var certificates = new List<Certificate>();

        await using var connection = await _connectionFactory.CreateConnectionAsync();
        await using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT * FROM Certificates
            WHERE SyncStatus = 'failed'
            ORDER BY SyncAttempts ASC;";

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            certificates.Add(MapFromReader(reader));
        }

        return certificates;
    }

    /// <inheritdoc/>
    public async Task IncrementSyncAttemptsAsync(string certificateId)
    {
        if (string.IsNullOrEmpty(certificateId))
            throw new ArgumentNullException(nameof(certificateId));

        await using var connection = await _connectionFactory.CreateConnectionAsync();
        await using var transaction = connection.BeginTransaction();

        try
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                UPDATE Certificates
                SET SyncAttempts = SyncAttempts + 1,
                    UpdatedAt = @updatedAt
                WHERE CertificateId = @id;";
            command.Parameters.AddWithValue("@id", certificateId);
            command.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow.ToString("O"));

            await command.ExecuteNonQueryAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task ResetSyncStatusAsync(string certificateId)
    {
        if (string.IsNullOrEmpty(certificateId))
            throw new ArgumentNullException(nameof(certificateId));

        await using var connection = await _connectionFactory.CreateConnectionAsync();
        await using var transaction = connection.BeginTransaction();

        try
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                UPDATE Certificates
                SET SyncStatus = 'pending',
                    SyncError = NULL,
                    SyncAttempts = 0,
                    UpdatedAt = @updatedAt
                WHERE CertificateId = @id;";
            command.Parameters.AddWithValue("@id", certificateId);
            command.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow.ToString("O"));

            await command.ExecuteNonQueryAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Certificate>> GetRetryableCertificatesAsync(int maxAttempts)
    {
        var certificates = new List<Certificate>();

        await using var connection = await _connectionFactory.CreateConnectionAsync();
        await using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT * FROM Certificates
            WHERE SyncStatus = 'failed' AND SyncAttempts < @maxAttempts
            ORDER BY SyncAttempts ASC, CreatedAt ASC;";
        command.Parameters.AddWithValue("@maxAttempts", maxAttempts);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            certificates.Add(MapFromReader(reader));
        }

        return certificates;
    }

    /// <inheritdoc/>
    public async Task UpdateSyncErrorAsync(string certificateId, string errorMessage)
    {
        if (string.IsNullOrEmpty(certificateId))
            throw new ArgumentNullException(nameof(certificateId));

        await using var connection = await _connectionFactory.CreateConnectionAsync();
        await using var transaction = connection.BeginTransaction();

        try
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                UPDATE Certificates
                SET SyncError = @error,
                    UpdatedAt = @updatedAt
                WHERE CertificateId = @id;";
            command.Parameters.AddWithValue("@id", certificateId);
            command.Parameters.AddWithValue("@error", (object?)errorMessage ?? DBNull.Value);
            command.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow.ToString("O"));

            await command.ExecuteNonQueryAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<int> GetSyncAttemptsAsync(string certificateId)
    {
        if (string.IsNullOrEmpty(certificateId))
            throw new ArgumentNullException(nameof(certificateId));

        await using var connection = await _connectionFactory.CreateConnectionAsync();
        await using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT SyncAttempts FROM Certificates
            WHERE CertificateId = @id;";
        command.Parameters.AddWithValue("@id", certificateId);

        var result = await command.ExecuteScalarAsync();
        return result != null && result != DBNull.Value ? Convert.ToInt32(result) : 0;
    }

    #endregion

    #region Private Helper Methods

    private async Task InsertCertificateAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Certificate entity)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;

        command.CommandText = @"
            INSERT INTO Certificates (
                CertificateId, LicenseId, LicenseeCode, ModuleId, ModuleCertificateCode,
                ModuleVersion, IssuedAt, ProjectName, ProjectLocation, Vessel, Client,
                SignatoryName, SignatoryTitle, CompanyName, ProcessingDataJson,
                InputFilesJson, OutputFilesJson, Signature, SignatureAlgorithm, DataHash,
                SyncStatus, SyncedAt, SyncError, SyncAttempts, CreatedAt, UpdatedAt,
                QrCodeUrl, EditionName
            ) VALUES (
                @certificateId, @licenseId, @licenseeCode, @moduleId, @moduleCertificateCode,
                @moduleVersion, @issuedAt, @projectName, @projectLocation, @vessel, @client,
                @signatoryName, @signatoryTitle, @companyName, @processingDataJson,
                @inputFilesJson, @outputFilesJson, @signature, @signatureAlgorithm, @dataHash,
                @syncStatus, @syncedAt, @syncError, @syncAttempts, @createdAt, @updatedAt,
                @qrCodeUrl, @editionName
            );";

        AddCertificateParameters(command, entity);

        await command.ExecuteNonQueryAsync();
    }

    private async Task UpdateCertificateAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Certificate entity)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;

        command.CommandText = @"
            UPDATE Certificates SET
                LicenseId = @licenseId,
                LicenseeCode = @licenseeCode,
                ModuleId = @moduleId,
                ModuleCertificateCode = @moduleCertificateCode,
                ModuleVersion = @moduleVersion,
                IssuedAt = @issuedAt,
                ProjectName = @projectName,
                ProjectLocation = @projectLocation,
                Vessel = @vessel,
                Client = @client,
                SignatoryName = @signatoryName,
                SignatoryTitle = @signatoryTitle,
                CompanyName = @companyName,
                ProcessingDataJson = @processingDataJson,
                InputFilesJson = @inputFilesJson,
                OutputFilesJson = @outputFilesJson,
                Signature = @signature,
                SignatureAlgorithm = @signatureAlgorithm,
                DataHash = @dataHash,
                SyncStatus = @syncStatus,
                SyncedAt = @syncedAt,
                SyncError = @syncError,
                SyncAttempts = @syncAttempts,
                UpdatedAt = @updatedAt,
                QrCodeUrl = @qrCodeUrl,
                EditionName = @editionName
            WHERE CertificateId = @certificateId;";

        AddCertificateParameters(command, entity);

        await command.ExecuteNonQueryAsync();
    }

    private void AddCertificateParameters(SqliteCommand command, Certificate entity)
    {
        command.Parameters.AddWithValue("@certificateId", entity.CertificateId);
        command.Parameters.AddWithValue("@licenseId", entity.LicenseId);
        command.Parameters.AddWithValue("@licenseeCode", entity.LicenseeCode);
        command.Parameters.AddWithValue("@moduleId", entity.ModuleId);
        command.Parameters.AddWithValue("@moduleCertificateCode", entity.ModuleCertificateCode);
        command.Parameters.AddWithValue("@moduleVersion", entity.ModuleVersion);
        command.Parameters.AddWithValue("@issuedAt", entity.IssuedAt.ToString("O"));
        command.Parameters.AddWithValue("@projectName", entity.ProjectName);
        command.Parameters.AddWithValue("@projectLocation", (object?)entity.ProjectLocation ?? DBNull.Value);
        command.Parameters.AddWithValue("@vessel", (object?)entity.Vessel ?? DBNull.Value);
        command.Parameters.AddWithValue("@client", (object?)entity.Client ?? DBNull.Value);
        command.Parameters.AddWithValue("@signatoryName", entity.SignatoryName);
        command.Parameters.AddWithValue("@signatoryTitle", (object?)entity.SignatoryTitle ?? DBNull.Value);
        command.Parameters.AddWithValue("@companyName", entity.CompanyName);
        command.Parameters.AddWithValue("@processingDataJson", (object?)entity.ProcessingDataJson ?? DBNull.Value);
        command.Parameters.AddWithValue("@inputFilesJson", (object?)entity.InputFilesJson ?? DBNull.Value);
        command.Parameters.AddWithValue("@outputFilesJson", (object?)entity.OutputFilesJson ?? DBNull.Value);
        command.Parameters.AddWithValue("@signature", entity.Signature);
        command.Parameters.AddWithValue("@signatureAlgorithm", entity.SignatureAlgorithm);
        command.Parameters.AddWithValue("@dataHash", (object?)entity.DataHash ?? DBNull.Value);
        command.Parameters.AddWithValue("@syncStatus", entity.SyncStatus);
        command.Parameters.AddWithValue("@syncedAt", entity.SyncedAt.HasValue
            ? entity.SyncedAt.Value.ToString("O")
            : DBNull.Value);
        command.Parameters.AddWithValue("@syncError", (object?)entity.SyncError ?? DBNull.Value);
        command.Parameters.AddWithValue("@syncAttempts", entity.SyncAttempts);
        command.Parameters.AddWithValue("@createdAt", entity.CreatedAt.ToString("O"));
        command.Parameters.AddWithValue("@updatedAt", entity.UpdatedAt.HasValue
            ? entity.UpdatedAt.Value.ToString("O")
            : DBNull.Value);
        command.Parameters.AddWithValue("@qrCodeUrl", (object?)entity.QrCodeUrl ?? DBNull.Value);
        command.Parameters.AddWithValue("@editionName", (object?)entity.EditionName ?? DBNull.Value);
    }

    private Certificate MapFromReader(SqliteDataReader reader)
    {
        return new Certificate
        {
            CertificateId = reader.GetString(reader.GetOrdinal("CertificateId")),
            LicenseId = reader.GetString(reader.GetOrdinal("LicenseId")),
            LicenseeCode = reader.GetString(reader.GetOrdinal("LicenseeCode")),
            ModuleId = reader.GetString(reader.GetOrdinal("ModuleId")),
            ModuleCertificateCode = reader.GetString(reader.GetOrdinal("ModuleCertificateCode")),
            ModuleVersion = reader.GetString(reader.GetOrdinal("ModuleVersion")),
            IssuedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("IssuedAt")), null,
                System.Globalization.DateTimeStyles.RoundtripKind),
            ProjectName = reader.GetString(reader.GetOrdinal("ProjectName")),
            ProjectLocation = GetNullableString(reader, "ProjectLocation"),
            Vessel = GetNullableString(reader, "Vessel"),
            Client = GetNullableString(reader, "Client"),
            SignatoryName = reader.GetString(reader.GetOrdinal("SignatoryName")),
            SignatoryTitle = GetNullableString(reader, "SignatoryTitle"),
            CompanyName = reader.GetString(reader.GetOrdinal("CompanyName")),
            ProcessingDataJson = GetNullableString(reader, "ProcessingDataJson"),
            InputFilesJson = GetNullableString(reader, "InputFilesJson"),
            OutputFilesJson = GetNullableString(reader, "OutputFilesJson"),
            Signature = reader.GetString(reader.GetOrdinal("Signature")),
            SignatureAlgorithm = reader.GetString(reader.GetOrdinal("SignatureAlgorithm")),
            DataHash = GetNullableString(reader, "DataHash"),
            SyncStatus = reader.GetString(reader.GetOrdinal("SyncStatus")),
            SyncedAt = GetNullableDateTime(reader, "SyncedAt"),
            SyncError = GetNullableString(reader, "SyncError"),
            SyncAttempts = reader.GetInt32(reader.GetOrdinal("SyncAttempts")),
            CreatedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("CreatedAt")), null,
                System.Globalization.DateTimeStyles.RoundtripKind),
            UpdatedAt = GetNullableDateTime(reader, "UpdatedAt"),
            QrCodeUrl = GetNullableString(reader, "QrCodeUrl"),
            EditionName = GetNullableString(reader, "EditionName")
        };
    }

    private static string? GetNullableString(SqliteDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static DateTime? GetNullableDateTime(SqliteDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        if (reader.IsDBNull(ordinal))
            return null;

        var value = reader.GetString(ordinal);
        return DateTime.Parse(value, null, System.Globalization.DateTimeStyles.RoundtripKind);
    }

    #endregion
}
