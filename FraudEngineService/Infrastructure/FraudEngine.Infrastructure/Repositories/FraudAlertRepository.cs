using FraudEngine.Core.Models;
using FraudEngine.Core.Repositories;
using FraudEngine.Infrastructure.Data;
using FraudEngine.Infrastructure.Mapping;
using Microsoft.EntityFrameworkCore;

namespace FraudEngine.Infrastructure.Repositories;

public class FraudAlertRepository(FraudEngineDbContext context) : IFraudAlertRepository
{
    private readonly FraudEngineDbContext _context = context;

    public async Task<Alert> AddAsync(Alert alert)
    {
        var entity = alert.ToEntity();

        _context.FraudAlerts.Add(entity);
        await _context.SaveChangesAsync();

        return entity.ToDomain();
    }

    public async Task<Alert?> GetByIdAsync(long id)
    {
        var entity = await _context.FraudAlerts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id);

        return entity?.ToDomain();
    }

    public async Task<Alert?> GetByTransactionIdAsync(long transactionId)
    {
        var entity = await _context.FraudAlerts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.TransactionId == transactionId);

        return entity?.ToDomain();
    }

    public async Task<List<Alert>> GetByCustomerIdAsync(long customerId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _context.FraudAlerts
            .AsNoTracking()
            .Where(a => a.CustomerId == customerId);

        if (fromDate.HasValue)
            query = query.Where(a => a.TransactionDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(a => a.TransactionDate <= toDate.Value);

        var entities = await query
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        return entities.Select(e => e.ToDomain()).ToList();
    }

    public async Task<List<Alert>> GetAllAsync(int skip = 0, int take = 50)
    {
        var entities = await _context.FraudAlerts
            .AsNoTracking()
            .OrderByDescending(a => a.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        return entities.Select(e => e.ToDomain()).ToList();
    }
}