using FluentAssertions;
using FluentValidation.TestHelper;
using IID.Application.Common.Interfaces;
using IID.Application.Features.Dealerships.Commands.CreateDealership;
using Moq;

namespace IID.Application.Tests.Features.Dealerships.Commands.CreateDealership;

public class CreateDealershipCommandValidatorTests
{
    private readonly Mock<IDealershipRepository> _repo = new();

    private CreateDealershipCommandValidator CreateSut(bool codeExists = false)
    {
        _repo.Setup(r => r.CodeExistsAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(codeExists);
        return new CreateDealershipCommandValidator(_repo.Object);
    }

    private static CreateDealershipCommand Valid() =>
        new("Downtown Motors", "DT01", "Austin", "TX", "555-1234");

    [Fact]
    public async Task Validate_Should_Pass_WhenValid()
    {
        var result = await CreateSut().TestValidateAsync(Valid());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_Should_Pass_WhenPhoneEmpty()
    {
        var result = await CreateSut().TestValidateAsync(Valid() with { Phone = "" });

        result.ShouldNotHaveValidationErrorFor(c => c.Phone);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validate_Should_Fail_WhenNameBlank(string name)
    {
        var result = await CreateSut().TestValidateAsync(Valid() with { Name = name });

        result.ShouldHaveValidationErrorFor(c => c.Name);
    }

    [Fact]
    public async Task Validate_Should_RespectNameLengthBoundary()
    {
        var sut = CreateSut();

        (await sut.TestValidateAsync(Valid() with { Name = new string('a', 150) }))
            .ShouldNotHaveValidationErrorFor(c => c.Name);
        (await sut.TestValidateAsync(Valid() with { Name = new string('a', 151) }))
            .ShouldHaveValidationErrorFor(c => c.Name);
    }

    [Fact]
    public async Task Validate_Should_RespectCodeLengthBoundary()
    {
        var sut = CreateSut();

        (await sut.TestValidateAsync(Valid() with { Code = new string('A', 20) }))
            .ShouldNotHaveValidationErrorFor(c => c.Code);
        (await sut.TestValidateAsync(Valid() with { Code = new string('A', 21) }))
            .ShouldHaveValidationErrorFor(c => c.Code);
    }

    [Fact]
    public async Task Validate_Should_Fail_WhenCodeAlreadyExists()
    {
        var result = await CreateSut(codeExists: true).TestValidateAsync(Valid());

        result.ShouldHaveValidationErrorFor(c => c.Code)
            .WithErrorMessage("A dealership with this code already exists.");
        _repo.Verify(r => r.CodeExistsAsync("DT01", null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Validate_Should_ReportAllErrors_WhenMultipleFieldsInvalid()
    {
        var cmd = new CreateDealershipCommand("", "", "", new string('x', 51), new string('9', 51));

        var result = await CreateSut().TestValidateAsync(cmd);

        result.ShouldHaveValidationErrorFor(c => c.Name);
        result.ShouldHaveValidationErrorFor(c => c.Code);
        result.ShouldHaveValidationErrorFor(c => c.City);
        result.ShouldHaveValidationErrorFor(c => c.State);
        result.ShouldHaveValidationErrorFor(c => c.Phone);
    }
}
