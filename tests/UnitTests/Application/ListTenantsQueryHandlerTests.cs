using Application.Common.Interfaces;
using Application.Tenants.Queries;
using AwesomeAssertions;
using Domain.Entities;
using Moq;
using UnitTests.Common;

namespace UnitTests.Application;

[TestFixture]
public sealed class ListTenantsQueryHandlerTests
{
    private Mock<IAppDbContext> _db = null!;
    private ListTenantsQueryHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _db = new Mock<IAppDbContext>();
        _handler = new ListTenantsQueryHandler(_db.Object);
    }

    [Test]
    public async Task Handle_MultipleTenants_ReturnsThemOrderedByName()
    {
        var zeta = Tenant.Create("Zeta Academy").Value!;
        var alpha = Tenant.Create("Alpha Academy").Value!;
        _db.Setup(x => x.Tenants).Returns(MockDbSet.Create<Tenant>([zeta, alpha]).Object);

        var result = await _handler.Handle(new ListTenantsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value!.Select(t => t.Name).Should().ContainInOrder("Alpha Academy", "Zeta Academy");
    }

    [Test]
    public async Task Handle_NoTenants_ReturnsEmptyList()
    {
        _db.Setup(x => x.Tenants).Returns(MockDbSet.Create<Tenant>([]).Object);

        var result = await _handler.Handle(new ListTenantsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
