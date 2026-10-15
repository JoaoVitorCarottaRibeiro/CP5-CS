using System;
using System.Threading.Tasks;
using ExpenseHub.Api.Data;
using ExpenseHub.Api.Domain;
using ExpenseHub.Api.Dtos;
using ExpenseHub.Api.Responses;
using ExpenseHub.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests;

/// <summary>Testes unitarios das transicoes e da persistencia coordenadas pelo servico.</summary>
[TestClass]
public sealed class ExpenseServiceTests
{
    private static readonly DateTimeOffset _fixedNow = new(2026, 10, 13, 15, 30, 0, TimeSpan.Zero);

    /// <summary>Aprovacao altera o estado e cria um unico historico com ator e horario do servidor.</summary>
    [TestMethod]
    public async Task ApproveCreatesHistoryAndRepeatedAttemptConflicts()
    {
        await using AppDbContext database = CreateDatabase();
        Expense expense = await SeedExpenseAsync(database, ExpenseState.Submitted);
        ExpenseService service = CreateService(database);
        ExpenseCaller approver = Caller("approver", Roles.Approver);

        ServiceResult<ExpenseResponse> approved = await service.ApproveAsync(approver, expense.Id);
        ServiceResult<ExpenseResponse> repeated = await service.ApproveAsync(approver, expense.Id);

        Assert.AreEqual(ServiceStatus.Success, approved.Status);
        Assert.AreEqual(ExpenseState.Approved, approved.Value?.State);
        Assert.AreEqual(ServiceStatus.Conflict, repeated.Status);
        Assert.AreEqual(1, await database.ExpenseHistory.CountAsync());

        ExpenseHistory history = await database.ExpenseHistory.SingleAsync();
        Assert.AreEqual(ExpenseAction.Approved, history.Action);
        Assert.AreEqual("approver", history.ActorId);
        Assert.AreEqual(_fixedNow.UtcDateTime, history.OccurredAtUtc);
        Assert.AreEqual(ExpenseState.Submitted, history.PreviousState);
        Assert.AreEqual(ExpenseState.Approved, history.NextState);
    }

    /// <summary>Reprovacao persiste a justificativa validada no historico.</summary>
    [TestMethod]
    public async Task RejectPersistsJustification()
    {
        await using AppDbContext database = CreateDatabase();
        Expense expense = await SeedExpenseAsync(database, ExpenseState.Submitted);
        ExpenseService service = CreateService(database);
        ExpenseCaller approver = Caller("approver", Roles.Approver);
        RejectExpenseRequest request = new() { Justification = "Comprovante fiscal ilegivel." };

        ServiceResult<ExpenseResponse> result = await service.RejectAsync(approver, expense.Id, request);

        Assert.AreEqual(ServiceStatus.Success, result.Status);
        Assert.AreEqual(ExpenseState.Rejected, result.Value?.State);

        ExpenseHistory history = await database.ExpenseHistory.SingleAsync();
        Assert.AreEqual(ExpenseAction.Rejected, history.Action);
        Assert.AreEqual(request.Justification, history.Justification);
        Assert.AreEqual(_fixedNow.UtcDateTime, history.OccurredAtUtc);
    }

    /// <summary>Autoaprovacao e bloqueada sem alterar estado nem historico.</summary>
    [TestMethod]
    public async Task ApproverCannotApproveOwnExpense()
    {
        await using AppDbContext database = CreateDatabase();
        Expense expense = await SeedExpenseAsync(database, ExpenseState.Submitted, ownerId: "same-user");
        ExpenseService service = CreateService(database);
        ExpenseCaller caller = Caller("same-user", Roles.Employee, Roles.Approver);

        ServiceResult<ExpenseResponse> result = await service.ApproveAsync(caller, expense.Id);

        Assert.AreEqual(ServiceStatus.Forbidden, result.Status);
        Assert.AreEqual(ExpenseState.Submitted, expense.State);
        Assert.AreEqual(0, await database.ExpenseHistory.CountAsync());
    }

    /// <summary>Pagamento grava PaymentRecord e historico com os mesmos dados do servidor.</summary>
    [TestMethod]
    public async Task PayCreatesPaymentAndHistoryAtomically()
    {
        await using AppDbContext database = CreateDatabase();
        Expense expense = await SeedExpenseAsync(database, ExpenseState.Approved);
        ExpenseService service = CreateService(database);
        ExpenseCaller finance = Caller("finance", Roles.Finance);

        ServiceResult<ExpenseResponse> result = await service.PayAsync(finance, expense.Id);

        Assert.AreEqual(ServiceStatus.Success, result.Status);
        Assert.AreEqual(ExpenseState.Paid, result.Value?.State);
        Assert.AreEqual(1, await database.Payments.CountAsync());
        Assert.AreEqual(1, await database.ExpenseHistory.CountAsync());

        PaymentRecord payment = await database.Payments.SingleAsync();
        ExpenseHistory history = await database.ExpenseHistory.SingleAsync();
        Assert.AreEqual("finance", payment.PaidById);
        Assert.AreEqual(expense.Amount, payment.Amount);
        Assert.AreEqual(_fixedNow.UtcDateTime, payment.PaidAtUtc);
        Assert.AreEqual(_fixedNow.UtcDateTime, history.OccurredAtUtc);
        Assert.AreEqual(ExpenseAction.Paid, history.Action);
        Assert.AreEqual(ExpenseState.Approved, history.PreviousState);
        Assert.AreEqual(ExpenseState.Paid, history.NextState);
    }

