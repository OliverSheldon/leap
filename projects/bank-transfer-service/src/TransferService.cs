using System.Collections.Concurrent;

namespace BankTransfer;

public sealed class TransferService : ITransferService
{
    private readonly IAccountStore _accounts;
    private readonly IIdempotencyStore _idempotency;
    private static readonly ConcurrentDictionary<Guid, object> _locks = new();

    public TransferService(IAccountStore accounts, IIdempotencyStore idempotency)
    {
        _accounts = accounts;
        _idempotency = idempotency;
    }

    public async Task TransferAsync(Guid fromId, Guid toId, decimal amount, string requestId, CancellationToken ct = default)
    {
        //Approach:
        //Write the whole transaction out from start to finish, checking back to the requirements at each step

        //Assumptions:
        //Custom exceptions to be written later, but for the sake of testing, messages will be fine for now
        //Requirements were only to implement the TransferAsync method, so implementing the CancellationToken would be out of scope

        //Check account IDs

        if (fromId == toId)
        {
            throw new Exception("Can not transfer to the same account");
        }

        if(await _accounts.GetAsync(fromId, ct) == null)
        {
            throw new Exception($"From Account not found: {fromId}");
        }

        if(await _accounts.GetAsync(toId, ct) == null)
        {
            throw new Exception($"To Account not found: {toId}");
        }

        //Check amount
        if (amount == 0 || amount < 0)
        {
            throw new Exception("Amount must be a positive, none zero value");
        }

        //idempotency check
        if(await _idempotency.TryRecordAsync(requestId, ct))
        {
            bool retry = true;
            int retryCount = 0;

            while (retry)
            {
                if (!_locks.TryGetValue(fromId, out _) && !_locks.TryGetValue(toId, out _))
                {
                    _locks.TryAdd(fromId, null);
                    _locks.TryAdd(toId, null);

                    retry = false;

                    //Check for funds
                    Account fromAccount = await _accounts.GetAsync(fromId, ct);

                    decimal fromAccountNewBalance = fromAccount.Balance - amount;

                    if (fromAccountNewBalance <= 0)
                    {
                        throw new Exception("Insufficient Funds");
                    }

                    //Transaction
                    Account toAccount = await _accounts.GetAsync(toId, ct);

                    await _accounts.UpdateAsync(new Account(fromId, fromAccountNewBalance), ct);
                    await _accounts.UpdateAsync(new Account(toId, toAccount.Balance + amount), ct);

                    break;

                    //Future improvements:
                    //reverse account update if one update call fails
                    //keep log of transaction history to help with the above and to produce statements
                }

                Thread.Sleep(100);

                if(retryCount == 5) { throw new Exception("An error has occured with your transaction. Please try again later"); }

                retryCount++;
            }

            //Remove the lock
            _locks.TryRemove(fromId, out _);
            _locks.TryRemove(toId, out _);
        }
    }
}