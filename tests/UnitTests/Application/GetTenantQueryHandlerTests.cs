using Application.Common.Interfaces;
using Application.Tenants.Queries;
using AwesomeAssertions;
using Domain.Entities;
using Moq;
using UnitTests.Common;

namespace UnitTests.Application;

[TestFixture]
public sealed class GetTenantQueryHandlerTests
{
    private Mock<IAppDbContext> _db = null!;
    private GetTenantQueryHandler _handler = null!;
    private Tenant _tenant = null!;

    [SetUp]
    public void SetUp()
    {
        _tenant = Tenant.Create("Greenfield Academy").Value!;

        _db = new Mock<IAppDbContext>();
        _db.Setup(x => x.Tenants).Returns(MockDbSet.Create<Tenant>([_tenant]).Object);

        _handler = new GetTenantQueryHandler(_db.Object);
    }

    [Test]
    public async Task Handle_ExistingId_ReturnsTenantDto()
    {
        var result = await _handler.Handle(new GetTenantQuery(_tenant.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(_tenant.Id);
        result.Value.Name.Should().Be(_tenant.Name);
        result.Value.CreatedAt.Should().Be(_tenant.CreatedAt);
    }

    [Test]
    public async Task Handle_UnknownId_ReturnsNotFound()
    {
        var result = await _handler.Handle(new GetTenantQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }
}
