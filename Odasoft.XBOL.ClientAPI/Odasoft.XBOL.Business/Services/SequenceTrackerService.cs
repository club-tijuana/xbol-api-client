using Microsoft.EntityFrameworkCore;
using Odasoft.XBOL.Data.Repositories;

namespace Odasoft.XBOL.Business.Services
{
    public class SequenceTrackerService
    {
        private readonly SequenceTrackerRepository _sequenceTrackerRepository;

        public SequenceTrackerService(SequenceTrackerRepository sequenceTrackerRepository)
        {
            _sequenceTrackerRepository = sequenceTrackerRepository;
        }

        public async Task<string> GenerateLocalizerAsync(string prefix, long identifier)
        {
            string sequenceKey = $"{prefix}-{identifier}";

            long nextValue = await GetNextSequenceValueAsync(sequenceKey);

            return $"{prefix}-{identifier}-{nextValue}";
        }

        private async Task<long> GetNextSequenceValueAsync(string sequenceKey)
        {
            var tracker = await _sequenceTrackerRepository
                                .Get()
                                .Where(st => st.SequenceKey == sequenceKey)
                                .FirstOrDefaultAsync();

            if (tracker == null)
            {
                tracker = new Models.SequenceTracker
                {
                    SequenceKey = sequenceKey,
                    LastValue = 1
                };
                await _sequenceTrackerRepository.InsertAsync(tracker);
            }
            else
            {
                tracker.LastValue++;
                await _sequenceTrackerRepository.UpdateAsync(tracker);
            }

            await _sequenceTrackerRepository.CommitAsync();

            return tracker.LastValue;
        }
    }
}