    /// <summary>Repetir pagamento resulta em conflito e nao duplica os registros.</summary>
    [TestMethod]
    public async Task RepeatedPaymentDoesNotDuplicateRecords()
    {
        await using AppDbContext database = CreateDatabase();
        Expense expense = await SeedExpenseAsync(database, ExpenseState.Approved);
        ExpenseService service = CreateService(database);
        ExpenseCaller finance = Caller("finance", Roles.Finance);

        await service.PayAsync(finance, expense.Id);
        ServiceResult<ExpenseResponse> repeated = await service.PayAsync(finance, expense.Id);

        Assert.AreEqual(ServiceStatus.Conflict, repeated.Status);
        Assert.AreEqual(1, await database.Payments.CountAsync());
        Assert.AreEqual(1, await database.ExpenseHistory.CountAsync());
    }

    /// <summary>Finance nao pode pagar o proprio reembolso.</summary>
    [TestMethod]
    public async Task FinanceCannotPayOwnExpense()
    {
        await using AppDbContext database = CreateDatabase();
        Expense expense = await SeedExpenseAsync(database, ExpenseState.Approved, ownerId: "same-user");
        ExpenseService service = CreateService(database);
        ExpenseCaller caller = Caller("same-user", Roles.Employee, Roles.Finance);

        ServiceResult<ExpenseResponse> result = await service.PayAsync(caller, expense.Id);

        Assert.AreEqual(ServiceStatus.Forbidden, result.Status);
        Assert.AreEqual(0, await database.Payments.CountAsync());
        Assert.AreEqual(0, await database.ExpenseHistory.CountAsync());
    }

    /// <summary>Historico e devolvido em ordem cronologica para quem enxerga o reembolso.</summary>
    [TestMethod]
    public async Task HistoryIsOrderedAndUsesExpenseVisibility()
    {
        await using AppDbContext database = CreateDatabase();
        Expense expense = await SeedExpenseAsync(database, ExpenseState.Submitted);
        database.ExpenseHistory.AddRange(
            History(expense.Id, ExpenseAction.Submitted, new DateTime(2026, 10, 13, 11, 0, 0, DateTimeKind.Utc)),
            History(expense.Id, ExpenseAction.Created, new DateTime(2026, 10, 13, 10, 0, 0, DateTimeKind.Utc)));
        await database.SaveChangesAsync();
        ExpenseService service = CreateService(database);

        ServiceResult<System.Collections.Generic.IReadOnlyList<ExpenseHistoryResponse>> visible =
            await service.GetHistoryAsync(Caller("approver", Roles.Approver), expense.Id);
        ServiceResult<System.Collections.Generic.IReadOnlyList<ExpenseHistoryResponse>> hidden =
            await service.GetHistoryAsync(Caller("other", Roles.Employee), expense.Id);

        Assert.AreEqual(ServiceStatus.Success, visible.Status);
        Assert.AreEqual(2, visible.Value?.Count);
        Assert.AreEqual(ExpenseAction.Created, visible.Value?[0].Action);
        Assert.AreEqual(ExpenseAction.Submitted, visible.Value?[1].Action);
        Assert.AreEqual(ServiceStatus.NotFound, hidden.Status);
    }

    private static AppDbContext CreateDatabase()
    {
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static ExpenseService CreateService(AppDbContext database)
    {
        return new ExpenseService(database, new FixedTimeProvider(_fixedNow));
    }

    private static async Task<Expense> SeedExpenseAsync(
        AppDbContext database,
        ExpenseState state,
        string ownerId = "owner")
    {
        ExpenseCategory category = new() { Name = "Transporte" };
        Expense expense = new()
        {
            OwnerId = ownerId,
            Category = category,
            Description = "Taxi para reuniao externa",
            Amount = 75.40m,
            ExpenseDate = new DateTime(2026, 10, 10),
            State = state,
            CreatedAtUtc = new DateTime(2026, 10, 13, 10, 0, 0, DateTimeKind.Utc),
            UpdatedAtUtc = new DateTime(2026, 10, 13, 10, 0, 0, DateTimeKind.Utc),
        };

        database.Expenses.Add(expense);
        await database.SaveChangesAsync();

        return expense;
    }

    private static ExpenseHistory History(int expenseId, ExpenseAction action, DateTime occurredAtUtc)
    {
        return new ExpenseHistory
        {
            ExpenseId = expenseId,
            Action = action,
            ActorId = "owner",
            OccurredAtUtc = occurredAtUtc,
            PreviousState = ExpenseState.Draft,
            NextState = action == ExpenseAction.Submitted ? ExpenseState.Submitted : ExpenseState.Draft,
        };
    }

    private static ExpenseCaller Caller(string userId, params string[] roles)
    {
        return new ExpenseCaller(userId, roles);
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        internal FixedTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _utcNow;
        }
    }
}
