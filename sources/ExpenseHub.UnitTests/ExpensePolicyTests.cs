using System;
using ExpenseHub.Api.Domain;
using ExpenseHub.Api.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests;

/// <summary>Testes unitarios da matriz de autorizacao de reembolsos.</summary>
[TestClass]
public sealed class ExpensePolicyTests
{
    /// <summary>Employee pode ler o proprio reembolso.</summary>
    [TestMethod]
    public void EmployeeReadsOwnExpense()
    {
        ExpenseCaller caller = Caller("owner", Roles.Employee);

        AccessDecision decision = ExpensePolicy.Evaluate(
            caller,
            "owner",
            ExpenseState.Draft,
            ExpenseOperation.Read);

        Assert.AreEqual(AccessDecision.Allowed, decision);
    }

    /// <summary>Employee nao descobre reembolso de outro usuario.</summary>
    [TestMethod]
    public void EmployeeCannotReadAnotherUsersExpense()
    {
        ExpenseCaller caller = Caller("employee", Roles.Employee);

        AccessDecision decision = ExpensePolicy.Evaluate(
            caller,
            "owner",
            ExpenseState.Draft,
            ExpenseOperation.Read);

        Assert.AreEqual(AccessDecision.NotFound, decision);
    }

    /// <summary>Editar ou enviar fora de Draft resulta em conflito.</summary>
    [TestMethod]
    [DataRow((int)ExpenseOperation.Edit)]
    [DataRow((int)ExpenseOperation.Submit)]
    public void OwnerTransitionOutsideDraftConflicts(int operationValue)
    {
        ExpenseCaller caller = Caller("owner", Roles.Employee);
        ExpenseOperation operation = (ExpenseOperation)operationValue;

        AccessDecision decision = ExpensePolicy.Evaluate(
            caller,
            "owner",
            ExpenseState.Submitted,
            operation);

        Assert.AreEqual(AccessDecision.Conflict, decision);
    }

    /// <summary>Approver pode aprovar ou reprovar um Submitted alheio.</summary>
    [TestMethod]
    [DataRow((int)ExpenseOperation.Approve)]
    [DataRow((int)ExpenseOperation.Reject)]
    public void ApproverTransitionsSubmittedExpense(int operationValue)
    {
        ExpenseCaller caller = Caller("approver", Roles.Approver);
        ExpenseOperation operation = (ExpenseOperation)operationValue;

        AccessDecision decision = ExpensePolicy.Evaluate(
            caller,
            "owner",
            ExpenseState.Submitted,
            operation);

        Assert.AreEqual(AccessDecision.Allowed, decision);
    }

    /// <summary>Approver nunca aprova nem reprova o proprio reembolso.</summary>
    [TestMethod]
    [DataRow((int)ExpenseOperation.Approve)]
    [DataRow((int)ExpenseOperation.Reject)]
    public void ApproverCannotTransitionOwnExpense(int operationValue)
    {
        ExpenseCaller caller = Caller("same-user", Roles.Employee, Roles.Approver);
        ExpenseOperation operation = (ExpenseOperation)operationValue;

        AccessDecision decision = ExpensePolicy.Evaluate(
            caller,
            "same-user",
            ExpenseState.Submitted,
            operation);

        Assert.AreEqual(AccessDecision.Forbidden, decision);
    }

    /// <summary>Repetir aprovacao ou reprovar depois da aprovacao resulta em conflito.</summary>
    [TestMethod]
    [DataRow((int)ExpenseOperation.Approve)]
    [DataRow((int)ExpenseOperation.Reject)]
    public void ApproverRepeatedTransitionConflicts(int operationValue)
    {
        ExpenseCaller caller = Caller("approver", Roles.Approver);
        ExpenseOperation operation = (ExpenseOperation)operationValue;

        AccessDecision decision = ExpensePolicy.Evaluate(
            caller,
            "owner",
            ExpenseState.Approved,
            operation);

        Assert.AreEqual(AccessDecision.Conflict, decision);
    }

    /// <summary>Rascunho alheio permanece oculto do Approver.</summary>
    [TestMethod]
    public void ApproverCannotDiscoverDraft()
    {
        ExpenseCaller caller = Caller("approver", Roles.Approver);

        AccessDecision decision = ExpensePolicy.Evaluate(
            caller,
            "owner",
            ExpenseState.Draft,
            ExpenseOperation.Approve);

        Assert.AreEqual(AccessDecision.NotFound, decision);
    }

    /// <summary>Finance pode pagar Approved alheio e recebe conflito ao repetir.</summary>
    [TestMethod]
    public void FinancePaymentRespectsState()
    {
        ExpenseCaller caller = Caller("finance", Roles.Finance);

        AccessDecision first = ExpensePolicy.Evaluate(
            caller,
            "owner",
            ExpenseState.Approved,
            ExpenseOperation.Pay);
        AccessDecision repeated = ExpensePolicy.Evaluate(
            caller,
            "owner",
            ExpenseState.Paid,
            ExpenseOperation.Pay);

        Assert.AreEqual(AccessDecision.Allowed, first);
        Assert.AreEqual(AccessDecision.Conflict, repeated);
    }

    /// <summary>Finance nunca paga o proprio reembolso.</summary>
    [TestMethod]
    public void FinanceCannotPayOwnExpense()
    {
        ExpenseCaller caller = Caller("same-user", Roles.Employee, Roles.Finance);

        AccessDecision decision = ExpensePolicy.Evaluate(
            caller,
            "same-user",
            ExpenseState.Approved,
            ExpenseOperation.Pay);

        Assert.AreEqual(AccessDecision.Forbidden, decision);
    }

    /// <summary>Auditor e Admin nao recebem permissao de escrita.</summary>
    [TestMethod]
    [DataRow(Roles.Auditor)]
    [DataRow(Roles.Admin)]
    public void ReadOnlyRolesCannotWrite(string role)
    {
        ExpenseCaller caller = Caller("user", role);

        AccessDecision decision = ExpensePolicy.Evaluate(
            caller,
            "owner",
            ExpenseState.Submitted,
            ExpenseOperation.Approve);

        Assert.AreEqual(AccessDecision.Forbidden, decision);
    }

    /// <summary>O filtro SQL e a regra em memoria concordam em todas as combinacoes relevantes.</summary>
    [TestMethod]
    public void DatabaseFilterMatchesInMemoryReadRule()
    {
        ExpenseCaller[] callers =
        {
            Caller("owner", Roles.Employee),
            Caller("reviewer", Roles.Approver),
            Caller("finance", Roles.Finance),
            Caller("auditor", Roles.Auditor),
            Caller("multi", Roles.Employee, Roles.Approver, Roles.Finance),
            Caller("admin", Roles.Admin),
        };

        foreach (ExpenseCaller caller in callers)
        {
            Func<Expense, bool> filter = ExpensePolicy.VisibleTo(caller).Compile();

            foreach (ExpenseState state in Enum.GetValues<ExpenseState>())
            {
                foreach (string ownerId in new[] { caller.UserId, "another-user" })
                {
                    Expense expense = new() { OwnerId = ownerId, State = state };
                    bool expected = ExpensePolicy.CanRead(caller, ownerId, state);

                    Assert.AreEqual(expected, filter(expense), $"Role/estado divergente: {state}, {ownerId}.");
                }
            }
        }
    }

    private static ExpenseCaller Caller(string userId, params string[] roles)
    {
        return new ExpenseCaller(userId, roles);
    }
}
