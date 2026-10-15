using System;
using System.Collections.Generic;
using ExpenseHub.Api.Dtos;
using ExpenseHub.Api.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests;

/// <summary>Testes unitarios das validacoes declarativas de entrada.</summary>
[TestClass]
public sealed class RequestValidationTests
{
    /// <summary>Os limites aceitos da descricao sao inclusivos.</summary>
    [TestMethod]
    [DataRow(9, false)]
    [DataRow(10, true)]
    [DataRow(500, true)]
    [DataRow(501, false)]
    public void ExpenseDescriptionRespectsLengthLimits(int length, bool expected)
    {
        ExpenseRequest request = ValidExpenseRequest();
        request.Description = new string('a', length);

        bool actual = RequestValidator.TryValidate(request, out _);

        Assert.AreEqual(expected, actual);
    }

    /// <summary>Os limites aceitos do valor sao inclusivos.</summary>
    [TestMethod]
    [DataRow("0", false)]
    [DataRow("0.01", true)]
    [DataRow("2147483647", true)]
    [DataRow("2147483648", false)]
    public void ExpenseAmountRespectsRange(string amountText, bool expected)
    {
        ExpenseRequest request = ValidExpenseRequest();
        request.Amount = decimal.Parse(amountText, System.Globalization.CultureInfo.InvariantCulture);

        bool actual = RequestValidator.TryValidate(request, out _);

        Assert.AreEqual(expected, actual);
    }

    /// <summary>Data futura e rejeitada.</summary>
    [TestMethod]
    public void FutureExpenseDateIsInvalid()
    {
        ExpenseRequest request = ValidExpenseRequest();
        request.ExpenseDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);

        bool actual = RequestValidator.TryValidate(request, out IDictionary<string, string[]> errors);

        Assert.IsFalse(actual);
        Assert.IsTrue(errors.ContainsKey(nameof(ExpenseRequest.ExpenseDate)));
    }

    /// <summary>A justificativa deve ter entre 10 e 500 caracteres.</summary>
    [TestMethod]
    [DataRow(0, false)]
    [DataRow(9, false)]
    [DataRow(10, true)]
    [DataRow(500, true)]
    [DataRow(501, false)]
    public void RejectJustificationRespectsLengthLimits(int length, bool expected)
    {
        RejectExpenseRequest request = new() { Justification = new string('a', length) };

        bool actual = RequestValidator.TryValidate(request, out _);

        Assert.AreEqual(expected, actual);
    }

    private static ExpenseRequest ValidExpenseRequest()
    {
        return new ExpenseRequest
        {
            CategoryId = 1,
            Description = "Despesa valida para o teste",
            Amount = 10.50m,
            ExpenseDate = DateOnly.FromDateTime(DateTime.UtcNow),
        };
    }
}
