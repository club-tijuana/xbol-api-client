using Microsoft.EntityFrameworkCore;
using Odasoft.XBOL.Commons.Constants;
using Odasoft.XBOL.Commons.Enums;
using Odasoft.XBOL.Commons.Responses;
using Odasoft.XBOL.Data.Repositories;
using Odasoft.XBOL.DTO.QueryParams;
using Odasoft.XBOL.DTO.Requests;
using Odasoft.XBOL.DTO.Results;
using Odasoft.XBOL.Models;

namespace Odasoft.XBOL.Business.Services
{
    public class ClientCreditTransactionService
    {
        private readonly ClientCreditTransactionRepository _clientCreditTransactionRepository;
        private readonly SequenceTrackerService _sequenceTrackerService;

        private const string CLIENT_CREDIT_TRANSACTION_LOCALIZER_PREFIX = "CCT"; // TODO: Get this value from a configuration file or database in the future

        public ClientCreditTransactionService(ClientCreditTransactionRepository clientCreditTransactionRepository, SequenceTrackerService sequenceTrackerService)
        {
            _clientCreditTransactionRepository = clientCreditTransactionRepository;
            _sequenceTrackerService = sequenceTrackerService;
        }

        public async Task<PagedResponse<CreditTransactionResult>> GetCreditTransactionsAsync(CreditTransactionsQueryParams queryParams)
        {
            var transactions = queryParams.CreditAccountId.HasValue
                                ? _clientCreditTransactionRepository.Get()
                                    .Where(x => x.ClientCreditAccountId == queryParams.CreditAccountId)
                                : _clientCreditTransactionRepository.Get();

            var query = transactions
                        .AsNoTracking()
                        .Where(x => x.TransactionType == CreditTransactionType.Payment
                            || x.TransactionType == CreditTransactionType.Reversal
                            || x.TransactionType == CreditTransactionType.AdjustmentCredit) // For now only return credit records
                        .Select(ct => new CreditTransactionResult
                        {
                            Id = ct.Id,
                            Amount = ct.Amount,
                            PaymentDate = ct.TransactionDate,
                            PaymentType = ct.PaymentType,
                            ReceivedBy = "John Doe", // TODO: properly link to the system user that record the installment
                            ReferenceId = ct.ReferenceId,
                            TransactionType = ct.TransactionType
                        });

            SetFilters(ref query, queryParams);
            SetSearchTermFilter(ref query, queryParams.SearchTerm);
            SetOrder(ref query, queryParams.SortBy, queryParams.Descending);

            List<CreditTransactionResult> creditTransactions = await query.ToListAsync();

            int totalCount = creditTransactions.Count();

            return new PagedResponse<CreditTransactionResult>
            {
                TotalCount = totalCount,
                Page = queryParams.Page,
                PageSize = queryParams.PageSize,
                Items = creditTransactions
                        .Skip(queryParams.Page * queryParams.PageSize)
                        .Take(queryParams.PageSize)
                        .ToList(),
                TotalPages = 0
            };
        }

        public async Task<bool> CreateCreditTransactionByCreditAccountIdAsync(long clientCreditAccountId, ClientCreditTransactionRequest request)
        {
            string localizer = await _sequenceTrackerService.GenerateLocalizerAsync(CLIENT_CREDIT_TRANSACTION_LOCALIZER_PREFIX);

            var newCreditTransaction = new ClientCreditTransaction
            {
                ClientCreditAccountId = clientCreditAccountId,
                Amount = request.Amount,
                PaymentType = request.PaymentType,
                TransactionDate = request.TransactionDate.ToUniversalTime(),
                TransactionType = request.TransactionType,
                Description = request.ReferenceId,
                ReferenceId = localizer,
                CreatedAt = DateTimeOffset.UtcNow.ToUniversalTime(),
                CreatedBy = Guid.Empty,
                UpdatedAt = DateTimeOffset.UtcNow.ToUniversalTime(),
                UpdatedBy = Guid.Empty
            };

            try
            {
                await _clientCreditTransactionRepository.InsertAsync(newCreditTransaction);
                await _clientCreditTransactionRepository.CommitAsync();

                // TODO: recalculate credit account balance
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating credit transaction for the client account {clientCreditAccountId}. Error: {ex.Message}");
                return false;
            }

            return true;
        }

