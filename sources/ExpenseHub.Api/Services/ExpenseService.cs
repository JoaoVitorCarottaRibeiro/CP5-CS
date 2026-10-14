using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using ExpenseHub.Api.Data;
using ExpenseHub.Api.Domain;
using ExpenseHub.Api.Dtos;
using Microsoft.EntityFrameworkCore;

namespace ExpenseHub.Api.Services;

/// <summary>
/// Regras de negocio dos reembolsos. Toda operacao combina role, ownership e
/// estado por meio da <see cref="ExpensePolicy"/>, e cada alteracao e gravada
/// junto do seu historico no mesmo SaveChanges (mesma transacao).
/// </summary>
internal sealed class ExpenseService
{
    private readonly AppDbContext _database;
    private readonly TimeProvider _timeProvider;

    /// <summary>Cria o servico.</summary>
    /// <param name="database">Contexto de persistencia.</param>
    /// <param name="timeProvider">Fonte do horario do servidor.</param>
    public ExpenseService(AppDbContext database, TimeProvider timeProvider)
    {
        _database = database;
        _timeProvider = timeProvider;
    }

    /// <summary>Cria um rascunho cujo proprietario e o chamador.</summary>
    /// <param name="caller">Usuario autenticado.</param>
    /// <param name="request">Dados ja validados do reembolso.</param>
    /// <returns>Reembolso criado em Draft.</returns>
    public async Task<ServiceResult<ExpenseResponse>> CreateAsync(ExpenseCaller caller, ExpenseRequest request)
    {
        if (!caller.IsEmployee)
        {
            return ServiceResult.Failure<ExpenseResponse>(
                ServiceStatus.Forbidden,
                "Somente Employee pode criar reembolsos.");
        }

        int categoryId = request.CategoryId.GetValueOrDefault();
        ExpenseCategory? category = await _database.Categories.FirstOrDefaultAsync(c => c.Id == categoryId);
        if (category is null)
        {
            return ServiceResult.Invalid<ExpenseResponse>(nameof(ExpenseRequest.CategoryId), "Categoria inexistente.");
        }

        DateTime now = UtcNow();
        Expense expense = new()
        {
            OwnerId = caller.UserId,
            Category = category,
            CategoryId = category.Id,
            Description = request.Description,
            Amount = request.Amount.GetValueOrDefault(),
            ExpenseDate = ToDateTime(request.ExpenseDate.GetValueOrDefault()),
            State = ExpenseState.Draft,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };

        expense.History.Add(new ExpenseHistory
        {
            Action = ExpenseAction.Created,
            ActorId = caller.UserId,
            OccurredAtUtc = now,
            PreviousState = ExpenseState.Draft,
            NextState = ExpenseState.Draft,
        });

        _database.Expenses.Add(expense);
        await _database.SaveChangesAsync();

        return ServiceResult.Success(ExpenseResponse.FromEntity(expense));
    }

    /// <summary>Edita um rascunho do proprietario.</summary>
    /// <param name="caller">Usuario autenticado.</param>
    /// <param name="id">Identificador do reembolso.</param>
    /// <param name="request">Novos dados ja validados.</param>
    /// <returns>Reembolso atualizado.</returns>
    public async Task<ServiceResult<ExpenseResponse>> UpdateAsync(
        ExpenseCaller caller,
        int id,
        ExpenseRequest request)
    {
        (Expense? expense, AccessDecision decision) = await FindForAsync(caller, id, ExpenseOperation.Edit);
        if (expense is null || decision != AccessDecision.Allowed)
        {
            return ServiceResult.Denied<ExpenseResponse>(decision, "Somente reembolsos em Draft podem ser editados.");
        }

        int categoryId = request.CategoryId.GetValueOrDefault();
        if (categoryId != expense.CategoryId)
        {
            ExpenseCategory? category = await _database.Categories.FirstOrDefaultAsync(c => c.Id == categoryId);
            if (category is null)
            {
                return ServiceResult.Invalid<ExpenseResponse>(nameof(ExpenseRequest.CategoryId), "Categoria inexistente.");
            }

            expense.Category = category;
        }

        List<string> changes = ApplyChanges(expense, request);
        if (changes.Count > 0)
        {
            DateTime now = UtcNow();
            expense.UpdatedAtUtc = now;
            expense.History.Add(new ExpenseHistory
            {
                Action = ExpenseAction.Updated,
                ActorId = caller.UserId,
                OccurredAtUtc = now,
                PreviousState = ExpenseState.Draft,
                NextState = ExpenseState.Draft,
                Changes = string.Join("; ", changes),
            });

            await _database.SaveChangesAsync();
        }

        return ServiceResult.Success(ExpenseResponse.FromEntity(expense));
    }

    /// <summary>Envia um rascunho do proprietario para analise.</summary>
    /// <param name="caller">Usuario autenticado.</param>
    /// <param name="id">Identificador do reembolso.</param>
    /// <returns>Reembolso em Submitted.</returns>
    public async Task<ServiceResult<ExpenseResponse>> SubmitAsync(ExpenseCaller caller, int id)
    {
        (Expense? expense, AccessDecision decision) = await FindForAsync(caller, id, ExpenseOperation.Submit);
        if (expense is null || decision != AccessDecision.Allowed)
        {
            return ServiceResult.Denied<ExpenseResponse>(decision, "Somente reembolsos em Draft podem ser enviados.");
        }

        RecordTransition(expense, caller, ExpenseAction.Submitted, ExpenseState.Submitted, justification: null);
        await _database.SaveChangesAsync();

        return ServiceResult.Success(ExpenseResponse.FromEntity(expense));
    }

