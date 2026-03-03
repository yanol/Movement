using System;
using Microsoft.EntityFrameworkCore;

namespace DataService
{
    public class DataRepository : IDataRepository
    {
        private readonly AppDbContext _db;

        public DataRepository(AppDbContext db) => _db = db;

        public async Task<DataItem?> GetByIdAsync(string id, CancellationToken ct = default)
            => await _db.DataItems
                        .FirstOrDefaultAsync(x => x.Id == id, ct);

        public async Task<string> SaveAsync(DataItem item, CancellationToken ct = default)
        {
            _db.DataItems.Add(item);
            await _db.SaveChangesAsync(ct);
            return item.Id;
        }
    }
}
