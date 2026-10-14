using System;
using System.Linq.Expressions;
using ExpenseHub.Api.Domain;

namespace ExpenseHub.Api.Services;

/// <summary>
/// Matriz de autorizacao dos reembolsos (docs/MATRIZ-AUTORIZACAO.md). Combina
/// role, ownership e estado sem depender de banco, para ser reutilizada pelo
/// servico e coberta por testes unitarios.
/// </summary>
/// <remarks>
/// Ordem de avaliacao de uma operacao: role exigida (403), visibilidade (404),
/// ownership (403) e estado esperado (409). Rascunhos sao privados do
/// proprietario; depois de enviado, o reembolso entra no fluxo e os atores da
/// transicao recebem 409 (e nao 404) quando o estado nao e o esperado, o que
/// garante conflito ao repetir uma aprovacao, reprovacao ou pagamento.
/// </remarks>
internal static class ExpensePolicy
{
    /// <summary>Indica se o chamador possui alguma role com acesso a reembolsos.</summary>
    /// <param name="caller">Usuario autenticado.</param>
    /// <returns><c>true</c> para Employee, Approver, Finance ou Auditor.</returns>
    internal static bool HasFunctionalAccess(ExpenseCaller caller)
    {
        return caller.IsEmployee || caller.IsApprover || caller.IsFinance || caller.IsAuditor;
    }

    /// <summary>Indica se o reembolso esta no escopo de leitura do chamador.</summary>
    /// <param name="caller">Usuario autenticado.</param>
    /// <param name="ownerId">Proprietario do reembolso.</param>
    /// <param name="state">Estado atual do reembolso.</param>
    /// <returns><c>true</c> quando o chamador pode consultar o reembolso.</returns>
    internal static bool CanRead(ExpenseCaller caller, string ownerId, ExpenseState state)
    {
        return caller.IsAuditor
            || (caller.IsEmployee && caller.Owns(ownerId))
            || (caller.IsApprover && state == ExpenseState.Submitted)
            || (caller.IsFinance && (state == ExpenseState.Approved || state == ExpenseState.Paid));
    }

    /// <summary>
    /// Filtro equivalente a <see cref="CanRead"/>, traduzivel pelo Entity
    /// Framework, para aplicar a visibilidade antes de materializar os dados.
    /// </summary>
    /// <param name="caller">Usuario autenticado.</param>
    /// <returns>Predicado de visibilidade.</returns>
    internal static Expression<Func<Expense, bool>> VisibleTo(ExpenseCaller caller)
    {
        string userId = caller.UserId;
        bool seesAll = caller.IsAuditor;
        bool seesOwn = caller.IsEmployee;
        bool seesSubmitted = caller.IsApprover;
        bool seesApprovedOrPaid = caller.IsFinance;

        return expense => seesAll
            || (seesOwn && expense.OwnerId == userId)
            || (seesSubmitted && expense.State == ExpenseState.Submitted)
            || (seesApprovedOrPaid
                && (expense.State == ExpenseState.Approved || expense.State == ExpenseState.Paid));
    }

    /// <summary>Avalia uma operacao sobre um reembolso existente.</summary>
    /// <param name="caller">Usuario autenticado.</param>
    /// <param name="ownerId">Proprietario do reembolso.</param>
    /// <param name="state">Estado atual do reembolso.</param>
    /// <param name="operation">Operacao pretendida.</param>
    /// <returns>Decisao com o status correspondente do contrato.</returns>
    internal static AccessDecision Evaluate(
        ExpenseCaller caller,
        string ownerId,
        ExpenseState state,
        ExpenseOperation operation)
    {
        if (!HasRequiredRole(caller, operation))
        {
            return AccessDecision.Forbidden;
        }

        bool visible = CanRead(caller, ownerId, state);

        return operation switch
        {
            ExpenseOperation.Read => visible ? AccessDecision.Allowed : AccessDecision.NotFound,
            ExpenseOperation.Edit or ExpenseOperation.Submit => EvaluateOwnerTransition(caller, ownerId, state, visible),
            _ => EvaluateReviewerTransition(caller, ownerId, state, visible, ExpectedState(operation)),
        };
    }

    /// <summary>Estado em que a operacao pode ser executada.</summary>
    /// <param name="operation">Operacao de escrita.</param>
    /// <returns>Estado esperado antes da transicao.</returns>
    internal static ExpenseState ExpectedState(ExpenseOperation operation)
    {
        return operation switch
        {
            ExpenseOperation.Edit or ExpenseOperation.Submit => ExpenseState.Draft,
            ExpenseOperation.Approve or ExpenseOperation.Reject => ExpenseState.Submitted,
            ExpenseOperation.Pay => ExpenseState.Approved,
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, "Operacao sem transicao."),
        };
    }

    /// <summary>Indica se o chamador possui a role exigida pela operacao.</summary>
    /// <param name="caller">Usuario autenticado.</param>
    /// <param name="operation">Operacao pretendida.</param>
    /// <returns><c>true</c> quando a role exigida esta presente.</returns>
    internal static bool HasRequiredRole(ExpenseCaller caller, ExpenseOperation operation)
    {
        return operation switch
        {
            ExpenseOperation.Read => HasFunctionalAccess(caller),
            ExpenseOperation.Edit or ExpenseOperation.Submit => caller.IsEmployee,
            ExpenseOperation.Approve or ExpenseOperation.Reject => caller.IsApprover,
            ExpenseOperation.Pay => caller.IsFinance,
            _ => false,
        };
    }

    private static AccessDecision EvaluateOwnerTransition(
        ExpenseCaller caller,
        string ownerId,
        ExpenseState state,
        bool visible)
    {
        if (!caller.Owns(ownerId))
        {
            return visible ? AccessDecision.Forbidden : AccessDecision.NotFound;
        }

        return state == ExpenseState.Draft ? AccessDecision.Allowed : AccessDecision.Conflict;
    }

    private static AccessDecision EvaluateReviewerTransition(
        ExpenseCaller caller,
        string ownerId,
        ExpenseState state,
        bool visible,
        ExpenseState expectedState)
    {
        if (!visible && state == ExpenseState.Draft)
        {
            return AccessDecision.NotFound;
        }

        if (caller.Owns(ownerId))
        {
            return AccessDecision.Forbidden;
        }

        return state == expectedState ? AccessDecision.Allowed : AccessDecision.Conflict;
    }
}