    /// <summary>Lista os reembolsos visiveis ao chamador, filtrando no banco.</summary>
    /// <param name="caller">Usuario autenticado.</param>
    /// <returns>Reembolsos visiveis, do mais recente para o mais antigo.</returns>
    public async Task<ServiceResult<IReadOnlyList<ExpenseResponse>>> ListAsync(ExpenseCaller caller)
    {
        if (!ExpensePolicy.HasFunctionalAccess(caller))
        {
            return ServiceResult.Failure<IReadOnlyList<ExpenseResponse>>(
                ServiceStatus.Forbidden,
                "Operacao nao permitida para este usuario.");
        }

        List<Expense> expenses = await _database.Expenses
            .AsNoTracking()
            .Where(ExpensePolicy.VisibleTo(caller))
            .Include(e => e.Category)
            .OrderByDescending(e => e.Id)
            .ToListAsync();

        List<ExpenseResponse> result = expenses.ConvertAll(ExpenseResponse.FromEntity);

        return ServiceResult.Success<IReadOnlyList<ExpenseResponse>>(result);
    }

    /// <summary>Consulta o detalhe de um reembolso visivel ao chamador.</summary>
    /// <param name="caller">Usuario autenticado.</param>
    /// <param name="id">Identificador do reembolso.</param>
    /// <returns>Reembolso, ou 404 quando fora do escopo de leitura.</returns>
    public async Task<ServiceResult<ExpenseResponse>> GetAsync(ExpenseCaller caller, int id)
    {
        (Expense? expense, AccessDecision decision) = await FindForAsync(caller, id, ExpenseOperation.Read);
        if (expense is null || decision != AccessDecision.Allowed)
        {
            return ServiceResult.Denied<ExpenseResponse>(decision, string.Empty);
        }

        return ServiceResult.Success(ExpenseResponse.FromEntity(expense));
    }

    /// <summary>
    /// Carrega o reembolso e avalia a operacao. A role exigida e verificada
    /// antes da consulta, para que a existencia do recurso nao seja revelada.
    /// </summary>
    private async Task<(Expense? Expense, AccessDecision Decision)> FindForAsync(
        ExpenseCaller caller,
        int id,
        ExpenseOperation operation)
    {
        if (!ExpensePolicy.HasRequiredRole(caller, operation))
        {
            return (null, AccessDecision.Forbidden);
        }

        Expense? expense = await _database.Expenses
            .Include(e => e.Category)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (expense is null)
        {
            return (null, AccessDecision.NotFound);
        }

        return (expense, ExpensePolicy.Evaluate(caller, expense.OwnerId, expense.State, operation));
    }

    /// <summary>
    /// Aplica uma transicao de estado e registra o historico correspondente.
    /// Deve ser seguido de um unico SaveChanges, garantindo a atomicidade.
    /// </summary>
    private void RecordTransition(
        Expense expense,
        ExpenseCaller caller,
        ExpenseAction action,
        ExpenseState nextState,
        string? justification)
    {
        DateTime now = UtcNow();

        expense.History.Add(new ExpenseHistory
        {
            Action = action,
            ActorId = caller.UserId,
            OccurredAtUtc = now,
            PreviousState = expense.State,
            NextState = nextState,
            Justification = justification,
        });

        expense.State = nextState;
        expense.UpdatedAtUtc = now;
    }

    private static List<string> ApplyChanges(Expense expense, ExpenseRequest request)
    {
        List<string> changes = new();
        CultureInfo culture = CultureInfo.InvariantCulture;

        if (!string.Equals(expense.Description, request.Description, StringComparison.Ordinal))
        {
            changes.Add("Description alterada");
            expense.Description = request.Description;
        }

        decimal amount = request.Amount.GetValueOrDefault();
        if (expense.Amount != amount)
        {
            changes.Add(string.Format(culture, "Amount: {0:0.00} -> {1:0.00}", expense.Amount, amount));
            expense.Amount = amount;
        }

        DateOnly currentDate = DateOnly.FromDateTime(expense.ExpenseDate);
        DateOnly newDate = request.ExpenseDate.GetValueOrDefault();
        if (currentDate != newDate)
        {
            changes.Add(string.Format(culture, "ExpenseDate: {0:yyyy-MM-dd} -> {1:yyyy-MM-dd}", currentDate, newDate));
            expense.ExpenseDate = ToDateTime(newDate);
        }

        int categoryId = request.CategoryId.GetValueOrDefault();
        if (expense.CategoryId != categoryId)
        {
            changes.Add(string.Format(culture, "CategoryId: {0} -> {1}", expense.CategoryId, categoryId));
            expense.CategoryId = categoryId;
        }

        return changes;
    }

    private static DateTime ToDateTime(DateOnly date)
    {
        return date.ToDateTime(TimeOnly.MinValue);
    }

    private DateTime UtcNow()
    {
        return _timeProvider.GetUtcNow().UtcDateTime;
    }
}
