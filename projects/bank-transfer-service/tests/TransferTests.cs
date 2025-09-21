using System;
using System.Threading.Tasks;
using BankTransfer;
using Xunit;

public class TransferTests
{
    [Fact]
    public async Task Transfer_MovesMoney_Once_WithIdempotency()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var store = new InMemoryAccountStore(new[]
        {
            new Account(a, 100m),
            new Account(b, 0m)
        });
        var idem = new InMemoryIdempotencyStore();
        var svc = new TransferService(store, idem);

        var requestId = Guid.NewGuid().ToString();
        await svc.TransferAsync(a, b, 10m, requestId);
        await svc.TransferAsync(a, b, 10m, requestId); // same id, should no-op

        var from = await store.GetAsync(a, default);
        var to = await store.GetAsync(b, default);

        Assert.Equal(90m, from!.Balance);
        Assert.Equal(10m, to!.Balance);
    }

    [Fact]
    public async Task Transfer_Exception_ToAccountNotFound()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var store = new InMemoryAccountStore(new[]
        {
            new Account(a, 100m),
            new Account(b, 0m)
        });
        var idem = new InMemoryIdempotencyStore();
        var svc = new TransferService(store, idem);

        try
        {
            await svc.TransferAsync(a, Guid.NewGuid(), 10m, Guid.NewGuid().ToString());
        }
        catch (Exception ex)
        {
            Assert.True(ex.Message.StartsWith("To Account not found"));
        }
    }

    [Fact]
    public async Task Transfer_Exception_FromAccountNotFound()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var store = new InMemoryAccountStore(new[]
        {
            new Account(a, 100m),
            new Account(b, 0m)
        });
        var idem = new InMemoryIdempotencyStore();
        var svc = new TransferService(store, idem);

        try
        {
            await svc.TransferAsync(Guid.NewGuid(), b, 10m, Guid.NewGuid().ToString());
        }
        catch (Exception ex)
        {
            Assert.True(ex.Message.StartsWith("From Account not found"));
        }
    }

    [Fact]
    public async Task Transfer_Exception_SameAccount()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var store = new InMemoryAccountStore(new[]
        {
            new Account(a, 100m),
            new Account(b, 0m)
        });
        var idem = new InMemoryIdempotencyStore();
        var svc = new TransferService(store, idem);

        try
        {
            await svc.TransferAsync(a, a, 10m, Guid.NewGuid().ToString());
        }
        catch (Exception ex)
        {
            Assert.True(ex.Message.StartsWith("Can not transfer to the same account"));
        }
    }

    [Fact]
    public async Task Transfer_Exception_ZeroAmount()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var store = new InMemoryAccountStore(new[]
        {
            new Account(a, 100m),
            new Account(b, 10m)
        });
        var idem = new InMemoryIdempotencyStore();
        var svc = new TransferService(store, idem);

        try
        {
            await svc.TransferAsync(a, b, 0, Guid.NewGuid().ToString());
        }
        catch (Exception ex)
        {
            Assert.True(ex.Message.StartsWith("Amount must be a positive, none zero value"));

            var from = await store.GetAsync(a, default);
            var to = await store.GetAsync(b, default);

            Assert.Equal(100m, from!.Balance);
            Assert.Equal(10m, to!.Balance);
        }
    }

    [Fact]
    public async Task Transfer_Exception_NegativeAmount()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var store = new InMemoryAccountStore(new[]
        {
            new Account(a, 100m),
            new Account(b, 10m)
        });
        var idem = new InMemoryIdempotencyStore();
        var svc = new TransferService(store, idem);

        try
        {
            await svc.TransferAsync(a, b, -10m, Guid.NewGuid().ToString());
        }
        catch (Exception ex)
        {
            Assert.True(ex.Message.StartsWith("Amount must be a positive, none zero value"));

            var from = await store.GetAsync(a, default);
            var to = await store.GetAsync(b, default);

            Assert.Equal(100m, from!.Balance);
            Assert.Equal(10m, to!.Balance);
        }
    }

    [Fact]
    public async Task Transfer_Exception_InsufficientFunds()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var store = new InMemoryAccountStore(new[]
        {
            new Account(a, 100m),
            new Account(b, 0m)
        });
        var idem = new InMemoryIdempotencyStore();
        var svc = new TransferService(store, idem);

        try
        {
            await svc.TransferAsync(b, a, 10m, Guid.NewGuid().ToString());
        }
        catch (Exception ex)
        {
            Assert.True(ex.Message.StartsWith("Insufficient Funds"));

            var from = await store.GetAsync(a, default);
            var to = await store.GetAsync(b, default);

            Assert.Equal(100m, from!.Balance);
            Assert.Equal(0m, to!.Balance);
        }
    }
}