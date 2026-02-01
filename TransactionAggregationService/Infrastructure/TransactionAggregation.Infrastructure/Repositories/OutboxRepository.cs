using Microsoft.EntityFrameworkCore;
using TransactionAggregation.Infrastructure.Data;
using TransactionAggregation.Infrastructure.Data.Entities;
using TransactionAggregation.Infrastructure.Outbox.Models;

namespace TransactionAggregation.Infrastructure.Repositories;

public class OutboxRepository : IOutboxRepository
{
    private readonly OutboxDbContext _context;

    public OutboxRepository(OutboxDbContext context)
    {
        _context = context;
    }

    public async Task<OutboxMessage> AddAsync(OutboxMessage message)
    {
        var entity = MapToEntity(message);

        _context.OutboxMessages.Add(entity);
        await _context.SaveChangesAsync();

        return MapToDomain(entity);
    }

    public async Task AddBatchAsync(List<OutboxMessage> messages)
    {
        var entities = messages.Select(MapToEntity).ToList();

        _context.OutboxMessages.AddRange(entities);
        await _context.SaveChangesAsync();
    }

    public async Task<List<OutboxMessage>> GetPendingAsync(int batchSize)
    {
        var entities = await _context.OutboxMessages
            .Where(m => m.Status == "Pending")
            .OrderBy(m => m.CreatedAt)
            .Take(batchSize)
            .ToListAsync();

        return entities.Select(MapToDomain).ToList();
    }

    public async Task UpdateAsync(OutboxMessage message)
    {
        var entity = await _context.OutboxMessages.FindAsync(message.Id);

        if (entity != null)
        {
            entity.Status = message.Status;
            entity.ProcessedAt = message.ProcessedAt;
            entity.AttemptCount = message.AttemptCount;
            entity.LastAttemptAt = message.LastAttemptAt;
            entity.LastError = message.LastError;

            await _context.SaveChangesAsync();
        }
    }

    public async Task<int> CountByStatusAsync(string status)
    {
        return await _context.OutboxMessages
            .Where(m => m.Status == status)
            .CountAsync();
    }

    public async Task<OutboxMessage?> GetOldestPendingAsync()
    {
        var entity = await _context.OutboxMessages
            .Where(m => m.Status == "Pending")
            .OrderBy(m => m.CreatedAt)
            .FirstOrDefaultAsync();

        return entity == null ? null : MapToDomain(entity);
    }

    public async Task<OutboxMessage?> GetByEventIdAsync(string eventId)
    {
        var entity = await _context.OutboxMessages
            .FirstOrDefaultAsync(m => m.EventId == eventId);

        return entity == null ? null : MapToDomain(entity);
    }

    private static OutboxMessageEntity MapToEntity(OutboxMessage message)
    {
        return new OutboxMessageEntity
        {
            Id = message.Id,
            EventId = message.EventId,
            EventType = message.EventType,
            Payload = message.Payload,
            CreatedAt = message.CreatedAt,
            ProcessedAt = message.ProcessedAt,
            AttemptCount = message.AttemptCount,
            LastAttemptAt = message.LastAttemptAt,
            LastError = message.LastError,
            Status = message.Status
        };
    }

    private static OutboxMessage MapToDomain(OutboxMessageEntity entity)
    {
        return new OutboxMessage
        {
            Id = entity.Id,
            EventId = entity.EventId,
            EventType = entity.EventType,
            Payload = entity.Payload,
            CreatedAt = entity.CreatedAt,
            ProcessedAt = entity.ProcessedAt,
            AttemptCount = entity.AttemptCount,
            LastAttemptAt = entity.LastAttemptAt,
            LastError = entity.LastError,
            Status = entity.Status
        };
    }
}