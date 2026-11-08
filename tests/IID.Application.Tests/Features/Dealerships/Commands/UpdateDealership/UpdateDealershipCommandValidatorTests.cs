using FluentAssertions;
using FluentValidation.TestHelper;
using IID.Application.Common.Interfaces;
using IID.Application.Features.Dealerships.Commands.UpdateDealership;
using Moq;

namespace IID.Application.Tests.Features.Dealerships.Commands.UpdateDealership;

public class UpdateDealershipCommandValidatorTests
{
    private readonly Mock<IDealershipRepository> _repo = new();

    private UpdateDealershipCommandValidator CreateSut(bool codeExists = false)
    {
        _repo.Setup(r => r.CodeExistsAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(codeExists);
        return new UpdateDealershipCommandValidator(_repo.Object);
    }

    private static UpdateDealershipCommand Valid(Guid? id = null) =>
        new(id ?? Guid.NewGuid(), "Downtown Motors", "DT01", "Austin", "TX", "555-1234");

    [Fact]
    public async Task Validate_Should_Pass_WhenValid()
    {
        var result = await CreateSut().TestValidateAsync(Valid());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_Should_Fail_WhenIdEmpty()
    {
        var result = await CreateSut().TestValidateAsync(Valid(Guid.Empty));

        result.ShouldHaveValidationErrorFor(c => c.Id);
    }

    [Fact]
    public async Task Validate_Should_ExcludeOwnId_WhenCheckingCodeUniqueness()
    {
        var id = Guid.NewGuid();

        await CreateSut().TestValidateAsync(Valid(id));

        _repo.Verify(r => r.CodeExistsAsync("DT01", id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Validate_Should_Fail_WhenAnotherDealershipUsesCode()
    {
        var result = await CreateSut(codeExists: true).TestValidateAsync(Valid());

        result.ShouldHaveValidationErrorFor(c => c.Code)
            .WithErrorMessage("Another dealership with this code already exists.");
    }

    [Fact]
    public async Task Validate_Should_ReportAllErrors_WhenMultipleFieldsInvalid()
    {
        var cmd = new UpdateDealershipCommand(Guid.Empty, new string('n', 151), "", "", "", new string('9', 51));

        var result = await CreateSut().TestValidateAsync(cmd);

        result.ShouldHaveValidationErrorFor(c => c.Id);
        result.ShouldHaveValidationErrorFor(c => c.Name);
        result.ShouldHaveValidationErrorFor(c => c.Code);
        result.ShouldHaveValidationErrorFor(c => c.City);
        result.ShouldHaveValidationErrorFor(c => c.State);
        result.ShouldHaveValidationErrorFor(c => c.Phone);
    }
}
