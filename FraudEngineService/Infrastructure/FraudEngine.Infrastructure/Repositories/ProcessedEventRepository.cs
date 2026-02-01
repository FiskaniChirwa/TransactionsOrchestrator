using FraudEngine.Core.Repositories;
using FraudEngine.Infrastructure.Data;
using FraudEngine.Infrastructure.Data.Entities;
using FraudEngine.Models.Responses;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace FraudEngine.Infrastructure.Repositories;

public class ProcessedEventRepository(FraudEngineDbContext context) : IProcessedEventRepository
{
    private readonly FraudEngineDbContext _context = context;

    public async Task<FraudAnalysisResponse?> GetByEventIdAsync(string eventId)
    {
        var entity = await _context.ProcessedEvents
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.EventId == eventId);

        if (entity == null)
            return null;

        return JsonSerializer.Deserialize<FraudAnalysisResponse>(entity.Result);
    }

    public async Task AddAsync(string eventId, long transactionId, FraudAnalysisResponse result)
    {
        var entity = new ProcessedEventEntity
        {
            EventId = eventId,
            TransactionId = transactionId,
            ProcessedAt = DateTime.UtcNow,
            Result = JsonSerializer.Serialize(result)
        };

        _context.ProcessedEvents.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ExistsAsync(string eventId)
    {
        return await _context.ProcessedEvents
            .AnyAsync(e => e.EventId == eventId);
    }
}