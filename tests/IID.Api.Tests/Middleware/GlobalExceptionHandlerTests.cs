using System.Text.Json;
using FluentAssertions;
using FluentValidation.Results;
using IID.Api.Middleware;
using IID.Application.Common.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace IID.Api.Tests.Middleware;

public class GlobalExceptionHandlerTests
{
    private readonly GlobalExceptionHandler _sut = new(NullLogger<GlobalExceptionHandler>.Instance);

    private static (DefaultHttpContext context, MemoryStream body) CreateHttpContext(string path = "/api/test")
    {
        var context = new DefaultHttpContext();
        var body = new MemoryStream();
        context.Response.Body = body;
        context.Request.Path = path;
        return (context, body);
    }

    private static async Task<ProblemDetails> ReadProblemDetailsAsync(MemoryStream body)
    {
        body.Seek(0, SeekOrigin.Begin);
        var problem = await JsonSerializer.DeserializeAsync<ProblemDetails>(body, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        return problem!;
    }

    [Fact]
    public async Task TryHandleAsync_ShouldReturn404_WhenNotFoundException()
    {
        var (context, body) = CreateHttpContext();
        var ex = new NotFoundException("Vehicle", Guid.NewGuid());

        var handled = await _sut.TryHandleAsync(context, ex, CancellationToken.None);

        handled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound);

        var problem = await ReadProblemDetailsAsync(body);
        problem.Title.Should().Be("Resource not found");
        problem.Detail.Should().Contain("was not found");
        problem.Instance.Should().Be("/api/test");
    }

    [Fact]
    public async Task TryHandleAsync_ShouldReturn409_WhenConflictException()
    {
        var (context, body) = CreateHttpContext();
        var ex = new ConflictException("VIN already exists");

        var handled = await _sut.TryHandleAsync(context, ex, CancellationToken.None);

        handled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status409Conflict);

        var problem = await ReadProblemDetailsAsync(body);
        problem.Title.Should().Be("Conflict");
        problem.Detail.Should().Be("VIN already exists");
    }

    [Fact]
    public async Task TryHandleAsync_ShouldReturn400_WhenApplicationValidationException()
    {
        var (context, body) = CreateHttpContext();
        var errors = new Dictionary<string, string[]>
        {
            ["Make"] = ["Make is required"]
        };
        var ex = new ValidationException(errors);

        var handled = await _sut.TryHandleAsync(context, ex, CancellationToken.None);

        handled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);

        var problem = await ReadProblemDetailsAsync(body);
        problem.Title.Should().Be("Validation failed");
        problem.Extensions.Should().ContainKey("errors");
    }

    [Fact]
    public async Task TryHandleAsync_ShouldReturn400_WhenFluentValidationException()
    {
        var (context, body) = CreateHttpContext();
        var failures = new List<ValidationFailure>
        {
            new("Price", "Price must be greater than zero"),
            new("Price", "Price cannot be negative")
        };
        var ex = new FluentValidation.ValidationException(failures);

        var handled = await _sut.TryHandleAsync(context, ex, CancellationToken.None);

        handled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);

        var problem = await ReadProblemDetailsAsync(body);
        problem.Title.Should().Be("Validation failed");
        problem.Extensions.Should().ContainKey("errors");
    }

    [Fact]
    public async Task TryHandleAsync_ShouldReturn400_WhenInvalidOperationException()
    {
        var (context, body) = CreateHttpContext();
        var ex = new InvalidOperationException("Sold vehicles cannot be modified");

        var handled = await _sut.TryHandleAsync(context, ex, CancellationToken.None);

        handled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);

        var problem = await ReadProblemDetailsAsync(body);
        problem.Title.Should().Be("Invalid operation");
        problem.Detail.Should().Be("Sold vehicles cannot be modified");
    }

    [Fact]
    public async Task TryHandleAsync_ShouldReturn400_WhenArgumentException()
    {
        var (context, body) = CreateHttpContext();
        var ex = new ArgumentException("Dealership ID cannot be empty");

        var handled = await _sut.TryHandleAsync(context, ex, CancellationToken.None);

        handled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);

        var problem = await ReadProblemDetailsAsync(body);
        problem.Title.Should().Be("Invalid argument");
        problem.Detail.Should().Contain("Dealership ID cannot be empty");
    }

    [Fact]
    public async Task TryHandleAsync_ShouldReturn500_WhenUnhandledException()
    {
        var (context, body) = CreateHttpContext();
        var ex = new Exception("Fatal database crash");

        var handled = await _sut.TryHandleAsync(context, ex, CancellationToken.None);

        handled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);

        var problem = await ReadProblemDetailsAsync(body);
        problem.Title.Should().Be("Server error");
        problem.Detail.Should().Be("An unexpected error occurred.");
    }
}