        public async Task<bool> UpdateCreditTransactionByIdAsync(long creditTransactionId, ClientCreditTransactionRequest request)
        {
            ClientCreditTransaction? existingCreditTransaction = await _clientCreditTransactionRepository.GetByIdAsync(creditTransactionId);

            if (existingCreditTransaction == null)
            {
                Console.WriteLine($"Credit transaction with Id {creditTransactionId} not found.");
                return false;
            }

            existingCreditTransaction.Amount = request.Amount;
            existingCreditTransaction.TransactionDate = request.TransactionDate.ToUniversalTime();
            existingCreditTransaction.TransactionType = request.TransactionType;
            existingCreditTransaction.PaymentType = request.PaymentType;
            existingCreditTransaction.UpdatedAt = DateTimeOffset.UtcNow.ToUniversalTime();
            existingCreditTransaction.UpdatedBy = Guid.Empty;

            try
            {
                await _clientCreditTransactionRepository.UpdateAsync(existingCreditTransaction);
                await _clientCreditTransactionRepository.CommitAsync();

                // TODO: recalculate credit account balance
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating credit transaction with Id {creditTransactionId}. Error: {ex.Message}");
                return false;
            }

            return true;
        }

        public async Task<bool> DeleteCreditTransactionByIdAsync(long creditTransactionId)
        {
            ClientCreditTransaction? esistingCreditTransaction = await _clientCreditTransactionRepository.GetByIdAsync(creditTransactionId);

            if (esistingCreditTransaction == null)
            {
                Console.WriteLine($"Credit transaction with Id {creditTransactionId} does not exist");
                return false;
            }

            try
            {
                await _clientCreditTransactionRepository.HardDeleteAsync(esistingCreditTransaction);
                await _clientCreditTransactionRepository.CommitAsync();

                // TODO: recalculate credit account balance
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting credit transaction with Id {creditTransactionId}. Error: {ex.Message}");
                return false;
            }

            return true;
        }

        private void SetFilters(ref IQueryable<CreditTransactionResult> query, CreditTransactionsQueryParams queryParams)
        {
            if (queryParams.PaymentTypes is not null && queryParams.PaymentTypes.Any())
            {
                query = query.Where(x => queryParams.PaymentTypes.Contains(x.PaymentType));
            }

            // Filter between dates
            if (queryParams.StartDate.HasValue && queryParams.EndDate.HasValue)
            {
                var startDateUtc = queryParams.StartDate.Value.ToUniversalTime();
                var endDateUtc = queryParams.EndDate.Value.ToUniversalTime();

                query = query.Where(ct => ct.PaymentDate >= startDateUtc && ct.PaymentDate <= endDateUtc);
            }
        }

        private void SetSearchTermFilter(ref IQueryable<CreditTransactionResult> query, string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm) == false)
            {
                var lowerSearchTerm = searchTerm.ToLower();
                query = query.Where(x => x.ReferenceId.ToLower().Contains(lowerSearchTerm)
                    || x.ReceivedBy.ToLower().Contains(searchTerm));
            }
        }

        private void SetOrder(ref IQueryable<CreditTransactionResult> query, string sortBy, bool descending)
        {
            if (string.IsNullOrWhiteSpace(sortBy))
            {
                return;
            }

            // TODO: validate each case
            query = sortBy.ToLower() switch
            {
                QueryParamsFieldNames.CREDIT_TRANSACTION_AMOUNT => descending ? query.OrderByDescending(x => x.Amount) : query.OrderBy(x => x.Amount),
                QueryParamsFieldNames.CREDIT_TRANSACTION_PAYMENT_DATE => descending ? query.OrderByDescending(x => x.PaymentDate) : query.OrderBy(x => x.PaymentDate),
                QueryParamsFieldNames.CREDIT_TRANSACTION_PAYMENT_TYPE => descending ? query.OrderByDescending(x => x.PaymentType) : query.OrderBy(x => x.PaymentType),
                QueryParamsFieldNames.CREDIT_TRANSACTION_RECEIVED_BY => descending ? query.OrderByDescending(x => x.ReceivedBy) : query.OrderBy(x => x.ReceivedBy),
                QueryParamsFieldNames.CREDIT_TRANSACTION_REFERENCE_ID => descending ? query.OrderByDescending(x => x.ReferenceId) : query.OrderBy(x => x.ReferenceId),
                _ => query
            };
        }
    }
}
