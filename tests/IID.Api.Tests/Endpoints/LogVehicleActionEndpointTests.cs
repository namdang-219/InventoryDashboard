using System.Text.Json;
using FastEndpoints;
using FluentAssertions;
using IID.Api.Endpoints.VehicleActions;
using IID.Api.Endpoints.Vehicles;
using IID.Application.VehicleActions.Commands.LogVehicleAction;
using IID.Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace IID.Api.Tests.Endpoints;

public class LogVehicleActionEndpointTests
{
    [Fact]
    public async Task LogVehicleActionEndpoint_Should_Return201WithDataIdBody()
    {
        var actionId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var sender = new Mock<ISender>();
        sender.Setup(s => s.Send(It.IsAny<LogVehicleActionCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Guid>.Success(actionId));

        var ep = Factory.Create<LogVehicleActionEndpoint>(
            ctx => ctx.Request.RouteValues["id"] = vehicleId.ToString(),
            sender.Object);
        ep.HttpContext.Response.Body = new MemoryStream();

        await ep.HandleAsync(
            new LogVehicleActionRequest { ActionType = "ManagerReview", Notes = "Checked" },
            CancellationToken.None);

        ep.HttpContext.Response.StatusCode.Should().Be(StatusCodes.Status201Created);
        ep.HttpContext.Response.Headers.Location.ToString().Should().Be($"/api/v1/vehicle-actions/{actionId}");

        ep.HttpContext.Response.Body.Position = 0;
        using var doc = await JsonDocument.ParseAsync(ep.HttpContext.Response.Body);
        doc.RootElement.GetProperty("data").GetProperty("id").GetGuid().Should().Be(actionId);
    }

    [Fact]
    public async Task CreateVehicleActionEndpoint_Should_Return201WithDataIdBody()
    {
        var actionId = Guid.NewGuid();
        var sender = new Mock<ISender>();
        sender.Setup(s => s.Send(It.IsAny<LogVehicleActionCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Guid>.Success(actionId));

        var ep = Factory.Create<CreateVehicleActionEndpoint>(sender.Object);
        ep.HttpContext.Response.Body = new MemoryStream();

        await ep.HandleAsync(
            new CreateVehicleActionRequest
            {
                VehicleId = Guid.NewGuid(),
                ActionType = "PriceReductionPlanned",
                Notes = "Reduce 5%"
            },
            CancellationToken.None);

        ep.HttpContext.Response.StatusCode.Should().Be(StatusCodes.Status201Created);
        ep.HttpContext.Response.Headers.Location.ToString().Should().Be($"/api/v1/vehicle-actions/{actionId}");

        ep.HttpContext.Response.Body.Position = 0;
        using var doc = await JsonDocument.ParseAsync(ep.HttpContext.Response.Body);
        doc.RootElement.GetProperty("data").GetProperty("id").GetGuid().Should().Be(actionId);
    }
}

